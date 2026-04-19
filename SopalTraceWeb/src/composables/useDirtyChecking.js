/**
 * Composable pour dirty checking avec snapshots
 * Détecte automatiquement les modifications et gère l'historique de changements
 * Réutilisable pour modèles et plans
 */

import { ref, computed } from 'vue';

export function useDirtyChecking() {
  const snapshotInitial = ref(null);
  const snapshotCurrent = ref(null);

  /**
   * Prend un snapshot de l'état actuel
   */
  const takeSnapshot = (data) => {
    const snapshot = JSON.stringify(data);
    return snapshot;
  };

  /**
   * Initialise le snapshot (état de base au chargement)
   */
  const initializeSnapshot = (data) => {
    snapshotInitial.value = takeSnapshot(data);
    snapshotCurrent.value = snapshotInitial.value;
  };

  /**
   * Met à jour le snapshot courant
   */
  const updateCurrentSnapshot = (data) => {
    snapshotCurrent.value = takeSnapshot(data);
  };

  /**
   * Vérifie si des modifications ont été effectuées
   */
  const isDirty = computed(() => {
    if (!snapshotInitial.value || !snapshotCurrent.value) return false;
    return snapshotInitial.value !== snapshotCurrent.value;
  });

  /**
   * Réinitialise à l'état initial (annulation des modifications)
   */
  const resetToInitial = () => {
    snapshotCurrent.value = snapshotInitial.value;
  };

  /**
   * Confirme les modifications (devient le nouvel état initial)
   */
  const confirmChanges = () => {
    snapshotInitial.value = snapshotCurrent.value;
  };

  return {
    snapshotInitial,
    snapshotCurrent,
    takeSnapshot,
    initializeSnapshot,
    updateCurrentSnapshot,
    isDirty,
    resetToInitial,
    confirmChanges
  };
}
