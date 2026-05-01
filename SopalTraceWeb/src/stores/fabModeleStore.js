import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { fabPlanService } from '@/services/fabPlanService';

export const useFabModeleStore = defineStore('fabModele', () => {
  // --- DICTIONNAIRES ---
  const operations = ref([]);
  const typesRobinet = ref([]);
  const naturesComposant = ref([]);
  const typesCaracteristique = ref([]);
  const typesControle = ref([]);
  const moyensControle = ref([]);
  const periodicites = ref([]);
  const typesSection = ref([]); 
  const instruments = ref([]); 
  const gammesOperatoires = ref([]); 
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU MODÈLE ---
  const entete = ref({
    operationCode: '', 
    natureComposantCode: '',
    typeRobinetCode: '',
    libelle: '',
    notes: '',
    legendeMoyens: ''
  });
  
  const sections = ref([]);
  const isLoading = ref(false);
  const version = ref(1); 

  const codeModeleAuto = computed(() => {
    const op = entete.value.operationCode || 'XXX';
    const nat = entete.value.natureComposantCode || 'XXX';
    const typ = entete.value.typeRobinetCode || 'ALL';
    return `MOD-${op}-${nat}-${typ}-V${version.value}`.toUpperCase();
  });

  // --- ACTIONS ---
  const fetchDictionnaires = async () => {
    try {
      const response = await fabPlanService.getDictionnaires();
      const data = response.data.data;
      
      operations.value = data.operations || [];
      typesRobinet.value = data.typesRobinet || [];
      naturesComposant.value = data.naturesComposant || [];
      typesCaracteristique.value = data.typesCaracteristique || [];
      typesControle.value = data.typesControle || [];
      moyensControle.value = data.moyensControle || [];
      periodicites.value = data.periodicites || [];
      typesSection.value = data.typesSection || data.typesSections || []; 
      instruments.value = data.instruments || []; 
      gammesOperatoires.value = data.gammes || []; 

      isDicosLoaded.value = true;
    } catch (apiError) {
      console.error("Erreur réseau (Dictionnaires):", apiError);
      throw apiError; 
    }
  };

  const addSection = () => {
    sections.value.push({
      id: crypto.randomUUID(),
      ordreAffiche: sections.value.length + 1,
      typeSectionId: '', 
      periodiciteId: null, 
      libelleSection: '', 
      frequenceLibelle: '',
      notes: '',
      lignes: []
    });
  };

  const removeSection = (sectionId) => {
    sections.value = sections.value.filter(s => s.id !== sectionId);
    sections.value.forEach((s, idx) => s.ordreAffiche = idx + 1);
  };

  const addLigneLibre = (sectionId) => {
    const section = sections.value.find(s => s.id === sectionId);
    if (!section) return;

    section.lignes.push({
      id: crypto.randomUUID(),
      ordreAffiche: section.lignes.length + 1,
      typeCaracteristiqueId: typesCaracteristique.value.length > 0 ? typesCaracteristique.value[0].id : '',
      typeControleId: null, 
      libelleAffiche: '',
      moyenControleId: null,
      instrumentCode: null, 
      periodiciteId: null,
      instruction: '',
      estCritique: false,
      limiteSpecTexte: ''
    });
  };

  const removeLigne = (sectionId, ligneId) => {
    const section = sections.value.find(s => s.id === sectionId);
    if (section) {
      section.lignes = section.lignes.filter(l => l.id !== ligneId);
      section.lignes.forEach((l, idx) => l.ordreAffiche = idx + 1);
    }
  };

  const mapPayload = (legendeMoyens = '') => ({
    code: codeModeleAuto.value,
    libelle: entete.value.libelle || `Modèle ${codeModeleAuto.value}`,
    typeRobinetCode: entete.value.typeRobinetCode || null,
    natureComposantCode: entete.value.natureComposantCode || '',
    operationCode: entete.value.operationCode || '', 
    notes: entete.value.notes || "",
    legendeMoyens: legendeMoyens || '',
    sections: sections.value.map(s => ({
      ordreAffiche: s.ordreAffiche,
      typeSectionId: s.typeSectionId,
      periodiciteId: s.periodiciteId,
      libelleSection: s.libelleSection ,
      frequenceLibelle: s.frequenceLibelle,
      notes: s.notes,
      lignes: s.lignes.map(l => ({
        ordreAffiche: l.ordreAffiche,
        typeCaracteristiqueId: l.typeCaracteristiqueId,
        libelleAffiche: l.libelleAffiche,
        typeControleId: l.typeControleId,
        moyenControleId: l.moyenControleId,
        instrumentCode: l.instrumentCode,
        periodiciteId: l.periodiciteId,
        instruction: l.instruction,
        estCritique: l.estCritique,
        valeurNominale: l.valeurNominale ?? null,
        toleranceSuperieure: l.toleranceSuperieure ?? null,
        toleranceInferieure: l.toleranceInferieure ?? null,
        unite: l.unite || '',
        limiteSpecTexte: l.limiteSpecTexte || null
      }))
    }))
  });

  const saveModele = async (legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = mapPayload(legendeMoyens);
      const res = await fabPlanService.creerModele(payload);
      return res.data.modeleId;
    } finally {
      isLoading.value = false;
    }
  };

  const creerNouvelleVersion = async (id, motif, legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = {
        ...mapPayload(legendeMoyens),
        ancienId: id,
        modifiePar: 'Admin',
        motifModification: motif
      };
      const res = await fabPlanService.creerNouvelleVersionModele(payload);
      return res.data.modeleId;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    operations, typesRobinet, naturesComposant, 
    typesCaracteristique, typesControle, moyensControle, 
    periodicites, typesSection, instruments, gammesOperatoires, isDicosLoaded, 
    entete, sections, isLoading, version, codeModeleAuto,
    fetchDictionnaires, addSection, removeSection, addLigneLibre, removeLigne, saveModele, creerNouvelleVersion
  };
});
