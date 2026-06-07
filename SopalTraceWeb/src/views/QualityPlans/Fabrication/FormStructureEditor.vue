<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <div class="max-w-[1600px] mx-auto">
      
      <!-- HEADER -->
      <div class="bg-white rounded-xl border border-slate-200 p-6 mb-6 shadow-sm flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <div class="flex items-center gap-3">
            <div class="w-12 h-12 rounded-xl bg-amber-500/10 flex items-center justify-center text-amber-500 shadow-inner">
              <i class="pi pi-cog text-xl animate-spin-slow"></i>
            </div>
            <div>
              <h1 class="text-xl font-black text-slate-900 tracking-tight">Configuration Spécifique</h1>
              <p class="text-xs text-slate-500 mt-0.5">Gestion du formulaire de référence en cours de fabrication</p>
            </div>
          </div>
        </div>
        <div class="flex flex-wrap items-center gap-3 bg-slate-50 px-4 py-2 rounded-xl border border-slate-200">
          <div class="flex items-center gap-1.5">
            <span class="text-[10px] font-black text-slate-400 uppercase">Code :</span>
            <span class="font-mono font-bold text-sm text-slate-700">{{ formCode }}</span>
          </div>
          <span class="text-slate-300">|</span>
          <div class="flex items-center gap-1.5">
            <span class="text-[10px] font-black text-slate-400 uppercase">Version active :</span>
            <span class="font-mono font-bold text-sm text-slate-700">V{{ formVersion }}.0</span>
          </div>
          <span class="bg-emerald-500/10 text-emerald-600 px-2 py-0.5 rounded text-[10px] uppercase font-black tracking-widest border border-emerald-500/20">ACTIF</span>
        </div>
      </div>

      <!-- MAIN CONFIGURATOR CARD -->
      <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
        <!-- Card Header (Dark Title) -->
        <div class="bg-[#1e293b] text-white px-6 py-4 flex justify-between items-center">
          <div class="flex items-center gap-3 font-bold tracking-wide">
            <i class="pi pi-sliders-h text-lg text-amber-500"></i>
            <span>Structure des Plans Spécifiques</span>
          </div>
          <button @click="$router.push('/dev/hub')" class="text-slate-400 hover:text-white transition-colors">
            <i class="pi pi-times text-lg"></i>
          </button>
        </div>

        <div class="p-6 md:p-8 space-y-8">
          <!-- Top Section: Config Form & Actives columns -->
          <div v-if="!isViewOnly" class="bg-[#0f172a] text-slate-100 rounded-2xl p-6 md:p-8 shadow-2xl border border-slate-800">
            <div class="flex justify-between items-start mb-8">
              <div>
                <h2 class="text-lg font-black text-amber-500 flex items-center gap-2.5 tracking-wide uppercase">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
                  </svg>
                  CONFIGURATION DES COLONNES PERSONNALISÉES
                </h2>
                <p class="text-xs text-slate-400 mt-1">Ajoutez des colonnes de contrôle dynamique sans modification de la base de données.</p>
              </div>
            </div>

            <!-- Form & Active Columns Grid -->
            <div class="grid grid-cols-1 lg:grid-cols-12 gap-8">
              <!-- Left side: New Column Creation -->
              <div class="lg:col-span-5 space-y-6">
                <h3 class="text-xs font-black text-slate-300 uppercase tracking-widest border-b border-slate-800 pb-3">Nouvelle colonne</h3>
                
                <div class="space-y-4">
                  <div>
                    <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-2">Libellé</label>
                    <input 
                      v-model="newCol.label" 
                      type="text" 
                      placeholder="Ex: Instructions spécifiques..."
                      class="w-full bg-[#1e293b] border border-slate-700 rounded-lg px-4 py-2.5 text-sm text-white focus:border-amber-500 focus:ring-1 focus:ring-amber-500 outline-none transition-all placeholder-slate-600 font-medium"
                    >
                  </div>
                  
                  <div class="grid grid-cols-2 gap-4">
                    <div>
                      <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-2">Type</label>
                      <select 
                        v-model="newCol.type" 
                        class="w-full bg-[#1e293b] border border-slate-700 rounded-lg px-4 py-2.5 text-sm text-white focus:border-amber-500 focus:ring-1 focus:ring-amber-500 outline-none transition-all appearance-none font-semibold cursor-pointer"
                      >
                        <option value="Texte">Texte</option>
                        <option value="Nombre">Nombre</option>
                        <option value="Liste">Liste</option>
                      </select>
                    </div>
                    <div>
                      <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-2">Insérer après</label>
                      <select 
                        v-model="newCol.insertAfter" 
                        class="w-full bg-[#1e293b] border border-slate-700 rounded-lg px-4 py-2.5 text-sm text-white focus:border-amber-500 focus:ring-1 focus:ring-amber-500 outline-none transition-all appearance-none font-semibold cursor-pointer"
                      >
                        <option v-for="col in baseColumns" :key="col.key" :value="col.key">{{ col.label }}</option>
                      </select>
                    </div>
                  </div>
                  <button @click="addColumn" class="w-full bg-amber-500 hover:bg-amber-600 text-[#0f172a] font-black uppercase text-xs py-2.5 rounded-lg transition-colors">
                    Ajouter la colonne
                  </button>
                </div>
              </div>

              <!-- Divider line for LG screens -->
              <div class="hidden lg:block lg:col-span-1 flex justify-center">
                <div class="h-full w-px bg-slate-800"></div>
              </div>

              <!-- Right side: Actives Columns List -->
              <div class="lg:col-span-6 space-y-6">
                <h3 class="text-xs font-black text-slate-300 uppercase tracking-widest border-b border-slate-800 pb-3 flex justify-between items-center">
                  <span>Colonnes actives ({{ customColumns.length }})</span>
                  <span v-if="customColumns.length > 0" class="text-[10px] text-amber-500 font-bold bg-amber-500/10 px-2 py-0.5 rounded">Personnalisées</span>
                </h3>
                
                <div class="bg-[#1e293b]/40 border border-slate-800/80 rounded-2xl h-[240px] p-4 flex flex-col gap-2.5 overflow-y-auto custom-scrollbar">
                  <div v-if="customColumns.length === 0" class="m-auto text-center text-slate-500 text-xs flex flex-col items-center gap-3 py-8">
                    <div class="w-12 h-12 rounded-full bg-slate-800/50 flex items-center justify-center">
                      <i class="pi pi-info-circle text-lg text-slate-600"></i>
                    </div>
                    <span class="font-medium">Aucune colonne personnalisée pour le moment.</span>
                  </div>
                  
                  <div 
                    v-else 
                    v-for="(col, index) in customColumns" 
                    :key="col.key" 
                    class="bg-[#1e293b] border border-slate-700/60 hover:border-slate-600 rounded-xl p-3.5 flex justify-between items-center text-sm shadow-sm transition-all hover:translate-x-0.5 group"
                  >
                    <div class="flex items-center gap-3">
                      <div class="w-2 h-2 rounded-full bg-amber-500"></div>
                      <div>
                        <span class="font-bold text-white tracking-wide">{{ col.label }}</span>
                        <span class="text-xs text-slate-400 ml-2 bg-slate-800/50 px-2 py-0.5 rounded border border-slate-700 font-mono">{{ col.type }}</span>
                      </div>
                    </div>
                    <div class="flex items-center gap-3">
                      <span class="text-[10px] text-slate-500 font-medium">Après : {{ getColumnLabel(col.insertAfter) }}</span>
                      <button 
                        v-if="!isViewOnly"
                        @click="removeColumn(index)" 
                        class="text-slate-500 hover:text-red-400 p-1.5 hover:bg-red-500/10 rounded-lg transition-colors cursor-pointer"
                        title="Supprimer la colonne"
                      >
                        <i class="pi pi-trash text-sm"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Bottom Section: Live Preview -->
          <div class="border border-slate-200 rounded-2xl bg-white overflow-hidden shadow-sm">
            <div class="p-4 border-b border-slate-100 bg-slate-50 flex justify-between items-center">
              <div class="flex items-center gap-2">
                <div class="w-2 h-2 rounded-full bg-amber-500 animate-ping"></div>
                <h3 class="text-xs font-black text-slate-700 uppercase tracking-widest">Aperçu de la structure du plan</h3>
              </div>
              <span class="text-[10px] font-black text-slate-400 uppercase tracking-wider">Visualisation temps réel</span>
            </div>
            
            <div class="p-6 overflow-x-auto custom-scrollbar">
              <table class="w-full text-left text-xs border border-slate-200 rounded-lg overflow-hidden whitespace-nowrap">
                <thead class="bg-[#0f172a] text-white font-black text-[10px] uppercase tracking-widest border-b border-slate-800">
                  <tr>
                    <th v-for="col in previewColumns" :key="col.key" class="px-6 py-4.5 border-r border-slate-800/50 last:border-0">{{ col.label }}</th>
                  </tr>
                </thead>
                <tbody class="text-slate-600 bg-white">
                  <tr class="hover:bg-slate-50 transition-colors">
                    <td v-for="col in previewColumns" :key="col.key" class="px-6 py-5 border-r border-slate-100 last:border-0 font-medium">
                      <span v-if="col.key === 'caracteristique'" class="text-slate-900 font-bold">Exemple...</span>
                      <span v-else-if="col.key === 'limite_spec'" class="text-slate-500 font-mono">N/A</span>
                      <span v-else-if="col.key === 'type_controle'" class="bg-blue-50 text-blue-700 px-2 py-0.5 rounded font-bold border border-blue-100 uppercase tracking-wider text-[10px]">Visuel</span>
                      <span v-else-if="col.key === 'moyen_controle'" class="text-slate-700">Oeil Nu</span>
                      <span v-else-if="col.key === 'code_instrument'" class="text-slate-400 font-mono">-</span>
                      <span v-else-if="col.key === 'observations'" class="text-slate-400 border border-slate-200 rounded-lg px-3 py-1.5 w-44 inline-block bg-slate-50 font-normal">Obs...</span>
                      
                      <!-- Custom Columns Preview -->
                      <span v-else class="text-amber-700 font-bold bg-amber-50 px-2.5 py-1 rounded-lg border border-amber-200 flex items-center gap-1.5 w-fit">
                        <i class="pi pi-lock text-[10px]"></i>
                        {{ col.label }} (Auto)
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- ACTIONS FOOTER -->
        <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end gap-3.5">
          <button 
            @click="$router.push('/dev/hub-plans')" 
            class="px-6 py-3 rounded-xl text-xs font-black tracking-widest text-slate-500 bg-white border-2 border-slate-200 hover:bg-slate-50 hover:text-slate-700 transition-all uppercase active:scale-95 cursor-pointer"
            :disabled="isSaving"
          >
            {{ isViewOnly ? 'Fermer' : 'Annuler' }}
          </button>
          
          <button 
            v-if="!isViewOnly"
            @click="saveStructure" 
            class="px-6 py-3 rounded-xl text-xs font-black tracking-widest text-slate-900 bg-amber-500 hover:bg-amber-400 flex items-center gap-2 transition-all uppercase active:scale-95 shadow-lg shadow-amber-500/20 border-2 border-amber-500 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="isSaving"
          >
            <i v-if="isSaving" class="pi pi-spinner animate-spin"></i>
            <i v-else class="pi pi-save"></i>
            {{ isSaving ? 'Enregistrement...' : 'Enregistrer' }}
          </button>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import apiClient from '@/services/apiClient'

const route = useRoute();
const toast = useToast()

const isSaving = ref(false)
const isViewOnly = computed(() => route.query.view === 'true')
const customColumns = ref([])
const newCol = ref({ label: '', type: 'Texte', insertAfter: 'code_instrument' })
const formCode = ref('')
const formVersion = ref(0)

const baseColumns = [
  { key: 'caracteristique', label: 'CARACTÉRISTIQUE CONTRÔLÉE' },
  { key: 'limite_spec', label: 'LIMITE SPÉCIF.' },
  { key: 'type_controle', label: 'TYPE DE CONTRÔLE' },
  { key: 'moyen_controle', label: 'MOYEN DE CONTRÔLE' },
  { key: 'code_instrument', label: 'CODE INSTRUMENT' },
  { key: 'observations', label: 'OBSERVATIONS' }
]

// Fetch the RefFormulaire for EN_COURS_DE_FABRICATION
const loadStructure = async () => {
  try {
    const endpoint = route.query.id 
      ? `/referentiels/formulaires/${route.query.id}`
      : '/referentiels/formulaires/role/EN_COURS_DE_FABRICATION'
      
    const res = await apiClient.get(endpoint)
    if (res.data?.success && res.data?.data) {
      formCode.value = res.data.data.codeReference
      formVersion.value = res.data.data.version
      const configJson = res.data.data.configurationStructureJson
      if (configJson) {
        customColumns.value = JSON.parse(configJson)
      } else {
        customColumns.value = []
      }
    }
  } catch (error) {
    console.error('Erreur chargement structure:', error)
    toast.add({
      severity: 'error',
      summary: 'Erreur',
      detail: 'Impossible de charger la structure du formulaire.',
      life: 5000
    })
  }
}

onMounted(loadStructure)

const getColumnLabel = (key) => {
  const base = baseColumns.find(c => c.key === key)
  if (base) return base.label
  const custom = customColumns.value.find(c => c.key === key)
  return custom ? custom.label : key
}

const addColumn = () => {
  if (!newCol.value.label.trim()) {
    toast.add({
      severity: 'warn',
      summary: 'Attention',
      detail: 'Le libellé de la colonne est requis.',
      life: 3000
    })
    return
  }

  // Check duplicates
  const exists = customColumns.value.some(
    c => c.label.toLowerCase().trim() === newCol.value.label.toLowerCase().trim()
  ) || baseColumns.some(
    b => b.label.toLowerCase().trim() === newCol.value.label.toLowerCase().trim()
  )

  if (exists) {
    toast.add({
      severity: 'warn',
      summary: 'Doublon',
      detail: 'Une colonne avec ce libellé existe déjà.',
      life: 3000
    })
    return
  }

  customColumns.value.push({
    key: `custom_${Date.now()}`,
    label: newCol.value.label.trim(),
    type: newCol.value.type,
    insertAfter: newCol.value.insertAfter
  })

  // Toast success
  toast.add({
    severity: 'success',
    summary: 'Colonne ajoutée',
    detail: `La colonne "${newCol.value.label}" a été ajoutée temporairement à l'aperçu.`,
    life: 3000
  })

  // Reset inputs
  newCol.value.label = ''
}

const removeColumn = (index) => {
  const removed = customColumns.value.splice(index, 1)[0]
  
  // Re-adjust insertAfter for other columns that might have pointed to the deleted one
  customColumns.value.forEach(cc => {
    if (cc.insertAfter === removed.key) {
      cc.insertAfter = 'code_instrument'
    }
  })
}

const previewColumns = computed(() => {
  let cols = [...baseColumns]
  customColumns.value.forEach(cc => {
    const insertIdx = cols.findIndex(c => c.key === cc.insertAfter)
    if (insertIdx !== -1) {
      cols.splice(insertIdx + 1, 0, cc)
    } else {
      cols.push(cc)
    }
  })
  
  if (newCol.value.label.trim()) {
    const draftCol = {
      key: 'draft_col',
      label: newCol.value.label.trim(),
      type: newCol.value.type,
      insertAfter: newCol.value.insertAfter
    }
    const insertIdx = cols.findIndex(c => c.key === draftCol.insertAfter)
    if (insertIdx !== -1) {
      cols.splice(insertIdx + 1, 0, draftCol)
    } else {
      cols.push(draftCol)
    }
  }
  
  return cols
})

// Save custom columns array to the backend DB as JSON
const saveStructure = async () => {
  if (newCol.value.label.trim()) {
    customColumns.value.push({
      key: `custom_${Date.now()}`,
      label: newCol.value.label.trim(),
      type: newCol.value.type,
      insertAfter: newCol.value.insertAfter
    })
    newCol.value.label = ''
  }

  isSaving.value = true
  try {
    const payload = {
      configurationStructureJson: JSON.stringify(customColumns.value)
    }
    const res = await apiClient.put('/referentiels/formulaires/role/EN_COURS_DE_FABRICATION', payload)
    
    if (res.data?.success) {
      toast.add({
        severity: 'success',
        summary: 'Enregistré',
        detail: 'La structure du plan spécifique a été mise à jour avec succès.',
        life: 4000
      })
      await loadStructure()
    }
  } catch (error) {
    console.error('Erreur sauvegarde structure:', error)
    const errorMsg = error.response?.data?.message || 'Erreur lors de la communication avec le serveur.'
    toast.add({
      severity: 'error',
      summary: 'Erreur d\'enregistrement',
      detail: errorMsg,
      life: 6000
    })
  } finally {
    isSaving.value = false
  }
}
</script>

<style scoped>
.animate-spin-slow {
  animation: spin 8s linear infinite;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

/* Custom Scrollbar for list and table */
.custom-scrollbar::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: rgba(15, 23, 42, 0.05);
  border-radius: 8px;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background: rgba(15, 23, 42, 0.2);
  border-radius: 8px;
}
.custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: rgba(15, 23, 42, 0.3);
}
</style>
