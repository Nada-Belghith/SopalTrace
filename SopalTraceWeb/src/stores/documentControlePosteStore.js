import { defineStore } from 'pinia';
import { ref } from 'vue';
import { documentControlePosteService } from '@/services/documentControlePosteService';
import { referentielsService } from '@/services/referentielsService';
import { genererUid } from '@/utils/uuidUtils';
import apiClient from '@/services/apiClient';
import { parseDesignation } from '@/utils/designationParser';

export const usedocumentControlePosteStore = defineStore('documentControlePoste', () => {
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
    // Build the lignes for the single section
    const lignesDtos = lignes.value
      .filter(l => l.machineCode || l.risqueDefautId || l._libelleDefautBrut)
      .map((l, idx) => ({
        Id: l.id || undefined,
        // Architecture: MachineCodeCtrlPoste is the correct field for CTRL_POSTE machine
        MachineCodeCtrlPoste: l.machineCode || null,
        MachineCode: null,
        RisqueDefautId: l.risqueDefautId || null,
        // LibelleAffiche stores the defaut designation text
        LibelleAffiche: resoudreLibelleDefaut(l) || null,
        OrdreAffiche: idx + 1
      }));

    return {
      Id: entete.value.id || undefined,
      TypeDocumentCode: 'CTRL_POSTE',
      PosteCode: entete.value.posteCode,
      FamilleProduitFiniCode: entete.value.familleProduitFiniCode || null,
      Nom: entete.value.nom,
      Remarques: entete.value.remarques || '',
      LegendeMoyens: entete.value.legendeMoyens || '',
      ConfigurationColonnesJson: JSON.stringify(entete.value.configurationColonnes),
      FormulaireId: entete.value.formulaireId || null,
      RefFormulaireCodeReference: entete.value.formulaireCodeReference || null,
      VersionInitiale: (!entete.value.id && entete.value.versionInitiale != null) ? entete.value.versionInitiale : undefined,
      // CTRL_POSTE stores lines in a single unnamed section
      Sections: [{
        Id: entete.value.sectionId || undefined,
        OrdreAffiche: 1,
        LibelleSection: 'Lignes de Contrôle',
        Lignes: lignesDtos
      }]
    };
  };

  const fetchDictionnaires = async () => {
    if (isDicosLoaded.value) return;
    try {
      const res = await documentControlePosteService.getDictionaries();
      if (res.data.success) {
        postes.value = res.data.data.postes;
        machines.value = res.data.data.machines;
        risquesDefauts.value = res.data.data.risquesDefauts;
        isDicosLoaded.value = true;
      }
    } catch (error) {
      console.error('Erreur chargement dictionnaires documentControlePoste:', error);
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

  const fetchTousLesPlans = async () => {
    try {
      const res = await documentControlePosteService.getAllDocuments();
      const data = res?.data;
      plansExistants.value = Array.isArray(data) ? data : (data?.data || []);
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

  const chargerdocumentControlePoste = async (id) => {
    isLoading.value = true;
    entete.value.isModeleTemplate = false;
    entete.value.formulaireId = null;

    try {
      const res = await documentControlePosteService.getDocumentControlePoste(id);
      // documentService.getById returns response.data (the DTO directly),
      // documentControlePosteService wraps it as { data: DTO }, so we read res.data (not res.data.data)
      const data = res.data;

      if (!data || !data.id) throw new Error('Réponse vide du serveur');

      // Parse configuration des colonnes/équipes
      let configColonnes = {
        equipes: [
          { nom: 'Equipe 1', debut: 6, fin: 14 },
          { nom: 'Equipe 2', debut: 14, fin: 22 }
        ],
        customCols: []
      };

      if (data.configurationColonnesJson) {
        try {
          const parsed = JSON.parse(data.configurationColonnesJson);
          if (parsed.equipes && parsed.equipes.length > 0) {
            configColonnes.equipes = parsed.equipes;
          }
        } catch (e) {
          console.error('Failed to parse ConfigurationColonnesJson', e);
        }
      }

      if (data.colonneDefs && data.colonneDefs.length > 0) {
        configColonnes.customCols = data.colonneDefs.map(c => ({
          key: c.cleColonne,
          label: c.labelAffiche || c.titre,
          type: c.typeValeur || 'Texte',
          insertAfter: c.insertAfter || 'col_designation'
        }));
      }

      entete.value = {
        id: data.id,
        posteCode: data.posteCode,
        nom: data.nom,
        version: data.version,
        statut: data.statut,
        remarques: data.remarques || '',
        legendeMoyens: data.legendeMoyens || '',
        formulaireId: data.formulaireId || null,
        formulaireCodeReference: data.formulaireCodeReference || null,
        isModeleTemplate: false,
        configurationColonnes: configColonnes,
        sectionId: (data.sections && data.sections.length > 0) ? data.sections[0].id : null
      };

      // Use lignesControlePoste (new DTO field) to reload saved lines
      const rawLignes = (data.lignesControlePoste || []).map(l => ({
        _uid: genererUid(),
        id: l.id,
        machineCode: l.machineCode,
        risqueDefautId: l.risqueDefautId,
        _libelleDefautBrut: l.libelleDefaut,
        ordreAffiche: l.ordreAffiche
      }));

      lignes.value = rawLignes.length > 0 ? rawLignes : [{
        _uid: genererUid(),
        id: null,
        machineCode: '',
        risqueDefautId: null,
        ordreAffiche: 1
      }];

      planInitialise.value = true;
      prendreSnapshot();
    } catch (e) {
      console.error('Erreur lors du chargement du Contrôle Poste', e);
    } finally {
      isLoading.value = false;
    }
  };

  const initialiserNouveauPlan = (posteCode, formulaireId = null, formulaireCodeReference = null, configurationColonnes = null) => {
    const p = postes.value.find(x => x.code === posteCode);
    entete.value = {
      id: null,
      posteCode,
      nom: `Résultat DocumentControlePoste_${p?.libelle || posteCode}`,
      version: null,
      versionInitiale: null,
      statut: 'ACTIF',
      remarques: '',
      legendeMoyens: '',
      formulaireId: formulaireId,
      formulaireCodeReference: formulaireCodeReference,
      isModeleTemplate: false,
      configurationColonnes: configurationColonnes || {
        equipes: [
          { nom: 'Equipe 1', debut: 6, fin: 14 },
          { nom: 'Equipe 2', debut: 14, fin: 22 }
        ],
        customCols: []
      }
    };
    lignes.value = [{
      _uid: genererUid(),
      id: null,
      machineCode: '',
      risqueDefautId: null,
      ordreAffiche: 1
    }];
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

  const sauvegarderPlan = async (isCorrection = false, forceSave = false) => {
    if (!forceSave && isCorrection && !aDesModifications()) {
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
          if (!isCorrection && entete.value.statut === 'ACTIF') {
            // Mode Edition -> Nouvelle version
            // We pass the documentId as ancienId in the payload
            payload.ancienId = entete.value.id;
            const res = await documentControlePosteService.createNewVersion(payload);
            // La réponse contient data: { id: ... } ou data: { id: { id: ... } } selon le wrapping
            const newId = res.data?.id?.id || res.data?.id || res.data?.planId;
            if (newId) {
              entete.value.id = newId;
              prendreSnapshot();
            }
            return { success: true, planId: entete.value.id, message: "Nouvelle version créée avec succès" };
          } else {
            // Mode Edition -> Correction mineure ou update Brouillon
            const res = await documentControlePosteService.updateDocumentControlePoste(entete.value.id, payload);
            prendreSnapshot();
            return { success: true, planId: entete.value.id, message: "Plan mis à jour avec succès" };
          }
        } else {
          // Mode Création
          const res = await documentControlePosteService.createDocumentControlePoste(payload);
          const newId = res.data?.id?.id || res.data?.id || res.data?.planId;
          if (newId) {
            entete.value.id = newId;
            prendreSnapshot();
          }
          return { success: true, planId: entete.value.id, message: "Brouillon créé avec succès" };
        }
      }
    } catch (error) {
      const msg = error?.response?.data?.message || error?.message || 'Erreur lors de la sauvegarde du plan';
      return { success: false, message: msg };
    } finally {
      isLoading.value = false;
    }
  };

  const createNewVersion = async (motif = '') => {
    return await sauvegarderPlan(false, true);
  };
  const creerNouvelleVersion = createNewVersion;

  const restaurerPlan = async (motif = "Restauration d'une ancienne version") => {
    isLoading.value = true;
    try {
      const payload = {
        AncienId: entete.value.id,
        ModifiePar: "ADMIN",
        MotifModification: motif
      };
      const res = await documentControlePosteService.restore(payload);
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
    lignes.value = [{
      _uid: genererUid(),
      id: null,
      machineCode: '',
      risqueDefautId: null,
      ordreAffiche: 1
    }];
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
      const res = await documentControlePosteService.importExcel(file);
      const data = res.data;

      // Check if backend API returned business errors
      if (data && (data.succes === false || data.success === false)) {
        return {
          success: false,
          message: (data.erreurs && data.erreurs.length) ? data.erreurs.join(', ') : 'Le fichier Excel est invalide ou vide.'
        };
      }

      if (data.posteCode && !entete.value.posteCode) {
        entete.value.posteCode = data.posteCode;
      }
      if (data.nomPlan && !entete.value.nom) entete.value.nom = data.nomPlan;
      
      // Ensure data.remarques is a string before calling trim
      if (data.remarques) {
        entete.value.remarques = String(data.remarques).trim();
      }

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
    } catch (error) {
      console.error('Erreur import Excel:', error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    postes, machines, risquesDefauts, isDicosLoaded, formulairesReferences,
    entete, lignes, isLoading, planInitialise, plansExistants,
    fetchDictionnaires, fetchFormulairesReferences, fetchTousLesPlans, initialiserNouveauPlan, chargerdocumentControlePoste,
    ajouterLigne, supprimerLigne, sauvegarderPlan, createNewVersion, creerNouvelleVersion, aDesModifications, restaurerPlan,
    resetState, importerDepuisExcel
  };
});
