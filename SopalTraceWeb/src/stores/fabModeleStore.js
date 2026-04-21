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
  // const groupesInstruments = ref([]); 
  const instruments = ref([]); 
  const gammesOperatoires = ref([]); // <-- NOUVEAU
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU MODÈLE ---
  const entete = ref({
    operationCode: '', 
    natureComposantCode: '',
    typeRobinetCode: '',
    libelle: '',
    notes: ''
  });
  
  const sections = ref([]);
  const isLoading = ref(false);
  const version = ref(1); // <-- NOUVEAU : Rend la version dynamique

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
      // groupesInstruments.value = data.groupesInstruments || []; 
      instruments.value = data.instruments || []; 
      
      gammesOperatoires.value = data.gammes || []; // <-- NOUVEAU

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
      typeSectionId: '', // ID Strict obligatoire
      periodiciteId: null, // ID Strict optionnel
      libelleSection: '', // Le texte libre pour l'opérateur !
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
      // groupeInstrumentId: null,
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

  const saveModele = async (legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = {
        code: codeModeleAuto.value,
        libelle: entete.value.libelle || `Modèle ${codeModeleAuto.value}`,
        typeRobinetCode: entete.value.typeRobinetCode || null,
        natureComposantCode: entete.value.natureComposantCode || '',
        operationCode: entete.value.operationCode || '', 
        notes: entete.value.notes || "",
        legendeMoyens: legendeMoyens || '',
        sections: sections.value.map(s => ({
          ordreAffiche: s.ordreAffiche,
          typeSectionId: s.typeSectionId, // Ajouté dans le mapping
          periodiciteId: s.periodiciteId, // Ajouté dans le mapping
          libelleSection: s.libelleSection || 'SECTION SANS NOM',
          frequenceLibelle: s.frequenceLibelle,
          notes: s.notes, // Ajouté dans le mapping
          lignes: s.lignes.map(l => ({
            ordreAffiche: l.ordreAffiche,
            typeCaracteristiqueId: l.typeCaracteristiqueId,
            libelleAffiche: l.libelleAffiche,
            typeControleId: l.typeControleId,
            moyenControleId: l.moyenControleId,
            // groupeInstrumentId: l.groupeInstrumentId,
            instrumentCode: l.instrumentCode, // <-- NOUVEAU
            periodiciteId: l.periodiciteId,
            instruction: l.instruction,
            estCritique: l.estCritique,
            limiteSpecTexte: l.limiteSpecTexte || null
          }))
        }))
      };

      await fabPlanService.creerModele(payload);
    } finally {
      isLoading.value = false;
    }
  };

  return {
    operations, typesRobinet, naturesComposant, 
    typesCaracteristique, typesControle, moyensControle, 
    periodicites, typesSection, instruments, gammesOperatoires, isDicosLoaded, 
    entete, sections, isLoading, version, codeModeleAuto,
    fetchDictionnaires, addSection, removeSection, addLigneLibre, removeLigne, saveModele
  };
});
