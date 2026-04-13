<script setup>
import { RouterView } from 'vue-router'
import Toast from 'primevue/toast'
import { onMounted } from 'vue'

onMounted(() => {
  // Vous pouvez ajouter ici la logique d'initialisation globale
  console.log('Application SopalTrace initialisée')
})
</script>

<template>
  <div class="app-container bg-slate-50 min-h-screen text-slate-900 font-sans">
    <!-- Le composant Toast pour les notifications globales PrimeVue -->
    <Toast />
    
    <!-- Le router-view affiche la page correspondante à l'URL actuelle -->
    <RouterView :key="$route.fullPath" />
  </div>
</template>

<style>
/* Nous retirons le "scoped" pour que ces règles de base s'appliquent partout.
  Tout le reste du design est géré par Tailwind CSS !
*/
body {
  margin: 0;
  padding: 0;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.app-container {
  width: 100%;
  min-height: 100vh;
  /* overflow: hidden a été retiré pour permettre le scroll sur les longues pages */
}
</style>