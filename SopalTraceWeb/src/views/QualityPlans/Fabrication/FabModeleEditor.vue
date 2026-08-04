<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <div class="max-w-[1600px] mx-auto">
      <PlanHeader 
        :id="modeleEditionId"
        :title="headerTitle"
        :subtitle="headerSubtitle"
        icon="pi pi-cog"
        iconColorClass="text-amber-500"
        :is-read-only="isReadOnly"
        :version="isArchiveEditing ? version + 1 : version"
        :statut="isArchiveEditing ? 'BROUILLON' : statut"
        :is-restoring="isLoading"
        :show-restaurer-btn="!isArchiveEditing && !hasActiveVersion"
        @restaurer="handleEditorSubmit"
      >
        <template #actions>
          <div class="flex items-center gap-2 bg-slate-50 px-3 py-1.5 rounded-lg border border-slate-200 ml-4 hidden md:flex">
            <span class="text-[10px] font-black text-slate-400 uppercase">Code:</span>
            <span class="font-mono font-bold text-sm text-slate-700">{{ codeAffiche }}</span>
          </div>
        </template>
      </PlanHeader>
      
      <div class="bg-white rounded-xl shadow-xl border border-slate-200 overflow-hidden">
        <div class="bg-[#1e293b] text-white px-5 py-3.5 flex justify-between items-center">
          <div class="flex items-center gap-3 font-bold tracking-wide">
            <i :class="isReadOnly ? 'pi pi-eye' : 'pi pi-book'" class="text-lg"></i>
            {{ isReadOnly ? 'Visualisation du modèle' : 'Éditeur de Structure' }}
            <span v-if="isForcedView" class="bg-blue-500/20 text-blue-300 px-2 py-0.5 rounded text-xs ml-2 uppercase tracking-widest border border-blue-400/30">Mode Lecture</span>
          </div>
          <button @click="$router.push(returnUrl)" class="text-slate-400 hover:text-white transition-colors">
            <i class="pi pi-times text-lg"></i>
          </button>
        </div>

        <div class="p-6 md:p-8">
          <div class="flex flex-col md:flex-row items-center justify-between p-6 bg-slate-50 border-b border-slate-200">
            <div class="flex-1 w-full md:w-auto">
              <FabModeleHeader :is-edit-mode="isEditMode" :is-read-only="isReadOnly" />
            </div>
          </div>

          <div class="mb-4 mt-6 flex items-center justify-between">
            <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest">2. Structure des lignes de controle</h3>
          </div>

          <FabModelEmptyState 
            :has-valid-structure="hasValidStructure"
            :groupes-length="groupes.length"
          />
          
          <template v-if="hasValidStructure">
            <!-- Mode LECTURE -->
            <div v-if="isReadOnly" class="p-4 md:p-6">
              <PlanReadView
                :sections="groupes"
                :remarques="store.entete.notes"
                :legende-moyens="store.entete.legendeMoyens"
                :configuration-colonnes="store.effectiveConfigurationColonnes"
                :types-section="refStore.typesSection || []"
                :types-caracteristique="refStore.typesCaracteristique || []"
                :types-controle="refStore.typesControle || []"
                :moyens-controle="refStore.moyensControle || []"
                :periodicites="refStore.periodicites || []"
              />
            </div>

            <!-- Mode EDITION -->
            <div v-else class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <BaseTableHeader :columns="modeleColumns" />
                
                <PlanSection 
                    v-for="(section, index) in groupes" 
                    :key="section.id" 
                    :section="section" 
                    :index="index"
                    :columns="modeleColumns"
                    :is-read-only="isReadOnly"
                    :types-section="refStore.typesSection"
                    :periodicites="refStore.periodicites"
                    :regles-echantillonnage="refStore.reglesEchantillonnage"
                    :types-controle="refStore.typesControle"
                    :moyens-controle="refStore.moyensControle"
                    :instruments="refStore.instruments"
                    :operation-code="store.entete.operationCode"
                    defaultTitle="Caractéristiques à contrôler"
                    @remove="removeSection"
                    @update-section="(updatedSection) => updateSection(index, updatedSection)"
                    @section-type-required="() => toast.add({ severity: 'warn', summary: 'Type de section requis', detail: 'Veuillez définir la nature de la section avant d\'ajouter une ligne.', life: 4000 })"
                  />
              </table>
            </div>
              
            <div class="mt-2" v-if="!isReadOnly">
              <button @click="addSection" class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
                <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
              </button>
            </div>
          </template>

          <!-- Notes & Légende en mode éditeur uniquement -->
          <div v-if="!isReadOnly" class="mt-2">
            <RemarquesLegendeBox 
              v-model:remarques="store.entete.notes"
              v-model:legendeMoyens="store.entete.legendeMoyens"
              :show-validation="showLegendValidation"
              :has-custom-instruments="hasCustomInstrumentsGlobal"
              :is-read-only="isReadOnly"
            />
          </div>
        </div>

        <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end" v-if="!isForcedView">
          <EditorActions 
            :label="actionButtonLabel"
            :icon="actionButtonIcon"
            :variant="actionButtonVariant"
            :is-loading="isLoading"
            @submit="handleEditorSubmitClick"
            @cancel="() => $router.push(returnUrl)"
          />
        </div>
      </div>
    </div>
    
    <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
    />
    <ConfirmDialog></ConfirmDialog>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';

import { useFabModeleStore } from '@/stores/fabModeleStore';
import { useReferentielStore } from '@/stores/referentielStore';
import { useDirtyChecking } from '@/composables/useDirtyChecking';
import { createModeleSnapshot } from '@/utils/modelMapper';
import { useActivePlanConfirmation } from '@/composables/useActivePlanConfirmation';
import { useEditorSections } from '@/composables/useEditorSections';
import { useEditorValidation } from '@/composables/useEditorValidation';

import { useModelLoader } from '@/composables/useModelLoader';
import { useModelSaver } from '@/composables/useModelSaver';
import { useModelHeaderMeta } from '@/composables/useModelHeaderMeta';

import PlanHeader from '@/components/Shared/PlanHeader.vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import PlanReadView from '@/components/Shared/PlanReadView.vue';
import FabModeleHeader from '@/components/Fabrication/FabModeleHeader.vue';
import BaseTableHeader from '@/components/Shared/BaseTableHeader.vue';
import PlanSection from '@/components/Shared/PlanSection.vue';
import FabModelEmptyState from '@/components/Fabrication/FabModelEmptyState.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import ConfirmDialog from 'primevue/confirmdialog';

const store = useFabModeleStore();
const refStore = useReferentielStore();
const toast = useToast();
const route = useRoute();
const router = useRouter();
const { confirmArchivagePlanActif } = useActivePlanConfirmation();
const { isDirty, updateCurrentSnapshot, initializeSnapshot } = useDirtyChecking();

const returnUrl = computed(() => '/dev/fab/modeles');

// ============================================================================
// ÉTAT LOCAL (Métier)
// ============================================================================
const { 
  sections, 
  ajouterSection: addSection, 
  supprimerSection: removeSection, 
  mettreAJourSection: updateSection, 
  supprimerLigneASection: removeLineFromSection, 
  mettreAJourLigne: updateLine 
} = useEditorSections();

const groupes = sections;

const modeleEditionId = ref(null);
const codeOriginal = ref('');
const statut = ref('BROUILLON');
const version = ref(0);
const hasActiveVersion = ref(false);
const showColumnModal = ref(false);

const isArchiveEditing = ref(route.query.draft === 'true' || route.query.upgrade === 'true');
const isUpgradeMode = computed(() => route.query.upgrade === 'true');
const isForcedView = computed(() => route.query.view === 'true');
const isArchived = computed(() => statut.value === 'ARCHIVE');
const isEditMode = computed(() => !!modeleEditionId.value);
const isReadOnly = computed(() => (isEditMode.value && isArchived.value && !isArchiveEditing.value && !isUpgradeMode.value) || isForcedView.value);

const isLoading = computed(() => store.isLoading);

// ============================================================================
// REGROUPEMENT DE L'ÉTAT (Contexte pour les composables)
// ============================================================================
const context = {
  modeleEditionId,
  codeOriginal,
  statut,
  version,
  hasActiveVersion,
  isArchiveEditing,
  isUpgradeMode,
  isForcedView,
  isArchived,
  isEditMode,
  isReadOnly,
  isLoading,
  groupes,
  isDirty,
  returnUrl,
  initializeSnapshot,
  route
};

const { loadModelForEditing, resetForNewModel } = useModelLoader(store, context);
const { onEditorSubmit, onEditorSubmitClick } = useModelSaver(store, context);
const { codeAffiche, headerTitle, headerSubtitle, actionButtonLabel, actionButtonIcon, actionButtonVariant } = useModelHeaderMeta(store, context);

const { 
  showLegendValidation, 
  hasCustomInstrumentsGlobal, 
  validerLegendeMoyens, 
  validerSaisieValeurs 
} = useEditorValidation(groupes, computed(() => store.entete.legendeMoyens), toast);

// Watchers
watch(
  [() => store.entete, () => groupes.value],
  ([newEntete, newGroupes]) => {
    if (!isForcedView.value) {
      const enteteClone = JSON.parse(JSON.stringify(newEntete));
      const groupesClone = JSON.parse(JSON.stringify(newGroupes));
      updateCurrentSnapshot(createModeleSnapshot(enteteClone, groupesClone));
    }
  },
  { deep: true }
);

// Variables métier diverses
// hasValidStructure = true dès que les dictionnaires sont chargés
// (peu importe le statut du formulaire PRC)
const hasValidStructure = computed(() => refStore.isDicosFabricationLoaded);
const modeleColumns = computed(() => store.tableColumns);

const handleEditorSubmit = () => onEditorSubmit(toast, router, validerSaisieValeurs, validerLegendeMoyens);
const handleEditorSubmitClick = () => onEditorSubmitClick(toast, router, confirmArchivagePlanActif, validerSaisieValeurs, validerLegendeMoyens);

onMounted(async () => {
  try {
    // Charger les dictionnaires EN PREMIER (avant tout le reste, car resetForNewModel en dépend)
    if (!refStore.isDicosFabricationLoaded) {
      await refStore.fetchDictionnaires('fabrication');
    }
    if (!refStore.formulairesReferencesByRole['EN_COURS_DE_FABRICATION']?.length) {
      await refStore.fetchFormulairesReferences('EN_COURS_DE_FABRICATION');
    }

    if (!route.params.id || route.params.id === 'nouveau') {
      resetForNewModel();
      if (groupes.value.length === 0) addSection();
    }
    
    if (route.params.id && route.params.id !== 'nouveau') {
      await loadModelForEditing(route.params.id, toast, router);
    }
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur réseau', detail: error.message, life: 5000 });
  }
});

</script>
