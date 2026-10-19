export function useDocumentSaveManager({ callbacks, toast, router, returnUrl = '/dev/hub' }) {
  const { onSaveDirect, onSaveCorrection, onSaveNewVersion, validateForm, preSaveHook } = callbacks;

  const showToast = (severity, summary, detail, life) => {
    if (typeof toast.add === 'function') {
      toast.add({ severity, summary, detail, life });
    } else {
      // Fallback for useAppToast
      if (severity === 'success' && typeof toast.success === 'function') toast.success(detail, summary);
      else if (severity === 'error' && typeof toast.error === 'function') toast.error(detail, summary);
      else if (severity === 'info' && typeof toast.info === 'function') toast.info(detail, summary);
      else if (severity === 'warn' && typeof toast.warn === 'function') toast.warn(detail, summary);
    }
  };

  const executeAction = async (actionFn, successMessage, ...args) => {
    if (validateForm) {
      const isValid = await validateForm();
      if (!isValid) return false;
    }
    
    if (preSaveHook) {
      const canProceed = await preSaveHook();
      if (!canProceed) return false;
    }

    try {
      const res = await actionFn(...args);
      // Some stores return { success, message, noChanges }, some just throw
      if (res && res.success === false) {
        showToast('error', 'Erreur', res.message || 'Erreur lors de l\'enregistrement', 4000);
        return false;
      }
      if (res && res.noChanges) {
        showToast('info', 'Info', 'Aucune modification détectée.', 3000);
        return true;
      }

      showToast('success', 'Succès', successMessage, 3000);
      if (returnUrl) {
        router.push(returnUrl);
      }
      return res || true;
    } catch (err) {
      const errorMsg = err.response?.data?.message || err.message || 'Échec de l\'opération.';
      showToast('error', 'Erreur', errorMsg, 4000);
      return false;
    }
  };

  const handleSaveDirect = async () => {
    await executeAction(onSaveDirect, 'Document enregistré avec succès.');
  };

  const handleSaveCorrection = async () => {
    await executeAction(onSaveCorrection, 'Correction enregistrée avec succès.');
  };

  const handleSaveNewVersion = async (motif) => {
    await executeAction(onSaveNewVersion, 'Nouvelle version créée avec succès.', motif);
  };

  return {
    handleSaveDirect,
    handleSaveCorrection,
    handleSaveNewVersion
  };
}
