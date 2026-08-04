import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { documentService as documentService } from '@/services/documentService';
import { referentielsService } from '@/services/referentielsService';
import { parseFrequenceLibelle } from '@/utils/frequencyUtils';
import { mapSectionForBackend, hydrateSectionFromBackend, mapImportedSection } from '@/utils/sectionUtils';

export const usedocumentProduitFiniStore = defineStore('documentProduitFini', () => {
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
    refFormulaireCodeReference: null,
    versionInitiale: null,
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
  // (Logique déplacée vers sectionUtils.js)

  // --- ACTIONS ---
  const fetchDictionnaires = async () => {
    try {
      const response = await referentielsService.getDictionnairesFabrication();
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
      const response = await referentielsService.getFormulairesListByRole(role);
      formulairesReferences.value = response.data?.data || [];
    } catch (e) {
      console.error("Erreur fetch formulaires:", e);
    }
  };

  const getPlan = async (id) => {
    isLoading.value = true;
    try {
      const response = await documentService.getById(id);
      const data = response.data?.data || response.data || response;

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
        configurationColonnes: (data.colonneDefs || []).map(c => ({
          key: c.cleColonne || c.key,
          label: c.labelAffiche || c.label,
          type: c.typeValeur || c.type || 'Texte',
          insertAfter: c.insertAfter || 'code_instrument'
        })),
      };

      sections.value = (data.sections || [])
        .sort((a, b) => (a.ordreAffiche ?? 9999) - (b.ordreAffiche ?? 9999))
        .map(s => hydrateSectionFromBackend(s, periodicites.value, reglesEchantillonnage.value));
    } finally {
      isLoading.value = false;
    }
  };

  const createPlan = async () => {
    isLoading.value = true;
    try {
      const payload = {
        nom: `PLAN_PF_${entete.value.familleProduitFiniCode}`,
        designation: entete.value.familleProduitFiniLibelle,
        typeDocumentCode: 'PLAN_PF',
        familleProduitFiniCode: entete.value.familleProduitFiniCode,
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        versionInitiale: entete.value.versionInitiale,
        refFormulaireCodeReference: entete.value.refFormulaireCodeReference,
        colonneDefs: entete.value.configurationColonnes || [],
        sections: (sections.value || []).map((s, idx) => mapSectionForBackend(s, idx, periodicites.value))
      };

      const response = await documentService.createDocument(payload);
      return response.data?.planId || response.planId || response;
    } finally {
      isLoading.value = false;
    }
  };

  const archiverPlan = async () => {
    if (!entete.value.id) return;
    entete.value.statut = 'ARCHIVE';
  };

  const updatePlan = async () => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      const payload = {
        id: entete.value.id,
        nom: `PLAN_PF_${entete.value.familleProduitFiniCode}`,
        designation: entete.value.familleProduitFiniLibelle,
        typeDocumentCode: 'PLAN_PF',
        familleProduitFiniCode: entete.value.familleProduitFiniCode,
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        versionInitiale: entete.value.versionInitiale,
        refFormulaireCodeReference: entete.value.refFormulaireCodeReference,
        configurationColonnesJson: JSON.stringify(entete.value.configurationColonnes || []),
        sections: (sections.value || []).map((s, idx) => mapSectionForBackend(s, idx, periodicites.value))
      };
      const response = await documentService.updateDocument(entete.value.id, payload);
      return response.data || response;
    } finally {
      isLoading.value = false;
    }
  };

  const createNewVersion = async (motif) => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      const payload = {
        ancienId: entete.value.id,
        nom: `PLAN_PF_${entete.value.familleProduitFiniCode}`,
        designation: entete.value.familleProduitFiniLibelle,
        typeDocumentCode: 'PLAN_PF',
        familleProduitFiniCode: entete.value.familleProduitFiniCode,
        modifiePar: 'Admin',
        motifModification: motif,
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        versionInitiale: entete.value.versionInitiale,
        refFormulaireCodeReference: entete.value.refFormulaireCodeReference,
        colonneDefs: entete.value.configurationColonnes || [],
        sections: (sections.value || []).map((s, idx) => mapSectionForBackend(s, idx, periodicites.value))
      };
      const response = await documentService.createNewVersion(payload.ancienId, payload);
      return response.data?.planId || response.planId || response;
    } finally {
      isLoading.value = false;
    }
  };
  const creerNouvelleVersion = createNewVersion;

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
      const parsedData = await documentService.importExcel(formData);

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
    typesRobinet, famillesProduit, typesCaracteristique, typesControle, moyensControle,
    periodicites, typesSection, instruments, postes, isDicosLoaded,
    entete, sections, isLoading, reglesEchantillonnage, formulairesReferences,
    fetchDictionnaires, fetchFormulairesReferences, getPlan, createPlan, updatePlan, archiverPlan, createNewVersion, creerNouvelleVersion, restaurerPlan, importerDepuisExcel
  };
});
