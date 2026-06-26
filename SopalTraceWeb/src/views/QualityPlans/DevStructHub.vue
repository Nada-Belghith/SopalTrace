<template>
  <div class="max-w-[1400px] mx-auto animate-in fade-in duration-500">
    <Toast position="top-right" />
    <ConfirmDialog></ConfirmDialog>

    <div class="mb-4 md:mb-0">
      <h1 class="text-2xl font-black text-slate-900 tracking-tight flex items-center gap-3">
        <i class="pi pi-th-large text-blue-600"></i>
        Hub des Plans Spécifiques
      </h1>
      <p class="text-slate-500 text-sm mt-1">Visualisez les structures de référence des plans en cours de fabrication.</p>
    </div>

    <!-- ========================================== -->
    <!-- BARRE DE CONTRÔLE : Onglets & Filtres      -->
    <!-- ========================================== -->
    <div class="bg-white p-2 rounded-xl shadow-sm border border-slate-200 mb-8">
      <div class="flex flex-col xl:flex-row justify-between gap-4">

        <!-- Navigation par Onglets (Type de plan) -->
        <div class="flex flex-wrap gap-1 p-1 bg-slate-100/80 rounded-lg">
          <button v-for="tab in tabs" :key="tab.id"
            @click="activeTab = tab.id"
            :class="[
              'px-4 py-2 rounded-md text-sm font-semibold transition-all flex items-center gap-2',
              activeTab === tab.id
                ? 'bg-white text-blue-700 shadow-sm ring-1 ring-slate-900/5'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/50'
            ]">
            <i :class="tab.icon"></i>
            <span class="hidden sm:inline">{{ tab.label }}</span>
            <span class="sm:hidden">{{ tab.short }}</span>
          </button>
        </div>

        <!-- Filtres Rapides -->
        <div class="flex flex-wrap items-center gap-3 px-2">

          <!-- Status Filter -->
          <div class="relative">
            <select v-model="vueActuelle" class="appearance-none bg-slate-50 border border-slate-200 text-slate-700 py-2 pl-3 pr-8 rounded-lg text-sm font-medium focus:outline-none focus:border-blue-500 cursor-pointer">
              <option value="ALL">Tous les statuts</option>
              <option value="ACTIF">Actifs</option>
              <option value="ARCHIVE">Archivés</option>
            </select>
            <i class="pi pi-angle-down absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none text-xs"></i>
          </div>

          <!-- Filtre Opération -->
          <div class="relative" v-if="operationsDisponibles.length > 0">
            <select v-model="selectedOperation" class="appearance-none bg-slate-50 border border-slate-200 text-slate-700 py-2 pl-3 pr-8 rounded-lg text-sm font-medium focus:outline-none focus:border-blue-500 cursor-pointer">
              <option value="">Toutes opérations</option>
              <option v-for="op in operationsDisponibles" :key="op" :value="op">{{ op }}</option>
            </select>
            <i class="pi pi-filter absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none text-xs"></i>
          </div>

          <!-- Barre de recherche intégrée -->
          <div class="relative group">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-blue-500 text-sm"></i>
            <input type="text" v-model="searchQuery" placeholder="Rechercher un plan ou article..."
              class="pl-9 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-lg text-sm w-full md:w-72 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-all">
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex flex-col items-center justify-center py-20 text-blue-500">
      <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
      <p class="text-sm font-bold text-slate-500 uppercase tracking-widest">Chargement des plans...</p>
    </div>

    <!-- ========================================== -->
    <!-- GRILLE DES RÉSULTATS (Cards)               -->
    <!-- ========================================== -->
    <div v-else>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5">
        <!-- Boucle sur les plans paginés -->
        <div v-for="plan in paginatedPlans" :key="plan.id"
          @click="consulter(plan.category, plan.id)"
          :class="[
            'group bg-white border rounded-xl p-5 hover:shadow-lg transition-all cursor-pointer relative overflow-hidden flex flex-col h-full',
            categoryStyles[plan.category]?.hoverClass || 'hover:border-slate-300 border-slate-200',
            plan.statut === 'ARCHIVE' ? 'opacity-80 grayscale-[20%]' : ''
          ]">

          <!-- Petite barre de couleur en haut selon le type -->
          <div class="absolute top-0 left-0 w-full h-1" :class="categoryStyles[plan.category]?.colorClass || 'bg-slate-300'"></div>

          <!-- Header Card: Type & Statut -->
          <div class="flex justify-between items-start mb-3">
            <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wide" :class="categoryStyles[plan.category]?.textClass || 'text-slate-500'">
              <i :class="categoryStyles[plan.category]?.icon || 'pi pi-file'" class="text-lg"></i>
              {{ categoryStyles[plan.category]?.label || plan.category }}
            </div>
          </div>

          <!-- Article Code & Designation are deliberately omitted because they are shown in the tags and title -->

          <!-- Titre & Référence -->
          <div class="flex-1 mb-3">
            <h3 class="text-base font-bold text-slate-800 leading-tight mb-1 transition-colors line-clamp-2" :class="categoryStyles[plan.category]?.titleHoverClass || 'group-hover:text-blue-600'">
              {{ plan.libelle || plan.designation || '(Sans désignation)' }}
            </h3>
          </div>

          <!-- Tags -->
          <div class="flex flex-wrap gap-1.5 mb-4">
            <span v-if="plan.nature && plan.nature !== 'N/A'"
              class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-slate-100 text-slate-600 border border-slate-200 px-2 py-1 rounded">
              <i class="pi pi-box text-[9px]"></i> {{ (plan.category === 'RC' && plan.nature === 'POSTE') ? 'RÉSULTAT DE CONTRÔLE' : plan.nature }}
            </span>
            <span v-if="plan.type && plan.type !== 'N/A'"
              class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-purple-50 text-purple-700 border border-purple-200 px-2 py-1 rounded">
              <i class="pi pi-tag text-[9px]"></i> {{ plan.type }}
            </span>
            <span v-if="plan.operation && plan.operation !== 'N/A'"
              class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-amber-50 text-amber-700 border border-amber-200 px-2 py-1 rounded">
              <i class="pi pi-cog text-[9px]"></i> {{ plan.operation }}
            </span>
            <span v-if="plan.codeReferenceFormulaire"
              class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-indigo-50 text-indigo-700 border border-indigo-200 px-2 py-1 rounded">
              <i class="pi pi-file text-[9px]"></i> {{ plan.codeReferenceFormulaire }}
            </span>
          </div>

          <!-- Footer Card: Version + Actions -->
          <div class="mt-auto pt-4 border-t border-slate-100 flex items-center justify-between">
            <div class="flex gap-2">
              <span class="inline-flex items-center justify-center bg-slate-800 text-white text-xs font-bold px-2 py-1 rounded">
                V{{ plan.formulaireVersion ?? plan.version }}
              </span>
              <span :class="[
                'px-2.5 py-1 rounded-full text-[10px] font-extrabold uppercase tracking-wider shadow-sm',
                plan.statut === 'ACTIF' ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'
              ]">
                {{ plan.statut }}
              </span>
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <button v-if="plan.statut === 'ACTIF'" @click.stop="editer(plan.category, plan.id)" class="p-1.5 text-slate-400 hover:text-blue-500 hover:bg-blue-50 rounded transition-colors" title="Éditer">
                <i class="pi pi-pencil"></i>
              </button>
              <button v-if="plan.statut === 'ARCHIVE'" @click.stop="upgrader(plan.category, plan.id)" class="p-1.5 text-slate-400 hover:text-emerald-500 hover:bg-emerald-50 rounded transition-colors" title="Mettre à jour vers le dernier PRC">
                <i class="pi pi-arrow-up"></i>
              </button>
              <button v-if="plan.statut === 'ACTIF'" @click.stop="confirmArchivage(plan)" class="p-1.5 text-slate-400 hover:text-amber-500 hover:bg-amber-50 rounded transition-colors" title="Archiver">
                <i class="pi pi-box"></i>
              </button>
              <button @click.stop="consulter(plan.category, plan.id)" class="p-1.5 text-slate-400 hover:text-blue-500 hover:bg-blue-50 rounded transition-colors" title="Visualiser">
                <i class="pi pi-eye"></i>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div class="mt-10 flex justify-center pb-10" v-if="filteredPlans.length > rows">
        <Paginator
          :rows="rows"
          :totalRecords="filteredPlans.length"
          :first="first"
          @page="onPage($event)"
          template="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport"
          currentPageReportTemplate="{first} - {last} sur {totalRecords}"
          class="!bg-transparent !border-0"
        />
      </div>
    </div>

    <!-- Empty State -->
    <div v-if="!isLoading && filteredPlans.length === 0" class="text-center py-20">
      <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-slate-100 text-slate-400 mb-4">
        <i class="pi pi-search text-2xl"></i>
      </div>
      <h3 class="text-lg font-bold text-slate-700">Aucune structure trouvée</h3>
      <p class="text-slate-500 mt-1">Modifiez vos filtres ou créez une nouvelle structure.</p>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useAppToast } from '@/composables/useAppToast';
import { useAppConfirm } from '@/composables/useAppConfirm';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import Paginator from 'primevue/paginator';
import apiClient from '@/services/apiClient';

const router = useRouter();
const toast = useAppToast();
const confirm = useAppConfirm();

const activeTab = ref('ALL');
const searchQuery = ref('');
const selectedOperation = ref('');
const vueActuelle = ref('ACTIF');
const isLoading = ref(true);
const plans = ref([]);

const first = ref(0);
const rows = ref(12);

const onPage = (event) => {
  first.value = event.first;
};

const tabs = [
  { id: 'ALL', label: 'Tous', short: 'Tous', icon: 'pi pi-th-large' },
  { id: 'FAB', label: 'Fabrication', short: 'Fab', icon: 'pi pi-cog' }
];

const categoryStyles = {
  FAB: { label: 'Fabrication', icon: 'pi pi-cog', colorClass: 'bg-amber-500', textClass: 'text-amber-500', hoverClass: 'hover:border-amber-300 border-slate-200', titleHoverClass: 'group-hover:text-amber-600' },
  PRC_STRUCT: { label: 'Fabrication', icon: 'pi pi-sitemap', colorClass: 'bg-amber-500', textClass: 'text-amber-500', hoverClass: 'hover:border-amber-300 border-slate-200', titleHoverClass: 'group-hover:text-amber-600' }
};

onMounted(async () => {
  await chargerPlans();
});

const chargerPlans = async () => {
  try {
    isLoading.value = true;
    const response = await apiClient.get('/hub/structures');
    plans.value = response.data.data || response.data || [];
  } catch {
    toast.error('Impossible de charger les plans');
  } finally {
    isLoading.value = false;
  }
};

const filteredPlans = computed(() => {
  return plans.value.filter(plan => {
    const matchTab = activeTab.value === 'ALL' || plan.category === activeTab.value;
    const matchOp = selectedOperation.value === '' || plan.operation === selectedOperation.value;

    const q = searchQuery.value.toLowerCase();
    const libelle = (plan.libelle || '').toLowerCase();
    const designation = (plan.designation || '').toLowerCase();
    const codeRef = `${plan.nature || ''} ${plan.type || ''} ${plan.codeArticleSage || ''}`.toLowerCase();
    const matchSearch = q === '' || libelle.includes(q) || designation.includes(q) || codeRef.includes(q);

    const matchStatut = vueActuelle.value === 'ALL' || plan.statut === vueActuelle.value;

    return matchTab && matchOp && matchSearch && matchStatut;
  }).sort((a, b) => {
    const priorite = { 'ACTIF': 1, 'ARCHIVE': 2 };
    const pA = priorite[a.statut] || 99;
    const pB = priorite[b.statut] || 99;
    
    if (pA !== pB) return pA - pB;
    return (a.codeArticleSage || '').localeCompare(b.codeArticleSage || '');
  });
});

const paginatedPlans = computed(() => {
  return filteredPlans.value.slice(first.value, first.value + rows.value);
});

const operationsDisponibles = computed(() => {
  const ops = new Set();
  plans.value.forEach(p => {
    const matchCategory = activeTab.value === 'ALL' || p.category === activeTab.value;
    if (matchCategory && p.operation && p.operation !== 'N/A') {
      ops.add(p.operation);
    }
  });
  return Array.from(ops).sort();
});

watch([activeTab, searchQuery, selectedOperation, vueActuelle], () => {
  first.value = 0;
});

const confirmArchivage = (plan) => {
  confirm.ask({
    message: `Voulez-vous vraiment archiver "${plan.libelle || plan.codeArticleSage}" ?`,
    header: 'Confirmation d\'archivage',
    icon: 'pi pi-exclamation-triangle',
    acceptClass: 'p-button-warning',
    acceptLabel: 'Oui, Archiver',
    rejectLabel: 'Annuler',
    accept: () => archiverPlan(plan)
  });
};

const archiverPlan = async (plan) => {
  try {
    isLoading.value = true;
    await apiClient.put(`/hub/plans/${plan.category}/${plan.id}/statut?statut=ARCHIVE`);
    plan.statut = 'ARCHIVE';
    toast.success(`Le plan a été archivé avec succès.`);
  } catch {
    toast.error('L\'archivage a échoué.');
  } finally {
    isLoading.value = false;
  }
};

const editer = (category, id) => {
  const routes = {
    FAB: `/dev/fab/plans/editer/${id}`,
    PF: `/dev/produit-fini/editer/${id}`,
    ECH: `/dev/echantillonnage/editer/${id}`,
    PRC_STRUCT: { path: `/dev/fab/specifique`, query: { id: id } }
  };
  if (routes[category]) router.push(routes[category]);
  else toast.warn('Édition non disponible.', 'Catégorie inconnue');
};

const upgrader = (category, id) => {
  const routes = {
    FAB: { path: `/dev/fab/plans/editer/${id}`, query: { upgrade: 'true' } }
  };
  if (routes[category]) router.push(routes[category]);
  else toast.warn('Mise à jour non disponible.', 'Catégorie inconnue');
};

const consulter = (category, id) => {
  const routes = {
    FAB: { path: `/dev/fab/plans/editer/${id}`, query: { view: 'true' } },
    PF: { path: `/dev/produit-fini/editer/${id}`, query: { view: 'true' } },
    ECH: { path: `/dev/echantillonnage/editer/${id}`, query: { view: 'true' } },
    PRC_STRUCT: { path: `/dev/fab/specifique`, query: { view: 'true', id: id } }
  };
  if (routes[category]) router.push(routes[category]);
  else toast.warn('Consultation non disponible.', 'Catégorie inconnue');
};
</script>