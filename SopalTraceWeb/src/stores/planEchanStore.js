import { defineStore } from 'pinia';
import apiClient from '@/services/apiClient';
import { genererUid } from '@/utils/uuidUtils';

export const usePlanEchanStore = defineStore('planEchan', {
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
      legendeMoyens: ''
    },
    regles: [],

    // Dicos
    nqaList: [],
    formulaires: [],
    isLoading: false,
    isDicosLoaded: false
  }),

  getters: {
    isReadOnly: (state) => state.entete.statut === 'ARCHIVE'
  },

  actions: {
    async fetchDictionnaires() {
      try {
        const res = await apiClient.get('/referentiels/plans-nc'); // Reusing some shared dicos or specialized
        // Wait, sampling might need its own referentials if different
        // For now let's assume we need NQA and Formulaires
        const nqaRes = await apiClient.get('/referentiels/fabrication');
        this.nqaList = (nqaRes.data.data.nqa || []).map(n => ({
          ...n,
          valeurNqa: Number(n.code || n.libelle)
        }));
        this.formulaires = nqaRes.data.data.formulaires || [];
        this.isDicosLoaded = true;

        // Ensure an NQA is selected by default if available
        if (this.nqaList.length > 0 && !this.entete.nqaId) {
          this.entete.nqaId = this.nqaList[0].id;
        }
      } catch (error) {
        console.error("Erreur dicos echantillonnage:", error);
      }
    },

    async chargerPlan(id) {
      this.isLoading = true;
      try {
        const res = await apiClient.get(`/plans-echantillonnage/${id}`);
        const data = res.data.data;
        this.entete = { ...data };
        this.regles = data.regles.map(r => ({ ...r, _uid: genererUid() }));
      } finally {
        this.isLoading = false;
      }
    },

    async getPlanActif() {
      try {
        const res = await apiClient.get('/plans-echantillonnage/actif');
        return res.data.data;
      } catch (error) {
        console.error("Erreur lors de la récupération du plan actif:", error);
        return null;
      }
    },

    ajouterRegle() {
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

    supprimerRegle(uid) {
      this.regles = this.regles.filter(r => r._uid !== uid);
    },

    async sauvegarderPlan() {
      const payload = {
        ...this.entete,
        regles: this.regles
      };

      if (this.entete.id) {
        await apiClient.put(`/plans-echantillonnage/${this.entete.id}`, payload);
      } else {
        const res = await apiClient.post('/plans-echantillonnage', payload);
        // Stocker l'ID retourné pour que activerPlan puisse l'utiliser
        this.entete.id = res.data.data;
      }
    },

    async creerNouvelleVersion(motif) {
      const payload = {
        ancienId: this.entete.id,
        modifiePar: 'ADMIN',
        motifModification: motif,
        donnees: {
          ...this.entete,
          regles: this.regles
        }
      };
      const res = await apiClient.post('/plans-echantillonnage/nouvelle-version', payload);
      return res.data;
    },

    async restaurerPlan(motif) {
      const res = await apiClient.post('/plans-echantillonnage/restaurer', {
        archiveId: this.entete.id,
        modifiePar: 'ADMIN',
        motifRestauration: motif
      });
      return res.data;
    },

    async activerPlan() {
      const res = await apiClient.put(`/plans-echantillonnage/${this.entete.id}/activer`);
      this.entete.statut = 'ACTIF';
      return res.data;
    }
  }
});
