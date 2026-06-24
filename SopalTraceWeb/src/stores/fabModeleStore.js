import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { modeleFabricationService as fabModeleService } from '@/services/modeleFabricationService';
import { referentielsService } from '@/services/referentielsService';
import { resolveSectionDisplayTitle } from '@/utils/sectionTitleUtils';

export const useFabModeleStore = defineStore('fabModele', () => {
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
    refFormulaireCodeReference: '',  // Code du formulaire ref sélectionné (ex: PRC)
    configurationColonnes: []
  });

  const getFormulaireConfigJson = (ref) =>
    ref?.configurationStructureJson ?? ref?.ConfigurationStructureJson ?? null;

  const parseConfigurationColonnes = (configJson) => {
    if (!configJson) return [];
    try {
      return typeof configJson === 'string' ? JSON.parse(configJson) : configJson;
    } catch {
      return [];
    }
  };

  const findFormulaireActif = (codeReference) => {
    if (!codeReference) return null;
    const refs = formulairesReferences.value || [];
    return refs
      .filter(r => (r.codeReference || '').trim() === codeReference.trim())
      .sort((a, b) => {
        const statutA = String(a.statut || a.Statut || '').trim().toUpperCase() === 'ACTIF' ? 0 : 1;
        const statutB = String(b.statut || b.Statut || '').trim().toUpperCase() === 'ACTIF' ? 0 : 1;
        if (statutA !== statutB) return statutA - statutB;
        return (b.version ?? b.Version ?? 0) - (a.version ?? a.Version ?? 0);
      })[0] || null;
  };

  const applyFormulaireConfiguration = (codeReference = null, force = false) => {
    if (!force && entete.value.configurationColonnes && entete.value.configurationColonnes.length > 0) return;
    const refs = formulairesReferences.value || [];
    if (!refs.length) return;

    const codeRef = (codeReference || entete.value.refFormulaireCodeReference || refs[0]?.codeReference || '').trim();
    const refObj = findFormulaireActif(codeRef) || refs[0];
    if (!refObj) return;

    entete.value.refFormulaireCodeReference = refObj.codeReference || '';
    entete.value.configurationColonnes = parseConfigurationColonnes(getFormulaireConfigJson(refObj));
  };

  const syncConfigurationFromFormulaire = () => {
    applyFormulaireConfiguration(null, true);
  };

  /** Colonnes PRC/PRNC : Retourner les colonnes de l'entete si elles existent, sinon celles du formulaire actif */
  const effectiveConfigurationColonnes = computed(() => {
    if (entete.value.configurationColonnes !== undefined && entete.value.configurationColonnes !== null) {
      return entete.value.configurationColonnes;
    }

    const codeRef = (entete.value.refFormulaireCodeReference || '').trim();
    if (codeRef) {
      const refObj = findFormulaireActif(codeRef);
      const cols = parseConfigurationColonnes(getFormulaireConfigJson(refObj));
      if (cols.length > 0) return cols;
    }

    const refs = formulairesReferences.value || [];
    if (refs.length > 0) {
      const latest = [...refs].sort((a, b) => {
        const statutA = String(a.statut || a.Statut || '').trim().toUpperCase() === 'ACTIF' ? 0 : 1;
        const statutB = String(b.statut || b.Statut || '').trim().toUpperCase() === 'ACTIF' ? 0 : 1;
        if (statutA !== statutB) return statutA - statutB;
        return (b.version ?? b.Version ?? 0) - (a.version ?? a.Version ?? 0);
      })[0];
      const cols = parseConfigurationColonnes(getFormulaireConfigJson(latest));
      if (cols.length > 0) return cols;
    }

    return [];
  });

  const baseTableColumns = [
    { key: 'caracteristique', label: 'Caractéristique contrôlée', width: 'w-[22%]' },
    { key: 'limite_spec', label: 'Limite spécif.', width: 'w-[12%]', textAlign: 'center' },
    { key: 'type_controle', label: 'Type de contrôle', width: 'w-[15%]', textAlign: 'center' },
    { key: 'moyen_controle', label: 'Moyen de contrôle', width: 'w-[15%]', textAlign: 'center' },
    { key: 'code_instrument', label: 'Code instrument', width: 'w-[15%]', textAlign: 'center' },
    { key: 'observations', label: 'Observations', width: 'flex-1' }
  ];

  const tableColumns = computed(() => {
    let cols = [...baseTableColumns];
    (effectiveConfigurationColonnes.value || []).forEach((cc) => {
      const insertIdx = cols.findIndex((c) => c.key === cc.insertAfter);
      const newCol = {
        key: cc.key,
        label: cc.label,
        type: cc.type,
        width: 'w-[12%]',
        textAlign: 'center',
        isCustom: true
      };
      if (insertIdx !== -1) cols.splice(insertIdx + 1, 0, newCol);
      else cols.push(newCol);
    });
    cols.push({ key: 'actions', label: '', width: 'w-12', textAlign: 'center' });
    return cols;
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

    const prefix = (nat === 'PF') ? 'PLAN' : 'MOD';
    return `${prefix}-${op}-${nat}${fam}-${poste}`.toUpperCase();
  });

  // --- ACTIONS ---
  const fetchDictionnaires = async () => {
    try {
      const response = await referentielsService.getDictionnaires();
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
      const response = await referentielsService.getFormulairesListByRole(role);
      formulairesReferences.value = response.data?.data || [];
      applyFormulaireConfiguration();
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

  const syncSectionLibellesFromTypes = () => {
    sections.value.forEach((section) => {
      if (!section.typeSectionId) return;
      section.libelleSection = resolveSectionDisplayTitle(section, typesSection.value);
    });
  };

  const mapPayload = (legendeMoyens = '') => {
    syncSectionLibellesFromTypes();

    const configCols = effectiveConfigurationColonnes.value || [];
    const codeRef = entete.value.refFormulaireCodeReference || null;

    return {
      nom: codeModeleAuto.value || entete.value.code,
      designation: entete.value.libelle || `Modèle ${codeModeleAuto.value} V${version.value}`,
      libre1: entete.value.typeRobinetCode || null,
      natureArticleCode: entete.value.natureComposantCode || '',
      operationCode: entete.value.operationCode || '',
      posteCode: entete.value.posteCode || null,
      familleProduitFiniCode: entete.value.familleProduitCode || null,
      remarques: entete.value.notes || "",
      legendeMoyens: legendeMoyens || '',
      versionInitiale: entete.value.versionInitiale,
      libre2: null,
      colonneDefs: typeof configCols === 'string' ? JSON.parse(configCols) : configCols,
      refFormulaireCodeReference: codeRef,
      sections: sections.value.map((s, idx) => ({
        ordreAffiche: idx + 1,
        typeSectionId: (s.typeSectionId && s.typeSectionId !== '') ? s.typeSectionId : null,
        periodiciteId: s.periodiciteId || null,
        regleEchantillonnageId: s.regleEchantillonnageId || null,
        libelleSection: s.libelleSection || resolveSectionDisplayTitle(s, typesSection.value) || 'SECTION SANS NOM',
        frequenceLibelle: s.frequenceLibelle || '',
        notes: s.notes || '',
        lignes: (s.lignes || []).map((l, lIdx) => ({
          ordreAffiche: lIdx + 1,
          typeCaracteristiqueId: l.typeCaracteristiqueId || null,
          libelleAffiche: l.libelleAffiche || '',
          typeControleId: l.typeControleId || null,
          moyenControleId: l.moyenControleId || null,
          instrumentCode: l.instrumentCode || '',
          periodiciteId: l.periodiciteId || null,
          instruction: l.instruction || '',
          observations: l.observations || '',
          estCritique: l.estCritique || false,
          unite: l.unite || '',
          limiteSpecTexte: l.limiteSpecTexte || null,
          extraColonnes: Object.entries(l.valeursColonnesSpecifiques || {}).map(([k, v], idx) => ({
            cleColonne: k,
            valeurColonne: v ? String(v) : null,
            ordreAffiche: idx + 1
          }))
        }))
      }))
    };
  };

  const saveModele = async (legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = mapPayload(legendeMoyens);
      const res = await fabModeleService.createModele(payload);
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
      const res = await fabModeleService.newModeleVersion(payload);
      return res.data;
    } finally {
      isLoading.value = false;
    }
  };

  const updateModele = async (id, legendeMoyens = '') => {
    isLoading.value = true;
    try {
      const payload = mapPayload(legendeMoyens);
      // Ensure we send sections and other required fields properly for PUT
      const res = await fabModeleService.updateModeleValeurs(id, payload);
      return res.data;
    } finally {
      isLoading.value = false;
    }
  };

  const activerModeleDraft = async (id) => {
    isLoading.value = true;
    try {
      const res = await fabModeleService.activerModele(id);
      return res.data;
    } finally {
      isLoading.value = false;
    }
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
      const parsedData = await fabModeleService.importExcel(formData);
      
      if (parsedData && parsedData.sections) {
        if (parsedData.remarques && parsedData.remarques.trim() !== '') {
          entete.value.notes = (entete.value.notes ? entete.value.notes + '\n' : '') + parsedData.remarques.trim();
        }
        
        sections.value = parsedData.sections.map(sec => ({
          id: sec.id || crypto.randomUUID(),
          isFromDb: false,
          nom: sec.nom || '',
          libelleSection: sec.nom,
          typeSectionId: sec.typeSectionId,
          modeFreq: sec.modeFreq,
          periodiciteId: sec.periodiciteId,
          freqNum: sec.freqNum,
          typeVariable: sec.typeVariable,
          freqHours: sec.freqHours,
          lignes: sec.lignes.map(lig => ({
            id: lig.id || crypto.randomUUID(),
            isFromDb: false,
            typeCaracteristiqueId: lig.typeCaracteristiqueId,
            typeControleId: lig.typeControleId,
            moyenControleId: lig.moyenControleId,
            instrumentCode: lig.instrumentCode,
            valeurNominale: lig.valeurNominale,
            toleranceSuperieure: lig.toleranceSuperieure,
            toleranceInferieure: lig.toleranceInferieure,
            unite: lig.unite || '',
            limiteSpecTexte: lig.limiteSpecTexte,
            observations: lig.observations,
            instruction: lig.instruction,
            estCritique: lig.estCritique,
            libelleAffiche: lig.libelleAffiche,
            imageBase64: lig.imageBase64 || null
          }))
        }));

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
    entete, sections, isLoading, version, codeModeleAuto, effectiveConfigurationColonnes, tableColumns,
    // Actions
    fetchDictionnaires,
    fetchFormulairesReferences,
    applyFormulaireConfiguration,
    syncConfigurationFromFormulaire,
    addSection,
    removeSection, addLigneLibre, removeLigne, saveModele, creerNouvelleVersion, updateModele, activerModeleDraft, importerDepuisExcel,
    isBeingLoaded
  };
});
