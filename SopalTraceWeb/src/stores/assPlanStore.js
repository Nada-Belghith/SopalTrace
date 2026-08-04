import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { documentService as assPlanService } from '@/services/documentService';
import { referentielsService } from '@/services/referentielsService';
import { mapSectionForBackend, mapImportedSection } from '@/utils/sectionUtils';
import { useReferentielStore } from './referentielStore';

export const useAssPlanStore = defineStore('assPlan', () => {
  // --- DICTIONNAIRES ---
  const operations = ref([]);
  const typesRobinet = ref([]);
  const naturesComposant = ref([]);
  const typesCaracteristique = ref([]);
  const typesControle = ref([]);
  const moyensControle = ref([]);
  const periodicites = ref([]);
  const reglesEchantillonnage = ref([]); // Ajouté pour les règles ISO
  const typesSection = ref([]);
  const instruments = ref([]);
  const postes = ref([]);
  const famillesProduit = ref([]);
  const gammesOperatoires = ref([]);
  const formulairesReferences = ref([]);
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU MODÈLE ---
  const entete = ref({
    code: '',
    operationCode: '',
    natureComposantCode: '',
    typeRobinetCode: '',
    libelle: '',
    notes: '',
    legendeMoyens: '',
    posteCode: '',
    familleProduitCode: '',
    versionInitiale: null,
    refFormulaireCodeReference: ''  // Code du formulaire ref sélectionné (ex: FE-ASS-PISTON)
  });

  const sections = ref([]);
  const isLoading = ref(false);
  const isBeingLoaded = ref(false); // ✅ Empêche les watchers de cascade pendant le chargement
  const version = ref(0);

  const codePlanAuto = computed(() => {
    const op = entete.value.operationCode || 'XXX';
    const nat = entete.value.natureComposantCode || 'XXX';
    const fam = entete.value.familleProduitCode ? `-${entete.value.familleProduitCode}` : '';
    const poste = entete.value.posteCode ? `P${entete.value.posteCode}` : '';

    const prefix = (nat === 'PF' || nat === 'PISTON') ? 'PLAN' : 'MOD';
    return `${prefix}-${op}-${nat}${fam}-${poste}`.toUpperCase();
  });

  // --- ACTIONS ---
  const fetchDictionnaires = async () => {
    try {
      const response = await referentielsService.getDictionnairesFabrication();
      const data = response.data.data || response.data;

      operations.value = data.operations || [];
      typesRobinet.value = data.typesRobinet || [];
      naturesComposant.value = data.naturesComposant || [];
      typesCaracteristique.value = data.typesCaracteristique || [];
      typesControle.value = data.typesControle || [];
      moyensControle.value = data.moyensControle || [];
      periodicites.value = data.periodicites || [];
      reglesEchantillonnage.value = data.reglesEchantillonnage || []; // Récupération depuis le backend
      typesSection.value = data.typesSection || data.typesSections || [];
      instruments.value = data.instruments || [];
      postes.value = data.postes || [];
      famillesProduit.value = (data.famillesProduit || []).map(f => ({
        code: f.code,
        libelle: f.designation || f.libelle || '',
        typeRobinetCode: f.typeRobinetCode
      }));
      gammesOperatoires.value = data.gammes || [];

      isDicosLoaded.value = true;
    } catch (apiError) {
      console.error("Erreur réseau (Dictionnaires):", apiError);
      throw apiError;
    }
  };

  const fetchFormulairesReferences = async (role) => {
    try {
      const response = await referentielsService.getFormulairesListByRole(role);
      formulairesReferences.value = response.data?.data || [];
    } catch (e) {
      console.error("Erreur fetch formulaires:", e);
    }
  };

  const addSection = () => {
    sections.value.push({
      id: crypto.randomUUID(),
      ordreAffiche: sections.value.length + 1,
      typeSectionId: '',
      periodiciteId: null,
      regleEchantillonnageId: null, // Ajouté pour la règle d'échantillonnage
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
      observations: '',
      estCritique: false,
      limiteSpecTexte: '',
      valeursColonnesSpecifiques: {}
    });
  };

  const removeLigne = (sectionId, ligneId) => {
    const section = sections.value.find(s => s.id === sectionId);
    if (section) {
      section.lignes = section.lignes.filter(l => l.id !== ligneId);
      section.lignes.forEach((l, idx) => l.ordreAffiche = idx + 1);
    }
  };

  const refStore = useReferentielStore();

  const mapPayload = (legendeMoyens = '') => ({
    id: entete.value.id || undefined,
    typeDocumentCode: 'PLAN_ASS',
    nom: entete.value.code || codePlanAuto.value,
    designation: entete.value.libelle || `Modèle ${codePlanAuto.value} V${version.value}`,
    libre1: entete.value.typeRobinetCode || null,
    natureArticleCode: entete.value.natureComposantCode || '',
    operationCode: entete.value.operationCode || '',
    posteCode: entete.value.posteCode || null,
    familleProduitFiniCode: (entete.value.natureComposantCode?.trim().toUpperCase() === 'PISTON') ? null : (entete.value.familleProduitCode || null),
    remarques: entete.value.notes || "",
    legendeMoyens: legendeMoyens || '',
    versionInitiale: entete.value.versionInitiale,
    colonneDefs: entete.value.configurationColonnes || [],
    refFormulaireCodeReference: entete.value.refFormulaireCodeReference || null,
    sections: sections.value.map((s, sIdx) => mapSectionForBackend(s, sIdx, refStore.periodicites))
  });

  const savePlan = async (legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = mapPayload(legendeMoyens);
      const res = await assPlanService.createDocument(payload);
      return res.data || res;
    } finally {
      isLoading.value = false;
    }
  };

  const createNewVersion = async (id, motif, legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = {
        ...mapPayload(legendeMoyens),
        ancienId: id,
        modifiePar: 'Admin',
        motifModification: motif
      };
      const res = await assPlanService.createNewVersion(payload.ancienId, payload);
      return res.data || res;
    } finally {
      isLoading.value = false;
    }
  };
  const creerNouvelleVersion = createNewVersion;

  const updatePlan = async (id, legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = mapPayload(legendeMoyens);
      // Ensure we send sections and other required fields properly for PUT
      const res = await assPlanService.updateDocument(id, payload);
      return res.data;
    } finally {
      isLoading.value = false;
    }
  };

  const activerPlanDraft = async (id) => {
    isLoading.value = true;
    try {
      return { success: true };
    } finally {
      isLoading.value = false;
    }
  };

  const restaurerPlan = async () => {
    throw new Error("La restauration n'est pas supportée pour les documents centralisés.");
  };

  const importerDepuisExcel = async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    if (entete.value.configurationColonnes) {
      const configJson = typeof entete.value.configurationColonnes === 'string'
        ? entete.value.configurationColonnes
        : JSON.stringify(entete.value.configurationColonnes);
      formData.append('colonneDefsJson', configJson);
      formData.append('configurationColonnesJson', configJson);
    }

    isLoading.value = true;
    try {
      // Pour les modèles, on utilise l'import generic plan (qui a été unifié côté backend)
      const parsedData = await assPlanService.importExcel(formData);

      if (parsedData && parsedData.sections) {
        if (parsedData.remarques && parsedData.remarques.trim() !== '') {
          entete.value.notes = (entete.value.notes ? entete.value.notes + '\n' : '') + parsedData.remarques.trim();
        }

        sections.value = parsedData.sections.map(sec => mapImportedSection(sec, reglesEchantillonnage.value));

        await fetchDictionnaires();
      }
      return parsedData;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    operations, typesRobinet, naturesComposant,
    typesCaracteristique, typesControle, moyensControle,
    periodicites, typesSection, reglesEchantillonnage,
    instruments,
    postes,
    famillesProduit,
    gammesOperatoires,
    formulairesReferences,
    isDicosLoaded,

    // État Modèle
    entete, sections, isLoading, version, codePlanAuto,
    // Actions
    fetchDictionnaires,
    fetchFormulairesReferences,
    addSection,
    removeSection, addLigneLibre, removeLigne, savePlan, createNewVersion, creerNouvelleVersion, updatePlan, activerPlanDraft, restaurerPlan, importerDepuisExcel,
    isBeingLoaded
  };
});
