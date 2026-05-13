import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';

/**
 * Composable centralisé pour le cycle de vie complet (CRUD + Versionning + Brouillon) de tous les plans qualité.
 * 
 * Responsabilités:
 * - Gestion de l'état (isLoading, isSaving, isReadOnly, etc.)
 * - Gestion des toasts (notifications PrimeVue)
 * - Orchestration des appels API génériques (charger, sauver, activer, archiver)
 * - Gestion du routing
 * - Coordonnée avec les composables spécialisés (usePlanAutoSave, etc.)
 * 
 * Props attendues:
 * - planService: Service API du plan (pfPlanService, fabPlanService, etc.)
 * - planId: ID du plan (null si nouveau)
 * - isForcedView: Mode consultation (lecture seule)
 * 
 * Returns:
 * - État réactif (isLoading, isSaving, isReadOnly, etc.)
 * - Méthodes de cycle de vie (loadPlan, savePlan, activatePlan, archivePlan)
 * - État du formulaire (formData, errors)
 * - Utilitaires (showSuccessToast, showErrorToast)
 */
export function usePlanLifecycle(planService, planId, isForcedView = false) {
  const router = useRouter();
  const toast = useToast();

  // ==================== ÉTAT RÉACTIF ====================

  const isLoading = ref(false);
  const isSaving = ref(false);
  const isDirty = ref(false);
  const isReadOnly = computed(() => isForcedView || plan.value?.statut === 'ARCHIVE');

  const plan = ref(null);
  const errors = ref([]);
  const showVersioningDialog = ref(false);
  const versioningMode = ref('new-version'); // 'new-version' | 'restore'

  // ==================== WATCHERS ====================

  // Marquer comme modifié quand le plan change
  watch(
    () => plan.value,
    (newVal, oldVal) => {
      if (newVal && oldVal && JSON.stringify(newVal) !== JSON.stringify(oldVal)) {
        isDirty.value = true;
      }
    },
    { deep: true }
  );

  // ==================== CHARGEMENT DES DONNÉES ====================

  /**
   * Charge le plan depuis l'API.
   * @param {string} id - ID du plan à charger
   */
  const loadPlan = async (id) => {
    isLoading.value = true;
    try {
      const response = await planService.getPlan(id);
      plan.value = response.data;
      isDirty.value = false;
      errors.value = [];
    } catch (error) {
      showErrorToast('Erreur de chargement', error.response?.data?.message || 'Impossible de charger le plan');
      console.error('Erreur loadPlan:', error);
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Initialise un nouveau plan (brouillon vide).
   * @param {object} initialData - Données initiales du plan
   */
  const createNewPlan = (initialData = {}) => {
    plan.value = {
      statut: 'BROUILLON',
      version: 1,
      ...initialData
    };
    isDirty.value = true;
    errors.value = [];
  };

  // ==================== SAUVEGARDE ====================

  /**
   * Sauvegarde le brouillon courant (mise à jour ou création).
   * @param {object} payload - Données à sauvegarder
   * @param {string} user - Utilisateur effectuant la sauvegarde
   */
  const saveDraft = async (payload, user = 'CURRENT_USER') => {
    isSaving.value = true;
    try {
      let response;

      if (!plan.value.id) {
        // Nouveau plan: créer
        response = await planService.creerPlan(payload);
        plan.value.id = response.data;
        showSuccessToast('Succès', 'Plan créé');
      } else {
        // Mettre à jour le brouillon
        await planService.mettreAJourValeurs(plan.value.id, payload);
        showSuccessToast('Succès', 'Plan mis à jour');
      }

      isDirty.value = false;
      errors.value = [];
    } catch (error) {
      showErrorToast('Erreur de sauvegarde', error.response?.data?.message || 'Impossible de sauvegarder le plan');
      console.error('Erreur saveDraft:', error);
      throw error;
    } finally {
      isSaving.value = false;
    }
  };

  // ==================== ACTIVATION & VERSIONNING ====================

  /**
   * Affiche le dialog de versioning/activation.
   * @param {string} mode - 'new-version' ou 'restore'
   */
  const showVersioningConfirm = (mode = 'new-version') => {
    versioningMode.value = mode;
    showVersioningDialog.value = true;
  };

  /**
   * Active le plan brouillon (passage en ACTIF avec versionning).
   * @param {object} payload - Données supplémentaires (ex: motif, commentaire)
   */
  const activatePlan = async (payload = {}) => {
    isSaving.value = true;
    try {
      if (!plan.value?.id) {
        showErrorToast('Erreur', 'Le plan doit d\'abord être créé');
        return;
      }

      if (versioningMode.value === 'new-version') {
        // Créer une nouvelle version depuis le brouillon actif
        const response = await planService.creerNouvelleVersion(plan.value.id, {
          ...payload,
          sections: plan.value.sections // Inclure les sections
        });
        showSuccessToast('Succès', 'Nouvelle version activée');
        plan.value.id = response.data;
        plan.value.statut = 'ACTIF';
      } else if (versioningMode.value === 'restore') {
        // Restaurer un plan archivé
        const response = await planService.restaurerPlan({
          planArchiveId: plan.value.id,
          ...payload
        });
        showSuccessToast('Succès', 'Plan restauré et activé');
        plan.value.id = response.data;
        plan.value.statut = 'ACTIF';
      }

      isDirty.value = false;
      errors.value = [];
    } catch (error) {
      showErrorToast('Erreur d\'activation', error.response?.data?.message || 'Impossible d\'activer le plan');
      console.error('Erreur activatePlan:', error);
      throw error;
    } finally {
      isSaving.value = false;
      showVersioningDialog.value = false;
    }
  };

  // ==================== ARCHIVAGE ====================

  /**
   * Archive le plan courant.
   */
  const archivePlan = async () => {
    if (!plan.value?.id) {
      showErrorToast('Erreur', 'Impossible d\'archiver un plan sans ID');
      return;
    }

    isSaving.value = true;
    try {
      await planService.archiverPlan(plan.value.id);
      plan.value.statut = 'ARCHIVE';
      showSuccessToast('Succès', 'Plan archivé');
      isDirty.value = false;
    } catch (error) {
      showErrorToast('Erreur d\'archivage', error.response?.data?.message || 'Impossible d\'archiver le plan');
      console.error('Erreur archivePlan:', error);
      throw error;
    } finally {
      isSaving.value = false;
    }
  };

  // ==================== NAVIGATION ====================

  /**
   * Revient à la liste des plans.
   */
  const navigateToList = () => {
    router.push('/dev/hub');
  };

  /**
   * Navigue vers un plan spécifique.
   * @param {string} planId - ID du plan
   * @param {object} query - Paramètres de query (ex: view=true)
   */
  const navigateToPlan = (planId, query = {}) => {
    router.push({
      path: `/dev/hub/plans/${planId}`,
      query
    });
  };

  // ==================== NOTIFICATIONS ====================

  /**
   * Affiche un toast de succès.
   */
  const showSuccessToast = (title, message) => {
    toast.add({
      severity: 'success',
      summary: title,
      detail: message,
      life: 3000
    });
  };

  /**
   * Affiche un toast d'erreur.
   */
  const showErrorToast = (title, message) => {
    toast.add({
      severity: 'error',
      summary: title,
      detail: message,
      life: 4000
    });
  };

  /**
   * Affiche un toast d'avertissement.
   */
  const showWarningToast = (title, message) => {
    toast.add({
      severity: 'warn',
      summary: title,
      detail: message,
      life: 3000
    });
  };

  // ==================== VALIDATION ====================

  /**
   * Valide le formulaire courant.
   * À personnaliser par les enfants.
   */
  const validateForm = () => {
    errors.value = [];
    return errors.value.length === 0;
  };

  // ==================== ÉTAT CALCULÉ ====================

  const isPlanNew = computed(() => !plan.value?.id);
  const isPlanActive = computed(() => plan.value?.statut === 'ACTIF');
  const isPlanArchived = computed(() => plan.value?.statut === 'ARCHIVE');
  const isPlanDraft = computed(() => plan.value?.statut === 'BROUILLON');

  // ==================== RETURN ====================

  return {
    // État
    isLoading,
    isSaving,
    isDirty,
    isReadOnly,
    plan,
    errors,
    showVersioningDialog,
    versioningMode,

    // État calculé
    isPlanNew,
    isPlanActive,
    isPlanArchived,
    isPlanDraft,

    // Méthodes de cycle de vie
    loadPlan,
    createNewPlan,
    saveDraft,
    activatePlan,
    archivePlan,
    showVersioningConfirm,

    // Navigation
    navigateToList,
    navigateToPlan,

    // Notifications
    showSuccessToast,
    showErrorToast,
    showWarningToast,

    // Validation
    validateForm
  };
}
