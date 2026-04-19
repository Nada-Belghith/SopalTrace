/**
 * Composable pour gestion des plans QualityPlans
 * Centralizes la logique d'instanciation, édition, versioning et clone
 * Réutilisable pour tous les types de plans
 */

import { ref, computed } from 'vue';
import { qualityPlansService } from '@/services/qualityPlansService';
import { mapBackendPlanToEditor, preparePlanValuesPayload, prepareInstantiatePayload, prepareClonePlanPayload } from '@/utils/planMapper';
import { usePlanVersioning } from './useVersioning';

export function usePlanManagement() {
  const isLoading = ref(false);
  const isCreating = ref(false);
  const planId = ref(null);
  const planData = ref(null);
  const statut = ref('BROUILLON');
  const version = ref(1);
  const codeArticleSage = ref('');
  const designation = ref('');

  const { creerNouvelleVersionPlan, mettreAJourValeurs } = usePlanVersioning();

  const isEditMode = computed(() => !!planId.value);

  /**
   * Charge un plan depuis le backend
   */
  const loadPlan = async (id) => {
    isLoading.value = true;
    try {
      const response = await qualityPlansService.getPlanById(id);
      const data = response.data.data || response.data;
      
      planId.value = data.id;
      statut.value = data.statut;
      version.value = data.version;
      codeArticleSage.value = data.codeArticleSage;
      designation.value = data.designation;
      planData.value = mapBackendPlanToEditor(data);
      
      return planData.value;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Instancie un plan depuis un modèle
   */
  const instantiateFromModele = async (modeleSourceId, codeArticleSage, designation) => {
    isLoading.value = true;
    try {
      const payload = prepareInstantiatePayload(modeleSourceId, codeArticleSage, designation);
      const response = await qualityPlansService.instantiatePlan(payload);
      return response.data.planId;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Clone un plan existant pour un nouvel article
   */
  const cloneExistingPlan = async (planSourceId, codeArticleSage, designation) => {
    isLoading.value = true;
    try {
      const payload = prepareClonePlanPayload(planSourceId, codeArticleSage, designation);
      const response = await qualityPlansService.clonePlan(payload);
      return response.data.planId;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Met à jour les valeurs/tolérances du plan (activation du plan)
   */
  const savePlanValues = async (planId, sections) => {
    isLoading.value = true;
    try {
      const payload = preparePlanValuesPayload(sections);
      await mettreAJourValeurs(planId, payload);
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Crée une nouvelle version du plan (archivage + création brouillon)
   */
  const createNewVersion = async (ancienPlanId, motifModification) => {
    isLoading.value = true;
    try {
      const response = await creerNouvelleVersionPlan({
        ancienId: ancienPlanId,
        modifiePar: 'ADMIN',
        motifModification: motifModification || 'Mise à jour du plan'
      });
      return response.data.planId;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Réinitialise l'état pour création d'un nouveau plan
   */
  const resetForNewPlan = () => {
    planId.value = null;
    planData.value = null;
    statut.value = 'BROUILLON';
    version.value = 1;
    codeArticleSage.value = '';
    designation.value = '';
    isCreating.value = false;
  };

  /**
   * Marque le plan comme en cours d'édition
   */
  const startEditing = () => {
    isCreating.value = true;
  };

  return {
    // State
    isLoading,
    isCreating,
    planId,
    planData,
    statut,
    version,
    codeArticleSage,
    designation,
    
    // Computed
    isEditMode,
    
    // Methods
    loadPlan,
    instantiateFromModele,
    cloneExistingPlan,
    savePlanValues,
    createNewVersion,
    resetForNewPlan,
    startEditing
  };
}
