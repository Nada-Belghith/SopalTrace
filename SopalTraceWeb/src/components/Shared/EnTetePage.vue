<script setup>
import BoutonRetour from './BoutonRetour.vue'

/**
 * En-tête commun des écrans : flèche de retour (facultative), titre, précision du titre en gris
 * (à la place des parenthèses), sous-titre et actions à droite.
 */
defineProps({
  titre: { type: String, required: true },
  // Précision affichée en gris après le titre : « Fiche d'échantillonnage · ISO 2859 »
  complement: { type: String, default: '' },
  surTitre: { type: String, default: '' },
  sousTitre: { type: String, default: '' },
  retour: { type: Boolean, default: false },
  libelleRetour: { type: String, default: 'Retour' }
})
defineEmits(['retour'])
</script>

<template>
  <header class="flex flex-col lg:flex-row lg:items-start justify-between gap-4">
    <div class="flex items-start gap-2 min-w-0">
      <BoutonRetour v-if="retour" :libelle="libelleRetour" class="-ml-2 mt-0.5" @click="$emit('retour')" />
      <div class="min-w-0" :class="{ 'pt-1': retour }">
        <p v-if="surTitre" class="m-0 mb-0.5 text-xs font-bold uppercase tracking-wider text-slate-400">{{ surTitre }}</p>
        <h1 class="m-0 text-2xl md:text-3xl font-bold text-slate-800 leading-tight">
          {{ titre }}<span v-if="complement" class="text-slate-400 font-semibold"> · {{ complement }}</span>
        </h1>
        <p v-if="sousTitre || $slots['sous-titre']" class="m-0 mt-1 text-sm text-slate-500">
          <slot name="sous-titre">{{ sousTitre }}</slot>
        </p>
        <slot />
      </div>
    </div>
    <div v-if="$slots.actions" class="flex flex-wrap items-center gap-2 lg:justify-end lg:shrink-0">
      <slot name="actions" />
    </div>
  </header>
</template>
