<script setup>
/** Flèche de retour commune à tous les écrans. */
defineProps({
  libelle: { type: String, default: 'Retour' }
})
defineEmits(['click'])
</script>

<template>
  <button type="button" :aria-label="libelle" :title="libelle" @click="$emit('click')"
          class="shrink-0 w-11 h-11 rounded-full border-0 bg-transparent text-emerald-600 hover:bg-emerald-50 hover:text-emerald-700 cursor-pointer flex items-center justify-center transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-300">
    <i class="pi pi-arrow-left text-lg"></i>
  </button>
</template>
