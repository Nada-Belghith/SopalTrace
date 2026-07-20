<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import apiClient from '@/services/apiClient'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'

const route = useRoute()
const router = useRouter()
const execControleOfId = route.params.execControleOfId

const posteCode = route.query.posteCode || ''

const echantillonnagePlan = ref(null)
const isLoading = ref(true)
const errorMessage = ref('')

onMounted(async () => {
  try {
    const response = await apiClient.get(`/ExecEchantillonnage/${execControleOfId}?posteCode=${posteCode}`)
    echantillonnagePlan.value = response.data
  } catch (error) {
    console.error("Erreur chargement:", error)
    errorMessage.value = "Erreur lors du chargement des données."
  } finally {
    isLoading.value = false
  }
})

const valider = async () => {
  if (echantillonnagePlan.value) {
    try {
      await apiClient.put(`/ExecEchantillonnage/${echantillonnagePlan.value.id}`, echantillonnagePlan.value)
      if (echantillonnagePlan.value.execControleDocumentStatutId) {
        await apiClient.put(`/Operateur/assemblage-documents/${echantillonnagePlan.value.execControleDocumentStatutId}/terminer`)
      }
      router.push({ name: 'operateur-of-fini', query: { execControleOfId: execControleOfId, posteCode: posteCode } })
    } catch (error) {
      console.error('Erreur lors de la validation:', error)
      errorMessage.value = "Erreur lors de la validation."
    }
  }
}

const goBack = () => {
  router.push({ name: 'operateur-of-fini', query: { execControleOfId: execControleOfId, posteCode: posteCode } })
}

// Helpers for UI state
const isNiveauActive = (lvl) => {
  if (!echantillonnagePlan.value?.niveauControle) return false;
  return echantillonnagePlan.value.niveauControle.toUpperCase() === lvl;
}

const isPlanActive = (plan) => {
  if (!echantillonnagePlan.value?.typePlan) return false;
  return echantillonnagePlan.value.typePlan.toUpperCase() === plan;
}

const isModeActive = (mode) => {
  if (!echantillonnagePlan.value?.modeControle) return false;
  const m = echantillonnagePlan.value.modeControle.toUpperCase();
  if (mode === 'RÉDUIT') return m === 'REDUIT' || m === 'RÉDUIT';
  if (mode === 'RENFORCÉ') return m === 'RENFORCE' || m === 'RENFORCÉ';
  return m === mode;
}

</script>

<template>
  <div class="p-6 max-w-7xl mx-auto font-sans">
    <div class="flex items-center gap-4 mb-6">
      <Button icon="pi pi-arrow-left" class="p-button-rounded p-button-text" @click="goBack" />
      <div>
        <h1 class="text-3xl font-bold text-gray-800">Fiche d'Échantillonnage (ISO 2859)</h1>
        <p class="text-gray-500">Plan de contrôle dynamique généré pour l'OF</p>
      </div>
    </div>

    <div v-if="isLoading" class="flex justify-center p-12">
      <ProgressSpinner />
    </div>

    <div v-else-if="errorMessage" class="bg-red-50 text-red-600 p-4 rounded-lg flex items-center gap-3">
      <i class="pi pi-exclamation-triangle text-2xl"></i>
      <p>{{ errorMessage }}</p>
    </div>

    <div v-else-if="echantillonnagePlan" class="space-y-8 mt-6">
      
      <!-- EN-TÊTE DU DOCUMENT -->
      <div class="border rounded-md overflow-hidden shadow-sm bg-white">
        <!-- Header -->
        <div class="bg-[#1f2937] text-white px-5 py-3 flex items-center gap-2 font-bold uppercase tracking-wide text-[13px]">
          <i class="pi pi-file-o text-green-400"></i> Informations Générales
        </div>
        <!-- Body -->
        <div class="p-6 grid grid-cols-1 md:grid-cols-3 gap-6 items-center text-sm">
          <div>
            <p><span class="font-bold text-gray-700">Code article :</span> {{ echantillonnagePlan.codeArticle || '-' }}</p>
            <p class="mt-3"><span class="font-bold text-gray-700">Atelier :</span> {{ echantillonnagePlan.atelier || '-' }}</p>
            <p class="mt-3 flex items-center gap-2">
              <span class="font-bold text-gray-700 whitespace-nowrap">Poste / Machine :</span> 
              <InputText v-model="echantillonnagePlan.posteCode" placeholder="Poste" class="w-20" />
              <span class="text-gray-400">/</span>
              <InputText v-model="echantillonnagePlan.codeMachine" placeholder="Machine" class="w-20" />
            </p>
          </div>
          <div>
            <p><span class="font-bold text-gray-700">Désignation :</span> <span class="text-blue-600 font-medium">{{ echantillonnagePlan.designation || '-' }}</span></p>
            <p class="mt-3"><span class="font-bold text-gray-700">Date de fabrication :</span> 
              <input type="date" :disabled="echantillonnagePlan?.estTermine" :value="echantillonnagePlan.dateFabrication ? echantillonnagePlan.dateFabrication.split('T')[0] : ''" @input="echantillonnagePlan.dateFabrication = $event.target.value" class="border border-gray-300 rounded px-2 py-1 text-sm mt-1 w-full focus:outline-none focus:border-blue-500 disabled:bg-gray-100 disabled:text-gray-500" />
            </p>
            <p class="mt-3"><span class="font-bold text-gray-700">Code instrument de mesure :</span> 
              <span class="text-blue-600">{{ echantillonnagePlan.instrumentCodes?.join('  ') || '-' }}</span>
            </p>
          </div>
          <div>
            <p><span class="font-bold text-gray-700">Numéro de l'OF :</span> <span class="text-blue-600">{{ echantillonnagePlan.numeroOf || '-' }}</span></p>
            <p class="mt-3"><span class="font-bold text-gray-700">Date de l'échantillonnage :</span> 
              <input type="datetime-local" :disabled="echantillonnagePlan?.estTermine" :value="echantillonnagePlan.dateEchantillonnage ? echantillonnagePlan.dateEchantillonnage.substring(0, 16) : ''" @input="echantillonnagePlan.dateEchantillonnage = $event.target.value" class="border border-gray-300 rounded px-2 py-1 text-sm mt-1 w-full focus:outline-none focus:border-blue-500 disabled:bg-gray-100 disabled:text-gray-500" />
            </p>
          </div>
        </div>
      </div>

      <!-- PARAMÈTRES DU PLAN -->
      <div class="border rounded-md overflow-hidden shadow-sm bg-white">
        <!-- Header -->
        <div class="bg-[#1f2937] text-white px-5 py-3 flex items-center gap-2 font-bold uppercase tracking-wide text-[13px]">
          <i class="pi pi-sliders-h text-green-400"></i> Paramètres du plan
        </div>
        <!-- Body -->
        <div class="p-6 grid grid-cols-1 md:grid-cols-4 gap-8 items-end">
          
          <!-- Niveau de contrôle -->
          <div>
            <p class="text-xs font-bold text-gray-600 uppercase tracking-wide mb-3">Niveau de contrôle</p>
            <div class="flex gap-1 bg-gray-50 p-1 rounded border border-gray-200">
              <div v-for="lvl in ['I', 'II', 'III']" :key="lvl" 
                   class="flex-1 text-center py-2 text-[11px] font-bold rounded transition-colors"
                   :class="isNiveauActive(lvl) ? 'bg-[#059669] text-white shadow-sm' : 'text-gray-400 bg-transparent'">
                NIVEAU {{ lvl }}
              </div>
            </div>
          </div>

          <!-- Plan d'échantillonnage -->
          <div>
            <p class="text-xs font-bold text-gray-600 uppercase tracking-wide mb-3">Plan d'échantillonnage</p>
            <div class="flex gap-1 bg-gray-50 p-1 rounded border border-gray-200">
              <div v-for="plan in ['SIMPLE', 'DOUBLE']" :key="plan" 
                   class="flex-1 text-center py-2 text-[11px] font-bold rounded transition-colors"
                   :class="isPlanActive(plan) ? 'bg-[#059669] text-white shadow-sm' : 'text-gray-400 bg-transparent'">
                {{ plan }}
              </div>
            </div>
          </div>

          <!-- Mode de contrôle -->
          <div>
            <p class="text-xs font-bold text-gray-600 uppercase tracking-wide mb-3">Mode de contrôle</p>
            <div class="flex gap-1 bg-gray-50 p-1 rounded border border-gray-200">
              <div v-for="mode in ['RÉDUIT', 'NORMAL', 'RENFORCÉ']" :key="mode" 
                   class="flex-1 text-center py-2 text-[11px] font-bold rounded transition-colors"
                   :class="isModeActive(mode) ? 'bg-[#059669] text-white shadow-sm' : 'text-gray-400 bg-transparent'">
                {{ mode }}
              </div>
            </div>
          </div>

          <!-- NQA -->
          <div>
            <p class="text-xs font-bold text-gray-600 uppercase tracking-wide mb-3">NQA</p>
            <div class="flex items-center justify-center py-[7px] text-[13px] font-bold border border-green-200 rounded text-gray-700 bg-white shadow-sm w-[120px]">
              {{ echantillonnagePlan.nqaValeur ?? '-' }}
            </div>
          </div>

        </div>
      </div>

      <!-- TABLEAU DE DÉTERMINATION -->
      <div class="border rounded-md overflow-hidden shadow-sm bg-white">
        <!-- Header -->
        <div class="bg-[#1f2937] text-white px-5 py-3 flex items-center gap-2 font-bold uppercase tracking-wide text-[13px]">
          <i class="pi pi-table text-green-400"></i> Tableau de détermination de l'échantillon
        </div>
        <!-- Table -->
        <div class="overflow-x-auto">
          <table class="w-full text-center border-collapse">
            <thead>
              <tr class="bg-[#f8fafc] text-[11px] text-gray-600 font-bold uppercase tracking-wider border-b border-gray-200">
                <th class="py-4 px-2 border-r border-gray-200 w-16">N°</th>
                <th class="py-4 px-2 border-r border-gray-200">Effectif du lot<br><span class="text-gray-400 font-normal">(Tranche)</span></th>
                <th class="py-4 px-2 border-r border-gray-200">Lettre<br>Code</th>
                <th class="py-4 px-2 border-r border-gray-200">Échantillon global (A)</th>
                <th class="py-4 px-2 border-r border-gray-200">Nb postes (B)</th>
                <th class="py-4 px-2 border-r border-gray-200">Échantillon /poste (A/B)</th>
                <th class="py-4 px-2 border-r border-gray-200 text-[#059669]">Critère d'acceptation</th>
                <th class="py-4 px-2 text-[#dc2626]">Critère de rejet</th>
              </tr>
            </thead>
            <tbody class="text-[13px] font-bold text-gray-700">
              <tr>
                <td class="py-5 px-2 border-r border-gray-200 text-gray-400">1</td>
                <td class="py-5 px-2 border-r border-gray-200">{{ echantillonnagePlan.tailleLot }}</td>
                <td class="py-2 px-2 border-r border-gray-200"><InputText v-model="echantillonnagePlan.lettreCode" :disabled="echantillonnagePlan?.estTermine" class="w-16 text-center" /></td>
                <td class="py-2 px-2 border-r border-gray-200"><InputNumber v-model="echantillonnagePlan.effectifEchantillonA" :disabled="echantillonnagePlan?.estTermine" class="w-20" inputClass="text-center w-full" /></td>
                <td class="py-2 px-2 border-r border-gray-200"><InputNumber v-model="echantillonnagePlan.nbPostesB" :disabled="echantillonnagePlan?.estTermine" class="w-16" inputClass="text-center w-full" /></td>
                <td class="py-2 px-2 border-r border-gray-200"><InputNumber v-model="echantillonnagePlan.effectifParPosteAb" :disabled="echantillonnagePlan?.estTermine" class="w-20" inputClass="text-center w-full" /></td>
                <td class="py-2 px-2 border-r border-gray-200">
                  <span class="font-bold text-[#059669] text-base">
                    {{ echantillonnagePlan.critereAcceptationAc }}
                  </span>
                </td>
                <td class="py-2 px-2">
                  <span class="font-bold text-[#dc2626] text-base">
                    {{ echantillonnagePlan.critereRejetRe }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="mt-8 flex justify-end">
        <Button label="Imprimer la fiche" icon="pi pi-print" class="p-button-outlined mr-3" />
        <Button v-if="!echantillonnagePlan?.estTermine" label="Valider" icon="pi pi-check" class="p-button-success shadow-lg" @click="valider" />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Les styles additionnels ne sont plus nécessaires grâce à TailwindCSS */
</style>
