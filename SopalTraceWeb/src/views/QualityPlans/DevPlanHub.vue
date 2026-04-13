<template>
  <div class="max-w-[1400px] mx-auto animate-in fade-in duration-500">
    
    <!-- HEADER -->
    <div class="flex flex-col md:flex-row justify-between items-start md:items-center mb-10 gap-6">
      <div>
        <h1 class="text-3xl font-black text-slate-900 tracking-tight">Gestion des Modèles</h1>
        <p class="text-slate-500 mt-1 font-medium text-sm">Visualisez et gérez vos modèles des plans du contrôle existants.</p>
      </div>
    </div>

    <!-- FILTRES DE CATÉGORIES -->
    <div class="flex flex-wrap gap-2 mb-8 p-1.5 bg-slate-200/50 rounded-2xl w-max border border-slate-200">
      <button 
        v-for="cat in categories" :key="cat.id"
        @click="filtreActif = cat.id"
        class="px-5 py-2.5 rounded-xl text-xs font-black uppercase tracking-wider transition-all"
        :class="filtreActif === cat.id ? 'bg-white text-blue-600 shadow-sm' : 'text-slate-500 hover:text-slate-700'"
      >
        {{ cat.label }}
      </button>
    </div>

    <!-- LOADER -->
    <div v-if="isLoading" class="flex flex-col items-center justify-center py-20 text-blue-500">
      <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
      <p class="text-sm font-bold text-slate-500 uppercase tracking-widest">Chargement des modèles depuis l'ERP...</p>
    </div>

    <!-- GRILLE DE VISUALISATION -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
      
      <!-- CARTE DE MODÈLE -->
      <div v-for="mod in modelesFiltrés" :key="mod.id" 
           class="bg-white rounded-2xl border border-slate-200 shadow-sm hover:shadow-xl transition-all overflow-hidden flex flex-col group"
           :class="getHoverBorderColor(mod.category)">
        
        <div class="p-6 flex-1">
          <div class="flex justify-between items-start mb-4">
            <h3 class="text-base font-black text-slate-900 leading-tight">{{ mod.libelle }}</h3>
            <span class="px-2 py-1 bg-slate-100 text-slate-500 text-[10px] font-bold uppercase rounded-md tracking-wider">v{{ mod.version }}</span>
          </div>
          
          <!-- NOUVEAU DESIGN DES BADGES (Plus doux, avec icônes) -->
          <div class="flex flex-wrap gap-2 mb-6 mt-auto">
            <div v-if="mod.nature && mod.nature !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-box text-slate-400 text-[11px]"></i> {{ mod.nature }}
            </div>
            <div v-if="mod.type && mod.type !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-tag text-slate-400 text-[11px]"></i> {{ mod.type }}
            </div>
            <div v-if="mod.poste && mod.poste !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-map-marker text-slate-400 text-[11px]"></i> {{ mod.poste }}
            </div>
          </div>
          
          <p class="text-xs text-slate-500 flex items-center gap-2">
            <i class="pi pi-list"></i> {{ mod.description }}
          </p>
        </div>
        
        <!-- ACTIONS -->
        <div class="p-3 bg-slate-50 border-t border-slate-100 grid grid-cols-2 gap-3">
          <button @click="editer(mod.category)" class="bg-slate-800 text-white py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-slate-900 transition-colors">
            <i class="pi pi-pencil"></i> Éditer
          </button>
          <button class="bg-emerald-100 text-emerald-700 py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-emerald-200 transition-colors">
            <i class="pi pi-copy"></i> Cloner
          </button>
        </div>
      </div>

      <!-- ÉTAT VIDE -->
      <div v-if="modelesFiltrés.length === 0" class="col-span-full py-12 text-center bg-slate-50 rounded-2xl border-2 border-dashed border-slate-200">
        <i class="pi pi-inbox text-4xl text-slate-300 mb-3"></i>
        <h3 class="text-lg font-bold text-slate-700">Aucun modèle trouvé</h3>
        <p class="text-sm text-slate-500 mt-1">Utilisez la barre latérale pour créer un nouveau modèle dans cette catégorie.</p>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import apiClient from '@/services/apiClient'; // Importation de votre client API

const router = useRouter();
const filtreActif = ref('ALL');
const isLoading = ref(true);
const modeles = ref([]);

const categories = [
  { id: 'ALL', label: 'Tous les modèles' },
  { id: 'FAB', label: 'En cours Fabrication' },
  { id: 'PF',  label: 'Produit Fini' },
  { id: 'VM',  label: 'Vérif Machine' },
  { id: 'ECH', label: 'Échantillonnage' }
];

onMounted(async () => {
  try {
    isLoading.value = true;
    const response = await apiClient.get('/hub/modeles');
    
    // CORRECTION ICI : On descend d'un niveau supplémentaire pour accéder au tableau !
    // Si l'API renvoie un objet avec "data", on le prend, sinon on prend la réponse directe.
    modeles.value = response.data.data || response.data || [];
    
  } catch (error) {
    console.error("Erreur lors de la récupération des modèles :", error);
  } finally {
    isLoading.value = false;
  }
});

const modelesFiltrés = computed(() => {
  if (filtreActif.value === 'ALL') return modeles.value;
  return modeles.value.filter(m => m.category === filtreActif.value);
});

const getHoverBorderColor = (category) => {
  const colors = {
    'FAB': 'hover:border-blue-400',
    'ASS': 'hover:border-indigo-400',
    'PF':  'hover:border-emerald-400',
    'VM':  'hover:border-orange-400',
    'ECH': 'hover:border-purple-400'
  };
  return colors[category] || 'hover:border-slate-400';
};

const getBadgeColor = (category) => {
  const colors = {
    'FAB': 'bg-blue-50 text-blue-600 border border-blue-200',
    'ASS': 'bg-indigo-50 text-indigo-600 border border-indigo-200',
    'PF':  'bg-emerald-50 text-emerald-600 border border-emerald-200',
    'VM':  'bg-orange-50 text-orange-600 border border-orange-200',
    'ECH': 'bg-purple-50 text-purple-600 border border-purple-200'
  };
  return colors[category] || 'bg-slate-100 text-slate-600 border border-slate-200';
};

// Fonction d'édition (Peut inclure l'ID à l'avenir pour ouvrir directement le bon plan)
const editer = (category) => {
  switch (category) {
    case 'FAB': 
      router.push('/dev/fabrication/nouveau'); // Plus tard: router.push(`/dev/fabrication/editer/${id}`)
      break;
    case 'ASS': 
      router.push('/dev/assemblage/nouveau'); 
      break;
    case 'PF': 
      router.push('/dev/produit-fini/nouveau'); 
      break;
    case 'VM': 
      router.push('/dev/verif-machine/nouveau'); 
      break;
    case 'ECH': 
      router.push('/dev/echantillonnage/nouveau'); 
      break;
    default: 
      console.warn("Catégorie inconnue pour l'édition.");
  }
};
</script>