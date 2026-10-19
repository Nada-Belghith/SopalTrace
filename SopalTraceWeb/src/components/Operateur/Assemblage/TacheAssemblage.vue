<script setup>
import { computed } from 'vue'
import { ETATS_TACHE, TACHES_ASSEMBLAGE } from '@/constants/execution'

/** Une tâche de l'étape courante : état uniforme (à faire, fait, document manquant) et une seule action. */
const props = defineProps({
  tache: { type: Object, required: true },
  desactivee: { type: Boolean, default: false },
  /** Colonne étroite : bouton sous le texte, sur toute la largeur. */
  compacte: { type: Boolean, default: false }
})

const emit = defineEmits(['ouvrir'])

const ICONES = {
  [TACHES_ASSEMBLAGE.ECHANTILLONNAGE]: 'pi pi-chart-pie',
  [TACHES_ASSEMBLAGE.DEMARRER_POSTE]: 'pi pi-play',
  [TACHES_ASSEMBLAGE.VERIF_MACHINE]: 'pi pi-cog',
  [TACHES_ASSEMBLAGE.PLAN_PREMIERE_PIECE]: 'pi pi-flag',
  [TACHES_ASSEMBLAGE.CONTROLES_PRODUCTION]: 'pi pi-clock',
  [TACHES_ASSEMBLAGE.CONTROLE_POSTE]: 'pi pi-check-square',
  [TACHES_ASSEMBLAGE.PLAN_DERNIERE_PIECE]: 'pi pi-flag-fill',
  [TACHES_ASSEMBLAGE.TRACABILITE]: 'pi pi-history',
  [TACHES_ASSEMBLAGE.REGLAGE]: 'pi pi-sliders-h',
  [TACHES_ASSEMBLAGE.TOUTES_PIECES]: 'pi pi-th-large'
}

/** Contrôles que l'opérateur déclenche quand il le juge utile. */
const A_LA_DEMANDE = [TACHES_ASSEMBLAGE.REGLAGE, TACHES_ASSEMBLAGE.TOUTES_PIECES]

const estFaite = computed(() => props.tache.etat === ETATS_TACHE.FAIT)
const estManquante = computed(() => props.tache.etat === ETATS_TACHE.MANQUANT)

const aLaDemande = computed(() => A_LA_DEMANDE.includes(props.tache.code))

const statut = computed(() => {
  if (estManquante.value) return { libelle: 'Document manquant', classe: 'bg-red-100 text-red-700 border-red-200' }
  if (aLaDemande.value) return { libelle: 'À la demande', classe: 'bg-slate-100 text-slate-600 border-slate-200' }
  if (estFaite.value) return { libelle: 'Fait', classe: 'bg-emerald-100 text-emerald-700 border-emerald-200' }
  if (!props.tache.bloquante) return { libelle: 'À suivre', classe: 'bg-sky-100 text-sky-700 border-sky-200' }
  return { libelle: 'À faire', classe: 'bg-amber-100 text-amber-800 border-amber-200' }
})

const action = computed(() => {
  if (estManquante.value) return 'Signaler'
  if (aLaDemande.value) return 'Saisir'
  if (estFaite.value) return 'Consulter'
  if (props.tache.code === TACHES_ASSEMBLAGE.DEMARRER_POSTE) return 'Démarrer le poste'
  return props.tache.code === TACHES_ASSEMBLAGE.CONTROLES_PRODUCTION ? 'Saisir les contrôles' : 'Commencer'
})

const couleurAction = computed(() => {
  if (estManquante.value) return 'bg-red-600 hover:bg-red-700 text-white'
  if (aLaDemande.value) return 'bg-slate-700 hover:bg-slate-800 text-white'
  if (estFaite.value) return 'bg-slate-100 hover:bg-slate-200 text-slate-700'
  return 'bg-blue-600 hover:bg-blue-700 text-white'
})
</script>

<template>
  <article class="flex gap-4 rounded-2xl border bg-white p-4 shadow-sm"
           :class="[compacte ? 'flex-wrap items-start' : 'items-center',
                    estManquante ? 'border-red-200' : estFaite ? 'border-emerald-200' : 'border-slate-200']">
    <span class="w-12 h-12 shrink-0 rounded-xl flex items-center justify-center text-xl"
          :class="estManquante ? 'bg-red-50 text-red-600' : estFaite ? 'bg-emerald-50 text-emerald-600' : 'bg-blue-50 text-blue-700'">
      <i :class="ICONES[tache.code] || 'pi pi-file'"></i>
    </span>

    <div class="flex-1 min-w-0">
      <div class="flex items-center gap-2 flex-wrap">
        <h4 class="m-0 text-base font-bold text-slate-800">{{ tache.libelle }}</h4>
        <span class="px-2 py-0.5 rounded-full border text-[11px] font-bold uppercase tracking-wide" :class="statut.classe">{{ statut.libelle }}</span>
        <span v-if="tache.compteur > 0 && !aLaDemande" class="px-2 py-0.5 rounded-full bg-red-600 text-white text-[11px] font-black">{{ tache.compteur }}</span>
        <span v-else-if="tache.compteur > 0" class="text-[11px] font-semibold text-slate-500">{{ tache.compteur }} saisie(s) par l'équipe</span>
      </div>
      <p v-if="tache.detail" class="m-0 mt-1 text-sm text-slate-500">{{ tache.detail }}</p>
    </div>

    <button type="button" :disabled="desactivee" @click="emit('ouvrir', tache)"
            class="min-h-[48px] px-5 rounded-xl font-bold text-sm border-0 cursor-pointer transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            :class="[compacte ? 'w-full' : 'shrink-0', couleurAction]">
      {{ action }}
    </button>
  </article>
</template>
