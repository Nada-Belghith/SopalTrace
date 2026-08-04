<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <Toast position="top-right" />
    <ConfirmDialog />

    <div class="max-w-[1600px] mx-auto">
      <PlanHeader 
        :id="modeleEditionId"
        :title="headerTitle"
        :subtitle="headerSubtitle"
        icon="pi pi-cog"
        iconColorClass="text-amber-500"
        :is-read-only="isReadOnly"
        :version="version"
        :statut="statut"
        :showRestaurerBtn="false"
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
              <assPlanHeader :is-edit-mode="isEditMode" :is-read-only="isReadOnly">
                <template #actions>
                  <button v-if="!isReadOnly" @click="() => fileInput.click()" class="px-4 py-2 bg-[#059669] text-white hover:bg-[#047857] rounded-lg border border-[#059669] text-xs font-bold flex items-center gap-2 transition-colors shadow-sm">
                    <i class="pi pi-file-excel"></i>
                    <span>Importer la structure Excel</span>
                  </button>
                  <button v-if="!isReadOnly" @click="showColumnModal = true" class="text-xs font-bold px-4 py-2 bg-[#0f172a] text-white rounded-lg border border-[#0f172a] hover:bg-[#1e293b] transition-colors flex items-center gap-2 shadow-sm ml-2">
                    <i class="pi pi-sliders-h text-sm"></i>
                    <span>Configurer Colonnes</span>
                  </button>
                  <input type="file" ref="fileInput" @change="onFileSelected" accept=".xlsx,.xls" class="hidden" />
                </template>
              </assPlanHeader>
            </div>
          </div>

          <div class="mb-4 flex items-center justify-between">
            <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mt-6">2. Structure des lignes de contrôle</h3>
          </div>

          <template v-if="groupes.length === 0">
            <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
              Cliquez sur "Créer une nouvelle section" pour commencer.
            </div>
          </template>

          <!-- Mode LECTURE -->
          <div v-if="isReadOnly" class="p-4 md:p-6">
            <PlanReadView
              :sections="groupes"
              :remarques="store.entete.notes"
              :legende-moyens="store.entete.legendeMoyens"
              :configuration-colonnes="store.entete.configurationColonnes"
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
                :operation-code="store.entete?.operationCode"
                defaultTitle="Caractéristiques à contrôler"
                @remove="supprimerGroupe(section.id)"
                @update-section="(updatedGroupe) => mettreAJourGroupe(index, updatedGroupe)"
                @section-type-required="() => toast.add({ severity: 'warn', summary: 'Type de section requis', detail: 'Veuillez définir la nature de la section avant d\'ajouter une ligne.', life: 4000 })"
              />
            </table>
          </div>
          
          <div class="mt-2" v-if="!isReadOnly">
            <button @click="ajouterGroupe" class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
              <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
            </button>
          </div>

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
          <DocumentSaveManager 
             :plan-id="modeleEditionId"
             :statut="statut"
             :is-loading="isLoading"
             :is-read-only="isReadOnly"
             @save-direct="handleSaveDirect"
             @save-correction="handleSaveCorrection"
             @save-new-version="handleSaveNewVersion"
             @cancel="() => $router.push(returnUrl)"
          />
        </div>
      </div>
    </div>
    
    <!-- MODAL DE CONFIGURATION DES COLONNES -->
    <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAssPlanStore } from '@/stores/assPlanStore';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import DocumentSaveManager from '@/components/Shared/DocumentSaveManager.vue';
import { useActivePlanConfirmation } from '@/composables/useActivePlanConfirmation';

import { documentService as assPlanService } from '@/services/documentService';
import { planFabricationService as fabPlanService } from '@/services/planFabricationService';
import { createModeleSnapshot } from '@/utils/modelMapper';
import { prepareSectionsForBackend } from '@/utils/sectionUtils';
import { parseFrequenceLibelle, resolveFrequencyFromPeriodiciteId } from '@/utils/frequencyUtils';

import PlanHeader from '@/components/Shared/PlanHeader.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import PlanReadView from '@/components/Shared/PlanReadView.vue';
import assPlanHeader from '@/components/Assemblage/assPlanHeader.vue';
import BaseTableHeader from '@/components/Shared/BaseTableHeader.vue';
import PlanSection from '@/components/Shared/PlanSection.vue';
import PlanSectionHeader from '@/components/Shared/PlanSectionHeader.vue';
import { useReferentielStore } from '@/stores/referentielStore';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';

import { useEditorSections } from '@/composables/useEditorSections';
import { useEditorValidation } from '@/composables/useEditorValidation';
import { useDirtyChecking } from '@/composables/useDirtyChecking';

const store = useAssPlanStore();
const refStore = useReferentielStore();
const toast = useToast();
const route = useRoute();
const router = useRouter();
const { confirmArchivagePlanActif } = useActivePlanConfirmation();

const returnUrl = computed(() => '/dev/hub');
const isSaving = ref(false);

// ============================================================================
// ÉTAT LOCAL (Métier)
// ============================================================================
const { 
  sections, 
  ajouterSection: ajouterGroupe, 
  supprimerSection: supprimerGroupe, 
  mettreAJourSection: mettreAJourGroupe, 
  supprimerLigneASection, 
  mettreAJourLigne 
} = useEditorSections();

const groupes = sections;

const modeleEditionId = ref(null);
const codeOriginal = ref('');
const statut = computed(() => store.entete?.statut || 'BROUILLON');
const version = ref(0);
const showColumnModal = ref(false);
const isArchiveEditing = ref(route.query.draft === 'true');
const isUpgradeMode = computed(() => route.query.upgrade === 'true');

const { 
  showLegendValidation, 
  hasCustomInstrumentsGlobal, 
  validerLegendeMoyens, 
  validerSaisieValeurs 
} = useEditorValidation(groupes, computed(() => store.entete.legendeMoyens), toast);

const { isDirty, updateCurrentSnapshot, initializeSnapshot } = useDirtyChecking();

// 👁️ NOUVEAU : DÉTECTION DU MODE LECTURE SEULE DEPUIS L'URL
// isForcedView is now provided by useDocumentHeaderMeta

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

// ============================================================================
// COLONNES RÉUTILISABLES ET DYNAMIQUES
// ============================================================================
const baseModeleColumns = [
  { key: 'caracteristique', label: 'Caractéristique contrôlée', width: 'w-[22%]' },
  { key: 'limite_spec', label: 'Limite spécif.', width: 'w-[12%]', textAlign: 'center' },
  { key: 'type_controle', label: 'Type de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { key: 'moyen_controle', label: 'Moyen de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { key: 'code_instrument', label: 'Code instrument', width: 'w-[15%]', textAlign: 'center' },
  { key: 'observations', label: 'Observations', width: 'flex-1' }
];

const modeleColumns = computed(() => {
  let cols = [...baseModeleColumns];
  const customCols = store.entete.configurationColonnes || [];
  
  customCols.forEach(cc => {
    const insertIdx = cols.findIndex(c => c.key === cc.insertAfter);
    const newCol = { key: cc.key, label: cc.label, width: 'w-[12%]', textAlign: 'center', isCustom: true };
    if (insertIdx !== -1) {
      cols.splice(insertIdx + 1, 0, newCol);
    } else {
      cols.push(newCol);
    }
  });

  cols.push({ key: 'actions', label: '', width: 'w-12', textAlign: 'center' });
  
  return cols;
});

import { useDocumentHeaderMeta } from '@/composables/useDocumentHeaderMeta';
import { useDocumentSaveManager } from '@/composables/useDocumentSaveManager';

const isLoading = computed(() => store.isLoading || isSaving.value);

const isEditMode = computed(() => !!modeleEditionId.value);
const { isReadOnly, isArchived, isForcedView } = useDocumentHeaderMeta(route, store);

const codeAffiche = computed(() => {
  if (isEditMode.value && codeOriginal.value) {
    if (isReadOnly.value) return codeOriginal.value;
    return `${codeOriginal.value.replace(/(?:[-\s]+V\d+)+$/i, '')}-V${version.value + 1}`;
  }
  return store.entete.code || store.codeModeleAuto;
});

const headerTitle = computed(() => {
  const nature = store.entete.natureComposantCode;
  const famille = store.entete.familleProduitCode;
  const poste = store.entete.posteCode;
  if (isForcedView.value) return 'Consultation du Plan d\'Assemblage';
  if (nature === 'PISTON') return "Plan en cours d'assemblage PISTON";
  if (nature === 'PF') {
    let title = "Plan en cours d'assemblage PF";
    if (famille) title += ` - ${famille}`;
    const famObject = store.famillesProduit?.find(f => f.code === famille);
    const isSoupape = famObject?.libelle?.toLowerCase().includes('soupape');
    if (isSoupape && poste) title += ` - Poste ${poste}`;
    return title;
  }
  if (nature === 'CORPS' || nature === 'VOLANT') return `Plan d'assemblage ${nature}`;
  if (isEditMode.value) return isArchived.value ? 'Mise à jour d\'Archive' : `Édition du Plan Générique`;
  return 'Création d\'un Plan Générique';
});

const headerSubtitle = computed(() => {
  if (isForcedView.value) return 'Mode lecture seule (Aperçu de la structure).';
  if (isEditMode.value) {
    return isArchived.value 
      ? 'Vous consultez une archive. Mettre à jour créera une nouvelle version en brouillon.'
      : 'Modifiez la structure. L\'ancienne version sera archivée automatiquement.';
  }
  return 'Configurez la structure des plans du contrôle.';
});

const fileInput = ref(null);

const onFileSelected = async (event) => {
  const file = event.target.files[0];
  if (!file) return;

  event.target.value = '';
  try {
    store.isLoading = true;
    await store.importerDepuisExcel(file);
    groupes.value = JSON.parse(JSON.stringify(store.sections));
    toast.add({ severity: 'success', summary: 'Import réussi', detail: 'Les données ont été chargées.', life: 4000 });
    updateCurrentSnapshot(createModeleSnapshot(store.entete, store.sections));
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur d\'import', detail: 'Impossible de lire le fichier.', life: 4000 });
  } finally {
    store.isLoading = false;
  }
};

onMounted(async () => {
  try {
    if (!refStore.isDicosFabricationLoaded) {
      await refStore.fetchDictionnaires('fabrication');
    }
    if (!refStore.formulairesReferencesByRole['EN_COURS_DE_ASSEMBLAGE']?.length) {
      await refStore.fetchFormulairesReferences('EN_COURS_DE_ASSEMBLAGE');
    }
    if (!route.params.id || route.params.id === 'nouveau') {
      resetForNewPlan();
      if (groupes.value.length === 0) ajouterGroupe();
    }
    if (route.params.id && route.params.id !== 'nouveau') {
      await chargerModelePourEdition(route.params.id);
    }
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur réseau', detail: error.message, life: 5000 });
  }
});

const chargerModelePourEdition = async (id) => {
  store.isLoading = true;
  try {
    const res = await assPlanService.getById(id);
    const data = res?.data?.data || res?.data || res;
    modeleEditionId.value = data.id;
    codeOriginal.value = data.nom;
    version.value = data.version;
    store.entete.code = data.nom;
    store.entete.statut = data.statut;
    store.entete.operationCode = data.operationCode;
    store.entete.natureComposantCode = data.natureArticleCode;
    store.entete.typeRobinetCode = data.libre1;
    store.entete.libelle = data.designation;
    store.entete.notes = data.remarques || '';
    store.entete.legendeMoyens = data.legendeMoyens || '';
    store.entete.posteCode = data.posteCode || '';
    store.entete.familleProduitCode = data.familleProduitFiniCode || '';
    
    const sortedSections = [...(data.sections || [])].sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0));
    groupes.value = sortedSections.map(sec => ({
      ...sec,
      lignes: [...(sec.lignes || [])].sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0))
    }));
    
    if (!isForcedView.value) {
      setTimeout(() => initializeSnapshot(createModeleSnapshot(store.entete, groupes.value)), 100);
    }
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Introuvable', detail: 'Modèle introuvable.', life: 5000 });
  } finally {
    store.isLoading = false;
  }
};

const preparerDonneesEtFrequences = async () => {
  await prepareSectionsForBackend(
    groupes.value,
    refStore.periodicites,
    async (payloadFreq) => {
      const res = await fabPlanService.createPeriodicite(payloadFreq);
      refStore.periodicites.push({ id: res.data.periodiciteId || res.data.id, ...payloadFreq });
      return res;
    }
  );
  return groupes.value;
};

const { handleSaveDirect, handleSaveCorrection, handleSaveNewVersion } = useDocumentSaveManager({
  callbacks: {
    validateForm: async () => {
      if (!validerSaisieValeurs()) return false;
      if (!validerLegendeMoyens()) return false;
      return true;
    },
    onSaveDirect: async () => {
      isSaving.value = true;
      store.sections = await preparerDonneesEtFrequences();
      let res;
      if (!modeleEditionId.value) {
          res = await store.savePlan(store.entete.legendeMoyens);
      } else {
          res = await store.updatePlan(modeleEditionId.value, store.entete.legendeMoyens);
      }
      isSaving.value = false;
      return res;
    },
    onSaveCorrection: async () => {
      isSaving.value = true;
      store.sections = await preparerDonneesEtFrequences();
      const res = await store.updatePlan(modeleEditionId.value, store.entete.legendeMoyens);
      isSaving.value = false;
      return res;
    },
    onSaveNewVersion: async (motif) => {
      isSaving.value = true;
      store.sections = await preparerDonneesEtFrequences();
      const resData = await store.createNewVersion(modeleEditionId.value, motif, store.entete.legendeMoyens);
      isSaving.value = false;
      return resData;
    },
    preSaveHook: async () => {
      if (!modeleEditionId.value) {
        const nomPlan = store.entete.code || store.codePlanAuto;
        const cleanName = (name) => name ? name.split('- V')[0].trim() : '';
        const baseNom = cleanName(nomPlan);
        
        try {
          const res = await assPlanService.getByFilters({ 
            typeDocumentCode: 'PLAN_ASS', 
            statut: 'ACTIF',
            natureComposantCode: store.entete.natureComposantCode || undefined,
            operationCode: store.entete.operationCode || undefined,
            posteCode: store.entete.posteCode || undefined
          });
          const plansActifs = Array.isArray(res) ? res : (res?.data || []);
          const planActif = plansActifs.find(p => cleanName(p.nom) === baseNom) || plansActifs[0];
          
          if (planActif) {
            const isConfirmed = await confirmArchivagePlanActif({
              typeDocument: "plan d'assemblage",
              identifiant: `cette famille`, 
              version: planActif.version || 1
            });
            if (!isConfirmed) return false;
            await new Promise(resolve => setTimeout(resolve, 200));
          }
        } catch (err) {
          console.warn("Erreur", err);
        }
      }
      return true;
    }
  },
  toast,
  router,
  returnUrl: returnUrl.value
});

const activerPlanCourant = async () => {
  if (!validerSaisieValeurs()) return;
  if (!validerLegendeMoyens()) return;

  isSaving.value = true;
  try {
    await store.updatePlan(modeleEditionId.value, store.entete.legendeMoyens);
    await Promise.resolve(); // Simulate activation logic if needed
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Le modèle a été activé avec succès.', life: 5000 });
    setTimeout(() => router.push(returnUrl.value), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Erreur lors de l\'activation', life: 6000 });
  } finally {
    isSaving.value = false;
  }
};

const resetForNewPlan = () => {
  modeleEditionId.value = null;
  codeOriginal.value = '';
  version.value = 0;
  store.entete = { 
    operationCode: '', 
    natureComposantCode: '', 
    typeRobinetCode: '', 
    libelle: '', 
    notes: '', 
    legendeMoyens: '', 
    posteCode: '',
    familleProduitCode: '' 
  };
  groupes.value = [];
  
  setTimeout(() => {
    initializeSnapshot(createModeleSnapshot(store.entete, groupes.value));
  }, 100);
};
</script>
