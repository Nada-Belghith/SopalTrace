import { defineStore } from 'pinia';
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
      this.sections = [];
      this.error = null;
    },

    async loadAllPlans(includeArchived = false) {
      this.isLoading = true;
      try {
        const response = await api.get(`/planrccf?includeArchived=${includeArchived}`);
        this.plans = response.data;
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
        const response = await api.get(`/planrccf/${id}`);
        const data = response.data;
        if (typeof data.configurationJson === 'string' && data.configurationJson) {
          try { data.configurationJson = JSON.parse(data.configurationJson); } catch(e) { data.configurationJson = []; }
        } else if (!data.configurationJson) {
          data.configurationJson = [];
        }
        this.entete = data;
        this.sections = response.data.sections || [];
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
        const payload = {
          posteCode: this.entete.posteCode,
          formulaireId: this.entete.formulaireId,
          nom: this.entete.nom,
          remarques: this.entete.remarques || this.entete.notes,
          legendeMoyens: this.entete.legendeMoyens,
          configurationJson: typeof this.entete.configurationJson === 'object' ? JSON.stringify(this.entete.configurationJson) : this.entete.configurationJson,
          sections: this.sections
        };

        let response;
        if (this.entete.id) {
          response = await api.put(`/planrccf/${this.entete.id}`, payload);
        } else {
          response = await api.post('/planrccf', payload);
        }
        
        let data = response.data;
        if (typeof data.configurationJson === 'string' && data.configurationJson) {
          try { data.configurationJson = JSON.parse(data.configurationJson); } catch(e) { data.configurationJson = []; }
        } else if (!data.configurationJson) {
          data.configurationJson = [];
        }
        this.entete = data;
        this.sections = data.sections || [];
        return { success: true, planId: data.id };
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
      try {
        await api.post(`/planrccf/${this.entete.id}/valider`);
        await this.chargerPlan(this.entete.id);
        return { success: true };
      } catch (error) {
        return { success: false, message: error.response?.data?.message || 'Erreur de validation' };
      }
    },

    async annulerValidation() {
      try {
        await api.post(`/planrccf/${this.entete.id}/annuler-validation`);
        await this.chargerPlan(this.entete.id);
        return { success: true };
      } catch (error) {
        return { success: false, message: error.response?.data?.message || "Erreur d'annulation" };
      }
    },

    async creerNouvelleVersion() {
      try {
        const res = await api.post(`/planrccf/${this.entete.id}/nouvelle-version`);
        return { success: true, newPlanId: res.data.id };
      } catch (error) {
        return { success: false, message: error.response?.data?.message || 'Erreur' };
      }
    },

    async archiverPlan() {
      try {
        await api.post(`/planrccf/${this.entete.id}/archive`);
        await this.chargerPlan(this.entete.id);
        return { success: true };
      } catch (error) {
        return { success: false, message: error.response?.data?.message || "Erreur d'archivage" };
      }
    },

    async importExcel(file) {
      try {
        const formData = new FormData();
        formData.append('file', file);
        const res = await api.post('/planrccf/import-excel', formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
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

