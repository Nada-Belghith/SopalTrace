<template>
  <div class="max-w-[1400px] mx-auto animate-in fade-in duration-500">
    <Toast position="top-right" />
    <ConfirmDialog></ConfirmDialog>

    <div class="mb-10">
      <h1 class="text-3xl font-black text-slate-900 tracking-tight">Plans par Article</h1>
      <p class="text-slate-500 mt-1 font-medium text-sm">Visualisez et gérez vos plans de contrôle instanciés par article.</p>
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

          <!-- Filtre Statut -->
          <div class="relative">
            <select v-model="vueActuelle" class="appearance-none bg-slate-50 border border-slate-200 text-slate-700 py-2 pl-3 pr-8 rounded-lg text-sm font-medium focus:outline-none focus:border-blue-500 cursor-pointer">
              <option value="ACTIF">Actifs</option>
              <option value="BROUILLON">Brouillons</option>
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

          <!-- Bouton Nouveau Plan -->
          <button @click="router.push('/dev/fab/plans/nouveau')"
            class="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-bold flex items-center gap-2 hover:bg-blue-700 transition-colors shadow-sm shrink-0">
            <i class="pi pi-plus"></i>
            <span class="hidden md:inline">Nouveau Plan</span>
          </button>
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
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5">

      <!-- Boucle sur les plans -->
      <div v-for="plan in filteredPlans" :key="plan.id"
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

          <!-- Badge Statut -->
          <span :class="[
            'px-2.5 py-1 rounded-full text-[10px] font-extrabold uppercase tracking-wider shadow-sm',
            plan.statut === 'ACTIF' ? 'bg-emerald-100 text-emerald-700' :
            plan.statut === 'BROUILLON' ? 'bg-amber-100 text-amber-700' : 'bg-slate-100 text-slate-600'
          ]">
            {{ plan.statut }}
          </span>
        </div>

        <!-- Article Code & Designation (prominant) -->
        <div v-if="plan.codeArticleSage" class="mb-2">
          <span class="font-mono text-xs font-bold text-blue-700 bg-blue-50 border border-blue-200 px-2 py-0.5 rounded">
            {{ plan.codeArticleSage }}
          </span>
          <div v-if="plan.designation" class="mt-1.5 text-[11px] font-bold text-slate-500 italic line-clamp-1 leading-tight" :title="plan.designation">
            {{ plan.designation }}
          </div>
        </div>

        <!-- Titre & Référence -->
        <div class="flex-1 mb-3">
          <h3 class="text-base font-bold text-slate-800 leading-tight mb-1 transition-colors line-clamp-2" :class="categoryStyles[plan.category]?.titleHoverClass || 'group-hover:text-blue-600'">
            {{ plan.libelle || plan.designation || '(Sans désignation)' }}
          </h3>
        </div>

        <!-- Tags: Nature, Type, Opération -->
        <div class="flex flex-wrap gap-1.5 mb-4">
          <span v-if="plan.nature && plan.nature !== 'N/A'"
            class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-slate-100 text-slate-600 border border-slate-200 px-2 py-1 rounded">
            <i class="pi pi-box text-[9px]"></i> {{ plan.nature }}
          </span>
          <span v-if="plan.type && plan.type !== 'N/A'"
            class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-slate-100 text-slate-600 border border-slate-200 px-2 py-1 rounded">
            <i class="pi pi-tag text-[9px]"></i> {{ plan.type }}
          </span>
          <!-- Badge Opération — affiché en couleur distinctive -->
          <span v-if="plan.poste && plan.poste !== 'N/A'"
            class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-amber-50 text-amber-700 border border-amber-200 px-2 py-1 rounded">
            <i class="pi pi-cog text-[9px]"></i> {{ plan.poste }}
          </span>
        </div>

        <!-- Footer Card: Version + Actions -->
        <div class="mt-auto pt-4 border-t border-slate-100 flex items-center justify-between">
          <div class="flex gap-2">
            <!-- Badge Version -->
            <span class="inline-flex items-center justify-center bg-slate-800 text-white text-xs font-bold px-2 py-1 rounded">
              V{{ plan.version }}
            </span>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button v-if="plan.statut === 'ACTIF' || plan.statut === 'BROUILLON'" @click.stop="editer(plan.category, plan.id)" class="p-1.5 text-slate-400 hover:text-blue-500 hover:bg-blue-50 rounded transition-colors" title="Éditer">
              <i class="pi pi-pencil"></i>
            </button>
            <button v-if="plan.statut === 'BROUILLON'" @click.stop="confirmSuppressionBrouillon(plan)" class="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded transition-colors" title="Supprimer le brouillon">
              <i class="pi pi-trash"></i>
            </button>
            <button v-if="plan.statut === 'ACTIF'" @click.stop="confirmArchivage(plan)" class="p-1.5 text-slate-400 hover:text-amber-500 hover:bg-amber-50 rounded transition-colors" title="Archiver">
              <i class="pi pi-box"></i>
            </button>

            <i class="pi pi-eye text-slate-300 ml-1 transition-colors text-sm" :class="categoryStyles[plan.category]?.textClass || 'group-hover:text-blue-500'"></i>
          </div>
        </div>

      </div>
    </div>

    <!-- Empty State -->
    <div v-if="!isLoading && filteredPlans.length === 0" class="text-center py-20 bg-white border border-slate-200 rounded-xl mt-4 shadow-sm">
      <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-slate-100 text-slate-400 mb-4">
        <i class="pi pi-search text-2xl"></i>
      </div>
      <h3 class="text-lg font-bold text-slate-700">Aucun plan trouvé</h3>
      <p class="text-slate-500 mt-1">Modifiez vos filtres ou créez un nouveau plan par article.</p>
      <button @click="router.push('/dev/fab/plans/nouveau')"
        class="mt-6 px-6 py-3 bg-blue-600 text-white rounded-xl text-sm font-bold flex items-center gap-2 hover:bg-blue-700 transition-colors shadow-sm mx-auto">
        <i class="pi pi-plus"></i> Créer un Plan
      </button>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import apiClient from '@/services/apiClient';

const router = useRouter();
const toast = useToast();
const confirm = useConfirm();

const activeTab = ref('ALL');
const searchQuery = ref('');
const selectedOperation = ref('');
const vueActuelle = ref('ACTIF');
const isLoading = ref(true);
const plans = ref([]);

const tabs = [
  { id: 'ALL', label: 'Tous', short: 'Tous', icon: 'pi pi-th-large' },
  { id: 'FAB', label: 'Fabrication', short: 'Fab', icon: 'pi pi-cog' },
  { id: 'PF', label: 'Produit Fini', short: 'PF', icon: 'pi pi-box' },
  { id: 'ECH', label: 'Échantillonnage', short: 'Échan', icon: 'pi pi-check-square' },
];

const categoryStyles = {
  FAB: { label: 'Fabrication', icon: 'pi pi-cog', colorClass: 'bg-amber-500', textClass: 'text-amber-500', hoverClass: 'hover:border-amber-300 border-slate-200', titleHoverClass: 'group-hover:text-amber-600' },
  PF: { label: 'Produit Fini', icon: 'pi pi-box', colorClass: 'bg-blue-500', textClass: 'text-blue-500', hoverClass: 'hover:border-blue-300 border-slate-200', titleHoverClass: 'group-hover:text-blue-600' },
  ECH: { label: 'Échantillonnage', icon: 'pi pi-check-square', colorClass: 'bg-purple-500', textClass: 'text-purple-500', hoverClass: 'hover:border-purple-300 border-slate-200', titleHoverClass: 'group-hover:text-purple-600' }
};

onMounted(async () => {
  await chargerPlans();
});

const chargerPlans = async () => {
  try {
    isLoading.value = true;
    const response = await apiClient.get('/hub/plans');
    plans.value = response.data.data || response.data || [];
  } catch (error) {
    console.error(error);
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les plans', life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

const filteredPlans = computed(() => {
  return plans.value.filter(plan => {
    // 1. Filtre par Onglet (Type)
    const matchTab = activeTab.value === 'ALL' || plan.category === activeTab.value;

    // 2. Filtre par Opération
    const matchOp = selectedOperation.value === '' || plan.poste === selectedOperation.value;

    // 3. Filtre par Recherche texte
    const q = searchQuery.value.toLowerCase();
    const libelle = (plan.libelle || '').toLowerCase();
    const designation = (plan.designation || '').toLowerCase();
    const codeRef = `${plan.nature || ''} ${plan.type || ''} ${plan.codeArticleSage || ''}`.toLowerCase();
    const matchSearch = q === '' || libelle.includes(q) || designation.includes(q) || codeRef.includes(q);

    // 4. Filtre par Statut
    const matchStatut = plan.statut === vueActuelle.value;

    return matchTab && matchOp && matchSearch && matchStatut;
  });
});

const operationsDisponibles = computed(() => {
  const ops = new Set();
  plans.value.forEach(p => {
    const matchCategory = activeTab.value === 'ALL' || p.category === activeTab.value;
    if (matchCategory && p.poste && p.poste !== 'N/A') {
      ops.add(p.poste);
    }
  });
  return Array.from(ops).sort();
});

watch(activeTab, () => {
  selectedOperation.value = '';
});

// === ACTIONS ===
const confirmSuppressionBrouillon = (plan) => {
  confirm.require({
    message: `Retirer définitivement le brouillon "${plan.libelle || plan.codeArticle}" ?`,
    header: 'Suppression du Brouillon',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Supprimer',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await supprimerBrouillon(plan);
    }
  });
};

const supprimerBrouillon = async (plan) => {
  try {
    isLoading.value = true;
    await apiClient.delete(`/hub/plans/${plan.category}/${plan.id}`);
    plans.value = plans.value.filter(p => p.id !== plan.id);
    toast.add({ severity: 'success', summary: 'Supprimé', detail: 'Le brouillon a été retiré.', life: 3000 });
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'La suppression a échoué.', life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

const confirmArchivage = (plan) => {
  confirm.require({
    message: `Voulez-vous archiver le plan "${plan.libelle || plan.codeArticle}" ?`,
    header: 'Confirmation d\'archivage',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Archiver',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await archiver(plan);
    }
  });
};

const archiver = async (plan) => {
  try {
    isLoading.value = true;
    await apiClient.put(`/hub/plans/${plan.category}/${plan.id}/statut?statut=ARCHIVE`);
    plan.statut = 'ARCHIVE';
    toast.add({ severity: 'success', summary: 'Archivé', detail: 'Le plan a été archivé.', life: 3000 });
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: "L'archivage a échoué.", life: 3000 });
  } finally {
    isLoading.value = false;
  }
};



const editer = (category, id) => {
  const routes = {
    FAB: `/dev/fab/plans/editer/${id}`,
    PF: `/dev/produit-fini/editer/${id}`,
    ECH: `/dev/echantillonnage/editer/${id}`
  };
  if (routes[category]) router.push(routes[category]);
  else toast.add({ severity: 'warn', summary: 'Catégorie inconnue', detail: 'Redirection non disponible.', life: 3000 });
};

const consulter = (category, id) => {
  const routes = {
    FAB: { path: `/dev/fab/plans/editer/${id}`, query: { view: 'true' } },
    PF: { path: `/dev/produit-fini/editer/${id}`, query: { view: 'true' } },
    ECH: { path: `/dev/echantillonnage/editer/${id}`, query: { view: 'true' } }
  };
  if (routes[category]) router.push(routes[category]);
  else toast.add({ severity: 'warn', summary: 'Catégorie inconnue', detail: 'Consultation non disponible.', life: 3000 });
};
</script>