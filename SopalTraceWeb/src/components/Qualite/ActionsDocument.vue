<script setup>
/**
 * Actions sur un document mis en forme : voir le PDF, l'imprimer, télécharger l'Excel rempli et (superviseur
 * qualité) l'Excel du modèle. Le PDF est produit par le serveur au premier clic avec le modèle en vigueur à la création
 * du document, puis conservé. « modeleActuel » : réimpression avec le modèle le plus récent.
 */
import { computed, ref } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useAppToast } from '@/composables/useAppToast'
import rapportsService, { imprimerPdf, ouvrirPdf, telechargerBlob } from '@/services/rapportsService'

const props = defineProps({
  /** { documentEnteteId } ou { execControleDocumentStatutId }, avec nom et modele. */
  source: { type: Object, required: true },
  modeleActuel: { type: Boolean, default: false },
  compact: { type: Boolean, default: false }
})
const emit = defineEmits(['genere'])

const authStore = useAuthStore()
const toast = useAppToast()
const enCours = ref(null)
const superviseur = computed(() => authStore.userRole === 'SUPERVISEUR_QUALITE')
const nom = computed(() => (props.source.nom || 'document').replace(/[^\w-]+/g, '_'))

const executer = async (action, operation) => {
  enCours.value = action
  try {
    await operation()
    if (action !== 'modele') emit('genere')
  } catch (e) {
    toast.error(e.message, 'Document indisponible')
  } finally {
    enCours.value = null
  }
}

const voir = () => executer('pdf', () => ouvrirPdf(() => rapportsService.pdfDocument(props.source, props.modeleActuel), `${nom.value}.pdf`))
const imprimer = () => executer('imprimer', () => imprimerPdf(() => rapportsService.pdfDocument(props.source, props.modeleActuel), `${nom.value}.pdf`))
const excel = () => executer('excel', async () => telechargerBlob(await rapportsService.excelDocument(props.source, props.modeleActuel), `${nom.value}.xlsx`))
const modele = () => executer('modele', async () =>
  telechargerBlob(await rapportsService.modeleDocument(props.source, props.modeleActuel), `Modele_${nom.value}.xlsx`))

const icone = (action, base) => enCours.value === action ? 'pi-spin pi-spinner' : base
const bouton = 'min-h-[40px] px-3 rounded-lg text-sm font-semibold flex items-center gap-2 cursor-pointer disabled:opacity-50 disabled:cursor-wait'
</script>

<template>
  <div class="flex flex-wrap gap-2">
    <button type="button" :disabled="!!enCours" @click="voir" :class="[bouton, 'bg-slate-800 hover:bg-slate-900 text-white font-bold border-0']">
      <i class="pi" :class="icone('pdf', 'pi-eye')"></i> {{ modeleActuel ? 'PDF (modèle actuel)' : 'PDF' }}
    </button>
    <button type="button" :disabled="!!enCours" @click="imprimer" :class="[bouton, 'border border-slate-200 bg-white hover:bg-slate-50 text-slate-700']">
      <i class="pi" :class="icone('imprimer', 'pi-print')"></i><span v-if="!compact">Imprimer</span>
    </button>
    <button type="button" :disabled="!!enCours" @click="excel" title="Excel rempli" :class="[bouton, 'border border-slate-200 bg-white hover:bg-slate-50 text-slate-700']">
      <i class="pi" :class="icone('excel', 'pi-file-excel')"></i><span v-if="!compact">Excel</span>
    </button>
    <button v-if="superviseur" type="button" :disabled="!!enCours" @click="modele" title="Fichier Excel du modèle (vide, avec ses champs)"
            :class="[bouton, 'border border-slate-200 bg-white hover:bg-slate-50 text-slate-700']">
      <i class="pi" :class="icone('modele', 'pi-download')"></i><span v-if="!compact">Modèle Excel</span>
    </button>
  </div>
</template>
