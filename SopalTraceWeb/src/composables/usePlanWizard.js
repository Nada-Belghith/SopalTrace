import { ref, computed, watch } from 'vue';
import { useToast } from 'primevue/usetoast';
import { referentielsService } from '@/services/referentielsService';
import { documentService as fabModeleService } from '@/services/documentService';
import { documentService as fabPlanService } from '@/services/documentService';
import { useFabModeleStore } from '@/stores/fabModeleStore'; // Accès au dictionnaire global

/**
 * Composable pour gérer la logique du wizard de création de plan
 * Responsabilité : Orchestrer l'assistant étape par étape (Article -> Opération -> Modèles filtrés)
 */
export function usePlanWizard() {
  const toast = useToast();
  const store = useFabModeleStore();

  // ============================================================================
  // STATE
  // ============================================================================
  const codeArticleSage = ref(''); // Peut être un objet si sélectionné depuis AutoComplete
  const designationArticle = ref('');
  const typeRobinetCode = ref('');
  const natureComposantCode = ref('');
  const isArticleValid = ref(false);
  const isCheckingArticle = ref(false);

  const filteredArticles = ref([]);
  const isSearchingArticles = ref(false);

  const searchArticles = async (event) => {
    try {
      const query = event.query;
      if (!query || query.trim().length === 0) {
        filteredArticles.value = [];
        return;
      }
      isSearchingArticles.value = true;
      const res = await referentielsService.searchArticlesSf(query);
      filteredArticles.value = res.data || [];
    } catch (e) {
      console.error(e);
      filteredArticles.value = [];
    } finally {
      isSearchingArticles.value = false;
    }
  };

  // Utilisation stricte de 1 ou 0
  const isGenerique = ref(0);

  // 🔍 DEBUG : Affiche les infos de vérification
  const debugInfo = ref({
    natureLookup: null,
    isGeneriqueValue: 0,
    timestamp: null
  });

  const operationCode = ref('');
  const posteCode = ref('');   // Poste de travail BDD (ex : PAS71, PAS72, PAS78)
  const familleCode = ref(''); // Famille d'article (uniquement pour PF)

  const sourceType = ref('MODELE');
  const selectedSourceId = ref(null);
  const refFormulaireCodeReference = ref('PRC');
  const isGenerating = ref(false);

  // Listes et état de chargement pour les sources
  const availableModeles = ref([]);
  const availablePlans = ref([]);
  const isLoadingSources = ref(false);

  // ============================================================================
  // FAMILLES (Filtrage par Type Robinet)
  // ============================================================================
  const famillesFiltrees = computed(() => {
    const type = String(typeRobinetCode.value || '').trim().toUpperCase();
    if (!type) return [];

    return (store.famillesProduit || [])
      .filter(f => String(f.typeRobinetCode || '').trim().toUpperCase() === type);
  });

  const requiertFamille = computed(() => {
    const nat = String(natureComposantCode.value || '').trim().toUpperCase();
    // Uniquement pour les composants Finis (PF)
    return nat === 'PF';
  });

  const isPlanCreationBlocked = computed(() => {
    const nat = String(natureComposantCode.value || '').trim().toUpperCase();
    // Bloquer la création pour PISTON et PF (Pas de plan par article)
    return nat === 'PISTON' || nat === 'PF';
  });

  const hasValidStructure = computed(() => {
    const refs = store.formulairesReferences || [];
    if (refs.length === 0) return false;
    return refs.some(r => {
      const s = String(r.statut || r.Statut || '').trim().toUpperCase();
      return s === 'ACTIF';
    });
  });

  // ============================================================================
  // ÉTAPE 1: VÉRIFICATION ERP
  // ============================================================================
  const verifierArticleERP = async () => {
    if (!codeArticleSage.value) return;

    // Extraire le code si l'utilisateur a sélectionné un objet depuis l'AutoComplete
    let codeStr = codeArticleSage.value;
    if (typeof codeArticleSage.value === 'object' && codeArticleSage.value !== null) {
      codeStr = codeArticleSage.value.codeArticle;
      codeArticleSage.value = codeStr; // On remet en chaîne pour l'affichage
    }

    if (!codeStr) return;

    isCheckingArticle.value = true;
    try {
      const response = await referentielsService.getArticleFromERP(codeStr);
      // Correction: le backend renvoie { success: true, data: { ... } }
      const articleData = response.data?.data || response.data || response;

      designationArticle.value = articleData.designation || '';
      typeRobinetCode.value = articleData.typeRobinetCode || '';
      natureComposantCode.value = articleData.natureComposantCode || '';
      isArticleValid.value = true;

      // 🔧 Récupérer estGenerique depuis la table naturesComposant du store
      const nature = store.naturesComposant?.find(n => {
        const nCode = n.code || n.Code;
        const aCode = articleData.natureComposantCode || articleData.NatureComposantCode;
        return nCode && aCode && String(nCode).trim().toUpperCase() === String(aCode).trim().toUpperCase();
      });

      isGenerique.value = (nature?.estGenerique || nature?.EstGenerique) === true ? 1 : 0;

      // 🔍 DEBUG : Stocker les infos pour l'inspecteur
      debugInfo.value = {
        natureLookup: nature,
        isGeneriqueValue: isGenerique.value,
        timestamp: new Date().toLocaleTimeString(),
        articleCode: articleData.natureComposantCode || articleData.NatureComposantCode,
        allNatures: store.naturesComposant
      };

      operationCode.value = '';
      posteCode.value = '';

      // 🤖 AUTO-SÉLECTION DE L'OPÉRATION (via Gamme Opératoire)
      if (natureComposantCode.value && isGenerique.value === 0) {
        const targetNature = String(natureComposantCode.value).trim().toUpperCase();
        const opsPossibles = (store.gammesOperatoires || [])
          .filter(g => {
            const code = g.natureComposantCode || g.NatureComposantCode;
            return code && String(code).trim().toUpperCase() === targetNature;
          })
          .map(g => g.operationCode || g.OperationCode);

        if (opsPossibles.length === 1) {
          operationCode.value = opsPossibles[0];
          console.log(`Auto-sélection de l'opération unique : ${operationCode.value}`);
        }
      }

      sourceType.value = 'MODELE';
      selectedSourceId.value = null;
      availableModeles.value = [];
      availablePlans.value = [];
      posteCode.value = '';

      toast.add({
        severity: 'success',
        summary: 'Article identifié',
        detail: operationCode.value
          ? `${articleData.designation || 'Article trouvé'} - Opération "${operationCode.value}" sélectionnée.`
          : `${articleData.designation || 'Article trouvé'} - Veuillez choisir l'opération.`,
        life: 3000
      });
    } catch (error) {
      console.error('Erreur vérification article ERP:', error);
      toast.add({
        severity: 'error',
        summary: 'Introuvable',
        detail: 'Cet article n\'existe pas dans l\'ERP SAGE X3.',
        life: 4000
      });
      reset();
    } finally {
      isCheckingArticle.value = false;
    }
  };

  // ============================================================================
  // ÉTAPE 2: FILTRES DES OPÉRATIONS (Via la Gamme Opératoire en BDD)
  // ============================================================================
  const operationsFiltrees = computed(() => {
    const ops = store.operations || [];
    const gammes = store.gammesOperatoires || [];

    // Si pas d'article identifié, on affiche tout par défaut
    if (!natureComposantCode.value) return ops;

    const targetNature = String(natureComposantCode.value).trim().toUpperCase();

    // Croisement BDD : Quelles opérations correspondent à cette Nature de composant ?
    // On gère les deux types de casse (camelCase/PascalCase) pour être robuste aux changements de sérialiseur
    const operationsPermises = gammes
      .filter(g => {
        const code = g.natureComposantCode || g.NatureComposantCode;
        return code && String(code).trim().toUpperCase() === targetNature;
      })
      .map(g => String(g.operationCode || g.OperationCode || '').trim().toUpperCase())
      .filter(code => code !== '');

    // Fallback : Si aucune gamme n'est définie spécifiquement pour cette nature, 
    // on affiche toutes les opérations plutôt que rien, pour permettre la création libre.
    if (operationsPermises.length === 0) {
      console.warn(`Aucune gamme opératoire trouvée pour la nature "${targetNature}". Affichage de toutes les opérations.`);
      return ops;
    }

    return ops.filter(op => {
      const code = op.code || op.Code;
      return code && operationsPermises.includes(String(code).trim().toUpperCase());
    });
  });

  const getLibelleType = (code) => {
    if (!code) return '--';
    const match = store.typesRobinet?.find(t => {
      const tCode = t.code || t.Code;
      return tCode && String(tCode).trim().toUpperCase() === String(code).trim().toUpperCase();
    });
    return match?.libelle || match?.Libelle || code;
  };

  const getLibelleNature = (code) => {
    if (!code) return '--';
    const match = store.naturesComposant?.find(n => {
      const nCode = n.code || n.Code;
      return nCode && String(nCode).trim().toUpperCase() === String(code).trim().toUpperCase();
    });
    return match?.libelle || match?.Libelle || code;
  };

  // ============================================================================
  // ÉTAPE 3: CHARGEMENT DES MODÈLES (Déclenché par événement)
  // ============================================================================
  const chargerModelesFiltrés = async () => {
    // ❌ BLOQUER SI ARTICLE GÉNÉRIQUE
    if (isGenerique.value === 1) {
      availableModeles.value = [];
      return;
    }

    if (!typeRobinetCode.value || !natureComposantCode.value || !operationCode.value) return;

    isLoadingSources.value = true;
    try {
      const response = await fabModeleService.getModelesByFilters(
        typeRobinetCode.value,
        natureComposantCode.value,
        operationCode.value,
        posteCode.value || undefined
      );
      // Exclure les modèles génériques stricto sensu (isGenerique === 1)
      const modeles = response.data?.data || response.data || [];
      availableModeles.value = modeles.filter(m => m.isGenerique === 0 || m.isGenerique === undefined);
    } catch (error) {
      console.error('Erreur lors du chargement des modèles filtrés:', error);
      availableModeles.value = [];
    } finally {
      isLoadingSources.value = false;
    }
  };

  // ============================================================================
  // ÉTAPE 3B: CHARGEMENT DES PLANS EXISTANTS (Pour clonage)
  // ============================================================================
  const chargerPlansFiltrés = async () => {
    if (isGenerique.value === 1) {
      availablePlans.value = [];
      return;
    }

    isLoadingSources.value = true;
    try {
      // Filtrer par Type, Nature et Opération pour proposer des plans pertinents à cloner
      const response = await fabPlanService.getPlansByFilters(
        typeRobinetCode.value,
        natureComposantCode.value,
        operationCode.value,
        posteCode.value || undefined
      );
      const allPlans = response.data?.data || response.data || [];

      // Récupérer la version de la structure PRC active (Role EN_COURS_DE_FABRICATION)
      const activePrcVersion = store.formulairesReferences?.find(r => String(r.statut || r.Statut || '').trim().toUpperCase() === 'ACTIF')?.version;

      availablePlans.value = allPlans.filter(p => {
        // Exclure les archives et les plans de ce même article (on ne se clone pas soi-même)
        if (p.statut === 'ARCHIVE') return false;
        if (p.codeArticleSage === codeArticleSage.value) return false;

        // FILTRE STRICT : On ne propose de cloner que les plans liés à la version active de la structure PRC
        if (activePrcVersion !== undefined) {
          const planVersion = p.version !== undefined ? p.version : p.Version;
          if (planVersion !== activePrcVersion) return false;
        }

        // Optionnel : filtrer par poste
        if (posteCode.value) {
          return p.posteCode === posteCode.value;
        }
        return true;
      });
    } catch (error) {
      console.error('Erreur lors du chargement des plans:', error);
      availablePlans.value = [];
    } finally {
      isLoadingSources.value = false;
    }
  };

  // 🔥 LE WATCHER MAGIQUE : Lance l'API *uniquement* quand l'opération est sélectionnée
  // Poste requis uniquement pour Auto avec Soupape
  const requiertPoste = computed(() => {
    const nat = String(natureComposantCode.value || '').trim().toUpperCase();
    const type = String(typeRobinetCode.value || '').trim().toUpperCase();

    // "je veux liste apparait que dans auto avec souape"
    return type === 'AUTO' && nat === 'SOUPAPE';
  });

  const postesDisponibles = computed(() =>
    (store.postes || [])
      .map(p => ({
        code: p.code || p.Code || p.codePoste || p.CodePoste,
        libelle: p.libelle || p.Libelle || p.designation || p.Designation
      }))
      .filter(p => p.code)
  );

  watch([operationCode, sourceType, codeArticleSage, posteCode], ([newOp, newSource, newCode, newPoste], [oldOp, oldSource, oldCode, oldPoste]) => {
    if (newOp !== oldOp || newSource !== oldSource || newPoste !== oldPoste) {
      selectedSourceId.value = null;
    }

    // Si poste requis, attendre qu'il soit saisi
    const posteOk = !requiertPoste.value || !!posteCode.value;
    if (newOp && posteOk && newSource === 'MODELE') {
      chargerModelesFiltrés();
    } else if (newOp && posteOk && newSource === 'CLONE') {
      chargerPlansFiltrés();
    }
  });

  // ============================================================================
  // WATCHERS POUR AUTO-SÉLECTION
  // ============================================================================

  // Auto-sélectionner l'opération s'il n'y en a qu'une
  watch(operationsFiltrees, (newOps) => {
    if (newOps.length === 1 && !operationCode.value) {
      operationCode.value = newOps[0].code;
    }
  });

  // Auto-sélectionner le modèle s'il n'y en a qu'un
  watch(availableModeles, (newModeles) => {
    if (newModeles.length === 1 && !selectedSourceId.value && sourceType.value === 'MODELE') {
      selectedSourceId.value = newModeles[0].id;
    }
  });

  // Auto-sélectionner le plan s'il n'y en a qu'un
  watch(availablePlans, (newPlans) => {
    if (newPlans.length === 1 && !selectedSourceId.value && sourceType.value === 'CLONE') {
      selectedSourceId.value = newPlans[0].id;
    }
  });

  // ============================================================================
  // GÉNÉRATION & VALIDATION
  // ============================================================================
  const genererPlan = async () => {
    isGenerating.value = true;
    try {
      let payload = {};

      if (sourceType.value === 'MODELE' || sourceType.value === 'VIERGE') {
        payload = {
          modeleSourceId: sourceType.value === 'MODELE' ? selectedSourceId.value : null,
          codeArticleSage: codeArticleSage.value,
          designation: designationArticle.value,
          operationCode: operationCode.value,
          natureComposantCode: natureComposantCode.value,
          posteCode: posteCode.value || null,
          familleCode: familleCode.value || null,
          refFormulaireCodeReference: refFormulaireCodeReference.value || 'PRC',
          nom: `PC-${codeArticleSage.value}${posteCode.value ? '-P' + posteCode.value : ''}`,
          creePar: 'ADMIN_QUALITE'
        };
        return await fabPlanService.instantiatePlan(payload);
      } else {
        payload = {
          planExistantId: selectedSourceId.value,
          nouveauCodeArticleSage: codeArticleSage.value,
          nouvelleDesignation: designationArticle.value,
          creePar: 'ADMIN_QUALITE'
        };
        return await fabPlanService.clonePlan(payload);
      }
    } catch (error) {
      toast.add({ severity: 'error', summary: 'Erreur', detail: error.message, life: 6000 });
      throw error;
    } finally {
      isGenerating.value = false;
    }
  };

  const canGeneratePlan = () => {
    if (isGenerique.value === 1) return false;
    if (isPlanCreationBlocked.value) return false;
    if (requiertPoste.value && !posteCode.value) return false;
    if (requiertFamille.value && !familleCode.value) return false;
    if (sourceType.value === 'VIERGE') {
      return isArticleValid.value && operationCode.value && !isGenerating.value;
    }
    return isArticleValid.value && operationCode.value && selectedSourceId.value !== null && !isGenerating.value;
  };

  const reset = () => {
    codeArticleSage.value = '';
    designationArticle.value = '';
    typeRobinetCode.value = '';
    natureComposantCode.value = '';
    operationCode.value = '';
    posteCode.value = '';
    familleCode.value = '';
    isArticleValid.value = false;
    isGenerique.value = 0;
    sourceType.value = 'MODELE';
    selectedSourceId.value = null;
    refFormulaireCodeReference.value = 'PRC';
    availableModeles.value = [];
    availablePlans.value = [];
  };

  return {
    codeArticleSage, designationArticle, typeRobinetCode, natureComposantCode, operationCode, posteCode,
    isArticleValid, isCheckingArticle, isGenerique, sourceType, selectedSourceId, refFormulaireCodeReference, isGenerating, isLoadingSources,
    availableModeles, availablePlans, operationsFiltrees, debugInfo,
    requiertPoste, postesDisponibles, requiertFamille, famillesFiltrees, familleCode, isPlanCreationBlocked, hasValidStructure,
    filteredArticles, isSearchingArticles, searchArticles,
    verifierArticleERP, genererPlan, canGeneratePlan, reset,
    getLibelleType, getLibelleNature, chargerPlansFiltrés
  };
}
