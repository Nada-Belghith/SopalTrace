import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { pfPlanService } from '@/services/pfPlanService';
import { qualityPlansService } from '@/services/qualityPlansService';
import { parseFrequenceLibelle } from '@/utils/frequencyUtils';

export const usePfPlanStore = defineStore('pfPlan', () => {
  // --- DICTIONNAIRES ---
  const typesRobinet = ref([]);
  const famillesProduit = ref([]);
  const typesCaracteristique = ref([]);
  const typesControle = ref([]);
  const moyensControle = ref([]);
  const periodicites = ref([]);
  const typesSection = ref([]); 
  const instruments = ref([]); 
  const postes = ref([]);
  const reglesEchantillonnage = ref([]);
  const formulairesReferences = ref([]);
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU PLAN ---
  const entete = ref({
    id: null,
    familleProduitFiniCode: '',
    familleProduitFiniLibelle: '',
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
      regleEchantillonnageId: s.regleEchantillonnageId || null,
      regleEchantillonnageLibelle: s.regleEchantillonnageLibelle || (s.modeFreq === 'VARIABLE' ? s.libelleSection?.split('(').pop()?.replace(')', '') : null),
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
      famillesProduit.value = data.famillesProduit || [];
      typesCaracteristique.value = data.typesCaracteristique || [];
      typesControle.value = data.typesControle || [];
      moyensControle.value = data.moyensControle || [];
      periodicites.value = data.periodicites || [];
      typesSection.value = data.typesSection || data.typesSections || []; 
      instruments.value = data.instruments || []; 
      postes.value = data.postes || [];
      reglesEchantillonnage.value = data.reglesEchantillonnage || [];
      
      isDicosLoaded.value = true;
    } catch (apiError) {
      console.error("Erreur réseau (Dictionnaires):", apiError);
      throw apiError; 
    }
  };

  const fetchFormulairesReferences = async (role) => {
    try {
      const response = await qualityPlansService.getFormulairesListByRole(role);
      formulairesReferences.value = response.data?.data || [];
    } catch (e) {
      console.error("Erreur fetch formulaires:", e);
    }
  };

  const getPlan = async (id) => {
    isLoading.value = true;
    try {
      const response = await pfPlanService.getPlan(id);
      const data = response.data.data;
      
      entete.value = {
        id: data.id,
        familleProduitFiniCode: data.familleProduitFiniCode || '',
        familleProduitFiniLibelle: data.familleProduitFiniLibelle || '',
        version: data.version,
        statut: data.statut,
        dateApplication: data.dateApplication,
        creePar: data.creePar,
        creeLe: data.creeLe,
        remarques: data.remarques || '',
        legendeMoyens: data.legendeMoyens || '',
      };

      sections.value = (data.sections || []).map(s => {
        const hydrated = { ...s };
        
        if (s.regleEchantillonnageId) {
          const regle = reglesEchantillonnage.value.find(r => r.id === s.regleEchantillonnageId);
          const libelle = regle ? regle.libelle : (s.regleEchantillonnageLibelle || '');
          
          // Détection du mode
          const parsingResult = parseFrequenceLibelle(libelle, periodicites.value);
          Object.assign(hydrated, parsingResult);
          
          // FIX: Si on a un ID de règle d'échantillonnage, on FORCE le mode FIXE
          // même si le libellé contient des mots clés comme "pièce" (ex: "1 pièce / heure")
          hydrated.modeFreq = 'FIXE';
        } else if (s.libelleSection && s.libelleSection.includes('(')) {
           hydrated.modeFreq = 'VARIABLE';
        } else {
          hydrated.modeFreq = 'SANS';
        }
        
        return hydrated;
      });
    } finally {
      isLoading.value = false;
    }
  };

  const createPlan = async () => {
    isLoading.value = true;
    try {
      const payload = {
        familleProduitFiniCode: entete.value.familleProduitFiniCode,
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
        familleProduitFiniCode: entete.value.familleProduitFiniCode,
        modifiePar: 'Admin',
        motifModification: motif,
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        sections: mapSectionsForBackend()
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
    typesRobinet, famillesProduit, typesCaracteristique, typesControle, moyensControle, 
    periodicites, typesSection, instruments, postes, isDicosLoaded, 
    entete, sections, isLoading, reglesEchantillonnage, formulairesReferences,
    fetchDictionnaires, fetchFormulairesReferences, getPlan, createPlan, archiverPlan, creerNouvelleVersion, restaurerPlan
  };
});
