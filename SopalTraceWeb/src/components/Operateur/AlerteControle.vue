<template>
  <div class="bg-white border rounded-lg p-4 mt-4 shadow-sm relative">
    <div class="flex justify-between items-center mb-4 border-b pb-2">
      <h3 class="text-xl font-bold text-gray-800">Alertes de Contrôle Actives</h3>
      <div class="text-lg font-mono bg-gray-100 px-3 py-1 rounded text-gray-700 font-semibold border">
        Heure actuelle : {{ currentTime }}
      </div>
    </div>
    
    <div v-if="loading" class="text-gray-500 py-4">Chargement...</div>
    <div v-else-if="tranches.length === 0" class="text-gray-500 py-4">Aucune occurrence en attente.</div>
    <div v-else>
      
      <!-- Zone Contrôles de Réglage -->
      <div v-if="tranchesReglage.length > 0" class="mb-8 bg-yellow-50 p-4 rounded-xl border border-yellow-200">
        <h4 class="text-lg font-bold text-yellow-700 mb-4 flex items-center">
          <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path></svg>
          Contrôles de Réglage Obligatoires
        </h4>
        <div v-for="tranche in tranchesReglage" :key="tranche.trancheHoraire" class="mb-4 bg-white border border-yellow-300 rounded overflow-hidden shadow-sm">
          <div class="bg-yellow-100 p-2 font-bold border-b border-yellow-200 text-yellow-800 flex justify-between items-center">
            <span>{{ formatTranche(tranche.trancheHoraire) }}</span>
          </div>
          
          <div class="p-4 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div v-for="occ in tranche.occurrences" :key="occ.id" 
                 class="p-4 rounded-xl border bg-white shadow-sm flex flex-col justify-between border-yellow-200">
              <div>
                <div class="font-bold flex items-center mb-2">
                  <span class="text-red-600 bg-red-100 px-2 py-0.5 rounded text-xs border border-red-200">⚠️ REQUIS AVANT PRODUCTION</span>
                </div>
                <div class="text-gray-700 font-semibold text-lg">Validation Machine</div>
              </div>
              
              <button @click="ouvrirPlan(occ)" class="mt-4 w-full px-4 py-2 bg-yellow-500 hover:bg-yellow-600 text-white text-sm font-bold rounded shadow transition-colors">
                Exécuter le contrôle
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Zone Contrôles de Production -->
      <div v-if="tranchesProd.length > 0">
        <h4 class="text-lg font-bold text-blue-600 mb-4 flex items-center" v-if="tranchesReglage.length > 0">
          <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          Contrôles Périodiques de Production
        </h4>
        
        <div v-for="tranche in tranchesProd" :key="tranche.trancheHoraire" class="mb-6 border rounded overflow-hidden shadow-sm">
          <div class="bg-slate-100 p-2 font-bold border-b text-slate-800 flex justify-between items-center">
            <span>Tranche : {{ formatTranche(tranche.trancheHoraire) }}</span>
          </div>
          
          <div class="p-4 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 bg-gray-50">
            <div v-for="occ in tranche.occurrences" :key="occ.id" 
                 class="p-4 rounded-xl border bg-white shadow-sm flex flex-col justify-between"
                 :class="getStatutClass(occ)">
              <div>
                <div class="font-bold flex items-center mb-2">
                  <span v-if="occ.estEnRetard" class="text-red-600 bg-red-100 px-2 py-0.5 rounded text-xs border border-red-200">⚠️ EN RETARD</span>
                  <span v-else class="text-yellow-600 bg-yellow-100 px-2 py-0.5 rounded text-xs border border-yellow-200">🔔 MAINTENANT</span>
                </div>
                <div class="text-gray-700 font-semibold text-lg">Occurrence #{{ occ.numeroOccurrence }}</div>
                <div class="text-sm text-gray-500 mt-1">Prévue à : {{ formatTime(occ.heureNotifPrevue) }}</div>
              </div>
              
              <button @click="ouvrirPlan(occ)" class="mt-4 w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-bold rounded shadow transition-colors">
                Ouvrir le Plan de Contrôle
              </button>
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- Modal du Plan de Contrôle -->
    <div v-if="selectedOcc" class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div class="bg-white rounded-xl shadow-2xl w-full max-w-6xl max-h-[90vh] flex flex-col overflow-hidden">
        
        <!-- Header Modal -->
        <div class="bg-slate-800 text-white p-4 flex justify-between items-center">
          <div>
            <h2 class="text-xl font-bold">Plan de Contrôle - Occurrence #{{ selectedOcc.numeroOccurrence }}</h2>
            <p class="text-slate-300 text-sm mt-1">Heure prévue : {{ formatTime(selectedOcc.heureNotifPrevue) }} | Tranche : {{ formatTranche(selectedOcc.trancheHoraire) }}</p>
          </div>
          <button @click="fermerPlan" class="text-slate-300 hover:text-white font-bold text-2xl px-2">&times;</button>
        </div>

        <!-- Body Modal (Tableau) -->
        <div class="p-4 overflow-y-auto flex-1 bg-gray-50">
          <div v-if="selectedOcc.caracteristiques && selectedOcc.caracteristiques.length > 0" class="bg-white border rounded-lg overflow-hidden shadow-sm">
            <table class="w-full text-sm text-left">
              <thead class="text-xs text-gray-700 bg-slate-100 border-b">
                <tr>
                  <th class="px-3 py-3 w-48">Caractéristique</th>
                  <th class="px-3 py-3 w-32">Spécification</th>
                  <th class="px-3 py-3 w-32">Type</th>
                  <th class="px-3 py-3 w-32">Moyen</th>
                  <th class="px-3 py-3 w-32">Instrument</th>
                  <th class="px-3 py-3 w-40 text-center">Résultat (C/NC)</th>
                  <th class="px-3 py-3">Valeur / Remarque</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="cara in selectedOcc.caracteristiques" :key="cara.lignePlanId" class="border-b last:border-b-0 hover:bg-blue-50 transition-colors">
                  <td class="px-3 py-3 font-semibold text-gray-900">{{ cara.libelle }}</td>
                  <td class="px-3 py-3 text-gray-600 font-mono bg-gray-50 font-medium">{{ cara.limiteSpecTexte || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ cara.typeControle || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ cara.moyenControle || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs font-mono">{{ cara.instrument || '-' }}</td>
                  <td class="px-3 py-3 text-center">
                    <div class="inline-flex rounded-md shadow-sm" role="group">
                      <button type="button" 
                              @click="setLigneResultat(selectedOcc.id, cara.lignePlanId, 'C')"
                              :class="getLigneResultat(selectedOcc.id, cara.lignePlanId) === 'C' ? 'bg-green-500 text-white border-green-600' : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'"
                              class="px-4 py-1.5 text-sm font-bold border rounded-l-lg transition-colors">
                        C
                      </button>
                      <button type="button" 
                              @click="setLigneResultat(selectedOcc.id, cara.lignePlanId, 'NC')"
                              :class="getLigneResultat(selectedOcc.id, cara.lignePlanId) === 'NC' ? 'bg-red-500 text-white border-red-600' : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'"
                              class="px-4 py-1.5 text-sm font-bold border-t border-b border-r rounded-r-lg transition-colors">
                        NC
                      </button>
                    </div>
                  </td>
                  <td class="px-3 py-3">
                    <input v-if="cara.typeControle === 'Mesure'" type="number" step="0.01" 
                           v-model="forms[selectedOcc.id][cara.lignePlanId].valeurMesuree"
                           class="w-full border rounded p-1.5 text-sm focus:ring-blue-500 focus:border-blue-500" placeholder="Valeur...">
                    <input v-if="getLigneResultat(selectedOcc.id, cara.lignePlanId) === 'NC'" type="text" 
                           v-model="forms[selectedOcc.id][cara.lignePlanId].remarque"
                           class="w-full border border-red-300 rounded p-1.5 text-sm mt-1 focus:ring-red-500 focus:border-red-500" placeholder="Remarque obligatoire...">
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="text-sm text-gray-500 italic mt-2 text-center py-8">
            Aucune caractéristique détaillée pour cette occurrence.
          </div>
        </div>

        <!-- Footer Modal -->
        <div class="border-t p-4 bg-white flex justify-end gap-3">
          <button @click="fermerPlan" class="px-4 py-2 border text-gray-600 font-bold rounded-lg hover:bg-gray-50 transition-colors">
            Annuler
          </button>
          <button v-if="selectedOcc.estEnRetard" @click="soumettreOccurrence(selectedOcc, 'REGLAGE')" 
                  class="px-6 py-2 bg-yellow-500 text-white font-bold rounded-lg hover:bg-yellow-600 shadow transition-colors flex items-center gap-2">
            ⚙️ Déclarer comme Réglage
          </button>
          <button @click="soumettreOccurrence(selectedOcc)" 
                  :disabled="!isOccurrenceComplete(selectedOcc)"
                  class="px-6 py-2 bg-blue-600 text-white font-bold rounded-lg hover:bg-blue-700 shadow disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2">
            ✅ Valider et Enregistrer
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue';
import operateurService from '@/services/operateurService';

const props = defineProps({
  execControleOfId: {
    type: String,
    required: true
  }
});

const tranches = ref([]);
const loading = ref(true);
let pollInterval = null;
let clockInterval = null;

const currentTime = ref('');
const selectedOcc = ref(null);

const forms = ref({});

const tranchesReglage = computed(() => {
  return tranches.value.filter(t => t.trancheHoraire && t.trancheHoraire.startsWith('REGLAGE'));
});

const tranchesProd = computed(() => {
  return tranches.value.filter(t => !t.trancheHoraire || !t.trancheHoraire.startsWith('REGLAGE'));
});

const updateClock = () => {
  currentTime.value = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
};

const initForms = (data) => {
  data.forEach(tranche => {
    tranche.occurrences.forEach(occ => {
      if (!forms.value[occ.id]) {
        forms.value[occ.id] = {};
        if (occ.caracteristiques) {
          occ.caracteristiques.forEach(cara => {
            forms.value[occ.id][cara.lignePlanId] = {
              lignePlanId: cara.lignePlanId,
              resultat: null,
              valeurMesuree: null,
              remarque: '',
              actionCorrective: ''
            };
          });
        }
      }
    });
  });
};

const fetchAlertes = async () => {
  try {
    const response = await operateurService.getAlertesActives(props.execControleOfId);
    tranches.value = response.data;
    initForms(response.data);
  } catch (error) {
    console.error('Erreur lors du chargement des alertes', error);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  updateClock();
  clockInterval = setInterval(updateClock, 1000);
  
  fetchAlertes();
  pollInterval = setInterval(fetchAlertes, 60000); // Polling toutes les minutes
});

onUnmounted(() => {
  if (pollInterval) clearInterval(pollInterval);
  if (clockInterval) clearInterval(clockInterval);
});

const formatTranche = (tranche) => {
  if (tranche === 'REGLAGE_DEBUT') return 'Démarrage (Réglage Initial)';
  
  const parts = tranche.split('_');
  if (parts.length === 3 && parts[0] === 'H') {
    return `${parts[1].padStart(2, '0')}:00 - ${parts[2].padStart(2, '0')}:00`;
  }
  return tranche;
};

const formatTime = (dateString) => {
  return new Date(dateString).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
};

const getStatutClass = (occ) => {
  if (occ.estEnRetard) return 'border-red-300 hover:border-red-400';
  return 'border-yellow-300 hover:border-yellow-400';
};

const ouvrirPlan = (occ) => {
  selectedOcc.value = occ;
};

const fermerPlan = () => {
  selectedOcc.value = null;
};

const setLigneResultat = (occId, ligneId, res) => {
  if (forms.value[occId] && forms.value[occId][ligneId]) {
    forms.value[occId][ligneId].resultat = res;
  }
};

const getLigneResultat = (occId, ligneId) => {
  return forms.value[occId]?.[ligneId]?.resultat;
};

const isOccurrenceComplete = (occ) => {
  if (!occ.caracteristiques || occ.caracteristiques.length === 0) return true;
  return occ.caracteristiques.every(cara => {
    return forms.value[occ.id]?.[cara.lignePlanId]?.resultat != null;
  });
};

const soumettreOccurrence = async (occ, forceResultat = null) => {
  try {
    const lignesForms = Object.values(forms.value[occ.id] || {});
    
    let resultatGlobal = forceResultat;
    if (!resultatGlobal) {
      const aDesNC = lignesForms.some(l => l.resultat === 'NC');
      resultatGlobal = aDesNC ? 'NC' : 'C';
    }

    const payload = {
      resultat: resultatGlobal,
      matriculeOperateur: 'OP01', // Simulation
      lignes: forceResultat === 'REGLAGE' ? [] : lignesForms
    };

    await operateurService.repondreOccurrence(occ.id, payload);
    
    delete forms.value[occ.id];
    fermerPlan();
    fetchAlertes();
  } catch (error) {
    alert('Erreur: ' + (error.response?.data?.message || error.message));
  }
};
</script>
