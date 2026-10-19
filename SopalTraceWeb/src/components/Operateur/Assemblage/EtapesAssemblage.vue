<script setup>
/**
 * Les quatre étapes du parcours d'assemblage (échantillonnage, démarrage, production, fin) telles que
 * calculées par le serveur : faites, en cours ou à venir.
 */
defineProps({
  etapes: { type: Array, default: () => [] }
})

const ICONES = {
  ECHANTILLONNAGE: 'pi pi-chart-pie',
  DEMARRAGE: 'pi pi-play',
  PRODUCTION: 'pi pi-cog',
  FIN: 'pi pi-flag'
}
</script>

<template>
  <ol class="grid grid-cols-2 md:grid-cols-4 gap-2 m-0 p-0 list-none" aria-label="Étapes de l'assemblage">
    <li v-for="(etape, i) in etapes" :key="etape.code"
        class="flex items-center gap-3 rounded-xl border px-3 py-3 min-h-[56px]"
        :class="{
          'bg-emerald-50 border-emerald-200 text-emerald-800': etape.etat === 'FAIT',
          'bg-blue-600 border-blue-600 text-white shadow-md': etape.etat === 'EN_COURS',
          'bg-white border-slate-200 text-slate-400': etape.etat === 'A_VENIR'
        }"
        :aria-current="etape.etat === 'EN_COURS' ? 'step' : undefined">
      <span class="w-9 h-9 shrink-0 rounded-full flex items-center justify-center text-sm font-black"
            :class="{
              'bg-emerald-500 text-white': etape.etat === 'FAIT',
              'bg-white text-blue-700': etape.etat === 'EN_COURS',
              'bg-slate-100 text-slate-400': etape.etat === 'A_VENIR'
            }">
        <i v-if="etape.etat === 'FAIT'" class="pi pi-check"></i>
        <i v-else :class="ICONES[etape.code]"></i>
      </span>
      <span class="leading-tight">
        <span class="block text-[11px] font-bold uppercase tracking-wider opacity-80">Étape {{ i + 1 }}</span>
        <span class="block font-bold">{{ etape.libelle }}</span>
      </span>
    </li>
  </ol>
</template>
