<script setup>
import { ref, computed } from 'vue'
import ExecPlanAssemblageView from '@/views/Operateur/ExecPlanAssemblageView.vue'

const props = defineProps({
  execControleOfId: {
    type: [String, Number],
    required: true
  },
  ofActif: {
    type: Object,
    default: null
  },
  selectedPoste: {
    type: String,
    default: null
  },
  tousLesPostesActifs: {
    type: Array,
    default: () => []
  },
  documentCategories: {
    type: Array,
    default: () => []
  },
  isLoadingDocs: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:selectedPoste', 'refresh', 'category-click', 'cloturer-category', 'open-doc', 'marquer-termine'])

const activeTab = ref('plan')

const getCat = (catId) => {
  return props.documentCategories.find(c => c.id === catId) || null
}

const currentCategory = computed(() => {
  if (activeTab.value === 'echantillonnage') return getCat('echantillonnage')
  if (activeTab.value === 'vm') return getCat('verifMachine')
  if (activeTab.value === 'rc') return getCat('documentControlePoste')
  return null
})

const isCatTermine = (cat) => {
  if (!cat) return false
  if (cat.isTermine !== undefined) return cat.isTermine
  if (!cat.docs || cat.docs.length === 0) return false
  return cat.docs.every(d => d.estTermine)
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <!-- Barre de sélection des postes actifs -->
    <div class="flex flex-wrap items-center justify-between gap-4 bg-slate-50 p-4 rounded-2xl border border-slate-200">
      <div class="flex items-center gap-3">
        <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">Sélectionnez le poste actif :</span>
        <div class="flex flex-wrap gap-2">
          <button 
            v-for="p in tousLesPostesActifs" 
            :key="p"
            @click="emit('update:selectedPoste', p)"
            class="px-5 py-2 rounded-xl font-bold text-sm transition-all shadow-sm flex items-center gap-2"
            :class="selectedPoste === p ? 'bg-blue-600 text-white shadow-blue-200 ring-2 ring-blue-600 ring-offset-2' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
          >
            <i class="pi pi-desktop text-xs" :class="selectedPoste === p ? 'text-white' : 'text-blue-500'"></i>
            {{ p }}
          </button>
          <span v-if="tousLesPostesActifs.length === 0" class="text-sm text-slate-400 italic">Aucun poste assigné</span>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <button 
          @click="emit('refresh')" 
          class="p-2 text-slate-400 hover:text-blue-600 hover:bg-white rounded-lg transition-all border border-transparent hover:border-slate-200"
          title="Actualiser les données"
        >
          <i class="pi pi-refresh"></i>
        </button>
      </div>
    </div>

    <!-- Navigation par Onglets -->
    <div class="flex border-b border-slate-200 gap-2 overflow-x-auto">
      <button 
        @click="activeTab = 'plan'" 
        class="px-6 py-3.5 font-bold text-sm rounded-t-xl transition-all border-b-2 flex items-center gap-2.5 whitespace-nowrap"
        :class="activeTab === 'plan' ? 'border-blue-600 text-blue-600 bg-blue-50/60' : 'border-transparent text-slate-500 hover:text-slate-800 hover:bg-slate-50'"
      >
        <i class="pi pi-sitemap text-base"></i>
        <span>Plan d'Assemblage & Notifications</span>
        <span class="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full font-extrabold">2 Étapes</span>
        <span v-if="getCat('planAss')" class="w-2 h-2 rounded-full" :class="isCatTermine(getCat('planAss')) ? 'bg-emerald-500' : 'bg-amber-500'"></span>
      </button>
      
      <button 
        @click="activeTab = 'echantillonnage'" 
        class="px-6 py-3.5 font-bold text-sm rounded-t-xl transition-all border-b-2 flex items-center gap-2.5 whitespace-nowrap"
        :class="activeTab === 'echantillonnage' ? 'border-blue-600 text-blue-600 bg-blue-50/60' : 'border-transparent text-slate-500 hover:text-slate-800 hover:bg-slate-50'"
      >
        <i class="pi pi-chart-pie text-base"></i>
        <span>Fiche Échantillonnage (FE0591)</span>
        <span v-if="getCat('echantillonnage')" class="w-2 h-2 rounded-full" :class="isCatTermine(getCat('echantillonnage')) ? 'bg-emerald-500' : 'bg-amber-500'"></span>
      </button>

      <button 
        @click="activeTab = 'vm'" 
        class="px-6 py-3.5 font-bold text-sm rounded-t-xl transition-all border-b-2 flex items-center gap-2.5 whitespace-nowrap"
        :class="activeTab === 'vm' ? 'border-blue-600 text-blue-600 bg-blue-50/60' : 'border-transparent text-slate-500 hover:text-slate-800 hover:bg-slate-50'"
      >
        <i class="pi pi-cog text-base"></i>
        <span>Vérification Machine</span>
        <span v-if="getCat('verifMachine')" class="w-2 h-2 rounded-full" :class="isCatTermine(getCat('verifMachine')) ? 'bg-emerald-500' : 'bg-amber-500'"></span>
      </button>

      <button 
        @click="activeTab = 'rc'" 
        class="px-6 py-3.5 font-bold text-sm rounded-t-xl transition-all border-b-2 flex items-center gap-2.5 whitespace-nowrap"
        :class="activeTab === 'rc' ? 'border-blue-600 text-blue-600 bg-blue-50/60' : 'border-transparent text-slate-500 hover:text-slate-800 hover:bg-slate-50'"
      >
        <i class="pi pi-check-square text-base"></i>
        <span>Résultat Contrôle Poste</span>
        <span v-if="getCat('documentControlePoste')" class="w-2 h-2 rounded-full" :class="isCatTermine(getCat('documentControlePoste')) ? 'bg-emerald-500' : 'bg-amber-500'"></span>
      </button>
    </div>

    <!-- Contenu des Onglets -->
    <div class="mt-2">
      <!-- Onglet 1 : Plan d'assemblage & Notifications -->
      <div v-if="activeTab === 'plan'">
        <ExecPlanAssemblageView 
          :execControleOfId="execControleOfId" 
          :selectedPoste="selectedPoste || tousLesPostesActifs[0] || 'Poste 1'" 
          :embedded="true" 
        />
      </div>

      <!-- Onglets 2, 3, 4 : Documents Secondaires -->
      <div v-else class="p-6 bg-slate-50/60 rounded-2xl border border-slate-200">
        <div v-if="isLoadingDocs" class="flex justify-center py-12">
          <i class="pi pi-spin pi-spinner text-4xl text-blue-600"></i>
        </div>

        <div v-else>
          <div class="flex flex-wrap justify-between items-center mb-6 gap-4">
            <div>
              <h3 class="text-xl font-bold text-slate-800 flex items-center gap-2">
                <i :class="currentCategory?.icon" class="text-blue-600"></i>
                {{ currentCategory?.title }}
              </h3>
              <p class="text-sm text-slate-500 mt-1">Gérez et remplissez les documents associés à cette catégorie pour le poste <strong class="text-slate-700">{{ selectedPoste || 'inconnu' }}</strong>.</p>
            </div>
            
            <button 
              v-if="currentCategory && currentCategory.docs && currentCategory.docs.length > 0 && !isCatTermine(currentCategory)"
              @click="emit('cloturer-category', currentCategory)"
              class="px-4 py-2 bg-emerald-600 text-white font-bold text-sm rounded-xl hover:bg-emerald-700 transition shadow-sm flex items-center gap-2"
            >
              <i class="pi pi-check-circle"></i> Clôturer cette catégorie
            </button>
          </div>

          <!-- Si aucun document initialisé -->
          <div v-if="!currentCategory || !currentCategory.docs || currentCategory.docs.length === 0" class="text-center py-14 bg-white rounded-2xl border border-dashed border-slate-300">
            <i class="pi pi-inbox text-5xl text-slate-300 mb-3 block"></i>
            <p class="text-slate-600 font-medium text-base">Aucun document actif ou requis dans cette catégorie pour ce poste.</p>
            <p class="text-slate-400 text-sm mt-1 mb-6">Vous pouvez initialiser ou ouvrir le document en cliquant ci-dessous.</p>
            <button 
              @click="emit('category-click', currentCategory)" 
              class="px-6 py-3 bg-blue-600 text-white font-bold text-sm rounded-xl hover:bg-blue-700 transition shadow-md inline-flex items-center gap-2"
            >
              <i class="pi pi-plus"></i> Initialiser / Remplir
            </button>
          </div>

          <!-- Grille des documents -->
          <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            <div 
              v-for="doc in currentCategory.docs" 
              :key="doc.id || doc.machineCode || Math.random()"
              class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex flex-col justify-between hover:border-blue-300 hover:shadow-md transition-all"
            >
              <div>
                <div class="flex justify-between items-start mb-3">
                  <span class="text-xs font-bold text-slate-400 uppercase tracking-wider">{{ doc.typeDocument || currentCategory?.title }}</span>
                  <span 
                    class="px-3 py-1 text-xs font-bold rounded-full"
                    :class="doc.estTermine ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-amber-50 text-amber-700 border border-amber-200'"
                  >
                    {{ doc.estTermine ? 'Terminé' : 'En attente' }}
                  </span>
                </div>
                <h4 class="font-bold text-slate-800 text-lg mb-1">{{ doc.libelleFormulaire || doc.machineCode || currentCategory?.title }}</h4>
                <p v-if="doc.machineCode" class="text-sm text-slate-500 mt-2">Machine assignée: <strong class="text-slate-700">{{ doc.machineCode }}</strong></p>
                <p v-if="doc.dateCreation" class="text-xs text-slate-400 mt-1">Créé le {{ new Date(doc.dateCreation).toLocaleDateString('fr-FR') }}</p>
              </div>

              <div class="mt-6 pt-4 border-t border-slate-100 flex gap-2">
                <button 
                  @click="emit('open-doc', doc)"
                  class="flex-1 px-4 py-2.5 bg-blue-600 text-white hover:bg-blue-700 font-bold text-sm rounded-xl transition-all text-center flex items-center justify-center gap-2 shadow-sm"
                >
                  <i class="pi pi-external-link text-xs"></i> Remplir / Modifier
                </button>
                <button 
                  v-if="!doc.estTermine && doc.id"
                  @click="emit('marquer-termine', doc.id)"
                  class="px-3 py-2.5 bg-emerald-50 text-emerald-600 hover:bg-emerald-600 hover:text-white rounded-xl transition-all border border-emerald-200 hover:border-emerald-600"
                  title="Marquer comme terminé"
                >
                  <i class="pi pi-check"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
