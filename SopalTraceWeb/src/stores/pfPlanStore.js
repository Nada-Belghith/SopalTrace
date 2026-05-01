import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { pfPlanService } from '@/services/pfPlanService';

export const usePfPlanStore = defineStore('pfPlan', () => {
  // --- DICTIONNAIRES ---
  const typesRobinet = ref([]);
  const typesCaracteristique = ref([]);
  const typesControle = ref([]);
  const moyensControle = ref([]);
  const periodicites = ref([]);
  const typesSection = ref([]); 
  const instruments = ref([]); 
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU PLAN ---
  const entete = ref({
    id: null,
    typeRobinetCode: '',
    designation: '',
    commentaireVersion: '',
    version: 1,
    statut: 'ACTIF',
    dateApplication: null,
    creePar: '',
    creeLe: null,
    remarques: '',
    legendeMoyens: '',
  });
  
  const sections = ref([]);
  const isLoading = ref(false);

  // --- UTILITAIRES DE MAPPING ---
  const mapSectionsForBackend = () => {
    return (sections.value || []).map((s, sIdx) => ({
      id: s.id,
      typeSectionId: s.typeSectionId || null,
      libelleSection: s.libelleSection || s.nom || `Section ${sIdx + 1}`,
      notes: s.notes || null,
      ordreAffiche: s.ordreAffiche ?? sIdx,
      lignes: (s.lignes || []).map((l, lIdx) => ({
        id: l.id,
        ordreAffiche: l.ordreAffiche ?? lIdx,
        typeCaracteristiqueId: l.typeCaracteristiqueId || null,
        libelleAffiche: l.libelleAffiche || null,
        typeControleId: l.typeControleId || null,
        moyenControleId: l.moyenControleId || null,
        instrumentCode: l.instrumentCode || null,
        moyenTexteLibre: l.moyenTexteLibre || null,
        valeurNominale: l.valeurNominale ?? null,
        toleranceSuperieure: l.toleranceSuperieure ?? null,
        toleranceInferieure: l.toleranceInferieure ?? null,
        limiteSpecTexte: l.limiteSpecTexte || null,
        defauthequeId: l.defauthequeId || null,
        instruction: l.instruction || null,
        observations: l.observations || null
      }))
    }));
  };

  // --- ACTIONS ---
  const fetchDictionnaires = async () => {
    try {
      const response = await pfPlanService.getDictionnaires();
      const data = response.data.data;
      
      typesRobinet.value = data.typesRobinet || [];
      typesCaracteristique.value = data.typesCaracteristique || [];
      typesControle.value = data.typesControle || [];
      moyensControle.value = data.moyensControle || [];
      periodicites.value = data.periodicites || [];
      typesSection.value = data.typesSection || data.typesSections || []; 
      instruments.value = data.instruments || []; 
      
      isDicosLoaded.value = true;
    } catch (apiError) {
      console.error("Erreur réseau (Dictionnaires):", apiError);
      throw apiError; 
    }
  };

  const getPlan = async (id) => {
    isLoading.value = true;
    try {
      const response = await pfPlanService.getPlan(id);
      const data = response.data.data;
      
      entete.value = {
        id: data.id,
        typeRobinetCode: data.typeRobinetCode || '',
        designation: data.designation || '',
        commentaireVersion: data.commentaireVersion || '',
        version: data.version,
        statut: data.statut,
        dateApplication: data.dateApplication,
        creePar: data.creePar,
        creeLe: data.creeLe,
        remarques: data.remarques || '',
        legendeMoyens: data.legendeMoyens || '',
      };

      sections.value = data.sections || [];
    } finally {
      isLoading.value = false;
    }
  };

  const createPlan = async () => {
    isLoading.value = true;
    try {
      const payload = {
        typeRobinetCode: entete.value.typeRobinetCode,
        designation: entete.value.designation || '',
        commentaireVersion: entete.value.commentaireVersion || '',
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        sections: mapSectionsForBackend()
      };

      const response = await pfPlanService.creerPlan(payload);
      return response.data.planId;
    } finally {
      isLoading.value = false;
    }
  };

  const archiverPlan = async () => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      await pfPlanService.archiverPlan(entete.value.id);
      entete.value.statut = 'ARCHIVE';
    } finally {
      isLoading.value = false;
    }
  };

  const creerNouvelleVersion = async (motif) => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      const payload = {
        ancienId: entete.value.id,
        typeRobinetCode: entete.value.typeRobinetCode,
        designation: entete.value.designation,
        modifiePar: 'Admin',
        motifModification: motif,
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        Sections: mapSectionsForBackend()
      };
      const response = await pfPlanService.creerNouvelleVersion(entete.value.id, payload);
      return response.data.planId;
    } finally {
      isLoading.value = false;
    }
  };

  const restaurerPlan = async (motif) => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      const payload = {
        planArchiveId: entete.value.id,
        restaurePar: 'Admin',
        motifRestoration: motif
      };
      const response = await pfPlanService.restaurerPlan(payload);
      return response.data.planId;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    typesRobinet, typesCaracteristique, typesControle, moyensControle, 
    periodicites, typesSection, instruments, isDicosLoaded, 
    entete, sections, isLoading, 
    fetchDictionnaires, getPlan, createPlan, archiverPlan, creerNouvelleVersion, restaurerPlan
  };
});
