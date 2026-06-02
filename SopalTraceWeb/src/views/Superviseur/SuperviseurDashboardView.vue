<template>
  <div class="min-h-screen bg-slate-900 text-slate-100 p-6 md:p-8 font-sans">
    <div class="max-w-7xl mx-auto space-y-8">
      
      <!-- HEADER -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-slate-800/50 p-6 rounded-2xl border border-slate-700 shadow-xl backdrop-blur-sm">
        <div class="flex items-center gap-4">
          <div class="p-3 bg-amber-500/20 text-amber-400 rounded-xl">
            <i class="pi pi-shield text-3xl"></i>
          </div>
          <div>
            <h1 class="text-2xl font-black text-white tracking-tight">Dashboard Superviseur</h1>
            <p class="text-slate-400 text-sm mt-1">Supervision et validation des plans qualité transverses</p>
          </div>
        </div>
        <div class="flex gap-3">
          <button @click="loadData" class="px-4 py-2 bg-slate-700 hover:bg-slate-600 text-white rounded-lg font-bold text-sm transition-colors flex items-center gap-2">
            <i class="pi pi-refresh" :class="{ 'animate-spin': loading }"></i> Actualiser
          </button>
        </div>
      </div>

      <!-- STATS CARDS -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div class="bg-gradient-to-br from-blue-900/50 to-slate-800 border border-blue-500/30 p-6 rounded-2xl shadow-lg relative overflow-hidden group">
          <div class="absolute top-0 right-0 p-4 opacity-20 group-hover:opacity-40 transition-opacity"><i class="pi pi-file text-6xl text-blue-400"></i></div>
          <p class="text-blue-300 font-bold text-sm uppercase tracking-wider">Plans d'Assemblage</p>
          <p class="text-4xl font-black text-white mt-2">{{ stats.assemblage }}</p>
        </div>
        <div class="bg-gradient-to-br from-emerald-900/50 to-slate-800 border border-emerald-500/30 p-6 rounded-2xl shadow-lg relative overflow-hidden group">
          <div class="absolute top-0 right-0 p-4 opacity-20 group-hover:opacity-40 transition-opacity"><i class="pi pi-box text-6xl text-emerald-400"></i></div>
          <p class="text-emerald-300 font-bold text-sm uppercase tracking-wider">Produits Finis</p>
          <p class="text-4xl font-black text-white mt-2">{{ stats.pf }}</p>
        </div>
        <div class="bg-gradient-to-br from-purple-900/50 to-slate-800 border border-purple-500/30 p-6 rounded-2xl shadow-lg relative overflow-hidden group">
          <div class="absolute top-0 right-0 p-4 opacity-20 group-hover:opacity-40 transition-opacity"><i class="pi pi-cog text-6xl text-purple-400"></i></div>
          <p class="text-purple-300 font-bold text-sm uppercase tracking-wider">Vérification Machines</p>
          <p class="text-4xl font-black text-white mt-2">{{ stats.machines }}</p>
        </div>
      </div>

      <!-- MAIN CONTENT -->
      <div class="bg-slate-800 rounded-2xl border border-slate-700 shadow-xl overflow-hidden">
        <div class="border-b border-slate-700 p-4 flex gap-4 bg-slate-800/80">
          <button @click="activeTab = 'assemblage'" :class="['px-4 py-2 rounded-lg text-sm font-bold transition-all', activeTab === 'assemblage' ? 'bg-blue-500 text-white shadow-lg shadow-blue-500/20' : 'text-slate-400 hover:text-white hover:bg-slate-700']">Assemblage</button>
          <button @click="activeTab = 'pf'" :class="['px-4 py-2 rounded-lg text-sm font-bold transition-all', activeTab === 'pf' ? 'bg-emerald-500 text-white shadow-lg shadow-emerald-500/20' : 'text-slate-400 hover:text-white hover:bg-slate-700']">Produits Finis</button>
          <button @click="activeTab = 'machines'" :class="['px-4 py-2 rounded-lg text-sm font-bold transition-all', activeTab === 'machines' ? 'bg-purple-500 text-white shadow-lg shadow-purple-500/20' : 'text-slate-400 hover:text-white hover:bg-slate-700']">Vérif Machines</button>
        </div>

        <div class="p-6 min-h-[400px]">
          <div v-if="loading" class="flex flex-col items-center justify-center h-64 space-y-4">
            <i class="pi pi-spinner animate-spin text-4xl text-amber-500"></i>
            <p class="text-slate-400 font-medium">Chargement des données...</p>
          </div>
          
          <div v-else-if="filteredPlans.length === 0" class="flex flex-col items-center justify-center h-64 space-y-4">
            <div class="w-16 h-16 rounded-full bg-slate-700 flex items-center justify-center">
              <i class="pi pi-folder-open text-2xl text-slate-400"></i>
            </div>
            <p class="text-slate-400 font-medium text-lg">Aucun plan trouvé pour cette catégorie</p>
          </div>

          <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div v-for="plan in filteredPlans" :key="plan.id" class="bg-slate-900 border border-slate-700 hover:border-amber-500/50 rounded-xl p-5 transition-all hover:shadow-xl hover:shadow-amber-500/10 group">
              <div class="flex justify-between items-start mb-4">
                <span class="px-2.5 py-1 text-[10px] font-black uppercase tracking-widest rounded-md" :class="getStatusClass(plan.statut)">
                  {{ plan.statut || 'BROUILLON' }}
                </span>
                <span class="text-slate-500 text-xs font-mono">{{ formatDate(plan.creeLe) }}</span>
              </div>
              <h3 class="text-lg font-bold text-white mb-1">{{ plan.codeAffiche || plan.code || 'Plan sans code' }}</h3>
              <p class="text-slate-400 text-sm mb-4 line-clamp-2">{{ plan.libelle || 'Aucune description' }}</p>
              
              <div class="flex items-center justify-between mt-auto pt-4 border-t border-slate-800">
                <div class="flex items-center gap-2 text-slate-400 text-xs font-mono">
                  <i class="pi pi-user"></i> {{ plan.creePar || 'Système' }}
                </div>
                <button @click="openPlan(plan)" class="w-8 h-8 rounded-full bg-slate-800 group-hover:bg-amber-500 text-slate-300 group-hover:text-slate-900 flex items-center justify-center transition-colors">
                  <i class="pi pi-arrow-right"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import apiClient from '@/services/apiClient';
import { useAuthStore } from '@/stores/authStore';


const router = useRouter();
const authStore = useAuthStore();

const activeTab = ref('assemblage');
const loading = ref(false);
const plans = ref({ assemblage: [], pf: [], machines: [] });

const stats = computed(() => ({
  assemblage: plans.value.assemblage.length,
  pf: plans.value.pf.length,
  machines: plans.value.machines.length
}));

const filteredPlans = computed(() => plans.value[activeTab.value] || []);

const loadData = async () => {
  loading.value = true;
  try {
    // We fetch data from actual endpoints
    const [resAss, resPf, resMach] = await Promise.allSettled([
      apiClient.get('/api/plan-fabrication/recherche'),
      apiClient.get('/api/plan-pf/recherche'),
      apiClient.get('/api/plan-nc/recherche') // Adjust endpoint if needed
    ]);

    plans.value.assemblage = resAss.status === 'fulfilled' ? resAss.value.data.data || resAss.value.data : [];
    plans.value.pf = resPf.status === 'fulfilled' ? resPf.value.data.data || resPf.value.data : [];
    plans.value.machines = resMach.status === 'fulfilled' ? resMach.value.data.data || resMach.value.data : [];
  } catch (err) {
    console.error("Erreur de chargement:", err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadData();
});

const getStatusClass = (statut) => {
  const s = (statut || '').toUpperCase();
  if (s === 'VALIDE' || s === 'APPROUVE') return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30';
  if (s === 'ATTENTE') return 'bg-amber-500/20 text-amber-400 border border-amber-500/30';
  return 'bg-slate-700 text-slate-300 border border-slate-600';
};

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const openPlan = (plan) => {
  if (activeTab.value === 'assemblage') router.push(`/plans/fabrication/edit/${plan.id}`);
  else if (activeTab.value === 'pf') router.push(`/plans/pf/edit/${plan.id}`);
  else router.push(`/plans/nc/edit/${plan.id}`); // Adjust path based on real routing
};
</script>
