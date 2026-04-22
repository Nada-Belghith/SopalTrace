<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <Toast position="top-right" />
    <ConfirmDialog />

    <VersioningDialog :visible="showVersioningDialog"
                      :mode="versioningMode"
                      :is-loading="isVersioningSaving"
                      @confirm="onVersioningConfirm"
                      @cancel="showVersioningDialog = false"
                      @update:visible="showVersioningDialog = $event" />

    <div class="max-w-[1600px] mx-auto">
      <div class="animate-in fade-in zoom-in-95 duration-500">

        <EditorHeader
          :title="headerTitle"
          :subtitle="headerSubtitle"
          :code="codeAffiche"
          code-label="Identifiant PF"
          :statut="statut"
          :is-edit-mode="isEditMode"
          @close="onCloseEditor"
        />

        <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden mb-6">
          <div class="p-6 md:p-8">
            <PfHeader :is-read-only="isReadOnly" />
          </div>
        </div>

        <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
          <div class="bg-[#1e293b] text-white px-5 py-4 flex justify-between items-center">
            <div class="flex items-center gap-3 font-bold tracking-wide text-sm">
              <i class="pi pi-sliders-v text-blue-400"></i> Éditeur de Structure
            </div>
          </div>

          <div v-if="isLoadingData" class="py-20 text-center text-blue-500">
            <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
            <p class="text-xs font-black uppercase tracking-widest">Chargement...</p>
          </div>

          <div v-else class="p-6 md:p-8">
            <template v-if="sections.length === 0">
              <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
                Cliquez sur "Créer une nouvelle section" pour commencer.
              </div>
            </template>

            <div v-else class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <FabTableHeader :columns="modeleColumns" />
                <PfSectionCard 
                  v-for="(section, index) in sections" 
                  :key="section.id" 
                  :groupe="section" 
                  :index="index"
                  :is-read-only="isReadOnly"
                  @remove="supprimerSection(section.id)"
                  @update-groupe="(updatedSection) => mettreAJourSection(index, updatedSection)"
                  @section-type-required="() => toast.add({ severity: 'warn', summary: 'Nature requise', detail: 'Veuillez définir la nature de la section.', life: 4000 })"
                >
                  <FabLigneControl 
                    v-for="ligne in section.lignes" 
                    :key="ligne.id" 
                    :ligne="ligne"
                    :is-read-only="isReadOnly"
                    :operation-code="'PF'"
                    @remove="(ligneId) => supprimerLigneASection(index, ligneId)"
                    @update="(updatedLigne) => mettreAJourLigne(index, updatedLigne)"
                  />
                </PfSectionCard>
              </table>
            </div>

            <div class="mt-2" v-if="!isReadOnly">
              <button @click="ajouterSection" class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
                <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
              </button>
            </div>
          </div>

          <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end">
            <template v-if="isForcedView">
              <button @click="onCloseEditor" class="px-6 py-3 bg-slate-500 text-white rounded-lg hover:bg-slate-600 flex items-center gap-2 shadow-sm font-bold">
                <i class="pi pi-times"></i> Fermer
              </button>
            </template>
            <template v-else>
              <EditorActions :label="editorLabel"
                             loading-label="Traitement..."
                             :icon="editorIcon"
                             :variant="editorVariant"
                             :is-loading="isSaving && !showVersioningDialog"
                             @submit="onEditorSubmit"
                             @cancel="onCloseEditor" />
            </template>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';

import { usePfPlanStore } from '@/stores/pfPlanStore';
import { useEditorSections } from '@/composables/useEditorSections';
import { useEditorValidation } from '@/composables/useEditorValidation';

import EditorHeader from '@/components/Shared/EditorHeader.vue';
import PfHeader from '@/components/ProduitFini/PfHeader.vue';
import PfSectionCard from '@/components/ProduitFini/PfSectionCard.vue';
import FabLigneControl from '@/components/Fabrication/FabLigneControl.vue';
import FabTableHeader from '@/components/Fabrication/FabTableHeader.vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import ConfirmDialog from 'primevue/confirmdialog';
import Toast from 'primevue/toast';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const confirm = useConfirm();
const store = usePfPlanStore();

const planId = ref(route.params.id === 'nouveau' ? null : route.params.id);
const isForcedView = ref(route.query.view === 'true');
const isLoadingData = ref(false);
const isSaving = ref(false);
const isVersioningSaving = ref(false);
const showVersioningDialog = ref(false);
const versioningMode = ref('PF');

const {
  sections,
  ajouterSection,
  supprimerSection,
  mettreAJourSection,
  ajouterLigneASection,
  supprimerLigneASection,
  mettreAJourLigne
} = useEditorSections();

const dummyLegende = ref('');
const { validerSaisiePlan: validerSaisieValeurs } = useEditorValidation(sections, dummyLegende, toast);

// ============================================================================
// COLONNES RÉUTILISABLES
// ============================================================================
const modeleColumns = [
  { label: 'Caractéristique contrôlée', width: 'w-[22%]' },
  { label: 'Limite spécif.', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Type de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { label: 'Moyen de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { label: 'Code instrument', width: 'w-[15%]', textAlign: 'center' },
  { label: 'Observations', width: 'flex-1' },
  { label: '', width: 'w-12', textAlign: 'center' }
];

const isEditMode = computed(() => !!planId.value);
const isArchived = computed(() => store.entete?.statut === 'ARCHIVE');
const isReadOnly = computed(() => isForcedView.value || isArchived.value);

const statut = computed(() => {
  if (!isEditMode.value) return 'BROUILLON';
  return store.entete?.statut || 'ACTIF';
});

const codeAffiche = computed(() => {
  const v = isEditMode.value ? (store.entete.version + 1) : (store.entete?.version || 1);
  
  if (store.entete?.codeArticleSage) {
    const baseSage = store.entete.codeArticleSage.replace(/-V\d+$/i, '');
    return `${baseSage}-V${v}`;
  }
  
  if (store.entete?.typeRobinetCode) {
    return `${store.entete.typeRobinetCode}-PF-V${v}`;
  }
  
  return `Nouveau-PF-V${v}`;
});

const headerTitle = computed(() => {
  if (isForcedView.value) return 'Consultation du Plan PF';
  if (!isEditMode.value) return "Création d'un plan Produit Fini";
  if (isArchived.value) return "Archive Produit Fini";
  return 'Édition du Plan Produit Fini';
});

const headerSubtitle = computed(() => {
  if (isForcedView.value) return 'Mode lecture seule.';
  if (!isEditMode.value) return "Sélectionnez un type de robinet et configurez les sections de contrôle.";
  if (isArchived.value) return "Vous consultez une archive.";
  return "Modifiez la structure. Ce plan est générique par type de robinet.";
});

const editorLabel = computed(() => {
  if (isArchived.value) return 'Restaurer ce Plan';
  if (isEditMode.value) return 'Créer une Nouvelle Version';
  return 'Enregistrer le Plan';
});

const editorIcon = computed(() => {
  if (isArchived.value) return 'pi pi-history';
  if (isEditMode.value) return 'pi pi-save';
  return 'pi pi-check';
});

const editorVariant = computed(() => {
  if (isArchived.value) return 'warning';
  if (isEditMode.value) return 'primary';
  return 'primary';
});

onMounted(async () => {
  if (!store.isDicosLoaded) {
    await store.fetchDictionnaires();
  }

  if (planId.value) {
    isLoadingData.value = true;
    try {
      await store.getPlan(planId.value);
      // Synchroniser le state local des sections
      sections.value = JSON.parse(JSON.stringify(store.sections));
    } catch (error) {
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger le plan PF', life: 3000 });
      router.push('/dev/hub');
    } finally {
      isLoadingData.value = false;
    }
  } else {
    // Mode Nouveau
    store.entete = {
      typeRobinetCode: '',
      designation: '',
      commentaireVersion: '',
      version: 1,
      statut: 'ACTIF'
    };
    sections.value = [];
    ajouterSection();
  }
});

const onCloseEditor = () => {
  router.push('/dev/hub');
};

const onEditorSubmit = async () => {
  if (isArchived.value) {
    // Si c'est un plan archivé, on propose de restaurer la version
    showVersioningDialog.value = true;
    return;
  }
  
  if (isEditMode.value) {
    if (!validerSaisieValeurs()) return;
    versioningMode.value = isArchived.value ? 'restore' : 'new-version';
    showVersioningDialog.value = true;
  } else {
    // Mode Nouveau
    if (!store.entete.typeRobinetCode) {
      toast.add({ severity: 'warn', summary: 'Champs requis', detail: 'Le type de robinet est obligatoire.', life: 3000 });
      return;
    }
    
    if (!validerSaisieValeurs()) return;

    isSaving.value = true;
    try {
      store.sections = sections.value;
      const newId = await store.createPlan();
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Plan créé et activé.', life: 3000 });
      router.push('/dev/hub');
    } catch (error) {
      toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Erreur lors de la sauvegarde.', life: 3000 });
    } finally {
      isSaving.value = false;
    }
  }
};

const onVersioningConfirm = async ({ type, motif }) => {
  showVersioningDialog.value = false;
  isVersioningSaving.value = true;
  try {
    store.sections = sections.value;
    
    let newId;
    if (isArchived.value) {
      // Cas Restauration : on appelle l'endpoint dédié
      newId = await store.restaurerPlan(motif);
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Version restaurée et activée.', life: 3000 });
    } else {
      // Cas Nouvelle Version (depuis un plan Actif)
      newId = await store.creerNouvelleVersion(motif);
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Nouvelle version activée.', life: 3000 });
    }
    
    router.push(`/dev/hub`);

  } catch (error) {
    const action = isArchived.value ? 'la restauration' : 'le versioning';
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || `Erreur lors de ${action}.`, life: 3000 });
  } finally {
    isVersioningSaving.value = false;
  }
};
</script>
