<template>
  <tr v-if="localLigne" class="hover:bg-blue-50/20 transition-colors group">

    <td class="p-2 align-top">
      <div class="flex items-center gap-1">
        <div v-if="localLigne.modeleLigneSourceId" class="w-full bg-slate-50 border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 font-bold overflow-hidden text-ellipsis whitespace-nowrap cursor-not-allowed" :title="localLigne.libelleAffiche">
          {{ localLigne.libelleAffiche }}
        </div>
        <template v-else>
          <input v-model="localLigne.libelleAffiche" :disabled="isArchived"
               type="text"
               placeholder="Ex: Ø29.8, Entraxe..."
               :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isArchived ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 text-slate-700 focus:border-blue-500']" />
        </template>
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top min-w-[150px]">
      <div class="h-full w-full flex items-center justify-center pt-1">
        <input v-model="localLigne.limiteSpecTexte" 
               type="text" 
               placeholder="Ex: ±0.3, 0 ; -0.3..."
               :disabled="isArchived"
               :class="isArchived ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 focus:border-blue-500'"
               class="w-full text-center border rounded px-2 py-1.5 outline-none text-[11px]">
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top w-[12%]">
      <select v-model="localLigne.typeControleId" :disabled="isArchived" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer disabled:opacity-60 disabled:bg-slate-50">
        <option :value="null" disabled>-- Type * --</option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.code }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top w-[12%]">
      <select v-model="localLigne.moyenControleId" :disabled="isArchived" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer disabled:opacity-60 disabled:bg-slate-50">
        <option :value="null">-- Moyen --</option>
        <option v-for="mco in (store.moyensControle || [])" :key="mco.id" :value="mco.id">{{ mco.libelle }}</option>
      </select>
    </td>

    <!-- Code instrument (Éditable - Combobox) -->
    <td class="p-2 border-r border-slate-200 align-top w-[12%]">
      <div class="flex gap-1 items-center">
        <select @change="(e) => { if (e.target.value) localLigne.instrumentCode = e.target.value; e.target.value = ''; }"
                :disabled="isVisuel"
                :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white cursor-pointer'"
                class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
          <option value="">-- Outil --</option>
          <option v-for="ins in (store.instruments || [])" :key="ins.codeInstrument" :value="ins.codeInstrument">
            {{ ins.codeInstrument }}
          </option>
        </select>
        <input v-model="localLigne.instrumentCode" type="text" placeholder="Perso"
               :disabled="isVisuel"
               :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white'"
               class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[10px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top w-[18%] min-w-[180px]">
      <input v-model="localLigne.observations" type="text" placeholder="Infos (Optionnel)..." :disabled="isArchived"
             class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-xs text-slate-600 outline-none focus:border-blue-500 disabled:opacity-60 disabled:bg-slate-50">
    </td>

    <td v-if="!isArchived" class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity w-8">
      <button @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
        <i class="pi pi-trash"></i>
      </button>
    </td>
    <td v-else class="p-2 w-8"></td>
  </tr>
</template>

<script setup>
import { ref, computed, watch, nextTick } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';


const props = defineProps({
  ligne: { type: Object, required: true },
  section: { type: Object, required: true },
  isArchived: { type: Boolean, default: false }
});

const emit = defineEmits(['remove', 'update']);

const store = useFabModeleStore();
// Local reactive copy to avoid mutating prop directly
const localLigne = ref({ ...props.ligne });
const isSyncingFromParent = ref(false);



// ⚠️ SECURITE
const isVisuel = computed(() => {
  if (!localLigne.value) return false;
  const typeCtrl = (store.typesControle || []).find(t => t.id === localLigne.value.typeControleId);
  return typeCtrl?.code === 'VISUEL';
});

watch(localLigne, (n) => { if (!isSyncingFromParent.value) emit('update', { ...n }); }, { deep: true });

watch(isVisuel, (devenuVisuel) => {
  if (devenuVisuel && localLigne.value && !isSyncingFromParent.value) {
    localLigne.value.instrumentCode = null;
    localLigne.value.moyenTexteLibre = '';
    localLigne.value.limiteSpecTexte = '';
  }
});
watch(() => props.ligne, (n) => {
  if (!n) return;
  isSyncingFromParent.value = true;
  localLigne.value = { ...n };
  nextTick(() => { isSyncingFromParent.value = false; });
}, { deep: true });

</script>
