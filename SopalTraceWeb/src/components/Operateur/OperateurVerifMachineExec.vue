<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import execAssemblageService from '@/services/execAssemblageService'
import { useAppToast } from '@/composables/useAppToast'
import Button from 'primevue/button'
import { idsDuMoment } from '@/utils/periodicitesMachine'

import { useVerifMachineStore } from '@/stores/verifMachineStore'
import { useOperateurStore } from '@/stores/execution/operateurStore'
import VerifMachineTableConformite from '@/components/VerifMachine/partials/VerifMachineTableConformite.vue'
import VerifMachineTableRisques from '@/components/VerifMachine/partials/VerifMachineTableRisques.vue'

const router = useRouter()
const route = useRoute()
const toast = useAppToast()
const store = useVerifMachineStore()
const operateurStore = useOperateurStore()

// Risques normaux et risques étalon en deux tableaux : configuration de la machine (table Machine)
const tablesEtalonSeparees = computed(() => !!store.getMachineConfig(store.entete?.machineCode || docHeader.value?.machineCode).tablesEtalonSeparees)

const planId = route.params.id
const currentStatutId = ref(route.query.statutId)

/**
 * Ouvert depuis les vérifications machine d'un poste d'assemblage : moment imposé (démarrage, après la pause,
 * fin de poste), pas de galerie, et une validation qui enregistre puis marque le moment comme fait.
 */
const modeAssistant = route.query.assistant === '1'
const periodiciteAssistant = route.query.periodicite || ''
const LIBELLES_MOMENT = { DEMARRAGE: 'Démarrage', APRES_PAUSE: 'Après la pause', FIN_POSTE: 'Fin de poste' }
const libelleMoment = LIBELLES_MOMENT[periodiciteAssistant] || periodiciteAssistant
const isValidating = ref(false)

const viewMode = ref(route.query.mode === 'all' && !modeAssistant ? 'gallery' : 'details')
const isConsultationMode = ref(route.query.mode === 'all' || route.query.consultation === '1')
const loading = ref(false)
const execReponses = ref([])
const availableSessions = ref([])
const selectedPeriodiciteId = ref(null)

const docHeader = ref({
  codeArticle: '-',
  designationArticle: '-',
  numeroOf: '-',
  equipe: '-',
  machineCode: '-',
  dateExecution: '-'
})

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('fr-FR')
}

const isSessionTerminee = (s) => {
  if (s.estTermine) return true
  if (s.dateExecution) {
    const todayStr = new Date().toISOString().split('T')[0]
    const docDateStr = s.dateExecution.split('T')[0]
    if (docDateStr < todayStr) return true
  }
  return false
}

const selectedFilterDate = ref('')
const selectedFilterEquipe = ref('')

const availableDates = computed(() => {
  const map = new Map()
  availableSessions.value.forEach(s => {
    if (s.dateExecution) {
      const rawDateStr = s.dateExecution.split('T')[0]
      if (!map.has(rawDateStr)) {
        map.set(rawDateStr, formatDate(s.dateExecution))
      }
    }
  })
  return Array.from(map.entries()).map(([raw, formatted]) => ({ raw, formatted }))
})

const availableEquipes = computed(() => {
  const equipes = new Set()
  availableSessions.value.forEach(s => {
    if (s.equipe) equipes.add(s.equipe)
  })
  return Array.from(equipes).sort()
})

const filteredSessions = computed(() => {
  return availableSessions.value.filter(s => {
    const matchDate = !selectedFilterDate.value || (s.dateExecution && s.dateExecution.split('T')[0] === selectedFilterDate.value)
    const matchEquipe = !selectedFilterEquipe.value || (s.equipe === selectedFilterEquipe.value)
    return matchDate && matchEquipe
  })
})

const fetchSessionData = async (targetStatutId) => {
  loading.value = true
  try {
    const res = await execAssemblageService.getVerifMachine(targetStatutId)
    if (res.data) {
      currentStatutId.value = targetStatutId

      if (res.data.reponses) {
        execReponses.value = res.data.reponses
      }

      if (res.data.availableSessions) {
        availableSessions.value = res.data.availableSessions
      }

      const rawDate = res.data.dateExecution || (execReponses.value.length > 0 ? execReponses.value[0].dateExecution : null)
      if (rawDate) {
        selectedFilterDate.value = rawDate.split('T')[0]
      }

      docHeader.value = {
        codeArticle: res.data.codeArticle || operateurStore.activeOfContext?.codeArticle || operateurStore.activeOfContext?.articleCode || '-',
        designationArticle: res.data.designationArticle || operateurStore.activeOfContext?.designationArticle || '-',
        numeroOf: res.data.numeroOf || operateurStore.activeOfContext?.numeroOf || operateurStore.activeOfContext?.numeroOF || '-',
        equipe: res.data.equipe || operateurStore.activeOfContext?.equipe || operateurStore.activeOfContext?.numEquipe || '-',
        machineCode: res.data.machineCode || operateurStore.activeOfContext?.machineCode || '-',
        dateExecution: formatDate(rawDate)
      }

      applyPeriodiciteFilter()
    }
  } catch {
    toast.error('Erreur', 'Impossible de charger la session.')
  } finally {
    loading.value = false
  }
}

/** Assistant : périodicités du moment imposé (code ControlMoment), affichées dans les tableaux. */
const idsMoment = computed(() => (modeAssistant ? idsDuMoment(store.periodicitesMachine, periodiciteAssistant) : null))

/** Fabrication : filtre initial d'après l'URL (identifiant, code ou moment), sinon le démarrage pour une première saisie. */
const applyPeriodiciteFilter = () => {
  // Assistant : le moment est imposé par idsMoment, jamais déduit d'un libellé ni des réponses.
  if (modeAssistant) return
  const periodicites = store.periodicitesMachine || []

  const cible = route.query.periodicite || route.query.perio || route.query.etape
  if (cible) {
    const valeur = String(cible)
    const trouvee = periodicites.find(p => p.id === valeur || p.code === valeur || p.controlMoment === valeur)
    if (trouvee) {
      selectedPeriodiciteId.value = trouvee.id
      toast.info('Information', `Filtré sur la périodicité "${trouvee.libelle}".`)
      return
    }
  }

  if (execReponses.value.length === 0 && availableSessions.value.length > 0) {
    const teamSessions = availableSessions.value.filter(s => s.equipe === docHeader.value.equipe)
    const demarrage = periodicites.find(p => p.controlMoment === 'DEMARRAGE')
    if (teamSessions.length <= 1 && demarrage) {
      selectedPeriodiciteId.value = demarrage.id
      toast.info('Information', 'Filtré sur la périodicité "Démarrage" pour la première exécution de l\'équipe.')
    }
  }
}

const checkConsultationMode = (statutId) => {
  // Assistant : le mode lecture est choisi par l'écran des machines (consultation ou saisie).
  if (modeAssistant) return
  const session = availableSessions.value.find(s => s.statutId === statutId)
  if (session && isSessionTerminee(session)) {
    isConsultationMode.value = true
  } else {
    isConsultationMode.value = false
  }
}

onMounted(async () => {
  loading.value = true
  try {
    await store.fetchDictionnaires()
    await store.loadDocumentById(planId)
    await fetchSessionData(currentStatutId.value)
    applyPeriodiciteFilter()
    checkConsultationMode(currentStatutId.value)
  } catch {
    toast.error('Erreur', 'Impossible de charger les données.')
  } finally {
    loading.value = false
  }
})



const selectCardSession = async (statutIdToLoad) => {
  await fetchSessionData(statutIdToLoad)
  checkConsultationMode(statutIdToLoad)
  viewMode.value = 'details'
}

const switchToGalleryMode = () => {
  viewMode.value = 'gallery'
}

const save = async () => {
  try {
    // L'opérateur est identifié par le serveur à partir de son jeton.
    await execAssemblageService.saveVerifMachine({
      execControleDocumentStatutId: currentStatutId.value,
      reponses: execReponses.value
    })
    toast.success('Succès', 'Vérification machine enregistrée avec succès.')
    
    // Mettre à jour les données pour rafraîchir la liste des sessions
    await fetchSessionData(currentStatutId.value)
    
    // Retour automatique à la vue galerie (les cartes)
    switchToGalleryMode()
  } catch (error) {
    toast.error('Erreur', error.message || 'Impossible de sauvegarder.')
  }
}

/** Assistant : retour à la liste des machines du poste pour ce moment. */
const retourMachines = () => router.push({
  name: 'exec-verif-machines',
  params: { execControleOfId: route.query.execControleOfId },
  query: { posteCode: route.query.posteCode, periodicite: periodiciteAssistant }
})

const enregistrerSansValider = async () => {
  try {
    await execAssemblageService.saveVerifMachine({ execControleDocumentStatutId: currentStatutId.value, reponses: execReponses.value })
    toast.success('Enregistré', 'Réponses enregistrées : la vérification reste à valider.')
    await fetchSessionData(currentStatutId.value)
  } catch (error) {
    toast.error('Erreur', error.message || "Impossible d'enregistrer.")
  }
}

const valider = async () => {
  isValidating.value = true
  try {
    await execAssemblageService.validerVerifMachine(currentStatutId.value, periodiciteAssistant, execReponses.value)
    toast.success('Vérification validée', `${docHeader.value.machineCode} — ${libelleMoment}`)
    retourMachines()
  } catch (error) {
    toast.error('Validation impossible', error.message)
  } finally {
    isValidating.value = false
  }
}

const goBack = () => {
  const execControleOfId = route.query.execControleOfId 
    || operateurStore.activeOfContext?.execControleOfId 
    || operateurStore.activeOfContext?.id 
    || route.params.id

  const posteCode = route.query.posteCode 
    || operateurStore.activeOfContext?.posteCode 
    || docHeader.value.posteCode

  const opCode = operateurStore.activeOfContext?.operationCode
  if (opCode === 'ASS' || !opCode) {
    router.push({
      name: 'operateur-of-fini',
      query: { execControleOfId, posteCode }
    })
  } else {
    router.push({
      name: 'operateur-of-semi-fini',
      query: { execControleOfId, posteCode }
    })
  }
}

const titre = computed(() => {
  if (modeAssistant) return `Vérification machine ${docHeader.value.machineCode} · ${libelleMoment}`
  return viewMode.value === 'gallery' ? "Galerie des rapports d'exécution" : 'Exécution : Vérification Machine'
})

const handleBackArrow = () => {
  if (modeAssistant) {
    retourMachines()
    return
  }
  if (viewMode.value === 'details') {
    switchToGalleryMode()
  } else {
    goBack()
  }
}

</script>

<template>
  <div class="p-4 max-w-[1400px] mx-auto">
    <div class="flex items-center justify-between gap-3 mb-6">
      <div class="flex items-center gap-3">
        <Button icon="pi pi-arrow-left" class="p-button-rounded p-button-text p-button-secondary" @click="handleBackArrow" />
        <h2 class="m-0 text-xl font-bold text-slate-800">
          {{ titre }}
        </h2>
      </div>

      <div class="flex items-center gap-3">
        <!-- Mode Toggle Button -->
        <button 
          v-if="!modeAssistant && availableSessions.length > 0 && viewMode === 'details'"
          @click="switchToGalleryMode"
          class="px-4 py-2 rounded-xl text-xs font-bold bg-white text-blue-600 border border-slate-300 shadow-sm hover:bg-blue-50 hover:border-blue-300 transition-all flex items-center gap-2"
        >
          <i class="pi pi-th-large"></i>
          <span>Voir toutes les cartes ({{ availableSessions.length }})</span>
        </button>

        <!-- Modifier Button -->
        <button 
          v-if="!modeAssistant && viewMode === 'details' && isConsultationMode"
          @click="isConsultationMode = false"
          class="px-4 py-2 rounded-xl text-xs font-bold bg-amber-500 text-white shadow-sm hover:bg-amber-600 transition-all flex items-center gap-2"
        >
          <i class="pi pi-pencil"></i>
          <span>Modifier</span>
        </button>
      </div>
    </div>

    <!-- ──────────────── MODE 1 : GALERIE DE CARTES ──────────────── -->
    <div v-if="viewMode === 'gallery'" class="space-y-6">
      <!-- Bandeau Filtres -->
      <div class="bg-white p-5 rounded-2xl shadow-sm border border-slate-200">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center font-bold">
              <i class="pi pi-th-large text-lg"></i>
            </div>
            <div>
              <h3 class="text-base font-bold text-slate-800 m-0">Rapports d'exécution ({{ availableSessions.length }})</h3>
              <p class="text-xs text-slate-500 m-0">Cliquez sur une carte pour ouvrir, consulter et modifier le rapport d'exécution.</p>
            </div>
          </div>

          <div class="flex flex-wrap items-center gap-4">
            <!-- Filtre 1: Date -->
            <div class="flex items-center gap-2">
              <label class="text-xs font-bold text-slate-600 uppercase tracking-wide">Date :</label>
              <select 
                v-model="selectedFilterDate" 
                class="bg-slate-50 border border-slate-300 text-slate-800 text-xs font-bold rounded-lg p-2.5 focus:ring-2 focus:ring-blue-100 focus:border-blue-500 outline-none shadow-sm cursor-pointer min-w-[160px]"
              >
                <option value="">Toutes les dates</option>
                <option v-for="d in availableDates" :key="d.raw" :value="d.raw">
                  📅 {{ d.formatted }}
                </option>
              </select>
            </div>

            <!-- Filtre 2: Équipe -->
            <div class="flex items-center gap-2">
              <label class="text-xs font-bold text-slate-600 uppercase tracking-wide">Équipe :</label>
              <select 
                v-model="selectedFilterEquipe" 
                class="bg-slate-50 border border-slate-300 text-slate-800 text-xs font-bold rounded-lg p-2.5 focus:ring-2 focus:ring-blue-100 focus:border-blue-500 outline-none shadow-sm cursor-pointer min-w-[150px]"
              >
                <option value="">Toutes les équipes</option>
                <option v-for="eq in availableEquipes" :key="eq" :value="eq">
                  Équipe {{ eq }}
                </option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <!-- Grille de cartes -->
      <div v-if="loading" class="text-center p-12">
        <i class="pi pi-spin pi-spinner text-blue-500" style="font-size: 2.5rem"></i>
        <p class="mt-3 text-slate-500 font-medium">Chargement des rapports...</p>
      </div>

      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        <div 
          v-for="s in filteredSessions" 
          :key="s.statutId"
          @click="selectCardSession(s.statutId)"
          class="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm hover:shadow-md hover:border-blue-400 transition-all cursor-pointer flex flex-col justify-between group relative overflow-hidden"
          :class="{ 'ring-2 ring-blue-500 border-blue-500': currentStatutId === s.statutId }"
        >
          <!-- Badge Statut & Équipe -->
          <div class="flex justify-between items-center mb-4">
            <span class="px-2.5 py-1 rounded-full text-[10px] font-extrabold uppercase tracking-wider flex items-center gap-1"
                  :class="isSessionTerminee(s) ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-amber-50 text-amber-700 border border-amber-200'">
              <i :class="isSessionTerminee(s) ? 'pi pi-check-circle' : 'pi pi-clock'"></i>
              {{ isSessionTerminee(s) ? 'Terminé' : 'En Cours' }}
            </span>
            
            <span class="text-xs font-black text-blue-700 bg-blue-50 border border-blue-200 px-3 py-1 rounded-lg">
              Équipe {{ s.equipe || '1' }}
            </span>
          </div>

          <!-- Infos du rapport -->
          <div class="space-y-2 mb-4">
            <div class="flex items-center text-slate-800 font-bold text-sm">
              <i class="pi pi-calendar mr-2 text-blue-500"></i>
              <span>Date : {{ formatDate(s.dateExecution) }}</span>
            </div>

            <div v-if="s.dateTermine" class="flex items-center text-emerald-700 text-xs font-semibold">
              <i class="pi pi-check-circle mr-2 text-emerald-500"></i>
              <span>Terminé le : {{ formatDate(s.dateTermine) }}</span>
            </div>

            <div class="flex items-center text-slate-500 text-xs">
              <i class="pi pi-user mr-2 text-slate-400"></i>
              <span>Matricule Opérateur : <strong class="text-slate-700">{{ s.matriculeOperateur || 'Non renseigné' }}</strong></span>
            </div>
          </div>

          <!-- Action -->
          <div class="pt-3 border-t border-slate-100 flex justify-between items-center text-xs font-bold text-blue-600 group-hover:text-blue-700">
            <span>Ouvrir & Consulter le rapport</span>
            <i class="pi pi-arrow-right transform group-hover:translate-x-1 transition-transform"></i>
          </div>
        </div>

        <div v-if="filteredSessions.length === 0" class="col-span-full text-center py-12 bg-white rounded-2xl border border-slate-200">
          <i class="pi pi-search text-3xl text-slate-300 mb-2"></i>
          <p class="text-slate-500 font-bold text-sm">Aucun rapport ne correspond à votre filtre (Date / Équipe).</p>
        </div>
      </div>
    </div>

    <!-- ──────────────── MODE 2 : DÉTAILS ET ÉDITION DU RAPPORT ──────────────── -->
    <div v-else-if="viewMode === 'details'">
      <!-- En-tête OF (Code Article, N°OF, Equipe, Date...) -->
      <div class="mb-4 bg-white rounded-xl shadow-sm border border-slate-300 overflow-hidden">
        <div class="bg-slate-100 px-4 py-2 border-b border-slate-300 flex justify-between items-center">
          <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">Rapport de vérification machine</span>
          <span class="text-xs font-bold text-slate-500 uppercase tracking-wider">Date d'exécution : <span class="text-slate-700 font-black">{{ docHeader.dateExecution }}</span></span>
        </div>
        <div class="p-4 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Code Article</p>
            <p class="text-sm font-bold text-blue-700">{{ docHeader.codeArticle }}</p>
          </div>
          <div class="lg:col-span-2">
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Désignation article</p>
            <p class="text-sm font-bold text-slate-800">{{ docHeader.designationArticle }}</p>
          </div>
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">N° OF</p>
            <p class="text-sm font-bold text-slate-800">{{ docHeader.numeroOf }}</p>
          </div>
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Équipe Active</p>
            <p class="text-sm font-bold text-slate-800">{{ docHeader.equipe }}</p>
          </div>
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Machine</p>
            <p class="text-sm font-bold text-slate-800">{{ docHeader.machineCode }}</p>
          </div>
        </div>
      </div>



    <div v-if="loading" class="text-center p-8">
      <i class="pi pi-spin pi-spinner text-emerald-500" style="font-size: 3rem"></i>
      <p class="mt-4 text-slate-500 font-medium">Chargement du plan de vérification...</p>
    </div>
    
    <div v-else>
      <div v-if="modeAssistant" class="mb-4 rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-900 font-semibold">
        <i class="pi pi-filter mr-2"></i>Points à vérifier : {{ libelleMoment }}
        <span v-if="idsMoment && !idsMoment.length" class="block mt-1 text-red-700">Aucune périodicité de ce plan n'est rattachée à ce moment : signalez-le au superviseur.</span>
        <span v-if="isConsultationMode" class="ml-2 text-blue-700 font-normal">(consultation)</span>
      </div>
      <div v-else class="mb-4 bg-white p-4 rounded-xl shadow-sm border border-slate-200">
        <label class="block font-bold mb-3 text-slate-700">Filtrer par Périodicité :</label>
        <div class="flex flex-wrap gap-3">
          <button 
            type="button"
            @click="selectedPeriodiciteId = null"
            class="px-5 py-2.5 rounded-lg text-sm font-bold transition-all border"
            :class="!selectedPeriodiciteId ? 'bg-blue-600 text-white border-blue-600 shadow-md ring-2 ring-blue-200 ring-offset-1' : 'bg-white text-slate-600 border-slate-300 hover:bg-slate-50 hover:border-slate-400 hover:shadow-sm'"
          >
            Toutes les périodicités
          </button>
          
          <button 
            v-for="perio in store.periodicitesMachine" 
            :key="perio.id"
            type="button"
            @click="selectedPeriodiciteId = selectedPeriodiciteId === perio.id ? null : perio.id"
            class="px-5 py-2.5 rounded-lg text-sm font-bold transition-all border"
            :class="selectedPeriodiciteId === perio.id ? 'bg-blue-600 text-white border-blue-600 shadow-md ring-2 ring-blue-200 ring-offset-1' : 'bg-white text-slate-600 border-slate-300 hover:bg-slate-50 hover:border-slate-400 hover:shadow-sm'"
          >
            {{ perio.libelle }}
          </button>
        </div>
      </div>

      <!-- SECTION CONFORMITÉ -->
      <div v-if="store.lignesConformite.length > 0" class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden mb-6">
        <div class="overflow-x-auto w-full">
          <VerifMachineTableConformite 
            :isReadOnly="true"
            :isExecution="true"
            :isConsultationMode="isConsultationMode"
            :execReponses="execReponses"
            :selectedPeriodiciteId="selectedPeriodiciteId"
              :selectedPeriodiciteIds="idsMoment"
            :showEtalonColumns="false"
          />
        </div>
      </div>

      <!-- SECTION RISQUES & DÉFAUTS -->
      <template v-if="tablesEtalonSeparees">
        <div class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden mb-6">
          <div class="overflow-x-auto w-full">
            <VerifMachineTableRisques 
              :isReadOnly="true"
              :isExecution="true"
              :isConsultationMode="isConsultationMode"
              :execReponses="execReponses"
              :selectedPeriodiciteId="selectedPeriodiciteId"
              :selectedPeriodiciteIds="idsMoment"
              :showEtalonColumns="false"
              filterType="normal"
              title="Section Risques & Défauts"
            />
          </div>
        </div>

        <div class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden mb-6">
          <div class="overflow-x-auto w-full">
            <VerifMachineTableRisques 
              :isReadOnly="true"
              :isExecution="true"
              :isConsultationMode="isConsultationMode"
              :execReponses="execReponses"
              :selectedPeriodiciteId="selectedPeriodiciteId"
              :selectedPeriodiciteIds="idsMoment"
              :showEtalonColumns="true"
              filterType="etalon"
              title="Section Contrôle d'Étanchéité / Fuite Étalon & Pression"
            />
          </div>
        </div>
      </template>

      <template v-else-if="store.lignesRisques.length > 0">
        <div class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden mb-6">
          <div class="overflow-x-auto w-full">
            <VerifMachineTableRisques 
              :isReadOnly="true"
              :isExecution="true"
              :isConsultationMode="isConsultationMode"
              :execReponses="execReponses"
              :selectedPeriodiciteId="selectedPeriodiciteId"
              :selectedPeriodiciteIds="idsMoment"
              filterType="all"
            />
          </div>
        </div>
      </template>
      
      <div v-if="modeAssistant && !isConsultationMode" class="bg-slate-50 border border-slate-200 p-4 flex flex-col md:flex-row justify-end gap-3 rounded-xl shadow-sm">
        <button type="button" :disabled="isValidating" @click="enregistrerSansValider"
                class="min-h-[52px] px-5 rounded-xl bg-white border border-slate-300 text-slate-700 font-bold cursor-pointer hover:bg-slate-50 disabled:opacity-50">
          <i class="pi pi-save mr-2"></i>Enregistrer sans valider
        </button>
        <button type="button" :disabled="isValidating" @click="valider"
                class="min-h-[52px] px-6 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-black border-0 cursor-pointer disabled:opacity-50">
          <i class="pi mr-2" :class="isValidating ? 'pi-spin pi-spinner' : 'pi-check-circle'"></i>Valider la vérification — {{ libelleMoment }}
        </button>
      </div>
      <div v-else-if="!isConsultationMode" class="bg-slate-50 border border-slate-200 p-6 flex justify-end rounded-xl shadow-sm">
        <Button label="Enregistrer la vérification" icon="pi pi-save" @click="save" class="p-button-success shadow-md" />
      </div>
    </div>
    </div>
  </div>
</template>

