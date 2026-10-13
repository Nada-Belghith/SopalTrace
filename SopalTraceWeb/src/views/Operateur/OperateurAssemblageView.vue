<script setup>
import { ref, onMounted, onUnmounted, computed, watch } from 'vue'
import { useRouter, useRoute, onBeforeRouteLeave } from 'vue-router'
import { confirmer } from '@/composables/useConfirmation'

import { useAppToast } from '@/composables/useAppToast'
import { useSignalementsAssemblage } from '@/composables/Operateur/assemblage/useSignalementsAssemblage'
import OFGrid from '@/components/Operateur/Assemblage/OFGrid.vue'
import EnteteListeOf from '@/components/Operateur/EnteteListeOf.vue'
import EnTetePage from '@/components/Shared/EnTetePage.vue'
import { useFiltreOfs } from '@/composables/Operateur/useFiltreOfs'
import EtapesAssemblage from '@/components/Operateur/Assemblage/EtapesAssemblage.vue'
import TacheAssemblage from '@/components/Operateur/Assemblage/TacheAssemblage.vue'
import PostesAvancement from '@/components/Operateur/Assemblage/PostesAvancement.vue'
import DocumentsPoste from '@/components/Operateur/Assemblage/DocumentsPoste.vue'
import PostesSelectionDialog from '@/components/Operateur/Assemblage/Dialogs/PostesSelectionDialog.vue'
import DocumentManquantDialog from '@/components/Operateur/Assemblage/Dialogs/DocumentManquantDialog.vue'
import EchantillonnageInitDialog from '@/components/Operateur/Assemblage/Dialogs/EchantillonnageInitDialog.vue'
import PauseDialog from '@/components/Operateur/Assemblage/Dialogs/PauseDialog.vue'
import { useOperateurStore } from '@/stores/execution/operateurStore'
import operateurService from '@/services/operateurService'
import execAssemblageService from '@/services/execAssemblageService'
import { referentielsService } from '@/services/referentielsService'
import { logger } from '@/utils/logger'
import {
  OPERATION_ASSEMBLAGE, STATUTS_EXECUTION, STATUTS_EXECUTION_OUVERTS, TYPES_DOCUMENT, PERIODICITES, TYPES_PAUSE,
  PHASES_ASSEMBLAGE, TACHES_ASSEMBLAGE, ETATS_TACHE, DEMANDES_CLOTURE, MOMENTS_LOT
} from '@/constants/execution'

const operateurStore = useOperateurStore()
const router = useRouter()
const route = useRoute()
const toast = useAppToast()

/** Écrans ouverts depuis l'assistant : les quitter ne quitte pas la session. */
const ROUTES_INTERNES = ['exec-verif-machines', 'exec-echantillonnage', 'exec-resultat-controle', 'exec-plan-assemblage', 'exec-tracabilite', 'operateur-vm-exec', 'operateur-vm-view']
const RAFRAICHISSEMENT_MS = 60_000

// ───────────────────────────────── État ─────────────────────────────────
const ofsStatut = ref([])
const NON_COMMENCE = 'NON_COMMENCE'
const { recherche: rechercheOf, statut: statutFiltreOf, optionsStatut: optionsStatutOf, ofsAffiches } = useFiltreOfs(
  ofsStatut,
  of => of.statut || NON_COMMENCE,
  [
    { valeur: STATUTS_EXECUTION.EN_COURS, libelle: 'En cours' },
    { valeur: STATUTS_EXECUTION.REGLAGE, libelle: 'En réglage' },
    { valeur: STATUTS_EXECUTION.EN_PAUSE, libelle: 'En pause' },
    { valeur: NON_COMMENCE, libelle: 'Non commencés' },
    { valeur: STATUTS_EXECUTION.CLOTURE, libelle: 'Clôturés' }
  ],
  // OF clôturés (7 derniers jours) : en consultation, montrés par leur propre filtre.
  { masques: [STATUTS_EXECUTION.CLOTURE] })
const postesDisponibles = ref([])
const isLoading = ref(false)
const ofClique = ref(null)
const showPostesDialog = ref(false)
const isSubmitting = ref(false)

// Lus dès la création depuis l'URL : au retour d'un document, la session s'affiche directement
// (sans passer par la liste des OF le temps des chargements).
const execOfIdActif = ref(route.query.execControleOfId || null)
const selectedPoste = ref(route.query.posteCode || null)

/** Avancement calculé par le serveur : étape, tâches du poste, postes, fin possible. */
const avancement = ref(null)
/**
 * Équipe de la tablette : portée par l'URL (plusieurs équipes peuvent travailler en même temps sur l'OF, chacune
 * sur ses postes) ; à défaut, celle que le serveur associe au poste (l'équipe qui le tient).
 */
const equipeSession = computed(() => route.query.equipe || avancement.value?.equipe || null)
const documents = ref([])
const isLoadingSession = ref(!!execOfIdActif.value)
const actionEnCours = ref(false)
let minuterie = null

// Échantillonnage
const showNbPostesDialog = ref(false)
const nbPostesInput = ref(1)
const isInitializingEchantillonnage = ref(false)
const allInstruments = ref([])
const selectedInstruments = ref([])

// Vérification machine et pause
const showPauseDialog = ref(false)
const typePauseSelected = ref(TYPES_PAUSE.DEJEUNER)

const phase = computed(() => avancement.value?.phase)
const tousLesPostesActifs = computed(() => (avancement.value?.postes ?? []).map(p => p.posteCode))
const ofActif = computed(() => avancement.value && {
  numeroOf: avancement.value.numeroOf,
  codeArticle: avancement.value.codeArticle,
  designationArticle: avancement.value.designationArticle
})
const enFin = computed(() => [PHASES_ASSEMBLAGE.FIN_POSTE, PHASES_ASSEMBLAGE.FIN_OF].includes(phase.value))
/** Le poste est en cours pour l'équipe : sa fin de poste peut être demandée. */
const posteEnCours = computed(() => [PHASES_ASSEMBLAGE.ECHANTILLONNAGE, PHASES_ASSEMBLAGE.DEMARRAGE, PHASES_ASSEMBLAGE.PRODUCTION,
  PHASES_ASSEMBLAGE.REPRISE].includes(phase.value))
/** Autres postes que l'équipe tient encore (elle reste sur l'OF après la fin d'un poste). */
const autresPostesTenus = computed(() => (avancement.value?.postes ?? [])
  .filter(p => p.posteCode !== selectedPoste.value && p.equipeTitulaire === equipeSession.value)
  .map(p => p.posteCode))
/** Paramètres d'URL communs aux écrans ouverts depuis l'assistant : poste et équipe de la tablette. */
const queryPoste = (extra = {}) => ({ posteCode: selectedPoste.value, equipe: equipeSession.value || undefined, ...extra })
const sessionOuverte = computed(() => ![PHASES_ASSEMBLAGE.CLOTURE, PHASES_ASSEMBLAGE.SANS_EQUIPE].includes(phase.value))
const documentsDuPoste = computed(() => documents.value.filter(d => d.posteCode === selectedPoste.value))

const {
  documentManquant, isSignaling, fermerSignalement, signalerDocumentManquant,
  signalerControlePosteManquant, signalerPlanAssManquant
} = useSignalementsAssemblage({ selectedPoste, tousLesPostesActifs, ofActif })

const ENTETES = {
  [PHASES_ASSEMBLAGE.ECHANTILLONNAGE]: ['Échantillonnage', 'Remplissez la fiche d\'échantillonnage de l\'OF : elle est obligatoire avant les contrôles de démarrage.'],
  [PHASES_ASSEMBLAGE.NON_DEMARRE]: ['Poste non démarré', 'Ce poste est libre : démarrez-le pour faire ses contrôles de démarrage, puis de fin de poste.'],
  [PHASES_ASSEMBLAGE.POSTE_TERMINE]: ['Poste terminé', 'Votre équipe a terminé ce poste : une autre équipe peut le prendre. Continuez sur vos autres postes.'],
  [PHASES_ASSEMBLAGE.DEMARRAGE]: ['Démarrage du poste', 'Vérifiez les machines du poste et contrôlez la première pièce avant de produire.'],
  [PHASES_ASSEMBLAGE.PRODUCTION]: ['Production en cours', 'Faites les contrôles de production quand ils sont dus et remplissez le contrôle au poste au fil du poste.'],
  [PHASES_ASSEMBLAGE.REPRISE]: ['Reprise après la pause déjeuner', 'Vérifiez les machines prévues « après la pause » avant de reprendre la production.'],
  [PHASES_ASSEMBLAGE.PAUSE]: ['Production en pause', 'Reprenez la production quand l\'équipe est de retour.'],
  [PHASES_ASSEMBLAGE.FIN_POSTE]: ['Fin de poste', 'Faites les contrôles de fin de ce poste, puis terminez-le : vos autres postes continuent et l\'OF reste en cours.'],
  [PHASES_ASSEMBLAGE.FIN_OF]: ['Clôture de l\'OF', 'Faites les contrôles de fin de chaque poste que vous tenez et la dernière pièce « fin d\'OF », puis clôturez l\'OF.'],
  [PHASES_ASSEMBLAGE.SANS_EQUIPE]: ['Aucune équipe sur ce poste', 'Ce poste est libre : prenez-le depuis la liste des OF (commencer votre poste ou seulement clôturer).'],
  [PHASES_ASSEMBLAGE.CLOTURE]: ['OF clôturé', 'Cet OF est terminé : ses documents restent consultables.']
}
const entete = computed(() => phase.value === PHASES_ASSEMBLAGE.POSTE_OCCUPE
  ? ['Poste tenu par une autre équipe', `L'équipe ${avancement.value?.equipeTitulaire ?? ''} tient ce poste : il se libère quand elle le termine.`]
  : ENTETES[phase.value] ?? ['', ''])
const messageSansTache = computed(() => phase.value === PHASES_ASSEMBLAGE.PAUSE
  ? 'Aucune saisie pendant la pause.'
  : 'Rien à faire sur ce poste pour le moment.')
const libelleTerminer = computed(() => phase.value === PHASES_ASSEMBLAGE.FIN_OF
  ? "Clôturer définitivement l'OF"
  : `Terminer le poste ${selectedPoste.value}`)
const heure = (date) => new Date(date).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })

// ────────────────────────────── Chargements ──────────────────────────────
const fetchOfsStatut = async () => {
  isLoading.value = true
  try {
    ofsStatut.value = (await execAssemblageService.getOfsStatut()).data
  } catch (error) {
    logger.error('Erreur chargement OFs assemblage', error)
    toast.error('Erreur', error.message)
  } finally {
    isLoading.value = false
  }
}

const fetchReferentiels = async () => {
  try {
    const [postes, instruments] = await Promise.all([
      operateurService.getPostesDisponibles(),
      referentielsService.getInstruments()
    ])
    postesDisponibles.value = postes.data
    allInstruments.value = instruments.data.data
  } catch (error) {
    logger.error('Erreur chargement référentiels', error)
  }
}

/** Recharge l'avancement (serveur) et les documents du poste sélectionné. */
const chargerSession = async ({ silencieux = false } = {}) => {
  if (!execOfIdActif.value || !selectedPoste.value) return
  if (!silencieux) isLoadingSession.value = true
  try {
    const [av, docs] = await Promise.all([
      execAssemblageService.getAvancement(execOfIdActif.value, selectedPoste.value, route.query.equipe || null),
      execAssemblageService.getDocuments(execOfIdActif.value, equipeSession.value)
    ])
    avancement.value = av.data
    documents.value = docs.data
  } catch (error) {
    logger.error('Erreur chargement session assemblage', error)
    if (!silencieux) toast.error('Erreur', error.message)
  } finally {
    isLoadingSession.value = false
  }
}

/** Restaure la session (OF, poste) à partir de l'URL : la même étape s'affiche après un rechargement. */
const synchroniserDepuisRoute = async () => {
  const execId = route.query.execControleOfId
  if (!execId) {
    execOfIdActif.value = null
    avancement.value = null
    return
  }
  execOfIdActif.value = execId
  const of = ofsStatut.value.find(o => o.execControleOfId?.toLowerCase() === String(execId).toLowerCase())
  selectedPoste.value = route.query.posteCode || selectedPoste.value || of?.postesExistants?.[0] || null
  await chargerSession()
}

onMounted(async () => {
  const references = Promise.all([fetchOfsStatut(), fetchReferentiels()])
  // Session connue par l'URL : chargée en parallèle de la liste des OF.
  if (execOfIdActif.value && selectedPoste.value) await Promise.all([references, chargerSession()])
  else {
    await references
    await synchroniserDepuisRoute()
  }
  minuterie = setInterval(() => chargerSession({ silencieux: true }), RAFRAICHISSEMENT_MS)
})

onUnmounted(() => clearInterval(minuterie))

watch(() => route.fullPath, synchroniserDepuisRoute)

const selectionnerPoste = (poste) => {
  if (poste === selectedPoste.value) return
  router.replace({ query: { ...route.query, posteCode: poste } })
}

// ──────────────────────────── Quitter ──────────────────────
/** Quitter en production met la session en pause ; la fin de poste passe par ses contrôles. */
const confirmerDepart = async () => {
  if (!execOfIdActif.value || !sessionOuverte.value || phase.value === PHASES_ASSEMBLAGE.PAUSE) return true
  const { confirme } = await confirmer({
    titre: 'Quitter la session ?',
    message: 'La production sera mise en pause. Pour terminer le poste, utilisez « Fin de poste » et faites les contrôles de fin.',
    libelleConfirmer: 'Mettre en pause et quitter',
    libelleAnnuler: 'Rester',
    icone: 'pi pi-pause'
  })
  if (!confirme) return false
  try {
    await operateurService.mettreEnPause(execOfIdActif.value, 'Départ opérateur', TYPES_PAUSE.AUTRE, equipeSession.value)
    return true
  } catch (error) {
    toast.error('Erreur', error.message)
    return false
  }
}

let departConfirme = false
onBeforeRouteLeave(async (to) => {
  if (ROUTES_INTERNES.includes(to.name) || departConfirme) return true
  return await confirmerDepart()
})

const retourListe = async () => {
  if (!(await confirmerDepart())) return
  departConfirme = true
  await router.replace({ name: 'operateur-of-fini' })
  departConfirme = false
  execOfIdActif.value = null
  avancement.value = null
  fetchOfsStatut()
}

// ──────────────────────────── Démarrer / rejoindre un OF ──────────────────────
const onOfClick = (of) => {
  // OF clôturé : l'assistant s'ouvre en consultation (frise terminée, documents consultables, aucune saisie).
  if (of.statut === STATUTS_EXECUTION.CLOTURE && of.execControleOfId) {
    router.replace({ query: { execControleOfId: of.execControleOfId, posteCode: of.postesExistants?.[0] || undefined } })
    return
  }
  ofClique.value = of
  showPostesDialog.value = true
}

const postesExistants = computed(() => ofClique.value?.postesExistants ?? [])
const isDejaEnCours = computed(() => STATUTS_EXECUTION_OUVERTS.includes(ofClique.value?.statut))

/** Ouvre l'assistant de l'équipe sur un OF où elle travaille déjà (sans rien reprendre). */
const ouvrirOf = ({ equipe }) => {
  if (!ofClique.value?.execControleOfId) return
  showPostesDialog.value = false
  const sesPostes = ofClique.value.equipes?.find(e => e.equipe === equipe)?.postes ?? []
  router.replace({ query: { execControleOfId: ofClique.value.execControleOfId, posteCode: sesPostes[0] || ofClique.value.postesExistants?.[0] || undefined, equipe } })
}

const confirmerPostes = async ({ postes, equipe, pourCloturer }) => {
  if (!ofClique.value) return
  if (!pourCloturer && postes.length === 0) {
    toast.warn('Attention', 'Veuillez sélectionner au moins un poste.')
    return
  }

  isSubmitting.value = true
  try {
    let execId = ofClique.value.execControleOfId
    if (isDejaEnCours.value) {
      await execAssemblageService.ajouterPostes(execId, postes, equipe, pourCloturer)
    } else {
      execId = (await execAssemblageService.demarrerOf(ofClique.value.numeroOf, postes, equipe)).data.id
    }
    showPostesDialog.value = false
    await fetchOfsStatut()
    const of = ofsStatut.value.find(o => o.execControleOfId?.toLowerCase() === String(execId).toLowerCase())
    const poste = postes[0] || of?.equipes?.find(e => e.equipe === equipe)?.postes?.[0] || of?.postesExistants?.[0]
    router.replace({ query: { execControleOfId: execId, posteCode: poste || undefined, equipe } })
  } catch (error) {
    logger.error('Erreur démarrage OF', error)
    toast.error('Erreur', error.message || 'Erreur lors de l\'opération.')
  } finally {
    isSubmitting.value = false
  }
}

// ──────────────────────────── Tâches ──────────────────────
const setContexteOf = (extra = {}) => {
  operateurStore.setActiveOfContext({ ...ofActif.value, equipe: avancement.value?.equipe, operationCode: OPERATION_ASSEMBLAGE, ...extra })
}

const ouvrirPlan = (moment) => {
  setContexteOf()
  router.push({
    name: 'exec-plan-assemblage',
    params: { execControleOfId: execOfIdActif.value },
    query: { posteCode: selectedPoste.value, moment, equipe: equipeSession.value || avancement.value?.equipe || undefined }
  })
}

const ouvrirTache = async (tache) => {
  const manquant = tache.etat === ETATS_TACHE.MANQUANT
  switch (tache.code) {
    case TACHES_ASSEMBLAGE.ECHANTILLONNAGE:
      if (manquant) signalerPlanAssManquant('Plan d\'échantillonnage actif', tache.detail)
      else if (tache.etat === ETATS_TACHE.FAIT) router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: queryPoste() })
      else ouvrirInitEchantillonnage()
      break
    case TACHES_ASSEMBLAGE.DEMARRER_POSTE:
      await executer(() => execAssemblageService.demarrerPoste(execOfIdActif.value, selectedPoste.value, equipeSession.value), `Poste ${selectedPoste.value} démarré.`)
      break
    case TACHES_ASSEMBLAGE.VERIF_MACHINE:
      ouvrirVerifMachines(tache.periodicite)
      break
    case TACHES_ASSEMBLAGE.PLAN_PREMIERE_PIECE:
    case TACHES_ASSEMBLAGE.PLAN_DERNIERE_PIECE:
      if (manquant) signalerPlanAssManquant(`Plan d'assemblage (famille ${avancement.value.familleProduitFiniCode || '?'}, poste ${selectedPoste.value})`, tache.detail)
      else ouvrirPlan(tache.code === TACHES_ASSEMBLAGE.PLAN_PREMIERE_PIECE ? MOMENTS_LOT.PREMIERE_PIECE : MOMENTS_LOT.DERNIERE_PIECE)
      break
    case TACHES_ASSEMBLAGE.CONTROLES_PRODUCTION:
      if (manquant) signalerPlanAssManquant(`Plan d'assemblage (famille ${avancement.value.familleProduitFiniCode || '?'}, poste ${selectedPoste.value})`, tache.detail)
      else ouvrirPlan('PRODUCTION')
      break
    case TACHES_ASSEMBLAGE.CONTROLE_POSTE:
      if (manquant) signalerControlePosteManquant()
      else await ouvrirControlePoste()
      break
    case TACHES_ASSEMBLAGE.REGLAGE:
      ouvrirPlan(MOMENTS_LOT.REGLAGE)
      break
    case TACHES_ASSEMBLAGE.TOUTES_PIECES:
      ouvrirPlan(MOMENTS_LOT.TOUTES_PIECES)
      break
    case TACHES_ASSEMBLAGE.TRACABILITE:
      setContexteOf()
      router.push({ name: 'exec-tracabilite', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: selectedPoste.value, equipe: equipeSession.value || undefined } })
      break
  }
}

const ouvrirControlePoste = async () => {
  setContexteOf()
  try {
    await execAssemblageService.getPlanRcPoste(execOfIdActif.value, selectedPoste.value, avancement.value.equipe)
    router.push({
      name: 'exec-resultat-controle',
      params: { execControleOfId: execOfIdActif.value },
      query: { posteCode: selectedPoste.value, mode: 'auto', equipe: equipeSession.value || avancement.value.equipe || undefined }
    })
  } catch (error) {
    if (error.status === 404 || error.status === 400) signalerControlePosteManquant()
    else toast.error('Erreur', error.message)
  }
}

/** Ouvre un document déjà commencé (liste des documents du poste) : en consultation pour la vérification machine. */
const ouvrirDocument = (doc) => {
  switch (doc.typeDocument) {
    case TYPES_DOCUMENT.VERIF_MACHINE:
      if (!doc.docId) break
      setContexteOf({ machineCode: doc.machineCode })
      router.push({
        name: 'operateur-vm-exec',
        params: { id: doc.docId },
        query: { statutId: doc.id, execControleOfId: execOfIdActif.value, posteCode: selectedPoste.value, equipe: equipeSession.value || undefined, assistant: '1', consultation: '1' }
      })
      break
    case TYPES_DOCUMENT.RESULTAT_CONTROLE_POSTE:
      setContexteOf()
      router.push({ name: 'exec-resultat-controle', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: doc.posteCode, statutId: doc.id, equipe: equipeSession.value || undefined } })
      break
    case TYPES_DOCUMENT.ECHANTILLONNAGE:
      router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: doc.posteCode, equipe: equipeSession.value || undefined } })
      break
    case TYPES_DOCUMENT.PLAN_ASS:
      ouvrirPlan(enFin.value ? MOMENTS_LOT.DERNIERE_PIECE : MOMENTS_LOT.PREMIERE_PIECE)
      break
    case TYPES_DOCUMENT.REGISTRE_TRACA:
      router.push({ name: 'exec-tracabilite', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: doc.posteCode, equipe: equipeSession.value || undefined } })
      break
  }
}

// ──────────────────────────── Échantillonnage ──────────────────────────────
const ouvrirInitEchantillonnage = () => {
  // Une fiche par poste : le poste est traité comme le seul de l'OF (modifiable si l'effectif est réparti).
  nbPostesInput.value = 1
  showNbPostesDialog.value = true
}

const initializeEchantillonnage = async () => {
  isInitializingEchantillonnage.value = true
  try {
    await execAssemblageService.initEchantillonnage(execOfIdActif.value, {
      posteCode: selectedPoste.value,
      nbPostes: nbPostesInput.value,
      instrumentCodes: selectedInstruments.value,
      equipe: avancement.value?.equipe
    })
    showNbPostesDialog.value = false
    router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: queryPoste() })
  } catch (error) {
    toast.error('Erreur', `Erreur lors de l'initialisation : ${error.message}`)
  } finally {
    isInitializingEchantillonnage.value = false
  }
}

// ──────────────────────────── Vérification machine ──────────────────────────────
/** Machines du poste à vérifier pour ce moment (démarrage, après la pause, fin de poste). */
const ouvrirVerifMachines = (periodicite) => {
  setContexteOf()
  router.push({
    name: 'exec-verif-machines',
    params: { execControleOfId: execOfIdActif.value },
    query: queryPoste({ periodicite: periodicite || PERIODICITES.DEMARRAGE })
  })
}

const executer = async (action, messageSucces) => {
  actionEnCours.value = true
  try {
    await action()
    if (messageSucces) toast.success('Succès', messageSucces)
    await chargerSession({ silencieux: true })
  } catch (error) {
    toast.error('Erreur', error.message || 'Action impossible.')
  } finally {
    actionEnCours.value = false
  }
}

// ──────────────────────────── Pause, fin de poste, fin d'OF ──────────────────────
const mettreEnPause = () => executer(async () => {
  await operateurService.mettreEnPause(execOfIdActif.value, 'Pause opérateur', typePauseSelected.value, equipeSession.value)
  showPauseDialog.value = false
}, 'Production mise en pause.')

const reprendre = () => executer(() => operateurService.reprendreDepuisPause(execOfIdActif.value, equipeSession.value), 'Production reprise.')

const demanderCloture = async (mode) => {
  const finOf = mode === DEMANDES_CLOTURE.FIN_OF
  const { confirme } = await confirmer({
    titre: finOf ? 'Clôturer l\'OF ?' : `Fin de poste ${selectedPoste.value} ?`,
    message: finOf
      ? 'Les contrôles de fin de poste et de fin d\'OF de tous les postes deviennent à faire. Vous pourrez annuler tant que l\'OF n\'est pas clôturé.'
      : `Les contrôles de fin de poste de ${selectedPoste.value} deviennent à faire ; vos autres postes continuent. Vous pourrez annuler tant que le poste n'est pas terminé.`,
    libelleConfirmer: 'Continuer',
    icone: 'pi pi-arrow-right'
  })
  if (confirme) await executer(() => execAssemblageService.demanderCloture(execOfIdActif.value, mode, selectedPoste.value, equipeSession.value))
}

const annulerCloture = () => executer(() => execAssemblageService.annulerDemandeCloture(execOfIdActif.value, selectedPoste.value, equipeSession.value), 'Retour en production.')

const terminer = async () => {
  const finOf = phase.value === PHASES_ASSEMBLAGE.FIN_OF
  const poste = selectedPoste.value
  const suivants = autresPostesTenus.value
  const { confirme } = await confirmer({
    titre: finOf ? 'Clôturer définitivement l\'OF ?' : `Terminer le poste ${poste} ?`,
    message: finOf
      ? 'L\'OF sera clôturé pour toutes les équipes.'
      : suivants.length
        ? `Le poste ${poste} sera libéré pour une autre équipe ; votre équipe continue sur ${suivants.join(', ')}.`
        : `Le poste ${poste} sera libéré pour une autre équipe. C'est votre dernier poste : la session de l'équipe se termine, l'OF reste ouvert.`,
    danger: true,
    libelleConfirmer: finOf ? 'Clôturer l\'OF' : `Terminer ${poste}`,
    icone: 'pi pi-stop-circle'
  })
  if (!confirme) return

  actionEnCours.value = true
  try {
    await execAssemblageService.terminer(execOfIdActif.value, poste, equipeSession.value)
    toast.success('Succès', finOf ? 'L\'OF a été clôturé.' : `Le poste ${poste} est terminé.`)
    // L'équipe tient encore d'autres postes : elle continue sur le suivant.
    if (!finOf && suivants.length) {
      router.replace({ query: { ...route.query, posteCode: suivants[0] } })
      return
    }
    departConfirme = true
    await router.replace({ name: 'operateur-of-fini' })
    departConfirme = false
    execOfIdActif.value = null
    avancement.value = null
    fetchOfsStatut()
  } catch (error) {
    toast.error('Fin impossible', error.message)
    await chargerSession({ silencieux: true })
  } finally {
    actionEnCours.value = false
  }
}
</script>

<template>
  <div class="p-4 md:p-6">
    <!-- ──────────────── Liste des OF ──────────────── -->
    <template v-if="!execOfIdActif">
      <EnteteListeOf titre="OF produits finis"
                     sous-titre="Choisissez un ordre de fabrication d'assemblage pour le démarrer ou rejoindre la session en cours."
                     v-model:recherche="rechercheOf"
                     v-model:statut="statutFiltreOf"
                     :options-statut="optionsStatutOf"
                     :nombre-affiches="ofsAffiches.length"
                     :nombre-total="ofsStatut.length" />
      <OFGrid :ofsStatut="ofsAffiches" :isLoading="isLoading" :filtre-actif="ofsAffiches.length !== ofsStatut.length" @select-of="onOfClick" />
    </template>

    <!-- ──────────────── Session ──────────────── -->
    <div v-else class="w-full space-y-5">
      <div v-if="isLoadingSession && !avancement" class="py-24 text-center text-slate-500">
        <i class="pi pi-spin pi-spinner text-3xl"></i>
        <p class="m-0 mt-3 text-sm">Chargement de la session…</p>
      </div>

      <template v-else-if="avancement">
        <!-- En-tête : retour, OF, article, équipe, actions -->
        <EnTetePage sur-titre="Assemblage · Ordre de fabrication" :titre="avancement.numeroOf"
                    :sous-titre="`${avancement.codeArticle} — ${avancement.designationArticle}`"
                    retour libelle-retour="Retour à la liste des OF" @retour="retourListe">
          <div class="mt-2 flex flex-wrap gap-2 text-xs">
            <span class="px-2.5 py-1 rounded-lg bg-slate-100 text-slate-600">Famille <strong class="text-slate-800">{{ avancement.familleProduitFiniCode || '—' }}</strong></span>
            <span class="px-2.5 py-1 rounded-lg bg-slate-100 text-slate-600">Type <strong class="text-slate-800">{{ avancement.typeRobinetCode || '—' }}</strong></span>
            <span class="px-2.5 py-1 rounded-lg bg-slate-100 text-slate-600">Équipe <strong class="text-slate-800">{{ equipeSession || '—' }}</strong></span>
          </div>

          <template v-if="sessionOuverte" #actions>
            <button v-if="phase === PHASES_ASSEMBLAGE.PAUSE" type="button" :disabled="actionEnCours" @click="reprendre"
                    class="min-h-[48px] px-5 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold border-0 cursor-pointer disabled:opacity-50">
              <i class="pi pi-play mr-2"></i>Reprendre
            </button>
            <button v-else-if="enFin" type="button" :disabled="actionEnCours" @click="annulerCloture"
                    class="min-h-[48px] px-5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold border-0 cursor-pointer disabled:opacity-50">
              <i class="pi pi-undo mr-2"></i>Revenir en production
            </button>
            <template v-else>
              <button type="button" :disabled="actionEnCours" @click="showPauseDialog = true"
                      class="min-h-[48px] px-5 rounded-xl bg-amber-500 hover:bg-amber-600 text-white font-bold border-0 cursor-pointer disabled:opacity-50">
                <i class="pi pi-pause mr-2"></i>Pause
              </button>
              <button v-if="posteEnCours" type="button" :disabled="actionEnCours" @click="demanderCloture(DEMANDES_CLOTURE.FIN_POSTE)"
                      class="min-h-[48px] px-5 rounded-xl bg-slate-700 hover:bg-slate-800 text-white font-bold border-0 cursor-pointer disabled:opacity-50">
                <i class="pi pi-flag mr-2"></i>Fin de poste {{ selectedPoste }}
              </button>
              <button type="button" :disabled="actionEnCours" @click="demanderCloture(DEMANDES_CLOTURE.FIN_OF)"
                      class="min-h-[48px] px-5 rounded-xl bg-slate-900 hover:bg-black text-white font-bold border-0 cursor-pointer disabled:opacity-50">
                <i class="pi pi-stop-circle mr-2"></i>Clôturer l'OF
              </button>
            </template>
          </template>
        </EnTetePage>

        <EtapesAssemblage :etapes="avancement.etapes" />

        <div class="grid grid-cols-1 xl:grid-cols-3 gap-5 items-start">
          <!-- Étape courante : ce qu'il faut faire maintenant -->
          <section class="xl:col-span-2 bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
            <div class="px-5 py-4 border-b border-slate-100 bg-slate-50">
              <h3 class="m-0 text-xl font-black text-slate-800">
                {{ entete[0] }}<span v-if="sessionOuverte" class="text-slate-400 font-semibold"> · poste {{ selectedPoste }}</span>
              </h3>
              <p class="m-0 mt-1 text-slate-500">{{ entete[1] }}</p>
              <p v-if="phase === PHASES_ASSEMBLAGE.PAUSE && avancement.enPauseDepuis" class="m-0 mt-1 text-sm text-amber-700 font-semibold">
                En pause depuis {{ heure(avancement.enPauseDepuis) }}
              </p>
              <div v-if="avancement.reglageEnCours && phase === PHASES_ASSEMBLAGE.PRODUCTION"
                   class="mt-2 rounded-lg bg-amber-50 border border-amber-200 px-3 py-2 text-sm text-amber-900 flex flex-wrap items-center gap-2">
                <span class="flex-1 min-w-[200px]"><i class="pi pi-wrench mr-1"></i>Réglage en cours : les contrôles de production sont suspendus
                  jusqu'à un contrôle de réglage conforme sur tous les critères.</span>
                <button type="button" @click="ouvrirPlan(MOMENTS_LOT.REGLAGE)"
                        class="min-h-[40px] px-4 rounded-lg bg-amber-600 hover:bg-amber-700 text-white font-bold border-0 cursor-pointer">Continuer le réglage</button>
              </div>
            </div>

            <div class="p-5 space-y-3">
              <TacheAssemblage v-for="tache in avancement.taches" :key="tache.code + (tache.periodicite || '')"
                               :tache="tache" :desactivee="actionEnCours" @ouvrir="ouvrirTache" />

              <div v-if="!avancement.taches.length" class="py-10 text-center text-slate-400">
                <i class="pi text-3xl" :class="phase === PHASES_ASSEMBLAGE.PAUSE ? 'pi-pause-circle' : 'pi-check-circle'"></i>
                <p class="m-0 mt-2 text-sm">{{ messageSansTache }}</p>
                <button v-if="!sessionOuverte" type="button" @click="retourListe"
                        class="mt-4 min-h-[48px] px-5 rounded-xl bg-blue-600 text-white font-bold border-0 cursor-pointer">
                  Retour à la liste des OF
                </button>
              </div>

              <!-- Fin : ce qui manque sur tous les postes, puis la clôture -->
              <div v-if="enFin" class="pt-2 space-y-3">
                <div v-if="avancement.manquants.length" class="rounded-xl border border-amber-200 bg-amber-50 p-4">
                  <p class="m-0 font-bold text-amber-900">Reste à faire avant de terminer :</p>
                  <ul class="m-0 mt-2 pl-5 text-sm text-amber-900">
                    <li v-for="m in avancement.manquants" :key="m">{{ m }}</li>
                  </ul>
                </div>
                <button type="button" :disabled="!avancement.peutTerminer || actionEnCours" @click="terminer"
                        class="w-full min-h-[56px] rounded-xl font-black text-white border-0 cursor-pointer disabled:cursor-not-allowed disabled:bg-slate-300"
                        :class="avancement.peutTerminer ? 'bg-red-600 hover:bg-red-700' : ''">
                  <i class="pi mr-2" :class="avancement.peutTerminer ? 'pi-stop-circle' : 'pi-lock'"></i>
                  {{ libelleTerminer }}
                </button>
              </div>
            </div>
          </section>

          <!-- Colonne latérale : postes, contrôles à la demande, documents -->
          <aside class="space-y-5 xl:sticky xl:top-4">
            <section class="bg-white rounded-2xl border border-slate-200 shadow-sm p-5">
              <h3 class="m-0 mb-3 text-base font-black text-slate-800">
                <i class="pi pi-sitemap mr-2 text-slate-400"></i>Postes de l'OF
              </h3>
              <PostesAvancement :postes="avancement.postes" :posteSelectionne="selectedPoste" @selectionner="selectionnerPoste" />
            </section>

            <section v-if="avancement.controlesALaDemande?.length" class="bg-white rounded-2xl border border-slate-200 shadow-sm p-5 space-y-3">
              <div>
                <h3 class="m-0 text-base font-black text-slate-800"><i class="pi pi-bolt mr-2 text-slate-400"></i>Contrôles à la demande</h3>
                <p class="m-0 mt-1 text-sm text-slate-500">Aux réglages ou sur toutes les pièces, quand vous le jugez nécessaire.</p>
              </div>
              <TacheAssemblage v-for="tache in avancement.controlesALaDemande" :key="tache.code"
                               :tache="tache" compacte :desactivee="actionEnCours" @ouvrir="ouvrirTache" />
            </section>

            <DocumentsPoste :documents="documentsDuPoste" @ouvrir="ouvrirDocument" />
          </aside>
        </div>
      </template>
    </div>

    <PostesSelectionDialog
      v-model:visible="showPostesDialog"
      :ofClique="ofClique"
      :postesExistants="postesExistants"
      :postesAAjouter="postesDisponibles"
      :isDejaEnCours="isDejaEnCours"
      :isSubmitting="isSubmitting"
      @confirmer="confirmerPostes"
      @ouvrir="ouvrirOf"
    />

    <EchantillonnageInitDialog
      v-model:visible="showNbPostesDialog"
      v-model:nbPostes="nbPostesInput"
      v-model:instruments="selectedInstruments"
      :instrumentsDisponibles="allInstruments"
      :isSubmitting="isInitializingEchantillonnage"
      @generer="initializeEchantillonnage"
    />

    <PauseDialog v-model:visible="showPauseDialog" v-model:typePause="typePauseSelected" @confirmer="mettreEnPause" />

    <DocumentManquantDialog :document="documentManquant" :isSignaling="isSignaling"
                            @signaler="signalerDocumentManquant" @fermer="fermerSignalement" />
  </div>
</template>
