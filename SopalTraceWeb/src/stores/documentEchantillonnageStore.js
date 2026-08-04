import { defineStore } from 'pinia';
import apiClient from '@/services/apiClient';
import { genererUid } from '@/utils/uuidUtils';
import { useReferentielStore } from './referentielStore';

export const useDocumentEchantillonnageStore = defineStore('documentEchantillonnage', {
  state: () => ({
    entete: {
      id: null,
      niveauControle: 'I',
      typePlan: 'SIMPLE',
      modeControle: 'NORMAL',
      nqaId: null,
      valeurNqa: 0.65,
      version: 1,
      statut: 'BROUILLON',
      remarques: '',
      legendeMoyens: '',
      formulaireId: null,
      formulaireCodeReference: null
    },
    regles: [],

    // Dicos are in useReferentielStore
    isLoading: false,
    plansExistants: []
  }),

  getters: {
    isReadOnly: (state) => state.entete.statut === 'ARCHIVE'
  },

  actions: {
    async fetchPlansExistants() {
      try {
        const res = await apiClient.get('/documents-echantillonnage/actif');
        if (res.data && res.data.data) {
           this.plansExistants = [res.data.data];
        } else {
           this.plansExistants = [];
        }
      } catch (error) {
        console.error("Erreur fetchPlansExistants:", error);
      }
    },

    async loadDocument(id) {
      this.isLoading = true;
      try {
        const res = await apiClient.get(`/documents-echantillonnage/${id}`);
        const data = res.data.data;
        this.entete = { ...data };
        
        const rawRegles = data.regles || (data.donnees && data.donnees.regles) || [];
        this.regles = rawRegles.map(r => ({ ...r, _uid: genererUid() }));
      } finally {
        this.isLoading = false;
      }
    },

    addRule() {
      this.regles.push({
        _uid: genererUid(),
        tailleMinLot: null,
        tailleMaxLot: null,
        lettreCode: '',
        effectifEchantillonA: 0,
        nbPostesB: 1,
        effectifParPosteAb: 0,
        critereAcceptationAc: 0,
        critereRejetRe: 1
      });
    },

    deleteRule(uid) {
      this.regles = this.regles.filter(r => r._uid !== uid);
    },

    async createDocument() {
      const payload = {
        ...this.entete,
        donnees: {
          regles: this.regles
        }
      };
      const res = await apiClient.post('/documents-echantillonnage', payload);
      this.entete.id = res.data.data;
      this.entete.statut = 'ACTIF';
    },

    async updateDocument() {
      const payload = {
        ...this.entete,
        regles: this.regles
      };
      const res = await apiClient.put(`/documents-echantillonnage/${this.entete.id}`, payload);
      this.entete.statut = 'ACTIF';
    },

    async createNewVersion(motif) {
      const payload = {
        ancienId: this.entete.id,
        modifiePar: 'ADMIN',
        motifModification: motif,
        donnees: {
          ...this.entete,
          regles: this.regles
        }
      };
      const res = await apiClient.post('/documents-echantillonnage/nouvelle-version', payload);
      this.entete.id = res.data.data;
      this.entete.version = (this.entete.version || 1) + 1;
    },

    async activerPlan() {
      const res = await apiClient.put(`/documents-echantillonnage/${this.entete.id}/activer`);
      this.entete.statut = 'ACTIF';
      return res.data;
    }
  }
});
