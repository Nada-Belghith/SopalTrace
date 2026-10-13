<script setup>
/** Postes de l'OF avec le nombre de tâches bloquantes restantes de leur étape : le poste en retard se voit d'un coup d'œil. */
defineProps({
  postes: { type: Array, default: () => [] },
  posteSelectionne: { type: String, default: null }
})

const emit = defineEmits(['selectionner'])
</script>

<template>
  <div class="flex flex-wrap gap-2" role="tablist" aria-label="Postes de l'OF">
    <button v-for="p in postes" :key="p.posteCode" type="button" role="tab"
            :aria-selected="p.posteCode === posteSelectionne"
            @click="emit('selectionner', p.posteCode)"
            class="min-h-[44px] px-4 rounded-xl border font-bold text-sm flex items-center gap-2 cursor-pointer transition-colors"
            :class="p.posteCode === posteSelectionne
              ? 'bg-slate-800 border-slate-800 text-white'
              : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'">
      {{ p.posteCode }}
      <span v-if="p.tachesRestantes > 0" class="px-2 py-0.5 rounded-full bg-amber-400 text-amber-950 text-[11px] font-black">
        {{ p.tachesRestantes }} à faire
      </span>
      <i v-else class="pi pi-check-circle text-emerald-500"></i>
    </button>
  </div>
</template>
