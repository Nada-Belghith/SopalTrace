<template>
  <div class="min-h-screen bg-slate-50 flex flex-col p-6 animate-fade-in">
    <!-- En-tête / Barre de recherche -->
    <div class="mb-8 flex flex-col items-center">
      <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mb-4 shadow-sm relative">
        <div class="absolute inset-0 rounded-full border-4 border-blue-500 border-t-transparent animate-spin" v-if="chargement"></div>
        <i class="pi pi-box text-3xl text-blue-600 relative z-10"></i>
      </div>
      <h1 class="text-3xl font-black text-slate-800 mb-2">Traçabilité Totale</h1>
      <p class="text-slate-500 mb-6 text-center max-w-lg">
        Recherchez un numéro d'Ordre de Fabrication (OF) pour consulter l'intégralité du dossier documentaire associé en cascade.
      </p>

      <form class="relative w-full max-w-2xl group" @submit.prevent="tracer">
        <div class="absolute inset-y-0 left-0 flex items-center pl-4 pointer-events-none">
          <i class="pi pi-search text-slate-400 group-focus-within:text-blue-500 transition-colors"></i>
        </div>
        <input
          v-model="recherche"
          type="text"
          class="w-full pl-12 pr-32 py-4 rounded-2xl border-2 border-slate-200 bg-white text-lg font-bold text-slate-800 shadow-sm focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-300 placeholder:font-normal"
          placeholder="Saisir le N° OF (ex: OF2607ASS02)..."
        >
        <button
          type="submit"
          :disabled="chargement || !recherche.trim()"
          class="absolute inset-y-2 right-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-bold px-6 rounded-xl transition-all hover:shadow-lg active:scale-95 flex items-center gap-2 border-0 cursor-pointer"
        >
          <span>Tracer</span>
          <i class="pi pi-arrow-right text-sm"></i>
        </button>
      </form>

      <p v-if="erreur" class="mt-4 mb-0 rounded-xl border border-red-200 bg-red-50 text-red-800 px-4 py-3 text-sm max-w-2xl w-full">{{ erreur }}</p>

      <!-- Informations de l'OF (affichées après recherche) -->
      <div v-if="resultat" class="mt-6 flex flex-wrap gap-4 text-sm font-bold animate-fade-in justify-center w-full max-w-2xl">
        <div class="bg-blue-100 text-blue-700 px-5 py-2.5 rounded-xl border border-blue-200 shadow-sm flex items-center gap-2">
          <i class="pi pi-box"></i> OF : <span class="text-blue-900">{{ resultat.numeroOf }}</span>
        </div>
        <div class="bg-emerald-100 text-emerald-700 px-5 py-2.5 rounded-xl border border-emerald-200 shadow-sm flex items-center gap-2">
          <i class="pi pi-file"></i> Documents : <span class="text-emerald-900">{{ resultat.documents.length }}</span>
        </div>
        <div v-if="manquants" class="bg-red-100 text-red-700 px-5 py-2.5 rounded-xl border border-red-200 shadow-sm flex items-center gap-2">
          <i class="pi pi-exclamation-triangle"></i> Manquants : <span class="text-red-900">{{ manquants }}</span>
        </div>
      </div>
    </div>

    <!-- État de chargement (Simulateur de branches) -->
    <div v-if="chargement" class="flex-1 flex flex-col items-center justify-center py-12">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-8 w-full max-w-4xl mb-8">
        <!-- Branche 1 -->
        <div class="flex flex-col items-center text-center animate-pulse-fast">
          <div class="w-12 h-12 rounded-full bg-violet-100 flex items-center justify-center mb-3">
            <i class="pi pi-truck text-xl text-violet-600"></i>
          </div>
          <span class="text-sm font-bold text-slate-600">Branche Fournisseurs...</span>
        </div>
        <!-- Branche 2 -->
        <div class="flex flex-col items-center text-center animate-pulse-fast" style="animation-delay: 150ms;">
          <div class="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center mb-3">
            <i class="pi pi-cog text-xl text-blue-600"></i>
          </div>
          <span class="text-sm font-bold text-slate-600">Branche Interne...</span>
        </div>
        <!-- Branche 3 -->
        <div class="flex flex-col items-center text-center animate-pulse-fast" style="animation-delay: 300ms;">
          <div class="w-12 h-12 rounded-full bg-emerald-100 flex items-center justify-center mb-3">
            <i class="pi pi-send text-xl text-emerald-600"></i>
          </div>
          <span class="text-sm font-bold text-slate-600">Branche Clients...</span>
        </div>
      </div>
      <div class="text-slate-400 font-medium">Analyse en cascade en cours...</div>
    </div>

    <!-- Dashboard de Traçabilité -->
    <div v-else-if="resultat" class="flex-1 grid grid-cols-1 lg:grid-cols-3 gap-6 animate-fade-in-up pb-10">

      <!-- Colonne 1: Fournisseurs -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden group flex flex-col max-h-[800px] overflow-y-auto">
        <div class="absolute inset-0 bg-gradient-to-br from-violet-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-violet-100 text-violet-600 flex items-center justify-center shrink-0">
            <i class="pi pi-truck text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Fournisseurs & Certificat</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Amont · <span class="font-semibold text-amber-600">exemple, non relié aux données</span></p>
          </div>
        </div>
        
        <div class="flex flex-col gap-5 relative z-10">
          <!-- Section MP -->
          <div>
            <h3 @click="toggleSection('mp')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><i class="pi pi-box"></i> Matière Première (MP)</span>
              <i :class="expandedSections.mp ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.mp" class="flex flex-col gap-3">
              <DocumentCard 
                title="Certificat Conformité.pdf" 
                subtitle="Laiton CuZn39Pb3" 
                category="Certif MP" 
                icon="pi pi-file-pdf" 
                status="disponible"
                imagePath="/certificat_mp.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Rapport Contrôle MP.pdf" 
                subtitle="Analyse spectro" 
                category="Rapport MP" 
                icon="pi pi-file-pdf" 
                status="disponible"
                imagePath="/rapport_mp.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande FRS.pdf" 
                subtitle="Introuvable pour cette commande" 
                category="BC Frs" 
                icon="pi pi-file-pdf" 
                status="manquant"
                @click="handleDocClick"
              />
            </div>
          </div>
          
          <div class="h-px bg-slate-200"></div>

          <!-- Section Accessoire: Ressort -->
          <div>
            <h3 @click="toggleSection('acc_ressort')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><i class="pi pi-paperclip"></i> Accessoire : Ressort</span>
              <i :class="expandedSections.acc_ressort ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.acc_ressort" class="flex flex-col gap-3">
              <DocumentCard 
                title="Certificat d'origine.pdf" 
                subtitle="Ressort INOX 304" 
                category="Certif Acc" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Rapport Contrôle Réception.pdf" 
                subtitle="Réf: 11022" 
                category="Rapport" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande FRS.pdf" 
                subtitle="Cmd: SOPCAC-2410100" 
                category="BC Frs" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>

          <div class="h-px bg-slate-200"></div>

          <!-- Section Accessoire: Joint -->
          <div>
            <h3 @click="toggleSection('acc_joint')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><i class="pi pi-paperclip"></i> Accessoire : Joint Torique</span>
              <i :class="expandedSections.acc_joint ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.acc_joint" class="flex flex-col gap-3">
              <DocumentCard 
                title="Certificat Matière Joint.pdf" 
                subtitle="Introuvable pour ce lot" 
                category="Certificat" 
                icon="pi pi-file-check" 
                status="manquant"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Rapport Contrôle Réception.pdf" 
                subtitle="Réf: 11055" 
                category="Rapport" 
                icon="pi pi-chart-bar" 
                status="disponible"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande FRS.pdf" 
                subtitle="Cmd: BC-ACC-502" 
                category="BC Frs" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Colonne 2: Interne / Production — documents de l'OF, par opération -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden group flex flex-col max-h-[800px] overflow-y-auto custom-scrollbar">
        <div class="absolute inset-0 bg-gradient-to-br from-blue-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-blue-100 text-blue-600 flex items-center justify-center shrink-0">
            <i class="pi pi-cog text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Production Sopal</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Interne · documents de l'OF</p>
          </div>
        </div>

        <div class="flex flex-col gap-5 relative z-10">
          <template v-for="(operation, index) in operations" :key="operation.code">
            <div v-if="index > 0" class="h-px bg-slate-200"></div>
            <div>
              <h3 @click="basculer(operation.code)" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
                <span class="flex items-center gap-2"><span class="w-2 h-2 rounded-full" :class="operation.couleur"></span> {{ operation.libelle }} ({{ operation.code }})</span>
                <span class="flex items-center gap-2">
                  <span class="px-2 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10px] font-black">{{ operation.documents.length }}</span>
                  <i :class="ouvertes[operation.code] ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
                </span>
              </h3>
              <div v-show="ouvertes[operation.code]" class="flex flex-col gap-3">
                <p v-if="!operation.documents.length" class="m-0 text-xs text-slate-400">Aucun document pour cette opération.</p>
                <DocumentCard
                  v-for="d in operation.documents"
                  :key="cle(d)"
                  :title="d.nom"
                  :subtitle="sousTitre(d)"
                  :category="d.typeLibelle"
                  :icon="icone(d)"
                  :status="etatCarte(d)"
                  @click="selection = d"
                />
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- Colonne 3: Clients / Expédition -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden group flex flex-col max-h-[800px] overflow-y-auto">
        <div class="absolute inset-0 bg-gradient-to-br from-emerald-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-emerald-100 text-emerald-600 flex items-center justify-center shrink-0">
            <i class="pi pi-send text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Expédition & Clients</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Aval · <span class="font-semibold text-amber-600">exemple, non relié aux données</span></p>
          </div>
        </div>

        <div class="flex flex-col gap-5 relative z-10">
          <div>
            <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3">Bons de Livraison (BL)</h3>
            <div class="flex flex-col gap-3">
              <DocumentCard 
                title="Bon de Livraison BL-2409-55.pdf" 
                subtitle="Client: Magasin Central Sfax" 
                category="BL" 
                icon="pi pi-file-export" 
                status="disponible"
                imagePath="/bl.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Livraison BL-2409-58.pdf" 
                subtitle="Client: Magasin Central Tunis" 
                category="BL" 
                icon="pi pi-file-export" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>

          <div class="h-px bg-slate-200"></div>
          
          <div>
            <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3">Bons de Commande Clients</h3>
            <div class="flex flex-col gap-3">
              <DocumentCard 
                title="Bon de Commande Client.pdf" 
                subtitle="Introuvable (BC-CLI-909)" 
                category="BC Client" 
                icon="pi pi-envelope" 
                status="manquant"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande Client.pdf" 
                subtitle="Introuvable (BC-CLI-915)" 
                category="BC Client" 
                icon="pi pi-envelope" 
                status="manquant"
                @click="handleDocClick"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Aperçu d'un document de l'OF : son modèle et ses actions (PDF, impression, Excel) -->
    <Dialog :visible="!!selection" modal :header="selection?.nom" :style="{ width: '40rem' }" :breakpoints="{ '640px': '95vw' }"
            @update:visible="valeur => { if (!valeur) selection = null }">
      <div v-if="selection" class="flex flex-col gap-4 text-sm">
        <dl class="m-0 grid grid-cols-[auto,1fr] gap-x-4 gap-y-2">
          <dt class="font-semibold text-slate-500">Type</dt><dd class="m-0 text-slate-800">{{ selection.typeLibelle }}</dd>
          <template v-if="selection.formulaire"><dt class="font-semibold text-slate-500">Formulaire</dt><dd class="m-0 text-slate-800">{{ selection.formulaire }}</dd></template>
          <template v-if="selection.version != null"><dt class="font-semibold text-slate-500">Version</dt><dd class="m-0 text-slate-800">V{{ selection.version }}</dd></template>
          <template v-if="selection.poste"><dt class="font-semibold text-slate-500">Poste</dt><dd class="m-0 text-slate-800">{{ selection.poste }}</dd></template>
          <template v-if="selection.machine"><dt class="font-semibold text-slate-500">Machine</dt><dd class="m-0 text-slate-800">{{ selection.machine }}</dd></template>
          <dt class="font-semibold text-slate-500">Statut</dt><dd class="m-0 text-slate-800">{{ STATUTS[selection.statut] ?? selection.statut }}</dd>
          <template v-if="selection.date"><dt class="font-semibold text-slate-500">Date</dt><dd class="m-0 text-slate-800">{{ formaterDate(selection.date) }}</dd></template>
        </dl>
        <!-- Plan qualité : son PDF (s'il en a un) et la fiche que remplissent les opérateurs -->
        <PdfDocumentQualite v-if="selection.categorie === 'QUALITE'" :document-id="selection.documentEnteteId" />
        <template v-else>
          <p v-if="selection.modele" class="m-0 text-xs text-slate-500">
            <i class="pi pi-file-excel text-emerald-600 mr-1"></i>Modèle {{ selection.modele.codeModele }} V{{ selection.modele.numeroVersion }} · {{ selection.modele.explication }}
          </p>
          <template v-else>
            <p class="m-0 text-xs text-amber-700">
              {{ selection.messageModeleManquant || (selection.contratDisponible ? 'Aucun modèle publié pour ce type de document.' : "L'Excel de ce document n'a pas encore été fourni.") }}
            </p>
            <LienRattacherModele v-if="selection.contratDisponible" :source="selection" />
          </template>
          <ActionsDocument v-if="selection.modele" :source="selection" />
        </template>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
/**
 * Traçabilité par OF : le dossier documentaire d'un ordre de fabrication en trois branches. La branche Production Sopal
 * présente les vrais documents de l'OF (plans qualité appliqués et documents remplis par les opérateurs), par
 * opération ; chacun est mis en forme avec le modèle en vigueur à sa création. Les branches Fournisseurs et Clients sont
 * des exemples, non reliés aux données (modules à venir).
 */
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Dialog from 'primevue/dialog'
import DocumentCard from '@/components/Qualite/TracabiliteTotale/DocumentCard.vue'
import ActionsDocument from '@/components/Qualite/ActionsDocument.vue'
import LienRattacherModele from '@/components/Qualite/LienRattacherModele.vue'
import PdfDocumentQualite from '@/components/Qualite/PdfDocumentQualite.vue'
import rapportsService from '@/services/rapportsService'

const route = useRoute()
const router = useRouter()

const recherche = ref(route.query.of ?? '')
const resultat = ref(null)
const chargement = ref(false)
const erreur = ref('')
const selection = ref(null)

// Sections des branches d'exemple (fournisseurs) et des opérations : ouvertes ou fermées.
const expandedSections = reactive({ mp: true, acc_ressort: false, acc_joint: false })
const toggleSection = (cle) => { expandedSections[cle] = !expandedSections[cle] }
const ouvertes = reactive({})
const basculer = (code) => { ouvertes[code] = !ouvertes[code] }
// Document de l'exemple (fournisseurs, clients) : pas de fichier relié.
const handleDocClick = () => {}

const STATUTS = { ACTIF: 'Actif', ARCHIVE: 'Archivé', BROUILLON: 'Brouillon', EN_COURS: 'En cours', TERMINE: 'Terminé', CLOTURE: 'OF clôturé' }
// Opérations de la gamme, dans l'ordre de fabrication.
const OPERATIONS = [
  { code: 'ESTOMP', libelle: 'Estompage', couleur: 'bg-orange-400' },
  { code: 'TRONC', libelle: 'Tronçonnage', couleur: 'bg-rose-400' },
  { code: 'USINAG', libelle: 'Usinage', couleur: 'bg-cyan-400' },
  { code: 'ASS', libelle: 'Assemblage', couleur: 'bg-blue-500' }
]

const cle = (d) => d.documentEnteteId ?? d.execControleDocumentStatutId
const formaterDate = (d) => d ? new Date(d).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' }) : ''
const sousTitre = (d) => d.etat === 'MANQUANT' && d.raisonManquant
  ? d.raisonManquant
  : [d.formulaire, d.poste && `Poste ${d.poste}`, d.machine && `Machine ${d.machine}`, formaterDate(d.date)].filter(Boolean).join(' · ')
const icone = (d) => d.categorie === 'QUALITE' ? 'pi pi-sitemap' : d.typeCode === 'REGISTRE_TRACA' ? 'pi pi-list' : 'pi pi-check-square'

// Plans qualité appliqués d'abord, puis les documents remplis dans l'ordre de l'OF.
const operations = computed(() => {
  const documents = resultat.value?.documents ?? []
  const parOperation = (code) => documents.filter(d => d.operation === code)
  const connues = OPERATIONS.map(o => ({
    ...o,
    libelle: documents.find(d => d.operation === o.code)?.operationLibelle ?? o.libelle,
    documents: parOperation(o.code)
  }))
  const autres = documents.filter(d => !OPERATIONS.some(o => o.code === d.operation))
  return autres.length ? [...connues, { code: 'AUTRES', libelle: 'Autres documents', couleur: 'bg-slate-400', documents: autres }] : connues
})
// État donné par le serveur : disponible (son PDF peut être produit) ou manquant (pas de modèle, ou document sans fiche
// remplie dans l'OF), avec la raison.
const etatCarte = (d) => d.etat === 'MANQUANT' ? 'manquant' : 'disponible'
const manquants = computed(() => (resultat.value?.documents ?? []).filter(d => d.etat === 'MANQUANT').length)

const tracer = async () => {
  const numero = recherche.value.trim()
  if (!numero) return
  chargement.value = true
  erreur.value = ''
  resultat.value = null
  try {
    resultat.value = await rapportsService.getTracabilite(numero)
    for (const o of operations.value) ouvertes[o.code] = o.documents.length > 0
    router.replace({ query: { of: numero } })
  } catch (e) {
    erreur.value = e.message
  } finally {
    chargement.value = false
  }
}

if (recherche.value) tracer()
</script>

<style scoped>
.animate-fade-in {
  animation: fadeIn 0.5s ease-out forwards;
}
.animate-fade-in-up {
  animation: fadeInUp 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}
.animate-pulse-fast {
  animation: pulse 1s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.7; transform: scale(0.95); }
}
</style>
