<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import apiClient from '@/services/apiClient'
import alertesService from '@/services/alertesService'
import Card from 'primevue/card'
import MultiSelect from 'primevue/multiselect'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Tooltip from 'primevue/tooltip'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'

import { useAppToast } from '@/composables/useAppToast'

const vTooltip = Tooltip
const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()
const toast = useAppToast()

const formatDate = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('fr-FR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  }).format(date)
}

// ───────────────────────────────── State ─────────────────────────────────
const ofsStatut = ref([])          // Liste des OFs avec statut (NON COMMENCÉ / EN COURS)
const postesDisponibles = ref([])  // Tous les postes depuis la DB
const isLoading = ref(false)

// OF sélectionné pour le dialog
const ofClique = ref(null)

// Dialog postes
const showPostesDialog = ref(false)
const postesSelectionnes = ref([])  // Nouveaux postes à ajouter
const isSubmitting = ref(false)

// Phase exécution (après démarrage / reprise)
const execOfIdActif = ref(null)
const ofActif = ref(null)
const selectedPoste = ref(null)
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

// ────────────────────────────── Init ──────────────────────────────────
onMounted(async () => {
  await Promise.all([fetchOfsStatut(), fetchPostes()])
  
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

// ──────────────────────────── Click sur carte OF ──────────────────────
const onOfClick = (of) => {
  ofClique.value = of
  postesSelectionnes.value = []   // reset sélection des NOUVEAUX postes
  showPostesDialog.value = true
}

// Postes déjà enregistrés (lecture seule)
const postesExistants = computed(() => ofClique.value?.postesExistants ?? [])

// Postes disponibles pour en ajouter (exclut ceux déjà présents)
const postesAAjouter = computed(() =>
  postesDisponibles.value.filter(p => !postesExistants.value.includes(p.codePoste))
)

const isDejaEnCours = computed(() => ofClique.value?.statut === 'EN_COURS')

// ────────────────────────── Démarrer ou Ajouter ───────────────────────
const confirmerPostes = async () => {
  if (!ofClique.value) return

  if (!isDejaEnCours.value && postesSelectionnes.value.length === 0) {
    toast.warn('Attention', 'Veuillez sélectionner au moins un poste.')
    return
  }

  isSubmitting.value = true
  try {
    if (!isDejaEnCours.value) {
      // 1ère fois : démarrer l'OF
      const request = {
        numeroOf: ofClique.value.numeroOf,
        operationCode: 'ASS',
        posteCodes: postesSelectionnes.value,
        numEquipe: 1,
        matriculeOperateur: authStore.user?.matricule
      }
      const response = await apiClient.post('/Operateur/of/start-assemblage', request)
      execOfIdActif.value = response.data.id
    } else {
      // OF existant : ajouter les nouveaux postes (si sélectionnés)
      execOfIdActif.value = ofClique.value.execControleOfId
      if (postesSelectionnes.value.length > 0) {
        await apiClient.post(
          `/Operateur/of/${execOfIdActif.value}/ajouter-postes`,
          postesSelectionnes.value
        )
      }
    }

    ofActif.value = ofClique.value
    showPostesDialog.value = false
    
    // Rafraîchir les données de l'OF
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
  
  return [
    { id: 'verifMachine',   title: 'Vérification Machine',     icon: 'pi pi-cog',        docs: posteDocs.filter(d => d.typeDocument === 'VERIF_MACHINE') },
    { id: 'controlePoste',  title: 'Résultat Contrôle Poste',  icon: 'pi pi-check-square', docs: posteDocs.filter(d => d.typeDocument === 'RESULTAT_CONTROLE_POSTE') },
    { id: 'echantillonnage',title: 'Fiche Échantillonnage',    icon: 'pi pi-chart-pie',  docs: posteDocs.filter(d => d.typeDocument === 'ECHANTILLONNAGE') },
    { id: 'planAss',        title: 'Plan Assemblage + Résultat',icon: 'pi pi-sitemap',   docs: [] },
    { id: 'tracabilite',    title: 'Registre de Traçabilité',  icon: 'pi pi-history',    docs: [] }
  ]
})

const getCategoryBorderColor = (cat) => {
  if (cat.id === 'planAss' || cat.id === 'tracabilite') return 'bg-blue-400'
  if (cat.docs.length === 0) return 'bg-gray-300'
  return cat.docs.some(d => !d.estTermine) ? 'bg-orange-400' : 'bg-green-400'
}
const getCategoryIconColor = (cat) => {
  if (cat.id === 'planAss' || cat.id === 'tracabilite') return 'text-blue-500 bg-blue-50'
  if (cat.docs.length === 0) return 'text-gray-400 bg-gray-100'
  return cat.docs.some(d => !d.estTermine) ? 'text-orange-500 bg-orange-50' : 'text-green-500 bg-green-50'
}

const onCategoryClick = async (cat) => {
  selectedCategory.value = cat
  if (cat.id === 'echantillonnage') {
    if (cat.docs.length > 0) {
      router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: selectedPoste.value } })
    } else {
      openEchantillonnageDialog()
    }
  } else if (cat.id === 'planAss' || cat.id === 'tracabilite') {
    showListDialog.value = true
  } else {
    if (cat.docs.length === 0) {
      let typeDoc = ''
      if (cat.id === 'verifMachine') typeDoc = 'VERIF_MACHINE'
      else if (cat.id === 'controlePoste') typeDoc = 'RESULTAT_CONTROLE_POSTE'
      if (typeDoc && execOfIdActif.value) {
        try {
          const pCode = selectedPoste.value || ''
          const res = await apiClient.post(`/Operateur/of/${execOfIdActif.value}/assemblage-documents/init/${typeDoc}?posteCode=${pCode}`)
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

const ouvrirDocument = (doc) => {
  toast.info('Ouvrir document', `${doc.typeDocument} - ${doc.libelleFormulaire || 'Sans nom'}`)
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

// Echantillonnage
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
    await apiClient.post(`/ExecEchantillonnage/init/${execOfIdActif.value}?posteCode=${pCode}&nbPostes=${nbPostesInput.value}`)
    showNbPostesDialog.value = false
    router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value }, query: { posteCode: pCode } })
  } catch (error) {
    toast.error('Erreur', 'Erreur lors de l\'initialisation : ' + (error.response?.data?.message || error.message))
  } finally {
    isInitializingEchantillonnage.value = false
  }
}

// Quitter session
const quitterSession = () => {
  execOfIdActif.value = null
  ofActif.value = null
  selectedPoste.value = null
  documents.value = []
  fetchOfsStatut()
}

// Postes actifs affichés dans le bandeau session (existants + nouveaux si ajoutés)
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

      <!-- Chargement -->
      <div v-if="isLoading" class="flex justify-center items-center h-40">
        <i class="pi pi-spin pi-spinner text-4xl text-primary"></i>
      </div>

      <!-- Grille -->
      <div v-else class="flex flex-wrap gap-6 ml-2">
        <div
          v-for="of in ofsStatut"
          :key="of.numeroOf"
          @click="onOfClick(of)"
          class="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 w-[400px] cursor-pointer hover:shadow-md hover:border-blue-300 transition-all flex flex-col justify-between"
        >
          <!-- En-tête : référence OF + badge statut -->
          <div class="flex justify-between items-start mb-4">
            <span class="text-xs font-bold text-blue-600 tracking-wider">OF: {{ of.numeroOf }}</span>
            <span class="px-2 py-1 font-bold text-[10px] rounded uppercase tracking-wider"
                  :class="of.statut === 'EN_COURS' ? 'bg-green-50 text-green-600' : 'bg-gray-100 text-gray-500'">
              {{ of.statut === 'EN_COURS' ? 'EN COURS' : 'NON COMMENCÉ' }}
            </span>
          </div>

          <!-- Titre article -->
          <div class="mb-4">
            <h3 class="text-lg font-bold text-slate-800 leading-tight mb-1">{{ of.designationArticle }}</h3>
            <p class="text-sm text-slate-400 font-medium">{{ of.codeArticle }}</p>
          </div>

          <!-- Postes (si EN COURS) -->
          <div v-if="of.postesExistants?.length > 0" class="mb-4">
            <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-2">Postes actifs</p>
            <div class="flex flex-wrap gap-1.5">
              <span
                v-for="p in of.postesExistants"
                :key="p"
                class="px-2.5 py-0.5 bg-blue-50 text-blue-700 text-xs font-semibold rounded-full border border-blue-100"
              >{{ p }}</span>
            </div>
          </div>
          <div v-else class="mb-4">
            <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-2">Postes actifs</p>
            <p class="text-xs text-slate-400 italic">Aucun poste assigné</p>
          </div>

          <!-- Pied : quantité -->
          <div class="flex justify-between items-end border-t border-slate-100 pt-4 mt-auto">
            <div>
              <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Qté lancée</p>
              <p class="text-sm font-bold text-slate-700">{{ of.quantiteLancee ?? '-' }}</p>
            </div>
            <div class="text-right">
              <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Date début</p>
              <p class="text-sm font-medium text-slate-600">{{ of.dateDebut ? formatDate(of.dateDebut) : '-' }}</p>
            </div>
          </div>
        </div>

        <!-- Aucun OF -->
        <div v-if="ofsStatut.length === 0" class="w-full text-center py-16 text-gray-400">
          <i class="pi pi-inbox text-5xl mb-3"></i>
          <p class="text-lg">Aucun OF d'assemblage disponible.</p>
        </div>
      </div>
    </div>

    <!-- ──────────────── PHASE 2 : Exécution ──────────────── -->
    <div v-if="execOfIdActif" class="flex flex-col gap-6">

      <!-- Bandeau session -->
      <div class="bg-white rounded-xl shadow-sm border border-green-200 p-4 flex flex-wrap justify-between items-center gap-4">
        <div class="flex items-center gap-4">
          <div class="bg-green-50 text-green-600 w-12 h-12 rounded-full flex items-center justify-center">
            <i class="pi pi-box text-2xl"></i>
          </div>
          <div>
            <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-0.5">OF Actif</div>
            <div class="font-bold text-gray-800">{{ ofActif?.numeroOf }} — {{ ofActif?.designationArticle }}</div>
          </div>
        </div>

        <div class="hidden md:block h-10 border-l border-gray-200"></div>

        <div class="flex-1">
          <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-2">Postes Actifs</div>
          <div class="flex flex-wrap gap-2">
            <Tag v-for="p in tousLesPostesActifs" :key="p" :value="p" severity="info" class="px-3 py-1 text-sm" />
            <span v-if="tousLesPostesActifs.length === 0" class="text-sm text-gray-400 italic">Aucun poste</span>
          </div>
        </div>

        <div class="ml-auto">
          <Button icon="pi pi-sign-out" label="Quitter la session" severity="danger" outlined size="small" @click="quitterSession" />
        </div>
      </div>

      <!-- Cartes documents -->
      <Card class="shadow-md border border-gray-100">
        <template #title>
          <div class="flex justify-between items-center p-2 mb-2 border-b border-gray-100 pb-4">
            <div class="flex items-center gap-2">
              <span class="text-sm font-bold text-gray-400 uppercase tracking-wider mr-2">Sélectionnez un poste :</span>
              <div 
                v-for="p in tousLesPostesActifs" 
                :key="p"
                @click="selectedPoste = p"
                class="px-6 py-2 rounded-xl font-bold text-sm cursor-pointer transition-all border-2"
                :class="selectedPoste === p ? 'bg-blue-50 text-blue-600 border-blue-200 shadow-sm' : 'bg-white text-gray-500 border-transparent hover:bg-gray-50 hover:text-gray-700'"
              >
                {{ p }}
              </div>
            </div>
            <Button icon="pi pi-refresh" text rounded class="text-gray-400 hover:text-primary hover:bg-gray-50" @click="fetchDocumentsStatus" v-tooltip.top="'Actualiser'" />
          </div>
        </template>
        <template #content>
          <div v-if="isLoadingDocs" class="flex justify-center p-4">
            <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
          </div>
          <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-5 mt-4">
            <div
              v-for="cat in documentCategories"
              :key="cat.id"
              @click="onCategoryClick(cat)"
              class="border rounded-xl p-6 cursor-pointer transition-all hover:-translate-y-1 hover:shadow-lg flex flex-col items-center text-center relative overflow-hidden bg-white"
              :class="getCategoryBorderColor(cat).replace('bg-', 'border-').replace('400', '200') + ' hover:border-primary'"
            >
              <div class="absolute left-0 top-0 right-0 h-1" :class="getCategoryBorderColor(cat)"></div>
              <div class="w-16 h-16 rounded-full flex items-center justify-center mb-4" :class="getCategoryIconColor(cat)">
                <i :class="cat.icon" class="text-3xl"></i>
              </div>
              <h3 class="font-bold text-gray-800 text-base leading-tight mb-3 flex-1 flex items-center">{{ cat.title }}</h3>
              <div class="mt-auto w-full">
                <Tag v-if="cat.id === 'planAss' || cat.id === 'tracabilite'" value="Accès Permanent" severity="info" class="w-full" />
                <Tag v-else-if="cat.docs.length > 0" :value="cat.docs.some(d => !d.estTermine) ? 'À Remplir' : 'Terminé'" :severity="cat.docs.some(d => !d.estTermine) ? 'warning' : 'success'" class="w-full" />
                <Tag v-else value="Non requis" severity="secondary" class="w-full" />
              </div>
            </div>
          </div>
        </template>
      </Card>
    </div>

    <!-- ──────────────── Dialog : Sélection / Ajout de postes ──────────────── -->
    <Dialog
      v-model:visible="showPostesDialog"
      modal
      :header="isDejaEnCours ? `Gérer les postes — ${ofClique?.numeroOf}` : `Démarrer l'assemblage — ${ofClique?.numeroOf}`"
      :style="{ width: '480px' }"
      :closable="!isSubmitting"
    >
      <div class="flex flex-col gap-5 mt-2">

        <!-- Infos OF -->
        <div class="bg-gray-50 rounded-lg p-3 text-sm text-gray-600">
          <span class="font-semibold text-gray-800">Article :</span> {{ ofClique?.designationArticle }}<br>
          <span class="font-semibold text-gray-800">Qté lancée :</span> {{ ofClique?.quantiteLancee ?? '-' }}
        </div>

        <!-- Postes existants (lecture seule, si EN COURS) -->
        <div v-if="isDejaEnCours && postesExistants.length > 0">
          <label class="block text-sm font-bold text-gray-700 mb-2">
            <i class="pi pi-lock mr-1 text-green-600"></i> Postes déjà enregistrés (non modifiables)
          </label>
          <div class="flex flex-wrap gap-2 bg-green-50 border border-green-200 rounded-lg p-3">
            <Tag v-for="p in postesExistants" :key="p" :value="p" severity="success" />
          </div>
        </div>

        <!-- Sélection de nouveaux postes -->
        <div>
          <label class="block text-sm font-bold text-gray-700 mb-2">
            <i class="pi pi-plus-circle mr-1 text-primary"></i>
            {{ isDejaEnCours ? 'Ajouter des postes supplémentaires (optionnel)' : 'Sélectionner les postes de travail *' }}
          </label>
          <MultiSelect
            v-model="postesSelectionnes"
            :options="postesAAjouter"
            optionLabel="libelle"
            optionValue="codePoste"
            :placeholder="isDejaEnCours ? 'Ajouter un poste...' : 'Sélectionner des postes'"
            :maxSelectedLabels="5"
            class="w-full"
            display="chip"
          />
          <small v-if="!isDejaEnCours" class="text-gray-500 mt-1 block">
            Sélectionnez tous les postes concernés par cet OF.
          </small>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end gap-2 mt-4">
          <Button label="Annuler" icon="pi pi-times" severity="secondary" text @click="showPostesDialog = false" :disabled="isSubmitting" />
          <Button
            :label="isDejaEnCours ? (postesSelectionnes.length > 0 ? 'Ajouter et Reprendre' : 'Reprendre') : 'Démarrer l\'assemblage'"
            :icon="isDejaEnCours ? 'pi pi-play' : 'pi pi-play'"
            severity="primary"
            @click="confirmerPostes"
            :loading="isSubmitting"
          />
        </div>
      </template>
    </Dialog>

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
                    <Button :label="doc.estTermine ? 'Consulter' : 'Ouvrir'" :icon="doc.estTermine ? 'pi pi-eye' : 'pi pi-pencil'" :severity="doc.estTermine ? 'secondary' : 'primary'" size="small" @click="() => { showListDialog = false; ouvrirDocument(doc) }" />
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
      </div>
      <template #footer>
        <div class="flex justify-end gap-2 mt-4">
          <Button label="Annuler" icon="pi pi-times" text severity="secondary" @click="showNbPostesDialog = false" :disabled="isInitializingEchantillonnage" />
          <Button label="Générer Fiche" icon="pi pi-check" @click="initializeEchantillonnage" :loading="isInitializingEchantillonnage" />
        </div>
      </template>
    </Dialog>

  </div>
</template>

<style scoped></style>
