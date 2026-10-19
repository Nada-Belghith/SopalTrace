<script setup>
import { STATUTS_EXECUTION } from '@/constants/execution'

defineProps({
  ofsStatut: {
    type: Array,
    required: true
  },
  isLoading: {
    type: Boolean,
    default: false
  },
  filtreActif: {
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

const AFFICHAGE_STATUTS = {
  [STATUTS_EXECUTION.REGLAGE]: { classe: 'bg-amber-100 text-amber-700', libelle: 'EN RÉGLAGE' },
  [STATUTS_EXECUTION.EN_COURS]: { classe: 'bg-green-50 text-green-600', libelle: 'EN COURS' },
  [STATUTS_EXECUTION.EN_PAUSE]: { classe: 'bg-blue-100 text-blue-700', libelle: 'EN PAUSE' },
  [STATUTS_EXECUTION.CLOTURE]: { classe: 'bg-slate-200 text-slate-700', libelle: 'CLÔTURÉ' }
}
const NON_COMMENCE = { classe: 'bg-gray-100 text-gray-500', libelle: 'NON COMMENCÉ' }

const getStatutClass = (statut) => (AFFICHAGE_STATUTS[statut] ?? NON_COMMENCE).classe
const formatStatutLabel = (statut) => (AFFICHAGE_STATUTS[statut] ?? NON_COMMENCE).libelle
</script>

<template>
  <div>
    <!-- Chargement -->
    <div v-if="isLoading" class="flex justify-center items-center h-40">
      <i class="pi pi-spin pi-spinner text-4xl text-primary"></i>
    </div>

    <!-- Grille -->
    <div v-else class="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-4">
      <div
        v-for="of in ofsStatut"
        :key="of.numeroOf"
        @click="emit('select-of', of)"
        class="rounded-2xl shadow-sm border p-4 min-w-0 cursor-pointer hover:shadow-md transition-all flex flex-col justify-between"
        :class="of.dateCloture ? 'bg-slate-50 border-slate-200 opacity-80 hover:border-slate-300' : 'bg-white border-slate-200 hover:border-blue-300'"
        :title="of.dateCloture ? 'OF clôturé : ouverture en consultation' : undefined"
      >
        <!-- En-tête : référence OF + badge statut -->
        <div class="flex justify-between items-start gap-2 mb-2">
          <span class="text-xs font-bold text-blue-600 tracking-wider truncate">OF: {{ of.numeroOf }}</span>
          <span class="shrink-0 px-2 py-0.5 font-bold text-[10px] rounded uppercase tracking-wider"
                :class="getStatutClass(of.statut)">
            {{ formatStatutLabel(of.statut) }}
          </span>
        </div>

        <!-- Titre article -->
        <div class="mb-3">
          <h3 class="text-base font-bold text-slate-800 leading-tight mb-0.5 line-clamp-2" :title="of.designationArticle">{{ of.designationArticle }}</h3>
          <p class="text-xs text-slate-400 font-medium">{{ of.codeArticle }}</p>
        </div>

        <!-- Équipes : celle qui a la main et celles en pause (elles reprendront leur étape à leur retour) -->
        <!-- Équipes sur l'OF : chacune tient ses postes (une seule équipe à la fois par poste) -->
        <div v-if="of.equipes?.length" class="mb-3 flex flex-wrap gap-1.5 text-[11px] font-semibold">
          <span v-for="e in of.equipes" :key="e.equipe" class="px-2 py-0.5 rounded-full border"
                :class="e.clotureDemandee ? 'bg-red-50 text-red-700 border-red-100'
                  : e.enPause ? 'bg-amber-50 text-amber-800 border-amber-100'
                  : 'bg-emerald-50 text-emerald-700 border-emerald-100'">
            {{ e.equipe }}{{ e.enPause ? ' en pause' : '' }}{{ e.clotureDemandee ? ' · clôture' : '' }}<template v-if="e.postes.length"> · {{ e.postes.join(', ') }}</template>
          </span>
        </div>

        <!-- Postes (si EN COURS) -->
        <div v-if="of.postesExistants?.length > 0" class="mb-3">
          <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-2">Postes actifs</p>
          <div class="flex flex-wrap gap-1.5">
            <span
              v-for="p in of.postesExistants"
              :key="p"
              class="px-2.5 py-0.5 bg-blue-50 text-blue-700 text-xs font-semibold rounded-full border border-blue-100"
            >{{ p }}</span>
          </div>
        </div>
        <div v-else class="mb-3">
          <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-2">Postes actifs</p>
          <p class="text-xs text-slate-400 italic">Aucun poste assigné</p>
        </div>

        <!-- Pied : quantité -->
        <div class="flex justify-between items-end border-t border-slate-100 pt-3 mt-auto">
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Qté lancée</p>
            <p class="text-sm font-bold text-slate-700">{{ of.quantiteLancee ?? '-' }}</p>
          </div>
          <div class="text-right">
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">{{ of.dateCloture ? 'Clôturé le' : 'Date début' }}</p>
            <p class="text-sm font-medium text-slate-600">{{ formatDate(of.dateCloture || of.dateDebut) }}</p>
          </div>
        </div>
      </div>

      <!-- Aucun OF -->
      <div v-if="ofsStatut.length === 0" class="col-span-full text-center py-16 text-gray-400">
        <i class="pi pi-inbox text-5xl mb-3"></i>
        <p class="text-lg">{{ filtreActif ? 'Aucun OF ne correspond à la recherche.' : "Aucun OF d'assemblage disponible." }}</p>
      </div>
    </div>
  </div>
</template>
