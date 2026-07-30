<script setup>
import { defineProps, defineEmits } from 'vue'
import Button from 'primevue/button'
import Tag from 'primevue/tag'

defineProps({
  ofActif: {
    type: Object,
    default: null
  },
  tousLesPostesActifs: {
    type: Array,
    default: () => []
  },
  equipe: {
    type: String,
    default: null
  },
  equipesList: {
    type: Array,
    default: () => ['E1', 'E2', 'E3']
  }
})

const emit = defineEmits(['quitter-session', 'update:equipe'])
</script>

<template>
  <div class="bg-white rounded-xl shadow-sm border border-green-200 p-4 flex flex-wrap justify-between items-center gap-4">
    <div class="flex items-center gap-4">
      <div class="bg-green-50 text-green-600 w-12 h-12 rounded-full flex items-center justify-center">
        <i class="pi pi-box text-2xl"></i>
      </div>
      <div>
        <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-0.5">OF Actif</div>
        <div class="font-bold text-gray-800">{{ ofActif?.numeroOf }} — {{ ofActif?.designationArticle }}</div>
      </div>
    </div>

    <div class="hidden md:block h-10 border-l border-gray-200"></div>

    <div class="flex-1">
      <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-2">Postes Actifs</div>
      <div class="flex flex-wrap gap-2">
        <Tag v-for="p in tousLesPostesActifs" :key="p" :value="p" severity="info" class="px-3 py-1 text-sm" />
        <span v-if="tousLesPostesActifs.length === 0" class="text-sm text-gray-400 italic">Aucun poste</span>
      </div>
    </div>

    <div class="flex items-center gap-4">

      <Button 
        label="Quitter la session" 
        icon="pi pi-sign-out" 
        severity="danger" 
        outlined 
        @click="emit('quitter-session')" 
      />
    </div>
  </div>
</template>
