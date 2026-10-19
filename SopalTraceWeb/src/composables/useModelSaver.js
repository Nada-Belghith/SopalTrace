import { periodiciteAvecLibelle } from '@/utils/frequencyUtils';
import { modeleFabricationService as fabModeleService } from '@/services/modeleFabricationService';
import { planFabricationService as fabPlanService } from '@/services/planFabricationService';
import { prepareSectionsForBackend } from '@/utils/sectionUtils';

export function useModelSaver(store, ctx) {
  const saveInitialModel = async (toast, router, confirmArchivagePlanActif, validerSaisieValeurs, validerLegendeMoyens) => {
    if (store.isLoading) return;
    if (!validerSaisieValeurs()) return;
    if (!validerLegendeMoyens()) return;

    store.isLoading = true;
    try {
      const resExist = await fabModeleService.getModelsByFilters(
        null,
        store.entete.natureComposantCode,
        store.entete.operationCode,
        store.entete.posteCode,
        store.entete.familleProduitCode
      );

      const activeModel = (Array.isArray(resExist) ? resExist : resExist?.data || []).find(m => m.statut === 'ACTIF');

      if (activeModel) {
        const confirmed = await confirmArchivagePlanActif({
          typeDocument: 'modèle',
          identifiant: 'cette combinaison',
          version: activeModel.version
        });
        if (!confirmed) {
          store.isLoading = false;
          return;
        }
      }

      await prepareSectionsForBackend(
        ctx.groupes.value,
        store.periodicites,
        async (payloadFreq) => {
          const res = await fabPlanService.createPeriodicite(payloadFreq);
          // res est déjà response.data (unwrappé par apiClient)
          // res = { success: true, data: "<guid>", id: "<guid>", periodiciteId: "<guid>" }
          const newId = res?.periodiciteId || res?.id || res?.data || res;
          store.periodicites.push(periodiciteAvecLibelle({ id: newId, ...payloadFreq }));
          return res;
        }
      );
      // Mise à jour directe du ref sections pour que mapPayload() lise les periodiciteIds corrects
      store.sections.splice(0, store.sections.length, ...ctx.groupes.value);
      await store.saveModele(store.entete.legendeMoyens);

      toast.add({ severity: 'success', summary: 'Succès', detail: `Modèle créé et activé !`, life: 3000 });
      setTimeout(() => router.push(ctx.returnUrl.value), 1500);
    } catch (error) {
      const errorMsg = error.response?.data?.message || error.message;
      toast.add({ severity: 'error', summary: 'Erreur', detail: errorMsg, life: 6000 });
    } finally {
      store.isLoading = false;
    }
  };

  const saveModelNewVersion = async (reason, toast, router) => {
    if (store.isLoading) return;
    store.isLoading = true;
    try {
      await prepareSectionsForBackend(
        ctx.groupes.value,
        store.periodicites,
        async (payloadFreq) => {
          const res = await fabPlanService.createPeriodicite(payloadFreq);
          const newId = res?.periodiciteId || res?.id || res?.data || res;
          store.periodicites.push(periodiciteAvecLibelle({ id: newId, ...payloadFreq }));
          return res;
        }
      );
      // Mise à jour directe du ref sections pour que mapPayload() lise les periodiciteIds corrects
      store.sections.splice(0, store.sections.length, ...ctx.groupes.value);

      await store.saveModele(store.entete.legendeMoyens);

      toast.add({ severity: 'success', summary: `Nouvelle version créée !`, detail: 'L\'ancienne version a été archivée.', life: 3000 });
      setTimeout(() => router.push(ctx.returnUrl.value), 1500);
    } catch (error) {
      const errorMsg = error.response?.data?.message || error.message;
      toast.add({ severity: 'error', summary: 'Erreur', detail: errorMsg, life: 6000 });
    } finally {
      store.isLoading = false;
    }
  };

  const onEditorSubmit = async (toast, router, validerSaisieValeurs, validerLegendeMoyens) => {
    if (store.isLoading) return;
    if (ctx.isArchived.value && !ctx.isArchiveEditing.value) {
      ctx.isArchiveEditing.value = true;

      store.syncConfigurationFromFormulaire();

      const newQuery = { ...ctx.route.query, draft: 'true' };
      delete newQuery.view;

      router.replace({ query: newQuery });
      toast.add({ severity: 'info', summary: 'Mode Édition Activé', detail: 'Modifiez la structure (mise à niveau avec le PRC actif), puis cliquez sur "Enregistrer la Nouvelle Version".', life: 5000 });
    } else if (ctx.isArchived.value || ctx.statut.value === 'ACTIF' || ctx.isUpgradeMode.value) {
      if (!validerSaisieValeurs()) return;
      if (!validerLegendeMoyens()) return;

      if (!ctx.isArchived.value && !ctx.isDirty.value) {
        toast.add({ severity: 'info', summary: 'Aucune modification', detail: 'Vous n\'avez effectué aucun changement sur la structure du modèle.', life: 4000 });
        return;
      }

      await saveModelNewVersion('Modification automatique', toast, router);
    } else {
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Les modèles ne gèrent pas de brouillons. Veuillez recréer le modèle.', life: 6000 });
    }
  };

  const onEditorSubmitClick = (toast, router, confirmArchivagePlanActif, validerSaisieValeurs, validerLegendeMoyens) => {
    if (!ctx.isEditMode.value) {
      saveInitialModel(toast, router, confirmArchivagePlanActif, validerSaisieValeurs, validerLegendeMoyens);
    } else {
      onEditorSubmit(toast, router, validerSaisieValeurs, validerLegendeMoyens);
    }
  };

  return {
    saveInitialModel,
    saveModelNewVersion,
    onEditorSubmit,
    onEditorSubmitClick
  };
}
