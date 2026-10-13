<template>
  <div class="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-4">
    <OfCard 
      v-for="(of, index) in ofs" 
      :key="index" 
      :ofItem="of"
      @select="$emit('select-of', of)"
    />
    
    <div v-if="ofs.length === 0 && !loading" class="col-span-full text-slate-500 mt-4">
      {{ filtreActif ? 'Aucun OF ne correspond à la recherche.' : 'Aucun OF préparé disponible pour le moment dans cette catégorie.' }}
    </div>
  </div>
</template>

<script setup>
import OfCard from './OfCard.vue';

defineProps({
  ofs: {
    type: Array,
    required: true
  },
  loading: {
    type: Boolean,
    default: false
  },
  filtreActif: {
    type: Boolean,
    default: false
  }
});

defineEmits(['select-of']);
</script>
