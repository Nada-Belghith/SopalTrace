<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <ConfirmDialog />

    <div class="max-w-[1600px] mx-auto">
      <div class="animate-in fade-in zoom-in-95 duration-500">

        <PlanHeader 
          :id="planId"
          :title="headerTitle"
          :subtitle="headerSubtitle"
          icon="pi pi-file-edit"
          iconColorClass="text-blue-500"
          :is-read-only="isReadOnly"
          :statut="plan?.statut"
          :is-restoring="isVersioningSaving"
          @restaurer="onEditorSubmit"
        >
          <template #actions>
            <div v-if="isEditMode" class="flex items-center bg-slate-50 rounded-lg border border-slate-200 ml-4 hidden md:flex overflow-hidden">
              <span class="px-3 py-1.5 text-[10px] font-black text-slate-400 uppercase bg-slate-100 border-r border-slate-200 flex items-center h-full">Code Article:</span>
              <div v-if="!isReadOnly && plan" class="flex items-center">
                <span class="font-mono font-bold text-sm text-slate-700 pl-2 py-1.5">{{ codeArticleBase }}.</span>
                <InputText v-model="codeArticleSuffix" :class="['font-mono font-bold text-sm text-blue-700 p-1 w-16 h-8 mx-1 bg-white focus:border-blue-500 rounded', !codeArticleSuffix ? 'border-2 border-red-500' : 'border border-slate-300']" placeholder="X" />
              </div>
              <span v-else class="font-mono font-bold text-sm text-slate-700 px-3 py-1.5">{{ codeAffiche }}</span>
            </div>
            <div v-if="isEditMode" class="flex items-center gap-2 bg-slate-50 px-3 py-1.5 rounded-lg border border-slate-200 ml-2 hidden md:flex">
              <span class="text-[10px] font-black text-slate-400 uppercase">Opération:</span>
              <span class="font-bold text-sm text-slate-700">{{ plan?.operationCode || wizard.operationCode.value || 'NON DÉFINIE' }}</span>
            </div>
          </template>
        </PlanHeader>

        <PlanWizardStep v-if="!isEditMode"
                        :wizard="wizard"
                        @load-model="onWizardGenerate"
                        @excel-selected="onExcelSelected" />

        <div v-else class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
          <div class="bg-[#1e293b] text-white px-5 py-4 flex justify-between items-center">
            <div class="flex items-center gap-3 font-bold tracking-wide text-sm">
              <i :class="isReadOnly ? 'pi pi-eye text-blue-400' : 'pi pi-sliders-v text-blue-400'"></i>
              {{ isReadOnly ? 'Visualisation du plan' : 'Éditeur de Structure du Plan' }}
            </div>
          </div>

          <div v-if="isLoadingData" class="py-20 text-center text-blue-500">
            <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
            <p class="text-xs font-black uppercase tracking-widest">Chargement de l'arbre...</p>
          </div>

          <div v-else class="p-6 md:p-8">
            <!-- Removed Configuration du plan pour versionInitiale -->

            <div class="mb-4">
              <h1 class="text-xl font-extrabold text-slate-800 flex items-center gap-3">
                <i class="pi pi-file-edit text-blue-600 text-2xl"></i>
                Édition : Plan de Fabrication
              </h1>
            </div>

            <template v-if="!hasValidStructure">
              <div class="p-8 text-center bg-amber-50 rounded-lg border border-amber-200 mb-6 flex flex-col items-center justify-center">
                <i class="pi pi-file-excel text-amber-500 text-4xl mb-3"></i>
                <h4 class="text-sm font-bold text-amber-800 mb-1">Structure PRC non définie</h4>
                <p class="text-sm text-amber-700 max-w-lg">Le formulaire sélectionné est à l'état de brouillon. Le Superviseur Qualité doit définir la structure du plan avant que vous puissiez créer des modèles ou des plans par article.</p>
              </div>
            </template>

            <template v-else>
              <template v-if="sections.length === 0">
                <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
                  Cliquez sur "Créer une nouvelle section" pour commencer.
                </div>
              </template>

              <!-- Mode LECTURE -->
              <div v-if="isReadOnly" class="p-4 md:p-6">
              <PlanReadView
                :sections="sections"
                :remarques="remarques"
                :legende-moyens="legendeMoyens"
                :configuration-colonnes="planConfigurationColonnes"
                :types-section="store.typesSection || []"
                :types-caracteristique="store.typesCaracteristique || []"
                :types-controle="store.typesControle || []"
                :moyens-controle="store.moyensControle || []"
                :periodicites="store.periodicites || []"
              />
              </div>

              <!-- Mode EDITION -->
              <div v-else class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
                <table class="w-full text-left border-collapse min-w-[1200px]">
                  <FabTableHeader :columns="planColumns" />
                  
                  <SharedSectionCard 
                    v-for="(section, index) in sections"
                    :key="section.id"
                    :groupe="section"
                    :index="index"
                    :is-read-only="isReadOnly"
                    :typesSection="store.typesSection"
                    :periodicites="store.periodicites"
                    :reglesEchantillonnage="store.reglesEchantillonnage"
                    :operationCode="plan?.operationCode || wizard.operationCode.value"
                    defaultTitle="Caractéristiques à contrôler"
                    @remove="supprimerSection(section.id)"
                    @update-groupe="(updatedSection) => mettreAJourSection(index, updatedSection)"
                  >
                    <FabPlanLigneControl
                      v-for="ligne in section.lignes"
                      :key="ligne.id"
                      :ligne="ligne"
                      :section="section"
                      :columns="planColumns"
                      :is-archived="isReadOnly"
                      :operation-code="plan?.operationCode || wizard.operationCode.value"
                      @remove="(ligneId) => supprimerLigneASection(index, ligneId)"
                      @update="(updated) => {
                        const idx = section.lignes.findIndex(l => l.id === updated.id);
                        if (idx !== -1) {
                           let updatedSection = JSON.parse(JSON.stringify(section));
                           updatedSection.lignes.splice(idx, 1, updated);
                           mettreAJourSection(index, updatedSection);
                        }
                      }"
                    />
                  </SharedSectionCard>
                </table>
              </div>

              <div class="mt-2" v-if="!isReadOnly">
                <button @click="ajouterSection" class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
                  <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
                </button>
              </div>

              <div v-if="!isReadOnly">
                <RemarquesLegendeBox 
                  v-model:remarques="remarques"
                  v-model:legendeMoyens="legendeMoyens"
                  :show-validation="showLegendValidation"
                  :has-custom-instruments="hasCustomInstrumentsGlobal"
                  :is-read-only="isReadOnly"
                />
              </div>
            </template>
          </div>

          <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end">
            <!-- Mode CRÉATION ou BROUILLON : 3 boutons (Annuler, Brouillon, Activer) -->
            <template v-if="(planId === 'nouveau' || plan?.statut === 'BROUILLON') && !isForcedView">
              <div class="flex gap-4">
                <button @click="onEditorCancel" class="px-5 py-2.5 text-slate-600 bg-white border border-slate-300 rounded-lg font-bold hover:bg-slate-50 transition-colors">
                  Annuler
                </button>
                <button @click="onSaveDraft"
                        :disabled="isSaving || isVersioningSaving"
                        class="px-5 py-2.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 shadow-sm font-bold">
                  <i v-if="isSaving && !isVersioningSaving" class="pi pi-spin pi-spinner"></i>
                  <i v-else class="pi pi-save"></i>
                  Enregistrer Brouillon
                </button>
                <button @click="onActivatePlan"
                        :disabled="isSaving || isVersioningSaving"
                        class="px-5 py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 shadow-sm font-bold">
                  <i v-if="isSaving && !isVersioningSaving" class="pi pi-spin pi-spinner"></i>
                  <i v-else class="pi pi-check-circle"></i>
                  Enregistrer & Activer le Plan
                </button>
              </div>
            </template>
            <!-- Mode ARCHIVE ou MISE À JOUR ACTIF : Bouton unique via EditorActions (Clonage/Version) -->
            <template v-else-if="!isForcedView">
              <EditorActions :label="editorLabel"
                             loading-label="Traitement..."
                             :icon="editorIcon"
                             :variant="editorVariant"
                             :is-loading="isSaving"
                             @submit="onEditorSubmit"
                             @cancel="onEditorCancel" />
            </template>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted, watch, computed, onUnmounted } from 'vue';
  import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router';
  import { useToast } from 'primevue/usetoast';
  import { useConfirm } from 'primevue/useconfirm';

  import { planFabricationService as fabPlanService } from '@/services/planFabricationService';
  import { modeleFabricationService as fabModeleService } from '@/services/modeleFabricationService';
  import { usePlanWizard } from '@/composables/usePlanWizard';
  import { useFabModeleStore } from '@/stores/fabModeleStore';


  import PlanWizardStep from '@/components/QualityPlans/PlanWizardStep.vue';
  import PlanReadView from '@/components/Shared/PlanReadView.vue';
  import FabTableHeader from '@/components/Fabrication/FabTableHeader.vue';
  import SharedSectionCard from '@/components/Shared/SharedSectionCard.vue';
  import FabPlanLigneControl from '@/components/Fabrication/FabPlanLigneControl.vue';
  import EditorActions from '@/components/Shared/EditorActions.vue';
  import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
  import ConfirmDialog from 'primevue/confirmdialog';
  import PlanHeader from '@/components/Shared/PlanHeader.vue';
  import InputText from 'primevue/inputtext';

  import { useEditorSections } from '@/composables/useEditorSections';
  import { useEditorValidation } from '@/composables/useEditorValidation';
  import { usePlanAutosave } from '@/composables/usePlanAutosave';
  import { useActivePlanConfirmation } from '@/composables/useActivePlanConfirmation';
  
  import { usePlanMapper } from '@/composables/usePlanMapper';
  import { usePlanActions } from '@/composables/usePlanActions';
  import { useDraftRecovery } from '@/composables/useDraftRecovery';

  const route = useRoute();
  const router = useRouter();
  const toast = useToast();
  const confirm = useConfirm();
  const { confirmArchivagePlanActif } = useActivePlanConfirmation();
  const { interceptCreation } = useDraftRecovery(fabPlanService, confirm, toast, router, '/dev/fab/plans/editer');
  const store = useFabModeleStore();

  const wizard = usePlanWizard();
  const isGeneratingPlan = ref(false);

  const isFromWizard = ref(false);
  const planId = ref(route.params.id === 'nouveau' ? null : route.params.id);
  const isForcedView = ref(route.query.view === 'true');
  const plan = ref(null);
  const legendeMoyens = ref('');
  const remarques = ref('');
  const versionInitiale = ref(null);
  const isLoadingData = ref(false);
  const isVersioningSaving = ref(false);
  
  const isExitingEditor = ref(false);
  const isCanceling = ref(false);
  const planCreationPayload = ref(null);
  const aEteCreePendantCetteSession = ref(false);
  const codeArticleSuffix = ref('');

  const {
    sections,
    ajouterSection,
    supprimerSection,
    mettreAJourSection,
    supprimerLigneASection
  } = useEditorSections();

  const {
    showLegendValidation,
    hasCustomInstrumentsGlobal,
    validerLegendeMoyens,
    validerSaisieValeurs
  } = useEditorValidation(sections, legendeMoyens, toast);

  const planMapper = usePlanMapper(store);
  const editorState = {
    toast, router, store, planId, plan, planCreationPayload, versionInitiale,
    wizard, codeArticleSuffix, legendeMoyens, remarques,
    sections, isExitingEditor, aEteCreePendantCetteSession,
    isLoadingData, isGeneratingPlan, isCanceling,
    validerLegendeMoyens, validerSaisieValeurs,
    get isSaving() { return isSaving; },
    get isArchived() { return isArchived; },
    get planConfigurationColonnes() { return planConfigurationColonnes; }
  };

  const planActions = usePlanActions(editorState, { toast, router, store, usePlanMapper: planMapper, loadPlan });

  const isEditMode = computed(() => !!planId.value);
  const isArchived = computed(() => plan.value?.statut === 'ARCHIVE');
  const isReadOnly = computed(() => isForcedView.value || isArchived.value);

  const headerTitle = computed(() => {
    if (isForcedView.value) return 'Visualisation';
    if (isArchived.value) return 'Mettre à jour ce Plan de Fabrication';
    return 'Plan de Fabrication';
  });

  const headerSubtitle = computed(() => {
    if (isForcedView.value) return 'Mode lecture seule (Aperçu de la structure).';
    if (isFromWizard.value) return "Configurez la structure du plan de fabrication.";
    if (plan.value && plan.value.statut === 'BROUILLON' && (plan.value.version || 0) <= 1) {
      return "Configurez la structure du plan de fabrication.";
    }
    if (!isEditMode.value) return "Configurez la structure du plan de Fabrication.";
    return isArchived.value 
      ? 'Vous consultez une archive. Mettre à jour créera une nouvelle version en brouillon.'
      : 'Modifiez les valeurs. L\'ancienne version sera archivée automatiquement.';
  });

  const codeAffiche = computed(() => plan.value?.codeArticleSageVersionne || plan.value?.nom || plan.value?.codeArticleSage || wizard.codeArticleSage.value || '');

  const codeArticleBase = computed(() => {
    let code = plan.value?.codeArticleSageVersionne || wizard.codeArticleSage.value || plan.value?.codeArticleSage || '';
    code = code.replace(/\.\./g, '.'); // Nettoyer d'éventuels doubles points
    const match = code.match(/^(.*?)(\.\w+)?$/);
    let base = match ? match[1] : code;
    if (base.endsWith('.')) {
      base = base.substring(0, base.length - 1);
    }
    return base;
  });

  const planColumns = computed(() => store.tableColumns || []);

  const planConfigurationColonnes = computed(() => {
    if (store.effectiveConfigurationColonnes?.length) return store.effectiveConfigurationColonnes;
    if (plan.value?.colonneDefs) {
      return plan.value.colonneDefs;
    }
    return [];
  });

  const hasValidStructure = computed(() => {
    if (plan.value?.colonneDefs && plan.value?.colonneDefs.length > 0) return true;
    const refs = store.formulairesReferences || [];
    if (refs.length === 0) return false;
    return refs.some(r => {
      const s = String(r.statut || r.Statut || '').trim().toUpperCase();
      return s === 'ACTIF';
    });
  });

  const syncPlanFormulaireConfig = (codeRef = null, fromExistingData = null) => {
    const code = codeRef || wizard.refFormulaireCodeReference?.value || plan.value?.codeReferenceFormulaire || 'PRC';
    
    if (fromExistingData) {
      let configParsed = null;

      if (fromExistingData.colonneDefs && fromExistingData.colonneDefs.length > 0) {
        configParsed = fromExistingData.colonneDefs;
      } else if (fromExistingData.configurationColonnesJson) {
        try {
          configParsed = JSON.parse(fromExistingData.configurationColonnesJson);
        } catch (e) {
          console.error('Erreur parsing configurationColonnesJson:', e);
        }
      }

      if (configParsed && configParsed.length > 0) {
        store.entete.configurationColonnes = configParsed;
      } else {
        store.applyFormulaireConfiguration(code);
      }
      store.entete.refFormulaireCodeReference = code;
    } else {
      store.applyFormulaireConfiguration(code);
    }

    if (plan.value) {
      plan.value.colonneDefs = store.effectiveConfigurationColonnes;
      plan.value.codeReferenceFormulaire = code;
    }
  };

  const editorLabel = computed(() => {
    if (!isEditMode.value) return 'Générer le Plan';
    if (isArchived.value) return 'Mettre à jour ce Plan';
    if (plan.value?.statut === 'BROUILLON' && (plan.value?.version || 0) <= 1) return 'Enregistrer & Activer le Plan';
    if (plan.value?.statut === 'ACTIF') return 'Créer une Nouvelle Version';
    return 'Enregistrer & Activer le Plan';
  });

  const editorIcon = computed(() => {
    if (!isEditMode.value) return 'pi pi-check';
    if (isArchived.value) return 'pi pi-sync';
    if (plan.value?.statut === 'BROUILLON' && (plan.value?.version || 0) <= 1) return 'pi pi-save';
    return plan.value?.statut === 'ACTIF' ? 'pi pi-history' : 'pi pi-save';
  });

  const editorVariant = computed(() => {
    if (!isEditMode.value) return 'success';
    if (isArchived.value) return 'warning';
    if (plan.value?.statut === 'BROUILLON' && (plan.value?.version || 0) <= 1) return 'success';
    if (plan.value?.statut === 'ACTIF') return 'warning';
    return 'success';
  });

  const onEditorCancel = () => {
    if (plan.value?.statut !== 'BROUILLON') {
      isExitingEditor.value = true;
      router.push('/dev/hub-plans');
      return;
    }

    confirm.require({
      message: 'Êtes-vous sûr de vouloir abandonner ce travail ? Ce brouillon et toutes ses données seront DÉFINITIVEMENT supprimés.',
      header: 'Supprimer le Brouillon',
      icon: 'pi pi-trash text-red-500',
      acceptLabel: 'Oui, Supprimer',
      rejectLabel: 'Annuler',
      acceptClass: 'p-button-danger',
      accept: async () => {
        isCanceling.value = true;
        isExitingEditor.value = true;

        try {
          if (planId.value && planId.value !== 'nouveau') {
            await fabPlanService.deletePlan(planId.value);
            toast.add({ severity: 'success', summary: 'Brouillon effacé', detail: 'La base de données a été nettoyée avec succès.', life: 4000 });
          }
        } catch (error) {
          console.error("Erreur lors de la suppression du brouillon", error);
          toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de supprimer le brouillon.', life: 4000 });
        } finally {
          router.push('/dev/hub-plans');
        }
      }
    });
  };

  onBeforeRouteLeave(async () => {
    if (isCanceling.value || isExitingEditor.value) {
      return true;
    }
    await planActions.sauvegarderBrouillonSilencieux(true);
    return true;
  });

  const { isSaving, startAutoSave, stopAutoSave } = usePlanAutosave(async () => {
    if (plan.value?.statut === 'BROUILLON' || planCreationPayload.value) {
      await planActions.sauvegarderBrouillonSilencieux(false);
    }
  }, 30000);

  onMounted(async () => {
    if (!store.isDicosLoaded) await store.fetchDictionnaires();
    if (!store.formulairesReferences?.length) {
      await store.fetchFormulairesReferences('EN_COURS_DE_FABRICATION');
    }
    if (!wizard.refFormulaireCodeReference?.value) {
      wizard.refFormulaireCodeReference.value = 'PRC';
    }
    syncPlanFormulaireConfig();
    if (planId.value && planId.value !== 'nouveau') await loadPlan(planId.value);
    startAutoSave();
  });

  onUnmounted(() => {
    stopAutoSave();
  });

  const preparerNouveauBrouillon = async (modeleId, codeArticle) => {
    const modRes = await fabModeleService.getModelById(modeleId);
    const data = modRes?.data?.data || modRes?.data || modRes;
    plan.value = {
      statut: 'ACTIF',
      codeArticleSage: codeArticle,
      designation: wizard.designationArticle.value,
      version: store.formulairesReferences?.find(f => f.codeReference === (wizard.refFormulaireCodeReference?.value || 'PRC'))?.version || 1,
      operationCode: data.operationCode,
      posteCode: wizard.posteCode.value || null,
      notes: data.notes || '',
      legendeMoyens: data.legendeMoyens || ''
    };
    syncPlanFormulaireConfig(wizard.refFormulaireCodeReference?.value || 'PRC');
    planCreationPayload.value = {
      modeleSourceId: modeleId,
      codeArticleSage: codeArticle,
      operationCode: data.operationCode,
      posteCode: wizard.posteCode.value || null,
      designation: wizard.designationArticle.value,
      refFormulaireCodeReference: wizard.refFormulaireCodeReference?.value || 'PRC',
      colonneDefs: store.effectiveConfigurationColonnes,
      creePar: 'ADMIN_QUALITE',
      remarques: data.notes || '',
      legendeMoyens: data.legendeMoyens || '',
      statut: 'ACTIF'
    };
    plan.value = {
      ...plan.value,
      colonneDefs: store.effectiveConfigurationColonnes,
      codeReferenceFormulaire: wizard.refFormulaireCodeReference?.value || 'PRC'
    };
    sections.value = planMapper.mapModelDataToSections(data);
    isFromWizard.value = true;
    planId.value = "nouveau";
    isGeneratingPlan.value = false;
  };



  const onWizardGenerate = async () => {
    if (isGeneratingPlan.value) return;
    
    const sourceType = wizard.sourceType.value;
    const modeleId = sourceType === 'MODELE' ? wizard.selectedSourceId.value : null;
    const codeArticle = wizard.codeArticleSage.value;
    const operationCode = wizard.operationCode.value;
    const famille = wizard.familleCode.value || wizard.typeRobinetCode.value;
    const nature = wizard.natureComposantCode.value;
    const posteCode = wizard.posteCode.value;

    await interceptCreation(
      codeArticle,
      famille,
      nature,
      operationCode,
      posteCode,
      async () => {
        isGeneratingPlan.value = true;
        await executerGenerationWizard(modeleId, codeArticle);
      }
    );
  };

  const executerGenerationWizard = async (modeleId, codeArticle) => {
    const sourceType = wizard.sourceType.value;
    const sourceId = (sourceType === 'CLONE') ? wizard.selectedSourceId.value : modeleId;
    try {
      if (sourceType === 'CLONE') {
        if (!sourceId) {
          toast.add({ severity: 'warn', summary: 'Attention', detail: 'Veuillez sélectionner un plan à cloner.', life: 4000 });
          isGeneratingPlan.value = false;
          return;
        }

        const res = await fabPlanService.getPlanById(sourceId);
        const rawData = res?.data?.data || res?.data || res;
        
        if (!rawData) throw new Error("Plan source introuvable.");
        const data = planMapper.normalizePlanData(rawData);

        plan.value = {
          statut: 'ACTIF',
          nom: `Plan de contrôle en cours de fabrication ${wizard.designationArticle.value || data.designation}${wizard.posteCode.value ? ' (' + wizard.posteCode.value + ')' : ''}`,
          codeArticleSage: codeArticle || data.codeArticleSage,
          designation: wizard.designationArticle.value || data.designation,
          version: store.formulairesReferences?.find(f => f.codeReference === (wizard.refFormulaireCodeReference?.value || 'PRC'))?.version || 1,
          operationCode: data.operationCode,
          posteCode: wizard.posteCode.value || data.posteCode
        };

        await loadPlan(data);
        
        planId.value = 'nouveau';
        isFromWizard.value = true;
        
        syncPlanFormulaireConfig(wizard.refFormulaireCodeReference?.value || 'PRC');
        planCreationPayload.value = {
            codeArticleSage: plan.value.codeArticleSage,
            designation: plan.value.designation,
            operationCode: plan.value.operationCode,
            posteCode: plan.value.posteCode,
            refFormulaireCodeReference: wizard.refFormulaireCodeReference?.value || 'PRC',
            colonneDefs: store.effectiveConfigurationColonnes,
            creePar: 'ADMIN_QUALITE',
            statut: 'ACTIF',
            sections: sections.value.map((s, idx) => ({
              ordreAffiche: idx + 1,
              libelleSection: s.nom || s.libelleSection || '',
              frequenceLibelle: s.frequenceLibelle || '',
              typeSectionId: (s.typeSectionId && s.typeSectionId !== "") ? s.typeSectionId : null,
              periodiciteId: (s.periodiciteId && s.periodiciteId !== "") ? s.periodiciteId : null,
              regleEchantillonnageId: (s.regleEchantillonnageId && s.regleEchantillonnageId !== "") ? s.regleEchantillonnageId : null,
              lignes: (s.lignes || []).map((l, lIdx) => ({
                ordreAffiche: lIdx + 1,
                typeCaracteristiqueId: (l.typeCaracteristiqueId && l.typeCaracteristiqueId !== "") ? l.typeCaracteristiqueId : null,
                typeControleId: (l.typeControleId && l.typeControleId !== "") ? l.typeControleId : null,
                moyenControleId: (l.moyenControleId && l.moyenControleId !== "") ? l.moyenControleId : null,
                instrumentCode: l.instrumentCode || '',
                moyenTexteLibre: l.moyenTexteLibre || '',
                valeurNominale: l.valeurNominale,
                toleranceSuperieure: l.toleranceSuperieure,
                toleranceInferieure: l.toleranceInferieure,
                unite: l.unite || '',
                limiteSpecTexte: l.limiteSpecTexte || '',
                instruction: l.instruction || '',
                observations: l.observations || '',
                estCritique: l.estCritique || false,
                libelleAffiche: l.libelleAffiche || ''
              }))
            }))
        };

        toast.add({ severity: 'success', summary: 'Succès', detail: 'Structure clonée chargée en mémoire.', life: 3000 });
      } else if (sourceType === 'VIERGE') {
        syncPlanFormulaireConfig(wizard.refFormulaireCodeReference?.value || 'PRC');
        plan.value = {
          statut: 'ACTIF',
          codeArticleSage: codeArticle,
          designation: wizard.designationArticle.value,
          version: store.formulairesReferences?.find(f => f.codeReference === (wizard.refFormulaireCodeReference?.value || 'PRC'))?.version || 1,
          operationCode: wizard.operationCode.value,
          posteCode: wizard.posteCode.value || null,
          colonneDefs: store.effectiveConfigurationColonnes,
          codeReferenceFormulaire: wizard.refFormulaireCodeReference?.value || 'PRC'
        };
        planCreationPayload.value = {
          modeleSourceId: null,
          codeArticleSage: codeArticle,
          operationCode: wizard.operationCode.value,
          posteCode: wizard.posteCode.value || null,
          designation: wizard.designationArticle.value,
          refFormulaireCodeReference: wizard.refFormulaireCodeReference?.value || 'PRC',
          colonneDefs: store.effectiveConfigurationColonnes,
          creePar: 'ADMIN_QUALITE',
          statut: 'ACTIF'
        };
        sections.value = [];
        isFromWizard.value = true;
        planId.value = 'nouveau';
        toast.add({ severity: 'success', summary: 'Succès', detail: 'Plan vierge prêt à éditer.', life: 3000 });
      } else {
        if (!sourceId) {
          toast.add({ severity: 'warn', summary: 'Attention', detail: 'Veuillez sélectionner un modèle.', life: 4000 });
          isGeneratingPlan.value = false;
          return;
        }
        await preparerNouveauBrouillon(sourceId, codeArticle);
      }
    } catch(err) {
      console.error("[WIZARD ERROR]", err);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de générer la structure : ' + (err.message || 'ID invalide'), life: 4000 });
    } finally {
      isGeneratingPlan.value = false;
    }
  };

  const onExcelSelected = async (event) => {
    const file = event.target?.files?.[0];
    if (!file) return;

    event.target.value = '';

    const codeArticle = wizard.codeArticleSage.value;
    const operationCode = wizard.operationCode.value;
    const famille = wizard.familleCode.value || wizard.typeRobinetCode.value;
    const nature = wizard.natureComposantCode.value;
    const posteCode = wizard.posteCode.value;

    const executeImport = async () => {
      wizard.isGenerating.value = true;
      const formData = new FormData();
      formData.append('file', file);
    const configCols = store.effectiveConfigurationColonnes?.length
      ? store.effectiveConfigurationColonnes
      : (plan.value?.colonneDefs || []);
    if (configCols?.length) {
      formData.append('configurationColonnesJson', JSON.stringify(configCols));
    }

    try {
      wizard.isGenerating.value = true;
      const parsedData = await store.importerDepuisExcel(file);

      if (parsedData) {
        if (store.entete.notes && store.entete.notes.trim() !== '') {
          remarques.value = store.entete.notes;
        }

        if (parsedData.sections) {
          const mappedSections = JSON.parse(JSON.stringify(store.sections));
          sections.value = mappedSections;

          if (parsedData.codeArticleSage) wizard.codeArticleSage.value = parsedData.codeArticleSage;
          if (parsedData.designation) wizard.designationArticle.value = parsedData.designation;
          if (parsedData.operationCode) wizard.operationCode.value = parsedData.operationCode;

          planCreationPayload.value = {
            modeleSourceId: null,
            codeArticleSage: parsedData.codeArticleSage || (typeof wizard.codeArticleSage.value === 'object' ? wizard.codeArticleSage.value.codeArticle : wizard.codeArticleSage.value),
            designation: parsedData.designation || wizard.designationArticle.value,
            operationCode: parsedData.operationCode || wizard.operationCode.value,
            natureComposantCode: wizard.natureComposantCode.value || '',
            posteCode: wizard.posteCode.value || null,
            refFormulaireCodeReference: wizard.refFormulaireCodeReference?.value || 'PRC',
            nom: '',
            creePar: 'ADMIN_QUALITE',
            statut: 'BROUILLON',
            sections: mappedSections.map(s => ({
              ...s,
              typeSectionId: (s.typeSectionId && s.typeSectionId !== "") ? s.typeSectionId : null,
              periodiciteId: (s.periodiciteId && s.periodiciteId !== "") ? s.periodiciteId : null,
              regleEchantillonnageId: (s.regleEchantillonnageId && s.regleEchantillonnageId !== "") ? s.regleEchantillonnageId : null,
              lignes: (s.lignes || []).map(l => ({
                ...l,
                typeCaracteristiqueId: (l.typeCaracteristiqueId && l.typeCaracteristiqueId !== "") ? l.typeCaracteristiqueId : null,
                typeControleId: (l.typeControleId && l.typeControleId !== "") ? l.typeControleId : null,
                moyenControleId: (l.moyenControleId && l.moyenControleId !== "") ? l.moyenControleId : null,
              }))
            }))
          };

          isFromWizard.value = true;
          planId.value = 'nouveau';
          plan.value = {
            statut: 'BROUILLON',
            nom: `Plan de contrôle en cours de fabrication ${parsedData.designation || wizard.designationArticle.value}${wizard.posteCode.value ? ' (' + wizard.posteCode.value + ')' : ''}`,
            codeArticleSage: parsedData.codeArticleSage || wizard.codeArticleSage.value,
            designation: parsedData.designation || wizard.designationArticle.value,
            operationCode: parsedData.operationCode || wizard.operationCode.value,
            posteCode: wizard.posteCode.value || null,
            version: 1
          };

          toast.add({ severity: 'success', summary: 'Import réussi', detail: 'Fichier Excel chargé en mémoire.', life: 3000 });
        }
      }
    } catch (error) {
      console.error('Erreur import Excel:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Erreur lors de la lecture du fichier.', life: 5000 });
    } finally {
      wizard.isGenerating.value = false;
    }
    };

    await interceptCreation(codeArticle, famille, nature, operationCode, posteCode, executeImport);
  };

  async function loadPlan(idOrData) {
    isLoadingData.value = true;
    try {
      let data;
      const isClone = typeof idOrData === 'object';
      
      if (isClone) {
        data = idOrData;
      } else {
        const res = await fabPlanService.getPlanById(idOrData);
        data = res?.data?.data || res?.data || res;
      }

      if (!isClone) {
        plan.value = planMapper.normalizePlanData(data);
        if (plan.value.nom) {
          const match = plan.value.nom.match(/^(.*?)(\.\w+)?$/);
          if (match && match[2]) {
            codeArticleSuffix.value = match[2].substring(1);
          } else {
            codeArticleSuffix.value = '';
          }
        }
        legendeMoyens.value = data.legendeMoyens || '';
        remarques.value = data.remarques || '';
        const codeRef = data.codeReferenceFormulaire || wizard.refFormulaireCodeReference?.value || 'PRC';
        if (wizard.refFormulaireCodeReference) wizard.refFormulaireCodeReference.value = codeRef;
        if (data.statut !== 'ARCHIVE') {
          syncPlanFormulaireConfig(codeRef, data);
        } else {
          let oldConfig = data.colonneDefs || [];
          if ((!oldConfig || oldConfig.length === 0) && data.configurationColonnesJson) {
            try { 
              const parsed = JSON.parse(data.configurationColonnesJson); 
              oldConfig = parsed.map(c => ({
                key: c.cleColonne || c.key,
                label: c.labelAffiche || c.label,
                type: c.typeValeur || c.type || 'Texte',
                insertAfter: c.insertAfter || 'code_instrument'
              }));
            } catch (e) {
              console.error('Erreur parsing configurationColonnesJson:', e);
            }
          }
          store.entete.configurationColonnes = oldConfig;
          if (plan.value) plan.value.colonneDefs = oldConfig;
        }
      } else if (data.codeReferenceFormulaire) {
        syncPlanFormulaireConfig(data.codeReferenceFormulaire);
      }

      const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
        (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
      );

      sections.value = planMapper.mapModelDataToSections({ sections: sectionsTriees });

    } catch (err) {
      console.error(err);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les données.', life: 4000 });
    } finally {
      isLoadingData.value = false;
    }
  }

  const onSaveDraft = async () => {
    if (isSaving.value) return;

    try {
      if (!codeArticleSuffix.value || codeArticleSuffix.value.trim() === '' || codeArticleSuffix.value === '.') {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'La version du Code Article (ex: .X) est obligatoire.', life: 4000 });
        return;
      }

      if (planCreationPayload.value) planCreationPayload.value.statut = 'BROUILLON';
      if (plan.value) plan.value.statut = 'BROUILLON';
      
      await planActions.sauvegarderBrouillonSilencieux(true, true);
      isExitingEditor.value = true;
      router.push('/dev/hub-plans');
    } catch (error) {
      console.error(error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible d\'enregistrer le brouillon.', life: 4000 });
    }
  };

  const onActivatePlan = async () => {
    if (isSaving.value) return;

    try {
      if (!codeArticleSuffix.value || codeArticleSuffix.value.trim() === '' || codeArticleSuffix.value === '.') {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'La version du Code Article (ex: .X) est obligatoire.', life: 4000 });
        return;
      }
      
      if (!validerSaisieValeurs()) return;
      if (!validerLegendeMoyens()) return;

      let rawCode = plan.value?.codeArticleSage || wizard.codeArticleSage.value;
      const codeArticle = typeof rawCode === 'object' && rawCode !== null ? rawCode.codeArticle : rawCode;
      
      const opCode = plan.value?.operationCode || wizard.operationCode.value;
      const pCode = plan.value?.posteCode || wizard.posteCode.value;
      const famille = wizard.familleCode.value || wizard.typeRobinetCode.value;
      const nature = wizard.natureComposantCode.value;
      
      const resVal = await fabPlanService.verifyPlanState(codeArticle, famille, nature, null, opCode, pCode);
      const etat = resVal.data;

      if (etat.hasActif && (!plan.value?.id || etat.actifId !== plan.value.id)) {
        const confirmed = await confirmArchivagePlanActif({
          typeDocument: 'plan de fabrication',
          identifiant: codeArticle
        });
        
        if (confirmed) {
          await planActions.declencherSauvegarde(true);
        }
      } else {
        await planActions.declencherSauvegarde(true);
      }
    } catch (error) {
      console.error('Erreur dans onActivatePlan:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible d\'activer le plan.', life: 5000 });
    }
  };

  const onEditorSubmit = async () => {
    if (isSaving.value) return;

    try {
      if (!isEditMode.value && planId.value !== 'nouveau') {
        await onWizardGenerate();
        return;
      }

      if (isArchived.value) {
        await planActions.mettreANiveauArchive();
        return;
      }

      if (plan.value?.statut === 'ACTIF') {
        await planActions.createNewVersionActive();
        return;
      }

      if (!codeArticleSuffix.value || codeArticleSuffix.value.trim() === '' || codeArticleSuffix.value === '.') {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'La version du Code Article (ex: .X) est obligatoire.', life: 4000 });
        return;
      }

      if (!validerSaisieValeurs()) return;
      if (!validerLegendeMoyens()) return;

      await planActions.declencherSauvegarde();
    } catch (error) {
      console.error(error);
    }
  };
</script>

<style scoped>
  .page-transition {
    transition: opacity 0.5s ease, transform 0.5s ease;
  }

  .fade-enter, .fade-leave-to {
    opacity: 0;
    transform: translateY(10px);
  }

  .zoom-enter, .zoom-leave-to {
    transform: scale(0.95);
  }
</style>
