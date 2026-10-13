<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { confirmer } from '@/composables/useConfirmation'
import execAssemblageService from '@/services/execAssemblageService'
import operateurService from '@/services/operateurService'
import { useAppToast } from '@/composables/useAppToast'
import { useSignalementsAssemblage } from '@/composables/Operateur/assemblage/useSignalementsAssemblage'
import DocumentManquantDialog from '@/components/Operateur/Assemblage/Dialogs/DocumentManquantDialog.vue'
import { logger } from '@/utils/logger'
import EnTetePage from '@/components/Shared/EnTetePage.vue'
import { ETATS_VERIF_MACHINE, LIBELLES_PERIODICITE, PERIODICITES } from '@/constants/execution'

/**
 * Vérifications machine d'un poste pour le moment demandé par l'assistant (démarrage, après la pause,
 * fin de poste) : une ligne par machine avec son état, ajout d'une machine, historique du poste.
 * La validation se fait dans l'écran de saisie, point par point ; l'état affiché vient du serveur.
 */
const route = useRoute()
const router = useRouter()
const toast = useAppToast()

const execControleOfId = route.params.execControleOfId
const posteCode = ref(route.query.posteCode || '')
const periodicite = computed(() => route.query.periodicite || PERIODICITES.DEMARRAGE)
const libelleMoment = computed(() => LIBELLES_PERIODICITE[periodicite.value] || periodicite.value)

const onglet = ref('machines')
const verif = ref(null)
const isLoading = ref(true)
const enCours = ref(false)
const toutesMachines = ref([])
const machineAjoutee = ref('')

const historique = ref([])
const filtres = ref({ machineCode: '', equipe: '', jour: '' })

const ofActif = computed(() => (verif.value ? { numeroOf: verif.value.numeroOf } : null))
const tousLesPostesActifs = computed(() => [posteCode.value])
const { documentManquant, isSignaling, fermerSignalement, signalerDocumentManquant, signalerPlanMachineManquant } =
  useSignalementsAssemblage({ selectedPoste: posteCode, tousLesPostesActifs, ofActif })

const machinesAjoutables = computed(() => {
  const presentes = new Set((verif.value?.machines ?? []).map(m => m.machineCode))
  return toutesMachines.value.filter(m => !presentes.has(m.codeMachine))
})
const progression = computed(() => (verif.value?.attendues ? Math.round(100 * verif.value.validees / verif.value.attendues) : 100))
const toutValide = computed(() => !!verif.value && verif.value.validees === verif.value.attendues)
const machinesHistorique = computed(() => [...new Set([
  ...(verif.value?.machines ?? []).map(m => m.machineCode),
  ...historique.value.map(h => h.machineCode)
])].sort())
const equipesHistorique = computed(() => [...new Set(historique.value.map(h => h.equipe).filter(Boolean))].sort())

const ETATS = {
  [ETATS_VERIF_MACHINE.A_FAIRE]: { libelle: 'À faire', classe: 'bg-amber-100 text-amber-800 border-amber-200', icone: 'pi pi-circle' },
  [ETATS_VERIF_MACHINE.EN_COURS]: { libelle: 'En cours', classe: 'bg-sky-100 text-sky-800 border-sky-200', icone: 'pi pi-spinner' },
  [ETATS_VERIF_MACHINE.VALIDEE]: { libelle: 'Validée', classe: 'bg-emerald-100 text-emerald-700 border-emerald-200', icone: 'pi pi-check' },
  [ETATS_VERIF_MACHINE.NON_PREVUE]: { libelle: 'Non prévue à ce moment', classe: 'bg-slate-100 text-slate-500 border-slate-200', icone: 'pi pi-minus' },
  [ETATS_VERIF_MACHINE.PLAN_MANQUANT]: { libelle: 'Plan manquant', classe: 'bg-red-100 text-red-700 border-red-200', icone: 'pi pi-exclamation-triangle' }
}

const heure = (date) => (date ? new Date(date).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }) : '')
const jour = (date) => (date ? new Date(date).toLocaleDateString('fr-FR') : '—')

const charger = async () => {
  isLoading.value = true
  try {
    verif.value = (await execAssemblageService.getVerifMachinesPoste(execControleOfId, posteCode.value, periodicite.value)).data
  } catch (error) {
    logger.error('Erreur chargement vérifications machine', error)
    toast.error('Erreur', error.message)
  } finally {
    isLoading.value = false
  }
}

const chargerHistorique = async () => {
  try {
    historique.value = (await execAssemblageService.getHistoriqueVerifMachines(execControleOfId, posteCode.value, filtres.value)).data
  } catch (error) {
    toast.error('Erreur', error.message)
  }
}

onMounted(async () => {
  if (!posteCode.value) {
    retour()
    return
  }
  await charger()
  try {
    toutesMachines.value = (await operateurService.getMachines()).data
  } catch (error) {
    logger.warn('Liste des machines indisponible', error)
  }
})

watch(onglet, (valeur) => { if (valeur === 'historique') chargerHistorique() })
watch(filtres, chargerHistorique, { deep: true })

const retour = () => router.push({ name: 'operateur-of-fini', query: { execControleOfId, posteCode: posteCode.value || undefined, equipe: route.query.equipe || undefined } })

/** Ouvre l'écran de saisie de la machine (création du document à la première ouverture). */
const ouvrir = async (machine, { consultation = false } = {}) => {
  enCours.value = true
  try {
    let statutId = machine.statutId
    if (!statutId) {
      statutId = (await execAssemblageService.ajouterMachinePoste(execControleOfId, posteCode.value, machine.machineCode)).data.id
    }
    router.push({
      name: 'operateur-vm-exec',
      params: { id: machine.planId },
      query: { statutId, execControleOfId, posteCode: posteCode.value, periodicite: periodicite.value, assistant: '1', consultation: consultation ? '1' : undefined }
    })
  } catch (error) {
    if (error.status === 404) signalerPlanMachineManquant(machine.machineCode)
    else toast.error('Erreur', error.message)
  } finally {
    enCours.value = false
  }
}

const corriger = async (machine) => {
  const { confirme } = await confirmer({
    titre: `Corriger la vérification de ${machine.machineCode} ?`,
    message: `La vérification « ${libelleMoment.value} » repasse à faire jusqu'à sa nouvelle validation.`,
    libelleConfirmer: 'Corriger',
    icone: 'pi pi-pencil'
  })
  if (!confirme) return
  enCours.value = true
  try {
    await execAssemblageService.corrigerVerifMachine(machine.statutId, periodicite.value)
    await ouvrir(machine)
  } catch (error) {
    toast.error('Erreur', error.message)
  } finally {
    enCours.value = false
  }
}

const ajouterMachine = async () => {
  if (!machineAjoutee.value) return
  enCours.value = true
  try {
    await execAssemblageService.ajouterMachinePoste(execControleOfId, posteCode.value, machineAjoutee.value)
    toast.success('Machine ajoutée', `${machineAjoutee.value} est ajoutée au poste ${posteCode.value} pour votre équipe.`)
    machineAjoutee.value = ''
    await charger()
  } catch (error) {
    if (error.status === 404) signalerPlanMachineManquant(machineAjoutee.value)
    else toast.error('Erreur', error.message)
  } finally {
    enCours.value = false
  }
}

const consulterHistorique = (h) => router.push({
  name: 'operateur-vm-exec',
  params: { id: h.planId },
  query: { statutId: h.statutId, execControleOfId, posteCode: posteCode.value, periodicite: periodicite.value, assistant: '1', consultation: '1' }
})
</script>

<template>
  <div class="p-4 md:p-6 w-full space-y-5">
    <!-- En-tête -->
    <EnTetePage :titre="libelleMoment" :complement="`poste ${posteCode}`"
                :sur-titre="`Vérification machine · OF ${verif?.numeroOf || '…'}`"
                retour libelle-retour="Retour à l'assistant" @retour="retour">
      <template #sous-titre>Équipe <strong class="text-slate-700">{{ verif?.equipe || '—' }}</strong></template>
      <template v-if="verif" #actions>
      <div class="min-w-[240px]">
        <div class="flex justify-between text-sm font-bold text-slate-700 mb-1">
          <span>Machines validées</span><span>{{ verif.validees }} / {{ verif.attendues }}</span>
        </div>
        <div class="h-3 rounded-full bg-slate-100 overflow-hidden">
          <div class="h-full bg-emerald-500 transition-all" :style="{ width: progression + '%' }"></div>
        </div>
      </div>
      </template>
    </EnTetePage>

    <div v-if="verif && !verif.modifiable" class="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
      <i class="pi pi-lock mr-2"></i>Saisie impossible pour le moment (OF en pause, clôturé ou sans équipe active) : les vérifications restent consultables.
    </div>

    <!-- Onglets -->
    <div class="flex gap-2" role="tablist">
      <button v-for="o in [{ cle: 'machines', libelle: 'Machines à vérifier', icone: 'pi-cog' }, { cle: 'historique', libelle: 'Historique du poste', icone: 'pi-history' }]"
              :key="o.cle" type="button" role="tab" :aria-selected="onglet === o.cle" @click="onglet = o.cle"
              class="min-h-[44px] px-5 rounded-xl border font-bold text-sm cursor-pointer"
              :class="onglet === o.cle ? 'bg-slate-800 border-slate-800 text-white' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'">
        <i class="pi mr-2" :class="o.icone"></i>{{ o.libelle }}
      </button>
    </div>

    <div v-if="isLoading" class="py-16 text-center text-slate-500"><i class="pi pi-spin pi-spinner text-3xl"></i></div>

    <!-- Machines du poste pour ce moment -->
    <section v-else-if="onglet === 'machines' && verif" class="space-y-3">
      <article v-for="m in verif.machines" :key="m.machineCode"
               class="flex flex-col md:flex-row md:items-center gap-4 rounded-2xl border bg-white p-4 shadow-sm"
               :class="m.etat === ETATS_VERIF_MACHINE.VALIDEE ? 'border-emerald-200' : m.etat === ETATS_VERIF_MACHINE.PLAN_MANQUANT ? 'border-red-200' : 'border-slate-200'">
        <div class="flex items-center gap-4 flex-1 min-w-0">
          <span class="w-12 h-12 shrink-0 rounded-xl flex items-center justify-center text-lg border" :class="ETATS[m.etat].classe">
            <i :class="ETATS[m.etat].icone"></i>
          </span>
          <div class="min-w-0">
            <div class="flex items-center gap-2 flex-wrap">
              <h3 class="m-0 text-lg font-black text-slate-800">{{ m.machineCode }}</h3>
              <span v-if="m.libelle && m.libelle !== m.machineCode" class="text-sm text-slate-500">{{ m.libelle }}</span>
              <span class="px-2 py-0.5 rounded-full border text-[11px] font-bold uppercase tracking-wide" :class="ETATS[m.etat].classe">{{ ETATS[m.etat].libelle }}</span>
              <span v-if="m.ajoutee" class="px-2 py-0.5 rounded-full bg-violet-100 text-violet-700 text-[11px] font-bold">Ajoutée par l'équipe</span>
            </div>
            <p class="m-0 mt-1 text-sm text-slate-500">
              <template v-if="m.etat === ETATS_VERIF_MACHINE.VALIDEE">Validée à {{ heure(m.valideeLe) }}<template v-if="m.valideePar"> par {{ m.valideePar }}</template> · </template>
              <template v-if="m.pointsTotal">{{ m.pointsRepondus }} / {{ m.pointsTotal }} point(s) répondu(s)</template>
              <template v-else-if="m.etat === ETATS_VERIF_MACHINE.NON_PREVUE">Pas de vérification à ce moment pour cette machine.</template>
              <template v-else-if="m.etat === ETATS_VERIF_MACHINE.PLAN_MANQUANT">Aucun plan de vérification actif : signalez-le au superviseur.</template>
            </p>
          </div>
        </div>

        <div class="flex gap-2 md:justify-end">
          <template v-if="m.etat === ETATS_VERIF_MACHINE.VALIDEE">
            <button type="button" :disabled="enCours" @click="ouvrir(m, { consultation: true })"
                    class="min-h-[48px] px-5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold border-0 cursor-pointer disabled:opacity-50">Voir</button>
            <button v-if="verif.modifiable && m.statutId" type="button" :disabled="enCours" @click="corriger(m)"
                    class="min-h-[48px] px-5 rounded-xl bg-amber-500 hover:bg-amber-600 text-white font-bold border-0 cursor-pointer disabled:opacity-50">Corriger</button>
          </template>
          <button v-else-if="m.etat === ETATS_VERIF_MACHINE.A_FAIRE || m.etat === ETATS_VERIF_MACHINE.EN_COURS" type="button"
                  :disabled="enCours || !verif.modifiable" @click="ouvrir(m)"
                  class="min-h-[48px] px-6 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold border-0 cursor-pointer disabled:opacity-50">
            {{ m.etat === ETATS_VERIF_MACHINE.EN_COURS ? 'Reprendre' : 'Remplir' }}
          </button>
          <button v-else-if="m.etat === ETATS_VERIF_MACHINE.PLAN_MANQUANT" type="button" @click="signalerPlanMachineManquant(m.machineCode)"
                  class="min-h-[48px] px-5 rounded-xl bg-red-600 hover:bg-red-700 text-white font-bold border-0 cursor-pointer">Signaler</button>
        </div>
      </article>

      <p v-if="!verif.machines.length" class="rounded-2xl border border-slate-200 bg-white p-6 text-center text-slate-500 m-0">
        Aucune machine n'est rattachée à ce poste : ajoutez celle que vous utilisez.
      </p>

      <!-- Ajout d'une machine -->
      <div v-if="verif.modifiable" class="rounded-2xl border border-dashed border-slate-300 bg-white p-4 flex flex-col md:flex-row md:items-center gap-3">
        <label for="machine-ajout" class="font-bold text-slate-700 text-sm">Ajouter une machine au poste</label>
        <select id="machine-ajout" v-model="machineAjoutee" class="flex-1 min-h-[48px] border border-slate-300 rounded-xl px-3 bg-white text-slate-800">
          <option value="">Choisir une machine…</option>
          <option v-for="mc in machinesAjoutables" :key="mc.codeMachine" :value="mc.codeMachine">{{ mc.codeMachine }}{{ mc.libelle ? ' — ' + mc.libelle : '' }}</option>
        </select>
        <button type="button" :disabled="!machineAjoutee || enCours" @click="ajouterMachine"
                class="min-h-[48px] px-5 rounded-xl bg-slate-800 hover:bg-slate-900 text-white font-bold border-0 cursor-pointer disabled:opacity-50">
          <i class="pi pi-plus mr-2"></i>Ajouter
        </button>
      </div>

      <button v-if="toutValide" type="button" @click="retour"
              class="w-full min-h-[56px] rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-black border-0 cursor-pointer">
        <i class="pi pi-check-circle mr-2"></i>Toutes les machines sont vérifiées : retour à l'assistant
      </button>
    </section>

    <!-- Historique : vérifications du poste par machine, équipe et jour -->
    <section v-else-if="onglet === 'historique'" class="space-y-3">
      <div class="bg-white rounded-2xl border border-slate-200 p-4 grid grid-cols-1 md:grid-cols-3 gap-3">
        <label class="text-xs font-bold text-slate-500 uppercase">Machine
          <select v-model="filtres.machineCode" class="mt-1 w-full min-h-[44px] border border-slate-300 rounded-xl px-3 bg-white text-slate-800 text-sm normal-case">
            <option value="">Toutes</option>
            <option v-for="mc in machinesHistorique" :key="mc" :value="mc">{{ mc }}</option>
          </select>
        </label>
        <label class="text-xs font-bold text-slate-500 uppercase">Équipe
          <select v-model="filtres.equipe" class="mt-1 w-full min-h-[44px] border border-slate-300 rounded-xl px-3 bg-white text-slate-800 text-sm normal-case">
            <option value="">Toutes</option>
            <option v-for="eq in equipesHistorique" :key="eq" :value="eq">{{ eq }}</option>
          </select>
        </label>
        <label class="text-xs font-bold text-slate-500 uppercase">Jour
          <input v-model="filtres.jour" type="date" class="mt-1 w-full min-h-[44px] border border-slate-300 rounded-xl px-3 bg-white text-slate-800 text-sm" />
        </label>
      </div>

      <div class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-slate-50 text-xs uppercase text-slate-500">
            <tr>
              <th class="text-left px-4 py-3">Machine</th>
              <th class="text-left px-4 py-3">Équipe</th>
              <th class="text-left px-4 py-3">Jour</th>
              <th class="text-left px-4 py-3">Validations</th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="!historique.length"><td colspan="5" class="px-4 py-8 text-center text-slate-400">Aucune vérification pour ces critères.</td></tr>
            <tr v-for="h in historique" :key="h.statutId">
              <td class="px-4 py-3 font-bold text-slate-800">{{ h.machineCode }}</td>
              <td class="px-4 py-3">{{ h.equipe || '—' }} <span v-if="h.sessionActive" class="ml-1 text-[11px] font-bold text-blue-600">(en cours)</span></td>
              <td class="px-4 py-3">{{ jour(h.jour) }}</td>
              <td class="px-4 py-3">
                <span class="inline-flex gap-1 flex-wrap">
                  <span v-for="v in [{ ok: h.demarrage, prevu: h.demarragePrevu, l: 'Démarrage' }, { ok: h.apresPause, prevu: h.apresPausePrevue, l: 'Après pause' }, { ok: h.finPoste, prevu: h.finPostePrevue, l: 'Fin de poste' }].filter(x => x.prevu)" :key="v.l"
                        class="px-2 py-0.5 rounded-full text-[11px] font-bold"
                        :class="v.ok ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-400'">{{ v.l }}</span>
                </span>
              </td>
              <td class="px-4 py-3 text-right">
                <button type="button" :disabled="!h.planId" @click="consulterHistorique(h)"
                        class="min-h-[40px] px-4 rounded-lg bg-slate-100 hover:bg-slate-200 border-0 cursor-pointer font-bold text-slate-700 disabled:opacity-50">Consulter</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <DocumentManquantDialog :document="documentManquant" :isSignaling="isSignaling"
                            @signaler="signalerDocumentManquant" @fermer="fermerSignalement" />
  </div>
</template>
