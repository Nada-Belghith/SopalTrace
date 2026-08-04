import { defineStore } from 'pinia';
import { ref } from 'vue';
import { referentielsService } from '@/services/referentielsService';

export const useReferentielStore = defineStore('referentiel', () => {
  // --- DICTIONNAIRES PARTAGÉS ---
  const operations = ref([]);
  const typesRobinet = ref([]);
  const naturesComposant = ref([]);
  const typesCaracteristique = ref([]);
  const typesControle = ref([]);
  const moyensControle = ref([]);
  const periodicites = ref([]);
  const reglesEchantillonnage = ref([]);
  const typesSection = ref([]);
  const instruments = ref([]);
  const postes = ref([]); // Postes (génériques)
  const famillesProduit = ref([]);
  const gammesOperatoires = ref([]);

  // Spécifiques Vérif Machine
  const machines = ref([]);
  const typesDocument = ref([]);
  const constructeurs = ref([]);
  const periodicitesMachine = ref([]);
  const famillesCorps = ref([]);
  const moyensDetection = ref([]);
  const piecesReference = ref([]);
  const fuitesEtalon = ref([]);

  // Spécifiques Contrôle au Poste
  const postesTravail = ref([]);
  const risquesDefauts = ref([]);

  // Formulaires (PRC, PRNC, FEC, FENC, etc.) — stockées par rôle pour éviter les écrasements
  const formulairesReferences = ref([]);
  const formulairesReferencesByRole = ref({});

  // Échantillonnage
  const nqaList = ref([]);

  // Status de chargement
  const isDicosFabricationLoaded = ref(false);
  const isDicosVerifMachineLoaded = ref(false);
  const isDicosControlePosteLoaded = ref(false);

  // --- ACTIONS ---

  const mergeDict = (refVar, newData) => {
    if (!newData || !Array.isArray(newData)) return;
    const existing = [...refVar.value];
    for (const item of newData) {
      const key = item.id || item.Id || item.code || item.Code;
      if (!existing.find(e => (e.id || e.Id || e.code || e.Code) === key)) {
        existing.push(item);
      }
    }
    refVar.value = existing;
  };

  /**
   * Charge les dictionnaires selon le contexte/type
   * @param {string} type - 'fabrication', 'verif-machine' ou 'controle-poste'
   */
  const fetchDictionnaires = async (type = 'fabrication', force = false) => {
    try {
      if (type === 'fabrication') {
        if (!force && isDicosFabricationLoaded.value) return;
        const res = await referentielsService.getDictionnairesFabrication();
        const data = res.data?.data || {};

        mergeDict(operations, data.operations);
        mergeDict(typesRobinet, data.typesRobinet);
        mergeDict(naturesComposant, data.naturesComposant);
        mergeDict(typesCaracteristique, data.typesCaracteristique);
        mergeDict(typesControle, data.typesControle);
        mergeDict(moyensControle, data.moyensControle);
        mergeDict(periodicites, data.periodicites);
        mergeDict(reglesEchantillonnage, data.reglesEchantillonnage);
        mergeDict(typesSection, data.typesSection || data.typesSections);
        mergeDict(instruments, data.instruments);
        mergeDict(postes, data.postes);

        const fpMapped = (data.famillesProduit || []).map(f => ({
          code: f.code,
          libelle: f.designation || f.libelle || '',
          typeRobinetCode: f.typeRobinetCode
        }));
        mergeDict(famillesProduit, fpMapped);

        // Fix: gammes n'a pas de 'id' ou 'code', mergeDict supprime donc tout sauf le 1er élément.
        // On remplace le tableau complet à la place.
        gammesOperatoires.value = data.gammes || [];
        const nqaMapped = (data.nqa || []).map(n => ({
          ...n,
          valeurNqa: Number(n.code || n.libelle)
        }));
        mergeDict(nqaList, nqaMapped);

        isDicosFabricationLoaded.value = true;
      }
      else if (type === 'verif-machine') {
        if (!force && isDicosVerifMachineLoaded.value) return;
        const res = await referentielsService.getDictionnairesVerifMachine();
        const data = res.data?.data || {};

        mergeDict(machines, data.machines);
        mergeDict(operations, data.operations);
        mergeDict(naturesComposant, data.naturesComposant);
        mergeDict(periodicites, data.periodicites);
        mergeDict(typesDocument, data.typesDocument);
        mergeDict(constructeurs, data.constructeurs);
        mergeDict(periodicitesMachine, data.periodicitesMachine);
        mergeDict(famillesCorps, data.famillesCorps);
        mergeDict(moyensDetection, data.moyensDetection);
        mergeDict(piecesReference, data.piecesReferences);
        mergeDict(fuitesEtalon, data.fuitesEtalon);
        mergeDict(typesCaracteristique, data.typesCaracteristique);
        mergeDict(instruments, data.instruments);
        mergeDict(gammesOperatoires, data.gammes);

        isDicosVerifMachineLoaded.value = true;
      }
      else if (type === 'controle-poste') {
        if (!force && isDicosControlePosteLoaded.value) return;
        const res = await referentielsService.getDictionnairesControlePoste();
        const data = res.data?.data || {};

        mergeDict(postesTravail, data.postes || data.postesTravail);
        mergeDict(risquesDefauts, data.risquesDefauts);
        mergeDict(periodicites, data.periodicites);
        mergeDict(instruments, data.instruments);
        mergeDict(typesCaracteristique, data.typesCaracteristique);
        mergeDict(typesControle, data.typesControle);
        mergeDict(moyensControle, data.moyensControle);

        isDicosControlePosteLoaded.value = true;
      }
    } catch (error) {
      console.error(`Erreur réseau (Dictionnaires ${type}):`, error);
      throw error;
    }
  };

  /**
   * Charge la liste des formulaires PRC/PRNC/FEC/FENC pour un rôle donné
   * @param {string} role - Ex: 'EN_COURS_DE_FABRICATION', 'RESULTAT_CONTROLE_CF', etc.
   */
  const fetchFormulairesReferences = async (role) => {
    try {
      const response = await referentielsService.getFormulairesListByRole(role);
      const data = response.data?.data || [];
      // Muter uniquement la clé du rôle (évite d'invalider toutes les computed qui lisent d'autres rôles)
      formulairesReferencesByRole.value[role] = data;
      // Mise à jour du ref partagé pour la compatibilité ascendante
      formulairesReferences.value = data;
    } catch (apiError) {
      console.error("Erreur réseau (FormulairesReferences):", apiError);
      throw apiError;
    }
  };

  return {
    // État
    operations,
    typesRobinet,
    naturesComposant,
    typesCaracteristique,
    typesControle,
    moyensControle,
    periodicites,
    reglesEchantillonnage,
    typesSection,
    instruments,
    postes,
    famillesProduit,
    gammesOperatoires,
    machines,
    typesDocument,
    constructeurs,
    periodicitesMachine,
    famillesCorps,
    moyensDetection,
    piecesReference,
    fuitesEtalon,
    postesTravail,
    risquesDefauts,
    formulairesReferences,
    formulairesReferencesByRole,
    nqaList,

    // Flags
    isDicosFabricationLoaded,
    isDicosVerifMachineLoaded,
    isDicosControlePosteLoaded,

    // Actions
    fetchDictionnaires,
    fetchFormulairesReferences
  };
});
