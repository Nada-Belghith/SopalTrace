<template>
  <div class="flex h-[80vh] border border-gray-300 rounded-lg overflow-hidden bg-white mb-6">
    <!-- Sidebar spécifique au Poste -->
    <div class="w-64 bg-slate-800 text-white p-4 overflow-y-auto">
      <h3 class="text-xl font-bold mb-6 border-b border-slate-600 pb-2">Poste: {{ poste.libelle }}</h3>
      
      <div class="mb-6">
        <h4 class="text-sm uppercase text-slate-400 font-semibold mb-2">Plan de Contrôle</h4>
        <ul class="space-y-2">
          <li class="p-2 bg-slate-700 rounded text-sm hover:bg-slate-600 cursor-pointer">
            Plan associé au poste
          </li>
        </ul>
      </div>

      <div>
        <h4 class="text-sm uppercase text-slate-400 font-semibold mb-2">Vérifications Machine</h4>
        <ul class="space-y-2">
          <li v-for="machine in machines" :key="machine.codeMachine" class="p-2 bg-slate-700 rounded text-sm hover:bg-slate-600 cursor-pointer">
            {{ machine.libelle }}
          </li>
          <li v-if="machines.length === 0" class="text-xs text-slate-500">
            Aucune machine affectée
          </li>
        </ul>
      </div>
    </div>

    <!-- Main Content -->
    <div class="flex-1 p-6 overflow-y-auto bg-slate-50">
      <div v-if="!activeOf">
        <h2 class="text-2xl font-bold text-slate-800 mb-6">Démarrer un Ordre de Fabrication</h2>
        
        <form @submit.prevent="demarrerOf" class="bg-white p-6 rounded-lg shadow-sm border max-w-md">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Numéro OF</label>
            <input v-model="form.numeroOf" type="text" class="w-full border rounded p-2" required>
          </div>
          
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Type d'opération</label>
            <select v-model="form.operationCode" class="w-full border rounded p-2" required>
              <option value="USI">Usinage (USI)</option>
              <option value="TRN">Tournage (TRN)</option>
              <option value="ESTOMP">Estampage (ESTOMP)</option>
              <option value="ASS">Assemblage (ASS)</option>
            </select>
          </div>

          <div v-if="['USI', 'TRN', 'ESTOMP'].includes(form.operationCode)" class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Machine</label>
            <select v-model="form.machineCode" class="w-full border rounded p-2">
              <option value="">-- Confirmer la machine --</option>
              <option v-for="m in machines" :key="m.codeMachine" :value="m.codeMachine">{{ m.libelle }}</option>
            </select>
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Équipe</label>
            <select v-model="form.numEquipe" class="w-full border rounded p-2">
              <option value="1">Matin</option>
              <option value="2">Après-midi</option>
              <option value="3">Nuit</option>
            </select>
          </div>

          <button type="submit" class="w-full bg-blue-600 text-white p-2 rounded hover:bg-blue-700">
            Démarrer
          </button>
        </form>
      </div>

      <div v-else>
        <div class="flex justify-between items-start mb-6">
          <div>
            <h2 class="text-2xl font-bold text-slate-800">OF: {{ activeOf.numeroOf }}</h2>
            <p class="text-gray-500">Statut: 
              <span class="font-semibold" :class="{'text-yellow-600': activeOf.statut === 'PAUSE', 'text-green-600': activeOf.statut === 'EN_COURS'}">
                {{ activeOf.statut }}
              </span>
            </p>
          </div>
          
          <div class="space-x-2">
            <button v-if="activeOf.statut === 'EN_COURS'" @click="mettreEnPause" class="px-4 py-2 bg-yellow-500 text-white rounded hover:bg-yellow-600">
              Pause
            </button>
            <button v-if="activeOf.statut === 'PAUSE'" @click="reprendre" class="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600">
              Reprendre
            </button>
            <button @click="cloturer" class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700">
              Clôturer (Chef d'équipe)
            </button>
          </div>
        </div>

        <AlerteControle :execControleOfId="activeOf.id" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import operateurService from '@/services/operateurService';
import AlerteControle from './AlerteControle.vue';

const props = defineProps({
  poste: {
    type: Object,
    required: true
  }
});

const machines = ref([]);
const activeOf = ref(null);

const form = ref({
  numeroOf: '',
  operationCode: 'USI',
  machineCode: '',
  numEquipe: 1,
  matriculeOperateur: 'OP001' // Simulé
});

onMounted(async () => {
  try {
    const res = await operateurService.getMachinesPourPoste(props.poste.codePoste);
    machines.value = res.data;
  } catch (error) {
    console.error('Erreur chargement machines', error);
  }
});

const demarrerOf = async () => {
  try {
    const data = { ...form.value };
    if (data.operationCode === 'ASS') {
      data.posteCode = props.poste.codePoste;
    }
    const res = await operateurService.demarrerOf(data);
    activeOf.value = res.data;
  } catch (error) {
    alert('Erreur au démarrage: ' + (error.response?.data?.message || error.message));
  }
};

const mettreEnPause = async () => {
  try {
    await operateurService.mettreEnPauseOf(activeOf.value.id);
    activeOf.value.statut = 'PAUSE';
  } catch (error) {
    console.error(error);
  }
};

const reprendre = async () => {
  try {
    await operateurService.reprendreOf(activeOf.value.id);
    activeOf.value.statut = 'EN_COURS';
  } catch (error) {
    console.error(error);
  }
};

const cloturer = async () => {
  if (!confirm('Êtes-vous sûr de vouloir clôturer cet OF ?')) return;
  try {
    await operateurService.cloturerOf(activeOf.value.id);
    activeOf.value = null; // Retour au formulaire
  } catch (error) {
    console.error(error);
  }
};
</script>
