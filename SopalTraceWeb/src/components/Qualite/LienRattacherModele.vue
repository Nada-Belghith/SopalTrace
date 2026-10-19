<script setup>
/**
 * Document sans modèle Excel : l'administrateur ouvre le rattachement du modèle à son formulaire (code + version) ; le
 * superviseur qualité voit que le modèle est attendu de l'administrateur. Un modèle manquant ne bloque jamais la
 * création du document, seulement son PDF.
 */
import { computed } from 'vue'
import { useAuthStore } from '@/stores/authStore'

const props = defineProps({
  source: { type: Object, required: true }
})

const authStore = useAuthStore()
const sansModele = computed(() => !props.source.modele && !!props.source.formulaireId)
</script>

<template>
  <router-link v-if="sansModele && authStore.userRole === 'ADMIN'"
               :to="{ name: 'qualite-modeles-documents', query: { formulaire: source.formulaireId } }"
               class="inline-flex items-center gap-1 text-xs font-bold text-blue-700 hover:text-blue-800 no-underline">
    <i class="pi pi-link"></i> Rattacher un modèle à {{ source.formulaire || 'ce formulaire' }}
  </router-link>
  <span v-else-if="sansModele && authStore.userRole === 'SUPERVISEUR_QUALITE'"
        class="inline-flex items-center gap-1 text-xs font-semibold text-amber-700">
    <i class="pi pi-clock"></i> Modèle Excel de {{ source.formulaire || 'ce formulaire' }} attendu de l'administrateur
  </span>
</template>
