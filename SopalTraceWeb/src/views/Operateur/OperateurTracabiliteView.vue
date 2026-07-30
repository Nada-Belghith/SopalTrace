<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from '@/services/apiClient'
import Swal from 'sweetalert2'
import Button from 'primevue/button'
import Dropdown from 'primevue/dropdown'
import InputText from 'primevue/inputtext'
import AutoComplete from 'primevue/autocomplete'
import { useAppToast } from '@/composables/useAppToast'

const router = useRouter()
const route = useRoute()
const toast = useAppToast()

const execControleOfId = ref(route.params.execControleOfId)
const posteCode = ref(route.query.posteCode)

const isLoading = ref(false)
const isSubmitting = ref(false)

// Data from backend
const historique = ref([])
const composantsDisponibles = ref([])
const estTermine = ref(false)

// Header info
const ofInfo = ref(null)

// Current input row
const newRow = ref({
  corps: '',
  volant: '',
  composantsSelectionnes: {}
})

// Validation real-time
const validiteLots = ref({
  corps: null,
  volant: null
})

let timeoutCorps = null
let timeoutVolant = null

const validateLot = async (type, lotValue) => {
  if (!lotValue?.trim()) {
    validiteLots.value[type.toLowerCase()] = null
    return
  }
  
  try {
    const res = await apiClient.get(`/Operateur/tracabilite/validate-lot?type=${type}&lot=${encodeURIComponent(lotValue)}`)
    validiteLots.value[type.toLowerCase()] = res.data?.isValid === true
  } catch(e) {
    validiteLots.value[type.toLowerCase()] = false
  }
}

const onCorpsInput = () => {
  validiteLots.value.corps = null
  clearTimeout(timeoutCorps)
  timeoutCorps = setTimeout(() => validateLot('CORPS', newRow.value.corps), 500)
}

const onVolantInput = () => {
  validiteLots.value.volant = null
  clearTimeout(timeoutVolant)
  timeoutVolant = setTimeout(() => validateLot('VOLANT', newRow.value.volant), 500)
}

// Autocomplete state
const filteredCorpsLots = ref([])
const filteredVolantLots = ref([])

const searchCorpsLots = async (event) => {
  try {
    const res = await apiClient.get(`/Operateur/tracabilite/search-lots?type=CORPS&query=${encodeURIComponent(event.query)}`)
    filteredCorpsLots.value = res.data?.data || []
  } catch(e) {
    console.error(e)
  }
}

const searchVolantLots = async (event) => {
  try {
    const res = await apiClient.get(`/Operateur/tracabilite/search-lots?type=VOLANT&query=${encodeURIComponent(event.query)}`)
    filteredVolantLots.value = res.data?.data || []
  } catch(e) {
    console.error(e)
  }
}

onMounted(async () => {
  await fetchOfInfo()
  await fetchRegistre()
})

const fetchOfInfo = async () => {
  try {
    const res = await apiClient.get(`/Operateur/ofs/assemblage-statut`)
    const ofs = res.data
    const found = ofs.find(o => o.execControleOfId?.toString().toLowerCase() === execControleOfId.value?.toString().toLowerCase())
    if (found) {
      ofInfo.value = found
    }
  } catch (e) {
    console.error('Erreur chargement OF Info', e)
  }
}

const fetchRegistre = async () => {
  isLoading.value = true
  try {
    const res = await apiClient.get(`/Operateur/tracabilite/${execControleOfId.value}`)
    if (res.data?.success) {
      historique.value = res.data.data.lignesHistorique || []
      composantsDisponibles.value = res.data.data.composantsDisponibles || []
      estTermine.value = res.data.data.estTermine || false
      
      // Init new row
      initNewRow()
    }
  } catch (e) {
    toast.error('Erreur', 'Impossible de charger le registre de traçabilité.')
    console.error(e)
  } finally {
    isLoading.value = false
  }
}

const initNewRow = () => {
  // Try to inherit from last row if exists
  const lastRow = historique.value.length > 0 ? historique.value[historique.value.length - 1] : null
  
  if (lastRow) {
    newRow.value.corps = lastRow.corps || ''
    newRow.value.volant = lastRow.volant || ''
    newRow.value.composantsSelectionnes = {}
    
    // Pour chaque composant disponible, pre-sélectionner la valeur précédente
    for (const comp of composantsDisponibles.value) {
      const prevComp = lastRow.composants?.find(c => c.designationComposant === comp.designationComposant)
      if (prevComp) {
        newRow.value.composantsSelectionnes[comp.designationComposant] = prevComp.lotSelectionne
      } else if (comp.lotsDisponibles?.length === 1) {
        newRow.value.composantsSelectionnes[comp.designationComposant] = comp.lotsDisponibles[0]
      }
    }
  } else {
    // No previous row
    newRow.value.corps = ''
    newRow.value.volant = ''
    newRow.value.composantsSelectionnes = {}
    
    // Auto select if only one lot
    for (const comp of composantsDisponibles.value) {
      if (comp.lotsDisponibles?.length === 1) {
        newRow.value.composantsSelectionnes[comp.designationComposant] = comp.lotsDisponibles[0]
      }
    }
  }
}

const formatDateTime = (dateStr) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  return date.toLocaleString('fr-FR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  }).replace(',', '')
}

const addLigne = async () => {
  if (!newRow.value.corps?.trim() || !newRow.value.volant?.trim()) {
    toast.error('Erreur', 'Veuillez saisir le Corps et le Volant.')
    return
  }

  if (validiteLots.value.corps === false || validiteLots.value.volant === false) {
    toast.error('Erreur', 'Les numéros de lot pour le Corps ou le Volant sont invalides.')
    return
  }
  
  for (const comp of composantsDisponibles.value) {
    if (!newRow.value.composantsSelectionnes[comp.designationComposant]) {
      toast.error('Erreur', `Veuillez sélectionner un lot pour le composant : ${comp.designationComposant}.`)
      return
    }
  }

  isSubmitting.value = true
  try {
    const composantsArray = []
    for (const key in newRow.value.composantsSelectionnes) {
      composantsArray.push({
        designationComposant: key,
        lotSelectionne: newRow.value.composantsSelectionnes[key]
      })
    }

    const payload = {
      execControleOfId: execControleOfId.value,
      corps: newRow.value.corps,
      volant: newRow.value.volant,
      composants: composantsArray
    }

    const res = await apiClient.post(`/Operateur/tracabilite/ligne`, payload)
    
    if (res.data?.success) {
      toast.success('Succès', 'Ligne ajoutée au registre.')
      historique.value.push(res.data.data)
      validiteLots.value = { corps: null, volant: null }
      initNewRow()
    }
  } catch (error) {
    toast.error('Erreur', 'Impossible d\'ajouter la ligne.')
    console.error(error)
  } finally {
    isSubmitting.value = false
  }
}

const goBack = () => {
  router.push({
    name: 'operateur-of-fini',
    query: {
      execControleOfId: execControleOfId.value,
      posteCode: posteCode.value
    }
  })
}

// Fonction utilitaire pour récupérer le lot d'un composant dans l'historique
const getLotHistorique = (ligne, designation) => {
  const comp = ligne.composants?.find(c => c.designationComposant === designation)
  return comp ? comp.lotSelectionne : ''
}

const terminerDocument = async () => {
  try {
    const res = await apiClient.post(`/Operateur/tracabilite/${execControleOfId.value}/cloturer?posteCode=${posteCode.value}`)
    if (res.data?.success) {
      toast.success('Succès', 'Document marqué comme terminé.')
      estTermine.value = true
      goBack()
    }
  } catch (e) {
    toast.error('Erreur', 'Impossible de clôturer le document.')
    console.error(e)
  }
}

const supprimerLigne = async (ligneId) => {
  if (confirm("Voulez-vous vraiment supprimer cette ligne d'assemblage ?")) {
    try {
      const res = await apiClient.delete(`/Operateur/tracabilite/ligne/${ligneId}`)
      if (res.data?.success) {
        toast.success('Succès', 'Ligne supprimée avec succès.')
        historique.value = historique.value.filter(l => l.id !== ligneId)
      }
    } catch (e) {
      toast.error('Erreur', 'Impossible de supprimer la ligne.')
      console.error(e)
    }
  }
}

const enregistrerProgres = () => {
  // Les lignes sont déjà enregistrées une par une avec le bouton +, on fait juste un retour visuel
  toast.success('Enregistré', 'Vos modifications ont été conservées.')
  goBack()
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 flex flex-col p-4 md:p-6 font-sans">
    
    <!-- En-tête -->
    <div class="mb-4 bg-[#0B1536] text-white rounded-xl shadow-lg border border-slate-700 overflow-hidden relative flex flex-col sm:flex-row sm:items-center justify-between p-4 sm:p-5">
      <div class="absolute inset-0 opacity-10 bg-gradient-to-r from-blue-500 to-indigo-600 pointer-events-none"></div>
      
      <div class="relative z-10 flex flex-col">
        <div class="flex items-center gap-3">
          <Button icon="pi pi-arrow-left" class="p-button-rounded p-button-text p-button-sm text-white hover:bg-white/10" @click="goBack" />
          <h1 class="text-2xl font-bold tracking-tight">
            OF: {{ ofInfo?.numeroOf || 'Chargement...' }}
            <span class="text-blue-300 ml-2 text-xl">(ASS)</span>
          </h1>
        </div>
        <div class="mt-1 ml-11 text-blue-200 font-medium text-sm">
          {{ ofInfo?.codeArticle }} — {{ ofInfo?.designationArticle }}
        </div>
      </div>
      
      <div class="relative z-10 mt-3 sm:mt-0 text-left sm:text-right flex flex-col justify-end h-full">
        <div class="text-xs text-blue-300 font-semibold uppercase tracking-wider mb-1">
          POSTE : {{ posteCode || ofInfo?.postesExistants?.[0] || 'N/A' }}
        </div>
        <div class="inline-flex items-center gap-2">
          <span class="relative flex h-3 w-3">
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span class="relative inline-flex rounded-full h-3 w-3 bg-emerald-500"></span>
          </span>
          <span class="text-sm font-bold text-emerald-400 uppercase">EN PRODUCTION</span>
        </div>
      </div>
    </div>

    <!-- Contenu Principal -->
    <div class="flex-1 bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden flex flex-col">
      
      <!-- Titre de la carte -->
      <div class="bg-[#0B1536] text-white p-4 flex items-center gap-3">
        <i class="pi pi-list text-white text-xl"></i>
        <h2 class="text-white font-bold text-lg">REGISTRE DE TRAÇABILITÉ</h2>
      </div>

      <div class="p-6 overflow-x-auto">
        <div v-if="isLoading" class="flex justify-center p-8">
          <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
        </div>

        <table v-else class="w-full text-sm text-left border-collapse min-w-max">
          <thead class="bg-[#1e293b] text-white">
            <tr>
              <th class="p-4 rounded-tl-lg font-semibold tracking-wider text-xs uppercase">DATE / HEURE</th>
              <th class="p-4 font-semibold tracking-wider text-xs uppercase text-center">VER.</th>
              <th class="p-4 font-semibold tracking-wider text-xs uppercase">CORPS</th>
              <th class="p-4 font-semibold tracking-wider text-xs uppercase">VOLANT</th>
              <th v-for="comp in composantsDisponibles" :key="comp.designationComposant" class="p-4 font-semibold tracking-wider text-xs uppercase">
                {{ comp.designationComposant }}
              </th>
              <th class="p-4 rounded-tr-lg font-semibold tracking-wider text-xs uppercase text-center w-24">ACTION</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <!-- Historique -->
            <tr v-for="ligne in historique" :key="ligne.id" class="hover:bg-gray-50 transition-colors">
              <td class="p-4 text-gray-700">{{ formatDateTime(ligne.dateHeure) }}</td>
              <td class="p-4 text-center">
                <span class="bg-purple-100 text-purple-800 text-xs font-bold px-2.5 py-1 rounded-md">
                  V{{ ligne.version }}
                </span>
              </td>
              <td class="p-4 font-medium text-emerald-600">{{ ligne.corps || '-' }}</td>
              <td class="p-4 font-medium text-blue-600">{{ ligne.volant || '-' }}</td>
              <td v-for="comp in composantsDisponibles" :key="comp.designationComposant" class="p-4 text-gray-600">
                {{ getLotHistorique(ligne, comp.designationComposant) || '-' }}
              </td>
              <td class="p-4 text-center">
                <Button 
                  v-if="!estTermine"
                  icon="pi pi-trash" 
                  class="p-button-rounded p-button-text p-button-danger hover:bg-red-50 w-8 h-8"
                  @click="supprimerLigne(ligne.id)"
                  title="Supprimer la ligne"
                />
                <i v-else class="pi pi-check-circle text-emerald-500 text-xl"></i>
              </td>
            </tr>

            <!-- Ligne de saisie -->
            <tr v-if="!estTermine" class="bg-blue-50/30">
              <td class="p-4">
                <div class="text-gray-400 font-medium text-xs uppercase">NOUVELLE<br>VERSION</div>
              </td>
              <td class="p-4 text-center">
                <span class="text-gray-400 font-bold uppercase text-xs">AUTO</span>
              </td>
              <td class="p-4">
                <AutoComplete 
                  v-model="newRow.corps" 
                  :suggestions="filteredCorpsLots"
                  @complete="searchCorpsLots"
                  @item-select="onCorpsInput"
                  @blur="onCorpsInput"
                  @input="onCorpsInput"
                  placeholder="SAISIR CORPS..." 
                  :inputClass="[
                    'w-32 bg-white transition-colors duration-300',
                    validiteLots.corps === true ? 'border-emerald-500 ring-1 ring-emerald-500' : 
                    validiteLots.corps === false ? 'border-red-500 ring-1 ring-red-500' : 
                    'border-amber-300 focus:border-amber-500'
                  ]"
                />
              </td>
              <td class="p-4">
                <AutoComplete 
                  v-model="newRow.volant" 
                  :suggestions="filteredVolantLots"
                  @complete="searchVolantLots"
                  @item-select="onVolantInput"
                  @blur="onVolantInput"
                  @input="onVolantInput"
                  placeholder="SAISIR VOLANT..." 
                  :inputClass="[
                    'w-36 bg-white transition-colors duration-300',
                    validiteLots.volant === true ? 'border-emerald-500 ring-1 ring-emerald-500' : 
                    validiteLots.volant === false ? 'border-red-500 ring-1 ring-red-500' : 
                    'border-amber-300 focus:border-amber-500'
                  ]"
                />
              </td>
              
              <!-- Composants Scannés -->
              <td v-for="comp in composantsDisponibles" :key="comp.designationComposant" class="p-4">
                <div v-if="comp.lotsDisponibles.length === 0" class="text-gray-400 italic text-xs">
                  Aucun lot
                </div>
                <div v-else-if="comp.lotsDisponibles.length === 1">
                  <!-- Affichage statique si un seul lot -->
                  <div class="bg-gray-50 border border-gray-200 px-3 py-2 rounded-md text-gray-600 min-w-[100px]">
                    {{ comp.lotsDisponibles[0] }}
                  </div>
                </div>
                <div v-else>
                  <!-- Dropdown si plusieurs lots -->
                  <Dropdown 
                    v-model="newRow.composantsSelectionnes[comp.designationComposant]" 
                    :options="comp.lotsDisponibles" 
                    placeholder="Sélectionner..." 
                    class="w-32 md:w-40" 
                  />
                </div>
              </td>

              <!-- Action + -->
              <td class="p-4 text-center">
                <Button 
                  icon="pi pi-plus" 
                  class="p-button-rounded bg-[#0f172a] hover:bg-[#1e293b] border-none text-white w-10 h-10" 
                  @click="addLigne"
                  :loading="isSubmitting"
                />
              </td>
            </tr>
          </tbody>
        </table>
        
        <div v-if="!isLoading && composantsDisponibles.length === 0" class="text-center p-6 text-gray-500 bg-gray-50 mt-4 rounded-lg border border-dashed border-gray-300">
          <i class="pi pi-info-circle text-2xl mb-2 text-gray-400"></i>
          <p>Aucun composant préparé par le magasinier n'a été trouvé pour cet OF.</p>
        </div>

      </div>

      <!-- Actions de fin -->
      <div class="bg-gray-50 p-4 border-t border-gray-200 flex justify-end gap-3 rounded-b-xl">
        <Button 
          label="Enregistrer" 
          icon="pi pi-save" 
          class="p-button-outlined p-button-secondary"
          @click="enregistrerProgres"
        />
        <Button 
          v-if="!estTermine"
          label="Marquer comme terminé" 
          icon="pi pi-check" 
          class="bg-emerald-600 hover:bg-emerald-700 border-none text-white font-semibold shadow-sm"
          @click="terminerDocument"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Personnalisation légère pour matcher le design mockup */
.p-dropdown {
  border-color: #e2e8f0;
}
.p-inputtext {
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
}
</style>
