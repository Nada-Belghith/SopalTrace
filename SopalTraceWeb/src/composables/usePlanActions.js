import { prepareSectionsForBackend } from '@/utils/sectionUtils';
import { planFabricationService as fabPlanService } from '@/services/planFabricationService';

export function usePlanActions(editorState, dependencies) {
  const {
    toast, router, store, usePlanMapper
  } = dependencies;

  const { buildServicePayload, syncDbIds } = usePlanMapper;

  const sauvegarderBrouillonSilencieux = async (showToast = false, force = false) => {
    if (!force && (
      editorState.isLoadingData.value || 
      editorState.isGeneratingPlan.value || 
      editorState.isCanceling.value || 
      editorState.isSaving.value || 
      editorState.plan.value?.statut === 'ACTIF' || 
      editorState.isArchived.value
    )) return;

    let currentPlanId = editorState.planId.value;

    try {
      if (currentPlanId === 'nouveau' && editorState.planCreationPayload.value) {
        if (editorState.versionInitiale.value !== null) {
          editorState.planCreationPayload.value.versionInitiale = editorState.versionInitiale.value;
        }
        if (editorState.plan.value?.codeArticleSage) {
          editorState.planCreationPayload.value.codeArticleSage = editorState.plan.value.codeArticleSage;
        }
        
        const sectionsPayload = buildServicePayload(editorState.sections, true);
        if (sectionsPayload && sectionsPayload.length > 0) {
          editorState.planCreationPayload.value.sections = sectionsPayload;
        }

        const instRes = await fabPlanService.instantiatePlan(editorState.planCreationPayload.value);
        const instData = instRes?.data?.data || instRes?.data || instRes;
        currentPlanId = instData.planId || instData.id;
        editorState.planId.value = currentPlanId;
        editorState.aEteCreePendantCetteSession.value = true;
        const newPlanRes = await fabPlanService.getPlanById(currentPlanId);
        const dataPlan = newPlanRes?.data?.data || newPlanRes?.data || newPlanRes;
        syncDbIds(dataPlan, editorState.plan, editorState.sections);
        editorState.planCreationPayload.value = null;
      }

      if (!currentPlanId || currentPlanId === 'nouveau') return;

      await prepareSectionsForBackend(editorState.sections.value, store.periodicites, async (payload) => {
        const res = await fabPlanService.createPeriodicite(payload);
        const resData = res?.data?.data || res?.data || res;
        store.periodicites.push({ id: resData.periodiciteId || resData.id, ...payload });
        return res;
      });

      const payload = buildServicePayload(editorState.sections, true);
      
      const payloadData = {
        sections: payload,
        colonneDefs: editorState.planConfigurationColonnes.value
      };
      
      let rawCode = editorState.plan.value?.codeArticleSage || editorState.wizard.codeArticleSage.value;
      let baseCode = typeof rawCode === 'object' && rawCode !== null ? rawCode.codeArticle : rawCode;
      let codeVersionne = editorState.codeArticleSuffix.value 
        ? `${baseCode}.${editorState.codeArticleSuffix.value}` 
        : (editorState.plan.value?.codeArticleSageVersionne || baseCode);

      await fabPlanService.updatePlanValues(
        currentPlanId, 
        payloadData, 
        editorState.legendeMoyens.value, 
        editorState.remarques.value, 
        false, 
        codeVersionne, 
        'Admin', 
        editorState.plan.value?.codeArticleSage, 
        codeVersionne
      );

      if (showToast) {
        toast.add({ severity: 'info', summary: 'Brouillon enregistré', detail: 'Vos données ont été sauvegardées.', life: 3000 });
      }
    } catch (error) {
      console.error("Auto-save failed.", error);
    }
  };

  const enregistrerValeurs = async (currentPlanId, redirectToHub = true, isActivating = false) => {
    try {
      await prepareSectionsForBackend(
        editorState.sections.value,
        store.periodicites || [],
        async (payloadFreq) => {
          const res = await fabPlanService.createPeriodicite(payloadFreq);
          const resData = res?.data?.data || res?.data || res;
          store.periodicites.push({ id: resData.periodiciteId || resData.id, ...payloadFreq });
          return res;
        }
      );

      const payload = buildServicePayload(editorState.sections, isActivating ? false : (editorState.plan.value?.statut === 'BROUILLON'));
      
      const payloadData = {
        sections: payload,
        colonneDefs: editorState.planConfigurationColonnes.value
      };

      let rawCode = editorState.plan.value?.codeArticleSage || editorState.wizard.codeArticleSage.value;
      let baseCode = typeof rawCode === 'object' && rawCode !== null ? rawCode.codeArticle : rawCode;
      let codeVersionne = editorState.codeArticleSuffix.value 
        ? `${baseCode}.${editorState.codeArticleSuffix.value}` 
        : (editorState.plan.value?.codeArticleSageVersionne || baseCode);
        
      await fabPlanService.updatePlanValues(
        currentPlanId, 
        payloadData, 
        editorState.legendeMoyens.value, 
        editorState.remarques.value, 
        isActivating, 
        codeVersionne, 
        'Admin', 
        editorState.plan.value?.codeArticleSage, 
        codeVersionne
      );

      if (isActivating) {
        toast.add({ severity: 'success', summary: 'Plan Activated', detail: 'The plan is now in production.', life: 4000 });
      } else {
        toast.add({ severity: 'info', summary: 'Data saved', detail: 'Your modifications have been saved.', life: 3000 });
      }

      if (redirectToHub) {
        editorState.isExitingEditor.value = true;
        router.push('/dev/hub-plans');
      } else {
        await dependencies.loadPlan(currentPlanId);
      }
    } catch (error) {
      console.error('Error saving:', error);
      toast.add({ severity: 'error', summary: 'Error', detail: 'An error occurred during saving.', life: 4000 });
      throw error;
    }
  };

  const declencherSauvegarde = async (isActivating = false) => {
    let currentPlanId = editorState.planId.value;

    if (currentPlanId === 'nouveau' && editorState.planCreationPayload.value) {
      try {
        if (editorState.versionInitiale.value !== null) {
          editorState.planCreationPayload.value.versionInitiale = editorState.versionInitiale.value;
        }
        if (editorState.plan.value?.codeArticleSage) {
          editorState.planCreationPayload.value.codeArticleSage = editorState.plan.value.codeArticleSage;
        }

        await prepareSectionsForBackend(
          editorState.sections.value,
          store.periodicites || [],
          async (payloadFreq) => {
            const res = await fabPlanService.createPeriodicite(payloadFreq);
            const resData = res?.data?.data || res?.data || res;
            store.periodicites.push({ id: resData.periodiciteId || resData.id, ...payloadFreq });
            return res;
          }
        );

        if (isActivating) {
          editorState.planCreationPayload.value.statut = 'ACTIF';
        }
        
        const finalNom = editorState.plan.value?.nom && !editorState.plan.value.nom.includes('Modèle') 
          ? editorState.plan.value.nom 
          : `Plan de contrôle en cours de fabrication ${editorState.plan.value?.designation || editorState.wizard.designationArticle.value}${editorState.plan.value?.posteCode || editorState.wizard.posteCode.value ? ' (' + (editorState.plan.value?.posteCode || editorState.wizard.posteCode.value) + ')' : ''}`;
        editorState.planCreationPayload.value.nom = finalNom;
        
        let rawCode = editorState.plan.value?.codeArticleSage || editorState.wizard.codeArticleSage.value;
        let baseCode = typeof rawCode === 'object' && rawCode !== null ? rawCode.codeArticle : rawCode;
        editorState.planCreationPayload.value.codeArticleSageVersionne = editorState.codeArticleSuffix.value 
          ? `${baseCode}.${editorState.codeArticleSuffix.value}` 
          : (editorState.plan.value?.codeArticleSageVersionne || baseCode);
        
        editorState.planCreationPayload.value.legendeMoyens = editorState.legendeMoyens.value;
        editorState.planCreationPayload.value.remarques = editorState.remarques.value;

        const sectionsPayload = buildServicePayload(editorState.sections, isActivating ? false : (editorState.plan.value?.statut === 'BROUILLON'));
        if (sectionsPayload && sectionsPayload.length > 0) {
          editorState.planCreationPayload.value.sections = sectionsPayload;
        }

        const instRes = await fabPlanService.instantiatePlan(editorState.planCreationPayload.value);
        const instData = instRes?.data?.data || instRes?.data || instRes;
        currentPlanId = instData.planId || instData.id;
        editorState.planId.value = currentPlanId;

        editorState.planCreationPayload.value = null;

        if (isActivating) {
          toast.add({ severity: 'success', summary: 'Plan activé', detail: 'Le plan a été créé et activé.', life: 4000 });
        } else {
          toast.add({ severity: 'info', summary: 'Brouillon créé', detail: 'Le brouillon a été créé avec succès.', life: 3000 });
        }

        editorState.isExitingEditor.value = true;
        router.push('/dev/hub-plans');
        return;
      } catch (err) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Cannot create the plan.', life: 6000 });
        throw err;
      }
    }

    await enregistrerValeurs(currentPlanId, true, isActivating);
  };

  const createNewVersionActive = async () => {
    try {
      if (!editorState.codeArticleSuffix.value || editorState.codeArticleSuffix.value.trim() === '' || editorState.codeArticleSuffix.value === '.') {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Article Code version (e.g., .X) is mandatory.', life: 4000 });
        return;
      }
      if (!editorState.validerSaisieValeurs()) return;
      if (!editorState.validerLegendeMoyens()) return;

      await prepareSectionsForBackend(
        editorState.sections.value,
        store.periodicites || [],
        async (payloadFreq) => {
          const res = await fabPlanService.createPeriodicite(payloadFreq);
          const resData = res?.data?.data || res?.data || res;
          store.periodicites.push({ id: resData.periodiciteId || resData.id, ...payloadFreq });
          return res;
        }
      );

      const payloadSections = buildServicePayload(editorState.sections, false);
      let rawCode = editorState.plan.value?.codeArticleSage || editorState.wizard.codeArticleSage.value;
      let baseCode = typeof rawCode === 'object' && rawCode !== null ? rawCode.codeArticle : rawCode;
      const codeVersionne = editorState.codeArticleSuffix.value 
        ? `${baseCode}.${editorState.codeArticleSuffix.value}` 
        : (editorState.plan.value?.codeArticleSageVersionne || baseCode);
      
      const reqPayload = {
        ancienId: editorState.planId.value,
        typeDocumentCode: editorState.plan.value.typeDocumentCode || 'FAB',
        nom: editorState.plan.value.nom,
        designation: editorState.plan.value.designation,
        natureArticleCode: editorState.plan.value.natureArticleCode,
        familleProduitFiniCode: editorState.plan.value.familleProduitFiniCode,
        operationCode: editorState.plan.value?.operationCode || editorState.wizard.operationCode.value,
        posteCode: editorState.plan.value?.posteCode || editorState.wizard.posteCode.value,
        legendeMoyens: editorState.legendeMoyens.value,
        remarques: editorState.remarques.value,
        libre1: editorState.plan.value.codeArticleSage,
        configurationColonnesJson: JSON.stringify(editorState.planConfigurationColonnes.value),
        refFormulaireCodeReference: editorState.plan.value.codeReferenceFormulaire,
        sections: payloadSections,
        codeArticleSageVersionne: codeVersionne,
        statut: 'ACTIF'
      };

      await fabPlanService.newPlanVersion(reqPayload);
      
      toast.add({ severity: 'success', summary: 'New Version', detail: 'The new version has been successfully created and activated.', life: 4000 });
      editorState.isExitingEditor.value = true;
      router.push('/dev/hub-plans');
    } catch (error) {
      console.error('Error new version:', error);
      toast.add({ severity: 'error', summary: 'Error', detail: 'Cannot create the new version.', life: 6000 });
      throw error;
    }
  };

  const mettreANiveauArchive = async () => {
    try {
      const res = await fabPlanService.upgradePlan(editorState.planId.value);
      const newId = res.data.planId;
      toast.add({ severity: 'success', summary: 'Upgrade successful', detail: 'The plan has been upgraded and is now the active version.', life: 4000 });
      router.push(`/dev/fab/plans/editer/${newId}`);
      setTimeout(() => window.location.reload(), 100);
    } catch (error) {
      console.error('Error upgrade:', error);
      toast.add({ severity: 'error', summary: 'Error', detail: error.response?.data?.message || 'Cannot upgrade the plan.', life: 6000 });
    }
  };

  return {
    sauvegarderBrouillonSilencieux,
    enregistrerValeurs,
    declencherSauvegarde,
    createNewVersionActive,
    mettreANiveauArchive
  };
}
