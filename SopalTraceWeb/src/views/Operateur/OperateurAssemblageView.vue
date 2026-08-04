<script setup>
import { ref, onMounted, computed, watch } from 'vue'
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
import DocumentCategories from '@/components/Operateur/Assemblage/DocumentCategories.vue'
import AssistantDemarrage from '@/components/Operateur/Assemblage/AssistantDemarrage.vue'
import PostesSelectionDialog from '@/components/Operateur/Assemblage/Dialogs/PostesSelectionDialog.vue'
import VerifMachineDialog from '@/components/Operateur/Assemblage/Dialogs/VerifMachineDialog.vue'
import { useOperateurStore } from '@/stores/execution/operateurStore'
import operateurService from '@/services/operateurService'

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

// Contrôle au Poste - Plan Manquant
const showdocumentControlePosteManquantDialog = ref(false)
const isSignalingdocumentControlePoste = ref(false)

// Assemblage & Résultat en cours - Plan Manquant
const showPlanAssManquantDialog = ref(false)
const isSignalingPlanAss = ref(false)
const docAssManquantDesc = ref('')
const docAssMessageErreur = ref('')

// Instruments pour Échantillonnage
const allInstruments = ref([])
const selectedInstruments = ref([])

// ────────────────────────────── Init ──────────────────────────────────
onMounted(async () => {
  const savedEquipe = localStorage.getItem('sopal_sessionEquipe')
  if (savedEquipe) sessionEquipe.value = savedEquipe

  await Promise.all([fetchOfsStatut(), fetchPostes(), fetchInstruments()])

  const targetExecId = route.query.execControleOfId
  if (targetExecId) {
    execOfIdActif.value = targetExecId
    const of = ofsStatut.value.find(o => 
      o.execControleOfId && o.execControleOfId.toString().toLowerCase() === targetExecId.toString().toLowerCase()
    )
    if (of) {
      ofActif.value = of
      if (!selectedPoste.value) {
        selectedPoste.value = route.query.posteCode || of.postesExistants?.[0]
      }
    } else if (route.query.posteCode) {
      selectedPoste.value = route.query.posteCode
    }
    await fetchDocumentsStatus()
  }
})

watch([() => route.query.execControleOfId, () => route.query.posteCode, () => route.fullPath], async () => {
  if (route.query.execControleOfId) {
    execOfIdActif.value = route.query.execControleOfId
    const of = ofsStatut.value.find(o => 
      o.execControleOfId && o.execControleOfId.toString().toLowerCase() === route.query.execControleOfId.toString().toLowerCase()
    )
    if (of) ofActif.value = of
    if (route.query.posteCode) selectedPoste.value = route.query.posteCode
    await fetchDocumentsStatus()
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

    router.replace({ query: { execControleOfId: execOfIdActif.value, posteCode: selectedPoste.value } })

    await fetchDocumentsStatus()
  } catch (error) {
    console.error('Erreur:', error)
    toast.error('Erreur', error.response?.data?.message || 'Erreur lors de l\'opération.')
  } finally {
    isSubmitting.value = false
  }
}

const alertesActives = ref([])

const fetchDocumentsStatus = async () => {
  if (!execOfIdActif.value) return
  isLoadingDocs.value = true
  try {
    const response = await apiClient.get(`/Operateur/of/${execOfIdActif.value}/assemblage-documents`)
    documents.value = response.data
    
    try {
      const responseAlertes = await apiClient.get(`/Operateur/of/${execOfIdActif.value}/alertes-actives`)
      alertesActives.value = responseAlertes.data || []
    } catch (e) {
      console.warn('Erreur chargement alertes-actives', e)
      alertesActives.value = []
    }

    await loadMachinesForPoste()
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

  // Verif Machine : Démarrage vs Clôture Finale
  const isVmForceTermine = posteDocs.some(d => d.typeDocument === 'CLOTURE_VM_POSTE')
  const isVmDemarrageTermine = isVmForceTermine || (
    isLoadingDocs.value ? false : (
      machinesPoste.value.length > 0 && machinesPoste.value.every(m => {
        const mDocs = vmDocs.filter(d => d.machineCode === m.codeMachine)
        return mDocs.length > 0 && mDocs.every(d => d.estDemarrageTermine || d.estTermine)
      })
    )
  )
  const isVmFinalTermine = isVmForceTermine || (
    isLoadingDocs.value ? false : (
      machinesPoste.value.length > 0 && machinesPoste.value.every(m => {
        const mDocs = vmDocs.filter(d => d.machineCode === m.codeMachine)
        return mDocs.length > 0 && mDocs.every(d => d.estTermine)
      })
    )
  )

  const cpDocs = posteDocs.filter(d => d.typeDocument === 'RESULTAT_CONTROLE_POSTE')
  const isCpTermine = posteDocs.some(d => d.typeDocument === 'CLOTURE_RC_POSTE')

  const echDocs = posteDocs.filter(d => d.typeDocument === 'ECHANTILLONNAGE')
  const isEchTermine = echDocs.length > 0 && echDocs.every(d => d.estTermine)

  const planAssDocs = posteDocs.filter(d => ['PLAN_ASS', 'PLAN_ASSEMBLAGE', 'PLAN_FAB', 'MODELE_FAB', 'RESULTAT_CF', 'RCCF'].includes(d.typeDocument))
  const isPlanAssDemarrageTermine = planAssDocs.length > 0 && planAssDocs.some(d => d.estDemarrageTermine || d.estTermine || d.repondu)
  const isPlanAssFinalTermine = planAssDocs.length > 0 && planAssDocs.every(d => d.estTermine)

  const tracaDocs = posteDocs.filter(d => d.typeDocument === 'REGISTRE_TRACA')
  const isTracaTermine = tracaDocs.length > 0 && tracaDocs.every(d => d.estTermine)

  return [
    { id: 'verifMachine',   title: 'Vérification Machine',     icon: 'pi pi-cog',        docs: vmDocs, isDemarrageTermine: isVmDemarrageTermine, isTermine: isVmFinalTermine },
    { id: 'documentControlePoste',  title: 'Résultat Contrôle Poste',  icon: 'pi pi-check-square', docs: cpDocs, isDemarrageTermine: isCpTermine, isTermine: isCpTermine },
    { id: 'echantillonnage',title: 'Fiche Échantillonnage',    icon: 'pi pi-chart-pie',  docs: echDocs, isDemarrageTermine: isEchTermine, isTermine: isEchTermine },
    { id: 'planAss',        title: 'Plan Assemblage + Résultat',icon: 'pi pi-sitemap',   docs: planAssDocs, isDemarrageTermine: isPlanAssDemarrageTermine, isTermine: isPlanAssFinalTermine },
    { id: 'tracabilite',    title: 'Registre de Traçabilité',  icon: 'pi pi-history',    docs: tracaDocs, isDemarrageTermine: isTracaTermine, isTermine: isTracaTermine }
  ]
})

const onCategoryClick = async (cat, isFromAssistant = false, opts = {}) => {
  // Blocage : Vérifier si Fiche Échantillonnage est terminée
  if (cat.id !== 'echantillonnage' && cat.id !== 'tracabilite') {
    const echCat = documentCategories.value.find(c => c.id === 'echantillonnage')
    if (echCat && !echCat.isTermine) {
      toast.warn('Action bloquée', 'Veuillez d\'abord remplir la Fiche d\'Échantillonnage.')
      return
    }
  }

  // Blocage : Vérifier si VM et PlanAss sont terminés au démarrage avant d'ouvrir le reste (ex: documentControlePoste)
  if (cat.id === 'documentControlePoste') {
    const vmCat = documentCategories.value.find(c => c.id === 'verifMachine')
    const planAssCat = documentCategories.value.find(c => c.id === 'planAss')
    const isVmOk = vmCat ? (vmCat.isDemarrageTermine || vmCat.isTermine) : true
    const isPlanAssOk = planAssCat ? (planAssCat.isDemarrageTermine || planAssCat.isTermine) : true
    if (!isVmOk || !isPlanAssOk) {
      toast.warn('Action bloquée', 'Veuillez terminer les Vérifications de Démarrage (Vérif. Machine et Plan d\'Assemblage) d\'abord.')
      return
    }
  }

  selectedCategory.value = cat
  if (cat.id === 'echantillonnage') {
    if (cat.docs.length > 0) {
      router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: selectedPoste.value } })
    } else {
      openEchantillonnageDialog()
    }
  } else if (cat.id === 'verifMachine') {
    openVerifMachineDialog(isFromAssistant)
  } else if (cat.id === 'documentControlePoste') {
    if (operateurStore) {
      operateurStore.setActiveOfContext({
        ...ofActif.value,
        equipe: sessionEquipe.value,
        operationCode: 'ASS'
      })
    }
    // Vérifier si un plan existe avant de naviguer
    try {
      await apiClient.get(`/ExecRcPoste/${execOfIdActif.value}?posteCode=${selectedPoste.value}&equipe=${sessionEquipe.value || 1}`)
      // Plan trouvé → navigation normale
      router.push({
        name: 'exec-resultat-controle',
        params: { execControleOfId: execOfIdActif.value },
        query: { 
          posteCode: selectedPoste.value,
          mode: 'auto'
        }
      })
    } catch (err) {
      if (err.status === 404 || err.status === 400) {
        // Pas de plan → ouvrir le modal de signalement
        showdocumentControlePosteManquantDialog.value = true
      } else {
        // Navigation quand même (autre erreur)
        router.push({
          name: 'exec-resultat-controle',
          params: { execControleOfId: execOfIdActif.value },
          query: { 
            posteCode: selectedPoste.value,
            mode: 'auto'
          }
        })
      }
    }
  } else if (cat.id === 'planAss') {
    if (operateurStore) {
      operateurStore.setActiveOfContext({
        ...ofActif.value,
        equipe: sessionEquipe.value,
        operationCode: 'ASS'
      })
    }
    // Vérifier si le doc existe avant d'ouvrir
    try {
      const res = await apiClient.get(`/ExecPlanAssemblage/${execOfIdActif.value}/verifier-documents?posteCode=${selectedPoste.value}`)
      const data = res.data || res
      if (data && !data.valide) {
        docAssManquantDesc.value = data.docManquantDescription || `Plan d'assemblage ou Résultat en cours introuvable pour ${selectedPoste.value}`
        docAssMessageErreur.value = data.messageErreur || `Aucun plan d'assemblage ou résultat en cours trouvé pour ${selectedPoste.value}.`
        showPlanAssManquantDialog.value = true
        return
      }
      const vmCatNav = documentCategories.value.find(c => c.id === 'verifMachine')
      const planAssCatNav = documentCategories.value.find(c => c.id === 'planAss')
      const isDemarrageDoneNav = (vmCatNav ? (vmCatNav.isDemarrageTermine || vmCatNav.isTermine) : true) &&
                                 (planAssCatNav ? (planAssCatNav.isDemarrageTermine || planAssCatNav.isTermine) : true)
      router.push({
        name: 'exec-plan-assemblage',
        params: { execControleOfId: execOfIdActif.value },
        query: {
          posteCode: selectedPoste.value,
          isDemarrageDone: isDemarrageDoneNav ? 'true' : 'false',
          autoOpen1stPending: isFromAssistant ? 'true' : 'false',
          autoOpen100pct: opts.section100pct ? 'true' : 'false'
        }
      })
    } catch (err) {
      docAssManquantDesc.value = `Plan d'assemblage ou Résultat en cours introuvable pour ${selectedPoste.value}`
      docAssMessageErreur.value = err.message || `Document introuvable pour l'article en cours.`
      showPlanAssManquantDialog.value = true
    }
  } else if (cat.id === 'tracabilite') {
    router.push({
      name: 'exec-tracabilite',
      params: { execControleOfId: execOfIdActif.value },
      query: { posteCode: selectedPoste.value }
    })
  }
}

const handleCloturerCategory = async (cat) => {
  let text = `Êtes-vous sûr de vouloir clôturer la catégorie "${cat.title}" ? Cela marquera la tâche comme terminée.`
  
  if (cat.id === 'verifMachine') {
    const uncompletedMachines = machinesPoste.value.filter(m => {
      const mDocs = cat.docs.filter(d => d.machineCode === m.codeMachine)
      return mDocs.length === 0 || !mDocs.every(d => d.estTermine)
    })
    if (uncompletedMachines.length > 0) {
      text = `Attention, des machines n'ont pas encore été contrôlées. Voulez-vous vraiment marquer la catégorie comme terminée même si vous n'avez pas répondu à tous les documents ?`
    }
  }

  const result = await Swal.fire({
    title: 'Confirmation de clôture',
    text: text,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#10b981', // green-500
    cancelButtonColor: '#d33',
    confirmButtonText: 'Oui, clôturer',
    cancelButtonText: 'Annuler'
  })

  if (result.isConfirmed) {
    try {
      if (cat.id === 'documentControlePoste') {
        await apiClient.post(`/ExecRcPoste/${execOfIdActif.value}/cloturer-tous?posteCode=${selectedPoste.value}`)
      } else if (cat.id === 'verifMachine') {
        await apiClient.post(`/Operateur/of/${execOfIdActif.value}/verif-machine/cloturer-tous?posteCode=${selectedPoste.value}`)
      } else if (cat.id === 'planAss') {
        await apiClient.post(`/ExecPlanAssemblage/${execOfIdActif.value}/cloturer`)
      } else {
        const docsToClose = cat.docs.filter(d => !d.estTermine)
        for (const doc of docsToClose) {
          await apiClient.put(`/Operateur/assemblage-documents/${doc.id}/terminer`)
        }
      }
      toast.success('Succès', 'La catégorie a été clôturée.')
      await fetchDocumentsStatus()
    } catch {
      toast.error('Erreur', 'Impossible de clôturer la catégorie.')
    }
  }
}

const marquerTermine = async (docId) => {
  try {
    await apiClient.put(`/Operateur/assemblage-documents/${docId}/terminer`)
    await fetchDocumentsStatus()
  } catch (error) { console.error('Erreur maj document:', error) }
}

const rouvrirDocument = async (docId) => {
  try {
    await apiClient.put(`/Operateur/assemblage-documents/${docId}/rouvrir`)
    toast.success('Succès', 'Document réouvert avec succès.')
    await fetchDocumentsStatus()
  } catch (error) {
    console.error('Erreur réouverture document:', error)
    toast.error('Erreur', 'Impossible de réouvrir le document.')
  }
}

const ouvrirDocument = async (doc) => {
  if (showVerifMachineDialog.value) showVerifMachineDialog.value = false
  if (showListDialog.value) showListDialog.value = false
  
  if (doc.typeDocument === 'VERIF_MACHINE' && doc.machineCode) {
    if (operateurStore) {
      operateurStore.setActiveOfContext({
        ...ofActif.value,
        equipe: sessionEquipe.value,
        operationCode: 'ASS',
        machineCode: doc.machineCode
      })
    }
    
    try {
      const plans = await apiClient.get(`/DocumentVerifMachine?machineCode=${encodeURIComponent(doc.machineCode)}`).then(r => r.data)
      let activePlan = plans && Array.isArray(plans) ? plans.find(p => p.statut?.toUpperCase() === 'ACTIF') : null
      
      if (!activePlan && plans && plans.length > 0) {
        activePlan = plans[0]
      }
      
      if (!activePlan) {
        const allPlans = await apiClient.get('/DocumentVerifMachine').then(r => r.data)
        if (allPlans && Array.isArray(allPlans)) {
          activePlan = allPlans.find(p => p.statut?.toUpperCase() === 'ACTIF') || allPlans[0]
        }
      }

      if (activePlan) {
        if (!doc.id && execOfIdActif.value) {
          const res = await apiClient.post(`/Operateur/of/${execOfIdActif.value}/assemblage-documents/init/VERIF_MACHINE?posteCode=${selectedPoste.value || ''}&machineCode=${doc.machineCode}&equipe=${sessionEquipe.value || ''}`)
          if (res.data.id) doc.id = res.data.id
        }
        
        const perioFilter = doc.targetPeriodicite || targetPeriodiciteDialog.value || (isDemarrageModeDialog.value ? 'demarrage' : '')
        router.push({ 
          name: 'operateur-vm-exec', 
          params: { id: activePlan.id }, 
          query: { 
            statutId: doc.id, 
            mode: doc._openMode || 'details',
            execControleOfId: execOfIdActif.value,
            posteCode: selectedPoste.value,
            periodicite: perioFilter || undefined
          } 
        })
      } else {
        toast.error('Erreur', `Aucun plan actif trouvé pour la machine ${doc.machineCode}.`)
      }
    } catch (e) {
      console.error('Erreur ouverture document VM:', e)
      toast.error('Erreur', 'Impossible de récupérer le plan de vérification.')
    }
  } else if (doc.typeDocument === 'RESULTAT_CONTROLE_POSTE') {
    if (operateurStore) {
      operateurStore.setActiveOfContext({
        ...ofActif.value,
        equipe: sessionEquipe.value,
        operationCode: 'ASS'
      })
    }
    router.push({
      name: 'exec-resultat-controle',
      params: { execControleOfId: execOfIdActif.value },
      query: { 
        posteCode: doc.posteCode || selectedPoste.value,
        statutId: doc.id
      }
    })
  } else if (doc.typeDocument === 'ECHANTILLONNAGE') {
    router.push({
      name: 'exec-echantillonnage',
      params: { execControleOfId: execOfIdActif.value },
      query: { posteCode: doc.posteCode || selectedPoste.value }
    })
  } else if (['PLAN_ASS', 'PLAN_ASSEMBLAGE', 'PLAN_FAB', 'MODELE_FAB', 'RESULTAT_CF', 'RCCF'].includes(doc.typeDocument)) {
    router.push({
      name: 'exec-plan-assemblage',
      params: { execControleOfId: execOfIdActif.value },
      query: { 
        posteCode: doc.posteCode || selectedPoste.value,
        isDemarrageDone: doc.estTermine ? 'true' : 'false'
      }
    })
  } else {
    toast.info('Ouvrir document', `${doc.typeDocument} - ${doc.libelleFormulaire || 'Sans nom'}`)
  }
}

// eslint-disable-next-line no-unused-vars
const marquerTermineDoc = async (docId) => {
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
      posteCode: selectedPoste.value || (ofActif.value?.postesExistants ?? []).join(', ') || 'Inconnu',
      articleCode: ofActif.value?.codeArticle || 'Inconnu',
      numeroOf: ofActif.value?.numeroOf,
      designationArticle: ofActif.value?.designationArticle,
      nomOperateur: authStore.user?.nom || authStore.user?.matricule || 'Opérateur Inconnu',
      descriptionProbleme: `Document manquant : ${nomDocument}.`
    }
    await alertesService.signalerPlanManquant(data)
    toast.success('Signalement envoyé', 'Signalement envoyé au Superviseur avec succès !')
    showListDialog.value = false
  } catch {
    toast.error('Erreur', 'Erreur lors de l\'envoi du signalement.')
  } finally {
    isSignalingPlan.value = false
  }
}
// ──────────────────────────── Contrôle au Poste - Plan Manquant ──────────────────────────────
const signalerdocumentControlePosteManquant = async () => {
  isSignalingdocumentControlePoste.value = true
  try {
    await apiClient.post('/Alertes/plan-manquant', {
      operationCode: 'ASS',
      posteCode: selectedPoste.value,
      numeroOf: ofActif.value?.numeroOf,
      articleCode: ofActif.value?.codeArticle,
      designationArticle: ofActif.value?.designationArticle,
      descriptionProbleme: `Document Résultat Contrôle au Poste introuvable ou inactif pour le poste ${selectedPoste.value}`
    })
    toast.success('Signalé !', 'Une notification a été envoyée au superviseur.')
    showdocumentControlePosteManquantDialog.value = false
  } catch {
    toast.error('Erreur', "Impossible d'envoyer l'alerte.")
  } finally {
    isSignalingdocumentControlePoste.value = false
  }
}

// ──────────────────────────── Assemblage & Résultat - Plan Manquant ──────────────────────────────
const signalerPlanAssManquant = async () => {
  isSignalingPlanAss.value = true
  try {
    await apiClient.post('/Alertes/plan-manquant', {
      operationCode: 'ASS',
      posteCode: selectedPoste.value,
      numeroOf: ofActif.value?.numeroOf,
      articleCode: ofActif.value?.codeArticle,
      designationArticle: ofActif.value?.designationArticle,
      descriptionProbleme: `Document manquant : ${docAssManquantDesc.value || 'Plan d\'Assemblage / Résultat en cours'} pour le poste ${selectedPoste.value}`
    })
    toast.success('Signalé !', 'Une notification a été envoyée au superviseur pour le document manquant.')
    showPlanAssManquantDialog.value = false
  } catch {
    toast.error('Erreur', "Impossible d'envoyer l'alerte.")
  } finally {
    isSignalingPlanAss.value = false
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
  } catch {
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
    const resAll = await apiClient.get('/Operateur/machines')
    allMachines.value = resAll.data

    const res = await apiClient.get(`/Operateur/poste/${selectedPoste.value}/machines`)
    const defaultMachines = res.data.map(m => ({ ...m, isDefault: true }))

    const machineList = [...defaultMachines]

    // 1. Inclure les machines ayant des documents d'exécution initialisés pour ce poste
    const posteVmDocs = documents.value.filter(d => d.posteCode === selectedPoste.value && d.typeDocument === 'VERIF_MACHINE' && d.machineCode)
    posteVmDocs.forEach(d => {
      if (!machineList.some(m => m.codeMachine === d.machineCode)) {
        const found = allMachines.value.find(m => m.codeMachine === d.machineCode)
        if (found) {
          machineList.push({ ...found, isDefault: false })
        } else {
          machineList.push({ codeMachine: d.machineCode, designationMachine: d.machineCode, isDefault: false })
        }
      }
    })

    // 2. Préserver les machines ajoutées manuellement
    machinesPoste.value.forEach(m => {
      if (!m.isDefault && !machineList.some(existing => existing.codeMachine === m.codeMachine)) {
        machineList.push(m)
      }
    })

    machinesPoste.value = machineList
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

const isDemarrageModeDialog = ref(false)
const targetPeriodiciteDialog = ref('')

const openVerifMachineDialog = async (isDemarrage = false, targetPeriodicite = '') => {
  await loadMachinesForPoste()
  isDemarrageModeDialog.value = isDemarrage === true
  targetPeriodiciteDialog.value = targetPeriodicite || (isDemarrage ? 'demarrage' : '')
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
      const doc = cat?.docs.find(d => d.machineCode === machineCode && !d.estTermine) 
               || cat?.docs.find(d => d.machineCode === machineCode) 
               || { typeDocument: 'VERIF_MACHINE', machineCode }
      doc.targetPeriodicite = targetPeriodiciteDialog.value || (isDemarrageModeDialog.value ? 'demarrage' : '')
      ouvrirDocument(doc)
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
  } catch {
    toast.error('Erreur', 'Impossible de clôturer les documents de la machine.')
  }
}

// ──────────────────────────── Utils ──────────────────────────────
const performLeave = () => {
  execOfIdActif.value = null
  ofActif.value = null
  selectedPoste.value = null
  documents.value = []
  router.replace({ name: 'operateur-of-fini' })
  fetchOfsStatut()
}

const quitterSession = async () => {
  performLeave()
}

const tousLesPostesActifs = computed(() => {
  if (!ofActif.value) return []
  const of = ofsStatut.value.find(o => o.numeroOf === ofActif.value.numeroOf)
  return of?.postesExistants ?? []
})

// eslint-disable-next-line no-unused-vars
const mettreEnPauseOf = async () => {
  const result = await Swal.fire({
    title: 'Mettre la production en Pause ?',
    text: 'Voulez-vous vraiment mettre l\'OF en pause ?',
    input: 'text',
    inputPlaceholder: 'Motif (Optionnel, ex: Panne machine, pause déjeuner)...',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#eab308',
    cancelButtonColor: '#64748b',
    confirmButtonText: 'Oui, mettre en pause',
    cancelButtonText: 'Annuler'
  })

  if (result.isConfirmed) {
    try {
      const raison = result.value?.trim() || 'Pause'
      await operateurService.mettreEnPause(execOfIdActif.value, raison)
      toast.success('Production en Pause', 'L\'OF est maintenant en pause.')
      if (ofActif.value) ofActif.value.statut = 'EN_PAUSE'
      await fetchOfsStatut()
    } catch (error) {
      console.error(error)
      toast.error('Erreur', 'Impossible de mettre en pause.')
    }
  }
}

// eslint-disable-next-line no-unused-vars
const reprendreOf = async () => {
  try {
    await operateurService.reprendreDepuisPause(execOfIdActif.value)
    toast.success('Reprise', 'La production a repris.')
    if (ofActif.value) ofActif.value.statut = 'EN_COURS'
    await fetchOfsStatut()
  } catch (error) {
    console.error(error)
    toast.error('Erreur', error.response?.data?.message || 'Impossible de reprendre la production.')
  }
}

// eslint-disable-next-line no-unused-vars
const mettreEnReglageOf = async () => {
  try {
    await operateurService.mettreEnReglage(execOfIdActif.value)
    toast.success('Mode Réglage', 'Production en mode réglage. Procédez aux contrôles au démarrage.')
    if (ofActif.value) ofActif.value.statut = 'REGLAGE'
    await fetchOfsStatut()
  } catch (error) {
    console.error(error)
    toast.error('Erreur', 'Impossible de mettre en réglage.')
  }
}

// eslint-disable-next-line no-unused-vars
const cloturerOf = async () => {
  const result = await Swal.fire({
    title: 'Clôturer l\'OF d\'assemblage ?',
    text: 'Êtes-vous sûr de vouloir clôturer définitivement cet ordre de fabrication ?',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#d33',
    cancelButtonColor: '#3085d6',
    confirmButtonText: 'Oui, clôturer',
    cancelButtonText: 'Annuler'
  })

  if (result.isConfirmed) {
    try {
      await operateurService.cloturerOf(execOfIdActif.value)
      toast.success('Succès', 'L\'OF a été clôturé.')
      quitterSession()
    } catch (error) {
      console.error(error)
      toast.error('Erreur', 'Impossible de clôturer l\'OF.')
    }
  }
}
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

    <!-- ──────────────── PHASE 2 : Exécution Unifiée (Identique au Dashboard Standard) ──────────────── -->
    <div v-if="execOfIdActif" class="flex flex-col bg-white rounded-2xl shadow-sm border border-slate-200 p-6 min-h-[80vh]">
      
      <div class="flex justify-between items-center mb-4">
        <button @click="quitterSession" class="text-blue-600 hover:text-blue-800 hover:underline text-sm flex items-center transition-colors font-medium">
          <i class="pi pi-arrow-left mr-2"></i> Retour à la liste des OFs
        </button>
      </div>

      <div class="flex justify-between items-start mb-6 pb-4 border-b border-slate-200 flex-wrap gap-4">
        <div>
          <h2 class="text-2xl font-bold text-slate-800 flex items-center flex-wrap gap-2">
            <span>OF: {{ ofActif?.numeroOf }} <span class="text-lg text-slate-500 font-normal">(ASS)</span></span>
            <span class="text-lg font-semibold text-blue-700">{{ ofActif?.codeArticle }} — {{ ofActif?.designationArticle }}</span>
          </h2>
          <p class="text-gray-500 mt-1.5 text-sm">
            Postes Actifs: <strong class="text-slate-700">{{ tousLesPostesActifs.join(', ') || 'Poste 1' }}</strong>
            <span class="mx-2 text-slate-300">|</span>
            Équipe: <strong class="text-slate-700">Equipe {{ sessionEquipe || '1' }}</strong>
          </p>
          <p class="text-gray-500 mt-1 text-sm flex items-center gap-2">
            Statut: 
            <span class="px-2.5 py-0.5 rounded-full text-xs font-bold uppercase" :class="{'bg-amber-50 text-amber-700 border border-amber-200': ofActif?.statut === 'REGLAGE' || ofActif?.statut === 'EN_PAUSE', 'bg-emerald-50 text-emerald-700 border border-emerald-200': ofActif?.statut === 'EN_COURS' || !ofActif?.statut}">
              {{ ofActif?.statut || 'EN_COURS' }}
            </span>
          </p>
        </div>
        
        <!-- Boutons d'action OF (Aux Réglages, Pause, Fin poste) -->
        <div class="flex items-center gap-2 flex-wrap">
          <button @click="mettreEnReglageOf" class="px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-cog"></i> Aux Réglages
          </button>

          <button v-if="ofActif?.statut === 'EN_PAUSE'" @click="reprendreOf" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-play"></i> Reprendre
          </button>
          <button v-else @click="mettreEnPauseOf" class="px-4 py-2 bg-slate-600 hover:bg-slate-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-pause"></i> Pause
          </button>

          <button @click="cloturerOf" class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-stop"></i> Fin poste
          </button>
        </div>
      </div>

      <!-- Assistant To-Do List (Flux de démarrage guidé) -->
      <AssistantDemarrage 
        v-if="documentCategories && documentCategories.length > 0"
        :categories="documentCategories"
        :documents="documents"
        :alertes="alertesActives"
        @action-click="(cat, opts) => onCategoryClick(cat, true, opts)"
      />

      <!-- Corps Principal : Cartes des catégories de documents -->
      <div class="flex-1 overflow-y-auto mt-2">
        <DocumentCategories 
          :tousLesPostesActifs="tousLesPostesActifs" 
          :selectedPoste="selectedPoste || tousLesPostesActifs?.[0] || ofActif?.postesExistants?.[0] || 'PAS71'" 
          :documentCategories="documentCategories" 
          :isLoadingDocs="isLoadingDocs" 
          @update:selectedPoste="val => selectedPoste = val"
          @refresh="fetchDocumentsStatus" 
          @category-click="cat => onCategoryClick(cat, false)" 
          @cloturer-category="handleCloturerCategory" 
        />
      </div>
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
        <div>
          <div v-if="selectedCategory.docs.length === 0" class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300 m-2">
            <i class="pi pi-info-circle text-5xl text-gray-400 mb-4" style="display:block"></i>
            <p class="text-gray-600 text-lg mb-2">Aucun document n'est paramétré pour <strong>{{ selectedCategory.title }}</strong>.</p>
            <p class="text-gray-500 text-sm mb-6">Contactez votre superviseur pour qu'il crée ou active le document associé.</p>
            <Button label="Signaler au Superviseur" icon="pi pi-exclamation-triangle" severity="danger" outlined @click="signalerPlanManquant(selectedCategory.title)" :loading="isSignalingPlan" />
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
                    <Button v-if="doc.estTermine" icon="pi pi-undo" severity="warning" outlined size="small" @click="rouvrirDocument(doc.id)" v-tooltip.top="'Réouvrir le document'" />
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
      :isDemarrageMode="isDemarrageModeDialog"
      :targetPeriodicite="targetPeriodiciteDialog"
      @ouvrir-document="ouvrirDocument" 
      @init-document="initMachineDocument" 
      @marquer-termine="marquerTermine"
      @rouvrir-document="rouvrirDocument"
      @add-machine="addMachineToPoste"
      @remove-machine="removeMachineFromPoste"
      @cloturer-machine="cloturerDocumentsMachine"
      @signaler-plan-manquant="code => { planManquantMachineCode = code; showPlanManquantDialog = true; showVerifMachineDialog = false; }"
    />

    <!-- Dialog Plan Manquant Contrôle au Poste -->
    <Dialog v-model:visible="showdocumentControlePosteManquantDialog" modal header="Contrôle au Poste" :style="{ width: '480px' }" :closable="!isSignalingdocumentControlePoste">
      <div class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300 m-2">
        <i class="pi pi-info-circle text-5xl text-gray-400 mb-4" style="display:block"></i>
        <p class="text-gray-600 text-lg mb-2">Aucun plan de contrôle au poste n'est paramétré pour <strong>{{ selectedPoste }}</strong>.</p>
        <p class="text-gray-500 text-sm mb-6">Contactez votre superviseur pour qu'il crée ou active le document associé à ce poste.</p>
        <Button 
          label="Signaler au Superviseur" 
          icon="pi pi-exclamation-triangle" 
          severity="danger" 
          outlined 
          :loading="isSignalingdocumentControlePoste"
          @click="signalerdocumentControlePosteManquant" 
        />
      </div>
    </Dialog>

    <!-- Dialog Plan Manquant Assemblage & Résultat en cours -->
    <Dialog v-model:visible="showPlanAssManquantDialog" modal header="Document Manquant - Assemblage & Résultats" :style="{ width: '520px' }" :closable="!isSignalingPlanAss">
      <div class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300 m-2">
        <i class="pi pi-exclamation-triangle text-5xl text-amber-500 mb-4" style="display:block"></i>
        <p class="text-gray-700 font-medium text-lg mb-2">{{ docAssMessageErreur || "Document requis introuvable." }}</p>
        <div v-if="docAssManquantDesc" class="bg-amber-50 border border-amber-200 text-amber-800 p-3 rounded-lg text-sm mb-4 text-left">
          <strong>À signaler :</strong> {{ docAssManquantDesc }}
        </div>
        <p class="text-gray-500 text-sm mb-6">En cliquant ci-dessous, vous notifierez le superviseur précisément du document qui manque pour cet article.</p>
        <Button 
          label="Signaler ce document au Superviseur" 
          icon="pi pi-send" 
          severity="danger" 
          :loading="isSignalingPlanAss"
          @click="signalerPlanAssManquant" 
        />
      </div>
    </Dialog>

  </div>
</template>
