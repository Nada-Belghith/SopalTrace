<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <ConfirmDialog />

    <VersioningDialog :visible="showVersioningDialog"
                      :mode="versioningMode"
                      :is-loading="isVersioningSaving"
                      @confirm="onVersioningConfirm"
                      @cancel="showVersioningDialog = false"
                      @update:visible="showVersioningDialog = $event" />

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
                <span class="font-mono font-bold text-sm text-slate-700 pl-2 py-1.5">{{ codeArticleBase }}</span>
                <InputText v-model="codeArticleSuffix" class="font-mono font-bold text-sm text-blue-700 p-1 w-16 h-8 mx-1 bg-white border-slate-300 focus:border-blue-500 rounded" placeholder=".X" />
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
                             :is-loading="isSaving && !isVersioningSaving && !showVersioningDialog"
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
  import { useFabPlanVersioning } from '@/composables/useVersioning';
  import { usePlanWizard } from '@/composables/usePlanWizard';
  import { useFabModeleStore } from '@/stores/fabModeleStore';
  import { prepareModeleDataAndFrequencies } from '@/utils/modelMapper';
  import { parseFrequenceLibelle, resolveFrequencyFromPeriodiciteId } from '@/utils/frequencyUtils';

  import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
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
  const route = useRoute();
  const router = useRouter();
  const toast = useToast();
  const confirm = useConfirm();
  const store = useFabModeleStore();
  const { creerNouvelleVersionPlan, upgradePlan, restaurerPlan } = useFabPlanVersioning();

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

  const {
    sections,
    ajouterSection,
    supprimerSection,
    mettreAJourSection,
    supprimerLigneASection
    // mettreAJourLigne (inutilisé)
  } = useEditorSections();
  const {
    showLegendValidation,
    hasCustomInstrumentsGlobal,
    validerLegendeMoyens,
    validerSaisiePlan: validerSaisieValeurs
  } = useEditorValidation(sections, legendeMoyens, toast);

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
    const code = plan.value?.codeArticleSageVersionne || wizard.codeArticleSage.value || plan.value?.codeArticleSage || '';
    const match = code.match(/^(.*?)(\.\w+)?$/);
    return match ? match[1] : code;
  });

  const codeArticleSuffix = computed({
    get: () => {
      const code = plan.value?.codeArticleSageVersionne || plan.value?.codeArticleSage || '';
      return code.substring(codeArticleBase.value.length) || '';
    },
    set: (val) => {
      if (plan.value) {
        let suffix = val || '';
        if (suffix && !suffix.startsWith('.')) suffix = '.' + suffix;
        plan.value.codeArticleSageVersionne = codeArticleBase.value + suffix;
        plan.value.codeArticleSage = plan.value.codeArticleSageVersionne; // Garder les deux synchros pour la vue
      }
    }
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

  const isExitingEditor = ref(false);
  const isCanceling = ref(false);
  const planCreationPayload = ref(null);
  const aEteCreePendantCetteSession = ref(false);

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
    await sauvegarderBrouillonSilencieux(true);
    return true;
  });

  const showVersioningDialog = ref(false);
  const versioningMode = ref('new-version');
  // Assure la cohérence des flags de chargement entre l'éditeur et la boîte de versioning

  const { isSaving, startAutoSave, stopAutoSave } = usePlanAutosave(async () => {
    if (plan.value?.statut === 'BROUILLON' || planCreationPayload.value) {
      await sauvegarderBrouillonSilencieux(false);
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
    if (planId.value && planId.value !== 'nouveau') await chargerPlan(planId.value);
    startAutoSave();
  });

  onUnmounted(() => {
    stopAutoSave();
  });


  const preparerNouveauBrouillon = async (modeleId, codeArticle) => {
    const modRes = await fabModeleService.getModeleById(modeleId);
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
    sections.value = mapModeleDataToSections(data);
    isFromWizard.value = true;
    planId.value = "nouveau";
    isGeneratingPlan.value = false;
  };

  // 🔥 SURVEILLANCE DES BROUILLONS EN AMONT
  // Dès que le couple Article/Opération est saisi, on vérifie s'il y a un brouillon
  let debounceTimeout = null;
  watch([wizard.codeArticleSage, wizard.operationCode, wizard.posteCode], ([code, op, poste]) => {
    if (code && op && (!wizard.requiertPoste.value || poste)) {
      if (debounceTimeout) clearTimeout(debounceTimeout);
      debounceTimeout = setTimeout(async () => {
        try {
          const famille = wizard.familleCode.value || wizard.typeRobinetCode.value;
          const nature = wizard.natureComposantCode.value;
          const res = await fabPlanService.verifierEtatPlan(code, famille, nature, null, op, poste);
          const etat = res?.data || res;

          if (etat.hasBrouillon) {
            confirm.require({
              message: `Un brouillon de plan existe déjà pour l'article ${code} / opération ${op}. Que souhaitez-vous faire ?`,
              header: '📋 Brouillon Existant',
              icon: 'pi pi-copy text-amber-500',
              acceptLabel: '↩ Récupérer le brouillon',
              rejectLabel: '🗑 Supprimer & Créer un nouveau',
              acceptClass: 'p-button-warning',
              rejectClass: 'p-button-danger p-button-outlined',
              accept: async () => {
                // Récupérer le brouillon existant → ouvrir directement dans l'éditeur
                planId.value = etat.brouillonId;
                isFromWizard.value = false;
                await chargerPlan(etat.brouillonId);
                router.replace(`/dev/fab/plans/editer/${etat.brouillonId}`);
                toast.add({ severity: 'info', summary: 'Brouillon récupéré', detail: 'Vous pouvez continuer la saisie et activer ce plan.', life: 4000 });
              },
              reject: async () => {
                // Supprimer le brouillon et laisser l'utilisateur continuer le wizard
                try {
                  await fabPlanService.deletePlan(etat.brouillonId);
                  toast.add({ severity: 'info', summary: 'Brouillon supprimé', detail: 'Vous pouvez maintenant choisir votre méthode de création.', life: 3000 });
                } catch (err) {
                  console.error('Erreur suppression brouillon:', err);
                  toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de supprimer le brouillon.', life: 4000 });
                }
              }
            });
          }
        } catch (err) {
          console.error("Erreur check draft amont", err);
        }
      }, 600);
    }
  });

  const onWizardGenerate = async () => {
    if (isGeneratingPlan.value) return;
    isGeneratingPlan.value = true; // ⚠️ DÉCLENCHE LE BOUCLIER AUTOSAVE PENDANT L'EXÉCUTION

    try {
      const sourceType = wizard.sourceType.value;
      const modeleId = sourceType === 'MODELE' ? wizard.selectedSourceId.value : null;
      const codeArticle = wizard.codeArticleSage.value;
      const operationCode = wizard.operationCode.value;
      const famille = wizard.familleCode.value || wizard.typeRobinetCode.value;
      const nature = wizard.natureComposantCode.value;

      // Unifié: on vérifie l'état peu importe qu'on clone ou utilise un modèle
      const resVal = await fabPlanService.verifierEtatPlan(codeArticle, famille, nature, modeleId, operationCode, wizard.posteCode.value);
      const etat = resVal.data;

      if (etat.hasBrouillon) {
        // Un brouillon existe → demander à l'utilisateur ce qu'il veut faire
        isGeneratingPlan.value = false;
        confirm.require({
          message: `Un brouillon de plan existe déjà pour l'article ${codeArticle} / opération ${operationCode}. Que souhaitez-vous faire ?`,
          header: '📋 Brouillon Existant',
          icon: 'pi pi-copy text-amber-500',
          acceptLabel: '↩ Récupérer le brouillon',
          rejectLabel: '🗑 Supprimer & Créer un nouveau',
          acceptClass: 'p-button-warning',
          rejectClass: 'p-button-danger p-button-outlined',
          accept: async () => {
            planId.value = etat.brouillonId;
            isFromWizard.value = false;
            await chargerPlan(etat.brouillonId);
            router.replace(`/dev/fab/plans/editer/${etat.brouillonId}`);
            toast.add({ severity: 'info', summary: 'Brouillon récupéré', detail: 'Vous pouvez continuer la saisie et activer ce plan.', life: 4000 });
          },
          reject: async () => {
            try {
              await fabPlanService.deletePlan(etat.brouillonId);
              toast.add({ severity: 'info', summary: 'Brouillon supprimé', detail: 'Vous pouvez maintenant générer un nouveau plan.', life: 3000 });
              // Relancer la génération
              await executerGenerationWizard(modeleId, codeArticle);
            } catch (err) {
              console.error('Erreur suppression brouillon:', err);
            }
          }
        });
        return;
      } 
      else if (etat.hasActif) {
        confirm.require({
          message: `Un plan ACTIF (Version ${etat.actifVersion}) existe déjà pour cette opération. Voulez-vous créer un nouveau plan ? Si vous l'activez plus tard, le plan actuel sera automatiquement archivé.`,
          header: 'Plan Actif Existant',
          icon: 'pi pi-exclamation-triangle text-blue-500',
          acceptLabel: 'Confirmer & Créer',
          rejectLabel: 'Annuler',
          acceptClass: 'p-button-primary',
          accept: async () => {
            await executerGenerationWizard(modeleId, codeArticle);
          },
          reject: () => {
            isGeneratingPlan.value = false;
          }
        });
      } 
      else {
        // Archivé ou rien => Création Libre
        await executerGenerationWizard(modeleId, codeArticle);
      }
    } catch (error) {
      console.error('Erreur génération:', error);
      isGeneratingPlan.value = false;
    }
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

        // Clonage en MÉMOIRE
        const res = await fabPlanService.getPlanById(sourceId);
        const rawData = res?.data?.data || res?.data || res;
        
        if (!rawData) throw new Error("Plan source introuvable.");
        const data = normaliserDonneesPlan(rawData);

        plan.value = {
          statut: 'ACTIF',
          nom: `Plan de contrôle en cours de fabrication ${wizard.designationArticle.value || data.designation}${wizard.posteCode.value ? ' (' + wizard.posteCode.value + ')' : ''}`,
          codeArticleSage: codeArticle || data.codeArticleSage,
          designation: wizard.designationArticle.value || data.designation,
          version: store.formulairesReferences?.find(f => f.codeReference === (wizard.refFormulaireCodeReference?.value || 'PRC'))?.version || 1,
          operationCode: data.operationCode,
          posteCode: wizard.posteCode.value || data.posteCode
        };

        // On charge les sections en mode "Clonage" (nettoyage des IDs)
        await chargerPlan(data);
        
        planId.value = 'nouveau';
        isFromWizard.value = true;
        
        // Préparer le payload pour la future création lors du clic Enregistrer
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
        // Nouveau depuis Modèle
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

  const normaliserDonneesPlan = (data) => {
    if (!data) return data;
    if (data.nom) {
      data.codeArticleSageVersionne = data.nom;
      const match = data.nom.match(/^(.*?)(\.\w+)?$/);
      data.codeArticleSage = match ? match[1] : data.nom;
    }
    return data;
  };

  const nettoyerNomSection = (libelleSection, typeSectionId, freqLib = '', regleLib = '') => {
    if (!libelleSection) return '';
    const normalizeApostrophes = (s) => s.replace(/’/g, "'");
    let clean = normalizeApostrophes(libelleSection).replace(/caractéristiques à contrôler/gi, '').trim();
    
    const escapeRegExp = (str) => str.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&');
    
    if (typeSectionId) {
      const typeSec = (store.typesSection || []).find(t => t.id === typeSectionId);
      if (typeSec && typeSec.libelle) {
        clean = clean.replace(new RegExp(escapeRegExp(normalizeApostrophes(typeSec.libelle)), 'gi'), '').trim();
      }
    }
    
    if (freqLib) {
      const freqNorm = normalizeApostrophes(freqLib);
      const freqPattern = '\\(?\\s*' + escapeRegExp(freqNorm) + '\\s*\\)?';
      clean = clean.replace(new RegExp(freqPattern, 'gi'), '').trim();
    }
    
    if (regleLib) {
      const regleNorm = normalizeApostrophes(regleLib);
      const reglePattern = '\\(?\\s*' + escapeRegExp(regleNorm) + '\\s*\\)?';
      clean = clean.replace(new RegExp(reglePattern, 'gi'), '').trim();
    }

    // Double clean to remove any redundant/empty parentheses like "()" or "( )" that might have been left
    clean = clean.replace(/\(\s*\)/g, '').trim();

    // Remove leading/trailing punctuation like dashes, slashes, parens (both open and close)
    clean = clean.replace(/^[\s\-_:/( )]+|[\s\-_:/( )]+$/g, '').trim();
    return clean;
  };

  const onExcelSelected = async (event) => {
    const file = event.target?.files?.[0];
    if (!file) return;

    event.target.value = '';
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

          // Mise à jour du wizard pour que le header affiche les bonnes infos
          if (parsedData.codeArticleSage) wizard.codeArticleSage.value = parsedData.codeArticleSage;
          if (parsedData.designation) wizard.designationArticle.value = parsedData.designation;
          if (parsedData.operationCode) wizard.operationCode.value = parsedData.operationCode;

          // Bascule dans l'éditeur comme brouillon en mémoire
          planCreationPayload.value = {
            modeleSourceId: null, // Pas de modèle source pour l'import Excel
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

          // Initialisation du statut pour l'UI
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

  const mapModeleDataToSections = (modeleModel) => {
    return (modeleModel.sections || []).map(sec => {
      let freqData = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
      if (sec.periodiciteId) {
        const resolved = resolveFrequencyFromPeriodiciteId(sec.periodiciteId, store.periodicites || []);
        if (resolved) {
          freqData = resolved;
        }
      }
      const texteParse = sec.frequenceLibelle || sec.libelleSection || '';
      if (freqData.modeFreq === 'SANS') {
        if (texteParse) {
          freqData = parseFrequenceLibelle(texteParse, store.periodicites || []);
        }
      }

      let modeFreq = freqData.modeFreq;
      let regleEchantillonnageId = sec.regleEchantillonnageId || null;
      let periodiciteId = sec.periodiciteId || freqData.periodiciteId;
      let freqNum = sec.freqNum || freqData.freqNum;
      let typeVariable = sec.typeVariable || freqData.typeVariable;
      let freqHours = sec.freqHours || freqData.freqHours;

      if (regleEchantillonnageId) {
        modeFreq = 'FIXE';
      } else if (periodiciteId) {
        modeFreq = 'VARIABLE';
      } else if (texteParse) {
        const regMatch = (store.reglesEchantillonnage || []).find(r => r.libelle === texteParse);
        if (regMatch) {
          modeFreq = 'FIXE';
          regleEchantillonnageId = regMatch.id;
        }
      }

      let typeSectionId = sec.typeSectionId || '';
      if (!typeSectionId && sec.libelleSection) {
        const secLib = sec.libelleSection.trim().toLowerCase();
        let bestMatch = null;
        let maxLength = -1;

        store.typesSection.forEach(t => {
          const tLib = (t.libelle || '').trim().toLowerCase();
          if (!tLib || secLib === 'section sans nom') return;

          if (secLib.includes(tLib)) {
            if (tLib.length > maxLength) {
              maxLength = tLib.length;
              bestMatch = t;
            }
          }
        });

        if (bestMatch) {
          typeSectionId = bestMatch.id;
        }
      }

      return {
        id: crypto.randomUUID(),
        isFromDb: false,
        modeleSectionId: sec.id,
        typeSectionId,
        modeFreq,
        periodiciteId,
        regleEchantillonnageId,
        freqNum,
        typeVariable,
        freqHours,
        isNewFreq: false,
        frequenceLibelle: sec.frequenceLibelle || '', // Indispensable pour la visualisation
        nom: nettoyerNomSection(sec.libelleSection, typeSectionId, sec.frequenceLibelle || '', sec.regleEchantillonnageLibelle || ''),
        // ⚠️ BOURCLIER ANTI-CRASH: Filtre les lignes nulles
        lignes: (sec.lignes || []).filter(lig => lig != null).map(lig => ({
          id: crypto.randomUUID(),
          isFromDb: false,
          modeleLigneSourceId: lig.id,
          typeCaracteristiqueId: lig.typeCaracteristiqueId,
          typeControleId: lig.typeControleId,
          moyenControleId: lig.moyenControleId,
          instrumentCode: lig.instrumentCode,
          moyenTexteLibre: lig.moyenTexteLibre || '',
          valeurNominale: lig.valeurNominale ?? null,
          toleranceSuperieure: lig.toleranceSuperieure ?? null,
          toleranceInferieure: lig.toleranceInferieure ?? null,
          unite: lig.unite || '',
          limiteSpecTexte: lig.limiteSpecTexte || '',
          instruction: lig.instruction || '',
          observations: lig.observations || '',
          estCritique: lig.estCritique,
          libelleAffiche: lig.libelleAffiche,
          imageBase64: lig.imageBase64 || null,
          valeursColonnesSpecifiques: lig.extraColonnes 
            ? Object.fromEntries(lig.extraColonnes.map(ec => [ec.cleColonne, ec.valeurColonne])) 
            : (lig.colonnesSupplementaires ? JSON.parse(lig.colonnesSupplementaires) : (lig.valeursColonnesSpecifiques || {}))
        }))
      };
    });
  };

  const chargerPlan = async (idOrData) => {
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
        plan.value = normaliserDonneesPlan(data);
        legendeMoyens.value = data.legendeMoyens || '';
        remarques.value = data.remarques || '';
        const codeRef = data.codeReferenceFormulaire || wizard.refFormulaireCodeReference?.value || 'PRC';
        if (wizard.refFormulaireCodeReference) wizard.refFormulaireCodeReference.value = codeRef;
        syncPlanFormulaireConfig(codeRef, data);
      } else if (data.codeReferenceFormulaire) {
        syncPlanFormulaireConfig(data.codeReferenceFormulaire);
      }

      const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
        (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
      );

      sections.value = sectionsTriees.map(sec => {
        let freqData = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
        if (sec.periodiciteId) {
          const resolved = resolveFrequencyFromPeriodiciteId(sec.periodiciteId, store.periodicites || []);
          if (resolved) {
            freqData = resolved;
          }
        }
        const texteParse = sec.frequenceLibelle || sec.libelleSection || '';
        if (freqData.modeFreq === 'SANS') {
          if (texteParse) {
            freqData = parseFrequenceLibelle(texteParse, store.periodicites || []);
          }
        }

        let modeFreq = freqData.modeFreq;
        let regleEchantillonnageId = sec.regleEchantillonnageId || null;
        let periodiciteId = sec.periodiciteId || freqData.periodiciteId;
        let freqNum = sec.freqNum || freqData.freqNum;
        let typeVariable = sec.typeVariable || freqData.typeVariable;
        let freqHours = sec.freqHours || freqData.freqHours;

        if (regleEchantillonnageId) {
          modeFreq = 'FIXE';
        } else if (periodiciteId) {
          modeFreq = 'VARIABLE';
        } else if (texteParse) {
          const regMatch = (store.reglesEchantillonnage || []).find(r => r.libelle === texteParse);
          if (regMatch) {
            modeFreq = 'FIXE';
            regleEchantillonnageId = regMatch.id;
          }
        }

        let typeSectionId = sec.typeSectionId || '';
        let extractedNature = "";
        let originalNom = (sec.nom || sec.libelleSection || '').trim();

        let finalNom = sec.libelleSection || '';

        // 2. Nettoyer le préfixe pour trouver la "Nature"
        let candidateNature = originalNom.replace(/caractéristiques à contrôler/gi, "").trim();

        // 3. Chercher si cette nature existe dans le dictionnaire
        if (!typeSectionId && candidateNature) {
          const secLibLower = candidateNature.toLowerCase();
          const match = (store.typesSection || []).find(t => (t.libelle || '').toLowerCase() === secLibLower);
          if (match) {
            typeSectionId = match.id;
            extractedNature = match.libelle;
          } else {
            // Nature personnalisée (ex: test test)
            extractedNature = candidateNature;
          }
        } else if (typeSectionId) {
          const match = (store.typesSection || []).find(t => t.id === typeSectionId);
          if (match) extractedNature = match.libelle;
        }

        // 4. Titre final (Source de vérité = libelleSection original)
        finalNom = finalNom || (extractedNature ? `Caractéristiques à contrôler ${extractedNature}` : "Section sans nom");

        const lignesTriees = [...(sec.lignes || [])]
          .filter(lig => lig != null)
          .sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0));

        return {
          id: isClone ? crypto.randomUUID() : sec.id,
          isFromDb: !isClone,
          typeSectionId,
          libelleSection: sec.libelleSection || finalNom, // On garde le libellé complet
          modeFreq,
          periodiciteId,
          regleEchantillonnageId,
          freqNum,
          typeVariable,
          freqHours,
          isNewFreq: false,
          frequenceLibelle: sec.frequenceLibelle || '',
          nom: nettoyerNomSection(finalNom, typeSectionId, sec.frequenceLibelle || '', sec.regleEchantillonnageLibelle || ''),
          lignes: lignesTriees.map(lig => ({
            id: isClone ? crypto.randomUUID() : lig.id,
            isFromDb: !isClone,
            typeCaracteristiqueId: lig.typeCaracteristiqueId,
            typeControleId: lig.typeControleId,
            moyenControleId: lig.moyenControleId,
            instrumentCode: lig.instrumentCode,
            moyenTexteLibre: lig.moyenTexteLibre || '',
            valeurNominale: lig.valeurNominale,
            toleranceSuperieure: lig.toleranceSuperieure,
            toleranceInferieure: lig.toleranceInferieure,
            unite: lig.unite || '',
            limiteSpecTexte: lig.limiteSpecTexte || '',
            instruction: lig.instruction || '',
            observations: lig.observations || '',
            estCritique: lig.estCritique,
            libelleAffiche: lig.libelleAffiche,
            imageBase64: lig.imageBase64 || null,
            valeursColonnesSpecifiques: lig.extraColonnes 
              ? Object.fromEntries(lig.extraColonnes.map(ec => [ec.cleColonne, ec.valeurColonne])) 
              : (lig.colonnesSupplementaires ? JSON.parse(lig.colonnesSupplementaires) : (lig.valeursColonnesSpecifiques || {}))
          }))
        };
      });

    } catch (err) {
      console.error(err);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les données.', life: 4000 });
    } finally {
      isLoadingData.value = false;
    }
  };

  const normalizeId = (id) => (typeof id === 'string' && id.length <= 36 ? id : null);

  const syncIdsFromDb = (dbPlanData) => {
    if (!dbPlanData) return;

    // Synchroniser l'objet plan principal pour avoir les bonnes métadonnées (nom, version, etc.)
    plan.value = normaliserDonneesPlan(dbPlanData);

    if (!dbPlanData.sections) return;

    sections.value.forEach((sec, sIdx) => {
      const dbSec = dbPlanData.sections.find(ds => ds.ordreAffiche === (sIdx + 1));

      if (dbSec) {
        sec.id = dbSec.id;
        sec.isFromDb = true;
        sec.modeleSectionId = dbSec.modeleSectionId;

        (sec.lignes || []).forEach((lig, lIdx) => {
          const dbLig = (dbSec.lignes || []).find(dl => dl.ordreAffiche === (lIdx + 1));

          if (dbLig) {
            lig.id = dbLig.id;
            lig.isFromDb = true;
            lig.modeleLigneSourceId = dbLig.modeleLigneSourceId;
          } else {
            lig.isFromDb = false;
          }
        });
      } else {
        sec.isFromDb = false;
      }
    });
  };

  // ⭐ FONCTION UTILITAIRE
  // Nettoie / prépare les valeurs numériques avant envoi.
  // Pour un brouillon (isDraft = true) on PRESERVE la `valeurNominale` même si les tolérances
  // sont absentes (l'utilisateur peut être en train de saisir). Pour l'activation (isDraft = false)
  // on neutralise la valeur si les tolérances sont incomplètes afin d'éviter des erreurs serveur.
  const sanitizeMesurements = (ligne, isDraft = false) => {
    const hasValeur = ligne.valeurNominale != null && ligne.valeurNominale !== '';
    const hasTolSup = ligne.toleranceSuperieure != null && ligne.toleranceSuperieure !== '';
    const hasTolInf = ligne.toleranceInferieure != null && ligne.toleranceInferieure !== '';

    if (isDraft) {
      return {
        valeurNominale: hasValeur ? ligne.valeurNominale : null,
        toleranceSuperieure: hasTolSup ? ligne.toleranceSuperieure : null,
        toleranceInferieure: hasTolInf ? ligne.toleranceInferieure : null
      };
    }

    // Si valeur nominale est présente MAIS tolérances manquent = neutraliser tout pour activation
    if (hasValeur && (!hasTolSup || !hasTolInf)) {
      return {
        valeurNominale: null,
        toleranceSuperieure: null,
        toleranceInferieure: null
      };
    }

    return {
      valeurNominale: hasValeur ? ligne.valeurNominale : null,
      toleranceSuperieure: hasTolSup ? ligne.toleranceSuperieure : null,
      toleranceInferieure: hasTolInf ? ligne.toleranceInferieure : null
    };
  };

  const construirePayloadService = (isDraft) => {
    return sections.value.map((originalSection, idx) => {
      // 1. Calcul de la fréquence pour le libellé technique
      let finalFrequenceLibelle = '';
      if (originalSection.modeFreq === 'VARIABLE') {
        const is100 = originalSection.freqNum === 100 && originalSection.typeVariable === 'HEURE';
        if (is100) {
          const p100 = (store.periodicites || []).find(p => p.frequenceNum === 100 || p.code === '100PCT_1H');
          finalFrequenceLibelle = p100 ? p100.libelle : "100% des pièces/h";
        } else {
          finalFrequenceLibelle = originalSection.frequenceLibelle || '';
        }
      } else if (originalSection.periodiciteId) {
        const matchingPeriod = (store.periodicites || []).find(p => {
          const pId = p.id || p.Id;
          return pId && typeof pId === 'string' && typeof originalSection.periodiciteId === 'string' && pId.toLowerCase() === originalSection.periodiciteId.toLowerCase();
        });
        finalFrequenceLibelle = matchingPeriod ? (matchingPeriod.libelle || matchingPeriod.Libelle || '') : '';
      }

      // 2. Libellé de règle d'échantillonnage
      let regleEchLibelle = '';
      const regleEchId = originalSection.regleEchantillonnageId;
      if (regleEchId) {
        regleEchLibelle = (store.reglesEchantillonnage || []).find(r => r.id === regleEchId)?.libelle || '';
      }

      const typeSectionId = originalSection.typeSectionId;

      return {
        id: originalSection.isFromDb ? normalizeId(originalSection.id) : null,
        modeleSectionId: originalSection.modeleSectionId,
        ordreAffiche: idx + 1,
        typeSectionId: (typeSectionId && typeSectionId !== "") ? typeSectionId : null,
        libelleSection: originalSection.nom || originalSection.libelleSection || 'SECTION SANS NOM',
        notes: originalSection.notes || '',
        frequenceLibelle: finalFrequenceLibelle,
        regleEchantillonnageLibelle: regleEchLibelle,
        periodiciteId: (originalSection.periodiciteId && originalSection.periodiciteId !== "") ? originalSection.periodiciteId : null,
        regleEchantillonnageId: (regleEchId && regleEchId !== "") ? regleEchId : null,
        lignes: (originalSection.lignes || []).map((l, lIdx) => {
          const caractMatch = (store.typesCaracteristique || store.caracteristiques || []).find(c => c.id === l.typeCaracteristiqueId);
          const nomCaract = caractMatch?.libelle || '';
          
          const mesurements = sanitizeMesurements(l, isDraft);
          const hasNumeric = mesurements.valeurNominale != null || mesurements.toleranceInferieure != null || mesurements.toleranceSuperieure != null;
          
          return {
            id: l.isFromDb ? normalizeId(l.id) : null,
            modeleLigneSourceId: l.modeleLigneSourceId,
            ordreAffiche: lIdx + 1,
            typeCaracteristiqueId: (l.typeCaracteristiqueId && l.typeCaracteristiqueId !== "") ? l.typeCaracteristiqueId : null,
            typeControleId: (l.typeControleId && l.typeControleId !== "") ? l.typeControleId : null,
            moyenControleId: (l.moyenControleId && l.moyenControleId !== "") ? l.moyenControleId : null,
            moyenTexteLibre: l.moyenTexteLibre || '',
            instrumentCode: l.instrumentCode || '',
            valeurNominale: hasNumeric ? mesurements.valeurNominale : null,
            toleranceSuperieure: hasNumeric ? mesurements.toleranceSuperieure : null,
            toleranceInferieure: hasNumeric ? mesurements.toleranceInferieure : null,
            unite: l.unite || '',
            limiteSpecTexte: !hasNumeric && l.limiteSpecTexte ? String(l.limiteSpecTexte).trim() : '',
            instruction: l.instruction || '',
            observations: l.observations || '',
            estCritique: l.estCritique || false,
            libelleAffiche: (l.libelleAffiche || nomCaract).trim(),
            imageBase64: l.imageBase64 || null,
            extraColonnes: l.valeursColonnesSpecifiques && Object.keys(l.valeursColonnesSpecifiques).length > 0 
              ? Object.entries(l.valeursColonnesSpecifiques).map(([key, val], idx) => ({
                  cleColonne: key,
                  valeurColonne: val,
                  ordreAffiche: idx + 1
                }))
              : []
          };
        })
      };
    });
  };

  const sauvegarderBrouillonSilencieux = async (afficherToast = false, force = false) => {
    // 🛡️ BLOCAGE DE SÉCURITÉ : Ne pas sauvegarder si on est en train de charger, de générer ou si on quitte
    if (!force && (isLoadingData.value || isGeneratingPlan.value || isCanceling.value || isSaving.value || plan.value?.statut === 'ACTIF' || isArchived.value)) return;

    let currentPlanId = planId.value;

    try {
      if (currentPlanId === 'nouveau' && planCreationPayload.value) {
        if (versionInitiale.value !== null) {
          planCreationPayload.value.versionInitiale = versionInitiale.value;
        }
        // Assure-toi qu'on envoie le code modifié (ex: avec .4)
        if (plan.value?.codeArticleSage) {
          planCreationPayload.value.codeArticleSage = plan.value.codeArticleSage;
        }
        
        const sectionsPayload = construirePayloadService(true);
        if (sectionsPayload && sectionsPayload.length > 0) {
          planCreationPayload.value.sections = sectionsPayload;
        }

        const instRes = await fabPlanService.instantiatePlan(planCreationPayload.value);
        const instData = instRes?.data?.data || instRes?.data || instRes;
        currentPlanId = instData.planId || instData.id;
        planId.value = currentPlanId;
        aEteCreePendantCetteSession.value = true;
        const newPlanRes = await fabPlanService.getPlanById(currentPlanId);
        const dataPlan = newPlanRes?.data?.data || newPlanRes?.data || newPlanRes;
        syncIdsFromDb(dataPlan);
        planCreationPayload.value = null;
      }

      if (!currentPlanId || currentPlanId === 'nouveau') return;

      await prepareModeleDataAndFrequencies(sections.value, store.periodicites, async (payload) => {
        const res = await fabPlanService.createPeriodicite(payload);
        const resData = res?.data?.data || res?.data || res;
        store.periodicites.push({ id: resData.periodiciteId || resData.id, ...payload });
        return res;
      });

      const payload = construirePayloadService(true);
      const finalNom = plan.value?.nom && !plan.value.nom.includes('Modèle') ? plan.value.nom : `Plan de contrôle en cours de fabrication ${plan.value?.designation || wizard.designationArticle.value}${plan.value?.posteCode || wizard.posteCode.value ? ' (' + (plan.value?.posteCode || wizard.posteCode.value) + ')' : ''}`;

      const payloadData = {
        sections: payload,
        colonneDefs: planConfigurationColonnes.value
      };

      await fabPlanService.mettreAJourValeurs(currentPlanId, payloadData, legendeMoyens.value, remarques.value, false, finalNom, 'Admin', plan.value?.codeArticleSage);

      if (afficherToast) {
        toast.add({ severity: 'info', summary: 'Brouillon enregistré', detail: 'Vos données sont sauvegardées.', life: 3000 });
      }
    } catch (error) {
      console.error("L'auto-save a échoué.", error);
    }
  };

  const declencherSauvegarde = async (isActivating = false) => {
    let currentPlanId = planId.value;

    if (currentPlanId === 'nouveau' && planCreationPayload.value) {
      try {
        if (versionInitiale.value !== null) {
          planCreationPayload.value.versionInitiale = versionInitiale.value;
        }
        if (plan.value?.codeArticleSage) {
          planCreationPayload.value.codeArticleSage = plan.value.codeArticleSage;
        }

        // 1. Préparer d'abord les périodicités (si certaines ont été saisies à la main)
        await prepareModeleDataAndFrequencies(
          sections.value,
          store.periodicites || [],
          async (payloadFreq) => {
            const res = await fabPlanService.createPeriodicite(payloadFreq);
            const resData = res?.data?.data || res?.data || res;
            store.periodicites.push({ id: resData.periodiciteId || resData.id, ...payloadFreq });
            return res;
          }
        );

        // 2. Assigner le statut définitif et les autres champs
        if (isActivating) {
          planCreationPayload.value.statut = 'ACTIF';
        }
        
        const finalNom = plan.value?.nom && !plan.value.nom.includes('Modèle') ? plan.value.nom : `Plan de contrôle en cours de fabrication ${plan.value?.designation || wizard.designationArticle.value}${plan.value?.posteCode || wizard.posteCode.value ? ' (' + (plan.value?.posteCode || wizard.posteCode.value) + ')' : ''}`;
        planCreationPayload.value.nom = finalNom;
        planCreationPayload.value.legendeMoyens = legendeMoyens.value;
        planCreationPayload.value.remarques = remarques.value;

        // 3. Injecter les sections construites dans le payload AVANT de créer le plan
        const sectionsPayload = construirePayloadService(isActivating ? false : (plan.value?.statut === 'BROUILLON'));
        if (sectionsPayload && sectionsPayload.length > 0) {
          planCreationPayload.value.sections = sectionsPayload;
        }

        const instRes = await fabPlanService.instantiatePlan(planCreationPayload.value);
        const instData = instRes?.data?.data || instRes?.data || instRes;
        currentPlanId = instData.planId || instData.id;
        planId.value = currentPlanId;

        planCreationPayload.value = null;

        // Le plan est déjà créé et sauvegardé avec toutes ses données et le bon statut.
        if (isActivating) {
          toast.add({ severity: 'success', summary: 'Plan Activé', detail: 'Le plan a été créé et activé directement.', life: 4000 });
        } else {
          toast.add({ severity: 'info', summary: 'Brouillon enregistré', detail: 'Le brouillon a été créé avec succès.', life: 3000 });
        }

        isExitingEditor.value = true;
        router.push('/dev/hub-plans');
        return; // FIN DE L'ACTION POUR UN NOUVEAU PLAN
      } catch (err) {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de créer le plan', life: 6000 });
        throw err;
      }
    }

    // SI LE PLAN EXISTE DÉJÀ (Mise à jour)
    await enregistrerValeurs(currentPlanId, true, isActivating);
  };

  const enregistrerValeurs = async (currentPlanId, redirectToHub = true, isActivating = false) => {
    try {
      await prepareModeleDataAndFrequencies(
        sections.value,
        store.periodicites || [],
        async (payloadFreq) => {
          const res = await fabPlanService.createPeriodicite(payloadFreq);
          const resData = res?.data?.data || res?.data || res;
          store.periodicites.push({ id: resData.periodiciteId || resData.id, ...payloadFreq });
          return res;
        }
      );

      const payload = construirePayloadService(isActivating ? false : (plan.value?.statut === 'BROUILLON'));
      const finalNom = plan.value?.nom && !plan.value.nom.includes('Modèle') ? plan.value.nom : `Plan de contrôle en cours de fabrication ${plan.value?.designation || wizard.designationArticle.value}${plan.value?.posteCode || wizard.posteCode.value ? ' (' + (plan.value?.posteCode || wizard.posteCode.value) + ')' : ''}`;

      const payloadData = {
        sections: payload,
        colonneDefs: planConfigurationColonnes.value
      };

      await fabPlanService.mettreAJourValeurs(currentPlanId, payloadData, legendeMoyens.value, remarques.value, isActivating, finalNom, 'Admin', plan.value?.codeArticleSage);

      if (isActivating) {
        toast.add({ severity: 'success', summary: 'Plan Activé', detail: 'Le plan est maintenant en production.', life: 4000 });
      } else {
        toast.add({ severity: 'info', summary: 'Données sauvegardées', detail: 'Vos modifications ont été enregistrées.', life: 3000 });
      }

      if (redirectToHub) {
        isExitingEditor.value = true;
        router.push('/dev/hub-plans');
      } else {
        await chargerPlan(currentPlanId);
      }
    } catch (error) {
      console.error('Erreur sauvegarde:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Une erreur est survenue lors de la sauvegarde.', life: 4000 });
      throw error;
    }
  };

  const onSaveDraft = async () => {
    if (isSaving.value) return;
    isSaving.value = true;

    try {
      if (planCreationPayload.value) planCreationPayload.value.statut = 'BROUILLON';
      if (plan.value) plan.value.statut = 'BROUILLON';
      // Forcer la sauvegarde manuelle (bypass le flag isSaving interne)
      await sauvegarderBrouillonSilencieux(true, true);
      isExitingEditor.value = true;
      router.push('/dev/hub-plans');
    } catch (error) {
      console.error(error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible d\'enregistrer le brouillon.', life: 4000 });
    } finally {
      isSaving.value = false;
    }
  };

  const onActivatePlan = async () => {
    if (isSaving.value) return;
    isSaving.value = true;

    try {
      if (!validerSaisieValeurs()) {
        isSaving.value = false;
        return;
      }
      if (!validerLegendeMoyens()) {
        isSaving.value = false;
        return;
      }

      // Vérification finale si un plan actif existe déjà avant d'activer celui-ci
      let rawCode = plan.value?.codeArticleSage || wizard.codeArticleSage.value;
      const codeArticle = typeof rawCode === 'object' && rawCode !== null ? rawCode.codeArticle : rawCode;
      
      const opCode = plan.value?.operationCode || wizard.operationCode.value;
      const pCode = plan.value?.posteCode || wizard.posteCode.value;
      const famille = wizard.familleCode.value || wizard.typeRobinetCode.value;
      const nature = wizard.natureComposantCode.value;
      
      const resVal = await fabPlanService.verifierEtatPlan(codeArticle, famille, nature, null, opCode, pCode);
      const etat = resVal.data;

      if (etat.existeActif && (!plan.value?.id || etat.actifId !== plan.value.id)) {
        confirm.require({
          message: `Attention : Un plan ACTIF existe déjà pour cet article. L'activation de ce nouveau plan archivera automatiquement l'ancien. Voulez-vous continuer ?`,
          header: 'Confirmation d\'Activation',
          icon: 'pi pi-exclamation-triangle text-amber-500',
          acceptLabel: 'Oui, Archiver & Activer',
          rejectLabel: 'Annuler',
          acceptClass: 'p-button-success',
          accept: async () => {
            await declencherSauvegarde(true);
          },
          reject: () => {
            isSaving.value = false;
          }
        });
      } else {
        await declencherSauvegarde(true);
      }
    } catch (error) {
      console.error('Erreur dans onActivatePlan:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible d\'activer le plan.', life: 5000 });
      isSaving.value = false;
    }
  };

  const onEditorSubmit = async () => {
    if (isSaving.value) return;
    isSaving.value = true;

    try {
      if (!isEditMode.value && planId.value !== 'nouveau') {
        await onWizardGenerate();
        isSaving.value = false;
        return;
      }

      if (isArchived.value) {
        await mettreANiveauArchive();
        isSaving.value = false;
        return;
      }

      if (!validerSaisieValeurs()) {
        isSaving.value = false;
        return;
      }
      if (!validerLegendeMoyens()) {
        isSaving.value = false;
        return;
      }

      if (plan.value?.statut === 'ACTIF') {
        versioningMode.value = 'new-version';
        showVersioningDialog.value = true;
        // Ne pas laisser le bouton principal en état "isSaving" pendant que la boîte de versioning est affichée
        // L'utilisateur doit cliquer sur "Publier la Nouvelle Version" dans la boîte pour lancer l'opération.
        isSaving.value = false;
      } else {
        await declencherSauvegarde();
      }
    } catch (error) {
      console.error(error);
    } finally {
      // Si on affiche la boîte de versioning, on laisse le flag `isSaving` à true
      // pour que le composant affiche l'état en cours jusqu'à confirmation.
      if (!showVersioningDialog.value) {
        isSaving.value = false;
      }
    }
  };

  const mettreANiveauArchive = async () => {
    try {
      const res = await upgradePlan(planId.value);
      const newId = res.data.planId;
      toast.add({ severity: 'success', summary: 'Mise à niveau réussie', detail: 'Le plan a été mis à niveau et est maintenant en mode brouillon.', life: 4000 });
      router.push(`/dev/fab/plans/editer/${newId}`);
      setTimeout(() => window.location.reload(), 100);
    } catch (error) {
      console.error('Erreur mise à niveau:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Impossible de mettre à niveau le plan.', life: 6000 });
    }
  };

  const restaurerArchive = async (motif) => {
    try {
      await restaurerPlan({
        planArchiveId: planId.value,
        restaurePar: 'ADMIN',
        motifRestoration: motif
      });

      toast.add({ severity: 'success', summary: 'Plan restauré', detail: 'Le plan a été réactivé avec succès.', life: 4000 });
      isExitingEditor.value = true;
      router.push('/dev/hub-plans');
    } catch (error) {
      console.error('Erreur restauration:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Une erreur est survenue lors de la restauration.', life: 4000 });
    }
  };

  const onVersioningConfirm = async (motif) => {
    isVersioningSaving.value = true;
    showVersioningDialog.value = false;

    try {
      if (versioningMode.value === 'new-version') {
        const newVersionPlan = await creerNouvelleVersionPlan({
          ancienId: planId.value,
          modifiePar: 'ADMIN',
          motifModification: motif || 'Modification de la structure du plan',
          codeArticleSage: plan.value?.codeArticleSage
        });
        const newPlanId = newVersionPlan.id || newVersionPlan.planId || newVersionPlan.data?.id || newVersionPlan.data?.planId;
        const clonedPlanRes = await fabPlanService.getPlanById(newPlanId);
        const dataPlan = clonedPlanRes?.data?.data || clonedPlanRes?.data || clonedPlanRes;
        syncIdsFromDb(dataPlan);
        await enregistrerValeurs(newPlanId, false, true);
      } else if (versioningMode.value === 'restore') {
        await restaurerArchive(motif);
      }
    } catch (error) {
      console.error('Erreur versioning:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Une erreur est survenue lors de l\'opération de versioning.', life: 4000 });
    } finally {
      isVersioningSaving.value = false;
      isSaving.value = false;
      if (versioningMode.value === 'new-version') {
        toast.add({ severity: 'success', summary: 'Nouvelle version publiée', detail: 'Le plan a été archivé et une nouvelle version ACTIF a été créée.', life: 5000 });
        isExitingEditor.value = true;
        router.push('/dev/hub-plans');
      }
    }
  };

  // Si l'utilisateur ferme la boîte de versioning sans confirmer, réinitialiser les flags
  watch(showVersioningDialog, (visible) => {
    if (!visible) {
      isVersioningSaving.value = false;
      isSaving.value = false;
    }
  });
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
