import { ref } from 'vue';

export function usePlanAutosave(autoSaveCallback, intervalMs = 30000) {
  const isSaving = ref(false);
  const autoSaveInterval = ref(null);

  const startAutoSave = () => {
    if (autoSaveInterval.value) {
      clearInterval(autoSaveInterval.value);
    }
    autoSaveInterval.value = setInterval(async () => {
      if (isSaving.value) return; // Évite les sauvegardes concurrentes
      isSaving.value = true;
      try {
        await autoSaveCallback();
      } catch (e) {
        console.error('Erreur auto-save en arrière-plan:', e);
      } finally {
        isSaving.value = false;
      }
    }, intervalMs);
  };

  const stopAutoSave = () => {
    if (autoSaveInterval.value) {
      clearInterval(autoSaveInterval.value);
      autoSaveInterval.value = null;
    }
  };

  return {
    isSaving,
    startAutoSave,
    stopAutoSave
  };
}
