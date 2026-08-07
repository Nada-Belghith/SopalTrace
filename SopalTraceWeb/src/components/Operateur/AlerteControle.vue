<template>
  <div :class="showHeader ? 'bg-white border rounded-lg p-4 mt-4 shadow-sm relative' : 'relative'">
    <div v-if="showHeader" class="flex justify-between items-center mb-4 border-b pb-2">
      <h3 class="text-xl font-bold text-gray-800">Alertes de Contrôle Actives</h3>
      <div class="text-lg font-mono bg-gray-100 px-3 py-1 rounded text-gray-700 font-semibold border">
        Date et Heure actuelle : {{ currentTime }}
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
            <div class="flex items-center space-x-3">
              <span>Tranche : {{ formatTrancheAvecDate(tranche) }}</span>
              <span v-if="tranche.resultatFinal" 
                    :class="{
                      'bg-green-100 text-green-800 border border-green-200': tranche.resultatFinal === 'C',
                      'bg-red-100 text-red-800 border border-red-200': tranche.resultatFinal === 'NC',
                      'bg-yellow-100 text-yellow-800 border border-yellow-200': tranche.resultatFinal === 'REGLAGE',
                      'bg-gray-200 text-gray-700 border border-gray-300': tranche.resultatFinal === 'IGNORE'
                    }"
                    class="px-2 py-0.5 text-xs rounded font-medium shadow-sm">
                Résultat: {{ tranche.resultatFinal === 'C' ? 'Conforme' : tranche.resultatFinal === 'NC' ? 'Non Conforme' : tranche.resultatFinal === 'REGLAGE' ? 'Réglage' : 'Ignoré' }}
              </span>
            </div>
            <div class="flex gap-2" v-if="tranche.occurrences.some(occ => !isUpcoming(occ))">
              <button v-if="aDesControlesReglage" @click="declarerTrancheReglageAction(tranche.trancheHoraire)" class="text-xs bg-yellow-50 border border-yellow-200 text-yellow-700 px-3 py-1 rounded hover:bg-yellow-100 transition-colors flex items-center shadow-sm">
                ⚙️ Déclarer comme Réglage
              </button>
              <button @click="ignorerTrancheAction(tranche.trancheHoraire)" class="text-xs bg-red-50 border border-red-200 text-red-600 px-3 py-1 rounded hover:bg-red-100 transition-colors flex items-center shadow-sm">
                <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                Ignorer la tranche
              </button>
            </div>
          </div>
          
          <div class="p-4 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 bg-gray-50">
            <div v-for="occ in tranche.occurrences" :key="occ.id" 
                 class="p-4 rounded-xl border bg-white shadow-sm flex flex-col justify-between"
                 :class="getStatutClass(occ)">
              <div>
                <div class="font-bold flex items-center mb-2">
                  <span v-if="isLocallyEnRetard(occ)" class="text-red-600 bg-red-100 px-2 py-0.5 rounded text-xs border border-red-200">⚠️ EN RETARD</span>
                  <span v-else-if="isUpcoming(occ)" class="text-blue-600 bg-blue-100 px-2 py-0.5 rounded text-xs border border-blue-200">⏳ À VENIR</span>
                  <span v-else class="text-yellow-600 bg-yellow-100 px-2 py-0.5 rounded text-xs border border-yellow-200">🔔 MAINTENANT</span>
                </div>
                <div class="text-gray-700 font-semibold text-lg">{{ occ.titreCombine || ('Occurrence #' + occ.numeroOccurrence) }}</div>
                <div class="text-sm text-gray-500 mt-1">Prévue à : {{ formatTime(occ.heureSimulee || occ.heureNotifPrevue) }}</div>
              </div>
              
              <button @click="ouvrirPlan(occ)" 
                      :disabled="isUpcoming(occ)"
                      :class="[
                        'mt-4 w-full px-4 py-2 text-white text-sm font-bold rounded shadow transition-colors',
                        isUpcoming(occ) ? 'bg-gray-400 cursor-not-allowed opacity-75' : 'bg-blue-600 hover:bg-blue-700'
                      ]">
                {{ isUpcoming(occ) ? 'Bientôt disponible' : 'Ouvrir le Plan de Contrôle' }}
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

        <!-- Informations Plan -->
        <div v-if="legendeMoyens || remarquesDeTranche" class="bg-blue-50 border-b border-blue-100 p-4 text-sm text-blue-900">
          <div v-if="legendeMoyens" class="mb-2"><strong class="font-bold">Légende des moyens :</strong> {{ legendeMoyens }}</div>
          <div v-if="remarquesDeTranche"><strong class="font-bold">Remarques / Observations :</strong> {{ remarquesDeTranche }}</div>
        </div>

        <!-- Body Modal (Tableau) -->
        <div class="p-4 overflow-y-auto flex-1 bg-gray-50">
          <div v-if="selectedOcc.caracteristiques && selectedOcc.caracteristiques.length > 0" class="bg-white border rounded-lg overflow-hidden shadow-sm">
            <table class="w-full text-sm text-left">
              <thead class="text-xs text-gray-700 bg-slate-100 border-b">
                <tr>
                  <th class="px-3 py-3 w-48">Caractéristique</th>
                  <th v-for="col in extraCols" :key="col.key" class="px-3 py-3 w-32 font-semibold text-gray-700 uppercase tracking-wider text-xs">{{ col.label }}</th>
                  <th class="px-3 py-3 w-32">Spécification</th>
                  <th class="px-3 py-3 w-32">Type</th>
                  <th class="px-3 py-3 w-32">Moyen</th>
                  <th class="px-3 py-3 w-32">Instrument</th>
                  <th class="px-3 py-3">Observations</th>
                  <th class="px-3 py-3 text-center">
                    <div class="inline-flex rounded-md shadow-sm" role="group">
                      <button @click="setAllResults('C')" class="px-2 py-0.5 text-[10px] uppercase font-bold border rounded-l-lg bg-green-50 text-green-700 hover:bg-green-100 border-green-200 transition-colors" title="Marquer tout comme Conforme">Tout C</button>
                      <button @click="setAllResults('NC')" class="px-2 py-0.5 text-[10px] uppercase font-bold border-t border-b border-r rounded-r-lg bg-red-50 text-red-700 hover:bg-red-100 border-red-200 transition-colors" title="Marquer tout comme Non-Conforme">Tout NC</button>
                    </div>
                  </th>
                  <th class="px-3 py-3 w-48">Valeur / Remarque</th>
                </tr>
              </thead>
              <tbody v-for="group in groupedCaracteristiques" :key="group.sectionId">
                <tr class="bg-blue-100 border-b border-blue-200">
                  <td :colspan="8 + extraCols.length" class="px-3 py-2 text-center font-bold text-blue-900 shadow-sm">
                    {{ formatSectionLibelle(group.sectionLibelle) }}
                  </td>
                </tr>
                <tr v-for="cara in group.items" :key="cara.lignePlanId" class="border-b last:border-b-0 hover:bg-blue-50 transition-colors">
                  <td class="px-3 py-3 font-semibold text-gray-900">
                    <div v-if="cara.imageBase64" class="mb-2">
                      <img :src="cara.imageBase64" class="max-h-12 object-contain rounded border border-slate-200 shadow-sm" alt="Image caractéristique" />
                    </div>
                    {{ cara.libelle }}
                  </td>
                  <td v-for="col in extraCols" :key="col.key" class="px-3 py-3 text-gray-700 text-xs font-mono font-medium bg-gray-50/50">
                    {{ getExtraValue(cara, col.key) }}
                  </td>
                  <td class="px-3 py-3 text-gray-600 font-mono bg-gray-50 font-medium">{{ cara.limiteSpecTexte || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ cara.typeControle || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ cara.moyenControle || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs font-mono">{{ cara.instrument || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ cara.observations || '-' }}</td>
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
                    <div>
                      <input v-if="cara.typeControle === 'Mesure'" type="number" step="0.01" 
                             v-model="forms[selectedOcc.id][cara.lignePlanId].valeurMesuree"
                             class="w-full border rounded p-1.5 text-sm focus:ring-blue-500 focus:border-blue-500 mb-1" placeholder="Valeur...">
                      <div v-if="getLigneResultat(selectedOcc.id, cara.lignePlanId)">
                        <input type="text" 
                               v-model="forms[selectedOcc.id][cara.lignePlanId].remarque"
                               class="w-full border rounded p-1.5 text-sm focus:ring-blue-500 focus:border-blue-500"
                               :class="getLigneResultat(selectedOcc.id, cara.lignePlanId) === 'NC' ? 'border-red-300' : 'border-gray-300'"
                               :placeholder="getLigneResultat(selectedOcc.id, cara.lignePlanId) === 'NC' ? 'Détails de la Non-Conformité...' : 'Remarque ou observation (optionnel)...'">
                        <input v-if="getLigneResultat(selectedOcc.id, cara.lignePlanId) === 'NC'" type="text" 
                               v-model="forms[selectedOcc.id][cara.lignePlanId].actionCorrective"
                               class="w-full border border-red-300 rounded p-1.5 text-sm mt-1 focus:ring-red-500 focus:border-red-500" placeholder="Action corrective (obligatoire pour NC)...">
                      </div>
                    </div>
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
          
          <button v-if="!selectedOcc.trancheHoraire?.startsWith('REGLAGE')" 
                  @click="soumettreOccurrence(selectedOcc, 'IGNORE')" 
                  class="px-4 py-2 bg-gray-200 text-gray-700 font-bold rounded-lg hover:bg-gray-300 transition-colors flex items-center gap-2 mr-auto">
            🚫 Ignorer ce contrôle
          </button>
          
          <button v-if="aDesControlesReglage && !selectedOcc.trancheHoraire?.startsWith('REGLAGE')" 
                  @click="soumettreOccurrence(selectedOcc, 'REGLAGE')" 
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
    <!-- Modal Motif Optionnel -->
    <div v-if="raisonModalConfig.show" class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100] p-4 backdrop-blur-sm">
      <div class="bg-white rounded-xl max-w-md w-full p-6 shadow-2xl animate-fade-in-up">
        <h3 class="text-xl font-bold text-gray-900 mb-2 flex items-center">
          <svg class="w-6 h-6 mr-2 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          {{ raisonModalConfig.titre }}
        </h3>
        <p class="text-sm text-gray-600 mb-4">{{ raisonModalConfig.description }}</p>
        <textarea v-model="raisonModalConfig.texte" 
                  rows="3" 
                  class="w-full border border-gray-300 rounded-lg shadow-sm focus:border-blue-500 focus:ring-blue-500 p-3 mb-4 text-sm" 
                  placeholder="Saisissez le motif (Optionnel)..."
                  @keyup.enter="confirmerRaison"
                  autofocus></textarea>
        <div class="flex justify-end gap-3">
          <button @click="raisonModalConfig.show = false" class="px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg font-medium transition-colors">Annuler</button>
          <button @click="confirmerRaison" 
                  class="px-4 py-2 bg-blue-600 text-white hover:bg-blue-700 rounded-lg font-medium shadow-sm transition-colors">
            Confirmer
          </button>
        </div>
      </div>
    </div>
    
    <!-- Modal Confirmation Simple -->
    <div v-if="confirmModalConfig.show" class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100] p-4 backdrop-blur-sm">
      <div class="bg-white rounded-xl max-w-md w-full p-6 shadow-2xl animate-fade-in-up">
        <h3 class="text-xl font-bold text-gray-900 mb-2 flex items-center">
          <svg class="w-6 h-6 mr-2 text-yellow-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
          {{ confirmModalConfig.titre }}
        </h3>
        <p class="text-sm text-gray-600 mb-6">{{ confirmModalConfig.description }}</p>
        <div class="flex justify-end gap-3">
          <button @click="confirmModalConfig.show = false" class="px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg font-medium transition-colors">Annuler</button>
          <button @click="confirmerAction" 
                  class="px-4 py-2 bg-yellow-500 text-white hover:bg-yellow-600 rounded-lg font-medium shadow-sm transition-colors">
            Confirmer
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import operateurService from '@/services/operateurService';
import { useOperateurStore } from '@/stores/execution/operateurStore';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();

const props = defineProps({
  execControleOfId: {
    type: String,
    required: true
  },
  aDesControlesReglage: {
    type: Boolean,
    default: false
  },
  legendeMoyens: {
    type: String,
    default: ''
  },
  remarques: {
    type: String,
    default: ''
  },
  showHeader: {
    type: Boolean,
    default: true
  }
});

const emit = defineEmits(['occurrence-submitted']);

const operateurStore = useOperateurStore();

const tranches = computed(() => operateurStore.alertesParOf[props.execControleOfId] || []);
const loading = ref(true);
let clockInterval = null;
let pollInterval = null;

const currentTime = ref('');
const selectedOcc = ref(null);

const forms = ref({});

const tranchesReglage = computed(() => {
  return tranches.value.filter(t => t.trancheHoraire && t.trancheHoraire.startsWith('REGLAGE'));
});

const tranchesProd = computed(() => {
  return tranches.value.filter(t => !t.trancheHoraire || !t.trancheHoraire.startsWith('REGLAGE'));
});

const trancheOfSelectedOcc = computed(() => {
  if (!selectedOcc.value) return null;
  return tranches.value.find(t => t.trancheHoraire === selectedOcc.value.trancheHoraire);
});

const remarquesDeTranche = computed(() => {
  if (!trancheOfSelectedOcc.value) return props.remarques;
  let r = trancheOfSelectedOcc.value.remarques || '';
  if (trancheOfSelectedOcc.value.detailsNc) {
     r += (r ? ' | ' : '') + 'NC: ' + trancheOfSelectedOcc.value.detailsNc;
  }
  return r || props.remarques;
});

const groupedCaracteristiques = computed(() => {
  if (!selectedOcc.value || !selectedOcc.value.caracteristiques) return [];
  
  const map = new Map();
  for (const cara of selectedOcc.value.caracteristiques) {
    if (!map.has(cara.sectionId)) {
      map.set(cara.sectionId, {
        sectionId: cara.sectionId,
        sectionLibelle: cara.sectionLibelle || 'Caractéristiques',
        items: []
      });
    }
    map.get(cara.sectionId).items.push(cara);
  }
  
  return Array.from(map.values());
});

const formatSectionLibelle = (lib) => {
  if (!lib) return '';
  // Le backend envoie déjà le libellé formaté dynamiquement
  return lib;
};

const isPlanAssSection = (lib) => {
  if (!lib) return false;
  const lower = lib.toLowerCase();
  if (lower.includes("échantillonnage") || lower.includes("echantillonnage") || lower.includes("fe0591")) {
    return false;
  }
  return lower.includes("100%") || lower.includes("100 %") || lower.includes("plan d'assemblage") || lower.includes("plan_ass");
};

const extraCols = computed(() => {
  if (!selectedOcc.value?.caracteristiques) return [];
  const map = new Map();
  selectedOcc.value.caracteristiques.forEach(c => {
    if (c.extraColonnes && Array.isArray(c.extraColonnes)) {
      c.extraColonnes.forEach(ec => {
        if (ec.cleColonne) {
          const label = ec.labelAffiche || ec.cleColonne;
          map.set(ec.cleColonne, label);
        }
      });
    }
  });
  return Array.from(map.entries()).map(([key, label]) => ({ key, label }));
});

const getExtraValue = (cara, key) => {
  if (!cara.extraColonnes || !Array.isArray(cara.extraColonnes)) return '-';
  const found = cara.extraColonnes.find(ec => ec.cleColonne === key);
  return found?.valeurColonne || '-';
};

const updateClock = () => {
  currentTime.value = new Date().toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' });
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
    const pCode = props.posteCode || route.query.posteCode || null;
    const response = await operateurService.getAlertesActives(props.execControleOfId, pCode);
    operateurStore.alertesParOf[props.execControleOfId] = response.data;
    
    if (route.query.autoOpen1stPending === 'true' && response.data && response.data.length > 0) {
      let firstPending = null;
      for (const tranche of response.data) {
        if (tranche.occurrences) {
          firstPending = tranche.occurrences.find(o => !isUpcoming(o) && !o.estRepondu);
          if (firstPending) break;
        }
      }
      if (firstPending && !selectedOcc.value) {
        ouvrirPlan(firstPending);
        const query = { ...route.query };
        delete query.autoOpen1stPending;
        router.replace({ query });
      }
    }
  } catch (error) {
    console.error('Erreur lors du chargement des alertes', error);
  } finally {
    loading.value = false;
  }
};

watch(tranches, (newVal) => {
  if (newVal && newVal.length > 0) {
    initForms(newVal);
  }
}, { immediate: true, deep: true });



onMounted(() => {
  updateClock();
  clockInterval = setInterval(updateClock, 1000);
  
  fetchAlertes();
  pollInterval = setInterval(() => {
    if (!selectedOcc.value) {
      fetchAlertes();
    }
  }, 300000);
});

onUnmounted(() => {
  if (clockInterval) clearInterval(clockInterval);
  if (pollInterval) clearInterval(pollInterval);
  operateurStore.isPollingPaused = false;
});

const formatTranche = (tranche) => {
  if (!tranche) return '';
  if (tranche === 'REGLAGE_DEBUT') return 'Démarrage (Réglage Initial)';
  
  const realTranche = tranche.split('|')[0];
  const parts = realTranche.split('_');
  if (parts.length === 3 && parts[0] === 'H') {
    return `${parts[1].padStart(2, '0')}:00 - ${parts[2].padStart(2, '0')}:00`;
  }
  return realTranche;
};

const formatTrancheAvecDate = (trancheObj) => {
  const timeStr = formatTranche(trancheObj.trancheHoraire);
  if (trancheObj.trancheHoraire === 'REGLAGE_DEBUT') return timeStr;
  
  if (trancheObj.occurrences && trancheObj.occurrences.length > 0) {
    const occ = trancheObj.occurrences[0];
    const targetDate = occ.heureSimulee || occ.heureNotifPrevue;
    if (targetDate) {
      const dateStr = new Date(targetDate).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' });
      return `${dateStr} | ${timeStr}`;
    }
  }
  return timeStr;
};

const formatTime = (dateString) => {
  return new Date(dateString).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' });
};

const isLocallyEnRetard = (occ) => {
  if (occ.estEnRetard) return true;
  if (tranches.value) {
    const allOccs = tranches.value.flatMap(t => t.occurrences);
    const myDate = new Date(occ.heureNotifPrevue);
    return allOccs.some(o => {
      if (!o.heureNotifPrevue) return false;
      const oDate = new Date(o.heureNotifPrevue);
      return oDate > myDate && !isUpcoming(o);
    });
  }
  return false;
};

const getStatutClass = (occ) => {
  if (isLocallyEnRetard(occ)) return 'border-red-300 hover:border-red-400';
  if (isUpcoming(occ)) return 'border-blue-300 hover:border-blue-400';
  return 'border-yellow-300 hover:border-yellow-400';
};

const isUpcoming = (occ) => {
  if (!occ.heureNotifPrevue) return false;
  const now = new Date();
  const prevue = new Date(occ.heureNotifPrevue);
  return prevue > now;
};

const ouvrirPlan = (occ) => {
  selectedOcc.value = occ;
  operateurStore.isPollingPaused = true;
};

const fermerPlan = () => {
  selectedOcc.value = null;
  operateurStore.isPollingPaused = false;
};

const setLigneResultat = (occId, ligneId, res) => {
  if (forms.value[occId] && forms.value[occId][ligneId]) {
    forms.value[occId][ligneId].resultat = res;
  }
};

const setAllResults = (res) => {
  if (selectedOcc.value && selectedOcc.value.caracteristiques) {
    selectedOcc.value.caracteristiques.forEach(cara => {
      setLigneResultat(selectedOcc.value.id, cara.lignePlanId, res);
    });
  }
};

const getLigneResultat = (occId, ligneId) => {
  return forms.value[occId]?.[ligneId]?.resultat;
};

const isOccurrenceComplete = (occ) => {
  if (!occ.caracteristiques || occ.caracteristiques.length === 0) return true;
  return occ.caracteristiques.every(cara => {
    if (isPlanAssSection(cara.sectionLibelle)) return true;
    return forms.value[occ.id]?.[cara.lignePlanId]?.resultat != null;
  });
};

const raisonModalConfig = ref({
  show: false,
  texte: '',
  titre: '',
  description: '',
  action: null,
  payload: null
});

const confirmModalConfig = ref({
  show: false,
  titre: '',
  description: '',
  action: null,
  payload: null
});

const demanderConfirmation = (titre, description, actionCallback, payload = null) => {
  confirmModalConfig.value = {
    show: true,
    titre,
    description,
    action: actionCallback,
    payload
  };
};

const confirmerAction = () => {
  const { action, payload } = confirmModalConfig.value;
  confirmModalConfig.value.show = false;
  if (action) {
    action(payload);
  }
};

const demanderRaison = (titre, description, actionCallback, payload = null) => {
  raisonModalConfig.value = {
    show: true,
    texte: '',
    titre,
    description,
    action: actionCallback,
    payload
  };
};

const confirmerRaison = () => {
  const { action, texte, payload } = raisonModalConfig.value;
  raisonModalConfig.value.show = false;
  if (action) {
    action(texte, payload);
  }
};

const soumettreOccurrence = async (occ, forceResultat = null) => {
  if (forceResultat === 'IGNORE') {
    demanderRaison(
      'Ignorer le contrôle',
      `Veuillez saisir le motif pour ignorer le contrôle "${occ.titreCombine || 'Occurrence #' + occ.numeroOccurrence}" :`,
      async (raison, occurrence) => {
        await executerSoumissionOccurrence(occurrence, forceResultat, raison);
      },
      occ
    );
    return;
  }
  await executerSoumissionOccurrence(occ, forceResultat, null);
};

const executerSoumissionOccurrence = async (occ, forceResultat, raison) => {
  try {
    const lignesForms = Object.values(forms.value[occ.id] || {}).filter(l => l.resultat != null && l.resultat !== '');
    let resultatGlobal = forceResultat;

    if (!resultatGlobal) {
      const aDesNC = lignesForms.some(l => l.resultat === 'NC');
      resultatGlobal = aDesNC ? 'NC' : 'C';
    }

    const payload = {
      resultat: resultatGlobal,
      matriculeOperateur: authStore.user?.matricule || 'OP_INCONNU',
      raison: raison,
      lignes: forceResultat === 'REGLAGE' || forceResultat === 'IGNORE' ? [] : lignesForms,
      associatedIds: occ.associatedIds
    };

    await operateurService.repondreOccurrence(occ.id, payload);
    
    delete forms.value[occ.id];
    fermerPlan();
    fetchAlertes();
    emit('occurrence-submitted');
  } catch (error) {
    alert('Erreur: ' + (error.response?.data?.message || error.message));
  }
};

const ignorerTrancheAction = (trancheHoraire) => {
  demanderRaison(
    'Ignorer la tranche',
    `Voulez-vous vraiment ignorer tous les contrôles de la tranche ${formatTranche(trancheHoraire)} ? Veuillez en préciser la raison :`,
    async (raison, tranche) => {
      try {
        const matricule = authStore.user?.matricule || 'OP_INCONNU';
        await operateurService.ignorerTranche(props.execControleOfId, tranche, matricule, raison);
        fetchAlertes();
      } catch (error) {
        alert('Erreur: ' + (error.response?.data?.message || error.message));
      }
    },
    trancheHoraire
  );
};

const declarerTrancheReglageAction = (trancheHoraire) => {
  demanderConfirmation(
    'Déclarer comme Réglage',
    `Voulez-vous vraiment déclarer tous les contrôles de la tranche ${formatTranche(trancheHoraire)} comme Réglage ?`,
    async (tranche) => {
      try {
        const matricule = authStore.user?.matricule || 'OP_INCONNU';
        await operateurService.declarerTrancheReglage(props.execControleOfId, tranche, matricule);
        fetchAlertes();
      } catch (error) {
        alert('Erreur: ' + (error.response?.data?.message || error.message));
      }
    },
    trancheHoraire
  );
};

watch(() => raisonModalConfig.value.show, (newVal) => {
  if (newVal) operateurStore.isPollingPaused = true;
  else if (!selectedOcc.value) operateurStore.isPollingPaused = false;
});

watch(() => confirmModalConfig.value.show, (newVal) => {
  if (newVal) operateurStore.isPollingPaused = true;
  else if (!selectedOcc.value) operateurStore.isPollingPaused = false;
});
</script>
