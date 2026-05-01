<template>
  <div class="max-w-[1400px] mx-auto animate-in fade-in duration-500">
    <Toast position="top-right" />
    <ConfirmDialog></ConfirmDialog>

    <div class="mb-10">
      <h1 class="text-3xl font-black text-slate-900 tracking-tight">Gestion des Modèles</h1>
      <p class="text-slate-500 mt-1 font-medium text-sm">Visualisez et gérez vos modèles de plans de contrôle existants.</p>
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
            <select v-model="vueActuelle" class="appearance-none bg-slate-50 border-t border-slate-200 text-slate-700 py-2 pl-3 pr-8 rounded-lg text-sm font-medium focus:outline-none focus:border-blue-500 cursor-pointer">
              <option value="ACTIF">Actifs</option>
              <option value="ARCHIVE">Archivés</option>
            </select>
            <i class="pi pi-angle-down absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none text-xs"></i>
          </div>

          <!-- Filtre Opération -->
          <div class="relative" v-if="operationsDisponibles.length > 0 && activeTab !== 'PF'">
            <select v-model="selectedOperation" class="appearance-none bg-slate-50 border border-slate-200 text-slate-700 py-2 pl-3 pr-8 rounded-lg text-sm font-medium focus:outline-none focus:border-blue-500 cursor-pointer">
              <option value="">Toutes opérations</option>
              <option v-for="op in operationsDisponibles" :key="op" :value="op">{{ op }}</option>
            </select>
            <i class="pi pi-filter absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none text-xs"></i>
          </div>

          <!-- Barre de recherche intégrée -->
          <div class="relative group">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 group-focus-within:text-blue-500 text-sm"></i>
            <input type="text" v-model="searchQuery" placeholder="Rechercher un modèle..." 
              class="pl-9 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-lg text-sm w-full md:w-64 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-all">
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex flex-col items-center justify-center py-20 text-blue-500">
      <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
      <p class="text-sm font-bold text-slate-500 uppercase tracking-widest">Chargement des modèles...</p>
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
            plan.statut === 'ACTIF' ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'
          ]">
            {{ plan.statut }}
          </span>
        </div>

        <!-- Titre & Référence -->
        <div class="flex-1 mb-3">
          <h3 class="text-base font-bold text-slate-800 leading-tight mb-1 transition-colors line-clamp-2" :class="categoryStyles[plan.category]?.titleHoverClass || 'group-hover:text-blue-600'">
            {{ plan.libelle }}
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
          <!-- Badge Opération -->
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
             <button v-if="plan.statut === 'ACTIF'" @click.stop="confirmArchivage(plan)" class="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded transition-colors" title="Archiver">
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
      <h3 class="text-lg font-bold text-slate-700">Aucun modèle trouvé</h3>
      <p class="text-slate-500 mt-1">Modifiez vos filtres ou effectuez une nouvelle recherche.</p>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import ConfirmDialog from 'primevue/confirmdialog';
import apiClient from '@/services/apiClient';

const router = useRouter();
const toast = useToast();
const confirm = useConfirm();

const activeTab = ref('ALL');
const searchQuery = ref('');
const selectedOperation = ref('');
const vueActuelle = ref('ACTIF'); // Default to ACTIF
const isLoading = ref(true);
const modeles = ref([]);

const tabs = [
  { id: 'ALL', label: 'Tous', short: 'Tous', icon: 'pi pi-th-large' },
  { id: 'FAB', label: 'Fabrication', short: 'Fab', icon: 'pi pi-cog' },
  { id: 'ASS', label: 'Assemblage', short: 'Ass', icon: 'pi pi-sitemap' },
  { id: 'VM', label: 'Vérif Machine', short: 'Vérif', icon: 'pi pi-desktop' },
  { id: 'RC', label: 'Résultat Contrôle', short: 'Contrôle', icon: 'pi pi-list' },
  { id: 'ECH', label: 'Échantillonnage', short: 'Échan', icon: 'pi pi-check-square' },
  { id: 'PF', label: 'Produit Fini', short: 'PF', icon: 'pi pi-box' },
];

const categoryStyles = {
  FAB: { label: 'Fabrication', icon: 'pi pi-cog', colorClass: 'bg-amber-500', textClass: 'text-amber-500', hoverClass: 'hover:border-amber-300', titleHoverClass: 'group-hover:text-amber-600' },
  ASS: { label: 'Assemblage', icon: 'pi pi-sitemap', colorClass: 'bg-indigo-500', textClass: 'text-indigo-500', hoverClass: 'hover:border-indigo-300', titleHoverClass: 'group-hover:text-indigo-600' },
  VM: { label: 'Vérif Machine', icon: 'pi pi-desktop', colorClass: 'bg-emerald-500', textClass: 'text-emerald-500', hoverClass: 'hover:border-emerald-300', titleHoverClass: 'group-hover:text-emerald-600' },
  RC: { label: 'Résultat Contrôle', icon: 'pi pi-list', colorClass: 'bg-teal-500', textClass: 'text-teal-500', hoverClass: 'hover:border-teal-300', titleHoverClass: 'group-hover:text-teal-600' },
  ECH: { label: 'Échantillonnage', icon: 'pi pi-check-square', colorClass: 'bg-purple-500', textClass: 'text-purple-500', hoverClass: 'hover:border-purple-300', titleHoverClass: 'group-hover:text-purple-600' },
  PF: { label: 'Produit Fini', icon: 'pi pi-box', colorClass: 'bg-blue-500', textClass: 'text-blue-500', hoverClass: 'hover:border-blue-300', titleHoverClass: 'group-hover:text-blue-600' }
};

onMounted(async () => {
  await chargerModeles();
});

const chargerModeles = async () => {
  try {
    isLoading.value = true;
    const response = await apiClient.get('/hub/modeles');
    modeles.value = response.data.data || response.data || [];
  } catch (error) {
    console.error(error);
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les modèles', life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

const filteredPlans = computed(() => {
  return modeles.value.filter(plan => {
    // 1. Filtre par Onglet (Type)
    const matchTab = activeTab.value === 'ALL' || plan.category === activeTab.value;
    
    // 2. Filtre par Opération
    const matchOp = selectedOperation.value === '' || plan.poste === selectedOperation.value;
    
    // 3. Filtre par Recherche texte
    const q = searchQuery.value.toLowerCase();
    const libelle = plan.libelle ? plan.libelle.toLowerCase() : '';
    const codeRef = `${plan.nature || ''} ${plan.type || ''}`.toLowerCase();
    const matchSearch = q === '' || libelle.includes(q) || codeRef.includes(q);

    // 4. Filtre par Statut
    const matchStatut = vueActuelle.value === 'ALL' || plan.statut === vueActuelle.value;

    return matchTab && matchOp && matchSearch && matchStatut;
  });
});

const operationsDisponibles = computed(() => {
  const ops = new Set();
  modeles.value.forEach(m => {
    // On n'inclut pas les opérations pour le Produit Fini (PF) car c'est toujours le même (Contrôle Final)
    const matchCategory = (activeTab.value === 'ALL' || m.category === activeTab.value) && m.category !== 'PF';
    if (matchCategory && m.poste && m.poste !== 'N/A') {
      ops.add(m.poste);
    }
  });
  return Array.from(ops).sort();
});

watch(activeTab, () => {
  selectedOperation.value = '';
});

// ACTIONS
const confirmArchivage = (mod) => {
  confirm.require({
    message: `Voulez-vous vraiment archiver le modèle "${mod.libelle}" ?`,
    header: 'Confirmation d\'archivage',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Archiver',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await archiver(mod);
    }
  });
};

const archiver = async (mod) => {
  try {
    isLoading.value = true;
    await apiClient.put(`/hub/modeles/${mod.category}/${mod.id}/statut?statut=ARCHIVE`);
    mod.statut = 'ARCHIVE';
    toast.add({ severity: 'success', summary: 'Archivé', detail: 'Le modèle a été placé dans les archives.', life: 3000 });
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: "L'archivage a échoué.", life: 3000 });
  } finally {
    isLoading.value = false;
  }
};



const editer = (category, id) => {
  const routes = {
    'FAB': `/dev/fab/editer/${id}`,
    'ASS': `/dev/assemblage/editer/${id}`,
    'VM': `/dev/verif-machine/editer/${id}`,
    'RC': `/dev/resultat-controle/editer/${id}`,
    'ECH': `/dev/echantillonnage/editer/${id}`,
    'PF': `/dev/produit-fini/editer/${id}`
  };
  if (routes[category]) router.push(routes[category]);
};

const consulter = (category, id) => {
  const routes = {
    'FAB': { path: `/dev/fab/editer/${id}`, query: { view: 'true' } },
    'ASS': { path: `/dev/assemblage/editer/${id}`, query: { view: 'true' } },
    'VM': { path: `/dev/verif-machine/editer/${id}`, query: { view: 'true' } },
    'RC': { path: `/dev/resultat-controle/editer/${id}`, query: { view: 'true' } },
    'ECH': { path: `/dev/echantillonnage/editer/${id}`, query: { view: 'true' } },
    'PF': { path: `/dev/produit-fini/editer/${id}`, query: { view: 'true' } }
  };
  if (routes[category]) router.push(routes[category]);
};
</script>