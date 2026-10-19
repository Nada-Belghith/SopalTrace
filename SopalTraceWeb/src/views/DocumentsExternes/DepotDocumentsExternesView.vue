<template>
  <div class="min-h-screen bg-slate-50 p-4 md:p-6 lg:p-8">
    <div class="max-w-5xl mx-auto space-y-6">
      <!-- En-tête -->
      <header class="bg-white border border-slate-200 rounded-2xl p-5 md:p-6 shadow-sm flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-blue-600 text-white flex items-center justify-center shrink-0">
          <i class="pi pi-upload text-xl"></i>
        </div>
        <div class="min-w-0">
          <h1 class="m-0 text-xl md:text-2xl font-black text-slate-900">Déposer un document</h1>
          <p class="m-0 mt-1 text-sm text-slate-500">
            Choisissez le type, saisissez ses numéros puis le PDF. Le fichier est enregistré sous un nom normalisé :
            le responsable qualité le retrouve ensuite pour chaque OF concerné.
          </p>
        </div>
      </header>

      <p v-if="erreurTypes" class="m-0 rounded-xl border border-red-200 bg-red-50 text-red-800 px-4 py-3 text-sm">{{ erreurTypes }}</p>

      <!-- Formulaire de dépôt -->
      <section v-if="types.length" class="bg-white border border-slate-200 rounded-2xl shadow-sm p-5 md:p-6 space-y-6">
        <!-- 1. Type -->
        <fieldset class="m-0 p-0 border-0 space-y-3">
          <legend class="text-xs font-black text-slate-500 uppercase tracking-widest mb-3">1. Type de document</legend>
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            <label v-for="t in types" :key="t.code"
                   class="flex items-start gap-3 p-4 rounded-xl border-2 cursor-pointer transition-colors"
                   :class="typeCode === t.code ? 'border-blue-500 bg-blue-50' : 'border-slate-200 hover:border-blue-300'">
              <input type="radio" name="type" :value="t.code" v-model="typeCode" class="mt-1 accent-blue-600" />
              <span class="min-w-0">
                <span class="block font-bold text-slate-800 text-sm">{{ t.libelle }}</span>
                <span class="block text-xs text-slate-500 mt-1">Nom : {{ t.champs.map(c => c.libelle.replace(/^(N°|Référence) (du |de la |de l')?/, '')).join(' – ') }}</span>
              </span>
            </label>
          </div>
        </fieldset>

        <template v-if="type">
          <!-- 2. Numéros -->
          <fieldset class="m-0 p-0 border-0">
            <legend class="text-xs font-black text-slate-500 uppercase tracking-widest mb-3">2. Numéros du document</legend>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label v-for="c in type.champs" :key="c.code" class="block">
                <span class="block text-sm font-semibold text-slate-700 mb-1">{{ c.libelle }} <span class="text-red-600">*</span></span>
                <input v-model="valeurs[c.code]" type="text" :maxlength="c.longueurMaximale" autocomplete="off" spellcheck="false"
                       @input="saisir(c)"
                       class="w-full min-h-[44px] px-3 rounded-lg border border-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 outline-none font-semibold"
                       :class="{ uppercase: c.format !== 'REFERENCE' }" />
                <span v-if="c.format === 'REFERENCE'" class="block mt-1 text-xs text-slate-500">Telle que dans la commande Sage (champ « Référence »).</span>
                <span v-if="c.code === 'numeroCommande'" class="block mt-1 text-xs"
                      :class="commande.erreur ? 'text-red-700' : 'text-slate-500'">
                  <template v-if="commande.erreur">{{ commande.erreur }}</template>
                  <template v-else-if="commande.reference">Référence client dans Sage : <strong>{{ commande.reference }}</strong></template>
                  <template v-else>La référence client de cette commande est reprise de Sage pour nommer le BL.</template>
                </span>
              </label>
            </div>
            <p class="m-0 mt-2 text-xs text-slate-500">Numéros : lettres, chiffres, « . », « _ » et « - » seulement. Tout est vérifié dans Sage.</p>
            <p class="m-0 mt-3 text-sm">
              <span class="text-slate-500">Nom du fichier :</span>
              <span v-if="nom" class="ml-1 font-mono font-bold text-slate-900">{{ nom }}</span>
              <span v-else class="ml-1 text-slate-400">saisissez tous les numéros</span>
            </p>
          </fieldset>

          <!-- 3. Fichier -->
          <fieldset class="m-0 p-0 border-0">
            <legend class="text-xs font-black text-slate-500 uppercase tracking-widest mb-3">3. Fichier PDF</legend>
            <ChoixFichier v-model="fichier" libelle="Choisir le fichier PDF" accept=".pdf,application/pdf" icone="pi-file-pdf" />
          </fieldset>

          <!-- Déjà déposé : remplacement avec motif -->
          <div v-if="conflit" class="rounded-xl border border-amber-300 bg-amber-50 p-4 space-y-3">
            <p class="m-0 text-sm text-amber-900">{{ conflit }}</p>
            <label class="flex items-center gap-2 text-sm font-semibold text-amber-900 cursor-pointer">
              <input type="checkbox" v-model="remplacer" class="accent-amber-600" /> Remplacer ce document
            </label>
            <label v-if="remplacer" class="block">
              <span class="block text-sm font-semibold text-slate-700 mb-1">Motif du remplacement <span class="text-red-600">*</span></span>
              <textarea v-model="motif" rows="2" maxlength="300" class="w-full px-3 py-2 rounded-lg border border-slate-300 focus:border-amber-500 outline-none text-sm"></textarea>
            </label>
          </div>

          <p v-if="erreur" class="m-0 rounded-xl border border-red-200 bg-red-50 text-red-800 px-4 py-3 text-sm" role="alert">{{ erreur }}</p>
          <p v-if="succes" class="m-0 rounded-xl border border-emerald-200 bg-emerald-50 text-emerald-800 px-4 py-3 text-sm" role="status">
            <i class="pi pi-check-circle mr-1"></i>{{ succes }}
          </p>

          <div class="flex justify-end">
            <button type="button" @click="deposer" :disabled="!peutDeposer || envoi"
                    class="min-h-[44px] px-6 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:bg-slate-200 disabled:text-slate-400 text-white font-bold text-sm flex items-center gap-2 border-0 cursor-pointer">
              <i class="pi" :class="envoi ? 'pi-spin pi-spinner' : (remplacer ? 'pi-refresh' : 'pi-upload')"></i>
              {{ remplacer ? 'Remplacer le document' : 'Déposer le document' }}
            </button>
          </div>
        </template>
      </section>

      <!-- Documents déposés -->
      <section class="bg-white border border-slate-200 rounded-2xl shadow-sm overflow-hidden">
        <div class="p-4 md:p-5 border-b border-slate-200 flex flex-col md:flex-row md:items-center gap-3 justify-between">
          <h2 class="m-0 text-base font-black text-slate-800">Documents déposés</h2>
          <div class="flex flex-col sm:flex-row gap-2">
            <select v-model="filtreType" @change="charger" class="min-h-[40px] px-3 rounded-lg border border-slate-300 text-sm">
              <option value="">Tous les types</option>
              <option v-for="t in types" :key="t.code" :value="t.code">{{ t.libelle }}</option>
            </select>
            <input v-model="recherche" @keydown.enter="charger" type="search" placeholder="Rechercher un nom…"
                   class="min-h-[40px] px-3 rounded-lg border border-slate-300 text-sm" />
          </div>
        </div>
        <p v-if="erreurListe" class="m-0 p-4 text-sm text-red-700">{{ erreurListe }}</p>
        <p v-else-if="!documents.length" class="m-0 p-6 text-sm text-slate-500 text-center">Aucun document déposé.</p>
        <ul v-else class="m-0 p-0 list-none divide-y divide-slate-100">
          <li v-for="d in documents" :key="d.id" class="p-4 md:px-5 flex flex-col md:flex-row md:items-center gap-3">
            <div class="flex-1 min-w-0">
              <p class="m-0 font-mono font-bold text-slate-900 text-sm break-all">{{ d.nomFichier }}</p>
              <p class="m-0 mt-1 text-xs text-slate-500">
                {{ d.typeLibelle }} · déposé le {{ dateHeure(d.deposeLe) }} par {{ d.deposeParNom || d.deposePar }} · fichier d'origine « {{ d.nomFichierOrigine }} »
              </p>
              <p v-if="d.motifRemplacement" class="m-0 mt-1 text-xs text-slate-600">Remplace une version précédente : {{ d.motifRemplacement }}</p>
            </div>
            <span class="self-start md:self-center px-2 py-1 rounded-md text-[11px] font-bold border"
                  :class="d.remplaceLe ? 'bg-slate-100 text-slate-500 border-slate-200' : 'bg-emerald-50 text-emerald-700 border-emerald-200'">
              {{ d.remplaceLe ? `Remplacé le ${dateHeure(d.remplaceLe)}` : 'En vigueur' }}
            </span>
            <button type="button" @click="ouvrir(d)" class="self-start md:self-center min-h-[40px] px-3 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-sm font-semibold text-slate-700 flex items-center gap-2 cursor-pointer">
              <i class="pi pi-file-pdf text-red-600"></i> Ouvrir
            </button>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>

<script setup>
/**
 * Dépôt des documents externes par un service (achats, commercial, qualité) : le service choisit le type, saisit les
 * numéros exigés (vérifiés dans Sage) et le PDF ; la base forme le nom normalisé. Un document déjà déposé se remplace
 * avec un motif, l'ancien reste consultable.
 */
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import ChoixFichier from '@/components/Qualite/ChoixFichier.vue'
import documentsExternesService, { nomNormalise } from '@/services/documentsExternesService'
import { ouvrirPdf } from '@/services/rapportsService'

const types = ref([])
const typeCode = ref('')
const valeurs = reactive({})
const fichier = ref(null)
const remplacer = ref(false)
const motif = ref('')
const conflit = ref('')
const erreur = ref('')
const erreurTypes = ref('')
const succes = ref('')
const envoi = ref(false)

const documents = ref([])
const filtreType = ref('')
const recherche = ref('')
const erreurListe = ref('')

const type = computed(() => types.value.find(t => t.code === typeCode.value))
// BL : référence client de la commande Sage saisie (cherchée par l'application, pas par le commercial).
const commande = reactive({ reference: '', erreur: '' })
const nom = computed(() => nomNormalise(type.value, { ...valeurs, referenceCommandeClient: commande.reference }))
const peutDeposer = computed(() => !!nom.value && !!fichier.value && (!remplacer.value || motif.value.trim().length > 0))
// Un numéro est saisi en majuscules ; une référence client garde son écriture (celle de Sage).
const saisir = (champ) => {
  if (champ.format !== 'REFERENCE') valeurs[champ.code] = (valeurs[champ.code] ?? '').toUpperCase()
}
const dateHeure = (d) => d ? new Date(d).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' }) : ''

// Autre type ou autres numéros : le conflit signalé ne vaut plus.
const reinitialiserConflit = () => { conflit.value = ''; remplacer.value = false; motif.value = '' }
watch(typeCode, () => {
  for (const cle of Object.keys(valeurs)) delete valeurs[cle]
  reinitialiserConflit()
  erreur.value = ''
})
watch(valeurs, reinitialiserConflit, { deep: true })

let minuterieCommande = null
watch(() => valeurs.numeroCommande, (numero) => {
  clearTimeout(minuterieCommande)
  commande.reference = ''
  commande.erreur = ''
  if (!type.value?.referenceClientDeLaCommande || !numero?.trim()) return
  minuterieCommande = setTimeout(async () => {
    try {
      const trouvee = await documentsExternesService.getCommandeClient(numero.trim())
      if (valeurs.numeroCommande !== numero) return
      if (trouvee.referenceCommandeClient) commande.reference = trouvee.referenceCommandeClient
      else commande.erreur = `La commande ${trouvee.numeroCommande} n'a pas de référence client dans Sage.`
    } catch (e) {
      if (valeurs.numeroCommande === numero) commande.erreur = e.message
    }
  }, 400)
})

const deposer = async () => {
  erreur.value = ''
  succes.value = ''
  envoi.value = true
  try {
    const reponse = await documentsExternesService.deposer({
      typeCode: typeCode.value,
      ...Object.fromEntries(type.value.champs.map(c => [c.code, valeurs[c.code]])),
      fichier: fichier.value,
      remplacer: remplacer.value || undefined,
      motifRemplacement: remplacer.value ? motif.value : undefined
    })
    succes.value = reponse.message
    fichier.value = null
    for (const cle of Object.keys(valeurs)) valeurs[cle] = ''
    reinitialiserConflit()
    await charger()
  } catch (e) {
    if (e.status === 409 && !remplacer.value) conflit.value = e.message
    else erreur.value = e.message
  } finally {
    envoi.value = false
  }
}

const charger = async () => {
  erreurListe.value = ''
  try {
    documents.value = await documentsExternesService.lister(filtreType.value, recherche.value.trim())
  } catch (e) {
    erreurListe.value = e.message
  }
}

const ouvrir = async (d) => {
  try {
    await ouvrirPdf(() => documentsExternesService.fichier(d.id), d.nomFichier)
  } catch (e) {
    erreurListe.value = e.message
  }
}

// Lien d'une relance (e-mail du responsable qualité) : type et numéros dans l'adresse, si ce type est déposé ici.
const route = useRoute()
const preremplir = async () => {
  const prevu = types.value.find(t => t.code === route.query.type)
  if (!prevu) return
  typeCode.value = prevu.code
  await nextTick()   // le changement de type vide les numéros : les remplir ensuite
  for (const c of prevu.champs) if (typeof route.query[c.code] === 'string') valeurs[c.code] = route.query[c.code]
}

onMounted(async () => {
  try {
    types.value = await documentsExternesService.getTypes()
    if (types.value.length === 1) typeCode.value = types.value[0].code
    await preremplir()
  } catch (e) {
    erreurTypes.value = e.message
  }
  await charger()
})
</script>
