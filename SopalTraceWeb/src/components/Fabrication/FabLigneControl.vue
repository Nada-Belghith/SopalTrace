<template>
  <tr class="hover:bg-blue-50/10 transition-colors group">
    
    <td class="p-2 align-top">
      <div class="flex items-center gap-1">
        <input v-model="localLigne.libelleAffiche" :disabled="isReadOnly"
               type="text"
               placeholder="Ex: Ø29.8, Entraxe..."
               :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 text-slate-700 focus:border-blue-500']" />
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top min-w-[150px]">
      
      <div class="h-full w-full flex items-center justify-center pt-1">
        <input v-model="localLigne.limiteSpecTexte" 
               type="text" 
               placeholder="Ex: ±0.3, 0 ; -0.3..."
               :disabled="isReadOnly"
               :class="isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 focus:border-blue-500'"
               class="w-full text-center border rounded px-2 py-1.5 outline-none text-[11px]">
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
        <select v-model="localLigne.typeControleId" :disabled="isReadOnly" 
                :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-700 cursor-pointer focus:border-blue-500']">
        <option :value="null" disabled>-- Type * --</option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.libelle }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
      <select v-model="localLigne.moyenControleId" :disabled="isReadOnly" 
              :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'cursor-pointer bg-white border border-slate-200 text-slate-700 focus:border-blue-500']">
        <option :value="null" disabled>-- Moyen  --</option>
        <option v-for="mc in (store.moyensControle || [])" :key="mc.id" :value="mc.id">{{ mc.libelle }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
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

    <td class="p-2 border-r border-slate-200 align-top">
      <input v-model="localLigne.observations" type="text" placeholder="Observations (Optionnel)..." :disabled="isReadOnly"
             :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-semibold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-600 focus:border-blue-500']">
    </td>

    <td class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity w-8">
      <button v-if="!isReadOnly" @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
        <i class="pi pi-trash"></i>
      </button>
    </td>
  </tr>
</template>

<script setup>
import { ref, computed, watch, nextTick } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { usePfPlanStore } from '@/stores/pfPlanStore';

const props = defineProps({
  ligne: { type: Object, required: true },
  isReadOnly: { type: Boolean, default: false },
  operationCode: { type: String, default: '' } 
});

const isReadOnly = computed(() => props.isReadOnly);
const emit = defineEmits(['remove', 'update']);
const fabStore = useFabModeleStore();
const pfStore = usePfPlanStore();
const store = props.operationCode === 'PF' ? pfStore : fabStore;

const localLigne = ref({ ...props.ligne });
const isSyncingFromParent = ref(false);

const isVisuel = computed(() => {
  const typeCtrl = (store.typesControle || []).find(t => t.id === localLigne.value.typeControleId);
  return (typeCtrl?.code === 'VISUEL');
});

watch(() => props.ligne, (newVal) => {
  if (!newVal) return;
  const sourceSource = JSON.stringify(newVal);
  const sourceLocale = JSON.stringify(localLigne.value);
  
  if (sourceSource !== sourceLocale) {
    isSyncingFromParent.value = true;
    localLigne.value = JSON.parse(sourceSource);
    
    nextTick(() => { isSyncingFromParent.value = false; });
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
