import { defineStore } from 'pinia';
import apiClient from '@/services/apiClient';

export const useExecEncfStore = defineStore('execEncf', {
  state: () => ({
    execData: null,
    planSourceData: null,
    loading: false,
    error: null,
  }),
  actions: {
    async loadExecById(id) {
      this.loading = true;
      this.error = null;
      try {
        const response = await apiClient.get(`/api/exec/encf/${id}`);
        this.execData = response.data;
        if (this.execData.planSourceId) {
          const planRes = await apiClient.get(`/planrccf/${this.execData.planSourceId}`);
          this.planSourceData = planRes.data;
        }
        this.ensureTranches(this.execData);
      } catch (err) {
        this.error = err.response?.data?.message || 'Erreur lors du chargement';
      } finally {
        this.loading = false;
      }
    },

    async loadOrCreateExecByOf(numeroOf, posteCode) {
      this.loading = true;
      this.error = null;
      try {
        const response = await apiClient.get(`/api/exec/encf/by-of/${numeroOf}/poste/${posteCode}`);
        this.execData = response.data;
        if (this.execData.planSourceId) {
          const planRes = await apiClient.get(`/planrccf/${this.execData.planSourceId}`);
          this.planSourceData = planRes.data;
        }
        this.ensureTranches(this.execData);
      } catch (err) {
        this.error = err.response?.data?.message || 'Erreur lors du chargement';
      } finally {
        this.loading = false;
      }
    },

    async saveExec() {
      this.loading = true;
      this.error = null;
      try {
        const response = await apiClient.post('/api/exec/encf', this.execData);
        this.execData = response.data;
        this.ensureTranches(this.execData);
        return { success: true };
      } catch (err) {
        this.error = err.response?.data?.message || 'Erreur de sauvegarde';
        return { success: false, message: this.error };
      } finally {
        this.loading = false;
      }
    },

    // Ensure 24 hourly tranches exist (from 6h to 6h next day)
    ensureTranches(exec) {
      if (!exec) return;
      if (!exec.tranches) exec.tranches = [];
      
      const orderedHours = [
        "6h - 7h", "7h - 8h", "8h - 9h", "9h - 10h", "10h - 11h", "11h - 12h",
        "12h - 13h", "13h - 14h", "14h - 15h", "15h - 16h", "16h - 17h", "17h - 18h",
        "18h - 19h", "19h - 20h", "20h - 21h", "21h - 22h", "22h - 23h", "23h - 00h",
        "00h - 1h", "1h - 2h", "2h - 3h", "3h - 4h", "4h - 5h", "5h - 6h"
      ];

      const newTranches = [];
      
      let baseDate = new Date();
      baseDate.setHours(6, 0, 0, 0);

      for (let i = 0; i < orderedHours.length; i++) {
        const trancheLabel = orderedHours[i];
        let existing = exec.tranches.find(t => t.trancheHoraire === trancheLabel);
        
        if (existing) {
          newTranches.push(existing);
        } else {
          let debut = new Date(baseDate);
          debut.setHours(baseDate.getHours() + i);
          
          let fin = new Date(debut);
          fin.setHours(fin.getHours() + 1);

          newTranches.push({
            id: null,
            execControleOFId: exec.id,
            trancheHoraire: trancheLabel,
            heureDebut: debut.toISOString(),
            heureFin: fin.toISOString(),
            resultatFinal: null,
            detailsNC: '',
            actionsCorrection: '',
            matriculeApprobateur: ''
          });
        }
      }

      exec.tranches = newTranches;
    }
  }
});
