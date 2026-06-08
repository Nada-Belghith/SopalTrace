<template>
  <tr class="hover:bg-blue-50/10 transition-colors group">
    <template v-for="col in columns" :key="col.key">
      <td v-if="col.key === 'caracteristique'" class="p-2 align-top">
        <div class="flex items-center gap-1">
          <input v-model="localLigne.libelleAffiche" :disabled="isReadOnly"
                 type="text"
                 placeholder="Ex: Ø29.8, Entraxe..."
                 :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 text-slate-700 focus:border-blue-500']" />
        </div>
      </td>

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

      <td v-else-if="col.key === 'type_controle'" class="p-2 border-r border-slate-200 align-top">
          <select v-model="localLigne.typeControleId" :disabled="isReadOnly" 
                  :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-700 cursor-pointer focus:border-blue-500']">
          <option :value="null" disabled>-- Type * --</option>
          <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.libelle }}</option>
        </select>
      </td>

      <td v-else-if="col.key === 'moyen_controle'" class="p-2 border-r border-slate-200 align-top">
        <select v-model="localLigne.moyenControleId" :disabled="isReadOnly" 
                :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'cursor-pointer bg-white border border-slate-200 text-slate-700 focus:border-blue-500']">
          <option :value="null" disabled>-- Moyen  --</option>
          <option v-for="mc in (store.moyensControle || [])" :key="mc.id" :value="mc.id">{{ mc.libelle }}</option>
        </select>
      </td>

      <td v-else-if="col.key === 'code_instrument'" class="p-2 border-r border-slate-200 align-top">
        <div class="flex gap-1">
            <select 
            @change="(e) => { if (e.target.value) localLigne.instrumentCode = e.target.value; e.target.value = ''; }"
            :disabled="isVisuel || isReadOnly"
            :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-50' : (isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-700 cursor-pointer focus:border-blue-500')"
            class="flex-1 border rounded px-2 py-1.5 text-[11px] outline-none transition-opacity">
            <option value="">-- Outil --</option>
            <option v-for="ins in (store.instruments || [])" :key="ins.codeInstrument" :value="ins.codeInstrument">
              {{ ins.codeInstrument }}
            </option>
          </select>
          <input 
            v-model="localLigne.instrumentCode" 
            type="text" 
            placeholder="Perso"
            :disabled="isVisuel || isReadOnly"
            :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-50' : (isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-700 focus:border-blue-500')"
            class="flex-1 border rounded px-2 py-1.5 text-[11px] outline-none transition-opacity" />
        </div>
      </td>

      <td v-else-if="col.key === 'observations'" class="p-2 border-r border-slate-200 align-top">
        <input v-model="localLigne.observations" type="text" placeholder="Observations (Optionnel)..." :disabled="isReadOnly"
               :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-semibold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-600 focus:border-blue-500']">
      </td>

      <td v-else-if="col.isCustom" class="p-2 border-r border-slate-200 align-top">
        <template v-if="localLigne.valeursColonnesSpecifiques">
          <input v-model="localLigne.valeursColonnesSpecifiques[col.key]" 
                 :type="col.type === 'Nombre' ? 'number' : 'text'" 
                 :disabled="isReadOnly"
                 :placeholder="col.label"
                 :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-semibold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-600 focus:border-blue-500']" />
        </template>
      </td>

      <td v-else-if="col.key === 'actions'" class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity w-8">
        <button v-if="!isReadOnly" @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
          <i class="pi pi-trash"></i>
        </button>
      </td>
    </template>
  </tr>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { usePfPlanStore } from '@/stores/pfPlanStore';

const props = defineProps({
  ligne: { type: Object, required: true },
  columns: { type: Array, required: true },
  isReadOnly: { type: Boolean, default: false },
  operationCode: { type: String, default: '' } 
});

const isReadOnly = computed(() => props.isReadOnly);
const emit = defineEmits(['remove', 'update']);
const fabStore = useFabModeleStore();
const pfStore = usePfPlanStore();
const store = props.operationCode === 'PF' ? pfStore : fabStore;

const localLigne = ref({
  ...props.ligne,
  valeursColonnesSpecifiques: props.ligne.valeursColonnesSpecifiques || {}
});
const isSyncingFromParent = ref(false);

const isVisuel = computed(() => {
  if (localLigne.value.typeControleId) {
    const tc = (store.typesControle || []).find(t => t.id === localLigne.value.typeControleId);
    if (tc && tc.libelle.toUpperCase().includes('VISUEL')) {
      return true;
    }
  }
  return false;
});

watch(() => props.ligne, (newVal) => {
  if (!isSyncingFromParent.value) {
    isSyncingFromParent.value = true;
    localLigne.value = { 
      ...newVal,
      valeursColonnesSpecifiques: newVal.valeursColonnesSpecifiques || {}
    };
    setTimeout(() => isSyncingFromParent.value = false, 0);
  }
}, { deep: true });

watch(localLigne, (newVal) => {
  if (isSyncingFromParent.value) return;
  const sourceLocale = JSON.stringify(newVal);
  const sourceSource = JSON.stringify(props.ligne);
  if (sourceLocale !== sourceSource) {
    emit('update', JSON.parse(sourceLocale));
  }
}, { deep: true });

</script>
