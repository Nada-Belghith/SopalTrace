import { defineStore } from 'pinia';
import { documentRccfService } from '@/services/documentRccfService';
import { documentService } from '@/services/documentService';
import api from '@/services/apiClient';
import { mapSectionForBackend, hydrateSectionFromBackend, mapImportedSection } from '@/utils/sectionUtils';
import { useReferentielStore } from './referentielStore';

export const usedocumentRccfStore = defineStore('DocumentRccf', {
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
        id: null,
        statut: null,
        version: null,
        posteCode: null,
        formulaireId: null,
        formulaireCodeReference: null,
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
        const response = await documentRccfService.getAllDocuments();
        const data = response?.data;
        this.plans = Array.isArray(data) ? data : (data?.data || []);
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
        const res = await documentRccfService.getDocumentRccf(id);
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

        const refStore = useReferentielStore();

        // Restore sections from Document generic structure
        if (data.sections && data.sections.length > 0) {
          this.sections = data.sections.map(s => {
            const hydrated = hydrateSectionFromBackend(s, refStore.periodicites, refStore.reglesEchantillonnage);
            return {
              ...hydrated,
              sectionType: s.notes || hydrated.sectionType || s.libelleSection,
              libelleAffiche: s.libelleSection,
              lignes: (s.lignes || []).map(l => ({
                ...l,
                caracteristique: l.libelleAffiche || l.caracteristique,
                libelleAffiche: l.libelleAffiche || l.caracteristique
              })).sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0))
            };
          }).sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0));
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

    async savePlan(isCorrection = false) {
      this.isLoading = true;
      try {
        const refStore = useReferentielStore();
        const payloadSections = this.sections.map((s, idx) => {
          const mapped = mapSectionForBackend(s, idx, refStore.periodicites);
          return {
            ...mapped,
            Notes: s.sectionType || s.notes || mapped.notes
          };
        });

        // Build document payload
        const payload = {
          TypeDocumentCode: 'RESULTAT_CF',
          PosteCode: this.entete.posteCode,
          FormulaireId: this.entete.formulaireId,
          RefFormulaireCodeReference: this.entete.formulaireCodeReference,
          Nom: this.entete.nom,
          Remarques: this.entete.remarques || this.entete.notes,
          LegendeMoyens: this.entete.legendeMoyens,
          ConfigurationColonnesJson: typeof this.entete.configurationJson === 'object' ? JSON.stringify(this.entete.configurationJson) : this.entete.configurationJson,
          Sections: payloadSections
        };

        let response;
        if (this.entete.id) {
          if (!isCorrection && (this.entete.statut === 'ACTIF' || this.entete.statut === 'ARCHIVE')) {
            // Document est actif, la modification crée une nouvelle version brouillon
            payload.ancienId = this.entete.id;
            response = await documentService.createNewVersion(payload);
            let newId = response.id || response.data?.id;
            this.entete.id = newId;
            await this.chargerPlan(newId);
            return { success: true, planId: newId };
          } else {
            // Document est en brouillon, mise à jour simple, ou correction mineure
            response = await documentService.updateDocument(this.entete.id, payload);
            await this.chargerPlan(this.entete.id); // Reload to ensure frontend and backend are in sync
            return { success: true, planId: this.entete.id };
          }
        } else {
          response = await documentService.createDocument(payload);
          let newId = response.id || response.data?.id;
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

    async createNewVersion(motif) {
      try {
        const refStore = useReferentielStore();
        const payloadSections = this.sections.map((s, idx) => {
          const mapped = mapSectionForBackend(s, idx, refStore.periodicites);
          return {
            ...mapped,
            Notes: s.sectionType || s.notes || mapped.notes
          };
        });

        const payload = {
          TypeDocumentCode: 'RESULTAT_CF',
          ancienId: this.entete.id,
          PosteCode: this.entete.posteCode,
          FormulaireId: this.entete.formulaireId,
          RefFormulaireCodeReference: this.entete.formulaireCodeReference,
          Nom: this.entete.nom,
          Remarques: this.entete.remarques,
          LegendeMoyens: this.entete.legendeMoyens,
          MotifModification: motif,
          ConfigurationColonnesJson: typeof this.entete.configurationJson === 'object' ? JSON.stringify(this.entete.configurationJson) : this.entete.configurationJson,
          Sections: payloadSections
        };
        const res = await documentService.createNewVersion(payload);
        return { success: true, newPlanId: res.id || res.data?.id };
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
        const res = await documentRccfService.importExcel(file);

        const importedSections = res.data?.sections || res.data;
        const importedRemarques = res.data?.remarques;

        // Merge imported lines into existing sections
        if (importedSections && importedSections.length > 0) {
          const refStore = useReferentielStore();
          importedSections.forEach(importedSec => {
            const mapped = mapImportedSection(importedSec, refStore.reglesEchantillonnage);
            const existSection = this.sections.find(s => s.sectionType === importedSec.sectionType || s.sectionType === importedSec.notes);
            if (existSection) {
              Object.assign(existSection, {
                typeSectionId: mapped.typeSectionId || existSection.typeSectionId,
                periodiciteId: mapped.periodiciteId || existSection.periodiciteId,
                regleEchantillonnageId: mapped.regleEchantillonnageId || existSection.regleEchantillonnageId,
                modeFreq: mapped.modeFreq || existSection.modeFreq,
                freqNum: mapped.freqNum || existSection.freqNum,
                typeVariable: mapped.typeVariable || existSection.typeVariable,
                freqHours: mapped.freqHours || existSection.freqHours,
                frequenceLibelle: mapped.frequenceLibelle || existSection.frequenceLibelle,
                lignes: importedSec.lignes || []
              });
              if (importedSec.libelleAffiche) {
                existSection.libelleAffiche = importedSec.libelleAffiche;
              }
            } else {
              importedSec.ordreAffiche = this.sections.length + 1;
              this.sections.push({ ...mapped, sectionType: importedSec.sectionType || importedSec.notes });
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

