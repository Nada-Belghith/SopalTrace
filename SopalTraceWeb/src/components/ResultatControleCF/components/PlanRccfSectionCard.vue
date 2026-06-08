<template>
  <tbody class="divide-y divide-black border-b-[1px] border-black">
    <!-- HEADER DE LA SECTION (Style Papier Img2) -->
    <tr>
      <th :colspan="100" class="border-[1px] border-black p-2 font-bold text-base bg-white text-center group relative">
        <input v-model="localSection.libelleAffiche"
               :disabled="isArchived"
               placeholder="Nom de la section (ex: Approbation des pièces types)"
               class="w-full text-center font-bold text-black outline-none bg-transparent placeholder-slate-400" />
        
        <!-- BOUTONS CACHÉS A DROITE POUR EDITER -->
        <div v-if="!isArchived" class="absolute right-2 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-2 bg-white px-2">
          <button @click="addLigne" class="text-blue-600 hover:text-blue-800 text-xs font-bold uppercase" title="Ajouter ligne">
            + Ligne
          </button>
          <button @click="$emit('remove')" class="text-red-500 hover:text-red-700 text-xs font-bold uppercase" title="Supprimer la section">
            X
          </button>
        </div>
      </th>
    </tr>

    <!-- LIGNES DE LA SECTION -->
    <PlanRccfLigneControl
      v-for="ligne in localSection.lignes"
      :key="ligne.id"
      :ligne="ligne"
      :is-archived="isArchived"
      :custom-columns-count="customColumnsCount"
      @remove="removeLigne"
      @update="(updated) => updateLigne(updated)"
    />

    <tr v-if="!localSection.lignes || localSection.lignes.length === 0">
      <td :colspan="100" class="border-[1px] border-black p-4 text-center text-slate-400 italic text-sm">
        Aucune caractéristique (ligne) définie. Cliquez sur "+ Ligne" en survolant le titre de la section.
      </td>
    </tr>
  </tbody>
</template>

<script setup>
import { defineProps, defineEmits, ref, watch } from 'vue';
import PlanRccfLigneControl from './PlanRccfLigneControl.vue';
import { genererUid } from '@/utils/uuidUtils';

const props = defineProps({
  section: { type: Object, required: true },
  index: { type: Number, required: true },
  isArchived: { type: Boolean, default: false },
  customColumnsCount: { type: Number, default: 0 }
});

const emit = defineEmits(['update:section', 'remove', 'remove-ligne']);

const localSection = ref({ ...props.section, lignes: props.section.lignes || [] });

watch(() => props.section, (newVal) => {
  if (!newVal) return;
  const oldSrc = JSON.stringify(localSection.value);
  const newSrc = JSON.stringify({ ...newVal, lignes: newVal.lignes || [] });
  if (oldSrc !== newSrc) {
    localSection.value = JSON.parse(newSrc);
  }
}, { deep: true });

watch(localSection, (newVal) => {
  emit('update:section', newVal);
}, { deep: true });

const addLigne = () => {
  if (!localSection.value.lignes) {
    localSection.value.lignes = [];
  }
  localSection.value.lignes.push({
    id: genererUid(),
    caracteristique: '',
    limiteSpecTexte: '',
    typeControleId: null,
    moyenControleId: null,
    instrumentCode: '',
    observations: '',
    ordreAffiche: localSection.value.lignes.length + 1
  });
};

const removeLigne = (id) => {
  localSection.value.lignes = localSection.value.lignes.filter(l => l.id !== id);
  emit('remove-ligne', id);
};

const updateLigne = (updated) => {
  const idx = localSection.value.lignes.findIndex(l => l.id === updated.id);
  if (idx !== -1) {
    localSection.value.lignes.splice(idx, 1, updated);
  }
};
</script>
