import { defineStore } from 'pinia';
import { rccfService } from '@/services/rccfService';
import api from '@/services/apiClient';

export const usePlanRccfStore = defineStore('planRccf', {
  state: () => ({
    plans: [],
    entete: null,
    sections: [],
    isLoading: false,
    error: null
  }),

  actions: {
    resetCurrentPlan() {
      this.entete = {
        posteCode: '',
        formulaireId: null,
        nom: '',
        remarques: '',
        notes: '',
        legendeMoyens: '',
        configurationJson: [],
      };
      this.sections = [{
        ordreAffiche: 1,
        libelleSection: 'Section par défaut',
        lignes: [{
          _uid: Date.now().toString(),
          ordreAffiche: 1,
          libelleAffiche: '',
          estCritique: false
        }]
      }];
      this.error = null;
    },

    async loadAllPlans(includeArchived = false) {
      this.isLoading = true;
      try {
        const response = await rccfService.getTousLesPlans();
        this.plans = response.data.data || [];
      } catch (error) {
        this.error = error.response?.data?.message || 'Erreur lors du chargement des plans';
        throw error;
      } finally {
        this.isLoading = false;
      }
    },

    async chargerPlan(id) {
      this.isLoading = true;
      try {
        const res = await rccfService.getPlan(id);
        const data = res.data;
        if (typeof data.configurationColonnesJson === 'string' && data.configurationColonnesJson) {
          try { data.configurationJson = JSON.parse(data.configurationColonnesJson); } catch (e) { data.configurationJson = []; }
        } else if (!data.configurationColonnesJson) {
          data.configurationJson = [];
        }

        // Restore entete
        this.entete = {
          id: data.id,
          posteCode: data.posteCode,
          nom: data.nom,
          remarques: data.remarques,
          legendeMoyens: data.legendeMoyens,
          formulaireId: data.formulaireId,
          formulaireCodeReference: data.formulaireCodeReference,
          version: data.version,
          statut: data.statut,
          configurationJson: data.configurationJson
        };

        // Restore sections from Document generic structure
        if (data.sections && data.sections.length > 0) {
          this.sections = data.sections.map(s => ({
            id: s.id,
            sectionType: s.notes || s.libelleSection,
            libelleAffiche: s.libelleSection,
            ordreAffiche: s.ordreAffiche,
            lignes: (s.lignes || []).map(l => ({
              id: l.id,
              caracteristique: l.libelleAffiche,
              limiteSpecTexte: l.limiteSpecTexte,
              observations: l.observations,
              ordreAffiche: l.ordreAffiche
            })).sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0))
          })).sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0));
        } else {
          this.sections = [];
        }
      } catch (error) {
        this.error = error.response?.data?.message || 'Erreur lors du chargement du plan';
        throw error;
      } finally {
        this.isLoading = false;
      }
    },

    async savePlan() {
      this.isLoading = true;
      try {
        // Build document payload
        const payload = {
          TypeDocumentCode: 'RESULTAT_CF',
          PosteCode: this.entete.posteCode,
          RefFormulaireCodeReference: this.entete.formulaireCodeReference,
          Nom: this.entete.nom,
          Remarques: this.entete.remarques || this.entete.notes,
          LegendeMoyens: this.entete.legendeMoyens,
          ConfigurationColonnesJson: typeof this.entete.configurationJson === 'object' ? JSON.stringify(this.entete.configurationJson) : this.entete.configurationJson,
          Sections: this.sections.map((s, idx) => ({
            Id: s.id,
            OrdreAffiche: s.ordreAffiche || (idx + 1),
            LibelleSection: s.libelleAffiche || s.sectionType,
            Notes: s.sectionType,
            Lignes: (s.lignes || []).map((l, lIdx) => ({
              Id: l.id,
              OrdreAffiche: l.ordreAffiche || (lIdx + 1),
              LibelleAffiche: l.caracteristique,
              LimiteSpecTexte: l.limiteSpecTexte,
              Observations: l.observations
            }))
          }))
        };

        let response;
        if (this.entete.id) {
          if (this.entete.statut === 'ACTIF' || this.entete.statut === 'ARCHIVE') {
            // Document est actif, la modification crée une nouvelle version brouillon
            payload.ancienId = this.entete.id;
            response = await rccfService.creerNouvelleVersion(payload);
            let newId = response.data.id;
            this.entete.id = newId;
            await this.chargerPlan(newId);
            return { success: true, planId: newId };
          } else {
            // Document est en brouillon, mise à jour simple
            response = await rccfService.mettreAJourPlan(this.entete.id, payload);
            await this.chargerPlan(this.entete.id); // Reload to ensure frontend and backend are in sync
            return { success: true, planId: this.entete.id };
          }
        } else {
          response = await rccfService.creerPlan(payload);
          let newId = response.data.id;
          this.entete.id = newId; // Set local ID immediately
          await this.chargerPlan(newId); // Re-fetch from backend to get generated section and line IDs
          return { success: true, planId: newId };
        }
      } catch (error) {
        let msg = error.response?.data?.message || 'Erreur de sauvegarde';
        if (error.response?.data?.errors) {
          const errs = error.response.data.errors;
          msg = Object.values(errs).flat().join(' | ');
        }
        return { success: false, message: msg };
      } finally {
        this.isLoading = false;
      }
    },

    async validerPlan() {
      // Logic for validation via Document generic process if needed
      // Temporarily doing nothing if validation is not strictly required in TPH yet
      try {
        this.entete.statut = 'ACTIF';
        await this.savePlan();
        return { success: true };
      } catch (error) {
        return { success: false, message: 'Erreur de validation' };
      }
    },

    async annulerValidation() {
      try {
        this.entete.statut = 'BROUILLON';
        await this.savePlan();
        return { success: true };
      } catch (error) {
        return { success: false, message: "Erreur d'annulation" };
      }
    },

    async creerNouvelleVersion() {
      try {
        const payload = {
          TypeDocumentCode: 'RESULTAT_CF',
          AncienId: this.entete.id,
          PosteCode: this.entete.posteCode,
          RefFormulaireCodeReference: this.entete.formulaireCodeReference,
          Nom: this.entete.nom,
          Remarques: this.entete.remarques,
          LegendeMoyens: this.entete.legendeMoyens,
          ConfigurationColonnesJson: typeof this.entete.configurationJson === 'object' ? JSON.stringify(this.entete.configurationJson) : this.entete.configurationJson,
          Sections: this.sections.map((s, idx) => ({
            OrdreAffiche: s.ordreAffiche || (idx + 1),
            LibelleSection: s.libelleAffiche || s.sectionType,
            Notes: s.sectionType,
            Lignes: (s.lignes || []).map((l, lIdx) => ({
              OrdreAffiche: l.ordreAffiche || (lIdx + 1),
              LibelleAffiche: l.caracteristique,
              LimiteSpecTexte: l.limiteSpecTexte,
              Observations: l.observations
            }))
          }))
        };
        const res = await rccfService.creerNouvelleVersion(payload);
        return { success: true, newPlanId: res.data.id };
      } catch (error) {
        return { success: false, message: error.response?.data?.message || 'Erreur' };
      }
    },

    async archiverPlan() {
      try {
        await api.put(`/hub/plans/RC/${this.entete.id}/statut?statut=ARCHIVE`);
        await this.chargerPlan(this.entete.id);
        return { success: true };
      } catch (error) {
        return { success: false, message: error.response?.data?.message || "Erreur d'archivage" };
      }
    },

    async importExcel(file) {
      try {
        const res = await rccfService.importerExcel(file);

        const importedSections = res.data?.sections || res.data;
        const importedRemarques = res.data?.remarques;

        // Merge imported lines into existing sections
        if (importedSections && importedSections.length > 0) {
          importedSections.forEach(importedSec => {
            const existSection = this.sections.find(s => s.sectionType === importedSec.sectionType);
            if (existSection) {
              existSection.lignes = importedSec.lignes || [];
              if (importedSec.libelleAffiche) {
                existSection.libelleAffiche = importedSec.libelleAffiche;
              }
            } else {
              // Fallback if section somehow wasn't predefined
              importedSec.ordreAffiche = this.sections.length + 1;
              this.sections.push(importedSec);
            }
          });
        }

        if (importedRemarques && this.entete) {
          if (!this.entete.notes) {
            this.entete.notes = importedRemarques.trim();
          } else {
            this.entete.notes += "\n" + importedRemarques.trim();
          }
        }

        return { success: true };
      } catch (error) {
        return { success: false, message: error.response?.data?.message || 'Erreur importation' };
      }
    }
  }
});

