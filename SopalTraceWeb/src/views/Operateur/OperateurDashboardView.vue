<template>
  <div class="min-h-screen bg-slate-50 flex flex-col p-8">
    <Toast position="top-right" />
    <header class="mb-8">
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
      
      <!-- Bouton Retour -->
      <button @click="quitterExecution" class="mb-4 text-blue-600 hover:text-blue-800 hover:underline text-sm flex items-center w-max transition-colors font-medium">
        <i class="pi pi-arrow-left mr-2"></i> Retour à la liste des OFs
      </button>

      <div class="flex justify-between items-start mb-6 pb-4 border-b">
        <div>
          <h2 class="text-2xl font-bold text-slate-800">OF: {{ activeOfContext.numeroOf }} <span class="text-lg text-slate-500 font-normal">({{ activeOfContext.operationCode }})</span></h2>
          <p class="text-gray-500">Machine Réelle: <span class="font-bold text-slate-700">{{ activeOfContext.machineCode }}</span></p>
          <p class="text-gray-500">Statut: 
            <span class="font-semibold" :class="{'text-yellow-600': activeOfContext.statut === 'REGLAGE', 'text-green-600': activeOfContext.statut === 'EN_COURS'}">
              {{ activeOfContext.statut }}
            </span>
          </p>
        </div>
        
        <div class="space-x-2">
          <div v-if="activeOfContext.statut === 'EN_COURS' && activeOfContext.a_Des_Controles_Reglage" class="inline-block mr-2">
            <button v-if="!activeOfContext.estEnReglage" @click="mettreEnReglage" class="px-4 py-2 bg-yellow-500 text-white font-semibold rounded-lg hover:bg-yellow-600 shadow-sm transition">
              Mettre Aux Réglages (Pause)
            </button>
            <span v-else class="px-4 py-2 bg-slate-200 text-yellow-700 font-bold rounded-lg border border-yellow-300 flex items-center">
              <i class="pi pi-pause-circle mr-2"></i> EN RÉGLAGE (Production en pause)
            </span>
          </div>
          <button @click="cloturer" class="px-4 py-2 bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700 shadow-sm transition">
            Clôturer l'OF
          </button>
        </div>
      </div>
      <div class="flex-1 overflow-y-auto">
        <AlerteControle :execControleOfId="activeOfContext.id" />
      </div>
    </div>

    <!-- Liste des OFs sous forme de Cards -->
    <div v-else class="flex flex-wrap gap-6 ml-9">
      <div v-for="(of, index) in filteredOfs" :key="index" 
           @click="ouvrirModalDemarrage(of)"
           class="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 w-[400px] cursor-pointer hover:shadow-md hover:border-blue-300 transition-all flex flex-col justify-between">
        
        <div class="flex justify-between items-start mb-4">
          <span class="text-xs font-bold text-blue-600 tracking-wider">OF: {{ of.numeroOf }}</span>
          <span class="px-2 py-1 bg-green-50 text-green-600 font-bold text-[10px] rounded uppercase tracking-wider"
                :class="of.statutOf === 'EN_COURS' ? 'bg-green-50 text-green-600' : 'bg-yellow-50 text-yellow-600'">
            {{ of.statutOf }}
          </span>
        </div>

        <div class="mb-6">
          <h3 class="text-lg font-bold text-slate-800 leading-tight mb-1">{{ of.designationArticle }}</h3>
          <p class="text-sm text-slate-400 font-medium">{{ of.codeArticle }}</p>
        </div>

        <div class="flex justify-between items-end border-t border-slate-100 pt-4 mt-auto">
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Qté lancée</p>
            <p class="text-sm font-bold text-slate-700">{{ of.quantiteLancee }} / {{ of.quantitePrevue }}</p>
          </div>
          <div class="text-right">
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Date début</p>
            <p class="text-sm font-medium text-slate-600">{{ formatDate(of.dateDebut) }}</p>
          </div>
        </div>
      </div>
      
      <div v-if="filteredOfs.length === 0 && !loading" class="text-slate-500 mt-4">
        Aucun OF préparé disponible pour le moment dans cette catégorie.
      </div>
    </div>

    <!-- Modal Démarrer le contrôle -->
    <div v-if="showModal" class="fixed inset-0 bg-slate-900 bg-opacity-50 flex items-center justify-center z-50 p-4 backdrop-blur-sm transition-opacity">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
        
        <div class="p-6 border-b border-slate-100 flex justify-between items-start">
          <div>
            <h2 class="text-2xl font-bold text-slate-800">Démarrer le contrôle</h2>
            <p class="text-sm text-slate-500 mt-1">OF: {{ selectedOf.numeroOf }} - {{ selectedOf.designationArticle }}</p>
          </div>
          <button @click="fermerModal" class="text-slate-400 hover:text-slate-600">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
          </button>
        </div>

        <div v-if="errorMessage" class="m-6 p-4 bg-red-50 border border-red-200 rounded-xl flex items-start animate-fade-in-down">
          <svg class="w-6 h-6 text-red-500 mr-3 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
          <div>
            <h3 class="text-red-800 font-bold text-sm">Opération impossible</h3>
            <p class="text-red-600 text-sm mt-1">{{ errorMessage }}</p>
            <p class="text-red-600 text-sm mt-1 font-semibold mb-3">Veuillez demander au responsable d'activer un plan pour cet article.</p>
            
            <button v-if="!planSignale" @click="signalerPlanManquant" :disabled="isSignalingPlan" class="px-4 py-2 bg-red-600 text-white text-xs font-bold rounded-lg hover:bg-red-700 transition flex items-center shadow-sm">
              <svg v-if="!isSignalingPlan" class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path></svg>
              <svg v-else class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
              Signaler au Responsable DI
            </button>
            <div v-else class="text-green-700 font-bold text-sm flex items-center mt-2 bg-green-50 p-2 rounded-lg">
              <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
              Signalement envoyé avec succès !
            </div>
          </div>
        </div>

        <div v-else class="p-6 overflow-y-auto flex-1">
          <p class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4">Gamme Opératoire</p>
          
          <div class="space-y-3">
            <div v-for="(op, idx) in selectedOf.gammeOperatoire" :key="idx"
                 class="border rounded-xl transition-all"
                 :class="form.operationCode === op.operationCode ? 'border-blue-200 bg-blue-50/30' : 'border-slate-200 hover:border-slate-300'">
              
              <label class="flex items-center p-4 cursor-pointer">
                <input type="radio" :value="op.operationCode" v-model="form.operationCode" @change="onOperationChange(op)"
                       class="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer">
                <span class="ml-3 font-semibold text-slate-700">{{ op.libelle }}</span>
              </label>

              <!-- Paramètres étendus si l'opération est sélectionnée -->
              <div v-if="form.operationCode === op.operationCode" class="px-4 pb-4 pt-1 animate-fade-in-down">
                <div class="flex gap-4">
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-500 uppercase tracking-wider mb-1">Machine / Poste Prévu</label>
                    <input type="text" :value="op.machinePrevueCode || 'N/A'" disabled 
                           class="w-full border border-slate-200 rounded-lg p-2.5 bg-white text-slate-500 text-sm font-medium">
                  </div>
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Machine / Poste Réel</label>
                    <input type="text" v-model="form.machineCode" placeholder="Machine utilisée"
                           class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none transition-all">
                  </div>
                  <div class="w-32">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Équipe</label>
                    <select v-model="form.numEquipe" class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none">
                      <option value="1">Matin</option>
                      <option value="2">A-Midi</option>
                      <option value="3">Nuit</option>
                    </select>
                  </div>
                </div>

                <!-- Champs spécifiques pour le Tronçonnage -->
                <div v-if="form.operationCode === 'TRONC' || form.operationCode === 'TRN'" class="flex gap-4 mt-4 animate-fade-in-down border-t border-blue-100 pt-4">
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Longueur (mm)</label>
                    <input type="number" step="0.01" v-model="form.longueur" placeholder="Ex: 120.5"
                           class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none transition-all">
                  </div>
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Diamètre (mm)</label>
                    <input type="number" step="0.01" v-model="form.diametre" placeholder="Ex: 15.2"
                           class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none transition-all">
                  </div>
                </div>
              </div>
            </div>
            
            <div v-if="!selectedOf.gammeOperatoire || selectedOf.gammeOperatoire.length === 0" class="text-sm text-red-500 p-4 bg-red-50 rounded-lg">
              Aucune gamme opératoire définie pour cet article.
            </div>
          </div>
        </div>

        <div class="p-5 border-t border-slate-100 bg-slate-50 flex justify-end space-x-3">
          <button @click="fermerModal" class="px-5 py-2.5 rounded-lg text-slate-600 font-semibold hover:bg-slate-200 transition-colors">
            Annuler
          </button>
          <button v-if="!errorMessage" @click="demarrerOf" :disabled="!form.operationCode"
                  class="px-5 py-2.5 rounded-lg bg-blue-600 text-white font-semibold flex items-center hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Démarrer le contrôle
          </button>
        </div>

      </div>
    </div>



  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import operateurService from '@/services/operateurService';
import alertesService from '@/services/alertesService';
import AlerteControle from '@/components/Operateur/AlerteControle.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';

const toast = useToast();
const route = useRoute();
const router = useRouter();

const ofs = ref([]);
const loading = ref(true);

const filteredOfs = computed(() => {
  return ofs.value.filter(of => {
    const isFini = of.gammeOperatoire && of.gammeOperatoire.some(o => o.operationCode === 'ASS');
    if (route.path.includes('/of-fini')) {
      return isFini;
    } else if (route.path.includes('/of-semi-fini')) {
      return !isFini;
    }
    return true;
  });
});

const showModal = ref(false);
const selectedOf = ref(null);
const errorMessage = ref('');
const isSignalingPlan = ref(false);
const planSignale = ref(false);



const activeOfContext = ref({}); // Les infos de l'OF démarré

const form = ref({
  numeroOf: '',
  operationCode: '',
  machineCode: '',
  numEquipe: 1,
  matriculeOperateur: 'OP01', // Simulation de l'opérateur connecté
  longueur: null,
  diametre: null
});

const formatDate = (dateStr) => {
  if (!dateStr) return 'N/A';
  const d = new Date(dateStr);
  return d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
};

const chargerOfs = async () => {
  loading.value = true;
  try {
    const res = await operateurService.getAllOfOperations();
    ofs.value = res.data;
    
    // Si on arrive sur la page avec un execId dans l'URL, on l'ouvre
    if (route.query.execution) {
      const execId = route.query.execution;
      const of = ofs.value.find(o => o.activeExecControleOfId === execId);
      if (of) {
        activeOfContext.value = {
          id: of.activeExecControleOfId,
          numeroOf: of.numeroOf,
          operationCode: of.activeOperationCode,
          machineCode: of.activeMachineCode,
          statut: of.activeExecStatut
        };
      }
    }
  } catch (error) {
    console.error("Erreur chargement OFs:", error);
    if(error.message === 'Network Error') {
        alert("Impossible de contacter le serveur. Le backend (API) est-il démarré ?");
    }
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  chargerOfs();
});

const ouvrirModalDemarrage = (of) => {
  if (of.activeExecControleOfId) {
    // Si un contrôle est déjà en cours ou en pause, on l'ouvre directement sans passer par le formulaire
    activeOfContext.value = {
      id: of.activeExecControleOfId,
      numeroOf: of.numeroOf,
      operationCode: of.activeOperationCode,
      machineCode: of.activeMachineCode,
      statut: of.activeExecStatut
    };
    router.push({ query: { execution: of.activeExecControleOfId } });
    return;
  }

  selectedOf.value = of;
  errorMessage.value = '';
  planSignale.value = false;
  form.value = {
    numeroOf: of.numeroOf,
    operationCode: '',
    machineCode: '',
    numEquipe: 1,
    matriculeOperateur: 'OP01',
    longueur: null,
    diametre: null
  };
  
  // Auto select first operation if exists
  if(of.gammeOperatoire && of.gammeOperatoire.length > 0) {
    onOperationChange(of.gammeOperatoire[0]);
  }
  
  showModal.value = true;
};

const fermerModal = () => {
  showModal.value = false;
  selectedOf.value = null;
};

const signalerPlanManquant = async () => {
  isSignalingPlan.value = true;
  try {
    const data = {
      operationCode: form.value.operationCode || (selectedOf.value.gammeOperatoire && selectedOf.value.gammeOperatoire.length > 0 ? selectedOf.value.gammeOperatoire[0].operationCode : 'Inconnu'),
      posteCode: form.value.machineCode || 'Non spécifié',
      articleCode: selectedOf.value.codeArticle,
      descriptionProbleme: errorMessage.value || 'Plan manquant ou introuvable.'
    };
    await alertesService.signalerPlanManquant(data);
    planSignale.value = true;
    toast.add({ severity: 'success', summary: 'Signalé', detail: 'Le responsable a été notifié par email.', life: 4000 });
  } catch (error) {
    const msg = error.response?.data?.message || 'Impossible d\'envoyer le signalement.';
    toast.add({ severity: 'error', summary: 'Erreur', detail: msg, life: 6000 });
  } finally {
    isSignalingPlan.value = false;
  }
};

const onOperationChange = async (op) => {
  form.value.operationCode = op.operationCode;
  form.value.machineCode = op.machinePrevueCode || '';
  errorMessage.value = '';
  
  if (selectedOf.value?.codeArticle) {
    try {
      const res = await operateurService.verifierPlan(selectedOf.value.codeArticle);
      if (!res.data.existe) {
        errorMessage.value = "Aucun plan de contrôle n'est actif pour cet article.";
      }
    } catch (err) {
      console.error(err);
      errorMessage.value = "Erreur lors de la vérification du plan.";
    }
  }
};

const demarrerOf = async () => {
  errorMessage.value = '';
  try {
    const res = await operateurService.demarrerOf({ ...form.value });
    // Démarrage réussi, on ferme le modal et on affiche le contexte
    fermerModal();
    activeOfContext.value = {
      id: res.data.id,
      numeroOf: res.data.numeroOf,
      operationCode: res.data.operationCode,
      machineCode: res.data.machineCode,
      statut: res.data.statut,
      estEnReglage: res.data.estEnReglage,
      a_Des_Controles_Reglage: res.data.a_Des_Controles_Reglage
    };
    router.push({ query: { execution: res.data.id } });
  } catch (error) {
    // Au lieu d'un alert(), on affiche l'erreur dans l'UI
    errorMessage.value = error.response?.data?.message || error.message;
  }
};

const mettreEnReglage = async () => {
  try {
    await operateurService.mettreEnReglage(activeOfContext.value.id);
    toast.add({ severity: 'success', summary: 'Mode Réglage', detail: 'Production mise en pause. Procédez aux contrôles de réglage.', life: 4000 });
    activeOfContext.value.estEnReglage = true;
    await loadAlertesActives(activeOfContext.value.id);
  } catch (error) {
    console.error(error);
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de mettre en réglage.', life: 3000 });
  }
};


const cloturer = async () => {
  if (!confirm('Êtes-vous sûr de vouloir clôturer cet OF ?')) return;
  try {
    await operateurService.cloturerOf(activeOfContext.value.id);
    quitterExecution();
    chargerOfs(); // Rafraîchir la liste
  } catch (error) {
    console.error(error);
  }
};

const quitterExecution = () => {
  activeOfContext.value = {};
  router.push({ query: {} }); // Enlève ?execution=... de l'URL
};

// Gérer le bouton "Précédent" du navigateur
watch(() => route.query.execution, (newVal) => {
  if (!newVal) {
    activeOfContext.value = {};
  }
});
</script>

<style scoped>
.animate-fade-in-down {
  animation: fadeInDown 0.2s ease-out;
}
@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-5px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
