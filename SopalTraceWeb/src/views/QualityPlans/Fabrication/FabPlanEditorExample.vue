<!--
  EXEMPLE SIMPLIFIÉ: FabPlanEditor utilisant les composables centralisés

  Ce composant montre comment utiliser:
  1. usePlanLifecycle: Gestion centralisée du cycle de vie
  2. usePlanAutoSave: Sauvegarde automatique
  3. useEditorSections: Composable métier pour gérer les sections

  Avantages de cette approche:
  - Logique de CRUD centralisée (pas de duplication)
  - Versionning/Archivage géré automatiquement
  - AutoSave transparent
  - Routing/Notifications uniformes
  - Facile à adapter pour d'autres types de plans

  Points clés:
  - Les appels API (load, save, activate, archive) sont dans usePlanLifecycle
  - L'état du formulaire est séparé du cycle de vie (sections, remarques)
  - AutoSave est optionnel et indépendant
-->

<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <ConfirmDialog />
    <Toast position="top-right" />

    <!-- Dialog de versioning/activation (réutilisable) -->
    <VersioningDialog 
      :visible="lifecycle.showVersioningDialog"
      :mode="lifecycle.versioningMode"
      :is-loading="lifecycle.isSaving"
      @confirm="onVersioningConfirm"
      @cancel="lifecycle.showVersioningDialog = false"
      @update:visible="lifecycle.showVersioningDialog = $event"
    />

    <div class="max-w-[1600px] mx-auto">
      <div class="animate-in fade-in zoom-in-95 duration-500">

        <!-- Header uniformisé -->
        <PlanHeader 
          :id="planId"
          :title="headerTitle"
          :subtitle="headerSubtitle"
          icon="pi pi-file-edit"
          icon-color-class="text-blue-500"
          :is-read-only="lifecycle.isReadOnly"
          :version="lifecycle.plan?.version"
          :statut="lifecycle.plan?.statut"
          :is-restoring="lifecycle.isSaving"
          @restaurer="onSubmit"
        />

        <!-- Éditeur principal -->
        <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
          <div class="bg-[#1e293b] text-white px-5 py-4 flex justify-between items-center">
            <div class="flex items-center gap-3 font-bold tracking-wide text-sm">
              <i class="pi pi-sliders-v text-blue-400"></i> Éditeur de Structure
            </div>
            <!-- Indicateur AutoSave (si activé) -->
            <div v-if="autoSave" class="text-xs opacity-75">
              {{ autoSave.getAutoSaveStatus }}
            </div>
          </div>

          <!-- Loader -->
          <div v-if="lifecycle.isLoading" class="py-20 text-center text-blue-500">
            <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
            <p class="text-xs font-black uppercase tracking-widest">Chargement...</p>
          </div>

          <!-- Contenu principal -->
          <div v-else class="p-6 md:p-8">
            <!-- Sections de contrôle -->
            <template v-if="sections.length === 0">
              <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
                Cliquez sur "Créer une nouvelle section" pour commencer.
              </div>
            </template>

            <div v-else class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <thead class="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th class="px-4 py-3 text-xs font-bold text-slate-600 uppercase">Section</th>
                    <th class="px-4 py-3 text-xs font-bold text-slate-600 uppercase">Type</th>
                    <th class="px-4 py-3 text-xs font-bold text-slate-600 uppercase">Fréquence</th>
                    <th class="px-4 py-3 text-xs font-bold text-slate-600 uppercase">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(section, index) in sections" :key="section.id" class="border-b hover:bg-slate-50">
                    <td class="px-4 py-3">{{ section.nom || 'Sans titre' }}</td>
                    <td class="px-4 py-3">{{ section.typeSection || '-' }}</td>
                    <td class="px-4 py-3">{{ section.periodicite || '-' }}</td>
                    <td class="px-4 py-3 flex gap-2">
                      <button 
                        v-if="!lifecycle.isReadOnly"
                        @click="supprimerSection(section.id)"
                        class="text-red-500 hover:text-red-700 text-sm"
                      >
                        <i class="pi pi-trash"></i>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Bouton ajouter section -->
            <div v-if="!lifecycle.isReadOnly" class="mt-4">
              <button 
                @click="ajouterSection"
                class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2"
              >
                <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
              </button>
            </div>

            <!-- Remarques et légende -->
            <RemarquesLegendeBox 
              v-model:remarques="remarques"
              v-model:legendeMoyens="legendeMoyens"
              :is-read-only="lifecycle.isReadOnly"
            />
          </div>

          <!-- Actions au bas -->
          <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end gap-3">
            <!-- Mode brouillon: Save Draft + Activate -->
            <template v-if="lifecycle.isPlanDraft && !lifecycle.isReadOnly">
              <button 
                @click="onSaveDraft"
                :disabled="lifecycle.isSaving"
                class="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 flex items-center gap-2"
              >
                <i v-if="lifecycle.isSaving" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-save"></i>
                Enregistrer Brouillon
              </button>
              <button 
                @click="onActivate"
                :disabled="lifecycle.isSaving"
                class="px-6 py-3 bg-emerald-500 text-white rounded-lg hover:bg-emerald-600 disabled:opacity-50 flex items-center gap-2"
              >
                <i v-if="lifecycle.isSaving" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-check-circle"></i>
                Activer le Plan
              </button>
            </template>

            <!-- Mode actif: Créer nouvelle version -->
            <template v-else-if="lifecycle.isPlanActive && !lifecycle.isReadOnly">
              <button 
                @click="onCreateNewVersion"
                :disabled="lifecycle.isSaving"
                class="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 flex items-center gap-2"
              >
                <i v-if="lifecycle.isSaving" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-save"></i>
                Créer une Nouvelle Version
              </button>
            </template>

            <!-- Mode archivé: Restaurer -->
            <template v-else-if="lifecycle.isPlanArchived && !lifecycle.isReadOnly">
              <button 
                @click="onRestore"
                :disabled="lifecycle.isSaving"
                class="px-6 py-3 bg-orange-500 text-white rounded-lg hover:bg-orange-600 disabled:opacity-50 flex items-center gap-2"
              >
                <i v-if="lifecycle.isSaving" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-history"></i>
                Restaurer ce Plan
              </button>
            </template>

            <!-- Fermer -->
            <button 
              @click="lifecycle.navigateToList"
              class="px-6 py-3 bg-slate-200 text-slate-700 rounded-lg hover:bg-slate-300"
            >
              <i class="pi pi-times"></i> Fermer
            </button>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, onBeforeUnmount } from 'vue';
import { useRoute } from 'vue-router';
import { useToast } from 'primevue/usetoast';

// Services
import { qualityPlansService } from '@/services/qualityPlansService';

// Composables (centralisés)
import { usePlanLifecycle } from '@/composables/usePlanLifecycle';
import { usePlanAutosave } from '@/composables/usePlanAutosave';

// Composables (métier spécifique)
import { useEditorSections } from '@/composables/useEditorSections';

// Composants
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';

// ==================== SETUP ====================

const route = useRoute();
const toast = useToast();
const planId = ref(route.params.id === 'nouveau' ? null : route.params.id);
const isForcedView = ref(route.query.view === 'true');

// Composables centralisés
const lifecycle = usePlanLifecycle(qualityPlansService, planId.value, isForcedView.value);

// Composables métier
const { sections, ajouterSection, supprimerSection } = useEditorSections();

// État du formulaire (séparé du lifecycle)
const remarques = ref('');
const legendeMoyens = ref('');

// AutoSave (optionnel)
const autoSave = ref(null);

// ==================== COMPUTED ====================

const headerTitle = computed(() => {
  if (isForcedView.value) return 'Consultation du Plan';
  if (lifecycle.isPlanNew) return "Création d'un plan de Fabrication";
  if (lifecycle.isPlanDraft) return "Brouillon du plan de Fabrication";
  if (lifecycle.isPlanActive) return 'Édition du Plan de Fabrication';
  if (lifecycle.isPlanArchived) return "Archive du plan de Fabrication";
  return 'Plan de Fabrication';
});

const headerSubtitle = computed(() => {
  if (isForcedView.value) return 'Mode lecture seule.';
  if (lifecycle.isPlanNew) return "Configurez la structure de contrôle.";
  if (lifecycle.isPlanDraft) return "Brouillon - complétez et activez le plan.";
  if (lifecycle.isPlanActive) return "Plan actif - créez une nouvelle version pour modifier.";
  if (lifecycle.isPlanArchived) return "Archive - restaurez pour réactiver.";
  return '';
});

// ==================== LIFECYCLE ====================

onMounted(async () => {
  // Charger le plan si édition
  if (planId.value) {
    await lifecycle.loadPlan(planId.value);

    // Synchroniser l'état du formulaire
    if (lifecycle.plan.value) {
      remarques.value = lifecycle.plan.value.remarques || '';
      legendeMoyens.value = lifecycle.plan.value.legendeMoyens || '';
      // TODO: Charger les sections depuis lifecycle.plan.value
    }
  } else {
    // Créer un nouveau plan
    lifecycle.createNewPlan({
      statut: 'BROUILLON',
      remarques: '',
      legendeMoyens: '',
      sections: []
    });
  }

  // Démarrer AutoSave si plan est brouillon et en édition
  if (lifecycle.isPlanDraft.value && !isForcedView.value) {
    autoSave.value = usePlanAutosave(
      async () => {
        await onAutoSave();
      },
      30000, // 30 secondes
      true // activé
    );
  }
});

onBeforeUnmount(() => {
  // Arrêter AutoSave
  if (autoSave.value) {
    autoSave.value.stopAutoSave();
  }
});

// ==================== HANDLERS ====================

/**
 * Sauvegarde le brouillon (mise à jour)
 */
const onSaveDraft = async () => {
  if (!lifecycle.validateForm()) {
    return;
  }

  try {
    await lifecycle.saveDraft({
      sections: sections.value,
      remarques: remarques.value,
      legendeMoyens: legendeMoyens.value
    });
  } catch (error) {
    console.error('Erreur save draft:', error);
  }
};

/**
 * Prépare l'activation
 */
const onActivate = () => {
  lifecycle.showVersioningConfirm('new-version');
};

/**
 * Crée une nouvelle version depuis un plan actif
 */
const onCreateNewVersion = () => {
  lifecycle.showVersioningConfirm('new-version');
};

/**
 * Restaure un plan archivé
 */
const onRestore = () => {
  lifecycle.showVersioningConfirm('restore');
};

/**
 * Confirme l'activation/versionning/restauration
 */
const onVersioningConfirm = async (motif) => {
  try {
    if (lifecycle.versioningMode.value === 'new-version') {
      // Créer nouvelle version
      await lifecycle.activatePlan({
        motif,
        sections: sections.value,
        remarques: remarques.value,
        legendeMoyens: legendeMoyens.value
      });
    } else if (lifecycle.versioningMode.value === 'restore') {
      // Restaurer archive
      await lifecycle.activatePlan({
        motif,
        sections: sections.value
      });
    }

    lifecycle.navigateToList();
  } catch (error) {
    console.error('Erreur activation:', error);
  }
};

/**
 * Submit principal (utilisé par PlanHeader)
 */
const onSubmit = async () => {
  if (lifecycle.isPlanDraft.value) {
    onActivate();
  } else if (lifecycle.isPlanArchived.value) {
    onRestore();
  }
};

/**
 * AutoSave callback
 */
const onAutoSave = async () => {
  if (lifecycle.isDirty.value && lifecycle.plan.value?.id) {
    await lifecycle.saveDraft({
      sections: sections.value,
      remarques: remarques.value,
      legendeMoyens: legendeMoyens.value
    });
  }
};
</script>

<style scoped>
/* Styles compacts */
</style>
