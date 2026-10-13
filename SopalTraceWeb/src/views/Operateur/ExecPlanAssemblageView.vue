<template>
  <div class="p-4 md:p-6 w-full space-y-5">
    <!-- En-tête -->
    <EnTetePage :titre="titre" retour libelle-retour="Retour à l'assistant" @retour="retour">
      <template #sous-titre>
        OF <strong class="text-slate-700">{{ execution?.numeroOf || '—' }}</strong>
        <span class="mx-2 text-slate-300">|</span> Article <strong class="text-slate-700">{{ execution?.codeArticle || '—' }}</strong>
        <span class="mx-2 text-slate-300">|</span> Poste <strong class="text-slate-700">{{ posteCode }}</strong>
        <template v-if="execution?.libellePlan">
          <span class="mx-2 text-slate-300">|</span> {{ execution.libellePlan }}
        </template>
      </template>
      <template v-if="!estProduction" #actions>
        <button type="button" @click="charger" :disabled="isLoading"
                class="min-h-[44px] px-4 rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 text-sm font-semibold cursor-pointer flex items-center disabled:opacity-50">
          <i class="pi pi-refresh mr-2" :class="{ 'pi-spin': isLoading }"></i> Rafraîchir
        </button>
      </template>
    </EnTetePage>

    <!-- Production : contrôles ECHANTILLONNAGE_2859 dus selon leur périodicité -->
    <section v-if="estProduction" class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm">
      <AlerteControle :execControleOfId="String(execControleOfId)" :posteCode="posteCode" :equipe="equipe" :aDesControlesReglage="false" :showHeader="false" />
    </section>

    <div v-else-if="isLoading" class="py-16 text-center text-slate-500">
      <i class="pi pi-spin pi-spinner text-3xl"></i>
    </div>

    <div v-else-if="errorMessage" class="rounded-2xl border border-red-200 bg-red-50 p-6 text-center">
      <i class="pi pi-exclamation-triangle text-3xl text-red-500"></i>
      <p class="m-0 mt-2 font-bold text-red-800">Impossible de charger le plan d'assemblage</p>
      <p class="m-0 mt-1 text-sm text-red-700">{{ errorMessage }}</p>
      <button type="button" @click="charger" class="mt-4 min-h-[44px] px-5 rounded-xl bg-red-600 text-white font-bold border-0 cursor-pointer">Réessayer</button>
    </div>

    <!-- Première / dernière pièce : sections de lot attendues maintenant -->
    <section v-else-if="execution" class="space-y-3">
      <!-- Réglage : les contrôles de production du poste s'arrêtent jusqu'à un réglage conforme sur tous les critères -->
      <div v-if="estReglage && sectionsAttendues.length" class="rounded-2xl border p-4 flex flex-wrap items-center gap-3"
           :class="reglageEnCours ? 'border-amber-300 bg-amber-50' : 'border-slate-200 bg-white'">
        <i class="pi pi-wrench text-xl" :class="reglageEnCours ? 'text-amber-700' : 'text-slate-500'"></i>
        <p class="m-0 flex-1 min-w-[220px] text-sm" :class="reglageEnCours ? 'text-amber-900' : 'text-slate-600'">
          <template v-if="reglageEnCours">
            <strong>Réglage en cours.</strong> Les contrôles de production du poste sont suspendus : ils reprennent, avec un nouveau calcul,
            dès qu'un contrôle de réglage est conforme sur tous les critères.
          </template>
          <template v-else>
            Commencez le réglage avant d'intervenir : les contrôles de production du poste s'arrêtent jusqu'à un réglage conforme.
          </template>
        </p>
        <button v-if="!reglageEnCours" type="button" @click="demarrerReglage" :disabled="actionReglage"
                class="min-h-[48px] px-5 rounded-xl bg-amber-600 hover:bg-amber-700 text-white font-bold border-0 cursor-pointer disabled:opacity-60">
          Commencer le réglage
        </button>
        <button v-else type="button" @click="annulerReglage" :disabled="actionReglage"
                class="min-h-[48px] px-5 rounded-xl bg-white hover:bg-amber-100 text-amber-800 font-bold border border-amber-300 cursor-pointer disabled:opacity-60">
          Annuler le réglage
        </button>
      </div>

      <p v-if="sectionsAttendues.length === 0" class="rounded-2xl border border-slate-200 bg-white p-6 text-center text-slate-500 m-0">
        {{ messageAucuneSection }}
      </p>

      <article v-for="sec in sectionsAttendues" :key="sec.id"
               class="flex items-center gap-4 rounded-2xl border bg-white p-4 shadow-sm"
               :class="controle(sec).fait ? 'border-emerald-200' : 'border-amber-200'">
        <span class="w-12 h-12 shrink-0 rounded-xl flex items-center justify-center text-xl"
              :class="controle(sec).fait ? 'bg-emerald-50 text-emerald-600' : 'bg-amber-50 text-amber-700'">
          <i :class="controle(sec).fait ? 'pi pi-check' : 'pi pi-flag'"></i>
        </span>
        <div class="flex-1 min-w-0">
          <span class="text-[11px] font-bold uppercase tracking-wider text-slate-500">{{ libelleMoment(sec) }}</span>
          <h3 class="m-0 text-base font-bold text-slate-800">{{ sec.libelle }}</h3>
          <p class="m-0 mt-1 text-sm text-slate-500">
            {{ sec.lignesPlan.length }} caractéristique(s)
            <template v-if="estALaDemande && sec.nombreControlesEquipe"> · {{ sec.nombreControlesEquipe }} saisie(s) par l'équipe</template>
            <template v-if="controle(sec).fait">
              · <strong :class="controle(sec).resultat === RESULTATS_CONTROLE.NON_CONFORME ? 'text-red-600' : 'text-emerald-700'">
                {{ controle(sec).resultat === RESULTATS_CONTROLE.NON_CONFORME ? 'Non conforme' : 'Conforme' }}
              </strong>
              à {{ heure(controle(sec).dateSaisie) }}<template v-if="controle(sec).equipe"> ({{ controle(sec).equipe }})</template>
            </template>
          </p>
        </div>
        <button type="button" @click="ouvrirSaisie(sec)" :disabled="sec.lignesPlan.length === 0"
                class="shrink-0 min-h-[48px] px-5 rounded-xl font-bold text-sm border-0 cursor-pointer disabled:opacity-50"
                :class="controle(sec).fait ? 'bg-slate-100 hover:bg-slate-200 text-slate-700' : 'bg-blue-600 hover:bg-blue-700 text-white'">
          {{ estALaDemande ? 'Saisir un contrôle' : controle(sec).fait ? 'Ressaisir' : 'Saisir le contrôle' }}
        </button>
      </article>
    </section>

    <!-- Saisie d'une section -->
    <div v-if="saisie.section.value" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" role="dialog" aria-modal="true">
      <div class="bg-white rounded-2xl shadow-2xl w-full max-w-6xl max-h-[90vh] flex flex-col overflow-hidden">
        <div class="bg-slate-800 text-white p-4 flex justify-between items-center gap-3">
          <div>
            <p class="m-0 text-xs font-bold uppercase tracking-wider text-slate-300">{{ libelleMoment(saisie.section.value) }}</p>
            <h2 class="m-0 text-lg font-bold">{{ saisie.section.value.libelle }}</h2>
          </div>
          <button type="button" @click="saisie.fermer" aria-label="Fermer"
                  class="min-w-[44px] min-h-[44px] text-slate-300 hover:text-white text-2xl bg-transparent border-0 cursor-pointer">&times;</button>
        </div>

        <div class="p-4 overflow-y-auto flex-1 bg-gray-50">
          <div class="bg-white border rounded-lg overflow-x-auto shadow-sm">
            <table class="w-full text-sm text-left min-w-[1100px]">
              <thead class="text-xs text-white bg-slate-800">
                <tr>
                  <th class="px-3 py-3 w-64">Caractéristique contrôlée</th>
                  <th v-for="col in colonnesSupplementaires" :key="col" class="px-3 py-3 w-32 uppercase tracking-wider">{{ col }}</th>
                  <th class="px-3 py-3 w-28">Limite</th>
                  <th class="px-3 py-3 w-24">Type</th>
                  <th class="px-3 py-3 w-32">Moyen</th>
                  <th class="px-3 py-3 w-24">Instrument</th>
                  <th class="px-3 py-3 w-36 text-center">
                    <div>Résultat</div>
                    <div class="inline-flex mt-1">
                      <button type="button" @click="saisie.setTous(RESULTATS_CONTROLE.CONFORME)" class="px-2 py-1 text-[10px] uppercase font-bold border-0 rounded-l bg-green-500 text-white cursor-pointer">Tout C</button>
                      <button type="button" @click="saisie.setTous(RESULTATS_CONTROLE.NON_CONFORME)" class="px-2 py-1 text-[10px] uppercase font-bold border-0 rounded-r bg-red-500 text-white cursor-pointer">Tout NC</button>
                    </div>
                  </th>
                  <th class="px-3 py-3 w-44">Détail NC</th>
                  <th class="px-3 py-3 w-44">Action corrective</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(lc, idx) in saisie.lignes.value" :key="lc.ligneId" class="border-b"
                    :class="lc.resultat === RESULTATS_CONTROLE.NON_CONFORME ? 'bg-red-50/60' : !lc.resultat ? 'bg-amber-50/60' : ''">
                  <td class="px-3 py-3 font-semibold text-gray-900">{{ lc.caracteristique }}</td>
                  <td v-for="col in colonnesSupplementaires" :key="col" class="px-3 py-3 text-gray-700 text-xs font-mono">
                    {{ lignePlan(idx)?.extraColonnes?.[col] || '-' }}
                  </td>
                  <td class="px-3 py-3 text-gray-600 font-mono text-xs">{{ lignePlan(idx)?.limiteSpecTexte || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs uppercase">{{ lignePlan(idx)?.typeControle || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ lignePlan(idx)?.moyenControle || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs font-mono">{{ lignePlan(idx)?.instrument || '-' }}</td>
                  <td class="px-3 py-3 text-center">
                    <div class="inline-flex">
                      <button type="button" @click="saisie.setResultat(lc, RESULTATS_CONTROLE.CONFORME)"
                              :class="lc.resultat === RESULTATS_CONTROLE.CONFORME ? 'bg-green-500 text-white border-green-600' : 'bg-white text-gray-700'"
                              class="min-w-[48px] min-h-[40px] text-sm font-bold border cursor-pointer rounded-l-lg">C</button>
                      <button type="button" @click="saisie.setResultat(lc, RESULTATS_CONTROLE.NON_CONFORME)"
                              :class="lc.resultat === RESULTATS_CONTROLE.NON_CONFORME ? 'bg-red-500 text-white border-red-600' : 'bg-white text-gray-700'"
                              class="min-w-[48px] min-h-[40px] text-sm font-bold border-t border-b border-r cursor-pointer rounded-r-lg">NC</button>
                    </div>
                  </td>
                  <td class="px-3 py-3">
                    <input v-if="lc.resultat === RESULTATS_CONTROLE.NON_CONFORME" v-model="lc.nc" type="text" maxlength="500"
                           placeholder="Obligatoire" class="w-full border rounded p-2 text-xs bg-white" :class="!lc.nc?.trim() ? 'border-red-400' : ''" />
                  </td>
                  <td class="px-3 py-3">
                    <input v-if="lc.resultat === RESULTATS_CONTROLE.NON_CONFORME" v-model="lc.actionCorrective" type="text" maxlength="500"
                           placeholder="Facultatif" class="w-full border rounded p-2 text-xs bg-white" />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="border-t p-4 bg-white flex items-center justify-end gap-3">
          <span class="mr-auto text-sm font-semibold" :class="saisie.estValide.value ? 'text-emerald-700' : 'text-amber-700'">
            {{ saisie.renseignees.value }} / {{ saisie.lignes.value.length }} caractéristique(s) renseignée(s)
          </span>
          <button type="button" @click="saisie.fermer" class="min-h-[48px] px-5 border text-gray-600 font-bold rounded-xl bg-white cursor-pointer">Annuler</button>
          <button type="button" @click="saisie.enregistrer" :disabled="saisie.isSaving.value || !saisie.estValide.value"
                  class="min-h-[48px] px-6 bg-blue-600 text-white font-bold rounded-xl border-0 cursor-pointer flex items-center gap-2 disabled:opacity-60">
            <i class="pi" :class="saisie.isSaving.value ? 'pi-spin pi-spinner' : 'pi-save'"></i> Valider
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useAppToast } from '@/composables/useAppToast'
import { useRoute, useRouter } from 'vue-router'
import AlerteControle from '@/components/Operateur/AlerteControle.vue'
import EnTetePage from '@/components/Shared/EnTetePage.vue'
import execAssemblageService from '@/services/execAssemblageService'
import { useSaisieControleLot } from '@/composables/Operateur/assemblage/useSaisieControleLot'
import { logger } from '@/utils/logger'
import { MOMENTS_LOT, RESULTATS_CONTROLE } from '@/constants/execution'

const route = useRoute()
const router = useRouter()
const toast = useAppToast()

const execControleOfId = ref(route.params.execControleOfId)
const posteCode = ref(route.query.posteCode || '')
/** Équipe de la session ouverte, transmise par l'assistant (le serveur prend sinon la session ouverte de l'OF). */
const equipe = ref(route.query.equipe || null)
/** PREMIERE_PIECE, DERNIERE_PIECE, REGLAGE, 100PCT ou PRODUCTION (choisi par l'assistant). */
const moment = computed(() => route.query.moment || MOMENTS_LOT.PREMIERE_PIECE)
const estProduction = computed(() => moment.value === 'PRODUCTION')
const estDernierePiece = computed(() => moment.value === MOMENTS_LOT.DERNIERE_PIECE)
const estReglage = computed(() => moment.value === MOMENTS_LOT.REGLAGE)
/** Réglage ou toutes les pièces : l'opérateur saisit quand il le juge nécessaire, autant de fois que voulu. */
const estALaDemande = computed(() => [MOMENTS_LOT.REGLAGE, MOMENTS_LOT.TOUTES_PIECES].includes(moment.value))

const execution = ref(null)
const isLoading = ref(false)
const errorMessage = ref('')

const titre = computed(() => {
  if (estProduction.value) return 'Contrôles de production'
  if (moment.value === MOMENTS_LOT.REGLAGE) return 'Contrôle aux réglages'
  if (moment.value === MOMENTS_LOT.TOUTES_PIECES) return 'Contrôle de toutes les pièces'
  return estDernierePiece.value ? 'Contrôle de la dernière pièce' : 'Contrôle de la première pièce'
})

const controle = (sec) => {
  if (estALaDemande.value) return sec.dernierControle
  return estDernierePiece.value ? sec.dernierePiece : sec.premierePiece
}
const sectionsAttendues = computed(() => estALaDemande.value
  ? (execution.value?.sectionsALaDemande ?? []).filter(s => s.contexte === moment.value)
  : (execution.value?.sections ?? []).filter(s => controle(s).requis))

const libelleMoment = (sec) => {
  if (!sec) return ''
  if (estALaDemande.value) return moment.value === MOMENTS_LOT.REGLAGE ? 'Aux réglages' : 'Toutes les pièces'
  const debut = sec.typeSection === 'LOT_OF' ? 'Début d\'OF' : 'Début de poste'
  const fin = sec.typeSection === 'LOT_OF' ? 'Fin d\'OF' : 'Fin de poste'
  return estDernierePiece.value ? `${fin} — dernière pièce` : `${debut} — première pièce`
}

const messageAucuneSection = computed(() => estALaDemande.value
  ? "Le plan de ce poste n'a pas de section pour ce contrôle."
  : "Aucun contrôle de lot n'est attendu à ce moment dans le plan de ce poste.")

const heure = (date) => (date ? new Date(date).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }) : '')

/** En-tête (OF, article, plan) pour tous les moments ; les sections ne servent qu'aux contrôles de lot. */
const charger = async () => {
  isLoading.value = true
  errorMessage.value = ''
  try {
    execution.value = (await execAssemblageService.getExecution(execControleOfId.value, posteCode.value)).data
  } catch (err) {
    logger.error('Erreur chargement plan assemblage', err)
    errorMessage.value = err.message || 'Erreur inconnue'
  } finally {
    isLoading.value = false
  }
}

// ──────────── Réglage : état tenu par le serveur (contrôles de production suspendus) ────────────
const reglageEnCours = ref(false)
const actionReglage = ref(false)

const chargerEtatReglage = async () => {
  if (!estReglage.value) return
  try {
    reglageEnCours.value = !!(await execAssemblageService.getAvancement(execControleOfId.value, posteCode.value, equipe.value)).data?.reglageEnCours
  } catch (err) {
    logger.error('Erreur lecture état du réglage', err)
  }
}

const actionSurReglage = async (action, message) => {
  actionReglage.value = true
  try {
    await action()
    await chargerEtatReglage()
    toast.success(message)
  } catch (err) {
    toast.error(err.message || 'Action impossible', 'Réglage')
  } finally {
    actionReglage.value = false
  }
}
const demarrerReglage = () => actionSurReglage(
  () => execAssemblageService.demarrerReglage(execControleOfId.value, posteCode.value),
  'Réglage commencé : contrôles de production suspendus.')
const annulerReglage = () => actionSurReglage(
  () => execAssemblageService.annulerReglage(execControleOfId.value, posteCode.value),
  'Réglage annulé : la production reprend.')

/** Après une saisie : rechargement, et pour un réglage, annonce de la reprise ou non des contrôles. */
const apresSaisie = async () => {
  await charger()
  if (!estReglage.value) return
  await chargerEtatReglage()
  if (reglageEnCours.value) toast.warn('Corrigez puis refaites le contrôle : les contrôles de production restent suspendus.', 'Réglage non conforme')
  else toast.success('Les contrôles de production reprennent.', 'Réglage conforme')
}

const saisie = useSaisieControleLot(execControleOfId, posteCode, moment, apresSaisie)

/**
 * Ressaisie d'un contrôle de lot : on repart des dernières réponses de chaque ligne. Un contrôle à la demande
 * (réglage, toutes les pièces) est une nouvelle mesure : il part vide.
 */
const ouvrirSaisie = (sec) => saisie.ouvrir(sec, estALaDemande.value ? [] : (controle(sec)?.lignes ?? []))

const lignePlan = (idx) => saisie.section.value?.lignesPlan?.[idx]
const colonnesSupplementaires = computed(() => {
  const cols = new Set()
  ;(saisie.section.value?.lignesPlan || []).forEach(l => Object.keys(l.extraColonnes || {}).forEach(k => cols.add(k)))
  return [...cols]
})

const retour = () => router.push({
  name: 'operateur-of-fini',
  query: { execControleOfId: execControleOfId.value, posteCode: posteCode.value || undefined, equipe: equipe.value || undefined }
})

onMounted(() => {
  if (!posteCode.value) {
    retour()
    return
  }
  charger()
  chargerEtatReglage()
})
</script>
