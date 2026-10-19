<script setup>
import { computed, ref } from 'vue'
import { TYPES_DOCUMENT } from '@/constants/execution'

/** Documents déjà ouverts par l'équipe sur le poste, pour les consulter ou les reprendre. */
const props = defineProps({
  documents: { type: Array, default: () => [] }
})

const emit = defineEmits(['ouvrir'])
const ouvert = ref(false)

const LIBELLES = {
  [TYPES_DOCUMENT.VERIF_MACHINE]: 'Vérification machine',
  [TYPES_DOCUMENT.ECHANTILLONNAGE]: 'Fiche d\'échantillonnage',
  [TYPES_DOCUMENT.PLAN_ASS]: 'Plan d\'assemblage',
  [TYPES_DOCUMENT.RESULTAT_CONTROLE_POSTE]: 'Résultat du contrôle au poste',
  [TYPES_DOCUMENT.REGISTRE_TRACA]: 'Registre de traçabilité'
}

const consultables = computed(() => props.documents.filter(d => LIBELLES[d.typeDocument]))
const libelle = (doc) => [LIBELLES[doc.typeDocument], doc.machineLibelle || doc.machineCode].filter(Boolean).join(' — ')
</script>

<template>
  <section class="rounded-2xl border border-slate-200 bg-white">
    <button type="button" @click="ouvert = !ouvert" :aria-expanded="ouvert"
            class="w-full min-h-[48px] px-4 flex items-center justify-between bg-transparent border-0 cursor-pointer text-slate-700 font-bold">
      <span><i class="pi pi-folder-open mr-2"></i>Documents du poste ({{ consultables.length }})</span>
      <i class="pi" :class="ouvert ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
    </button>
    <ul v-if="ouvert" class="m-0 p-0 list-none border-t border-slate-100 divide-y divide-slate-100">
      <li v-if="consultables.length === 0" class="px-4 py-4 text-sm text-slate-400">Aucun document ouvert sur ce poste.</li>
      <li v-for="doc in consultables" :key="doc.id" class="px-4 py-3 flex items-center justify-between gap-3">
        <span class="text-sm">
          <span class="font-semibold text-slate-700">{{ libelle(doc) }}</span>
          <span class="ml-2 px-2 py-0.5 rounded-full text-[11px] font-bold"
                :class="doc.estTermine ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-800'">
            {{ doc.estTermine ? 'Terminé' : 'En cours' }}
          </span>
        </span>
        <button type="button" @click="emit('ouvrir', doc)"
                class="min-h-[40px] px-4 rounded-lg bg-slate-100 hover:bg-slate-200 border-0 cursor-pointer text-sm font-bold text-slate-700">
          Ouvrir
        </button>
      </li>
    </ul>
  </section>
</template>
