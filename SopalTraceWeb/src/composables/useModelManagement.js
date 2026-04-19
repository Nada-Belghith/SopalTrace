/**
 * Composable pour gestion des modèles QualityPlans
 * Centralize la logique de création, édition, versioning et restauration
 * Réutilisable pour tous les types de modèles (Fabrication, Échantillonnage, PF, VerifMachine...)
 */

import { ref, computed } from 'vue';
import { qualityPlansService } from '@/services/qualityPlansService';
import { mapBackendModeleToEditor, prepareModelePayload } from '@/utils/modelMapper';
import { useModeleVersioning } from './useVersioning';

export function useModelManagement(modelType = 'fabrication') {
  const isLoading = ref(false);
  const isEditing = ref(false);
  const modeleId = ref(null);
  const modeleData = ref(null);
  const codeOriginal = ref('');
  const statut = ref('BROUILLON');
  const version = ref(1);

  const { creerNouvelleVersionModele, restaurerModele } = useModeleVersioning();

  const isEditMode = computed(() => !!modeleId.value);
  const isArchived = computed(() => statut.value === 'ARCHIVE');

  /**
   * Charge un modèle depuis le backend
   */
  const loadModele = async (id) => {
    isLoading.value = true;
    try {
      const response = await qualityPlansService.getModeleById(id);
      const data = response.data.data || response.data;
      
      modeleId.value = data.id;
      codeOriginal.value = data.code;
      statut.value = data.statut;
      version.value = data.version;
      modeleData.value = mapBackendModeleToEditor(data);
      
      return modeleData.value;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Crée un nouveau modèle
   */
  const createModele = async (entete, sections) => {
    isLoading.value = true;
    try {
      const payload = prepareModelePayload(entete, sections);
      const response = await qualityPlansService.createModele(payload);
      return response.data.modeleId;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Crée une nouvelle version du modèle
   */
  const createNewVersion = async (modeleId, modifiePar, motif) => {
    isLoading.value = true;
    try {
      const response = await creerNouvelleVersionModele({
        ancienId: modeleId,
        modifiePar: modifiePar || 'ADMIN_QUALITE',
        motifModification: motif || 'Mise à jour directe'
      });
      return response.data.modeleId;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Restaure un modèle archivé
   */
  const restoreModele = async (modeleId, restaurePar, motif) => {
    isLoading.value = true;
    try {
      const response = await restaurerModele({
        modeleArchiveId: modeleId,
        restaurePar: restaurePar || 'ADMIN_QUALITE',
        motifRestoration: motif || 'Restauration d\'archive'
      });
      return response.data.modeleId;
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Réinitialise l'état pour création d'un nouveau modèle
   */
  const resetForNewModele = () => {
    modeleId.value = null;
    codeOriginal.value = '';
    statut.value = 'BROUILLON';
    version.value = 1;
    modeleData.value = null;
    isEditing.value = false;
  };

  return {
    // State
    isLoading,
    isEditing,
    modeleId,
    modeleData,
    codeOriginal,
    statut,
    version,
    
    // Computed
    isEditMode,
    isArchived,
    
    // Methods
    loadModele,
    createModele,
    createNewVersion,
    restoreModele,
    resetForNewModele
  };
}
