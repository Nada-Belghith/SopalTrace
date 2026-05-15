import { defineStore } from 'pinia';
import { ref } from 'vue';
import { planNcService } from '@/services/planNcService';
import { genererUid } from '@/utils/uuidUtils';

export const usePlanNcStore = defineStore('planNc', () => {
  // --- DICTIONNAIRES ---
  const postes = ref([]);
  const machines = ref([]);
  const risquesDefauts = ref([]);
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU PLAN ---
  const entete = ref({
    id: null,
    posteCode: '',
    nom: '',
    version: 1,
    statut: 'ACTIF',
    remarques: '',
    legendeMoyens: '',
  });

  const lignes = ref([]);
  const isLoading = ref(false);
  const planInitialise = ref(false);
  const plansExistants = ref([]);
  const snapshotOriginal = ref(null);

  const prendreSnapshot = () => {
    snapshotOriginal.value = JSON.stringify(buildPayload());
  };

  const aDesModifications = () => {
    if (!entete.value.id) return true;
    if (!snapshotOriginal.value) return true;
    return snapshotOriginal.value !== JSON.stringify(buildPayload());
  };

  const resoudreLibelleDefaut = (ligne) => {
    let finalLibelle = ligne._libelleDefautBrut || '';
    if (!finalLibelle && ligne.risqueDefautId) {
      const matched = risquesDefauts.value.find(r => r.id === ligne.risqueDefautId);
      finalLibelle = matched?.libelle || matched?.Libelle || 'Défaut sélectionné';
    }
    return finalLibelle;
  };

  const buildPayload = () => {
    return {
      PosteCode: entete.value.posteCode,
      Nom: entete.value.nom,
      Remarques: entete.value.remarques || '',
      LegendeMoyens: entete.value.legendeMoyens || '',
      Lignes: lignes.value.map((l, idx) => ({
        Id: l.id,
        MachineCode: l.machineCode,
        RisqueDefautId: l.risqueDefautId,
        LibelleDefaut: resoudreLibelleDefaut(l),
        OrdreAffiche: idx + 1
      }))
    };
  };

  const fetchDictionnaires = async () => {
    if (isDicosLoaded.value) return;
    try {
      const res = await planNcService.getDictionnaires();
      if (res.data.success) {
        postes.value = res.data.data.postes;
        machines.value = res.data.data.machines;
        risquesDefauts.value = res.data.data.risquesDefauts;
        isDicosLoaded.value = true;
      }
    } catch (error) {
      console.error('Erreur chargement dictionnaires PlanNC:', error);
    }
  };

  const fetchTousLesPlans = async () => {
    try {
      const res = await planNcService.getTousLesPlans();
      plansExistants.value = res.data.data || [];
    } catch (error) {
      console.error('Erreur chargement plans existants NC:', error);
    }
  };

  const mapperLigneDepuisServeur = (l) => ({
    _uid: genererUid(),
    id: l.id,
    machineCode: l.machineCode,
    risqueDefautId: l.risqueDefautId,
    _libelleDefautBrut: l.libelleDefaut,
    ordreAffiche: l.ordreAffiche
  });

  const chargerPlanNc = async (id) => {
    isLoading.value = true;
    try {
      const res = await planNcService.getPlanNc(id);
      const data = res.data.data;
      entete.value = {
        id: data.id,
        posteCode: data.posteCode,
        nom: data.nom,
        version: data.version,
        statut: data.statut,
        remarques: data.remarques || '',
        legendeMoyens: data.legendeMoyens || '',
      };
      lignes.value = (data.lignes || []).map(mapperLigneDepuisServeur);
      planInitialise.value = true;
      prendreSnapshot();
    } finally {
      isLoading.value = false;
    }
  };

  const initialiserNouveauPlan = (posteCode) => {
    const p = postes.value.find(x => x.code === posteCode);
    entete.value = {
      id: null,
      posteCode,
      nom: `Fiche de Contrôle - ${p?.libelle || posteCode}`,
      version: 1,
      statut: 'ACTIF',
    };
    lignes.value = [];
    // On ne pré-initialise plus de ligne par défaut
    planInitialise.value = true;
  };

  const ajouterLigne = () => {
    lignes.value.push({
      _uid: genererUid(),
      id: null,
      machineCode: '', // Ne pas pré-remplir
      risqueDefautId: null,
      ordreAffiche: lignes.value.length + 1
    });
  };

  const supprimerLigne = (uid) => {
    lignes.value = lignes.value.filter(l => l._uid !== uid);
  };

  const sauvegarderPlan = async () => {
    if (!aDesModifications()) {
      return { noChanges: true };
    }

    isLoading.value = true;
    try {
      const payload = buildPayload();
      
      if (entete.value.id) {
        // Mode Edition -> Nouvelle version
        const res = await planNcService.mettreAJourPlan(entete.value.id, payload);
        if (res.data.success) {
           entete.value.id = res.data.planId;
           prendreSnapshot();
        }
        return res.data;
      } else {
        // Mode Création
        const res = await planNcService.creerPlanNc(payload);
        if (res.data.success) {
          entete.value.id = res.data.planId;
          prendreSnapshot();
        }
        return res.data;
      }
    } finally {
      isLoading.value = false;
    }
  };

  const restaurerPlan = async (motif = "Restauration d'une ancienne version") => {
    isLoading.value = true;
    try {
      const payload = {
        AncienId: entete.value.id,
        ModifiePar: "ADMIN",
        MotifModification: motif
      };
      const res = await planNcService.restaurer(payload);
      return res.data;
    } finally {
      isLoading.value = false;
    }
  };

  const resetState = () => {
    entete.value = {
      id: null,
      posteCode: '',
      nom: '',
      version: 1,
      statut: 'ACTIF',
      remarques: '',
      legendeMoyens: '',
    };
    lignes.value = [];
    planInitialise.value = false;
    snapshotOriginal.value = null;
  };

  const trouverDefautParLibelle = (libelle) => {
    if (!libelle) return null;
    const libellePropre = libelle.trim().toLowerCase();
    return risquesDefauts.value.find(rd => rd.libelle?.trim().toLowerCase() === libellePropre);
  };

  const mapperLigneDepuisExcel = (l) => {
    const defautTrouve = trouverDefautParLibelle(l.libelleDefaut);
    return {
      _uid: genererUid(),
      id: null,
      machineCode: l.machineCode || '',
      risqueDefautId: defautTrouve?.id || null,
      ordreAffiche: 0,
      _libelleDefautBrut: defautTrouve ? null : l.libelleDefaut
    };
  };

  const importerDepuisExcel = async (file) => {
    isLoading.value = true;
    try {
      const res = await planNcService.importerExcel(file);
      const data = res.data.data;

      if (data.posteCode && !entete.value.posteCode) {
        entete.value.posteCode = data.posteCode;
      }
      if (data.nomPlan) entete.value.nom = data.nomPlan;
      if (data.remarques) entete.value.remarques = data.remarques.trim();

      const nouvellesLignes = (data.lignes || []).map(mapperLigneDepuisExcel);

      if (nouvellesLignes.length > 0) {
        lignes.value = nouvellesLignes;
        planInitialise.value = true;
      }

      const nbNonResolus = nouvellesLignes.filter(l => !l.risqueDefautId).length;
      return {
        success: true,
        total: nouvellesLignes.length,
        nbNonResolus
      };
    } finally {
      isLoading.value = false;
    }
  };

  return {
    postes, machines, risquesDefauts, isDicosLoaded,
    entete, lignes, isLoading, planInitialise, plansExistants,
    fetchDictionnaires, fetchTousLesPlans, initialiserNouveauPlan, chargerPlanNc,
    ajouterLigne, supprimerLigne, sauvegarderPlan, aDesModifications, restaurerPlan,
    resetState, importerDepuisExcel
  };
});
