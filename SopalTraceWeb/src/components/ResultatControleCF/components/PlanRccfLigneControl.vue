<template>
  <tr v-if="localLigne" class="hover:bg-slate-50 transition-colors group">
    <td class="border-[1px] border-black p-0 h-8">
      <input v-model="localLigne.caracteristique" :disabled="isArchived"
           type="text"
           class="w-full h-full text-center outline-none bg-transparent text-sm text-black px-1" />
    </td>

    <td class="border-[1px] border-black p-0 h-8">
      <input v-model="localLigne.limiteSpecTexte" 
             type="text" 
             :disabled="isArchived"
             class="w-full h-full text-center outline-none bg-transparent text-sm text-black px-1" />
    </td>

    <td class="border-[1px] border-black p-0 h-8">
      <select v-model="localLigne.typeControleId" :disabled="isArchived" class="w-full h-full text-center outline-none bg-transparent hover:bg-white focus:bg-white text-sm text-black px-1 cursor-pointer">
        <option :value="null"></option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.code }}</option>
      </select>
    </td>

    <td class="border-[1px] border-black p-0 h-8">
      <select v-model="localLigne.moyenControleId" :disabled="isArchived" class="w-full h-full text-center outline-none bg-transparent hover:bg-white focus:bg-white text-sm text-black px-1 cursor-pointer">
        <option :value="null"></option>
        <option v-for="mco in (store.moyensControle || [])" :key="mco.id" :value="mco.id">{{ mco.libelle }}</option>
      </select>
    </td>

    <td class="border-[1px] border-black p-0 h-8">
      <div class="flex h-full w-full">
        <select @change="(e) => { if (e.target.value) localLigne.instrumentCode = e.target.value; e.target.value = ''; }"
                :disabled="isArchived"
                class="w-1/2 h-full text-center outline-none bg-white hover:bg-white focus:bg-white text-sm text-black cursor-pointer border-r-[1px] border-black appearance-none">
          <option value=""></option>
          <option v-for="ins in (store.instruments || [])" :key="ins.codeInstrument" :value="ins.codeInstrument">
            {{ ins.codeInstrument }}
          </option>
        </select>
        <input v-model="localLigne.instrumentCode" type="text"
               :disabled="isArchived"
               class="w-1/2 h-full text-center outline-none bg-transparent text-sm text-black px-1" />
      </div>
    </td>

    <td class="border-[1px] border-black p-0 h-8">
      <input v-model="localLigne.observations" type="text" :disabled="isArchived"
             class="w-full h-full text-center outline-none bg-transparent text-sm text-black px-1" />
    </td>

    <!-- Colonnes dynamiques configurées -->
    <td v-for="i in customColumnsCount" :key="'col'+i" class="border-[1px] border-black p-0 h-8 text-center">
      <span class="text-xs text-slate-400">...</span>
    </td>

    <td v-if="!isArchived" class="border-[1px] border-black p-0 h-8 text-center opacity-0 group-hover:opacity-100 transition-opacity w-8">
      <button @click="$emit('remove', localLigne.id)" class="text-red-500 hover:text-red-700 p-1 w-full h-full" title="Supprimer ligne">
        X
      </button>
    </td>
  </tr>
</template>

<script setup>
import { ref, watch } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore'; // To get the dictionaries

const props = defineProps({
  ligne: { type: Object, required: true },
  isArchived: { type: Boolean, default: false },
  customColumnsCount: { type: Number, default: 0 }
});

const emit = defineEmits(['remove', 'update']);
const store = useFabModeleStore();
const localLigne = ref({ ...props.ligne });

watch(() => props.ligne, (newVal) => {
  localLigne.value = { ...newVal };
}, { deep: true });

watch(localLigne, (newVal) => {
  emit('update', newVal);
}, { deep: true });
</script>
