<script setup>
/**
 * Postes de l'OF vus par l'équipe : chacun suit son propre parcours. Nombre de tâches bloquantes restantes de son
 * étape (le poste en retard se voit d'un coup d'œil), poste non démarré, tenu par une autre équipe ou terminé.
 */
import { PHASES_ASSEMBLAGE } from '@/constants/execution'

defineProps({
  postes: { type: Array, default: () => [] },
  posteSelectionne: { type: String, default: null }
})

const emit = defineEmits(['selectionner'])

const badge = (p) => {
  if (p.phase === PHASES_ASSEMBLAGE.POSTE_OCCUPE) return { texte: `tenu par ${p.equipeTitulaire ?? 'une autre équipe'}`, neutre: true }
  if (p.phase === PHASES_ASSEMBLAGE.POSTE_TERMINE) return { texte: 'terminé', termine: true }
  if (p.phase === PHASES_ASSEMBLAGE.CLOTURE) return { texte: 'clôturé', termine: true }
  if (p.phase === PHASES_ASSEMBLAGE.SANS_EQUIPE) return { texte: 'libre', neutre: true }
  if (!p.demarre) return { texte: 'non démarré', neutre: true }
  if (p.tachesRestantes > 0) return { texte: `${p.tachesRestantes} à faire`, aFaire: true }
  return { termine: true }
}
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
      <span v-if="badge(p).neutre" class="px-2 py-0.5 rounded-full text-[11px] font-bold"
            :class="p.posteCode === posteSelectionne ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-500'">{{ badge(p).texte }}</span>
      <span v-else-if="badge(p).aFaire" class="px-2 py-0.5 rounded-full bg-amber-400 text-amber-950 text-[11px] font-black">
        {{ badge(p).texte }}
      </span>
      <span v-else class="flex items-center gap-1 text-[11px] font-bold" :class="p.posteCode === posteSelectionne ? 'text-emerald-300' : 'text-emerald-600'">
        <i class="pi pi-check-circle"></i>{{ badge(p).texte }}
      </span>
    </button>
  </div>
</template>
