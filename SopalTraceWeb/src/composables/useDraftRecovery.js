import { ref } from 'vue';

export function useDraftRecovery(service, confirm, toast, router, basePath) {
  const isRecovering = ref(false);

  /**
   * Intercepte la création de plan pour vérifier l'existence d'un brouillon ou d'un plan actif.
   * @param {string} articleCode 
   * @param {string} familleCode 
   * @param {string} natureCode 
   * @param {string} operationCode 
   * @param {string} posteCode 
   * @param {Function} executeCreationCallback 
   * @param {Function} onCancelCallback (Optionnel)
   */
  const interceptCreation = async (
    articleCode,
    familleCode,
    natureCode,
    operationCode,
    posteCode,
    executeCreationCallback,
    onCancelCallback = () => {}
  ) => {
    isRecovering.value = true;
    try {
      const resVal = await service.verifyPlanState(
        articleCode,
        familleCode,
        natureCode,
        null,
        operationCode,
        posteCode
      );
      const etat = resVal.data;

      if (etat.hasBrouillon) {
        confirm.require({
          message: `Un brouillon de plan existe déjà pour l'article ${articleCode} / opération ${operationCode}. Que souhaitez-vous faire ?`,
          header: 'Brouillon Existant',
          acceptLabel: 'Récupérer le brouillon',
          rejectLabel: 'Supprimer & Créer un nouveau',
          acceptClass: 'p-button-warning',
          rejectClass: 'p-button-danger p-button-outlined',
          accept: () => {
            toast.add({ severity: 'info', summary: 'Brouillon récupéré', detail: 'Vous pouvez continuer la saisie de ce plan.', life: 4000 });
            router.replace(`${basePath}/${etat.brouillonId}`);
          },
          reject: async () => {
            try {
              await service.deletePlan(etat.brouillonId);
              toast.add({ severity: 'info', summary: 'Brouillon supprimé', detail: 'Vous pouvez maintenant générer un nouveau plan.', life: 3000 });
              
              await executeCreationCallback();
            } catch (err) {
              console.error('Erreur suppression brouillon:', err);
              toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de supprimer l\'ancien brouillon.', life: 3000 });
              onCancelCallback();
            }
          },
          onHide: () => onCancelCallback()
        });
      } else {
        await executeCreationCallback();
      }
    } catch (error) {
      console.error('Erreur lors de l\'interception de création:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de vérifier l\'état du plan.', life: 4000 });
      onCancelCallback();
    } finally {
      isRecovering.value = false;
    }
  };



  return {
    isRecovering,
    interceptCreation
  };
}
