import { defineStore } from 'pinia';
import apiClient from '@/services/apiClient';
import { referentielsService } from '@/services/referentielsService';

export const useReferentielStore = defineStore('referentiel', {
  state: () => ({
    postesTravail: [],
    formulaires: [],
    isPostesLoaded: false,
    isFormulairesLoaded: false
  }),
  actions: {
    async fetchPostesTravail() {
      if (this.isPostesLoaded) return;
      try {
        // You can use a generic endpoint or the one from planNc/dictionnaires
        const res = await apiClient.get('/referentiels/plans-nc');
        if (res.data.success && res.data.data.postes) {
          this.postesTravail = res.data.data.postes;
          this.isPostesLoaded = true;
        }
      } catch (error) {
        console.error('Erreur chargement postes de travail:', error);
      }
    },
    async fetchFormulaires() {
      if (this.isFormulairesLoaded) return;
      try {
        const res = await referentielsService.getFormulairesListByRole('RESULTAT_CONTROLE_CF');
        if (res.data && res.data.data) {
          this.formulaires = res.data.data;
          this.isFormulairesLoaded = true;
        }
      } catch (error) {
        console.error('Erreur chargement formulaires:', error);
      }
    }
  }
});

