<script setup>
/** Choix d'un fichier Excel (.xlsx). */
import ChoixFichier from './ChoixFichier.vue'

const fichier = defineModel({ type: [File, null], default: null })
defineProps({ libelle: { type: String, default: 'Choisir le fichier Excel (.xlsx)' } })
</script>

<template>
  <ChoixFichier v-model="fichier" :libelle="libelle" icone="pi-file-excel"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" />
</template>
