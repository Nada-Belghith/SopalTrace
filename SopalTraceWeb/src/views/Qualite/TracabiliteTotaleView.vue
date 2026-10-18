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
          <i class="pi pi-file"></i> Disponibles : <span class="text-emerald-900">{{ compteurs.disponibles }}</span>
        </div>
        <div v-if="compteurs.manquants" class="bg-red-100 text-red-700 px-5 py-2.5 rounded-xl border border-red-200 shadow-sm flex items-center gap-2">
          <i class="pi pi-exclamation-triangle"></i> Manquants : <span class="text-red-900">{{ compteurs.manquants }}</span>
        </div>
      </div>
    </div>

    <!-- État de chargement -->
    <div v-if="chargement" class="flex-1 flex flex-col items-center justify-center py-12">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-8 w-full max-w-4xl mb-8">
        <div v-for="(b, i) in BRANCHES" :key="b.libelle" class="flex flex-col items-center text-center animate-pulse-fast" :style="{ animationDelay: `${i * 150}ms` }">
          <div class="w-12 h-12 rounded-full flex items-center justify-center mb-3" :class="b.fond">
            <i class="pi text-xl" :class="[b.icone, b.couleur]"></i>
          </div>
          <span class="text-sm font-bold text-slate-600">{{ b.libelle }}…</span>
        </div>
      </div>
      <div class="text-slate-400 font-medium">Analyse en cascade en cours...</div>
    </div>

    <!-- Dossier de l'OF -->
    <div v-else-if="resultat" class="flex-1 grid grid-cols-1 lg:grid-cols-3 gap-6 animate-fade-in-up pb-10">

      <!-- Colonne 1 : Fournisseurs — par lot servi à l'OF (étiquettes scannées par le magasinier) -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden flex flex-col max-h-[800px] overflow-y-auto custom-scrollbar">
        <div class="absolute inset-0 bg-gradient-to-br from-violet-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-violet-100 text-violet-600 flex items-center justify-center shrink-0">
            <i class="pi pi-truck text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Fournisseurs & Certificats</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Amont · lots servis par le magasin</p>
          </div>
        </div>

        <div class="flex flex-col gap-5 relative z-10">
          <p v-if="!externes.fournisseurs.length" class="m-0 text-xs text-slate-400">Aucun lot scanné par le magasinier pour cet OF.</p>
          <template v-for="(lot, index) in externes.fournisseurs" :key="`${lot.codeArticle}|${lot.numeroLot}`">
            <div v-if="index > 0" class="h-px bg-slate-200"></div>
            <div>
              <h3 @click="basculer(cleLot(lot))" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between gap-2 cursor-pointer hover:text-slate-600 transition-colors">
                <span class="flex items-center gap-2 min-w-0">
                  <i class="pi pi-box shrink-0"></i>
                  <span class="truncate" :title="lot.designationArticle">{{ lot.codeArticle }} · Lot {{ lot.numeroLot }}</span>
                </span>
                <span class="flex items-center gap-2 shrink-0">
                  <span v-if="manquantsDe(lot.documents)" class="px-2 py-0.5 rounded-full bg-red-100 text-red-600 text-[10px] font-black">{{ manquantsDe(lot.documents) }}</span>
                  <i :class="ouvertes[cleLot(lot)] ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
                </span>
              </h3>
              <div v-show="ouvertes[cleLot(lot)]" class="flex flex-col gap-3">
                <p class="m-0 text-xs text-slate-500">
                  {{ lot.designationArticle }}<template v-if="lot.numeroRapport"> · rapport {{ lot.numeroRapport }}</template><template v-if="lot.numeroCommandeFournisseur"> · commande {{ lot.numeroCommandeFournisseur }}</template>
                </p>
                <DocumentCard v-for="c in lot.documents" :key="c.typeCode" :title="titreCarte(c)" :subtitle="sousTitreCarte(c)"
                              :category="c.typeLibelle" :icon="ICONES_EXTERNES[c.typeCode]" :status="etatExterne(c)" @click="ouvrirExterne(c)" />
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- Colonne 2 : Interne / Production — documents de l'OF, par opération -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden flex flex-col max-h-[800px] overflow-y-auto custom-scrollbar">
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
                  <span class="px-2 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10px] font-black">{{ operation.documents.length + operation.externes.length }}</span>
                  <i :class="ouvertes[operation.code] ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
                </span>
              </h3>
              <div v-show="ouvertes[operation.code]" class="flex flex-col gap-3">
                <p v-if="!operation.documents.length && !operation.externes.length" class="m-0 text-xs text-slate-400">Aucun document pour cette opération.</p>
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
                <DocumentCard v-for="c in operation.externes" :key="c.typeCode" :title="titreCarte(c)" :subtitle="sousTitreCarte(c)"
                              :category="c.typeLibelle" :icon="ICONES_EXTERNES[c.typeCode]" :status="etatExterne(c)" @click="ouvrirExterne(c)" />
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- Colonne 3 : Clients — par BL sur lequel l'OF a été expédié -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden flex flex-col max-h-[800px] overflow-y-auto custom-scrollbar">
        <div class="absolute inset-0 bg-gradient-to-br from-emerald-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-emerald-100 text-emerald-600 flex items-center justify-center shrink-0">
            <i class="pi pi-send text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Expédition & Clients</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Aval · BL de l'expédition</p>
          </div>
        </div>

        <div class="flex flex-col gap-5 relative z-10">
          <p v-if="!externes.clients.length" class="m-0 text-xs text-slate-400">Cet OF n'a pas encore été expédié (aucun BL scanné).</p>
          <template v-for="(bl, index) in externes.clients" :key="bl.numeroBl">
            <div v-if="index > 0" class="h-px bg-slate-200"></div>
            <div>
              <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">BL {{ bl.numeroBl }}</h3>
              <p class="m-0 mb-3 text-xs text-slate-500">
                Client {{ bl.codeClient }} · expédié le {{ date(bl.dateExpedition) }}<template v-if="bl.numeroCommande"> · commande {{ bl.numeroCommande }}</template><template v-if="bl.referenceCommandeClient"> · réf. client {{ bl.referenceCommandeClient }}</template>
              </p>
              <div class="flex flex-col gap-3">
                <DocumentCard v-for="c in bl.documents" :key="c.typeCode" :title="titreCarte(c)" :subtitle="sousTitreCarte(c)"
                              :category="c.typeLibelle" :icon="ICONES_EXTERNES[c.typeCode]" :status="etatExterne(c)" @click="ouvrirExterne(c)" />
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>

    <!-- Document externe manquant : la raison et qui doit le déposer -->
    <Dialog :visible="!!externeManquant" modal :header="externeManquant?.typeLibelle" :style="{ width: '32rem' }" :breakpoints="{ '640px': '95vw' }"
            @update:visible="valeur => { if (!valeur) externeManquant = null }">
      <div v-if="externeManquant" class="flex flex-col gap-3 text-sm">
        <p class="m-0 text-red-700"><i class="pi pi-exclamation-triangle mr-1"></i>{{ externeManquant.raisonManquant }}</p>
        <p v-if="externeManquant.nomFichier" class="m-0 text-slate-600">Nom attendu : <span class="font-mono font-bold">{{ externeManquant.nomFichier }}</span></p>
      </div>
    </Dialog>

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
 * Traçabilité par OF (responsable qualité) : le dossier documentaire d'un ordre de fabrication en trois branches, toutes
 * réelles. Fournisseurs : pour chaque lot servi (étiquette scannée par le magasinier), son rapport de contrôle, le bon
 * de commande fournisseur et le certificat de conformité retrouvés par le n° de commande du rapport. Production : les
 * documents internes de l'OF par opération (plans appliqués, fiches remplies), puis pour un produit fini son plan de
 * définition (dessin d'ensemble du service DI) et son rapport de contrôle rapide.
 * Clients : pour chaque BL de l'expédition, le BL et le bon de commande client retrouvé par la référence client de la
 * commande Sage du BL. Chaque document est disponible ou
 * manquant, avec la raison.
 */
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Dialog from 'primevue/dialog'
import DocumentCard from '@/components/Qualite/TracabiliteTotale/DocumentCard.vue'
import ActionsDocument from '@/components/Qualite/ActionsDocument.vue'
import LienRattacherModele from '@/components/Qualite/LienRattacherModele.vue'
import PdfDocumentQualite from '@/components/Qualite/PdfDocumentQualite.vue'
import rapportsService, { ouvrirPdf } from '@/services/rapportsService'
import documentsExternesService from '@/services/documentsExternesService'

const route = useRoute()
const router = useRouter()

const recherche = ref(route.query.of ?? '')
const resultat = ref(null)
const chargement = ref(false)
const erreur = ref('')
const selection = ref(null)
const externeManquant = ref(null)

// Sections ouvertes ou fermées (lots, opérations).
const ouvertes = reactive({})
const basculer = (code) => { ouvertes[code] = !ouvertes[code] }

const BRANCHES = [
  { libelle: 'Branche Fournisseurs', icone: 'pi-truck', fond: 'bg-violet-100', couleur: 'text-violet-600' },
  { libelle: 'Branche Interne', icone: 'pi-cog', fond: 'bg-blue-100', couleur: 'text-blue-600' },
  { libelle: 'Branche Clients', icone: 'pi-send', fond: 'bg-emerald-100', couleur: 'text-emerald-600' }
]
const STATUTS = { ACTIF: 'Actif', ARCHIVE: 'Archivé', BROUILLON: 'Brouillon', EN_COURS: 'En cours', TERMINE: 'Terminé', CLOTURE: 'OF clôturé' }
// Opérations de la gamme, dans l'ordre de fabrication, puis le contrôle du produit fini.
const OPERATIONS = [
  { code: 'ESTOMP', libelle: 'Estompage', couleur: 'bg-orange-400' },
  { code: 'TRONC', libelle: 'Tronçonnage', couleur: 'bg-rose-400' },
  { code: 'USINAG', libelle: 'Usinage', couleur: 'bg-cyan-400' },
  { code: 'ASS', libelle: 'Assemblage', couleur: 'bg-blue-500' },
  { code: 'PF', libelle: 'Produit fini', couleur: 'bg-emerald-500' }
]
const ICONES_EXTERNES = {
  CONTROL_REPORT: 'pi pi-file-check',
  CONFORMITY_CERTIFICATE: 'pi pi-verified',
  SUPPLIER_ORDER: 'pi pi-shopping-cart',
  DELIVERY_NOTE: 'pi pi-file-export',
  CUSTOMER_ORDER: 'pi pi-envelope',
  FINISHED_PRODUCT_CONTROL_REPORT: 'pi pi-check-square',
  FINISHED_PRODUCT_DEFINITION_PLAN: 'pi pi-file-edit'
}

const cle = (d) => d.documentEnteteId ?? d.execControleDocumentStatutId
const cleLot = (lot) => `lot|${lot.codeArticle}|${lot.numeroLot}`
const formaterDate = (d) => d ? new Date(d).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' }) : ''
const date = (d) => d ? new Date(d).toLocaleDateString('fr-FR') : ''
const sousTitre = (d) => d.etat === 'MANQUANT' && d.raisonManquant
  ? d.raisonManquant
  : [d.formulaire, d.poste && `Poste ${d.poste}`, d.machine && `Machine ${d.machine}`, formaterDate(d.date)].filter(Boolean).join(' · ')
const icone = (d) => d.categorie === 'QUALITE' ? 'pi pi-sitemap' : d.typeCode === 'REGISTRE_TRACA' ? 'pi pi-list' : 'pi pi-check-square'

// Documents externes : nom du fichier déposé (ou attendu), raison quand il manque.
const titreCarte = (c) => c.nomFichier ?? c.typeLibelle
const sousTitreCarte = (c) => c.etat === 'MANQUANT'
  ? c.raisonManquant
  : `Déposé le ${formaterDate(c.deposeLe)}${c.deposeParNom ? ` par ${c.deposeParNom}` : ''}`
const etatExterne = (c) => c.etat === 'MANQUANT' ? 'manquant' : 'disponible'
const manquantsDe = (cartes) => cartes.filter(c => c.etat === 'MANQUANT').length

const externes = computed(() => resultat.value?.externes ?? { fournisseurs: [], clients: [], produitFini: [] })

// Plans qualité appliqués d'abord, puis les documents remplis dans l'ordre de l'OF ; le produit fini reçoit son plan de
// définition et son rapport de contrôle rapide (documents externes).
const operations = computed(() => {
  const documents = resultat.value?.documents ?? []
  const produitFini = externes.value.produitFini
  const connues = OPERATIONS
    .map(o => ({
      ...o,
      libelle: documents.find(d => d.operation === o.code)?.operationLibelle ?? o.libelle,
      documents: documents.filter(d => d.operation === o.code),
      externes: o.code === 'PF' ? produitFini : []
    }))
    // La rubrique produit fini n'existe que pour un OF de produit fini.
    .filter(o => o.code !== 'PF' || o.documents.length || o.externes.length)
  const autres = documents.filter(d => !OPERATIONS.some(o => o.code === d.operation))
  return autres.length ? [...connues, { code: 'AUTRES', libelle: 'Autres documents', couleur: 'bg-slate-400', documents: autres, externes: [] }] : connues
})
// État donné par le serveur : disponible (son PDF peut être produit, ou consultable) ou manquant, avec la raison.
const etatCarte = (d) => d.etat === 'MANQUANT' ? 'manquant' : 'disponible'

const toutesLesCartes = computed(() => [
  ...(resultat.value?.documents ?? []),
  ...externes.value.fournisseurs.flatMap(l => l.documents),
  ...externes.value.clients.flatMap(b => b.documents),
  ...externes.value.produitFini
])
const compteurs = computed(() => ({
  manquants: toutesLesCartes.value.filter(d => d.etat === 'MANQUANT').length,
  disponibles: toutesLesCartes.value.filter(d => d.etat !== 'MANQUANT').length
}))

const ouvrirExterne = async (c) => {
  if (!c.documentId) {
    externeManquant.value = c
    return
  }
  try {
    await ouvrirPdf(() => documentsExternesService.fichier(c.documentId), c.nomFichier)
  } catch (e) {
    erreur.value = e.message
  }
}

const tracer = async () => {
  const numero = recherche.value.trim()
  if (!numero) return
  chargement.value = true
  erreur.value = ''
  resultat.value = null
  try {
    resultat.value = await rapportsService.getTracabilite(numero)
    for (const o of operations.value) ouvertes[o.code] = o.documents.length + o.externes.length > 0
    for (const lot of externes.value.fournisseurs) ouvertes[cleLot(lot)] = true
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
