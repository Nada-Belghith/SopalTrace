<script setup>
/**
 * PDF d'un document qualité : le modèle qui le met en forme (celui en vigueur à sa création), les actions (voir,
 * imprimer, Excel) et l'historique des PDF déjà produits. Le PDF est produit au clic, jamais en arrière-plan ; si un
 * modèle plus récent existe, une réimpression avec ce modèle est proposée (l'original reste dans l'historique).
 * Fiche vierge : la fiche que rempliront les opérateurs avec ce document (contrôle au poste, vérification machine,
 * échantillonnage, résultats d'assemblage), mise en forme avec son modèle, réponses vides ; produite à chaque demande.
 */
import { computed, onMounted, ref, watch } from 'vue'
import { useAppToast } from '@/composables/useAppToast'
import ActionsDocument from '@/components/Qualite/ActionsDocument.vue'
import LienRattacherModele from '@/components/Qualite/LienRattacherModele.vue'
import rapportsService, { imprimerPdf, ouvrirPdf, telechargerBlob } from '@/services/rapportsService'

const props = defineProps({
  documentId: { type: String, required: true }
})

const toast = useAppToast()
const etat = ref(null)
const historique = ref(false)

const source = computed(() => etat.value?.source ?? null)
const formaterDate = (d) => new Date(d).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' })

const charger = async () => {
  try {
    etat.value = await rapportsService.getEtat({ documentEnteteId: props.documentId })
  } catch {
    etat.value = null
  }
}

// Fiche vierge : toujours pour un document sans PDF propre (vérification machine, contrôle au poste…, la fiche est son
// PDF) ; à côté du PDF d'un document (plan d'assemblage), seulement si un modèle la met en forme.
const fiche = computed(() => {
  const vierge = etat.value?.ficheVierge
  return vierge && (vierge.modele || !etat.value?.source?.contratDisponible) ? vierge : null
})
const actionFiche = ref(null)
const nomFiche = computed(() => `Fiche_vierge_${(source.value?.nom || 'document').replace(/[^\w-]+/g, '_')}`)

const executerFiche = async (action, operation) => {
  actionFiche.value = action
  try {
    await operation()
  } catch (e) {
    toast.error(e.message, 'Fiche indisponible')
  } finally {
    actionFiche.value = null
  }
}
const voirFiche = () => executerFiche('pdf', () => ouvrirPdf(() => rapportsService.ficheViergePdf(props.documentId), `${nomFiche.value}.pdf`))
const imprimerFiche = () => executerFiche('imprimer', () => imprimerPdf(() => rapportsService.ficheViergePdf(props.documentId), `${nomFiche.value}.pdf`))
const excelFiche = () => executerFiche('excel', async () => telechargerBlob(await rapportsService.ficheViergeExcel(props.documentId), `${nomFiche.value}.xlsx`))

const ouvrir = async (genere) => {
  try {
    await ouvrirPdf(() => rapportsService.pdf(genere.id), `${genere.nomFichier}.pdf`)
  } catch (e) {
    toast.error(e.message)
  }
}

watch(() => props.documentId, charger)
onMounted(charger)
defineExpose({ recharger: charger })
</script>

<template>
  <section v-if="source?.contratDisponible || fiche" class="bg-white rounded-2xl border border-slate-200 p-4 flex flex-col gap-3">
    <div v-if="source.contratDisponible" class="flex flex-wrap items-center gap-3">
      <div class="w-10 h-10 rounded-xl bg-red-50 text-red-600 flex items-center justify-center shrink-0"><i class="pi pi-file-pdf text-xl"></i></div>
      <div class="flex-1 min-w-[200px]">
        <p class="m-0 text-sm font-bold text-slate-800">PDF du document</p>
        <p v-if="source.modele" class="m-0 text-xs text-slate-500">
          Modèle M{{ source.modele.numeroVersion }} · {{ source.modele.explication }}
        </p>
        <p v-else class="m-0 text-xs text-amber-700">{{ source.messageModeleManquant || 'Aucun modèle publié pour ce type de document.' }}</p>
        <LienRattacherModele :source="source" />
      </div>
      <ActionsDocument v-if="source.modele" :source="source" @genere="charger" />
    </div>

    <div v-if="etat.modeleActuel" class="rounded-xl border border-blue-100 bg-blue-50/60 p-3 flex flex-wrap items-center gap-3">
      <p class="m-0 flex-1 min-w-[200px] text-xs text-blue-900">
        Un modèle plus récent existe (M{{ etat.modeleActuel.numeroVersion }}). Ce document, et les OF qui l'utilisent, gardent
        son modèle (M{{ source.modele?.numeroVersion }}) ; vous pouvez seulement le réimprimer avec le modèle actuel, pour voir
        (les champs que le document n'a pas restent vides).
      </p>
      <ActionsDocument :source="source" modele-actuel compact @genere="charger" />
    </div>

    <!-- Fiche vierge : ce que rempliront les opérateurs avec ce document -->
    <div v-if="fiche" class="flex flex-wrap items-center gap-3" :class="{ 'pt-3 border-t border-slate-100': source.contratDisponible }">
      <div class="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0"><i class="pi pi-clipboard text-xl"></i></div>
      <div class="flex-1 min-w-[200px]">
        <p class="m-0 text-sm font-bold text-slate-800">Fiche à remplir par les opérateurs</p>
        <p v-if="fiche.modele" class="m-0 text-xs text-slate-500">
          {{ fiche.typeLibelle }} · modèle M{{ fiche.modele.numeroVersion }}<template v-if="fiche.formulaire"> · formulaire {{ fiche.formulaire }}</template>
          · vierge : les valeurs seront saisies en production
        </p>
        <template v-else>
          <p class="m-0 text-xs text-amber-700">{{ fiche.messageModeleManquant || 'Modèle non disponible pour cette fiche.' }}</p>
          <LienRattacherModele :source="fiche" />
        </template>
      </div>
      <div v-if="fiche.modele" class="flex flex-wrap gap-2">
        <button type="button" :disabled="!!actionFiche" @click="voirFiche"
                class="min-h-[40px] px-3 rounded-lg text-sm font-bold flex items-center gap-2 cursor-pointer bg-slate-800 hover:bg-slate-900 text-white border-0 disabled:opacity-50">
          <i class="pi" :class="actionFiche === 'pdf' ? 'pi-spin pi-spinner' : 'pi-eye'"></i> Fiche vierge
        </button>
        <button type="button" :disabled="!!actionFiche" @click="imprimerFiche"
                class="min-h-[40px] px-3 rounded-lg text-sm font-semibold flex items-center gap-2 cursor-pointer border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 disabled:opacity-50">
          <i class="pi" :class="actionFiche === 'imprimer' ? 'pi-spin pi-spinner' : 'pi-print'"></i> Imprimer
        </button>
        <button type="button" :disabled="!!actionFiche" @click="excelFiche" title="Fiche vierge en Excel"
                class="min-h-[40px] px-3 rounded-lg text-sm font-semibold flex items-center gap-2 cursor-pointer border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 disabled:opacity-50">
          <i class="pi" :class="actionFiche === 'excel' ? 'pi-spin pi-spinner' : 'pi-file-excel'"></i> Excel
        </button>
      </div>
    </div>

    <div v-if="etat.documents?.length">
      <button type="button" @click="historique = !historique" class="p-0 bg-transparent border-0 text-xs font-semibold text-blue-700 cursor-pointer">
        {{ historique ? 'Masquer' : 'Voir' }} l'historique des PDF ({{ etat.documents.length }})
      </button>
      <p v-if="historique" class="m-0 mt-1 text-xs text-slate-500">
        Un PDF est conservé à chaque modification du document ou de son modèle (traçabilité) ; le bouton « PDF » ouvre
        toujours celui des données actuelles.
      </p>
      <ul v-if="historique" class="m-0 mt-2 p-0 list-none divide-y divide-slate-100 text-xs">
        <li v-for="(g, i) in etat.documents" :key="g.id" class="py-2 flex items-center gap-3">
          <span class="flex-1 text-slate-600">
            {{ formaterDate(g.genereLe) }} · modèle M{{ g.versionModele }}
            <span v-if="i === 0" class="ml-1 px-1.5 rounded bg-emerald-100 text-emerald-700 font-semibold">le plus récent</span>
            <template v-if="g.demandeParNom || g.demandePar"> · demandé par {{ g.demandeParNom || g.demandePar }}</template>
          </span>
          <button type="button" @click="ouvrir(g)" class="p-0 bg-transparent border-0 text-blue-700 font-semibold cursor-pointer">PDF</button>
        </li>
      </ul>
    </div>
  </section>
</template>
