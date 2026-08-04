<template>
  <tr v-if="localLigne" class="hover:bg-blue-50/20 transition-colors group">
    <template v-for="col in columns" :key="col.key">
      <!-- 1. Caractéristique -->
      <td v-if="col.key === 'caracteristique'" class="p-2 align-top">
        <div :class="[
          'w-full flex flex-col rounded border overflow-hidden transition-colors',
          isReadOnly ? 'bg-slate-50 border-slate-200' : 'bg-white border-slate-300 focus-within:border-blue-500'
        ]">
          <!-- Zone texte -->
          <template v-if="isReadOnly">
            <div v-if="localLigne.libelleAffiche" class="px-2 py-1.5 text-[11px] text-slate-900 font-bold overflow-hidden text-ellipsis whitespace-nowrap cursor-not-allowed" :title="localLigne.libelleAffiche">
              {{ localLigne.libelleAffiche }}
            </div>
          </template>
          <input v-else v-model="localLigne.libelleAffiche"
                 type="text"
                 placeholder="Ex: Ø29.8, Entraxe..."
                 class="w-full px-2 py-1.5 text-[11px] outline-none bg-transparent text-slate-700" />
          
          <!-- Image preview if available -->
          <div v-if="localLigne.imageBase64" class="relative group/img px-2 pb-1.5 pt-0.5 bg-transparent w-fit">
             <img :src="localLigne.imageBase64" alt="Croquis" class="max-w-[120px] max-h-[60px] w-auto h-auto object-contain rounded" />
             <!-- Delete button on hover -->
             <button v-if="!isReadOnly" @click.stop="() => { localLigne.imageBase64 = null; }" class="absolute -top-1 -right-2 bg-white rounded-full p-0.5 shadow-sm border border-slate-200 text-slate-400 hover:text-red-500 hover:bg-red-50 opacity-0 group-hover/img:opacity-100 transition-opacity" title="Supprimer l'image">
               <i class="pi pi-times text-[9px]"></i>
             </button>
          </div>
        </div>
      </td>

      <!-- 2. Limite de Spécification -->
      <td v-else-if="col.key === 'limite_spec'" class="p-2 border-r border-slate-200 align-top min-w-[150px]">
        <div class="h-full w-full flex items-center justify-center pt-1">
          <input v-model="localLigne.limiteSpecTexte"
                 type="text"
                 placeholder="Ex: ±0.3, 0 ; -0.3..."
                 :disabled="isReadOnly"
                 :class="isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 focus:border-blue-500'"
                 class="w-full text-center border rounded px-2 py-1.5 outline-none text-[11px]">
        </div>
      </td>

      <!-- 3. Type de contrôle -->
      <td v-else-if="col.key === 'type_controle'" class="p-2 border-r border-slate-200 align-top w-[12%]">
        <select v-model="localLigne.typeControleId" :disabled="isReadOnly" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer disabled:opacity-60 disabled:bg-slate-50">
          <option :value="null" disabled>-- Type * --</option>
          <option v-for="tco in typesControle" :key="tco.id" :value="tco.id">{{ tco.libelle || tco.code }}</option>
        </select>
      </td>

      <!-- 4. Moyen de contrôle -->
      <td v-else-if="col.key === 'moyen_controle'" class="p-2 border-r border-slate-200 align-top w-[12%]">
        <select v-model="localLigne.moyenControleId" :disabled="isReadOnly" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer disabled:opacity-60 disabled:bg-slate-50">
          <option :value="null">-- Moyen --</option>
          <option v-for="mco in moyensControle" :key="mco.id" :value="mco.id">{{ mco.libelle }}</option>
        </select>
      </td>

      <!-- 5. Code Instrument -->
      <td v-else-if="col.key === 'code_instrument'" class="p-2 border-r border-slate-200 align-top w-[12%]">
        <div class="flex gap-1 items-center">
          <select @change="(e) => { if (e.target.value) localLigne.instrumentCode = e.target.value; e.target.value = ''; }"
                  :disabled="isVisuel || isReadOnly"
                  :class="(isVisuel || isReadOnly) ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white cursor-pointer'"
                  class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
            <option value="">-- Outil --</option>
            <option v-for="ins in instruments" :key="ins.codeInstrument" :value="ins.codeInstrument">
              {{ ins.codeInstrument }}
            </option>
          </select>
          <input v-model="localLigne.instrumentCode" type="text" placeholder="Perso"
                 :disabled="isVisuel || isReadOnly"
                 :class="(isVisuel || isReadOnly) ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white'"
                 class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[10px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
        </div>
      </td>

      <!-- 6. Observations -->
      <td v-else-if="col.key === 'observations'" class="p-2 border-r border-slate-200 align-top w-[18%] min-w-[180px]">
        <input v-model="localLigne.observations" type="text" placeholder="Infos (Optionnel)..." :disabled="isReadOnly"
               class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-xs text-slate-600 outline-none focus:border-blue-500 disabled:opacity-60 disabled:bg-slate-50">
      </td>

      <!-- 7. Colonnes Dynamiques (Custom) -->
      <td v-else-if="col.isCustom" class="p-2 border-r border-slate-200 align-top">
        <input v-if="localLigne.valeursColonnesSpecifiques"
               v-model="localLigne.valeursColonnesSpecifiques[col.key]"
               :type="col.type === 'Nombre' ? 'number' : 'text'"
               :disabled="isReadOnly"
               :placeholder="col.label"
               :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-semibold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-600 focus:border-blue-500']" />
      </td>

      <!-- 8. Actions (Supprimer) -->
      <td v-else-if="col.key === 'actions' && !isReadOnly" class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity w-8">
        <button @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
          <i class="pi pi-trash"></i>
        </button>
      </td>
      <td v-else-if="col.key === 'actions' && isReadOnly" class="p-2 w-8"></td>
    </template>
  </tr>
</template>

<script setup>
import { ref, computed, watch } from 'vue';

const props = defineProps({
  ligne: {
    type: Object,
    required: true
  },
  columns: {
    type: Array,
    required: true
  },
  isReadOnly: {
    type: Boolean,
    default: false
  },
  typesControle: {
    type: Array,
    default: () => []
  },
  moyensControle: {
    type: Array,
    default: () => []
  },
  instruments: {
    type: Array,
    default: () => []
  }
});

const emit = defineEmits(['update', 'remove']);

function stableStringify(obj) {
  if (obj === undefined) return undefined;
  if (obj === null || typeof obj !== 'object') return JSON.stringify(obj);
  if (Array.isArray(obj)) return '[' + obj.map(stableStringify).join(',') + ']';
  const keys = Object.keys(obj).sort();
  return '{' + keys.map(k => JSON.stringify(k) + ':' + stableStringify(obj[k])).join(',') + '}';
}

const localLigne = ref({ ...props.ligne });
if (!localLigne.value.valeursColonnesSpecifiques) {
  localLigne.value.valeursColonnesSpecifiques = {};
}

const isSyncing = ref(false);

watch(() => props.ligne, (newVal) => {
  if (newVal && stableStringify(newVal) !== stableStringify(localLigne.value)) {
    isSyncing.value = true;
    localLigne.value = JSON.parse(stableStringify(newVal));
    if (!localLigne.value.valeursColonnesSpecifiques) {
      localLigne.value.valeursColonnesSpecifiques = {};
    }
    setTimeout(() => { isSyncing.value = false; }, 0);
  }
}, { deep: true });

watch(localLigne, (newVal) => {
  if (isSyncing.value) return;
  emit('update', newVal);
}, { deep: true });

// Check if the control type implies a visual check to disable tools
const isVisuel = computed(() => {
  if (!props.typesControle) return false;
  const tc = props.typesControle.find(t => t.id === localLigne.value.typeControleId);
  return tc && (tc.libelle === 'Visuel' || tc.code === 'Visuel');
});
</script>
