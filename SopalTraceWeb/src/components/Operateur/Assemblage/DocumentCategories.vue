<script setup>
import { defineProps, defineEmits } from 'vue'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Tooltip from 'primevue/tooltip'

const vTooltip = Tooltip

defineProps({
  tousLesPostesActifs: {
    type: Array,
    default: () => []
  },
  selectedPoste: {
    type: String,
    default: null
  },
  documentCategories: {
    type: Array,
    default: () => []
  },
  isLoadingDocs: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:selectedPoste', 'refresh', 'category-click', 'cloturer-category'])

const isCatTermine = (cat) => {
  if (cat.isTermine !== undefined) return cat.isTermine
  if (cat.docs.length === 0) return false
  return cat.docs.every(d => d.estTermine)
}

const getCategoryBorderColor = (cat) => {
  if (cat.docs.length === 0 && cat.id !== 'tracabilite') return 'bg-gray-300'
  return isCatTermine(cat) ? 'bg-green-400' : 'bg-orange-400'
}
const getCategoryIconColor = (cat) => {
  if (cat.docs.length === 0 && cat.id !== 'tracabilite') return 'text-gray-400 bg-gray-100'
  return isCatTermine(cat) ? 'text-green-500 bg-green-50' : 'text-orange-500 bg-orange-50'
}
</script>

<template>
  <Card class="shadow-md border border-gray-100">
    <template #title>
      <div class="flex justify-between items-center p-2 mb-2 border-b border-gray-100 pb-4">
        <div class="flex items-center gap-2">
          <span class="text-sm font-bold text-gray-400 uppercase tracking-wider mr-2">Sélectionnez un poste :</span>
          <div 
            v-for="p in tousLesPostesActifs" 
            :key="p"
            @click="emit('update:selectedPoste', p)"
            class="px-6 py-2 rounded-xl font-bold text-sm cursor-pointer transition-all border-2"
            :class="selectedPoste === p ? 'bg-blue-50 text-blue-600 border-blue-200 shadow-sm' : 'bg-white text-gray-500 border-transparent hover:bg-gray-50 hover:text-gray-700'"
          >
            {{ p }}
          </div>
        </div>
        <Button icon="pi pi-refresh" text rounded class="text-gray-400 hover:text-primary hover:bg-gray-50" @click="emit('refresh')" v-tooltip.top="'Actualiser'" />
      </div>
    </template>
    <template #content>
      <div v-if="isLoadingDocs" class="flex justify-center p-4">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-5 mt-4">
        <div
          v-for="cat in documentCategories"
          :key="cat.id"
          @click="emit('category-click', cat)"
          class="border rounded-xl p-6 cursor-pointer transition-all hover:-translate-y-1 hover:shadow-lg flex flex-col items-center text-center relative overflow-hidden bg-white"
          :class="getCategoryBorderColor(cat).replace('bg-', 'border-').replace('400', '200') + ' hover:border-primary'"
        >
          <div class="absolute left-0 top-0 right-0 h-1" :class="getCategoryBorderColor(cat)"></div>
          
          <button v-if="cat.docs.length > 0 && !isCatTermine(cat) && cat.id !== 'tracabilite'"
            @click.stop="emit('cloturer-category', cat)"
            class="absolute top-3 right-3 text-emerald-600 bg-emerald-50 border border-emerald-200 hover:text-white hover:bg-emerald-500 p-2 rounded-full transition-all flex items-center justify-center z-10 shadow-sm"
            v-tooltip.top="'Clôturer cette tâche'"
          >
            <i class="pi pi-check-circle text-xl"></i>
          </button>

          <div class="w-16 h-16 rounded-full flex items-center justify-center mb-4 mt-2" :class="getCategoryIconColor(cat)">
            <i :class="cat.icon" class="text-3xl"></i>
          </div>
          <h3 class="font-bold text-gray-800 text-base leading-tight mb-3 flex-1 flex items-center">{{ cat.title }}</h3>
          <div class="mt-auto w-full">
            <Tag v-if="cat.docs.length > 0 || cat.id === 'tracabilite'" :value="isCatTermine(cat) ? 'Terminé' : 'À Remplir'" :severity="isCatTermine(cat) ? 'success' : 'warning'" class="w-full" />
            <Tag v-else value="Non requis" severity="secondary" class="w-full" />
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>
