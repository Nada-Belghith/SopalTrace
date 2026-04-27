import { defineStore } from 'pinia';
import { ref } from 'vue';
import { verifMachineService } from '@/services/verifMachineService';

/**
 * Store Pinia pour la gestion des Plans de Vérification Machine.
 * Version Hiérarchique 3-Niveaux : Risque -> Groupes Périodicité -> Sous-lignes (Détails)
 */
export const useVerifMachineStore = defineStore('verifMachine', () => {

  // --- DICTIONNAIRES (chargés depuis le backend) ---
  const machines = ref([]);
  const periodicites = ref([]);
  const famillesCorps = ref([]);
  const moyensDetection = ref([]);
  const piecesReference = ref([]);
  const fuitesEtalon = ref([]);
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU PLAN ---
  const entete = ref({
    id: null,
    nom: '',
    machineCode: '',
    afficheConformite: true,
    afficheMoyenDetectionRisques: true,
    afficheFamilles: true,
    afficheFuiteEtalon: false,
    version: 1,
    statut: 'ACTIF',
  });

  const familles = ref([]);
  const lignesConformite = ref([]);
  const lignesRisques = ref([]);

  const isLoading = ref(false);
  const planInitialise = ref(false);

  // --- DÉTECTION DE CHANGEMENTS ---
  const snapshotOriginal = ref(null);
  const plansExistants = ref([]);

  const prendreSnapshot = () => {
    snapshotOriginal.value = JSON.stringify(buildPayload());
  };

  const aDesModifications = () => {
    if (!entete.value.id) return true;
    if (!snapshotOriginal.value) return true;
    return snapshotOriginal.value !== JSON.stringify(buildPayload());
  };

  // --- HELPERS ---
  const uuidv4 = () =>
    "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
      (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
    );

  const creerRowVide = () => ({
    _uid: uuidv4(),
    refMoyenDetectionId: '',
    matricePieces: [],
  });

  const creerGroupVide = () => ({
    _uid: uuidv4(),
    periodiciteId: '',
    rows: [creerRowVide()],
  });

  const creerLigneVide = () => ({
    _uid: uuidv4(),
    libelleRisque: '',
    libelleMethode: '',
    groups: [creerGroupVide()],
  });

  // --- MAPPING VERS LE FORMAT BACKEND (APLATISSEMENT) ---
  const buildPayload = () => {
    const code = entete.value.machineCode?.toUpperCase().trim() || '';
    const isSansConformiteAuto = code.includes('BEE22') || code.startsWith('SER') || code.includes('MAS19');
    const finalAfficheConformite = isSansConformiteAuto ? false : entete.value.afficheConformite;
    const finalAfficheFuiteEtalon = code.includes('BEE22') ? true : entete.value.afficheFuiteEtalon;

    return {
      Nom: entete.value.nom,
      MachineCode: entete.value.machineCode,
      AfficheConformite: finalAfficheConformite,
      AfficheMoyenDetectionRisques: entete.value.afficheMoyenDetectionRisques,
      AfficheFamilles: entete.value.afficheFamilles,
      AfficheFuiteEtalon: finalAfficheFuiteEtalon,
      Familles: familles.value.map((f, idx) => ({
        RefFamilleCorpsId: f.refFamilleCorpsId,
        OrdreAffiche: idx + 1,
      })),
      LignesConformite: finalAfficheConformite
        ? lignesConformite.value.map((l, idx) => mapLigneVersBackend(l, idx, 'CONFORMITE'))
        : [],
      LignesRisques: lignesRisques.value.map((l, idx) => mapLigneVersBackend(l, idx, 'RISQUE')),
    };
  };

  const mapLigneVersBackend = (ligne, indexLigne, typeLigne) => ({
    OrdreAffiche: indexLigne + 1,
    TypeLigne: typeLigne,
    LibelleRisque: ligne.libelleRisque,
    LibelleMethode: ligne.libelleMethode,
    Echeances: ligne.groups.flatMap((group, gIdx) =>
      group.rows.map((row, rIdx) => ({
        OrdreAffiche: (gIdx * 100) + rIdx + 1,
        PeriodiciteId: group.periodiciteId || '00000000-0000-0000-0000-000000000000',
        RefMoyenDetectionId: row.refMoyenDetectionId || null,
        MatricePieces: row.matricePieces.map(mp => ({
          FamilleId: mp.familleId || null,
          RoleVerif: mp.roleVerif || '',
          PieceRefId: mp.pieceRefId || null,
        })),
      }))
    ).filter(ech => ech.PeriodiciteId !== '00000000-0000-0000-0000-000000000000'),
  });

  // --- ACTIONS: MANIPULATION LOCALE ---
  const initialiserPlan = (machineCode) => {
    entete.value.machineCode = machineCode;
    entete.value.nom = `Rapport de vérification machine ${machineCode}`;
    entete.value.id = null;
    const code = machineCode.toUpperCase().trim();
    const isSansConformiteAuto = code.includes('BEE22') || code.startsWith('SER') || code.includes('MAS19');

    if (isSansConformiteAuto) {
      entete.value.afficheConformite = false;
    }

    familles.value = [];
    lignesConformite.value = [];
    lignesRisques.value = [];

    if (entete.value.afficheConformite) {
      lignesConformite.value.push(creerLigneVide());
    }
    lignesRisques.value.push(creerLigneVide());
    planInitialise.value = true;
    snapshotOriginal.value = null;
  };

  const resetPlan = () => {
    entete.value = { id: null, nom: '', machineCode: '', afficheConformite: true, afficheMoyenDetectionRisques: true, afficheFamilles: true, afficheFuiteEtalon: false, version: 1, statut: 'ACTIF' };
    familles.value = [];
    lignesConformite.value = [];
    lignesRisques.value = [];
    planInitialise.value = false;
    snapshotOriginal.value = null;
  };

  const ajouterFamille = (refFamilleCorpsId) => {
    if (!refFamilleCorpsId) return;
    const famRef = famillesCorps.value.find(f => f.id === refFamilleCorpsId);
    if (!famRef) return;
    if (familles.value.some(f => f.refFamilleCorpsId === refFamilleCorpsId)) return;

    familles.value.push({
      id: uuidv4(),
      refFamilleCorpsId: famRef.id,
      libelle: famRef.libelle
    });
  };

  const supprimerFamille = (id) => {
    familles.value = familles.value.filter(f => f.id !== id);
  };

  const ajouterLigneConformite = () => lignesConformite.value.push(creerLigneVide());
  const ajouterLigneRisque = () => lignesRisques.value.push(creerLigneVide());

  const supprimerLigne = (uid, type) => {
    if (type === 'CONFORMITE') {
      lignesConformite.value = lignesConformite.value.filter(l => l._uid !== uid);
    } else {
      lignesRisques.value = lignesRisques.value.filter(l => l._uid !== uid);
    }
  };

  // ✅ LOGIQUE DE HIÉRARCHIE 3 NIVEAUX
  const ajouterGroupPeriodicite = (ligne) => {
    ligne.groups.push(creerGroupVide());
  };

  const supprimerGroupPeriodicite = (ligne, groupUid) => {
    if (ligne.groups.length > 1) {
      ligne.groups = ligne.groups.filter(g => g._uid !== groupUid);
    }
  };

  const ajouterRowDetail = (group) => {
    group.rows.push(creerRowVide());
  };

  const supprimerRowDetail = (group, rowUid) => {
    if (group.rows.length > 1) {
      group.rows = group.rows.filter(r => r._uid !== rowUid);
    }
  };

  const getPieceValue = (row, familleId, roleVerif = 'PRC') => {
    const mp = row.matricePieces.find(m => m.familleId === familleId && m.roleVerif === roleVerif);
    return mp ? mp.pieceRefId : '';
  };

  const setPieceValue = (row, familleId, roleVerif, pieceRefId) => {
    const idx = row.matricePieces.findIndex(m => m.familleId === familleId && m.roleVerif === roleVerif);
    if (idx !== -1) {
      if (!pieceRefId) {
        row.matricePieces.splice(idx, 1);
      } else {
        row.matricePieces[idx].pieceRefId = pieceRefId;
        row.matricePieces[idx].roleVerif = roleVerif;
      }
    } else if (pieceRefId) {
      row.matricePieces.push({
        familleId: familleId,
        roleVerif: roleVerif,
        pieceRefId: pieceRefId,
      });
    }
  };

  // --- ACTIONS: API ---
  const fetchDictionnaires = async () => {
    if (isDicosLoaded.value) return;
    try {
      const response = await verifMachineService.getDictionnaires();
      const data = response.data.data || response.data;
      machines.value = data.machines || [];
      periodicites.value = data.periodicites || [];
      famillesCorps.value = data.famillesCorps || [];
      moyensDetection.value = data.moyensDetection || [];
      piecesReference.value = data.piecesReferences || [];
      fuitesEtalon.value = data.fuitesEtalon || [];
      isDicosLoaded.value = true;
    } catch (apiError) {
      console.error("Erreur réseau (Dictionnaires VM):", apiError);
      throw apiError;
    }
  };

  const fetchTousLesPlans = async () => {
    try {
      const response = await verifMachineService.getTousLesPlans();
      plansExistants.value = response.data.data || response.data || [];
    } catch (err) {
      console.error("Erreur chargement plans existants:", err);
    }
  };

  const chargerPlanVerif = async (id) => {
    isLoading.value = true;
    try {
      const response = await verifMachineService.getPlanVerif(id);
      const data = response.data.data || response.data;

      entete.value = {
        id: data.id,
        nom: data.nom,
        machineCode: data.machineCode,
        afficheConformite: data.afficheConformite,
        afficheMoyenDetectionRisques: data.afficheMoyenDetectionRisques,
        afficheFamilles: data.afficheFamilles,
        afficheFuiteEtalon: data.afficheFuiteEtalon,
        version: data.version || 1,
        statut: data.statut || 'ACTIF',
      };

      familles.value = (data.familles || []).map(f => {
        const dicoFam = famillesCorps.value.find(fc => fc.id === f.refFamilleCorpsId);
        return {
          id: f.id || uuidv4(),
          refFamilleCorpsId: f.refFamilleCorpsId,
          libelle: dicoFam ? dicoFam.libelle : 'Inconnue',
        };
      });

      lignesConformite.value = (data.lignesConformite || []).map(mapPlanLigneFromBackend);
      lignesRisques.value = (data.lignesRisques || []).map(mapPlanLigneFromBackend);

      planInitialise.value = true;
      prendreSnapshot();
    } finally {
      isLoading.value = false;
    }
  };

  const mapPlanLigneFromBackend = (ligne) => {
    // Reconstitution de la hiérarchie à partir des échéances plates
    const groups = [];
    const groupedByPerio = {};

    (ligne.echeances || []).forEach(ech => {
      const pId = ech.periodiciteId || 'none';
      if (!groupedByPerio[pId]) {
        groupedByPerio[pId] = {
          _uid: uuidv4(),
          periodiciteId: ech.periodiciteId || '',
          rows: []
        };
        groups.push(groupedByPerio[pId]);
      }
      groupedByPerio[pId].rows.push({
        _uid: uuidv4(),
        refMoyenDetectionId: ech.refMoyenDetectionId || '',
        matricePieces: (ech.piecesRef || []).map(mp => ({
          familleId: mp.familleId || null,
          roleVerif: mp.roleVerif || '',
          pieceRefId: mp.pieceRefId || null,
        }))
      });
    });

    return {
      _uid: uuidv4(),
      libelleRisque: ligne.libelleRisque || '',
      libelleMethode: ligne.libelleMethode || '',
      groups: groups.length > 0 ? groups : [creerGroupVide()],
    };
  };

  const sauvegarderPlanVerif = async () => {
    if (!aDesModifications()) {
      return { id: entete.value.id, noChanges: true };
    }

    isLoading.value = true;
    try {
      const payload = buildPayload();
      if (entete.value.id) {
        const response = await verifMachineService.mettreAJourPlanVerif(entete.value.id, payload);
        const newId = response.data.planId || response.data.id;
        entete.value.id = newId;
        prendreSnapshot();
        return { id: newId, noChanges: false };
      } else {
        const response = await verifMachineService.creerPlanVerif(payload);
        const newId = response.data.planId || response.data.id;
        entete.value.id = newId;
        prendreSnapshot();
        await fetchTousLesPlans();
        return { id: newId, noChanges: false };
      }
    } finally {
      isLoading.value = false;
    }
  };

  const restaurerPlanVerif = async (id, motif = "Restauration d'une ancienne version") => {
    isLoading.value = true;
    try {
      const payload = {
        AncienId: id,
        ModifiePar: "ADMIN",
        MotifModification: motif
      };
      const response = await verifMachineService.restaurerPlanVerif(payload);
      return response.data;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    machines, periodicites, famillesCorps, moyensDetection, piecesReference, fuitesEtalon, isDicosLoaded,
    entete, familles, lignesConformite, lignesRisques,
    isLoading, planInitialise, plansExistants,
    uuidv4, creerGroupVide, creerLigneVide,
    initialiserPlan, resetPlan,
    ajouterFamille, supprimerFamille,
    ajouterLigneConformite, ajouterLigneRisque, supprimerLigne,
    ajouterGroupPeriodicite, supprimerGroupPeriodicite, ajouterRowDetail, supprimerRowDetail,
    getPieceValue, setPieceValue,
    fetchDictionnaires, fetchTousLesPlans, chargerPlanVerif, sauvegarderPlanVerif, buildPayload, aDesModifications,
    restaurerPlanVerif
  };
});