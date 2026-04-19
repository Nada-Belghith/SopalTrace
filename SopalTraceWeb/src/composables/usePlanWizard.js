import { ref, computed, watch } from 'vue';
import { useToast } from 'primevue/usetoast';
import { qualityPlansService } from '@/services/qualityPlansService';
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
  const codeArticleSage = ref('');
  const designationArticle = ref('');
  const typeRobinetCode = ref('');
  const natureComposantCode = ref('');
  const isArticleValid = ref(false);
  const isCheckingArticle = ref(false);
  
  // Utilisation stricte de 1 ou 0
  const isGenerique = ref(0);
  
  // 🔍 DEBUG : Affiche les infos de vérification
  const debugInfo = ref({
    natureLookup: null,
    isGeneriqueValue: 0,
    timestamp: null
  }); 
  
  // L'Opération choisie
  const operationCode = ref('');
  
  const sourceType = ref('MODELE');
  const selectedSourceId = ref(null);
  const isGenerating = ref(false);
  
  // Listes et état de chargement pour les sources
  const availableModeles = ref([]);
  const availablePlans = ref([]);
  const isLoadingSources = ref(false);

  // ============================================================================
  // ÉTAPE 1: VÉRIFICATION ERP
  // ============================================================================
  const verifierArticleERP = async () => {
    if (!codeArticleSage.value) return;
    
    isCheckingArticle.value = true;
    try {
      const response = await qualityPlansService.getArticleFromERP(codeArticleSage.value);
      const articleData = response.data || response;
      
      designationArticle.value = articleData.designation || '';
      typeRobinetCode.value = articleData.typeRobinetCode || '';
      natureComposantCode.value = articleData.natureComposantCode || '';
      isArticleValid.value = true;
      
      // 🔧 Récupérer estGenerique depuis la table naturesComposant du store (true = générique, false = pas générique)
      const nature = store.naturesComposant?.find(n => n.code === articleData.natureComposantCode);
      isGenerique.value = nature?.estGenerique === true ? 1 : 0;
      // 🔍 DEBUG : Stocker les infos pour l'inspecteur
      debugInfo.value = {
        natureLookup: nature,
        isGeneriqueValue: isGenerique.value,
        timestamp: new Date().toLocaleTimeString(),
        articleCode: articleData.natureComposantCode,
        allNatures: store.naturesComposant
      };      
      // On s'assure que les étapes suivantes sont remises à zéro
      operationCode.value = '';
      sourceType.value = 'MODELE';
      selectedSourceId.value = null;
      availableModeles.value = [];
      availablePlans.value = [];
      
      toast.add({
        severity: 'success',
        summary: 'Article identifié',
        detail: `${articleData.designation} - Veuillez choisir l'opération.`,
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
    
    if (!natureComposantCode.value) return ops;

    // Croisement BDD : Quelles opérations correspondent à cette Nature de composant ?
    const operationsPermises = gammes
      .filter(g => g.natureComposantCode === natureComposantCode.value)
      .map(g => g.operationCode);
    
    return ops.filter(op => operationsPermises.includes(op.code));
  });

  const getLibelleType = (code) => store.typesRobinet?.find(t => t.code === code)?.libelle || code;
  const getLibelleNature = (code) => store.naturesComposant?.find(n => n.code === code)?.libelle || code;

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
      const response = await qualityPlansService.getModelesByFilters(
        typeRobinetCode.value,
        natureComposantCode.value,
        operationCode.value
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
      // Pour permettre le clonage depuis n'importe quel article, récupérer tous les plans actifs
      const response = await qualityPlansService.getPlansByFilters();
      const plans = response.data?.data || response.data || [];
      availablePlans.value = plans.filter(p => p.statut === 'ACTIF');
    } catch (error) {
      console.error('Erreur lors du chargement des plans:', error);
      availablePlans.value = [];
    } finally {
      isLoadingSources.value = false;
    }
  };

  // 🔥 LE WATCHER MAGIQUE : Lance l'API *uniquement* quand l'opération est sélectionnée
  watch([operationCode, sourceType], ([newOp, newSource]) => {
    selectedSourceId.value = null; // Nettoie le dropdown
    if (newOp && newSource === 'MODELE') {
      chargerModelesFiltrés();
    } else if (newOp && newSource === 'CLONE') {
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

      if (sourceType.value === 'MODELE') {
        payload = {
          modeleSourceId: selectedSourceId.value,
          codeArticleSage: codeArticleSage.value,
          designation: designationArticle.value,
          nom: `PC-${codeArticleSage.value}-V1`,
          creePar: 'ADMIN_QUALITE'
        };
        return await qualityPlansService.instantiatePlan(payload);
      } else {
        payload = {
          planExistantId: selectedSourceId.value,
          nouveauCodeArticleSage: codeArticleSage.value,
          nouvelleDesignation: designationArticle.value,
          creePar: 'ADMIN_QUALITE'
        };
        return await qualityPlansService.clonePlan(payload);
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
    return isArticleValid.value && operationCode.value && selectedSourceId.value !== null && !isGenerating.value;
  };

  const reset = () => {
    codeArticleSage.value = '';
    designationArticle.value = '';
    typeRobinetCode.value = '';
    natureComposantCode.value = '';
    operationCode.value = '';
    isArticleValid.value = false;
    isGenerique.value = 0; // Remise à 0
    sourceType.value = 'MODELE';
    selectedSourceId.value = null;
    availableModeles.value = [];
    availablePlans.value = [];
  };

  return {
    codeArticleSage, designationArticle, typeRobinetCode, natureComposantCode, operationCode,
    isArticleValid, isCheckingArticle, isGenerique, sourceType, selectedSourceId, isGenerating, isLoadingSources,
    availableModeles, availablePlans, operationsFiltrees, debugInfo,
    verifierArticleERP, genererPlan, canGeneratePlan, reset,
    getLibelleType, getLibelleNature, chargerPlansFiltrés
  };
}
