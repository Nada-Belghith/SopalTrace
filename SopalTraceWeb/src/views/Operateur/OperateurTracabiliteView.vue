<script setup>
/**
 * Registre de traçabilité de l'OF : une ligne par changement de lots (corps, volant, lots des composants scannés
 * par le magasinier). Seule l'équipe en production saisit ; elle confirme ses lots avant sa fin de poste. Une
 * ligne erronée est annulée avec son motif (elle reste dans l'historique). Les règles sont celles du serveur.
 */
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from '@/services/apiClient'
import Dropdown from 'primevue/dropdown'
import AutoComplete from 'primevue/autocomplete'
import { useAppToast } from '@/composables/useAppToast'
import { useOperateurStore } from '@/stores/execution/operateurStore'
import EnTetePage from '@/components/Shared/EnTetePage.vue'

const router = useRouter()
const route = useRoute()
const toast = useAppToast()
const operateurStore = useOperateurStore()

const execControleOfId = route.params.execControleOfId
const posteCode = route.query.posteCode

const registre = ref(null)
const chargement = ref(true)
const envoi = ref(false)

const nouvelle = ref({ corps: '', volant: '', lots: {} })
const validite = ref({ CORPS: null, VOLANT: null })
const suggestions = ref({ CORPS: [], VOLANT: [] })
const timers = {}

const ofContexte = computed(() => operateurStore.activeOfContext || {})
const composants = computed(() => registre.value?.composantsDisponibles || [])
const lignes = computed(() => registre.value?.lignesHistorique || [])
const peutSaisir = computed(() => !!registre.value?.peutSaisir)
const derniereActive = computed(() => [...lignes.value].reverse().find(l => !l.annulee) || null)

const formatDate = (d) => d ? new Date(d).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' }).replace(',', '') : ''
const lotDe = (ligne, codeArticle, designation) =>
  ligne.composants?.find(c => (c.codeArticle && c.codeArticle === codeArticle) || (!c.codeArticle && c.designationComposant === designation))?.lotSelectionne || '—'

/** Nouvelle ligne : reprend les lots de la dernière ligne valide ; un seul lot scanné est choisi d'office. */
const preparerNouvelle = () => {
  const d = derniereActive.value
  const lots = {}
  for (const c of composants.value) {
    const precedent = d ? lotDe(d, c.codeArticle, c.designationComposant) : null
    lots[c.codeArticle] = c.lotsDisponibles.includes(precedent) ? precedent : (c.lotsDisponibles.length === 1 ? c.lotsDisponibles[0] : null)
  }
  nouvelle.value = { corps: d?.corps || '', volant: d?.volant || '', lots }
  validite.value = { CORPS: d?.corps ? true : null, VOLANT: d?.volant ? true : null }
}

const charger = async () => {
  chargement.value = true
  try {
    const res = await apiClient.get(`/Operateur/tracabilite/${execControleOfId}`, { params: { posteCode } })
    registre.value = res.data?.data
    preparerNouvelle()
  } catch (err) {
    toast.error(err.message || 'Impossible de charger le registre de traçabilité.')
  } finally {
    chargement.value = false
  }
}

const valider = (type) => {
  const valeur = (type === 'CORPS' ? nouvelle.value.corps : nouvelle.value.volant)?.trim()
  validite.value[type] = null
  clearTimeout(timers[type])
  if (!valeur) return
  timers[type] = setTimeout(async () => {
    try {
      const res = await apiClient.get(`/Operateur/tracabilite/${execControleOfId}/validate-lot`, { params: { type, lot: valeur } })
      validite.value[type] = res.data?.isValid === true
    } catch {
      validite.value[type] = false
    }
  }, 400)
}

const chercher = async (type, event) => {
  try {
    const res = await apiClient.get(`/Operateur/tracabilite/${execControleOfId}/search-lots`, { params: { type, query: event.query } })
    suggestions.value[type] = res.data?.data || []
  } catch {
    suggestions.value[type] = []
  }
}

const manquant = computed(() => {
  if (!nouvelle.value.corps?.trim() || !nouvelle.value.volant?.trim()) return 'Saisissez le lot du corps et celui du volant.'
  if (validite.value.CORPS === false || validite.value.VOLANT === false) return 'Lot de corps ou de volant non reconnu pour cet article.'
  const sansLot = composants.value.find(c => c.lotsDisponibles.length > 0 && !nouvelle.value.lots[c.codeArticle])
  return sansLot ? `Choisissez le lot du composant ${sansLot.designationComposant}.` : null
})

const ajouterLigne = async () => {
  if (manquant.value) return toast.error(manquant.value)
  envoi.value = true
  try {
    await apiClient.post('/Operateur/tracabilite/ligne', {
      execControleOfId, posteCode,
      corps: nouvelle.value.corps.trim(),
      volant: nouvelle.value.volant.trim(),
      composants: composants.value.filter(c => c.lotsDisponibles.length > 0).map(c => ({
        codeArticle: c.codeArticle, designationComposant: c.designationComposant, lotSelectionne: nouvelle.value.lots[c.codeArticle]
      }))
    })
    toast.success('Ligne ajoutée au registre.')
    await charger()
  } catch (err) {
    toast.error(err.message || 'Impossible d\'ajouter la ligne.')
  } finally {
    envoi.value = false
  }
}

const annuler = async (ligne) => {
  const raison = prompt(`Motif de l'annulation de la ligne du ${formatDate(ligne.dateHeure)} (elle restera visible, barrée) :`)
  if (raison === null) return
  try {
    await apiClient.post(`/Operateur/tracabilite/ligne/${ligne.id}/annuler`, { raison })
    toast.success('Ligne annulée.')
    await charger()
  } catch (err) {
    toast.error(err.message || 'Impossible d\'annuler la ligne.')
  }
}

const confirmer = async () => {
  envoi.value = true
  try {
    await apiClient.post(`/Operateur/tracabilite/${execControleOfId}/confirmer`, null, { params: { posteCode } })
    toast.success(`Lots de l'équipe ${registre.value.equipeSaisie} confirmés.`)
    await charger()
  } catch (err) {
    toast.error(err.message || 'Impossible de confirmer les lots.')
  } finally {
    envoi.value = false
  }
}

const retour = () => router.push({ name: 'operateur-of-fini', query: { execControleOfId, posteCode, equipe: route.query.equipe || undefined } })

const classeValidite = (type) => validite.value[type] === true ? 'border-emerald-500 ring-1 ring-emerald-500'
  : validite.value[type] === false ? 'border-red-500 ring-1 ring-red-500' : 'border-slate-300'

onMounted(charger)
</script>

<template>
  <div class="p-4 md:p-6 w-full space-y-4 pb-28">
    <EnTetePage titre="Registre de traçabilité" retour libelle-retour="Retour à l'assistant" @retour="retour">
      <template #sous-titre>
        OF <strong class="text-slate-700">{{ ofContexte.numeroOf || '—' }}</strong>
        <span class="mx-2 text-slate-300">|</span> Article <strong class="text-slate-700">{{ ofContexte.codeArticle || ofContexte.articleCode || '—' }}</strong>
        <span class="mx-2 text-slate-300">|</span> Poste <strong class="text-slate-700">{{ posteCode }}</strong>
      </template>
      <template v-if="registre" #actions>
        <span v-if="registre.equipeSaisie" class="px-3 py-1.5 rounded-lg bg-emerald-50 text-emerald-800 text-sm font-semibold">
          <i class="pi pi-users mr-1"></i>Équipe {{ registre.equipeSaisie }}
          <template v-if="registre.equipeAConfirme"> · lots confirmés</template>
        </span>
        <span v-for="c in registre.confirmations" :key="c.equipe + c.date" class="px-3 py-1.5 rounded-lg bg-slate-100 text-slate-600 text-sm font-semibold">
          <i class="pi pi-check mr-1 text-emerald-600"></i>{{ c.equipe }} confirmée à {{ formatDate(c.date) }}
        </span>
      </template>
    </EnTetePage>
    <p v-if="registre && !peutSaisir" class="m-0 rounded-lg bg-amber-50 border border-amber-200 px-3 py-2 text-sm text-amber-900">
        <i class="pi pi-lock mr-1"></i>Lecture seule : {{ registre.motifLectureSeule }}
    </p>

    <div v-if="chargement && !registre" class="py-16 text-center text-slate-500"><i class="pi pi-spin pi-spinner text-3xl"></i></div>

    <template v-else-if="registre">
      <p v-if="composants.length === 0" class="m-0 rounded-2xl border border-dashed border-slate-300 bg-white p-4 text-sm text-slate-500">
        <i class="pi pi-info-circle mr-1"></i>Aucun composant scanné par le magasinier pour cet OF : seuls le corps et le volant sont à renseigner.
      </p>

      <section class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="overflow-x-auto">
          <table class="min-w-max w-full text-sm border-collapse">
            <thead class="bg-slate-800 text-white text-xs uppercase tracking-wider">
              <tr>
                <th class="px-4 py-3 text-left">Date</th>
                <th class="px-3 py-3 text-center">Version</th>
                <th class="px-3 py-3 text-left">Équipe · poste · opérateur</th>
                <th class="px-3 py-3 text-left">Corps</th>
                <th class="px-3 py-3 text-left">Volant</th>
                <th v-for="c in composants" :key="c.codeArticle" class="px-3 py-3 text-left">
                  {{ c.designationComposant }}<div class="font-normal normal-case opacity-70">{{ c.codeArticle }}</div>
                </th>
                <th class="px-3 py-3 text-center w-28"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="l in lignes" :key="l.id" class="border-b border-slate-100" :class="l.annulee ? 'bg-slate-50 text-slate-400' : ''">
                <td class="px-4 py-3" :class="l.annulee ? 'line-through' : ''">{{ formatDate(l.dateHeure) }}</td>
                <td class="px-3 text-center"><span class="px-2 py-0.5 rounded-md text-xs font-bold" :class="l.annulee ? 'bg-slate-200' : 'bg-purple-100 text-purple-800'">V{{ l.version }}</span></td>
                <td class="px-3 text-xs">{{ l.equipe || '—' }} · {{ l.posteCode || '—' }} · {{ l.matriculeOperateur || '—' }}</td>
                <td class="px-3 font-semibold" :class="l.annulee ? 'line-through' : 'text-emerald-700'">{{ l.corps || '—' }}</td>
                <td class="px-3 font-semibold" :class="l.annulee ? 'line-through' : 'text-blue-700'">{{ l.volant || '—' }}</td>
                <td v-for="c in composants" :key="c.codeArticle" class="px-3" :class="l.annulee ? 'line-through' : ''">{{ lotDe(l, c.codeArticle, c.designationComposant) }}</td>
                <td class="px-3 text-center">
                  <span v-if="l.annulee" class="text-[11px] italic" :title="l.motifAnnulation">Annulée : {{ l.motifAnnulation }}</span>
                  <button v-else-if="peutSaisir" type="button" @click="annuler(l)"
                          class="min-h-[36px] px-3 rounded-lg text-xs font-bold text-red-600 bg-red-50 hover:bg-red-100 border-0 cursor-pointer">Annuler</button>
                </td>
              </tr>
              <tr v-if="!lignes.length"><td :colspan="6 + composants.length" class="px-4 py-6 text-center text-slate-400">Aucune ligne pour le moment.</td></tr>

              <!-- Nouvelle ligne -->
              <tr v-if="peutSaisir" class="bg-blue-50/40 border-t-2 border-blue-100">
                <td class="px-4 py-3 text-xs font-bold uppercase text-slate-500">Nouvelle ligne</td>
                <td class="px-3 text-center text-xs text-slate-400">auto</td>
                <td class="px-3 text-xs text-slate-500">{{ registre.equipeSaisie }} · {{ posteCode }}</td>
                <td class="px-3 py-2">
                  <AutoComplete v-model="nouvelle.corps" :suggestions="suggestions.CORPS" @complete="chercher('CORPS', $event)"
                                @item-select="valider('CORPS')" @input="valider('CORPS')" placeholder="Lot corps"
                                :inputClass="['w-36 min-h-[40px] bg-white', classeValidite('CORPS')]" />
                </td>
                <td class="px-3 py-2">
                  <AutoComplete v-model="nouvelle.volant" :suggestions="suggestions.VOLANT" @complete="chercher('VOLANT', $event)"
                                @item-select="valider('VOLANT')" @input="valider('VOLANT')" placeholder="Lot volant"
                                :inputClass="['w-36 min-h-[40px] bg-white', classeValidite('VOLANT')]" />
                </td>
                <td v-for="c in composants" :key="c.codeArticle" class="px-3 py-2">
                  <span v-if="!c.lotsDisponibles.length" class="text-xs italic text-slate-400">Aucun lot</span>
                  <span v-else-if="c.lotsDisponibles.length === 1" class="inline-block px-3 py-2 rounded-lg bg-white border border-slate-200 text-slate-700">{{ c.lotsDisponibles[0] }}</span>
                  <Dropdown v-else v-model="nouvelle.lots[c.codeArticle]" :options="c.lotsDisponibles" placeholder="Choisir…" class="w-40" />
                </td>
                <td class="px-3 text-center">
                  <button type="button" @click="ajouterLigne" :disabled="envoi"
                          class="min-h-[44px] px-4 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold border-0 cursor-pointer disabled:opacity-60">
                    <i class="pi pi-plus mr-1"></i>Ajouter
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-if="peutSaisir && manquant" class="m-0 px-4 py-2 text-xs text-amber-700 bg-amber-50 border-t border-amber-100">{{ manquant }}</p>
      </section>

      <div v-if="peutSaisir" class="fixed bottom-0 inset-x-0 z-30 bg-white/95 backdrop-blur border-t border-slate-200 px-4 md:px-6 py-3 flex flex-wrap items-center justify-end gap-3">
        <span class="mr-auto text-sm text-slate-500">
          Ajoutez une ligne à chaque changement de lot. Avant la fin de poste, confirmez les lots de votre équipe
          <template v-if="!derniereActive"> (après avoir ajouté la première ligne)</template>.
        </span>
        <button type="button" @click="confirmer" :disabled="envoi || registre.equipeAConfirme || !derniereActive"
                class="min-h-[48px] px-5 rounded-xl border-0 font-bold cursor-pointer disabled:opacity-60"
                :class="registre.equipeAConfirme ? 'bg-emerald-100 text-emerald-800' : 'bg-emerald-600 hover:bg-emerald-700 text-white'">
          <i class="pi pi-check mr-2"></i>{{ registre.equipeAConfirme ? `Lots ${registre.equipeSaisie} confirmés` : `Confirmer les lots de l'équipe ${registre.equipeSaisie}` }}
        </button>
      </div>
    </template>
  </div>
</template>
