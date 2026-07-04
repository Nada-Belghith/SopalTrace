<script setup>
import { RouterView, useRouter } from 'vue-router'
import Toast from 'primevue/toast'

const router = useRouter();
const goToOf = (execId) => {
  router.push(`/operateur/of-semi-fini?execution=${execId}`);
};
</script>

<template>
  <div class="app-container bg-slate-50 min-h-screen text-slate-900 font-sans">
    <!-- Le composant Toast pour les notifications globales PrimeVue -->
    <Toast />

    <!-- Toast spécifique pour les alertes Opérateur -->
    <Toast position="top-right" group="operator-alert">
      <template #message="slotProps">
        <div class="flex flex-col items-start flex-1 cursor-pointer w-full hover:bg-slate-50 p-2 rounded transition-colors"
             @click="goToOf(slotProps.message.data.execId)">
          <div class="font-bold text-lg text-slate-800">{{ slotProps.message.summary }}</div>
          <div class="font-medium text-slate-600 my-1">{{ slotProps.message.detail }}</div>
          <div class="text-sm mt-2 text-blue-600 font-bold flex items-center">
            Ouvrir le contrôle <i class="pi pi-arrow-right ml-2 text-xs"></i>
          </div>
        </div>
      </template>
    </Toast>
    
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