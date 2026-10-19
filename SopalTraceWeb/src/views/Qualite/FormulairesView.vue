<script setup>
/**
 * Formulaires : chaque formulaire (code + version) désigne, par son type de document et son champ d'application
 * (famille, poste, machine…), le formulaire des documents. Le superviseur qualité les crée (assistant en trois étapes,
 * avec leur version de départ) et change leur code ; l'administrateur leur rattache leur modèle Excel (page Modèles
 * Excel). Un formulaire utilisé garde son identité : changer de code crée une nouvelle ligne (même version, même
 * modèle repris), l'ancienne et ses documents restent archivés tels quels. Les types de document, leur nature
 * (document ou fiche de réponse) et leurs critères viennent de la base (table FormRole).
 * Modèles : une nouvelle version aux mêmes colonnes reprend le modèle ; des colonnes changées envoient une demande
 * à l'administrateur. Le superviseur peut demander un nouveau modèle (logo, mise en page) : même version (seuls les
 * documents créés ensuite le prennent) ou version suivante.
 */
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import EnTetePage from '@/components/Shared/EnTetePage.vue'
import { useAppToast } from '@/composables/useAppToast'
import { confirmer } from '@/composables/useConfirmation'
import rapportsService from '@/services/rapportsService'
import { NATURES_FORMULAIRE, STATUTS_DEMANDE, libelleDemande } from '@/constants/demandesModele'

const toast = useAppToast()
const route = useRoute()

const roles = ref([])                    // [{ role, libelle, modeles, formulaires }]
const rolesReference = ref([])           // types de document (FormRole) et leurs critères permis
const chargement = ref(true)
const actionEnCours = ref(null)
const formulaireMisEnAvant = ref(route.query.formulaire ?? null)
const recherche = ref('')
const filtreRole = ref('')

// Changement de code : { id, code, documentsActifs: null | [] } — liste chargée avant de confirmer.
const codeEnEdition = ref(null)

// Assistant « Nouveau formulaire » : étape 1 type, 2 champ d'application, 3 code et nom.
const assistant = ref(null)
const listes = ref(null)

// Demande de nouveau modèle : { formulaire, motif, changementVersion } ; historique des demandes.
const demande = ref(null)
const demandes = ref([])
const afficherDemandes = ref(false)

const ETAPES = ['Type de document', "Champ d'application", 'Code et nom']
// Critères d'un champ d'application ; nature et type de robinet ne désignent un formulaire que sans famille.
const CRITERES = [
  { cle: 'famille', titre: 'Famille de produit fini', court: 'famille', liste: 'familles', champ: 'familleProduitFiniCode', permis: (r) => r.parFamille },
  { cle: 'poste', titre: 'Poste de travail', court: 'poste', liste: 'postes', champ: 'posteCode', permis: (r) => r.parPoste },
  { cle: 'machine', titre: 'Machine', court: 'machine', liste: 'machines', champ: 'machineCode', permis: (r) => r.parMachine },
  { cle: 'nature', titre: "Nature d'article (sans famille, ex. PISTON)", court: 'nature', liste: 'natures', champ: 'natureArticleCode',
    permis: (r, a) => r.parNature && !a?.familleProduitFiniCode },
  { cle: 'typeRobinet', titre: 'Type de robinet (sans famille)', court: 'type de robinet', liste: 'typesRobinet', champ: 'typeRobinetCode',
    permis: (r, a) => r.parTypeRobinet && !a?.familleProduitFiniCode }
]
const criteresDuRole = (r) => CRITERES.filter(c => c.permis(r, null))

const roleAssistant = computed(() => rolesReference.value.find(r => r.code === assistant.value?.role) ?? null)
const criteresAssistant = computed(() => roleAssistant.value ? CRITERES.filter(c => c.permis(roleAssistant.value, assistant.value)) : [])
// Avec une famille, le type de robinet est le sien (repris par le serveur, jamais saisi à part).
const typeDeLaFamille = computed(() =>
  listes.value?.familles.find(f => f.code === assistant.value?.familleProduitFiniCode)?.typeRobinet ?? null)
const champAssistant = computed(() => [
  ...criteresAssistant.value.map(c => assistant.value[c.champ] && `${c.court} ${assistant.value[c.champ]}`),
  typeDeLaFamille.value && `type de robinet ${typeDeLaFamille.value} (de la famille)`
].filter(Boolean).join(' · ') || 'aucun critère (un seul formulaire pour ce type)')

// Seuls les types qui ont des formulaires sont listés ; le filtre choisit l'un d'eux.
const rolesAvecFormulaires = computed(() => roles.value.filter(r => r.formulaires.length))
const rolesAffiches = computed(() => {
  const texte = recherche.value.trim().toLowerCase()
  return rolesAvecFormulaires.value
    .filter(r => !filtreRole.value || r.role === filtreRole.value)
    .map(r => ({
      ...r,
      formulaires: r.formulaires.filter(f => !texte || f.codeReference.toLowerCase().includes(texte)
        || (f.champApplication ?? '').toLowerCase().includes(texte))
    }))
    .filter(r => r.formulaires.length)
})
const nombreFormulaires = computed(() => rolesAvecFormulaires.value.reduce((n, r) => n + r.formulaires.length, 0))
const sansModele = computed(() => rolesAvecFormulaires.value.reduce((n, r) => n + r.formulaires.filter(f => !f.modeleActuel).length, 0))

const charger = async () => {
  chargement.value = true
  try {
    const [parRole, reference, toutes] = await Promise.all([
      rapportsService.getFormulairesParRole(), rapportsService.rolesFormulaire(), rapportsService.demandesModele()
    ])
    roles.value = parRole
    rolesReference.value = reference
    demandes.value = toutes
  } catch (e) {
    toast.error(e.message)
  } finally {
    chargement.value = false
  }
  if (filtreRole.value && !rolesAvecFormulaires.value.some(r => r.role === filtreRole.value)) filtreRole.value = ''
  if (formulaireMisEnAvant.value) {
    await nextTick()
    document.getElementById(`formulaire-${formulaireMisEnAvant.value}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
  }
}

// ── Assistant « Nouveau formulaire » ─────────────────────────────────
const VIDE = { familleProduitFiniCode: '', posteCode: '', machineCode: '', natureArticleCode: '', typeRobinetCode: '' }

const ouvrirAssistant = async () => {
  assistant.value = { etape: 1, role: '', ...VIDE, code: '', designation: '', version: 0 }
  if (!listes.value) {
    try {
      listes.value = await rapportsService.listesChampApplication()
    } catch (e) {
      toast.error(e.message)
    }
  }
}

const choisirRole = (code) => {
  assistant.value = { ...assistant.value, role: code, ...VIDE }
  assistant.value.etape = criteresDuRole(roleAssistant.value).length ? 2 : 3
}

// Une famille choisie remplace nature et type de robinet (ils ne désignent un formulaire que sans famille).
watch(() => assistant.value?.familleProduitFiniCode, (famille) => {
  if (famille) Object.assign(assistant.value, { natureArticleCode: '', typeRobinetCode: '' })
})

// Version de départ : un entier de 0 à 999 (celle de la fiche Sopal).
const versionDepartValide = computed(() => Number.isInteger(assistant.value?.version) && assistant.value.version >= 0 && assistant.value.version <= 999)

const creerFormulaire = async () => {
  const a = assistant.value
  actionEnCours.value = 'nouveau-formulaire'
  try {
    const champs = Object.fromEntries(criteresAssistant.value.map(c => [c.champ, a[c.champ] || null]))
    const reponse = await rapportsService.creerFormulaire({
      role: a.role, code: a.code.trim(), designation: a.designation.trim() || null, version: a.version, ...champs
    })
    toast.success(reponse.message)
    assistant.value = null
    formulaireMisEnAvant.value = reponse.data
    filtreRole.value = ''
    await charger()
  } catch (e) {
    toast.error(e.message, 'Création refusée')
  } finally {
    actionEnCours.value = null
  }
}

// ── Changement de code ───────────────────────────────────────────────
const preparerCode = async () => {
  const { id } = codeEnEdition.value
  actionEnCours.value = `code-${id}`
  try {
    const documents = (await rapportsService.documentsActifsFormulaire(id)).data || []
    if (!documents.length) return await enregistrerCode()
    codeEnEdition.value = { ...codeEnEdition.value, documentsActifs: documents }
  } catch (e) {
    toast.error(e.message, 'Code refusé')
  } finally {
    if (actionEnCours.value === `code-${id}`) actionEnCours.value = null
  }
}

const enregistrerCode = async () => {
  const { id, code } = codeEnEdition.value
  actionEnCours.value = `code-${id}`
  try {
    const reponse = await rapportsService.renommerCodeFormulaire(id, code.trim())
    toast.success(reponse.message)
    codeEnEdition.value = null
    formulaireMisEnAvant.value = reponse.data?.formulaireId ?? id
    await charger()
  } catch (e) {
    toast.error(e.message, 'Code refusé')
  } finally {
    actionEnCours.value = null
  }
}

const nomModele = (role, code) => role.modeles.find(m => m.code === code)?.nom ?? code
const nature = (r) => NATURES_FORMULAIRE[r?.nature] ?? ''
const formaterDate = (d) => d ? new Date(d).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' }) : ''
const demandesEnAttente = computed(() => demandes.value.filter(d => d.statut === 'PENDING').length)

// ── Demande de nouveau modèle ────────────────────────────────────────
const ouvrirDemande = (formulaire) => {
  demande.value = { formulaire, raison: 'LOGO', motif: 'Nouveau logo', changementVersion: 'NEXT_VERSION' }
}

// Nouveau logo : toujours la version suivante (document ou fiche). Mise en page ou autre : au choix.
const choisirRaison = (raison) => {
  const d = demande.value
  d.raison = raison
  if (raison === 'LOGO') Object.assign(d, { motif: 'Nouveau logo', changementVersion: 'NEXT_VERSION' })
  else if (d.motif === 'Nouveau logo') d.motif = ''
}

const envoyerDemande = async () => {
  const d = demande.value
  actionEnCours.value = `demande-${d.formulaire.id}`
  try {
    const reponse = await rapportsService.demanderModele(d.formulaire.id, d.motif.trim(), d.changementVersion)
    toast.success(reponse.message)
    demande.value = null
    formulaireMisEnAvant.value = d.formulaire.id
    await charger()
  } catch (e) {
    toast.error(e.message, 'Demande refusée')
  } finally {
    actionEnCours.value = null
  }
}

const annulerDemande = async (d) => {
  const { confirme } = await confirmer({
    titre: 'Annuler la demande',
    message: `La demande de nouveau modèle pour ${d.codeFormulaire}.${d.versionFormulaire} sera annulée (elle reste dans l'historique).`,
    libelleConfirmer: 'Annuler la demande',
    icone: 'pi pi-times'
  })
  if (!confirme) return
  actionEnCours.value = `annuler-${d.id}`
  try {
    toast.success((await rapportsService.annulerDemandeModele(d.id)).message)
    await charger()
  } catch (e) {
    toast.error(e.message)
  } finally {
    actionEnCours.value = null
  }
}

onMounted(charger)
</script>

<template>
  <div class="p-4 md:p-8 max-w-7xl mx-auto flex flex-col gap-6">
    <EnTetePage titre="Formulaires"
                sous-titre="Chaque formulaire (code + version) désigne, par son champ d'application, le formulaire des documents d'un type. L'administrateur lui rattache son modèle Excel.">
      <template #actions>
        <button type="button" @click="ouvrirAssistant"
                class="min-h-[44px] px-4 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-sm flex items-center gap-2 border-0 cursor-pointer">
          <i class="pi pi-plus"></i> Nouveau formulaire
        </button>
      </template>
    </EnTetePage>

    <!-- Assistant : nouveau formulaire -->
    <section v-if="assistant" class="bg-white rounded-2xl border border-blue-200 overflow-hidden">
      <div class="px-5 py-4 border-b border-slate-100 flex flex-wrap items-center gap-3">
        <h2 class="m-0 flex-1 text-base font-bold text-slate-800">Nouveau formulaire</h2>
        <ol class="m-0 p-0 list-none flex flex-wrap gap-2 text-xs font-bold">
          <li v-for="(titre, i) in ETAPES" :key="titre"
              class="px-3 py-1.5 rounded-lg border" :class="assistant.etape === i + 1 ? 'bg-blue-600 border-blue-600 text-white' : assistant.etape > i + 1 ? 'bg-emerald-50 border-emerald-200 text-emerald-700' : 'bg-white border-slate-200 text-slate-400'">
            {{ i + 1 }}. {{ titre }}
          </li>
        </ol>
      </div>

      <!-- 1. Type de document -->
      <div v-if="assistant.etape === 1" class="p-5 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <button v-for="r in rolesReference" :key="r.code" type="button" @click="choisirRole(r.code)"
                class="min-h-[64px] p-4 rounded-xl border border-slate-200 bg-white hover:border-blue-400 hover:bg-blue-50 text-left cursor-pointer">
          <span class="block font-bold text-slate-800 text-sm">{{ r.libelle }}</span>
          <span class="block text-xs text-slate-500 mt-1">
            {{ nature(r) }} · {{ criteresDuRole(r).length ? `par ${criteresDuRole(r).map(c => c.court).join(', ')}` : 'un seul formulaire' }}
          </span>
        </button>
      </div>

      <!-- 2. Champ d'application -->
      <div v-else-if="assistant.etape === 2" class="p-5 flex flex-col gap-4">
        <p class="m-0 text-sm text-slate-600">
          <b>{{ roleAssistant.libelle }}</b> : choisissez les critères qui désignent ce formulaire. Laissez vide un critère qui ne
          s'applique pas (ex. plan de toute une famille, sans poste).
        </p>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <label v-for="c in criteresAssistant" :key="c.cle" class="flex flex-col gap-1 text-sm font-semibold text-slate-600">
            {{ c.titre }}
            <select v-model="assistant[c.champ]"
                    class="min-h-[44px] px-3 rounded-lg border border-slate-300 bg-white text-sm focus:outline-none focus:border-blue-500">
              <option value="">— Aucun —</option>
              <option v-for="e in listes?.[c.liste] ?? []" :key="e.code" :value="e.code">{{ e.code }} · {{ e.libelle }}</option>
            </select>
          </label>
          <div v-if="typeDeLaFamille" class="flex flex-col gap-1 text-sm font-semibold text-slate-600">
            Type de robinet
            <span class="min-h-[44px] px-3 rounded-lg border border-slate-200 bg-slate-50 text-slate-700 flex items-center gap-2">
              <i class="pi pi-lock text-xs text-slate-400"></i> {{ typeDeLaFamille }} <span class="font-normal text-slate-500">(celui de la famille)</span>
            </span>
          </div>
        </div>
        <div class="flex gap-2">
          <button type="button" @click="assistant.etape = 1"
                  class="min-h-[44px] px-4 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 font-semibold cursor-pointer">Précédent</button>
          <button type="button" @click="assistant.etape = 3"
                  class="min-h-[44px] px-4 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-bold border-0 cursor-pointer">Suivant</button>
        </div>
      </div>

      <!-- 3. Code et nom -->
      <div v-else class="p-5 flex flex-col gap-4">
        <p class="m-0 text-sm text-slate-600">
          <b>{{ roleAssistant.libelle }}</b> · {{ champAssistant }}. Le code et la version de départ sont ceux de Sopal
          (ex. FE0594, version 3) ; le code pourra être changé plus tard sans changer la version. Ensuite, la version
          augmente toute seule.
        </p>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <label class="flex flex-col gap-1 text-sm font-semibold text-slate-600">Code
            <input v-model="assistant.code" maxlength="30" placeholder="ex. FE0594"
                   class="min-h-[44px] px-3 rounded-lg border border-slate-300 uppercase text-sm focus:outline-none focus:border-blue-500" />
          </label>
          <label class="flex flex-col gap-1 text-sm font-semibold text-slate-600">Version de départ
            <input v-model.number="assistant.version" type="number" min="0" max="999" step="1" inputmode="numeric"
                   class="min-h-[44px] px-3 rounded-lg border border-slate-300 text-sm focus:outline-none focus:border-blue-500" />
          </label>
          <label class="flex flex-col gap-1 text-sm font-semibold text-slate-600 md:col-span-2">Nom du formulaire (facultatif)
            <input v-model="assistant.designation" maxlength="150" placeholder="ex. Plan de contrôle d'assemblage soupape PAS71"
                   class="min-h-[44px] px-3 rounded-lg border border-slate-300 text-sm focus:outline-none focus:border-blue-500" />
          </label>
        </div>
        <div class="flex gap-2">
          <button type="button" @click="assistant.etape = criteresDuRole(roleAssistant).length ? 2 : 1"
                  class="min-h-[44px] px-4 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 font-semibold cursor-pointer">Précédent</button>
          <button type="button" :disabled="!assistant.code.trim() || !versionDepartValide || actionEnCours === 'nouveau-formulaire'" @click="creerFormulaire"
                  class="min-h-[44px] px-4 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white font-bold border-0 cursor-pointer flex items-center gap-2">
            <i class="pi" :class="actionEnCours === 'nouveau-formulaire' ? 'pi-spin pi-spinner' : 'pi-check'"></i> Créer le formulaire
          </button>
        </div>
      </div>
      <div class="px-5 py-3 border-t border-slate-100 bg-slate-50">
        <button type="button" @click="assistant = null" class="bg-transparent border-0 text-sm font-semibold text-slate-500 hover:text-slate-700 cursor-pointer">Fermer l'assistant</button>
      </div>
    </section>

    <div v-if="chargement" class="flex items-center gap-3 text-slate-500"><i class="pi pi-spin pi-spinner"></i> Chargement…</div>

    <section v-else-if="!rolesAvecFormulaires.length" class="bg-white rounded-2xl border border-dashed border-slate-300 p-8 text-center text-slate-500">
      <i class="pi pi-file text-3xl text-slate-300"></i>
      <p class="m-0 mt-3 font-semibold text-slate-700">Aucun formulaire pour l'instant</p>
      <p class="m-0 mt-1 text-sm">Créez le premier avec « Nouveau formulaire » : type de document, champ d'application, puis code.</p>
    </section>

    <template v-else>
      <!-- Filtres : type de document (seulement ceux qui ont des formulaires) et recherche -->
      <div class="flex flex-col gap-3">
        <div class="flex flex-wrap gap-2" role="group" aria-label="Type de document">
          <button type="button" @click="filtreRole = ''" :aria-pressed="!filtreRole"
                  class="min-h-[40px] px-3 rounded-lg border text-sm font-semibold cursor-pointer"
                  :class="!filtreRole ? 'bg-slate-800 border-slate-800 text-white' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'">
            Tous <span class="opacity-70">({{ nombreFormulaires }})</span>
          </button>
          <button v-for="r in rolesAvecFormulaires" :key="r.role" type="button" @click="filtreRole = r.role" :aria-pressed="filtreRole === r.role"
                  class="min-h-[40px] px-3 rounded-lg border text-sm font-semibold cursor-pointer"
                  :class="filtreRole === r.role ? 'bg-blue-600 border-blue-600 text-white' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'">
            {{ r.libelle }} <span class="opacity-70">({{ r.formulaires.length }})</span>
          </button>
        </div>
        <div class="flex flex-col sm:flex-row sm:items-center gap-3">
          <input v-model="recherche" type="search" placeholder="Rechercher un code, une famille, un poste, une machine…"
                 class="min-h-[44px] px-3 rounded-lg border border-slate-300 text-sm w-full sm:max-w-md focus:outline-none focus:border-blue-500" />
          <span v-if="sansModele" class="px-3 py-1.5 rounded-lg border text-xs font-black uppercase tracking-wider bg-red-50 text-red-700 border-red-200 self-start sm:self-auto">
            {{ sansModele }} formulaire(s) sans modèle Excel
          </span>
        </div>
      </div>

      <p v-if="!rolesAffiches.length" class="m-0 text-sm text-slate-500">Aucun formulaire ne correspond à la recherche.</p>

      <section v-for="r in rolesAffiches" :key="r.role" class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
        <div class="px-5 py-4 border-b border-slate-100 flex flex-wrap items-center gap-2">
          <h2 class="m-0 text-base font-bold text-slate-800">{{ r.libelle }}</h2>
          <span class="px-2 py-0.5 rounded-md text-xs font-semibold"
                :class="r.nature === 'RESPONSE_SHEET' ? 'bg-violet-50 text-violet-700' : 'bg-slate-100 text-slate-600'"
                :title="r.nature === 'RESPONSE_SHEET' ? 'Gabarit rempli par les réponses des opérateurs : aucun document à gérer' : 'Contenu défini par le superviseur qualité'">
            {{ nature(r) }}
          </span>
        </div>
        <ul class="m-0 p-0 list-none divide-y divide-slate-100">
          <li v-for="f in r.formulaires" :id="`formulaire-${f.id}`" :key="f.id"
              class="px-5 py-3 flex flex-col md:flex-row md:flex-wrap md:items-center gap-2 text-sm"
              :class="{ 'bg-blue-50/60 ring-2 ring-inset ring-blue-300': f.id === formulaireMisEnAvant }">
            <!-- Code -->
            <span v-if="codeEnEdition?.id === f.id" class="md:w-64 shrink-0 flex gap-1">
              <input v-model="codeEnEdition.code" maxlength="30" :disabled="!!codeEnEdition.documentsActifs" aria-label="Nouveau code"
                     class="min-h-[36px] w-36 px-2 rounded-lg border border-slate-300 uppercase" @keyup.enter="preparerCode" />
              <button v-if="!codeEnEdition.documentsActifs" type="button" @click="preparerCode"
                      :disabled="!codeEnEdition.code.trim() || actionEnCours === `code-${f.id}`" title="Continuer"
                      class="px-2 rounded-lg bg-emerald-600 text-white border-0 cursor-pointer disabled:opacity-50"><i class="pi pi-check"></i></button>
              <button type="button" @click="codeEnEdition = null" title="Annuler" class="px-2 rounded-lg border border-slate-200 bg-white cursor-pointer"><i class="pi pi-times"></i></button>
            </span>
            <span v-else class="font-bold text-slate-800 md:w-72 shrink-0 break-all" :title="`${f.codeReference}, version ${f.version}`">
              {{ f.codeReference }}.{{ f.version }}
            </span>
            <!-- Champ d'application et colonnes -->
            <span class="flex-1 text-slate-500 min-w-[200px]">
              <span v-if="f.statut === 'BROUILLON'" class="mr-1 text-xs font-semibold text-slate-400" title="Pas encore utilisé par un document">Brouillon ·</span>
              <span class="font-semibold text-slate-600">{{ f.champApplication || 'Sans critère' }}</span>
              · {{ f.colonnes.length ? `${f.colonnes.length} colonne(s) propre(s)` : 'aucune colonne propre' }}
            </span>
            <!-- Modèle Excel (rattaché par l'administrateur) -->
            <span v-if="f.modeleActuel" class="self-start md:self-auto max-w-[18rem] px-2 py-1 rounded-lg border text-xs font-semibold bg-emerald-50 text-emerald-700 border-emerald-200 flex items-center gap-1"
                  :title="f.modeleActuel.explication">
              <i class="pi pi-check-circle shrink-0"></i>
              <span class="truncate">{{ f.modeleActuel.nomModele || `${nomModele(r, f.modeleActuel.codeModele)} · M${f.modeleActuel.numeroVersion}` }}</span>
            </span>
            <span v-else class="self-start md:self-auto px-2 py-1 rounded-lg border text-xs font-semibold bg-amber-50 text-amber-800 border-amber-200 flex items-center gap-1"
                  :title="f.messageModeleManquant">
              <i class="pi pi-clock shrink-0"></i>
              {{ f.modeleManquant === 'STRUCTURE_MODIFIEE' ? "Colonnes changées : modèle attendu de l'administrateur" : "Modèle Excel attendu de l'administrateur" }}
            </span>
            <span v-if="f.demandeEnAttente" class="self-start md:self-auto px-2 py-1 rounded-lg border text-xs font-semibold bg-blue-50 text-blue-700 border-blue-200 flex items-center gap-1"
                  :title="f.demandeEnAttente.motif || ''">
              <i class="pi pi-send shrink-0"></i> Demande en attente · {{ libelleDemande(f.demandeEnAttente) }}
            </span>
            <!-- Actions -->
            <span v-if="codeEnEdition?.id !== f.id" class="flex flex-wrap gap-2">
              <button type="button" @click="codeEnEdition = { id: f.id, code: f.codeReference }"
                      class="min-h-[36px] px-3 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold flex items-center gap-1 cursor-pointer">
                <i class="pi pi-pencil"></i> Changer le code
              </button>
              <button v-if="r.modeles.length && !f.demandeEnAttente && demande?.formulaire.id !== f.id" type="button" @click="ouvrirDemande(f)"
                      class="min-h-[36px] px-3 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold border-0 cursor-pointer flex items-center gap-1">
                <i class="pi pi-send"></i> Demander un nouveau modèle
              </button>
              <button v-if="f.demandeEnAttente?.origine === 'SUPERVISOR'" type="button" @click="annulerDemande(f.demandeEnAttente)"
                      :disabled="actionEnCours === `annuler-${f.demandeEnAttente.id}`"
                      class="min-h-[36px] px-3 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold cursor-pointer flex items-center gap-1">
                <i class="pi pi-times"></i> Annuler la demande
              </button>
            </span>
            <!-- Demande de nouveau modèle -->
            <div v-if="demande?.formulaire.id === f.id" class="w-full md:basis-full rounded-xl border border-blue-200 bg-blue-50/40 p-4 flex flex-col gap-3">
                            <p class="m-0 font-semibold text-slate-700">Nouveau modèle Excel pour {{ f.codeReference }}.{{ f.version }}</p>
              <div class="flex flex-wrap gap-2" role="group" aria-label="Raison">
                <button v-for="raison in [{ code: 'LOGO', libelle: 'Nouveau logo' }, { code: 'AUTRE', libelle: 'Mise en page ou autre changement' }]" :key="raison.code"
                        type="button" @click="choisirRaison(raison.code)" :aria-pressed="demande.raison === raison.code"
                        class="min-h-[40px] px-3 rounded-lg border text-sm font-semibold cursor-pointer"
                        :class="demande.raison === raison.code ? 'bg-blue-600 border-blue-600 text-white' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'">
                  {{ raison.libelle }}
                </button>
              </div>
              <label class="flex flex-col gap-1 text-sm font-semibold text-slate-600">Motif (envoyé à l'administrateur)
                <input v-model="demande.motif" maxlength="500" placeholder="ex. Nouveau logo, mise en page corrigée"
                       class="min-h-[44px] px-3 rounded-lg border border-slate-300 bg-white text-sm focus:outline-none focus:border-blue-500" />
              </label>
              <fieldset class="m-0 p-0 border-0 flex flex-col gap-2 text-sm">
                <legend class="mb-1 font-semibold text-slate-600">Version du formulaire</legend>
                                <p v-if="demande.raison === 'LOGO'" class="m-0 text-slate-500">Un nouveau logo change toujours de version.</p>
                <label v-if="demande.raison !== 'LOGO'" class="flex items-start gap-2 cursor-pointer">
                  <input v-model="demande.changementVersion" type="radio" value="SAME_VERSION" class="mt-1" />
                  <span><b>Même version ({{ f.codeReference }}.{{ f.version }})</b><br />
                    <span class="text-slate-500">Les documents déjà créés gardent leur modèle ; ceux créés ensuite prennent le nouveau. Rien n'est archivé.</span></span>
                </label>
                <label class="flex items-start gap-2 cursor-pointer">
                  <input v-model="demande.changementVersion" type="radio" value="NEXT_VERSION" class="mt-1" />
                  <span><b>Version suivante ({{ f.codeReference }}.{{ f.version + 1 }})</b><br />
                    <span class="text-slate-500">
                      {{ r.nature === 'RESPONSE_SHEET' ? 'La fiche passe à sa version suivante avec ce modèle.' : 'Le formulaire passe à sa version suivante : ses documents actifs sont archivés tels quels puis recréés en version suivante.' }}
                      Les OF déjà démarrés gardent leur version et leur modèle.
                    </span></span>
                </label>
              </fieldset>
              <div class="flex flex-col sm:flex-row gap-2">
                <button type="button" :disabled="!demande.motif.trim() || actionEnCours === `demande-${f.id}`" @click="envoyerDemande"
                        class="min-h-[40px] px-4 rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-bold border-0 cursor-pointer flex items-center gap-2">
                  <i class="pi" :class="actionEnCours === `demande-${f.id}` ? 'pi-spin pi-spinner' : 'pi-send'"></i> Envoyer la demande à l'administrateur
                </button>
                <button type="button" @click="demande = null"
                        class="min-h-[40px] px-4 rounded-lg bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 font-semibold cursor-pointer">Annuler</button>
              </div>
            </div>
            <!-- Confirmation du changement de code -->
            <div v-if="codeEnEdition?.id === f.id && codeEnEdition.documentsActifs"
                 class="w-full md:basis-full rounded-xl border border-amber-200 bg-amber-50 p-4 flex flex-col gap-3">
              <p class="m-0 font-semibold text-slate-700">
                {{ f.codeReference }} version {{ f.version }} devient {{ codeEnEdition.code.trim().toUpperCase() }} version {{ f.version }}.
                Ces {{ codeEnEdition.documentsActifs.length }} document(s) seront archivés tels quels, puis recréés à l'identique
                sous le nouveau code :
              </p>
              <ul class="m-0 pl-5 text-slate-600">
                <li v-for="d in codeEnEdition.documentsActifs" :key="d.id">
                  {{ d.typeDocument }} · {{ d.nom }} · version {{ d.version }} · {{ d.statut === 'BROUILLON' ? 'brouillon' : 'actif' }}
                </li>
              </ul>
              <div class="flex flex-col sm:flex-row gap-2">
                <button type="button" :disabled="actionEnCours === `code-${f.id}`" @click="enregistrerCode"
                        class="min-h-[40px] px-4 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold border-0 cursor-pointer">
                  Changer le code et recréer ces documents</button>
                <button type="button" @click="codeEnEdition = null"
                        class="min-h-[40px] px-4 rounded-lg bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 font-semibold cursor-pointer">
                  Annuler</button>
              </div>
              <p class="m-0 text-xs text-slate-500">
                {{ f.codeReference }}.{{ f.version }} et ses documents restent consultables (traçabilité). Les OF déjà démarrés
                continuent avec leurs documents ; les nouveaux OF prennent les documents recréés.
              </p>
            </div>
          </li>
        </ul>
      </section>

      <!-- Demandes de modèle -->
      <section class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
        <button type="button" @click="afficherDemandes = !afficherDemandes" :aria-expanded="afficherDemandes"
                class="w-full px-5 py-4 bg-transparent border-0 flex items-center gap-2 text-left cursor-pointer">
          <i class="pi" :class="afficherDemandes ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
          <span class="flex-1 text-base font-bold text-slate-800">Demandes de modèle</span>
          <span class="text-xs font-semibold text-slate-500">{{ demandesEnAttente }} en attente · {{ demandes.length }} au total</span>
        </button>
        <template v-if="afficherDemandes">
          <p v-if="!demandes.length" class="m-0 px-5 pb-4 text-sm text-slate-500">Aucune demande pour l'instant.</p>
          <ul v-else class="m-0 p-0 list-none divide-y divide-slate-100 border-t border-slate-100">
            <li v-for="d in demandes" :key="d.id" class="px-5 py-3 flex flex-col md:flex-row md:items-center gap-2 text-sm">
              <span class="md:w-56 shrink-0 font-bold text-slate-800">{{ d.codeFormulaire }}.{{ d.versionFormulaire }}</span>
              <span class="flex-1 min-w-0 text-slate-600">
                {{ libelleDemande(d) }}<template v-if="d.motif"> · « {{ d.motif }} »</template>
                <span class="block text-xs text-slate-400">
                  {{ formaterDate(d.demandeLe) }} par {{ d.demandeParNom || d.demandePar || "l'application" }}
                  <template v-if="d.traiteLe"> · {{ d.statut === 'DONE' ? 'traitée' : 'annulée' }} le {{ formaterDate(d.traiteLe) }} par {{ d.traiteParNom || d.traitePar }}</template>
                  <template v-if="d.numeroModele"> · modèle M{{ d.numeroModele }}</template>
                </span>
              </span>
              <span class="self-start md:self-auto px-2 py-1 rounded-lg text-xs font-semibold"
                    :class="d.statut === 'PENDING' ? 'bg-blue-50 text-blue-700' : d.statut === 'DONE' ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'">
                {{ STATUTS_DEMANDE[d.statut] }}
              </span>
            </li>
          </ul>
        </template>
      </section>
    </template>
  </div>
</template>
