<template>
  <div class="min-h-screen bg-slate-50 flex flex-col p-8">
    <Toast position="top-right" />
    <header class="mb-8" v-if="!activeOfContext.id">
      <h1 class="text-3xl font-bold text-slate-800 flex items-center">
        <svg class="w-6 h-6 mr-3 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16"></path></svg>
        Tableau de Bord Opérateur
      </h1>
      <p class="text-slate-500 mt-2 text-sm ml-9">Sélectionnez un Ordre de Fabrication pour démarrer vos contrôles.</p>
    </header>

    <div v-if="loading" class="text-center py-10">
      <p class="text-slate-500">Chargement des Ordres de Fabrication...</p>
    </div>

    <!-- Si l'opérateur est dans l'écran de contrôle actif -->
    <div v-else-if="activeOfContext.id" class="flex flex-col bg-white rounded-xl shadow-sm border border-slate-200 p-6 h-[80vh]">
      <div class="flex justify-between items-center mb-4">
        <!-- Bouton Retour -->
        <button @click="quitterExecution" class="text-blue-600 hover:text-blue-800 hover:underline text-sm flex items-center transition-colors font-medium">
          <i class="pi pi-arrow-left mr-2"></i> Retour à la liste des OFs
        </button>
        <!-- Bouton Rafraîchir -->
        <button @click="chargerOfs" class="text-slate-500 hover:text-slate-700 hover:underline text-sm flex items-center transition-colors font-medium">
          <i class="pi pi-refresh mr-2"></i> Rafraîchir les contrôles
        </button>
      </div>

      <ActiveExecutionHeader 
        :activeOfContext="activeOfContext"
        @reprendre="reprendre"
        @mettre-en-reglage="mettreEnReglage"
        @mettre-en-pause="mettreEnPause"
        @cloturer="cloturer"
      />

      <div class="flex-1 overflow-y-auto">
        <AlerteControle 
          :execControleOfId="activeOfContext.id" 
          :aDesControlesReglage="activeOfContext.a_Des_Controles_Reglage"
          :legendeMoyens="activeOfContext.legendeMoyens"
          :remarques="activeOfContext.remarques"
          @occurrence-submitted="chargerOfs" 
        />
      </div>
    </div>

    <!-- Liste des OFs sous forme de Cards -->
    <OfGrid v-else :ofs="filteredOfs" :loading="loading" @select-of="ouvrirModalDemarrage" />

    <!-- Modale de Démarrage -->
    <StartOfModal 
      :showModal="showModal"
      :selectedOf="selectedOf"
      :form="form"
      :errorMessage="errorMessage"
      :isSignalingPlan="isSignalingPlan"
      :planSignale="planSignale"
      :selectedOpState="selectedOpState"
      :isLongueurInitialized="isLongueurInitialized"
      :isDiametreInitialized="isDiametreInitialized"
      @close="fermerModal"
      @signaler="signalerPlanManquant"
      @operation-change="onOperationChange"
      @demarrer="demarrerOf"
      @reprendre-existant="reprendreOfExistant"
    />

    <!-- Modal Interception Départ -->
    <Dialog v-model:visible="showLeaveModal" modal header="Attention : OF toujours actif" :style="{ width: '450px' }" :closable="false">
      <div class="p-2 flex flex-col items-center">
        <div class="bg-yellow-100 p-4 rounded-full mb-4">
          <svg class="w-10 h-10 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
        </div>
        <p class="text-center text-slate-700 font-medium mb-6">
          Vous êtes sur le point de quitter cette page alors que l'OF <span class="font-bold text-slate-900">{{ activeOfContext.numeroOf }}</span> est toujours <span class="text-green-600 font-bold">EN COURS</span>.
          <br><br>Que souhaitez-vous faire ?
        </p>
        <div class="flex flex-col gap-3 w-full">
          <button @click="confirmLeave('pause')" class="w-full px-4 py-3 bg-yellow-500 text-white font-bold rounded-lg hover:bg-yellow-600 shadow-sm transition">
            ⏸️ Mettre l'OF en Pause et quitter
          </button>
          <button @click="confirmLeave('cloture')" class="w-full px-4 py-3 bg-red-600 text-white font-bold rounded-lg hover:bg-red-700 shadow-sm transition">
            ⏹️ Clôturer définitivement l'opération et quitter
          </button>
          <button @click="confirmLeave('quitter')" class="w-full px-4 py-3 bg-blue-600 text-white font-bold rounded-lg hover:bg-blue-700 shadow-sm transition">
            ▶️ Laisser l'OF en cours et quitter quand même
          </button>
          <button @click="cancelLeave" class="w-full px-4 py-3 bg-slate-200 text-slate-700 font-bold rounded-lg hover:bg-slate-300 shadow-sm transition mt-2">
            Annuler (Rester sur la page)
          </button>
        </div>
      </div>
    </Dialog>

    <!-- Modal Clôture Explicite -->
    <Dialog v-model:visible="showClotureModal" modal header="Confirmation de clôture" :style="{ width: '450px' }" :closable="false">
      <div class="p-2 flex flex-col items-center">
        <div class="bg-red-100 p-4 rounded-full mb-4">
          <svg class="w-10 h-10 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
        </div>
        <p class="text-center text-slate-700 font-medium mb-6">
          Êtes-vous sûr de vouloir clôturer l'opération pour cet OF ?<br><br>
          <span class="text-red-600 font-bold">Attention :</span> Tous les contrôles intermédiaires non réalisés seront ignorés.
        </p>
        <div class="flex flex-col gap-3 w-full">
          <button @click="confirmCloture" class="w-full px-4 py-3 bg-red-600 text-white font-bold rounded-lg hover:bg-red-700 shadow-sm transition">
            ⏹️ Oui, clôturer l'opération
          </button>
          <button @click="showClotureModal = false" class="w-full px-4 py-3 bg-slate-200 text-slate-700 font-bold rounded-lg hover:bg-slate-300 shadow-sm transition mt-2">
            Annuler
          </button>
        </div>
      </div>
    </Dialog>

    <!-- Modal Motif Pause Optionnel -->
    <div v-if="raisonModalConfig.show" class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100] p-4 backdrop-blur-sm">
      <div class="bg-white rounded-xl max-w-md w-full p-6 shadow-2xl animate-fade-in-up">
        <h3 class="text-xl font-bold text-gray-900 mb-2 flex items-center">
          <svg class="w-6 h-6 mr-2 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
          {{ raisonModalConfig.titre }}
        </h3>
        <p class="text-sm text-gray-600 mb-4">{{ raisonModalConfig.description }}</p>
        <textarea v-model="raisonModalConfig.texte" 
                  rows="3" 
                  class="w-full border border-gray-300 rounded-lg shadow-sm focus:border-yellow-500 focus:ring-yellow-500 p-3 mb-4 text-sm" 
                  placeholder="Saisissez le motif (Optionnel)..."
                  @keyup.enter="confirmerRaison"
                  autofocus></textarea>
        <div class="flex justify-end gap-3">
          <button @click="annulerRaison" class="px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg font-medium transition-colors">Annuler</button>
          <button @click="confirmerRaison" 
                  class="px-4 py-2 bg-yellow-600 text-white hover:bg-yellow-700 rounded-lg font-medium shadow-sm transition-colors">
            Confirmer la Pause
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter, onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import Dialog from 'primevue/dialog';

// Composants Extraits
import OfGrid from '@/components/Operateur/OfGrid.vue';
import StartOfModal from '@/components/Operateur/StartOfModal.vue';
import ActiveExecutionHeader from '@/components/Operateur/ActiveExecutionHeader.vue';
import AlerteControle from '@/components/Operateur/AlerteControle.vue';

// Composables (Logique Métier)
import { useOfList } from '@/composables/Operateur/useOfList';
import { useOfControls } from '@/composables/Operateur/useOfControls';
import { useStartOfForm } from '@/composables/Operateur/useStartOfForm';

import { useOperateurStore } from '@/stores/execution/operateurStore';

const props = defineProps({
  typeOf: {
    type: String,
    default: null
  }
});

const route = useRoute();
const router = useRouter();
const toast = useToast();
const operateurStore = useOperateurStore();

// 1. Liste et chargement des OFs
const { loading, filteredOfs, chargerOfs, ofs } = useOfList(props);

// 2. Contrôles de l'exécution active
const {
  activeOfContext,
  mettreEnReglage,
  mettreEnPause,
  reprendre,
  cloturer,
  quitterExecution,
  showClotureModal,
  confirmCloture,
  raisonModalConfig,
  annulerRaison,
  confirmerRaison,
  showLeaveModal,
  handleNavigation,
  confirmLeave,
  cancelLeave,
  handleUnload
} = useOfControls({ toast, router, route, chargerOfs });

// 3. Formulaire et modale de démarrage
const {
  showModal,
  selectedOf,
  errorMessage,
  form,
  isSignalingPlan,
  planSignale,
  isLongueurInitialized,
  isDiametreInitialized,
  selectedOpState,
  ouvrirModalDemarrage,
  fermerModal,
  reprendreOfExistant,
  signalerPlanManquant,
  onOperationChange,
  demarrerOf
} = useStartOfForm({ toast, router, activeOfContext });


// Configuration du Store et écouteurs
onMounted(() => {
  chargerOfs();
  
  // Gérer la reprise d'une exécution depuis l'URL
  if (route.query.execution) {
    const execId = route.query.execution;
    let foundOf = null;
    let foundOp = null;
    
    // Attendre que ofs soit chargé
    watch(ofs, (newOfs) => {
      if(newOfs.length > 0) {
         for (const o of newOfs) {
           if (o.gammeOperatoire) {
             const op = o.gammeOperatoire.find(x => x.activeExecControleOfId === execId);
             if (op) {
               foundOf = o;
               foundOp = op;
               break;
             }
           }
         }

         if (foundOf && foundOp) {
           activeOfContext.value = {
             id: foundOp.activeExecControleOfId,
             numeroOf: foundOf.numeroOf,
             operationCode: foundOp.operationCode,
             machineCode: foundOp.activeMachineCode,
             statut: foundOp.activeExecStatut,
             estEnReglage: foundOp.estEnReglage || false,
             a_Des_Controles_Reglage: foundOp.a_Des_Controles_Reglage || false,
             legendeMoyens: foundOp.legendeMoyens,
             remarques: foundOp.remarques
           };
         }
      }
    }, { immediate: true });
  }

  operateurStore.startGlobalPolling(toast, router);
  window.addEventListener('beforeunload', handleUnload);
  window.addEventListener('unload', handleUnload);
});

onUnmounted(() => {
  operateurStore.stopGlobalPolling();
  window.removeEventListener('beforeunload', handleUnload);
  window.removeEventListener('unload', handleUnload);
});

watch(() => route.query.execution, (newVal) => {
  if (!newVal) {
    activeOfContext.value = {};
  } else {
    chargerOfs();
  }
});

onBeforeRouteLeave((to, from) => handleNavigation(to));
onBeforeRouteUpdate((to, from) => handleNavigation(to));

</script>

<style scoped>
.animate-fade-in-down {
  animation: fadeInDown 0.2s ease-out;
}
@keyframes fadeInDown {
  from { opacity: 0; transform: translateY(-5px); }
  to { opacity: 1; transform: translateY(0); }
}
.animate-fade-in-up {
  animation: fadeInUp 0.2s ease-out;
}
@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(5px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
