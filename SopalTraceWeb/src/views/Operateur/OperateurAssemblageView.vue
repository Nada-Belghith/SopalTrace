<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import apiClient from '@/services/apiClient'
import Swal from 'sweetalert2'
import alertesService from '@/services/alertesService'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Tooltip from 'primevue/tooltip'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import MultiSelect from 'primevue/multiselect'

import { useAppToast } from '@/composables/useAppToast'

// Nouveaux Composants
import OFGrid from '@/components/Operateur/Assemblage/OFGrid.vue'
import SessionHeader from '@/components/Operateur/Assemblage/SessionHeader.vue'
import DocumentCategories from '@/components/Operateur/Assemblage/DocumentCategories.vue'
import PostesSelectionDialog from '@/components/Operateur/Assemblage/Dialogs/PostesSelectionDialog.vue'
import VerifMachineDialog from '@/components/Operateur/Assemblage/Dialogs/VerifMachineDialog.vue'
import { useOperateurStore } from '@/stores/execution/operateurStore'

const vTooltip = Tooltip
const authStore = useAuthStore()
const operateurStore = useOperateurStore()
const router = useRouter()
const route = useRoute()
const toast = useAppToast()

// ───────────────────────────────── State ─────────────────────────────────
const ofsStatut = ref([])          
const postesDisponibles = ref([])  
const isLoading = ref(false)
const ofClique = ref(null)

// Dialog postes
const showPostesDialog = ref(false)
const isSubmitting = ref(false)

// Phase exécution (après démarrage / reprise)
const execOfIdActif = ref(null)
const ofActif = ref(null)
const selectedPoste = ref(null)
const sessionEquipe = ref(null)
const documents = ref([])
const isLoadingDocs = ref(false)

// Echantillonnage
const showNbPostesDialog = ref(false)
const nbPostesInput = ref(1)
const isInitializingEchantillonnage = ref(false)
const isCheckingPlan = ref(false)
const showListDialog = ref(false)
const selectedCategory = ref(null)
const isSignalingPlan = ref(false)

// Vérification Machine
const showVerifMachineDialog = ref(false)
const machinesPoste = ref([])
const allMachines = ref([])
const showPlanManquantDialog = ref(false)
const planManquantMachineCode = ref('')

// Instruments pour Échantillonnage
const allInstruments = ref([])
const selectedInstruments = ref([])

// ────────────────────────────── Init ──────────────────────────────────
onMounted(async () => {
  const savedEquipe = localStorage.getItem('sopal_sessionEquipe')
  if (savedEquipe) sessionEquipe.value = savedEquipe

  await Promise.all([fetchOfsStatut(), fetchPostes(), fetchInstruments()])
  
  if (route.query.execControleOfId) {
    const of = ofsStatut.value.find(o => o.execControleOfId === route.query.execControleOfId)
    if (of) {
      ofActif.value = of
      execOfIdActif.value = of.execControleOfId
      selectedPoste.value = route.query.posteCode || of.postesExistants?.[0]
      await fetchDocumentsStatus()
    }
  }
})

const fetchOfsStatut = async () => {
  isLoading.value = true
  try {
    const response = await apiClient.get('/Operateur/ofs/assemblage-statut')
    ofsStatut.value = response.data
  } catch (error) {
    console.error('Erreur chargement OFs assemblage:', error)
  } finally {
    isLoading.value = false
  }
}

const fetchPostes = async () => {
  try {
    const response = await apiClient.get('/Operateur/postes')
    postesDisponibles.value = response.data
  } catch (error) {
    console.error('Erreur chargement postes:', error)
  }
}

const fetchInstruments = async () => {
  try {
    const response = await apiClient.get('/referentiels/instruments')
    allInstruments.value = response.data.data
  } catch (error) {
    console.error('Erreur chargement instruments:', error)
  }
}

// ──────────────────────────── Click sur carte OF ──────────────────────
const onOfClick = (of) => {
  ofClique.value = of
  showPostesDialog.value = true
}

const postesExistants = computed(() => ofClique.value?.postesExistants ?? [])
const postesAAjouter = computed(() =>
  postesDisponibles.value.filter(p => !postesExistants.value.includes(p.codePoste))
)
const isDejaEnCours = computed(() => ofClique.value?.statut === 'EN_COURS')

// ────────────────────────── Démarrer ou Ajouter ───────────────────────
const confirmerPostes = async ({ postes: postesSelectionnes, equipe }) => {
  if (!ofClique.value) return

  if (!isDejaEnCours.value && postesSelectionnes.length === 0) {
    toast.warn('Attention', 'Veuillez sélectionner au moins un poste.')
    return
  }

  isSubmitting.value = true
  try {
    if (!isDejaEnCours.value) {
      const request = {
        numeroOf: ofClique.value.numeroOf,
        operationCode: 'ASS',
        posteCodes: postesSelectionnes,
        numEquipe: 1,
        matriculeOperateur: authStore.user?.matricule
      }
      const response = await apiClient.post('/Operateur/of/start-assemblage', request)
      execOfIdActif.value = response.data.id
    } else {
      execOfIdActif.value = ofClique.value.execControleOfId
      if (postesSelectionnes.length > 0) {
        await apiClient.post(
          `/Operateur/of/${execOfIdActif.value}/ajouter-postes`,
          postesSelectionnes
        )
      }
    }

    ofActif.value = ofClique.value
    sessionEquipe.value = equipe
    localStorage.setItem('sopal_sessionEquipe', equipe)
    showPostesDialog.value = false
    
    await fetchOfsStatut()
    
    if (tousLesPostesActifs.value.length > 0) {
      if (!selectedPoste.value || !tousLesPostesActifs.value.includes(selectedPoste.value)) {
        selectedPoste.value = tousLesPostesActifs.value[0]
      }
    }

    await fetchDocumentsStatus()
  } catch (error) {
    console.error('Erreur:', error)
    toast.error('Erreur', error.response?.data?.message || 'Erreur lors de l\'opération.')
  } finally {
    isSubmitting.value = false
  }
}

// ──────────────────────────── Documents ──────────────────────────────
const fetchDocumentsStatus = async () => {
  if (!execOfIdActif.value) return
  isLoadingDocs.value = true
  try {
    const response = await apiClient.get(`/Operateur/of/${execOfIdActif.value}/assemblage-documents`)
    documents.value = response.data
  } catch (error) {
    console.error('Erreur chargement documents:', error)
  } finally {
    isLoadingDocs.value = false
  }
}

const documentCategories = computed(() => {
  if (!selectedPoste.value) return []
  
  const posteDocs = documents.value.filter(d => d.posteCode === selectedPoste.value)
  const vmDocs = posteDocs.filter(d => d.typeDocument === 'VERIF_MACHINE')

  // Verif Machine est Terminé ssi CHAQUE machine configurée du poste a des docs et que TOUS ses docs sont terminés
  const isVmTermine = machinesPoste.value.length > 0 && machinesPoste.value.every(m => {
    const mDocs = vmDocs.filter(d => d.machineCode === m.codeMachine)
    return mDocs.length > 0 && mDocs.every(d => d.estTermine)
  })

  const cpDocs = posteDocs.filter(d => d.typeDocument === 'RESULTAT_CONTROLE_POSTE')
  const isCpTermine = cpDocs.length > 0 && cpDocs.every(d => d.estTermine)

  const echDocs = posteDocs.filter(d => d.typeDocument === 'ECHANTILLONNAGE')
  const isEchTermine = echDocs.length > 0 && echDocs.every(d => d.estTermine)

  return [
    { id: 'verifMachine',   title: 'Vérification Machine',     icon: 'pi pi-cog',        docs: vmDocs, isTermine: isVmTermine },
    { id: 'controlePoste',  title: 'Résultat Contrôle Poste',  icon: 'pi pi-check-square', docs: cpDocs, isTermine: isCpTermine },
    { id: 'echantillonnage',title: 'Fiche Échantillonnage',    icon: 'pi pi-chart-pie',  docs: echDocs, isTermine: isEchTermine },
    { id: 'planAss',        title: 'Plan Assemblage + Résultat',icon: 'pi pi-sitemap',   docs: [] },
    { id: 'tracabilite',    title: 'Registre de Traçabilité',  icon: 'pi pi-history',    docs: [] }
  ]
})

const onCategoryClick = async (cat) => {
  selectedCategory.value = cat
  if (cat.id === 'echantillonnage') {
    if (cat.docs.length > 0) {
      router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: selectedPoste.value } })
    } else {
      openEchantillonnageDialog()
    }
  } else if (cat.id === 'verifMachine') {
    openVerifMachineDialog()
  } else if (cat.id === 'planAss' || cat.id === 'tracabilite') {
    showListDialog.value = true
  } else {
    if (cat.docs.length === 0) {
      let typeDoc = ''
      if (cat.id === 'controlePoste') typeDoc = 'RESULTAT_CONTROLE_POSTE'
      if (typeDoc && execOfIdActif.value) {
        try {
          const pCode = selectedPoste.value || ''
          const equipe = sessionEquipe.value || ''
          const res = await apiClient.post(`/Operateur/of/${execOfIdActif.value}/assemblage-documents/init/${typeDoc}?posteCode=${pCode}&equipe=${equipe}`)
          if (res.data.initialized) {
            await fetchDocumentsStatus()
            const updatedCat = documentCategories.value.find(c => c.id === cat.id)
            if (updatedCat && updatedCat.docs.length > 0) {
              if (updatedCat.docs.length === 1) ouvrirDocument(updatedCat.docs[0])
              else showListDialog.value = true
              return
            }
          }
        } catch (e) { console.error('Erreur init doc', e) }
      }
    }
    if (cat.docs.length === 0) showListDialog.value = true
    else if (cat.docs.length === 1) ouvrirDocument(cat.docs[0])
    else showListDialog.value = true
  }
}

const ouvrirDocument = async (doc) => {
  if (showVerifMachineDialog.value) showVerifMachineDialog.value = false
  if (showListDialog.value) showListDialog.value = false
  
  if (doc.typeDocument === 'VERIF_MACHINE' && doc.machineCode) {
    try {
      const res = await apiClient.get(`/DocumentVerifMachine?machineCode=${doc.machineCode}`)
      const plans = res.data.data || res.data
      const activePlan = plans.find(p => p.statut === 'ACTIF')
      if (activePlan) {
        // Hydrate store so VerifMachineExec knows the context
        if (operateurStore) {
          operateurStore.setActiveOfContext({
            ...ofActif.value,
            machineCode: doc.machineCode,
            equipe: sessionEquipe.value,
            operationCode: 'ASS'
          })
        }
        
        // Point to the new Exec view instead of the Model Editor
        router.push({ 
          name: 'operateur-vm-exec', 
          params: { id: activePlan.id }, 
          query: { statutId: doc.id, mode: doc._openMode || 'details' } 
        })
      } else {
        toast.error('Erreur', `Aucun plan actif trouvé pour la machine ${doc.machineCode}.`)
      }
    } catch (e) {
      toast.error('Erreur', 'Impossible de récupérer le plan de vérification.')
    }
  } else {
    toast.info('Ouvrir document', `${doc.typeDocument} - ${doc.libelleFormulaire || 'Sans nom'}`)
  }
}

const marquerTermine = async (docId) => {
  try {
    await apiClient.put(`/Operateur/assemblage-documents/${docId}/terminer`)
    await fetchDocumentsStatus()
  } catch (error) { console.error('Erreur maj document:', error) }
}

const signalerPlanManquant = async (docName) => {
  isSignalingPlan.value = true
  try {
    const nomDocument = typeof docName === 'string' ? docName : 'Plan ou document'
    const data = {
      operationCode: 'ASS',
      posteCode: (ofActif.value?.postesExistants ?? []).join(', ') || 'Inconnu',
      articleCode: ofActif.value?.codeArticle || 'Inconnu',
      numeroOf: ofActif.value?.numeroOf,
      designationArticle: ofActif.value?.designationArticle,
      nomOperateur: authStore.user?.nom || authStore.user?.matricule || 'Opérateur Inconnu',
      descriptionProbleme: `Document manquant : ${nomDocument}.`
    }
    await alertesService.signalerPlanManquant(data)
    toast.success('Signalement envoyé', 'Signalement envoyé au Superviseur avec succès !')
    showListDialog.value = false
  } catch (error) {
    toast.error('Erreur', 'Erreur lors de l\'envoi du signalement.')
  } finally {
    isSignalingPlan.value = false
  }
}

// ──────────────────────────── Echantillonnage ──────────────────────────────
const openEchantillonnageDialog = async () => {
  if (!execOfIdActif.value) return
  isCheckingPlan.value = true
  try {
    const resSource = await apiClient.get(`/ExecEchantillonnage/check-source/${execOfIdActif.value}`)
    if (resSource.data?.exists) {
      nbPostesInput.value = (ofActif.value?.postesExistants?.length ?? 1)
      showNbPostesDialog.value = true
    } else {
      showListDialog.value = true
    }
  } catch (e) {
    toast.error('Erreur', 'Erreur lors de la vérification du plan source.')
  } finally {
    isCheckingPlan.value = false
  }
}

const initializeEchantillonnage = async () => {
  if (!execOfIdActif.value) return
  isInitializingEchantillonnage.value = true
  try {
    const pCode = selectedPoste.value || ''
    const payload = {
      posteCode: pCode,
      nbPostes: nbPostesInput.value,
      instrumentCodes: selectedInstruments.value,
      equipe: sessionEquipe.value
    }
    await apiClient.post(`/ExecEchantillonnage/init/${execOfIdActif.value}`, payload)
    showNbPostesDialog.value = false
    router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: pCode } })
  } catch (error) {
    toast.error('Erreur', 'Erreur lors de l\'initialisation : ' + (error.response?.data?.message || error.message))
  } finally {
    isInitializingEchantillonnage.value = false
  }
}

// ──────────────────────────── Vérification Machine ──────────────────────────────
const loadMachinesForPoste = async () => {
  if (!selectedPoste.value) return
  try {
    const res = await apiClient.get(`/Operateur/postes/${selectedPoste.value}/machines`)
    machinesPoste.value = res.data.map(m => ({ ...m, isDefault: true }))
    const resAll = await apiClient.get('/Operateur/machines')
    allMachines.value = resAll.data
  } catch (e) {
    console.error('Erreur chargement machines', e)
  }
}

const addMachineToPoste = (machineCode) => {
  if (machineCode && !machinesPoste.value.some(m => m.codeMachine === machineCode)) {
    const machine = allMachines.value.find(m => m.codeMachine === machineCode)
    if (machine) machinesPoste.value.push({ ...machine, isDefault: false })
  }
}

const removeMachineFromPoste = (machineCode) => {
  machinesPoste.value = machinesPoste.value.filter(m => m.codeMachine !== machineCode)
}

const openVerifMachineDialog = async () => {
  await loadMachinesForPoste()
  showVerifMachineDialog.value = true
}

const initMachineDocument = async (machineCode) => {
  if (!execOfIdActif.value || !selectedPoste.value) return
  try {
    const equipe = sessionEquipe.value || ''
    const res = await apiClient.post(`/Operateur/of/${execOfIdActif.value}/assemblage-documents/init/VERIF_MACHINE?posteCode=${selectedPoste.value}&machineCode=${machineCode}&equipe=${equipe}`)
    if (res.data.initialized) {
      await fetchDocumentsStatus()
      toast.success('Succès', 'Document initialisé avec succès.')
      
      const cat = documentCategories.value.find(c => c.id === 'verifMachine')
      if (cat) {
        // Find the newly created active document for this machine
        const doc = cat.docs.find(d => d.machineCode === machineCode && !d.estTermine)
        if (doc) {
          ouvrirDocument(doc)
        }
      }
    }
  } catch (e) {
    console.error('Erreur init machine doc', e)
    // Fermer la dialog de sélection de machine, et afficher le dialogue "plan manquant" style app
    showVerifMachineDialog.value = false
    planManquantMachineCode.value = machineCode
    showPlanManquantDialog.value = true
  }
}

const signalerPlanManquantMachine = async () => {
  isSignalingPlan.value = true
  try {
    await apiClient.post('/Alertes/plan-manquant', {
      operationCode: 'ASS',
      posteCode: selectedPoste.value,
      machineCode: planManquantMachineCode.value,
      numeroOf: ofActif.value?.numeroOf,
      articleCode: ofActif.value?.articleCode,
      designationArticle: ofActif.value?.designationArticle,
      descriptionProbleme: `Document de vérification machine introuvable ou inactif pour la machine ${planManquantMachineCode.value}`
    })
    toast.success('Signalé !', 'Une notification a été envoyée au superviseur.')
    showPlanManquantDialog.value = false
  } catch {
    toast.error('Erreur', 'Impossible d\'envoyer l\'alerte.')
  } finally {
    isSignalingPlan.value = false
  }
}

const cloturerDocumentsMachine = async (machineCode) => {
  if (!execOfIdActif.value) return
  try {
    await apiClient.put(`/Operateur/of/${execOfIdActif.value}/machine/${machineCode}/terminer-tout`)
    toast.success('Succès', `Tous les documents de la machine ${machineCode} ont été marqués comme Terminé.`)
    await fetchDocumentsStatus()
  } catch (error) {
    toast.error('Erreur', 'Impossible de clôturer les documents de la machine.')
  }
}

// ──────────────────────────── Utils ──────────────────────────────
const quitterSession = () => {
  execOfIdActif.value = null
  ofActif.value = null
  selectedPoste.value = null
  documents.value = []
  fetchOfsStatut()
}

const tousLesPostesActifs = computed(() => {
  if (!ofActif.value) return []
  const of = ofsStatut.value.find(o => o.numeroOf === ofActif.value.numeroOf)
  return of?.postesExistants ?? []
})
</script>

<template>
  <div class="p-6">
    <div class="mb-6">
      <h2 class="text-3xl font-bold text-primary mb-1 flex items-center">
        <i class="pi pi-wrench mr-3 text-3xl"></i> Exécution Assemblage (Produits Finis)
      </h2>
      <p class="text-gray-500">Sélectionnez un OF pour configurer vos postes de travail et remplir les documents associés.</p>
    </div>

    <!-- ──────────────── PHASE 1 : Grille des cartes OF ──────────────── -->
    <div v-if="!execOfIdActif">
      <OFGrid 
        :ofsStatut="ofsStatut" 
        :isLoading="isLoading" 
        @select-of="onOfClick" 
      />
    </div>

    <!-- ──────────────── PHASE 2 : Exécution ──────────────── -->
    <div v-if="execOfIdActif" class="flex flex-col gap-6">

      <!-- Bandeau session -->
      <SessionHeader 
        :ofActif="ofActif" 
        :tousLesPostesActifs="tousLesPostesActifs" 
        :equipe="sessionEquipe"
        @quitter-session="quitterSession" 
      />

      <!-- Cartes documents -->
      <DocumentCategories 
        v-model:selectedPoste="selectedPoste" 
        :tousLesPostesActifs="tousLesPostesActifs" 
        :documentCategories="documentCategories" 
        :isLoadingDocs="isLoadingDocs" 
        @refresh="fetchDocumentsStatus" 
        @category-click="onCategoryClick" 
      />
    </div>

    <!-- ──────────────── Dialog : Sélection / Ajout de postes ──────────────── -->
    <PostesSelectionDialog 
      v-model:visible="showPostesDialog"
      :ofClique="ofClique"
      :postesExistants="postesExistants"
      :postesAAjouter="postesAAjouter"
      :isDejaEnCours="isDejaEnCours"
      :isSubmitting="isSubmitting"
      @confirmer="confirmerPostes"
    />

    <!-- Dialog générique (liste vide / plan manquant) -->
    <Dialog v-model:visible="showListDialog" modal :header="selectedCategory?.title" :style="{ width: '50vw' }" :breakpoints="{ '960px': '75vw', '641px': '100vw' }">
      <div v-if="selectedCategory" class="pt-4">
        <div v-if="selectedCategory.id === 'planAss' || selectedCategory.id === 'tracabilite'" class="bg-blue-50 p-8 rounded-xl text-center border border-blue-100">
          <i :class="selectedCategory.icon" class="text-5xl text-blue-300 mb-4"></i>
          <h4 class="text-xl font-bold text-blue-900 mb-2">Module en développement</h4>
          <p class="text-blue-700">L'interface spécifique pour accéder au {{ selectedCategory.title.toLowerCase() }} n'a pas encore été raccordée.</p>
        </div>
        <div v-else>
          <div v-if="selectedCategory.docs.length === 0" class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300">
            <i class="pi pi-info-circle text-5xl text-gray-400 mb-4"></i>
            <p class="text-gray-600 text-lg">Aucun document de ce type n'est paramétré pour les postes sélectionnés.</p>
            <Button label="Signaler au Superviseur" icon="pi pi-exclamation-triangle" severity="danger" outlined class="mt-6" @click="signalerPlanManquant(selectedCategory.title)" :loading="isSignalingPlan" />
          </div>
          <div v-else>
            <p class="mb-5 text-gray-600">Veuillez sélectionner le formulaire à ouvrir :</p>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
              <div v-for="doc in selectedCategory.docs" :key="doc.id" class="border rounded-xl p-5 shadow-sm" :class="doc.estTermine ? 'border-green-200 bg-green-50' : 'border-orange-200 bg-white'">
                <div class="mb-4">
                  <Tag v-if="doc.posteCode" :value="'Poste ' + doc.posteCode" severity="info" class="mb-2" />
                  <h4 class="font-bold text-gray-800">{{ doc.libelleFormulaire || 'Formulaire Sans Nom' }}</h4>
                </div>
                <div class="flex justify-between items-center mt-4 pt-4 border-t" :class="doc.estTermine ? 'border-green-200' : 'border-gray-100'">
                  <span :class="doc.estTermine ? 'text-green-600' : 'text-orange-600'" class="text-sm font-medium">
                    <i :class="doc.estTermine ? 'pi pi-check mr-1' : 'pi pi-clock mr-1'"></i>
                    {{ doc.estTermine ? 'Terminé' : 'À Remplir' }}
                  </span>
                  <div class="flex gap-2">
                    <Button v-if="!doc.estTermine" icon="pi pi-check" severity="success" outlined size="small" @click="marquerTermine(doc.id)" v-tooltip.top="'Marquer terminé'" />
                    <Button :label="doc.estTermine ? 'Consulter' : 'Ouvrir'" :icon="doc.estTermine ? 'pi pi-eye' : 'pi pi-pencil'" :severity="doc.estTermine ? 'secondary' : 'primary'" size="small" @click="ouvrirDocument(doc)" />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Dialog>

    <!-- Dialog Échantillonnage NbPostes -->
    <Dialog v-model:visible="showNbPostesDialog" modal header="Fiche Échantillonnage" :style="{ width: '400px' }" :closable="!isInitializingEchantillonnage">
      <div class="flex flex-col gap-4 mt-2">
        <p class="text-gray-600">Nombre de postes utilisé pour calculer l'effectif par poste.</p>
        <div class="flex flex-col gap-2">
          <label class="font-semibold text-gray-800">Nombre de postes</label>
          <InputNumber v-model="nbPostesInput" :min="1" :max="50" showButtons buttonLayout="horizontal"
            decrementButtonClass="p-button-secondary" incrementButtonClass="p-button-secondary"
            incrementButtonIcon="pi pi-plus" decrementButtonIcon="pi pi-minus" class="w-full" />
        </div>
        <div class="flex flex-col gap-2">
          <label class="font-semibold text-gray-800">Instruments de mesure</label>
          <MultiSelect v-model="selectedInstruments" :options="allInstruments" optionLabel="designation" optionValue="codeInstrument" placeholder="Sélectionner des instruments" :maxSelectedLabels="3" class="w-full" filter />
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-2 mt-4">
          <Button label="Annuler" icon="pi pi-times" text severity="secondary" @click="showNbPostesDialog = false" :disabled="isInitializingEchantillonnage" />
          <Button label="Générer Fiche" icon="pi pi-check" @click="initializeEchantillonnage" :loading="isInitializingEchantillonnage" />
        </div>
      </template>
    </Dialog>

    <!-- Dialog Plan Manquant Vérif Machine (style cohérent avec l'app) -->
    <Dialog v-model:visible="showPlanManquantDialog" modal header="Vérification Machine" :style="{ width: '480px' }" :closable="!isSignalingPlan">
      <div class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300 m-2">
        <i class="pi pi-info-circle text-5xl text-gray-400 mb-4" style="display:block"></i>
        <p class="text-gray-600 text-lg mb-2">Aucun plan de vérification machine n'est paramétré pour <strong>{{ planManquantMachineCode }}</strong>.</p>
        <p class="text-gray-500 text-sm mb-6">Contactez votre superviseur pour qu'il crée ou active le document associé à cette machine.</p>
        <Button 
          label="Signaler au Superviseur" 
          icon="pi pi-exclamation-triangle" 
          severity="danger" 
          outlined 
          :loading="isSignalingPlan"
          @click="signalerPlanManquantMachine" 
        />
      </div>
    </Dialog>

    <!-- Dialog Vérification Machine -->
    <VerifMachineDialog 
      v-model:visible="showVerifMachineDialog" 
      :machinesPoste="machinesPoste" 
      :allMachines="allMachines"
      :documentCategories="documentCategories" 
      :sessionEquipe="sessionEquipe"
      @ouvrir-document="ouvrirDocument" 
      @init-document="initMachineDocument" 
      @add-machine="addMachineToPoste"
      @remove-machine="removeMachineFromPoste"
      @cloturer-machine="cloturerDocumentsMachine"
    />
  </div>
</template>
