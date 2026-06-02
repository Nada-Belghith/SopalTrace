<template>
  <div v-if="visible" class="fixed inset-0 z-[100] flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4">
    <!-- MODAL CONTAINER (White background) -->
    <div class="bg-white text-slate-800 w-full max-w-5xl rounded-2xl shadow-2xl overflow-hidden flex flex-col">
      
      <!-- HEADER (optional, or we can just have the dark card) -->
      <div class="p-6 flex justify-between items-center bg-white">
        <div class="w-full">
          <!-- DARK CARD: CONFIGURATION -->
          <div class="bg-[#0f172a] text-slate-100 rounded-xl p-6 shadow-lg border border-slate-800">
            
            <div class="flex justify-between items-start mb-6">
              <div>
                <h2 class="text-lg font-black text-amber-500 flex items-center gap-2 tracking-wide uppercase">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path></svg>
                  Configuration des colonnes personnalisées
                </h2>
                <p class="text-xs text-slate-400 mt-1">Ajoutez des colonnes de contrôle dynamique sans modification de la base de données.</p>
              </div>
              <button @click="close" class="text-slate-500 hover:text-white transition-colors">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
              </button>
            </div>

            <!-- FORM & LIST -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
              <!-- NOUVELLE COLONNE -->
              <div class="space-y-4">
                <h3 class="text-xs font-bold text-slate-300 uppercase tracking-wider">Nouvelle colonne</h3>
                
                <div class="space-y-4">
                  <div>
                    <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-wide mb-1.5">Libellé</label>
                    <input v-model="newCol.label" type="text" class="w-full bg-[#1e293b] border border-slate-700 rounded-lg px-4 py-2.5 text-sm text-white focus:border-amber-500 focus:ring-1 focus:ring-amber-500 outline-none transition-all placeholder-slate-600">
                  </div>
                  
                  <div class="grid grid-cols-2 gap-4">
                    <div>
                      <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-wide mb-1.5">Type</label>
                      <select v-model="newCol.type" class="w-full bg-[#1e293b] border border-slate-700 rounded-lg px-4 py-2.5 text-sm text-white focus:border-amber-500 focus:ring-1 focus:ring-amber-500 outline-none transition-all appearance-none">
                        <option value="Texte">Texte</option>
                        <option value="Nombre">Nombre</option>
                        <option value="Liste">Liste</option>
                      </select>
                    </div>
                    <div>
                      <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-wide mb-1.5">Insérer après</label>
                      <select v-model="newCol.insertAfter" class="w-full bg-[#1e293b] border border-slate-700 rounded-lg px-4 py-2.5 text-sm text-white focus:border-amber-500 focus:ring-1 focus:ring-amber-500 outline-none transition-all appearance-none">
                        <option v-for="col in baseColumns" :key="col.key" :value="col.key">{{ col.label }}</option>
                      </select>
                    </div>
                  </div>

                </div>
              </div>

              <!-- COLONNES ACTIVES -->
              <div class="space-y-4">
                <h3 class="text-xs font-bold text-slate-300 uppercase tracking-wider">Colonnes actives ({{ customColumns.length }})</h3>
                <div class="bg-[#1e293b]/50 border border-slate-700/50 rounded-xl h-[240px] p-4 flex flex-col gap-2 overflow-y-auto">
                  <div v-if="customColumns.length === 0" class="m-auto text-center text-slate-500 text-xs flex flex-col items-center gap-2">
                    <svg class="w-5 h-5 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                    Aucune colonne personnalisée pour le moment.
                  </div>
                  <div v-else v-for="(col, index) in customColumns" :key="index" class="bg-[#1e293b] border border-slate-700 rounded-lg p-3 flex justify-between items-center text-sm shadow-sm">
                    <div>
                      <span class="font-bold text-white">{{ col.label }}</span>
                      <span class="text-xs text-slate-400 ml-2">({{ col.type }})</span>
                    </div>
                    <button @click="removeColumn(index)" class="text-red-400 hover:text-red-300">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- APERÇU -->
      <div class="px-6 pb-2 bg-white">
        <div class="border border-slate-200 rounded-xl bg-white overflow-hidden text-slate-800 shadow-sm">
          <div class="p-3 border-b border-slate-100 bg-slate-50 flex items-center gap-2">
            <svg class="w-4 h-4 text-amber-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"></path></svg>
            <h3 class="text-xs font-bold text-slate-700 uppercase tracking-wider">Aperçu de la structure du plan</h3>
          </div>
          <div class="p-4 overflow-x-auto">
            <table class="w-full text-left text-xs whitespace-nowrap">
              <thead class="bg-[#0f172a] text-white font-black text-[10px] uppercase tracking-wider">
                <tr>
                  <th v-for="col in previewColumns" :key="col.key" class="px-4 py-4">{{ col.label }}</th>
                </tr>
              </thead>
              <tbody class="text-slate-600 bg-white border border-t-0 border-slate-200">
                <tr class="hover:bg-slate-50">
                  <td v-for="col in previewColumns" :key="col.key" class="px-4 py-4 border-r border-slate-100 last:border-0">
                    <span v-if="col.key === 'caracteristique'" class="text-slate-600">Exemple...</span>
                    <span v-else-if="col.key === 'limite_spec'">N/A</span>
                    <span v-else-if="col.key === 'type_controle'">Visuel</span>
                    <span v-else-if="col.key === 'moyen_controle'">Oeil Nu</span>
                    <span v-else-if="col.key === 'code_instrument'">-</span>
                    <span v-else-if="col.key === 'observations'" class="text-slate-400 border border-slate-200 rounded px-3 py-1.5 w-full inline-block bg-slate-50">Obs...</span>
                    <span v-else class="text-amber-600 font-medium bg-amber-50 px-2 py-1 rounded border border-amber-200">{{ col.label }} (Auto)</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- FOOTER -->
      <div class="p-6 bg-white flex justify-end gap-3 mt-4">
        <button @click="close" class="px-6 py-2.5 rounded-lg text-xs font-black tracking-widest text-slate-500 bg-white border-2 border-slate-200 hover:bg-slate-50 transition-colors uppercase">Annuler</button>
        <button @click="save" class="px-6 py-2.5 rounded-lg text-xs font-black tracking-widest text-slate-900 bg-amber-500 hover:bg-amber-400 flex items-center gap-2 transition-colors uppercase shadow-lg shadow-amber-500/20 border-2 border-amber-500">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4"></path></svg>
          Enregistrer
        </button>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  visible: { type: Boolean, default: false },
  modelValue: { type: Array, default: () => [] },
  baseColumns: { 
    type: Array, 
    default: () => [
      { key: 'caracteristique', label: 'CARACTÉRISTIQUE CONTRÔLÉE' },
      { key: 'limite_spec', label: 'LIMITE SPÉCIF.' },
      { key: 'type_controle', label: 'TYPE DE CONTRÔLE' },
      { key: 'moyen_controle', label: 'MOYEN DE CONTRÔLE' },
      { key: 'code_instrument', label: 'CODE INSTRUMENT' },
      { key: 'observations', label: 'OBSERVATIONS' }
    ]
  }
})

const emit = defineEmits(['update:visible', 'update:modelValue', 'save'])

const customColumns = ref([])
const newCol = ref({ label: '', type: 'Texte', insertAfter: 'code_instrument' })

watch(() => props.visible, (val) => {
  if (val) {
    customColumns.value = JSON.parse(JSON.stringify(props.modelValue))
    if (!newCol.value.insertAfter && props.baseColumns.length > 0) {
      newCol.value.insertAfter = props.baseColumns[props.baseColumns.length - 2].key // default to before observations
    }
  }
})

const addColumn = () => {
  if (!newCol.value.label) return
  customColumns.value.push({
    key: `custom_${Date.now()}`,
    label: newCol.value.label,
    type: newCol.value.type,
    insertAfter: newCol.value.insertAfter
  })
  newCol.value.label = ''
}

const removeColumn = (index) => {
  customColumns.value.splice(index, 1)
}

const previewColumns = computed(() => {
  let cols = [...props.baseColumns]
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

const close = () => {
  emit('update:visible', false)
  newCol.value.label = '' // Reset when closing
}

const save = () => {
  if (newCol.value.label.trim()) {
    customColumns.value.push({
      key: `custom_${Date.now()}`,
      label: newCol.value.label.trim(),
      type: newCol.value.type,
      insertAfter: newCol.value.insertAfter
    })
    newCol.value.label = ''
  }
  emit('update:modelValue', customColumns.value)
  emit('save', customColumns.value)
  close()
}
</script>