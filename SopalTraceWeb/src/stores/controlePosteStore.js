import { defineStore } from 'pinia';
import { ref } from 'vue';
import { controlePosteService } from '@/services/controlePosteService';
import { qualityPlansService } from '@/services/qualityPlansService';
import { genererUid } from '@/utils/uuidUtils';
import apiClient from '@/services/apiClient';
import { parseDesignation } from '@/utils/designationParser';

export const useControlePosteStore = defineStore('ControlePoste', () => {
  // --- DICTIONNAIRES ---
  const postes = ref([]);
  const machines = ref([]);
  const risquesDefauts = ref([]);
  const formulairesReferences = ref([]);
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU PLAN ---
  const entete = ref({
    id: null,
    posteCode: '',
    nom: '',
    version: null,
    versionInitiale: null,
    statut: 'ACTIF',
    remarques: '',
    legendeMoyens: '',
    formulaireId: null,
    formulaireCodeReference: null, // ex: 'FE-RC-PAS71_SOUPAPE' - passé au backend pour éviter le lookup EF
    isModeleTemplate: false, // flag to know if we are editing a template
    configurationColonnes: {
      equipes: [
        { nom: 'Equipe 1', debut: 6, fin: 14 },
        { nom: 'Equipe 2', debut: 14, fin: 22 }
      ],
      customCols: []
    }
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
      ConfigurationColonnesJson: JSON.stringify(entete.value.configurationColonnes),
      // Transmettre FormulaireId ET CodeReference pour éviter un lookup EF Core côté backend
      // (le lookup causait un conflit de concurrence car le même enregistrement était tracké deux fois)
      FormulaireId: entete.value.formulaireId || null,
      FormulaireCodeReference: entete.value.formulaireCodeReference || null,
      // Inclure versionInitiale seulement lors de la création (pas d'édition)
      VersionInitiale: (!entete.value.id && entete.value.versionInitiale != null) ? entete.value.versionInitiale : undefined,
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
      const res = await controlePosteService.getDictionnaires();
      if (res.data.success) {
        postes.value = res.data.data.postes;
        machines.value = res.data.data.machines;
        risquesDefauts.value = res.data.data.risquesDefauts;
        isDicosLoaded.value = true;
      }
    } catch (error) {
      console.error('Erreur chargement dictionnaires ControlePoste:', error);
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

  const fetchTousLesPlans = async () => {
    try {
      const res = await controlePosteService.getTousLesPlans();
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

  const chargerControlePoste = async (id) => {
    isLoading.value = true;
    entete.value.isModeleTemplate = false;
    entete.value.formulaireId = null;
    
    try {
      const res = await controlePosteService.getControlePoste(id);
      const data = res.data.data;
      
      let configColonnes = {
        equipes: [
          { nom: 'Equipe 1', debut: 6, fin: 14 },
          { nom: 'Equipe 2', debut: 14, fin: 22 }
        ],
        customCols: []
      };
      
      if (data.configurationColonnesJson) {
        try {
          configColonnes = JSON.parse(data.configurationColonnesJson);
          if (!configColonnes.equipes) configColonnes.equipes = [];
          if (!configColonnes.customCols) configColonnes.customCols = [];
        } catch (e) {
          console.error('Failed to parse ConfigurationColonnesJson', e);
        }
      }
      
      entete.value = {
        id: data.id,
        posteCode: data.posteCode,
        nom: data.nom,
        version: data.version,
        statut: data.statut,
        remarques: data.remarques || '',
        legendeMoyens: data.legendeMoyens || '',
        formulaireId: null,
        isModeleTemplate: false,
        configurationColonnes: configColonnes
      };
      lignes.value = (data.lignes || []).map(mapperLigneDepuisServeur);
      planInitialise.value = true;
      prendreSnapshot();
    } catch (e) {
      // Fallback: Check if it's a generic template (RefFormulaire)
      try {
        const resForm = await apiClient.get(`/referentiels/modeles-generiques/${id}`);
        const data = resForm.data.data;
        
        let configColonnes = { equipes: [], customCols: [] };
        if (data.configurationStructureJson) {
          try {
            const parsed = JSON.parse(data.configurationStructureJson);
            if (Array.isArray(parsed)) {
              configColonnes.customCols = parsed;
            } else {
              configColonnes = parsed;
              if (!configColonnes.equipes) configColonnes.equipes = [];
              if (!configColonnes.customCols) configColonnes.customCols = [];
            }
          } catch (e) {
            console.error("Erreur parsing configurationStructureJson", e);
          }
        }
        
        if (configColonnes.equipes.length === 0) {
          configColonnes.equipes = [
            { nom: 'Equipe 1', debut: 6, fin: 14 },
            { nom: 'Equipe 2', debut: 14, fin: 22 }
          ];
        }

        // Use parseDesignation to find the poste
        const parsedDesignation = parseDesignation(data.designation, [], [], postes.value);
        const codePoste = parsedDesignation.posteCode || '';
        
        entete.value = {
          id: data.id,
          posteCode: codePoste,
          nom: data.designation, // we show designation as title
          version: data.version,
          statut: data.statut,
          remarques: '',
          legendeMoyens: '',
          formulaireId: data.id, // we save its own ID as the template ID
          isModeleTemplate: true, // IMPORTANT FLAG
          configurationColonnes: configColonnes
        };
        
        // Populate lines based on the poste
        if (codePoste) {
          // Initialize lines based on RisquesDefauts for this poste, just to show them
          // Wait, when initialising a plan, lines are empty, the user adds them manually.
          // BUT the user wants to see the table at the beginning! Wait, in ControlePosteForm.vue, 
          // the table ONLY shows the lines added by the user! If `lignes` is empty, it shows an empty state!
          lignes.value = [];
        } else {
          lignes.value = [];
        }
        
        planInitialise.value = true;
        prendreSnapshot();
      } catch (err) {
        console.error("Erreur lors du chargement (ni Plan, ni Modele)", err);
        throw e;
      }
    } finally {
      isLoading.value = false;
    }
  };

  const initialiserNouveauPlan = (posteCode, formulaireId = null, formulaireCodeReference = null) => {
    const p = postes.value.find(x => x.code === posteCode);
    entete.value = {
      id: null,
      posteCode,
      nom: `Fiche de Contrôle - ${p?.libelle || posteCode}`,
      version: null,
      versionInitiale: null,
      statut: 'ACTIF',
      remarques: '',
      legendeMoyens: '',
      // Conserver les infos du formulaire sélectionné (ex: FE-RC-PAS71_SOUPAPE)
      formulaireId: formulaireId,
      formulaireCodeReference: formulaireCodeReference,
      isModeleTemplate: false,
      configurationColonnes: {
        equipes: [
          { nom: 'Equipe 1', debut: 6, fin: 14 },
          { nom: 'Equipe 2', debut: 14, fin: 22 }
        ],
        customCols: []
      }
    };
    lignes.value = [];
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
      if (entete.value.isModeleTemplate) {
        // Mode Template -> call nouvelle-version on referentiels
        const payload = {
          ancienId: entete.value.formulaireId,
          configurationStructureJson: JSON.stringify(entete.value.configurationColonnes),
          modifiePar: 'ADMIN',
          motifModification: 'Mise à jour via Formulaire RC'
        };
        const res = await apiClient.post('/referentiels/modeles-generiques/nouvelle-version', payload);
        if (res.data.success) {
           entete.value.id = res.data.data.id;
           entete.value.formulaireId = res.data.data.id;
           entete.value.version = res.data.data.version;
           prendreSnapshot();
        }
        return { success: true, planId: res.data.data.id, message: "Modèle générique mis à jour avec succès." };
      } else {
        const payload = buildPayload();
        
        if (entete.value.id) {
          // Mode Edition -> Nouvelle version
          const res = await controlePosteService.mettreAJourPlan(entete.value.id, payload);
          if (res.data.success) {
             entete.value.id = res.data.planId;
             prendreSnapshot();
          }
          return res.data;
        } else {
          // Mode Création
          const res = await controlePosteService.creerControlePoste(payload);
          if (res.data.success) {
            entete.value.id = res.data.planId;
            prendreSnapshot();
          }
          return res.data;
        }
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
      const res = await controlePosteService.restaurer(payload);
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
      version: null,
      versionInitiale: null,
      statut: 'ACTIF',
      remarques: '',
      legendeMoyens: '',
      formulaireId: null,
      formulaireCodeReference: null,
      isModeleTemplate: false,
      configurationColonnes: {
        equipes: [
          { nom: 'Equipe 1', debut: 6, fin: 14 },
          { nom: 'Equipe 2', debut: 14, fin: 22 }
        ],
        customCols: []
      }
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
      const res = await controlePosteService.importerExcel(file);
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
    postes, machines, risquesDefauts, isDicosLoaded, formulairesReferences,
    entete, lignes, isLoading, planInitialise, plansExistants,
    fetchDictionnaires, fetchFormulairesReferences, fetchTousLesPlans, initialiserNouveauPlan, chargerControlePoste,
    ajouterLigne, supprimerLigne, sauvegarderPlan, aDesModifications, restaurerPlan,
    resetState, importerDepuisExcel
  };
});
