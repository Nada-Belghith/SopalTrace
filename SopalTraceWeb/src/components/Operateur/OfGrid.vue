<template>
  <div class="flex flex-wrap gap-6 ml-9">
    <OfCard 
      v-for="(of, index) in ofs" 
      :key="index" 
      :ofItem="of"
      @select="$emit('select-of', of)"
    />
    
    <div v-if="ofs.length === 0 && !loading" class="text-slate-500 mt-4">
      Aucun OF préparé disponible pour le moment dans cette catégorie.
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
  }
});

defineEmits(['select-of']);
</script>
