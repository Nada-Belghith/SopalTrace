<script setup>
/**
 * Contrôle au poste : un document par jour de production et par poste, commun aux équipes. Les équipes et leurs
 * heures viennent du document (définies par le superviseur) ; le serveur dit qui peut saisir (l'équipe en
 * production, sur le document du jour en cours) et lesquelles de ses heures. Cet écran n'en déduit rien.
 */
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from '@/services/apiClient'
import execAssemblageService from '@/services/execAssemblageService'
import { useAppToast } from '@/composables/useAppToast'
import { useOperateurStore } from '@/stores/execution/operateurStore'
import EnTetePage from '@/components/Shared/EnTetePage.vue'

const router = useRouter()
const route = useRoute()
const toast = useAppToast()
const operateurStore = useOperateurStore()

const execControleOfId = route.params.execControleOfId
const posteCode = route.query.posteCode
const equipeTablette = route.query.equipe || operateurStore.activeOfContext?.equipe || null

const doc = ref(null)
const chargement = ref(true)
const enregistrement = ref(false)
const planManquant = ref(false)
const signalementEnCours = ref(false)
const conflit = ref(null)

// Saisie : non-conformités par ligne et par heure, pièces réalisées par heure, rebuts de l'équipe.
const heures = ref({})
const realisees = ref({})
const rebutsEquipe = ref(null)
const modifie = ref(false)

const heureCourante = ref(new Date().getHours())
let minuteur = null

const ofContexte = computed(() => operateurStore.activeOfContext || {})
const tranchesEquipe = computed(() => new Set(doc.value?.tranchesEquipe || []))
const peutSaisir = computed(() => !!doc.value?.peutSaisir)
const equipes = computed(() => doc.value?.equipes || [])
const colonnes = computed(() => equipes.value.flatMap(e => e.tranches.map(t => ({ tranche: t, equipe: e.code }))))
const equipeSaisie = computed(() => equipes.value.find(e => e.code === doc.value?.equipeSaisie) || null)
const validations = computed(() => doc.value?.validations || [])
const validation = (code) => validations.value.find(v => v.equipe === code)
const jours = computed(() => doc.value?.jours || [])

const formatJour = (d) => d ? new Date(d).toLocaleDateString('fr-FR', { weekday: 'short', day: '2-digit', month: '2-digit' }) : '—'
const formatHeure = (d) => d ? new Date(d).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }) : ''
const libelleHeures = (e) => `${String(e.debut).padStart(2, '0')} h – ${String(e.fin).padStart(2, '0')} h`
const estHeureCourante = (tranche) => Number(tranche.slice(0, 2)) === heureCourante.value
const modifiable = (tranche) => peutSaisir.value && tranchesEquipe.value.has(tranche)
const nombre = (v) => (v === null || v === undefined || v === '' ? 0 : Number(v))

const totalLigne = (ligneId) => colonnes.value.reduce((s, c) => s + nombre(heures.value[ligneId]?.[c.tranche]), 0)
const totalDefautsTranche = (tranche) => (doc.value?.lignes || []).reduce((s, l) => s + nombre(heures.value[l.docLigneId]?.[tranche]), 0)
const testeesTranche = (tranche) => nombre(realisees.value[tranche]) + totalDefautsTranche(tranche)

/** Rebuts déjà enregistrés pour une équipe. */
const rebutsEnregistres = (code) => (doc.value?.rebutsParEquipe || []).filter(r => r.equipe === code).reduce((s, r) => s + r.nbPiecesRebutees, 0)

/** Bilan affiché : recalculé en direct pendant la saisie (le serveur le recalcule à l'enregistrement). */
const bilan = computed(() => {
  if (!doc.value) return null
  const defauts = colonnes.value.reduce((s, c) => s + totalDefautsTranche(c.tranche), 0)
  const testees = colonnes.value.reduce((s, c) => s + testeesTranche(c.tranche), 0)
  const total = (doc.value.rebutsParEquipe || []).reduce((s, r) => s + r.nbPiecesRebutees, 0)
  const rebuts = peutSaisir.value ? total - rebutsEnregistres(doc.value.equipeSaisie) + nombre(rebutsEquipe.value) : total
  return {
    defauts, testees, rebuts,
    tauxNc: testees > 0 ? Math.round((defauts * 10000) / testees) / 100 : 0,
    conformes: Math.max(0, testees - rebuts)
  }
})

/**
 * Pièces conformes cumulées du poste sur tous les documents (jours) de l'OF, saisie en cours comprise : elles ne
 * peuvent pas dépasser la quantité lancée (le serveur refuse l'enregistrement qui la dépasserait).
 */
const cumulConformes = computed(() => !doc.value ? 0
  : (doc.value.cumulPiecesConformes ?? 0) - (doc.value.bilan?.nbPieceConforme ?? 0) + (bilan.value?.conformes ?? 0))
const quantiteDepassee = computed(() => (doc.value?.quantiteLancee ?? 0) > 0 && cumulConformes.value > doc.value.quantiteLancee)

const appliquer = (data) => {
  doc.value = data
  const h = {}
  for (const l of data.lignes) {
    h[l.docLigneId] = {}
    for (const x of l.heures) h[l.docLigneId][x.trancheHoraire] = x.nbNcParHeure
  }
  heures.value = h
  realisees.value = Object.fromEntries(data.reponses.map(r => [r.trancheHoraire, r.totalRealiseHeure]))
  rebutsEquipe.value = data.nbPiecesRebuteesEquipe ?? null
  modifie.value = false
  conflit.value = null
}

const charger = async (statutId = null) => {
  chargement.value = true
  try {
    const res = statutId
      ? await apiClient.get(`/ExecRcPoste/statut/${statutId}`)
      : await execAssemblageService.getPlanRcPoste(execControleOfId, posteCode, equipeTablette || '')
    appliquer(res.data)
    planManquant.value = false
  } catch (err) {
    if (err.status === 404) planManquant.value = true
    else toast.error(err.message || 'Impossible de charger le contrôle au poste.')
  } finally {
    chargement.value = false
  }
}

const ouvrirJour = async (jour) => {
  if (modifie.value && !confirm('Des modifications ne sont pas enregistrées. Changer de jour quand même ?')) return
  await charger(jour.statutId)
}

/** Seules les heures de l'équipe sont envoyées ; le serveur refuse les autres. */
const payload = () => {
  const tranches = [...tranchesEquipe.value]
  const rempli = (v) => v !== null && v !== undefined && v !== ''
  return {
    execControleDocumentStatutId: doc.value.execControleDocumentStatutId,
    derniereSaisieEquipe: doc.value.derniereSaisieEquipe,
    nbPiecesRebuteesEquipe: rempli(rebutsEquipe.value) ? Number(rebutsEquipe.value) : 0,
    lignes: doc.value.lignes.map(l => ({
      docLigneId: l.docLigneId,
      heures: tranches.filter(t => rempli(heures.value[l.docLigneId]?.[t])).map(t => ({ trancheHoraire: t, nbNcParHeure: Number(heures.value[l.docLigneId][t]) }))
    })),
    reponses: tranches.filter(t => rempli(realisees.value[t])).map(t => ({ trancheHoraire: t, totalRealiseHeure: Number(realisees.value[t]) }))
  }
}

const enregistrer = async ({ silencieux = false } = {}) => {
  enregistrement.value = true
  try {
    const res = await apiClient.put(`/ExecRcPoste/${doc.value.execControleDocumentStatutId}`, payload())
    appliquer(res.data)
    if (!silencieux) toast.success('Heures enregistrées.')
    return true
  } catch (err) {
    if (err.status === 409) conflit.value = err.message
    toast.error(err.message || 'Impossible d\'enregistrer.')
    return false
  } finally {
    enregistrement.value = false
  }
}

const validerMesHeures = async () => {
  if (modifie.value && !(await enregistrer({ silencieux: true }))) return
  enregistrement.value = true
  try {
    const res = await execAssemblageService.validerHeuresRcPoste(doc.value.execControleDocumentStatutId)
    appliquer(res.data)
    toast.success(`Heures de l'équipe ${doc.value.equipeSaisie} validées.`)
  } catch (err) {
    toast.error(err.message || 'Impossible de valider les heures.')
  } finally {
    enregistrement.value = false
  }
}

const valeurSaisie = (valeur) => (valeur === '' ? null : Math.max(0, Number(valeur)))

const saisirDefauts = (ligneId, tranche, valeur) => {
  heures.value[ligneId] = { ...(heures.value[ligneId] || {}), [tranche]: valeurSaisie(valeur) }
  modifie.value = true
}
const saisirRealisees = (tranche, valeur) => {
  realisees.value = { ...realisees.value, [tranche]: valeurSaisie(valeur) }
  modifie.value = true
}
const saisirRebuts = (valeur) => {
  rebutsEquipe.value = valeurSaisie(valeur)
  modifie.value = true
}

const signalerPlanManquant = async () => {
  signalementEnCours.value = true
  try {
    await apiClient.post('/Alertes/plan-manquant', {
      operationCode: 'ASS', posteCode, numeroOf: ofContexte.value.numeroOf, articleCode: ofContexte.value.articleCode || ofContexte.value.codeArticle,
      designationArticle: ofContexte.value.designationArticle,
      descriptionProbleme: `Document de contrôle au poste introuvable ou inactif pour le poste ${posteCode}`
    })
    toast.success('Le superviseur a été prévenu.')
    retour()
  } catch {
    toast.error('Impossible d\'envoyer l\'alerte.')
  } finally {
    signalementEnCours.value = false
  }
}

const retour = () => {
  if (modifie.value && !confirm('Des modifications ne sont pas enregistrées. Quitter quand même ?')) return
  router.push({ name: 'operateur-of-fini', query: { execControleOfId, posteCode, equipe: route.query.equipe || undefined } })
}

onMounted(() => {
  charger(route.query.statutId || null)
  minuteur = setInterval(() => { heureCourante.value = new Date().getHours() }, 60000)
})
onUnmounted(() => clearInterval(minuteur))
</script>

<template>
  <div class="p-4 md:p-6 w-full min-w-0 space-y-4">
    <!-- En-tête -->
    <EnTetePage titre="Contrôle au poste" :complement="posteCode" retour libelle-retour="Retour à l'assistant" @retour="retour">
      <template #sous-titre>
        OF <strong class="text-slate-700">{{ ofContexte.numeroOf || '—' }}</strong>
        <span class="mx-2 text-slate-300">|</span> Article <strong class="text-slate-700">{{ ofContexte.codeArticle || ofContexte.articleCode || '—' }}</strong>
        <template v-if="doc?.planNom"><span class="mx-2 text-slate-300">|</span> {{ doc.planNom }}</template>
      </template>
      <template v-if="doc" #actions>
        <span class="px-3 py-1.5 rounded-lg bg-blue-50 text-blue-800 text-sm font-semibold"><i class="pi pi-calendar mr-1"></i>Journée du {{ formatJour(doc.jourProduction) }}</span>
        <span v-if="equipeSaisie" class="px-3 py-1.5 rounded-lg bg-emerald-50 text-emerald-800 text-sm font-semibold">
          <i class="pi pi-pencil mr-1"></i>Saisie : {{ equipeSaisie.nom }} · {{ equipeSaisie.code }} · {{ libelleHeures(equipeSaisie) }}
        </span>
      </template>
    </EnTetePage>

    <div v-if="(doc && !peutSaisir) || doc?.equipesParDefaut || conflit">

      <p v-if="doc && !peutSaisir" class="m-0 rounded-lg bg-amber-50 border border-amber-200 px-3 py-2 text-sm text-amber-900">
        <i class="pi pi-lock mr-1"></i>Lecture seule : {{ doc.motifLectureSeule }}
      </p>
      <p v-if="doc?.equipesParDefaut" class="m-0 mt-2 text-xs text-slate-500 italic">
        Le document ne définit pas ses équipes : les équipes standard s'appliquent (6 h–14 h, 14 h–22 h, 22 h–6 h).
      </p>
      <div v-if="conflit" class="mt-3 rounded-lg bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-900 flex flex-wrap items-center gap-3">
        <span class="flex-1"><i class="pi pi-exclamation-triangle mr-1"></i>{{ conflit }}</span>
        <button type="button" @click="charger(doc.execControleDocumentStatutId)"
                class="min-h-[40px] px-4 rounded-lg bg-red-600 text-white font-bold border-0 cursor-pointer">Recharger</button>
      </div>
    </div>

    <div v-if="chargement && !doc" class="py-16 text-center text-slate-500"><i class="pi pi-spin pi-spinner text-3xl"></i></div>

    <!-- Aucun document actif pour le poste -->
    <section v-else-if="planManquant" class="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center">
      <i class="pi pi-file-excel text-4xl text-slate-400"></i>
      <p class="m-0 mt-3 text-lg font-bold text-slate-700">Aucun document de contrôle au poste actif pour {{ posteCode }}</p>
      <p class="m-0 mt-1 text-sm text-slate-500">Le superviseur doit créer ou activer le document de ce poste (et de la famille de l'article).</p>
      <button type="button" @click="signalerPlanManquant" :disabled="signalementEnCours"
              class="mt-5 min-h-[48px] px-6 rounded-xl bg-red-600 hover:bg-red-700 text-white font-bold border-0 cursor-pointer disabled:opacity-60">
        <i class="pi pi-exclamation-triangle mr-2"></i>Signaler au superviseur
      </button>
    </section>

    <template v-else-if="doc">
      <!-- Jours et validations des équipes -->
      <section class="bg-white rounded-2xl border border-slate-200 shadow-sm p-4 flex flex-col gap-3">
        <div class="flex flex-wrap items-center gap-2">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500 mr-1">Jours</span>
          <button v-for="j in jours" :key="j.statutId" type="button" @click="ouvrirJour(j)"
                  class="min-h-[40px] px-3 rounded-lg border text-sm font-semibold cursor-pointer flex items-center gap-2"
                  :class="j.statutId === doc.execControleDocumentStatutId ? 'bg-blue-600 border-blue-600 text-white' : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'">
            {{ formatJour(j.jour) }}
            <span v-if="j.equipesValidees.length" class="text-[11px] px-1.5 py-0.5 rounded"
                  :class="j.statutId === doc.execControleDocumentStatutId ? 'bg-white/20' : 'bg-emerald-50 text-emerald-700'">
              <i class="pi pi-check text-[10px]"></i> {{ j.equipesValidees.join(', ') }}
            </span>
          </button>
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500 mr-1">Équipes</span>
          <span v-for="e in equipes" :key="e.code" class="px-3 py-1 rounded-full text-xs font-semibold border"
                :class="validation(e.code) ? 'bg-emerald-50 border-emerald-200 text-emerald-800' : 'bg-slate-50 border-slate-200 text-slate-600'">
            {{ e.nom }} ({{ e.code }}) · {{ libelleHeures(e) }} —
            <template v-if="validation(e.code)"><i class="pi pi-check text-[10px]"></i> validées à {{ formatHeure(validation(e.code).dateValidation) }}</template>
            <template v-else>non validées</template>
          </span>
        </div>
      </section>

      <!-- Tableau : défile dans sa carte, la colonne des défauts reste visible -->
      <section class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm border-collapse">
            <thead>
              <tr class="bg-slate-800 text-white">
                <th rowspan="2" class="sticky left-0 z-20 bg-slate-800 px-3 py-2 text-left w-[220px] min-w-[180px] border-r border-slate-700">Non-conformité</th>
                <th v-for="e in equipes" :key="e.code" :colspan="e.tranches.length"
                    class="px-2 py-1.5 text-center text-xs border-r border-slate-700 whitespace-nowrap"
                    :class="e.code === doc.equipeSaisie ? 'bg-emerald-700' : ''">
                  {{ e.nom }} ({{ e.code }}) <span class="font-normal opacity-80">· {{ libelleHeures(e) }}</span>
                </th>
                <th rowspan="2" class="px-2 py-2 text-center text-xs w-12">Total</th>
              </tr>
              <tr class="bg-slate-100 text-slate-700 text-[11px]">
                <th v-for="c in colonnes" :key="c.tranche" class="px-0.5 py-1.5 text-center min-w-[42px] border-r border-slate-200 font-semibold whitespace-nowrap"
                    :class="[estHeureCourante(c.tranche) && modifiable(c.tranche) ? 'bg-emerald-200 text-emerald-900' : '', modifiable(c.tranche) ? 'text-emerald-800' : '']">
                  {{ c.tranche }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="l in doc.lignes" :key="l.docLigneId" class="border-b border-slate-100 hover:bg-slate-50/60">
                <td class="sticky left-0 z-10 bg-white px-3 py-1.5 border-r border-slate-200">
                  <div class="text-xs font-semibold text-slate-800 leading-tight">{{ l.libelleAffiche }}</div>
                  <div v-if="l.machineCode && l.machineCode !== '-'" class="text-[10px] text-slate-500">Machine {{ l.machineCode }}</div>
                </td>
                <td v-for="c in colonnes" :key="c.tranche" class="p-0.5 text-center border-r border-slate-100"
                    :class="modifiable(c.tranche) ? 'bg-emerald-50/40' : ''">
                  <input v-if="modifiable(c.tranche)" type="number" min="0" inputmode="numeric"
                         :value="heures[l.docLigneId]?.[c.tranche] ?? ''"
                         @input="saisirDefauts(l.docLigneId, c.tranche, $event.target.value)"
                         class="case-saisie" />
                  <span v-else class="text-slate-500">{{ heures[l.docLigneId]?.[c.tranche] ?? '' }}</span>
                </td>
                <td class="px-1 text-center text-xs font-bold text-slate-800">{{ totalLigne(l.docLigneId) }}</td>
              </tr>
            </tbody>
            <tfoot class="text-xs">
              <tr class="bg-slate-50 border-t-2 border-slate-200">
                <td class="sticky left-0 z-10 bg-slate-50 px-3 py-1.5 font-bold text-slate-700 border-r border-slate-200">Total des défauts</td>
                <td v-for="c in colonnes" :key="c.tranche" class="text-center font-semibold text-slate-700 border-r border-slate-100">{{ totalDefautsTranche(c.tranche) || '' }}</td>
                <td class="text-center font-bold">{{ bilan.defauts }}</td>
              </tr>
              <tr class="bg-white">
                <td class="sticky left-0 z-10 bg-white px-3 py-1.5 font-bold text-slate-700 border-r border-slate-200">Pièces réalisées</td>
                <td v-for="c in colonnes" :key="c.tranche" class="p-0.5 text-center border-r border-slate-100"
                    :class="modifiable(c.tranche) ? 'bg-emerald-50/40' : ''">
                  <input v-if="modifiable(c.tranche)" type="number" min="0" inputmode="numeric"
                         :value="realisees[c.tranche] ?? ''" @input="saisirRealisees(c.tranche, $event.target.value)"
                         class="case-saisie" />
                  <span v-else class="text-slate-500">{{ realisees[c.tranche] ?? '' }}</span>
                </td>
                <td></td>
              </tr>
              <tr class="bg-slate-50">
                <td class="sticky left-0 z-10 bg-slate-50 px-3 py-1.5 font-bold text-slate-700 border-r border-slate-200">Pièces testées</td>
                <td v-for="c in colonnes" :key="c.tranche" class="text-center text-slate-700 border-r border-slate-100">{{ testeesTranche(c.tranche) || '' }}</td>
                <td class="text-center font-bold">{{ bilan.testees }}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </section>

      <!-- Bilan de la journée -->
      <section class="grid grid-cols-2 md:grid-cols-5 gap-3">
        <div class="bg-white rounded-2xl border border-slate-200 p-4"><p class="m-0 text-xs font-bold uppercase text-slate-500">Défauts</p><p class="m-0 mt-1 text-2xl font-black text-slate-800">{{ bilan.defauts }}</p></div>
        <div class="bg-white rounded-2xl border border-slate-200 p-4"><p class="m-0 text-xs font-bold uppercase text-slate-500">Pièces testées</p><p class="m-0 mt-1 text-2xl font-black text-slate-800">{{ bilan.testees }}</p></div>
        <div class="bg-white rounded-2xl border border-slate-200 p-4"><p class="m-0 text-xs font-bold uppercase text-slate-500">Taux de NC</p><p class="m-0 mt-1 text-2xl font-black" :class="bilan.tauxNc > 0 ? 'text-red-600' : 'text-slate-800'">{{ bilan.tauxNc }} %</p></div>
        <div class="bg-white rounded-2xl border border-slate-200 p-4">
          <p class="m-0 text-xs font-bold uppercase text-slate-500">Pièces rebutées</p>
          <p class="m-0 mt-1 text-2xl font-black text-slate-800">{{ bilan.rebuts }}</p>
          <label v-if="peutSaisir" class="mt-2 flex items-center gap-2 text-xs text-slate-600">
            dont {{ doc.equipeSaisie }}
            <input type="number" min="0" :value="rebutsEquipe ?? ''" @input="saisirRebuts($event.target.value)"
                   class="case-saisie w-14" />
          </label>
        </div>
        <div class="bg-white rounded-2xl border border-slate-200 p-4"><p class="m-0 text-xs font-bold uppercase text-slate-500">Conformes</p><p class="m-0 mt-1 text-2xl font-black text-emerald-700">{{ bilan.conformes }}</p></div>
      </section>

      <!-- Cumul de l'OF sur le poste : jamais plus de pièces conformes que la quantité lancée -->
      <section v-if="doc.quantiteLancee > 0" class="rounded-2xl border p-4 flex flex-wrap items-center gap-3"
               :class="quantiteDepassee ? 'bg-red-50 border-red-300' : 'bg-white border-slate-200'">
        <i class="pi text-xl" :class="quantiteDepassee ? 'pi-exclamation-triangle text-red-600' : 'pi-box text-slate-400'"></i>
        <div class="flex-1 min-w-[220px]">
          <p class="m-0 text-xs font-bold uppercase" :class="quantiteDepassee ? 'text-red-700' : 'text-slate-500'">
            Pièces conformes de l'OF sur le poste {{ doc.posteCode }} (tous les jours)
          </p>
          <p class="m-0 mt-1 text-lg font-black" :class="quantiteDepassee ? 'text-red-700' : 'text-slate-800'">
            {{ cumulConformes }} / {{ doc.quantiteLancee }} <span class="text-sm font-semibold">lancées</span>
          </p>
          <p v-if="quantiteDepassee" class="m-0 mt-1 text-sm text-red-800">
            Plus de pièces conformes que la quantité lancée : vérifiez les pièces réalisées et rebutées.
            Si la quantité est réelle, signalez-le au superviseur (risque de non-conformité).
          </p>
        </div>
        <div class="w-full md:w-56 h-2.5 rounded-full bg-slate-100 overflow-hidden">
          <div class="h-full transition-all" :class="quantiteDepassee ? 'bg-red-500' : 'bg-emerald-500'"
               :style="{ width: Math.min(100, Math.round(cumulConformes * 100 / doc.quantiteLancee)) + '%' }"></div>
        </div>
      </section>

      <section v-if="doc.planRemarques || doc.planLegendeMoyens" class="bg-white rounded-2xl border border-slate-200 p-4 text-sm text-slate-600 space-y-1">
        <p v-if="doc.planRemarques" class="m-0"><strong>Remarques :</strong> {{ doc.planRemarques }}</p>
        <p v-if="doc.planLegendeMoyens" class="m-0"><strong>Légende des moyens :</strong> {{ doc.planLegendeMoyens }}</p>
      </section>

      <!-- Actions : toujours visibles -->
      <div v-if="peutSaisir" class="sticky bottom-0 z-30 -mx-4 md:-mx-6 bg-white/95 backdrop-blur border-t border-slate-200 px-4 md:px-6 py-3 flex flex-wrap items-center justify-end gap-3">
        <span class="mr-auto text-sm font-semibold" :class="modifie ? 'text-amber-700' : 'text-slate-500'">
          <i class="pi" :class="modifie ? 'pi-circle-fill text-[8px]' : 'pi-check'"></i>
          {{ modifie ? 'Modifications non enregistrées' : 'Tout est enregistré' }}
        </span>
        <button type="button" @click="enregistrer()" :disabled="enregistrement || !modifie"
                class="min-h-[48px] px-5 rounded-xl border border-slate-300 bg-white text-slate-700 font-bold cursor-pointer disabled:opacity-50">
          <i class="pi pi-save mr-2"></i>Enregistrer
        </button>
        <button type="button" @click="validerMesHeures" :disabled="enregistrement"
                class="min-h-[48px] px-5 rounded-xl border-0 font-bold cursor-pointer disabled:opacity-60"
                :class="validation(doc.equipeSaisie) ? 'bg-emerald-100 text-emerald-800' : 'bg-emerald-600 hover:bg-emerald-700 text-white'">
          <i class="pi pi-check mr-2"></i>{{ validation(doc.equipeSaisie) ? `Heures ${doc.equipeSaisie} validées` : `Valider mes heures (${doc.equipeSaisie})` }}
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
/* Case de saisie compacte, à la taille d'un nombre de 1 à 3 chiffres, sans flèches. */
.case-saisie {
  width: 2.5rem;
  height: 1.875rem;
  padding: 0 0.125rem;
  text-align: center;
  font-size: 0.8125rem;
  font-weight: 600;
  color: #1e293b;
  background: #fff;
  border: 1px solid #cbd5e1;
  border-radius: 0.375rem;
  outline: none;
  -moz-appearance: textfield;
  appearance: textfield;
}
.case-saisie:focus {
  border-color: #10b981;
  box-shadow: 0 0 0 1px #10b981;
}
.case-saisie::-webkit-outer-spin-button,
.case-saisie::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
</style>
