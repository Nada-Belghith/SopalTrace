<script setup>
import { defineProps, defineEmits } from 'vue'

defineProps({
  ofsStatut: {
    type: Array,
    required: true
  },
  isLoading: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['select-of'])

const formatDate = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('fr-FR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  }).format(date)
}
</script>

<template>
  <div>
    <!-- Chargement -->
    <div v-if="isLoading" class="flex justify-center items-center h-40">
      <i class="pi pi-spin pi-spinner text-4xl text-primary"></i>
    </div>

    <!-- Grille -->
    <div v-else class="flex flex-wrap gap-6 ml-2">
      <div
        v-for="of in ofsStatut"
        :key="of.numeroOf"
        @click="emit('select-of', of)"
        class="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 w-[400px] cursor-pointer hover:shadow-md hover:border-blue-300 transition-all flex flex-col justify-between"
      >
        <!-- En-tête : référence OF + badge statut -->
        <div class="flex justify-between items-start mb-4">
          <span class="text-xs font-bold text-blue-600 tracking-wider">OF: {{ of.numeroOf }}</span>
          <span class="px-2 py-1 font-bold text-[10px] rounded uppercase tracking-wider"
                :class="of.statut === 'EN_COURS' ? 'bg-green-50 text-green-600' : 'bg-gray-100 text-gray-500'">
            {{ of.statut === 'EN_COURS' ? 'EN COURS' : 'NON COMMENCÉ' }}
          </span>
        </div>

        <!-- Titre article -->
        <div class="mb-4">
          <h3 class="text-lg font-bold text-slate-800 leading-tight mb-1">{{ of.designationArticle }}</h3>
          <p class="text-sm text-slate-400 font-medium">{{ of.codeArticle }}</p>
        </div>

        <!-- Postes (si EN COURS) -->
        <div v-if="of.postesExistants?.length > 0" class="mb-4">
          <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-2">Postes actifs</p>
          <div class="flex flex-wrap gap-1.5">
            <span
              v-for="p in of.postesExistants"
              :key="p"
              class="px-2.5 py-0.5 bg-blue-50 text-blue-700 text-xs font-semibold rounded-full border border-blue-100"
            >{{ p }}</span>
          </div>
        </div>
        <div v-else class="mb-4">
          <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-2">Postes actifs</p>
          <p class="text-xs text-slate-400 italic">Aucun poste assigné</p>
        </div>

        <!-- Pied : quantité -->
        <div class="flex justify-between items-end border-t border-slate-100 pt-4 mt-auto">
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Qté lancée</p>
            <p class="text-sm font-bold text-slate-700">{{ of.quantiteLancee ?? '-' }}</p>
          </div>
          <div class="text-right">
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Date début</p>
            <p class="text-sm font-medium text-slate-600">{{ of.statut === 'EN_COURS' && of.dateDebut ? formatDate(of.dateDebut) : '-' }}</p>
          </div>
        </div>
      </div>

      <!-- Aucun OF -->
      <div v-if="ofsStatut.length === 0" class="w-full text-center py-16 text-gray-400">
        <i class="pi pi-inbox text-5xl mb-3"></i>
        <p class="text-lg">Aucun OF d'assemblage disponible.</p>
      </div>
    </div>
  </div>
</template>
