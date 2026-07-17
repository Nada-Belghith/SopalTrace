<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/authStore'
import apiClient from '@/services/apiClient'
import alertesService from '@/services/alertesService'
import Card from 'primevue/card'
import Select from 'primevue/select'
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
const toast = useAppToast()

// State
const ofsDisponibles = ref([])
const ofSelectionne = ref(null)
const postesDisponibles = ref([])
const postesSelectionnes = ref([])
const isStarting = ref(false)
const documents = ref([])
const isLoadingDocs = ref(false)

const execOfIdActif = ref(null)

// --- Initialisation ---
onMounted(async () => {
  await fetchOfsDisponibles()
  await fetchPostes()
})

const fetchOfsDisponibles = async () => {
  try {
    const response = await apiClient.get('/Operateur/ofs/operations')
    const allOfs = response.data
    // Filtrer pour ne garder que ceux qui ont l'opération ASS et dont le statut permet de démarrer
    ofsDisponibles.value = allOfs.filter(of => 
      of.gammeOperatoire.some(op => op.operationCode === 'ASS')
    )
  } catch (error) {
    console.error("Erreur lors de la récupération des OFs :", error)
  }
}

const fetchPostes = async () => {
  try {
    const response = await apiClient.get('/Operateur/postes')
    postesDisponibles.value = response.data
  } catch (error) {
    console.error("Erreur lors de la récupération des postes :", error)
  }
}

// --- Démarrage de l'assemblage ---
const demarrerAssemblage = async () => {
  if (!ofSelectionne.value || postesSelectionnes.value.length === 0) {
    toast.warn("Attention", "Veuillez sélectionner un OF et au moins un poste de travail.")
    return
  }

  isStarting.value = true
  try {
    const request = {
      numeroOf: ofSelectionne.value.numeroOf,
      operationCode: 'ASS',
      posteCodes: postesSelectionnes.value,
      numEquipe: 1, // On pourrait ajouter un sélecteur d'équipe
      matriculeOperateur: authStore.user?.matricule
    }
    
    const response = await apiClient.post('/Operateur/of/start-assemblage', request)
    execOfIdActif.value = response.data.id
    
    // Une fois démarré, charger les documents requis
    await fetchDocumentsStatus()
  } catch (error) {
    console.error("Erreur au démarrage :", error)
    toast.error("Erreur", "Erreur lors du démarrage de l'assemblage.")
  } finally {
    isStarting.value = false
  }
}

// --- Documents ---
const fetchDocumentsStatus = async () => {
  if (!execOfIdActif.value) return
  isLoadingDocs.value = true
  try {
    const response = await apiClient.get(`/Operateur/of/${execOfIdActif.value}/assemblage-documents`)
    documents.value = response.data
  } catch (error) {
    console.error("Erreur chargement documents :", error)
  } finally {
    isLoadingDocs.value = false
  }
}

const documentCategories = computed(() => {
  return [
    { 
      id: 'verifMachine', 
      title: 'Vérification Machine', 
      icon: 'pi pi-cog', 
      docs: documents.value.filter(d => d.typeDocument === 'VERIF_MACHINE')
    },
    { 
      id: 'controlePoste', 
      title: 'Résultat Contrôle Poste', 
      icon: 'pi pi-check-square', 
      docs: documents.value.filter(d => d.typeDocument === 'RESULTAT_CONTROLE_POSTE')
    },
    { 
      id: 'echantillonnage', 
      title: 'Fiche Échantillonnage', 
      icon: 'pi pi-chart-pie', 
      docs: documents.value.filter(d => d.typeDocument === 'ECHANTILLONNAGE')
    },
    { 
      id: 'planAss', 
      title: 'Plan Assemblage + Résultat', 
      icon: 'pi pi-sitemap', 
      docs: [] 
    },
    { 
      id: 'tracabilite', 
      title: 'Registre de Traçabilité', 
      icon: 'pi pi-history', 
      docs: [] 
    }
  ]
})

const selectedCategory = ref(null)
const showListDialog = ref(false)

const onCategoryClick = async (cat) => {
  selectedCategory.value = cat
  
  if (cat.id === 'echantillonnage') {
    if (cat.docs.length > 0) {
      // Déjà présent dans Exec_ControleDocumentStatut, on navigue directement
      router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value } })
    } else {
      // Pas de doc, on vérifie dans la table source
      openEchantillonnageDialog()
    }
  } else if (cat.id === 'planAss' || cat.id === 'tracabilite') {
    showListDialog.value = true
  } else {
    // Si pas de docs initialisés, on essaie d'en initialiser
    if (cat.docs.length === 0) {
      let typeDoc = '';
      if (cat.id === 'verifMachine') typeDoc = 'VERIF_MACHINE';
      else if (cat.id === 'controlePoste') typeDoc = 'RESULTAT_CONTROLE_POSTE';
      else if (cat.id === 'controleCf') typeDoc = 'RESULTAT_CONTROLE_CF';
      else if (cat.id === 'produitFini') typeDoc = 'PRODUIT_FINI';

      if (typeDoc && execOfIdActif.value) {
        try {
          const res = await apiClient.post(`/Operateur/of/${execOfIdActif.value}/assemblage-documents/init/${typeDoc}`);
          if (res.data.initialized) {
            await fetchDocumentsStatus();
            const updatedCat = documentCategories.value.find(c => c.id === cat.id);
            if (updatedCat && updatedCat.docs.length > 0) {
              if (updatedCat.docs.length === 1) ouvrirDocument(updatedCat.docs[0]);
              else showListDialog.value = true;
              return;
            }
          }
        } catch (e) {
          console.error("Erreur init doc", e);
        }
      }
    }

    // Comportement standard (soit déjà initialisés, soit rien n'a été trouvé)
    if (cat.docs.length === 0) {
      showListDialog.value = true
    } else if (cat.docs.length === 1) {
      ouvrirDocument(cat.docs[0])
    } else {
      showListDialog.value = true
    }
  }
}

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


const ouvrirDocument = (doc) => {
  // Ici on devra rediriger vers la vue d'exécution du formulaire concerné
  // Par exemple :
  // router.push({ name: 'exec-encf-form', params: { id: doc.refFormulaireId } })
  // Mais il faut d'abord adapter le composant d'exécution pour prendre en charge l'OF
  toast.info("Ouvrir document", `${doc.typeDocument} - ${doc.libelleFormulaire || 'Sans nom'}`)
}

const marquerTermine = async (docId) => {
  try {
    await apiClient.put(`/Operateur/assemblage-documents/${docId}/terminer`)
    await fetchDocumentsStatus()
  } catch (error) {
    console.error("Erreur maj document :", error)
  }
}

const isSignalingPlan = ref(false)
const signalerPlanManquant = async (docName) => {
  isSignalingPlan.value = true
  try {
    const nomDocument = typeof docName === 'string' ? docName : 'Plan ou document';
    const data = {
      operationCode: 'ASS',
      posteCode: postesSelectionnes.value.join(', ') || 'Inconnu',
      articleCode: ofSelectionne.value?.numeroOf || ofSelectionne.value?.codeArticle || 'Inconnu',
      nomOperateur: authStore.user?.nom || authStore.user?.matricule || 'Opérateur Inconnu',
      descriptionProbleme: `Document manquant : ${nomDocument}. Veuillez vérifier le paramétrage et rattacher les plans nécessaires.`
    }
    await alertesService.signalerPlanManquant(data)
    toast.success("Signalement envoyé", "Signalement envoyé au Superviseur avec succès !")
    showListDialog.value = false
  } catch (error) {
    console.error("Erreur signalement :", error)
    toast.error("Erreur", "Erreur lors de l'envoi du signalement.")
  } finally {
    isSignalingPlan.value = false
  }
}

// --- Echantillonnage Logic ---
const showNbPostesDialog = ref(false)
const nbPostesInput = ref(1)
const isInitializingEchantillonnage = ref(false)
const isCheckingPlan = ref(false)
const showMissingPlanDialog = ref(false)

const openEchantillonnageDialog = async () => {
  if (!execOfIdActif.value) return
  isCheckingPlan.value = true
  
  try {
    // 1. Puisqu'il n'est pas dans Exec_ControleDocumentStatut (cat.docs est vide),
    // on vérifie s'il y a un plan source valide (Document_Echantillonnage_Entete)
    const resSource = await apiClient.get(`/ExecEchantillonnage/check-source/${execOfIdActif.value}`)
    if (resSource.data?.exists) {
        // Le plan existe, on demande le nb de postes
        nbPostesInput.value = 1
        showNbPostesDialog.value = true
    } else {
        // Pas de plan ! On utilise la modale générique de liste vide.
        showListDialog.value = true
    }
  } catch (sourceErr) {
    console.error("Erreur vérification plan source:", sourceErr)
    toast.error("Erreur", "Erreur lors de la vérification du plan source.")
  } finally {
    isCheckingPlan.value = false
  }
}

const initializeEchantillonnage = async () => {
  if (!execOfIdActif.value) return
  isInitializingEchantillonnage.value = true
  try {
    const response = await apiClient.post(`/ExecEchantillonnage/init/${execOfIdActif.value}?nbPostes=${nbPostesInput.value}`)
    showNbPostesDialog.value = false
    // Naviguer vers la nouvelle vue
    router.push({ name: 'exec-echantillonnage', params: { execControleOfId: execOfIdActif.value } })
    } catch (error) {
      console.error("Erreur initialisation echantillonnage :", error)
      if (error.message === "Plan d'échantillonnage source introuvable.") {
        showListDialog.value = true
        showNbPostesDialog.value = false
      } else {
      toast.error("Erreur", "Erreur lors de l'initialisation de la fiche d'échantillonnage: " + error.message)
    }
  } finally {
    isInitializingEchantillonnage.value = false
  }
}
</script>

<template>
  <div class="p-6">
    <div class="mb-6">
      <h2 class="text-3xl font-bold text-primary mb-2 flex items-center">
        <i class="pi pi-wrench mr-3 text-3xl"></i> Exécution Assemblage (Produits Finis)
      </h2>
      <p class="text-gray-500">Sélectionnez un OF, les postes concernés, puis remplissez les documents associés.</p>
    </div>

    <!-- Phase 1: Démarrage -->
    <Card v-if="!execOfIdActif" class="mb-6 shadow-md border border-gray-100">
      <template #title>
        <div class="flex items-center text-xl">
          <i class="pi pi-play-circle mr-2 text-primary"></i> Démarrer une session d'assemblage
        </div>
      </template>
      <template #content>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mt-4">
          <div>
            <label class="block font-bold mb-2">Ordre de Fabrication (OF)</label>
            <Select 
              v-model="ofSelectionne" 
              :options="ofsDisponibles" 
              optionLabel="numeroOf"
              placeholder="-- Choisir un OF --" 
              class="w-full"
            >
              <template #value="slotProps">
                <div v-if="slotProps.value" class="flex align-items-center">
                  <div>{{ slotProps.value.numeroOf }} - {{ slotProps.value.designationArticle }}</div>
                </div>
                <span v-else>
                  {{ slotProps.placeholder }}
                </span>
              </template>
              <template #option="slotProps">
                <div class="flex align-items-center">
                  <div>{{ slotProps.option.numeroOf }} - {{ slotProps.option.designationArticle }}</div>
                </div>
              </template>
            </Select>
          </div>
          <div>
            <label class="block font-bold mb-2">Postes de travail (sélection multiple)</label>
            <MultiSelect 
              v-model="postesSelectionnes" 
              :options="postesDisponibles" 
              optionLabel="libelle" 
              optionValue="codePoste" 
              placeholder="Sélectionner des postes" 
              :maxSelectedLabels="3" 
              class="w-full"
              display="chip"
            />
            <small class="text-gray-500 mt-2 block">Sélectionnez plusieurs postes (ex: PAS71, PAS72, PAS73 pour une soupape).</small>
          </div>
        </div>
        <div class="flex justify-end mt-6">
          <Button 
            label="Démarrer l'assemblage" 
            icon="pi pi-play" 
            @click="demarrerAssemblage" 
            :loading="isStarting" 
            :disabled="!ofSelectionne || postesSelectionnes.length === 0" 
            severity="primary"
          />
        </div>
      </template>
    </Card>

    <!-- Phase 2: Exécution et Documents -->
    <div v-if="execOfIdActif" class="flex flex-col gap-6">
      
      <!-- En-tête de session -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4 flex flex-wrap justify-between items-center gap-4">
        <div class="flex items-center gap-4">
          <div class="bg-primary/10 text-primary w-12 h-12 rounded-full flex items-center justify-center">
            <i class="pi pi-box text-2xl"></i>
          </div>
          <div>
            <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-1">Produit Fini</div>
            <div class="font-bold text-gray-800">{{ ofSelectionne?.codeArticle }} - {{ ofSelectionne?.designationArticle }}</div>
          </div>
        </div>
        
        <div class="hidden md:block h-10 border-l border-gray-200"></div>
        
        <div class="flex items-center gap-4">
          <div class="bg-blue-50 text-blue-600 w-12 h-12 rounded-full flex items-center justify-center">
            <i class="pi pi-file text-xl"></i>
          </div>
          <div>
            <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-1">Ordre de Fabrication</div>
            <div class="font-bold text-gray-800">{{ ofSelectionne?.numeroOf }}</div>
          </div>
        </div>
        
        <div class="hidden md:block h-10 border-l border-gray-200"></div>
        
        <div class="flex items-center gap-4">
          <div class="bg-orange-50 text-orange-600 w-12 h-12 rounded-full flex items-center justify-center">
            <i class="pi pi-hashtag text-xl"></i>
          </div>
          <div>
            <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-1">Quantité Lancée</div>
            <div class="font-bold text-gray-800">{{ ofSelectionne?.quantiteLancee ?? '-' }}</div>
          </div>
        </div>

        <div class="hidden md:block h-10 border-l border-gray-200"></div>
        
        <div class="flex-1">
          <div class="text-xs text-gray-500 uppercase tracking-wider font-bold mb-2">Postes Actifs</div>
          <div class="flex flex-wrap gap-2">
            <Tag v-for="poste in postesSelectionnes" :key="poste" :value="poste" severity="info" class="px-3 py-1 text-sm"></Tag>
          </div>
        </div>
        
        <div class="ml-auto">
          <!-- Bouton Terminer / Quitter la session -->
          <Button icon="pi pi-sign-out" label="Quitter la session" severity="danger" outlined size="small" @click="() => { execOfIdActif = null; ofSelectionne = null; postesSelectionnes = []; }" />
        </div>
      </div>
      
      <!-- Main Section : Cartes des documents en pleine largeur -->
      <Card class="shadow-md border border-gray-100">
        <template #title>
          <div class="flex justify-between items-center text-lg bg-primary text-white p-4 -m-5 rounded-t-lg mb-4">
            <span class="flex items-center"><i class="pi pi-file-check mr-2"></i> Documents associés</span>
            <Button icon="pi pi-refresh" text rounded class="text-white hover:bg-white/20" @click="fetchDocumentsStatus" v-tooltip.top="'Actualiser'" />
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
              <!-- Indicateur couleur sur le bord haut -->
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

      <!-- Modal (Dialog) pour les détails ou la liste des documents -->
      <Dialog v-model:visible="showListDialog" modal :header="selectedCategory?.title" :style="{ width: '50vw' }" :breakpoints="{ '960px': '75vw', '641px': '100vw' }">
        <div v-if="selectedCategory" class="pt-4">
          
          <!-- Cas Plan Assemblage et Tracabilité -->
          <div v-if="selectedCategory.id === 'planAss' || selectedCategory.id === 'tracabilite'" class="bg-blue-50 p-8 rounded-xl text-center border border-blue-100">
            <i :class="selectedCategory.icon" class="text-5xl text-blue-300 mb-4"></i>
            <h4 class="text-xl font-bold text-blue-900 mb-2">Module en développement</h4>
            <p class="text-blue-700">L'interface spécifique pour accéder au {{ selectedCategory.title.toLowerCase() }} n'a pas encore été raccordée.</p>
          </div>

          <!-- Cas Formulaires -->
          <div v-else>
            <!-- Si aucun document n'est requis -->
            <div v-if="selectedCategory.docs.length === 0" class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300">
              <i class="pi pi-info-circle text-5xl text-gray-400 mb-4"></i>
              <p class="text-gray-600 text-lg">Aucun document de ce type n'est paramétré ou requis pour les postes sélectionnés.</p>
              <Button 
                label="Signaler au Superviseur (Plan manquant)" 
                icon="pi pi-exclamation-triangle" 
                severity="danger" 
                outlined
                class="mt-6"
                @click="signalerPlanManquant(selectedCategory.title)" 
                :loading="isSignalingPlan" 
              />
            </div>
            
            <!-- Si plusieurs documents -->
            <div v-else>
                <p class="mb-5 text-gray-600">Veuillez sélectionner le formulaire à ouvrir dans la liste ci-dessous :</p>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
                  <div v-for="doc in selectedCategory.docs" :key="doc.id" class="border rounded-xl p-5 shadow-sm hover:shadow-md transition-shadow relative" :class="doc.estTermine ? 'border-green-200 bg-green-50' : 'border-orange-200 bg-white'">
                    
                    <div class="absolute top-4 right-4">
                      <i v-if="doc.estTermine" class="pi pi-check-circle text-green-500 text-2xl"></i>
                      <i v-else class="pi pi-exclamation-circle text-orange-400 text-2xl"></i>
                    </div>
                    
                    <div class="mb-4 pr-8">
                      <Tag v-if="doc.posteCode" :value="'Poste ' + doc.posteCode" severity="info" class="mb-3" />
                      <Tag v-else value="Global OF" severity="secondary" class="mb-3" />
                      <h4 class="font-bold text-gray-800 text-lg leading-tight">{{ doc.libelleFormulaire || 'Formulaire Sans Nom' }}</h4>
                    </div>
                    
                    <div class="flex justify-between items-end mt-4 pt-4 border-t" :class="doc.estTermine ? 'border-green-200' : 'border-gray-100'">
                      <div class="text-sm font-medium" :class="doc.estTermine ? 'text-green-600' : 'text-orange-600'">
                        <span v-if="doc.estTermine"><i class="pi pi-check mr-1"></i> Terminé</span>
                        <span v-else><i class="pi pi-clock mr-1"></i> À Remplir</span>
                      </div>
                      <div class="flex gap-2">
                        <Button v-if="!doc.estTermine" icon="pi pi-check" v-tooltip.top="'Forcer à terminé'" severity="success" outlined size="small" @click="marquerTermine(doc.id)" />
                        <Button :label="doc.estTermine ? 'Consulter' : 'Ouvrir'" :icon="doc.estTermine ? 'pi pi-eye' : 'pi pi-pencil'" :severity="doc.estTermine ? 'secondary' : 'primary'" size="small" @click="() => { showListDialog = false; ouvrirDocument(doc); }" />
                      </div>
                    </div>
                  </div>
                </div>
            </div>
          </div>
        </div>
      </Dialog>

      <!-- Dialog Echantillonnage: NbPostes -->
      <Dialog 
        v-model:visible="showNbPostesDialog" 
        modal 
        header="Fiche Échantillonnage" 
        :style="{ width: '400px' }"
        :closable="!isInitializingEchantillonnage"
      >
        <div class="flex flex-col gap-4 mt-2">
          <p class="text-gray-600">Veuillez renseigner le nombre de postes concernés par cet OF pour calculer l'effectif par poste.</p>
          
          <div class="flex flex-col gap-2">
            <label for="nbPostes" class="font-semibold text-gray-800">Nombre de postes</label>
            <InputNumber 
              id="nbPostes" 
              v-model="nbPostesInput" 
              :min="1" 
              :max="50"
              showButtons
              buttonLayout="horizontal"
              decrementButtonClass="p-button-secondary"
              incrementButtonClass="p-button-secondary"
              incrementButtonIcon="pi pi-plus"
              decrementButtonIcon="pi pi-minus"
              class="w-full"
            />
          </div>
        </div>
        
        <template #footer>
          <div class="flex justify-end gap-2 mt-4">
            <Button 
              label="Annuler" 
              icon="pi pi-times" 
              @click="showNbPostesDialog = false" 
              class="p-button-text p-button-secondary"
              :disabled="isInitializingEchantillonnage"
            />
            <Button 
              label="Générer Fiche" 
              icon="pi pi-check" 
              @click="initializeEchantillonnage" 
              :loading="isInitializingEchantillonnage"
              class="p-button-primary"
            />
          </div>
        </template>
      </Dialog>
    </div>
  </div>
</template>

<style scoped>
/* Les styles additionnels ne sont plus vraiment nécessaires grâce à Tailwind */
</style>
