<script setup>
/**
 * Prise de postes d'un OF par une équipe. Plusieurs équipes peuvent travailler en même temps sur l'OF, une seule
 * à la fois par poste : un poste tenu par une autre équipe se libère quand elle le termine. L'équipe peut aussi
 * venir seulement clôturer l'OF, si aucune autre équipe n'y travaille.
 */
import { ref, computed, watch } from 'vue'
import Dialog from 'primevue/dialog'
import MultiSelect from 'primevue/multiselect'
import Button from 'primevue/button'
import Dropdown from 'primevue/dropdown'

const props = defineProps({
  visible: { type: Boolean, required: true },
  ofClique: { type: Object, default: null },
  postesExistants: { type: Array, default: () => [] },
  postesAAjouter: { type: Array, default: () => [] },
  isDejaEnCours: { type: Boolean, default: false },
  isSubmitting: { type: Boolean, default: false }
})

const emit = defineEmits(['update:visible', 'confirmer', 'ouvrir'])

const EQUIPES = ['E1', 'E2', 'E3']

const postesSelectionnes = ref([])
const equipeSelectionnee = ref(null)
const pourCloturer = ref(false)

/** Équipes ouvertes sur l'OF et postes qu'elles tiennent. */
const equipesOuvertes = computed(() => props.ofClique?.equipes ?? [])
const maSession = computed(() => equipesOuvertes.value.find(e => e.equipe === equipeSelectionnee.value) ?? null)
const autresEquipes = computed(() => equipesOuvertes.value.filter(e => e.equipe !== equipeSelectionnee.value))
const clotureEnCours = computed(() => autresEquipes.value.find(e => e.clotureDemandee) ?? null)
/** Poste → équipe qui le tient (autre que la mienne). */
const titulaires = computed(() => Object.fromEntries(autresEquipes.value.flatMap(e => e.postes.map(p => [p, e.equipe]))))

const optionsPostes = computed(() => props.postesAAjouter.map(p => {
  const titulaire = titulaires.value[p.codePoste]
  const mien = maSession.value?.postes.includes(p.codePoste)
  return {
    ...p,
    libelleAffiche: titulaire ? `${p.libelle} — tenu par ${titulaire}` : mien ? `${p.libelle} — déjà pris par votre équipe` : p.libelle,
    indisponible: !!titulaire || !!mien
  }
}))

watch(() => props.visible, (visible) => {
  if (!visible) return
  equipeSelectionnee.value = null
  pourCloturer.value = false
  postesSelectionnes.value = []
}, { immediate: true })

// Un seul poste libre sur l'OF : proposé d'office.
watch(equipeSelectionnee, () => {
  const libres = props.postesExistants.filter(p => !titulaires.value[p] && !maSession.value?.postes.includes(p))
  postesSelectionnes.value = props.postesExistants.length === 1 && libres.length === 1 ? [...libres] : []
  if (autresEquipes.value.length > 0) pourCloturer.value = false
})

const libelleConfirmer = computed(() => {
  if (!props.isDejaEnCours) return "Démarrer l'assemblage"
  if (pourCloturer.value) return "Clôturer l'OF"
  return maSession.value ? 'Ajouter ces postes' : 'Commencer mon poste'
})
const peutConfirmer = computed(() => !!equipeSelectionnee.value && !clotureEnCours.value
  && (pourCloturer.value || postesSelectionnes.value.length > 0))

const fermer = () => emit('update:visible', false)
const confirmer = () => emit('confirmer', {
  postes: pourCloturer.value ? [] : postesSelectionnes.value,
  equipe: equipeSelectionnee.value,
  pourCloturer: pourCloturer.value
})
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="val => emit('update:visible', val)"
    modal
    :header="isDejaEnCours ? `Prendre des postes · ${ofClique?.numeroOf}` : `Démarrer l'assemblage · ${ofClique?.numeroOf}`"
    :style="{ width: '540px' }"
    :breakpoints="{ '575px': '94vw' }"
    :closable="!isSubmitting"
  >
    <div class="flex flex-col gap-5 mt-2">
      <div class="bg-gray-50 rounded-lg p-3 text-sm text-gray-600">
        <span class="font-semibold text-gray-800">Article :</span> {{ ofClique?.designationArticle }}<br>
        <span class="font-semibold text-gray-800">Qté lancée :</span> {{ ofClique?.quantiteLancee ?? '-' }}
        <template v-if="postesExistants.length"><br><span class="font-semibold text-gray-800">Postes de l'OF :</span> {{ postesExistants.join(', ') }}</template>
      </div>

      <!-- Équipes déjà sur l'OF : chacune tient ses postes -->
      <div v-if="equipesOuvertes.length" class="rounded-lg border border-slate-200 p-3 text-sm">
        <p class="m-0 mb-2 font-bold text-slate-700"><i class="pi pi-users mr-1 text-slate-400"></i>Équipes sur cet OF</p>
        <ul class="m-0 p-0 list-none space-y-1">
          <li v-for="e in equipesOuvertes" :key="e.equipe" class="flex flex-wrap items-center gap-2">
            <strong class="text-slate-800">{{ e.equipe }}</strong>
            <span class="px-2 py-0.5 rounded-full text-[11px] font-bold"
                  :class="e.clotureDemandee ? 'bg-red-50 text-red-700' : e.enPause ? 'bg-amber-50 text-amber-800' : 'bg-emerald-50 text-emerald-700'">
              {{ e.clotureDemandee ? 'clôture l\'OF' : e.enPause ? 'en pause' : 'en production' }}
            </span>
            <span class="text-slate-500">{{ e.postes.length ? e.postes.join(', ') : 'aucun poste' }}</span>
          </li>
        </ul>
      </div>

      <div>
        <label class="block text-sm font-bold text-gray-700 mb-2">
          <i class="pi pi-users mr-1 text-primary"></i>
          Votre équipe *
        </label>
        <Dropdown v-model="equipeSelectionnee" :options="EQUIPES" placeholder="Choisir l'équipe..." class="w-full" />
      </div>

      <template v-if="equipeSelectionnee">
        <!-- Mon équipe travaille déjà sur l'OF : elle continue dans l'assistant (et peut prendre d'autres postes). -->
        <div v-if="maSession" class="rounded-lg border border-blue-200 bg-blue-50 p-3 text-sm text-blue-900">
          <i class="pi pi-info-circle mr-1"></i>
          Votre équipe a déjà une session {{ maSession.enPause ? 'en pause' : 'en cours' }} sur cet OF
          <template v-if="maSession.postes.length"> ({{ maSession.postes.join(', ') }})</template>.
          <div class="mt-3">
            <Button label="Ouvrir l'assistant" icon="pi pi-arrow-right" size="small" @click="emit('ouvrir', { equipe: equipeSelectionnee })" />
          </div>
        </div>

        <div v-if="clotureEnCours" class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-900">
          <i class="pi pi-lock mr-1"></i>
          L'équipe <strong>{{ clotureEnCours.equipe }}</strong> clôture l'OF : aucune autre équipe ne peut commencer.
        </div>

        <template v-else>
          <div v-if="isDejaEnCours && !maSession?.enPause" class="flex items-start gap-2 rounded-lg border border-slate-200 p-3"
               :class="{ 'opacity-60': autresEquipes.length }">
            <input id="pourCloturer" v-model="pourCloturer" type="checkbox" class="mt-1 w-4 h-4" :disabled="autresEquipes.length > 0" />
            <label for="pourCloturer" class="text-sm text-slate-700 cursor-pointer">
              <strong>Je viens seulement clôturer l'OF</strong><br>
              <span class="text-slate-500">
                {{ autresEquipes.length
                  ? 'Impossible tant qu\'une autre équipe travaille sur l\'OF : chacune doit terminer ses postes.'
                  : 'Aucun poste démarré : seuls les contrôles de fin d\'OF sont demandés.' }}
              </span>
            </label>
          </div>

          <div v-if="!pourCloturer && !maSession?.enPause">
            <label class="block text-sm font-bold text-gray-700 mb-2">
              <i class="pi pi-play mr-1 text-primary"></i>
              Postes que votre équipe prend {{ maSession ? '(en plus des siens)' : '*' }}
            </label>
            <MultiSelect
              v-model="postesSelectionnes"
              :options="optionsPostes"
              optionLabel="libelleAffiche"
              optionValue="codePoste"
              optionDisabled="indisponible"
              placeholder="Sélectionner des postes"
              :maxSelectedLabels="5"
              class="w-full"
              display="chip"
            />
            <small class="text-gray-500 mt-1 block">
              Chaque poste suit son propre parcours (échantillonnage, démarrage, production, fin de poste). Un poste tenu
              par une autre équipe se libère quand elle le termine.
            </small>
          </div>
        </template>
      </template>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 mt-4">
        <Button label="Annuler" icon="pi pi-times" severity="secondary" text @click="fermer" :disabled="isSubmitting" />
        <Button v-if="!maSession?.enPause" :label="libelleConfirmer" :icon="pourCloturer ? 'pi pi-lock' : 'pi pi-play'"
                :severity="pourCloturer ? 'danger' : undefined" @click="confirmer" :loading="isSubmitting" :disabled="!peutConfirmer" />
      </div>
    </template>
  </Dialog>
</template>
