<script setup>
import EnTetePage from '@/components/Shared/EnTetePage.vue'

/**
 * En-tête commun des tableaux de bord opérateur (OF semi-finis et OF produits finis) :
 * titre, recherche (numéro d'OF, code ou désignation article) et filtre par statut.
 */
defineProps({
  titre: { type: String, required: true },
  sousTitre: { type: String, default: '' },
  // [{ valeur, libelle, nombre }] — la première option correspond à « tous les statuts »
  optionsStatut: { type: Array, default: () => [] },
  nombreAffiches: { type: Number, default: 0 },
  nombreTotal: { type: Number, default: 0 }
})

const recherche = defineModel('recherche', { type: String, default: '' })
const statut = defineModel('statut', { type: String, default: '' })
</script>

<template>
  <div class="mb-6 space-y-4">
    <EnTetePage titre="Tableau de bord" :complement="titre" :sous-titre="sousTitre" />

    <div class="flex flex-col lg:flex-row lg:items-center gap-3">
      <div class="relative w-full lg:max-w-sm">
        <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm"></i>
        <input v-model.trim="recherche" type="search" aria-label="Rechercher un OF"
               placeholder="N° d'OF, code ou désignation article…"
               class="w-full h-11 pl-9 pr-3 rounded-xl border border-slate-200 bg-white text-sm text-slate-700 placeholder:text-slate-400 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100" />
      </div>

      <div v-if="optionsStatut.length" class="flex flex-wrap gap-2" role="group" aria-label="Filtrer par statut">
        <button v-for="o in optionsStatut" :key="o.valeur" type="button" :aria-pressed="statut === o.valeur"
                @click="statut = o.valeur"
                class="h-9 px-3 rounded-full text-xs font-bold border cursor-pointer transition-colors"
                :class="statut === o.valeur
                  ? 'bg-blue-600 border-blue-600 text-white'
                  : 'bg-white border-slate-200 text-slate-600 hover:border-blue-300'">
          {{ o.libelle }} <span class="ml-1 opacity-75">{{ o.nombre }}</span>
        </button>
      </div>

      <p class="m-0 lg:ml-auto text-xs text-slate-400 whitespace-nowrap">
        {{ nombreAffiches }} / {{ nombreTotal }} OF
      </p>
    </div>
  </div>
</template>
