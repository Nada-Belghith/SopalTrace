import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { assPlanService } from '@/services/assPlanService';
import { qualityPlansService } from '@/services/qualityPlansService';

export const useAssModeleStore = defineStore('assModele', () => {
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

  const codeModeleAuto = computed(() => {
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
      const response = await assPlanService.getDictionnaires();
      const data = response.data.data;

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
      const response = await qualityPlansService.getFormulairesListByRole(role);
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

  const mapPayload = (legendeMoyens = '') => ({
    code: entete.value.code || codeModeleAuto.value,
    libelle: entete.value.libelle || `Modèle ${codeModeleAuto.value} V${version.value}`,
    typeRobinetCode: entete.value.typeRobinetCode || null,
    natureComposantCode: entete.value.natureComposantCode || '',
    operationCode: entete.value.operationCode || '',
    posteCode: entete.value.posteCode || null,
    familleProduitCode: (entete.value.natureComposantCode?.trim().toUpperCase() === 'PISTON') ? null : (entete.value.familleProduitCode || null),
    notes: entete.value.notes || "",
    legendeMoyens: legendeMoyens || '',
    versionInitiale: entete.value.versionInitiale,
    configurationColonnesJson: typeof entete.value.configurationColonnes === 'string' ? entete.value.configurationColonnes : JSON.stringify(entete.value.configurationColonnes || []),
    refFormulaireCodeReference: entete.value.refFormulaireCodeReference || null,
    sections: sections.value.map(s => ({
      ordreAffiche: s.ordreAffiche,
      typeSectionId: s.typeSectionId,
      periodiciteId: s.periodiciteId,
      regleEchantillonnageId: s.regleEchantillonnageId, // Ajouté ici
      libelleSection: s.libelleSection || 'SECTION SANS NOM',
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
        observations: l.observations,
        estCritique: l.estCritique,
        unite: l.unite || '',
        limiteSpecTexte: l.limiteSpecTexte || null,
        colonnesSupplementaires: l.valeursColonnesSpecifiques && Object.keys(l.valeursColonnesSpecifiques).length > 0 
          ? JSON.stringify(l.valeursColonnesSpecifiques) 
          : null
      }))
    }))
  });

  const saveModele = async (legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = mapPayload(legendeMoyens);
      const res = await assPlanService.creerModele(payload);
      return res.data; // Return the whole data which includes modeleId and version
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
      const res = await assPlanService.nouvelleVersionModele(payload);
      return res.data; // Return full data with modeleId and version
    } finally {
      isLoading.value = false;
    }
  };

  const updateModele = async (id, legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = mapPayload(legendeMoyens);
      // Ensure we send sections and other required fields properly for PUT
      const res = await assPlanService.updateModeleValeurs(id, payload);
      return res.data;
    } finally {
      isLoading.value = false;
    }
  };

  const activerModeleDraft = async (id) => {
    isLoading.value = true;
    try {
      const res = await assPlanService.activerModele(id);
      return res.data;
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
    entete, sections, isLoading, version, codeModeleAuto,    
    // Actions
    fetchDictionnaires,
    fetchFormulairesReferences,
    addSection,
    removeSection, addLigneLibre, removeLigne, saveModele, creerNouvelleVersion, updateModele, activerModeleDraft,
    isBeingLoaded
  };
});
