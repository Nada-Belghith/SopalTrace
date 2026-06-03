import { defineStore } from 'pinia';
import { ref } from 'vue';
import { verifMachineService } from '@/services/verifMachineService';
import { qualityPlansService } from '@/services/qualityPlansService';
import { genererUid } from '@/utils/uuidUtils';

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
  const formulairesReferences = ref([]);
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
    version: 0,
    statut: 'ACTIF',
    remarques: '',
    legendeMoyens: '',
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
  const creerRowVide = () => ({
    _uid: genererUid(),
    refMoyenDetectionId: '',
    matricePieces: [],
  });

  const creerGroupVide = () => ({
    _uid: genererUid(),
    periodiciteId: '',
    rows: [creerRowVide()],
  });

  const creerLigneVide = () => ({
    _uid: genererUid(),
    libelleRisque: '',
    libelleMethode: '',
    valeursColonnesSpecifiques: {},
    groups: [creerGroupVide()],
  });

  // --- MAPPING VERS LE FORMAT BACKEND (APLATISSEMENT) ---
  const buildPayload = () => {
    const code = entete.value.machineCode?.toUpperCase().replace('-', '').replace(' ', '').trim() || '';
    const isSansConformiteAuto = code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') ||
      code.startsWith('SER') || code.includes('MAS19');
    const hasFuiteEtalonByDefault = code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') ||
      code.includes('MAS22');

    const finalAfficheConformite = isSansConformiteAuto ? false : entete.value.afficheConformite;
    const finalAfficheFuiteEtalon = hasFuiteEtalonByDefault ? true : entete.value.afficheFuiteEtalon;

    return {
      Nom: entete.value.nom,
      MachineCode: entete.value.machineCode,
      AfficheConformite: finalAfficheConformite,
      AfficheMoyenDetectionRisques: entete.value.afficheMoyenDetectionRisques,
      AfficheFamilles: entete.value.afficheFamilles,
      AfficheFuiteEtalon: finalAfficheFuiteEtalon,
      Remarques: entete.value.remarques || '',
      LegendeMoyens: entete.value.legendeMoyens || '',
      ConfigurationColonnesJson: typeof entete.value.configurationColonnes === 'string' ? entete.value.configurationColonnes : JSON.stringify(entete.value.configurationColonnes || []),
      Familles: familles.value
        .filter((f, idx, self) => self.findIndex(t => t.refFamilleCorpsId === f.refFamilleCorpsId) === idx)
        .map((f, idx) => ({
          RefFamilleCorpsId: f.refFamilleCorpsId,
          OrdreAffiche: idx + 1,
        })),
      LignesConformite: finalAfficheConformite
        ? lignesConformite.value.map((l, idx) => mapLigneVersBackend(l, idx, 'CONFORMITE'))
        : [],
      LignesRisques: lignesRisques.value.map((l, idx) => mapLigneVersBackend(l, idx, 'RISQUE')),
    };
  };

  const mapLigneVersBackend = (ligne, indexLigne, typeLigne) => {
    const rawEcheances = ligne.groups.flatMap((group, gIdx) =>
      group.rows.map((row, rIdx) => ({
        PeriodiciteId: group.periodiciteId || '00000000-0000-0000-0000-000000000000',
        RefMoyenDetectionId: row.refMoyenDetectionId || null,
        MatricePieces: row.matricePieces.map(mp => ({
          FamilleId: mp.familleId || null,
          RoleVerif: mp.roleVerif || '',
          PieceRefId: mp.pieceRefId || null,
        })),
      }))
    ).filter(ech => ech.PeriodiciteId !== '00000000-0000-0000-0000-000000000000');

    // Fusionner les doublons (Perio + Moyen) pour éviter les erreurs SQL UNIQUE KEY
    const mergedMap = new Map();
    rawEcheances.forEach(ech => {
      const key = `${ech.PeriodiciteId}|${ech.RefMoyenDetectionId || 'null'}`;
      if (!mergedMap.has(key)) {
        mergedMap.set(key, { ...ech, MatricePieces: [...ech.MatricePieces] });
      } else {
        const existing = mergedMap.get(key);
        ech.MatricePieces.forEach(mp => {
          const mpExists = existing.MatricePieces.some(em =>
            em.FamilleId === mp.FamilleId && em.RoleVerif === mp.RoleVerif && em.PieceRefId === mp.PieceRefId
          );
          if (!mpExists) existing.MatricePieces.push(mp);
        });
      }
    });

    return {
      OrdreAffiche: indexLigne + 1,
      TypeLigne: typeLigne,
      LibelleRisque: ligne.libelleRisque,
      LibelleMethode: ligne.libelleMethode,
      ColonnesSupplementaires: ligne.valeursColonnesSpecifiques && Object.keys(ligne.valeursColonnesSpecifiques).length > 0
        ? JSON.stringify(ligne.valeursColonnesSpecifiques)
        : null,
      Echeances: Array.from(mergedMap.values()).map((ech, idx) => ({
        ...ech,
        OrdreAffiche: idx + 1
      })),
    };
  };

  // --- ACTIONS: MANIPULATION LOCALE ---
  const initialiserPlan = async (machineCode) => {
    entete.value.machineCode = machineCode;
    entete.value.nom = `Rapport de vérification machine ${machineCode}`;
    entete.value.id = null;
    entete.value.afficheConformite = true;
    entete.value.afficheMoyenDetectionRisques = true;
    entete.value.afficheFamilles = true;
    entete.value.afficheFuiteEtalon = false;

    const code = machineCode.toUpperCase().replace('-', '').replace(' ', '').trim();
    const isSansConformiteAuto = code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') ||
      code.startsWith('SER') || code.includes('MAS19') || code.includes('MAS20');
    const hasFuiteEtalonByDefault = code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47');

    if (isSansConformiteAuto) {
      entete.value.afficheConformite = false;
    }
    if (hasFuiteEtalonByDefault) {
      entete.value.afficheFuiteEtalon = true;
    }

    familles.value = [];

    // ✅ CHARGEMENT DYNAMIQUE des familles depuis Machine_FamilleCorps (API)
    try {
      const response = await verifMachineService.getFamillesParMachine(machineCode);
      const dataArray = response.data?.data; // Car l'API renvoie { success, data: [] }

      if (dataArray && dataArray.length > 0) {
        familles.value = dataArray.map(f => ({
          id: genererUid(),
          refFamilleCorpsId: f.id,
          libelle: f.designation || f.code
        }));
      }
    } catch (e) {
      console.warn(`[VerifMachine] Aucune famille trouvée pour la machine ${machineCode}:`, e);
    }

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
    entete.value = { id: null, nom: '', machineCode: '', afficheConformite: true, afficheMoyenDetectionRisques: true, afficheFamilles: true, afficheFuiteEtalon: false, version: 0, statut: 'ACTIF', remarques: '', legendeMoyens: '' };
    familles.value = [];
    lignesConformite.value = [];
    lignesRisques.value = [];
    planInitialise.value = false;
    plansExistants.value = [];
    snapshotOriginal.value = null;
  };

  const ajouterFamille = (refFamilleCorpsId) => {
    if (!refFamilleCorpsId) return;

    // On cherche d'abord dans les familles de corps
    let itemRef = famillesCorps.value.find(f => f.id === refFamilleCorpsId);

    // Si non trouvé, on cherche dans les pièces de référence (PRC/PRNC)
    if (!itemRef) {
      const pieceRef = piecesReference.value.find(p => p.id === refFamilleCorpsId);
      if (pieceRef) {
        itemRef = {
          id: pieceRef.id,
          libelle: pieceRef.code + (pieceRef.designation ? ` - ${pieceRef.designation}` : '')
        };
      }
    }

    if (!itemRef) return;
    if (familles.value.some(f => f.refFamilleCorpsId === refFamilleCorpsId)) return;

    // Éviter les doublons de nom (ex: Excel importe une pièce avec un ID différent mais le même libellé)
    const norm = s => s?.replace(/\s+/g, '').toUpperCase() || '';
    if (familles.value.some(f => norm(f.libelle) === norm(itemRef.libelle))) return;

    familles.value.push({
      id: genererUid(),
      refFamilleCorpsId: itemRef.id,
      libelle: itemRef.libelle
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
  const fetchDictionnaires = async (force = false) => {
    if (isDicosLoaded.value && !force) return;
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
        version: data.version ?? 0,
        statut: data.statut || 'ACTIF',
        remarques: data.remarques || '',
        legendeMoyens: data.legendeMoyens || '',
        configurationColonnes: data.configurationColonnesJson ? (typeof data.configurationColonnesJson === 'string' ? JSON.parse(data.configurationColonnesJson) : data.configurationColonnesJson) : [],
      };

      familles.value = (data.familles || []).map(f => {
        const dicoFam = famillesCorps.value.find(fc => fc.id === f.refFamilleCorpsId);
        return {
          id: f.id || genererUid(),
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
          _uid: genererUid(),
          periodiciteId: ech.periodiciteId || '',
          rows: []
        };
        groups.push(groupedByPerio[pId]);
      }
      groupedByPerio[pId].rows.push({
        _uid: genererUid(),
        refMoyenDetectionId: ech.refMoyenDetectionId || '',
        matricePieces: (ech.piecesRef || []).map(mp => ({
          familleId: mp.familleId || null,
          roleVerif: mp.roleVerif || '',
          pieceRefId: mp.pieceRefId || null,
        }))
      });
    });

    return {
      _uid: genererUid(),
      libelleRisque: ligne.libelleRisque || '',
      libelleMethode: ligne.libelleMethode || '',
      valeursColonnesSpecifiques: ligne.colonnesSupplementaires ? (typeof ligne.colonnesSupplementaires === 'string' ? JSON.parse(ligne.colonnesSupplementaires) : ligne.colonnesSupplementaires) : {},
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

  const importerDepuisExcel = async (file) => {
    isLoading.value = true;
    try {
      const configColonnesJson = typeof entete.value.configurationColonnes === 'string'
        ? entete.value.configurationColonnes
        : JSON.stringify(entete.value.configurationColonnes || []);

      const response = await verifMachineService.importExcel(file, configColonnesJson);
      const data = response.data.data;

      // ✅ REFRESH DICTIONARIES (Backend might have created new families/pieces)
      await fetchDictionnaires(true);

      const targetMachineCode = data.machineCode || entete.value.machineCode;

      if (targetMachineCode !== entete.value.machineCode || !planInitialise.value) {
        // Si on change de machine ou que le plan n'était pas initialisé, on recharge complètement
        await initialiserPlan(targetMachineCode);
      } else {
        // Sinon, on garde les familles telles qu'elles sont à l'écran (avec les suppressions manuelles !)
        // On vide simplement les lignes pour les remplacer par l'Excel
        lignesConformite.value = [];
        lignesRisques.value = [];
      }

      // Re-charger les dictionnaires pour inclure les éventuelles nouvelles pièces créées par le backend
      await fetchDictionnaires(true);

      // ✅ IMPORT DES FAMILLES DÉTECTÉES DANS L'EXCEL
      if (data.familles?.length > 0) {
        // On ne vide plus les familles existantes pour préserver les ajouts manuels
        const normalizeStr = s => s?.replace(/\s+/g, '').toUpperCase() || '';
        data.familles.forEach(fStr => {
          const normFStr = normalizeStr(fStr);
          // On cherche dans les dicos fraîchement mis à jour
          const famRef = famillesCorps.value.find(fc =>
            normalizeStr(fc.libelle) === normFStr ||
            normalizeStr(fc.code) === normFStr ||
            fStr.includes(`(${fc.libelle})`)
          );

          if (famRef) {
            ajouterFamille(famRef.id);
          } else {
            // C'est peut-être une pièce de référence directe
            const pRef = piecesReference.value.find(pr =>
              normalizeStr(pr.code) === normFStr
            );
            if (pRef) ajouterFamille(pRef.id);
          }
        });
      }

      if (data.nomPlan) entete.value.nom = data.nomPlan;
      if (data.remarques) entete.value.remarques = data.remarques;
      if (data.legendeMoyens) entete.value.legendeMoyens = data.legendeMoyens;

      // Mapping des lignes
      const mapLignes = (lignesImportees) => {
        return lignesImportees.map(li => ({
          _uid: genererUid(),
          libelleRisque: li.libelleRisque,
          libelleMethode: li.libelleMethode,
          valeursColonnesSpecifiques: li.colonnesSupplementaires || {},
          groups: li.echeances.map(ech => ({
            _uid: genererUid(),
            periodiciteId: ech.periodiciteId || '',
            // 🟢 NOUVEAU : Mapper chaque Row (moyen) sous la périodicité
            rows: (ech.rows || []).map(rowItem => ({
              _uid: genererUid(),
              refMoyenDetectionId: moyensDetection.value.find(m =>
                m.libelle?.trim().toUpperCase() === rowItem.moyenDetectionLibelle?.trim().toUpperCase() ||
                m.code?.trim().toUpperCase() === rowItem.moyenDetectionLibelle?.trim().toUpperCase()
              )?.id || '',
              matricePieces: (() => {
                const pieces = (rowItem.matricePieces || []).map(mp => {
                  const pieceRef = piecesReference.value.find(p => p.code === mp.pieceRefCode)
                    || fuitesEtalon.value.find(p => p.code === mp.pieceRefCode);

                  let matchingFamId = null;
                  if (mp.familleCode) {
                    const normMpFam = mp.familleCode.replace(/\s+/g, '').toUpperCase();
                    const importedFam = familles.value.find(f => {
                      const normFLib = f.libelle?.replace(/\s+/g, '').toUpperCase() || '';
                      return normFLib === normMpFam || mp.familleCode.includes(`(${f.libelle})`);
                    });
                    if (importedFam) matchingFamId = importedFam.refFamilleCorpsId;
                  }

                  return {
                    familleId: matchingFamId,
                    roleVerif: mp.roleVerif,
                    pieceRefId: mp.pieceRefId || (pieceRef ? pieceRef.id : null)
                  };
                });

                // Deduplicate to prevent UNIQUE KEY violations
                const seen = new Set();
                return pieces.filter(p => {
                  if (!p.pieceRefId) return false;
                  const key = `${p.familleId || 'null'}-${p.roleVerif}`;
                  if (seen.has(key)) return false;
                  seen.add(key);
                  return true;
                });
              })()
            }))
          }))
        }));
      };

      const checkMachineSansConformite = (machineCode) => {
        if (!machineCode) return false;
        const code = machineCode.toUpperCase().replace('-', '').replace(' ', '').trim();
        return code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') || 
               code.includes('MAS19') || code.includes('MAS20') || code.startsWith('SER');
      };

      if (checkMachineSansConformite(entete.value.machineCode)) {
        // Force all lines into Risques if the machine doesn't support Conformite
        const combinedLines = [...(data.lignesConformite || []), ...(data.lignesRisques || [])];
        if (combinedLines.length > 0) {
          lignesRisques.value = mapLignes(combinedLines);
        }
      } else {
        if (data.lignesConformite?.length > 0) {
          lignesConformite.value = mapLignes(data.lignesConformite);
          entete.value.afficheConformite = true;
        }
        if (data.lignesRisques?.length > 0) {
          lignesRisques.value = mapLignes(data.lignesRisques);
        }
      }

      return { success: true };
    } finally {
      isLoading.value = false;
    }
  };

  return {
    machines, periodicites, famillesCorps, moyensDetection, piecesReference, fuitesEtalon, isDicosLoaded, formulairesReferences,
    entete, familles, lignesConformite, lignesRisques,
    isLoading, planInitialise, plansExistants,
    genererUid, creerGroupVide, creerLigneVide,
    initialiserPlan, resetPlan,
    ajouterFamille, supprimerFamille,
    ajouterLigneConformite, ajouterLigneRisque, supprimerLigne,
    ajouterGroupPeriodicite, supprimerGroupPeriodicite, ajouterRowDetail, supprimerRowDetail,
    getPieceValue, setPieceValue,
    fetchDictionnaires, fetchFormulairesReferences, fetchTousLesPlans, chargerPlanVerif, sauvegarderPlanVerif, buildPayload, aDesModifications,
    restaurerPlanVerif, importerDepuisExcel
  };
});
