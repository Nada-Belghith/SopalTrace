<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import apiClient from '@/services/apiClient'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'

const route = useRoute()
const router = useRouter()
const execControleOfId = route.params.execControleOfId

const echantillonnagePlan = ref(null)
const isLoading = ref(true)
const errorMessage = ref('')

onMounted(async () => {
  try {
    const response = await apiClient.get(`/ExecEchantillonnage/${execControleOfId}`)
    echantillonnagePlan.value = response.data
  } catch (error) {
    console.error("Erreur chargement echantillonnage:", error)
    errorMessage.value = "Erreur lors du chargement des données d'échantillonnage."
  } finally {
    isLoading.value = false
  }
})

const goBack = () => {
  router.back()
}

// Helpers for UI state
const isNiveauActive = (lvl) => {
  if (!echantillonnagePlan.value?.niveauControle) return false;
  return echantillonnagePlan.value.niveauControle.toUpperCase().includes(lvl);
}

const isPlanActive = (plan) => {
  if (!echantillonnagePlan.value?.typePlan) return false;
  return echantillonnagePlan.value.typePlan.toUpperCase() === plan;
}

const isModeActive = (mode) => {
  if (!echantillonnagePlan.value?.modeControle) return false;
  const m = echantillonnagePlan.value.modeControle.toUpperCase();
  if (mode === 'RÉDUIT') return m.includes('REDUIT') || m.includes('RÉDUIT');
  if (mode === 'RENFORCÉ') return m.includes('RENFORCE') || m.includes('RENFORCÉ');
  return m.includes(mode);
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
                <td class="py-5 px-2 border-r border-gray-200 text-blue-600">{{ echantillonnagePlan.lettreCode }}</td>
                <td class="py-5 px-2 border-r border-gray-200">{{ echantillonnagePlan.effectifEchantillonA }}</td>
                <td class="py-5 px-2 border-r border-gray-200">{{ echantillonnagePlan.nbPostesB }}</td>
                <td class="py-5 px-2 border-r border-gray-200">{{ echantillonnagePlan.effectifParPosteAb || '-' }}</td>
                <td class="py-5 px-2 border-r border-gray-200 text-[#059669] text-base">{{ echantillonnagePlan.critereAcceptationAc }}</td>
                <td class="py-5 px-2 text-[#dc2626] text-base">{{ echantillonnagePlan.critereRejetRe }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="mt-8 flex justify-end">
        <Button label="Imprimer la fiche" icon="pi pi-print" class="p-button-outlined mr-3" />
        <Button label="Commencer le contrôle" icon="pi pi-play" class="p-button-success shadow-lg" @click="goBack" />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Les styles additionnels ne sont plus nécessaires grâce à TailwindCSS */
</style>
