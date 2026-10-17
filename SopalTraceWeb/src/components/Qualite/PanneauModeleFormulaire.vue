<script setup>
/**
 * Modèle Excel d'un formulaire (administrateur) : modèle du type, nouveau fichier (vérifié contre les colonnes du
 * formulaire) ou version déjà envoyée du modèle de ce formulaire, aperçu vierge, puis rattachement. Avec une demande,
 * le rattachement la traite en appliquant le choix du superviseur : même version (les documents créés ensuite prennent
 * le modèle), version suivante (formulaire en version suivante, documents actifs archivés puis recréés), ou modèle de
 * la version (nouveau formulaire, nouvelle structure).
 */
import { computed, onMounted, ref } from 'vue'
import { confirmer } from '@/composables/useConfirmation'
import { useAppToast } from '@/composables/useAppToast'
import rapportsService, { ouvrirPdf, telechargerBlob } from '@/services/rapportsService'
import { libelleDemande } from '@/constants/demandesModele'
import ChoixFichierExcel from '@/components/Qualite/ChoixFichierExcel.vue'

const props = defineProps({
  formulaire: { type: Object, required: true },
  role: { type: Object, required: true },
  modeles: { type: Array, required: true },
  demande: { type: Object, default: null }
})
const emit = defineEmits(['fermer', 'termine', 'modeles-modifies'])
const toast = useAppToast()

const PORTEES = [
  { code: 'DOCUMENT', libelle: 'En-tête et pied du document', aide: "Une seule valeur, n'importe où dans la feuille, l'en-tête ou le pied de page." },
  { code: 'SECTION', libelle: 'Titre de section (répété)', aide: 'Sur la ligne du titre de section : répétée pour chaque section.' },
  { code: 'LIGNE', libelle: 'Ligne du tableau (répétée)', aide: 'Tous sur une seule ligne Excel, sous le titre de section : répétée pour chaque caractéristique.' },
  { code: 'COLONNE', libelle: 'Colonnes propres au formulaire', aide: 'Sur la ligne du tableau, comme les champs de ligne : {{Ligne.Colonne.CLE}}.' }
]

const f = computed(() => props.formulaire)
const nomFormulaire = computed(() => `${f.value.codeReference}.${f.value.version}`)
const codeModele = ref(f.value.modeleActuel?.codeModele ?? props.role.modeles[0]?.code ?? '')
const source = ref('FICHIER')
const versionId = ref('')
const fichier = ref(null)
const motif = ref('')
const resultat = ref(null)
const champs = ref([])
const afficherChamps = ref(false)
const documents = ref(null)
const enCours = ref(null)

const versionSuivante = computed(() => props.demande?.changementVersion === 'NEXT_VERSION')
// Seules les versions du modèle de ce formulaire peuvent lui être rattachées (modèle dédié).
const versions = computed(() => (props.modeles.find(m => m.code === codeModele.value)?.versions ?? [])
  .filter(v => v.groupeFormulaire === f.value.groupeId))
const dejaEnPlace = computed(() => !!versionId.value && versionId.value === f.value.modeleActuel?.versionId)
const champsParPortee = computed(() => PORTEES
  .map(p => ({ ...p, champs: champs.value.filter(c => c.portee === p.code) }))
  .filter(p => p.champs.length))
const effet = computed(() => {
  if (!props.demande || props.demande.origine !== 'SUPERVISOR' || !f.value.modeleActuel)
    return `Le modèle met en forme les documents de ${nomFormulaire.value}, y compris ceux déjà créés sur cette version.`
  return versionSuivante.value
    ? `${f.value.codeReference} passe en version ${f.value.version + 1} avec ce modèle ; ses documents actifs sont archivés tels quels puis recréés en version suivante.`
    : `${nomFormulaire.value} garde sa version : les documents déjà créés gardent leur modèle, ceux créés ensuite prennent celui-ci.`
})

const changerModele = () => {
  versionId.value = ''; resultat.value = null; champs.value = []; afficherChamps.value = false
}

const verifier = async () => {
  enCours.value = 'envoi'
  resultat.value = null
  try {
    const reponse = await rapportsService.envoyerVersion(codeModele.value, fichier.value, motif.value.trim(), f.value.id)
    resultat.value = { accepte: true, ...reponse.data }
    versionId.value = reponse.data.version.id
    source.value = 'VERSION'
    fichier.value = null
    emit('modeles-modifies')
  } catch (e) {
    const donnees = e.response?.data ?? {}
    resultat.value = { accepte: false, erreurs: donnees.erreurs ?? [e.message], avertissements: donnees.avertissements ?? [] }
  } finally {
    enCours.value = null
  }
}

/** Aperçu : le modèle vide avec l'identité du formulaire (jamais les données d'un document). */
const apercu = async () => {
  enCours.value = 'apercu'
  try {
    await ouvrirPdf(() => rapportsService.apercuVersion(versionId.value, { formulaireId: f.value.id }), 'Apercu.pdf')
  } catch (e) {
    toast.error(e.message, 'Aperçu impossible')
  } finally {
    enCours.value = null
  }
}

const telecharger = async () => {
  const version = versions.value.find(v => v.id === versionId.value)
  try {
    telechargerBlob(await rapportsService.fichierVersion(versionId.value), `${f.value.codeReference}_M${version?.numero ?? ''}.xlsx`)
  } catch (e) {
    toast.error(e.message)
  }
}

const basculerChamps = async () => {
  afficherChamps.value = !afficherChamps.value
  if (afficherChamps.value && !champs.value.length) {
    try {
      champs.value = await rapportsService.getChamps(codeModele.value, f.value.id)
    } catch (e) {
      toast.error(e.message)
    }
  }
}

const copier = async (texte) => {
  try {
    await navigator.clipboard.writeText(texte)
    toast.success(`${texte} copié : collez-le dans la cellule du modèle.`, 'Champ copié')
  } catch {
    toast.error('Copie impossible : sélectionnez le texte et copiez-le.')
  }
}

const rattacher = async () => {
  if (versionSuivante.value) {
    const n = documents.value?.length ?? 0
    const { confirme } = await confirmer({
      titre: 'Version suivante',
      message: `${f.value.codeReference} passe en version ${f.value.version + 1} avec ce modèle. ${n ? `Ses ${n} document(s) actif(s) seront archivés tels quels puis recréés en version suivante.` : 'Il n\'a aucun document actif.'}`,
      libelleConfirmer: 'Rattacher',
      icone: 'pi pi-check'
    })
    if (!confirme) return
  }
  enCours.value = 'rattacher'
  try {
    const reponse = props.demande
      ? await rapportsService.traiterDemandeModele(props.demande.id, versionId.value, motif.value.trim())
      : await rapportsService.affecterAuFormulaire(versionId.value, f.value.id, motif.value.trim())
    toast.success(reponse.message)
    emit('termine')
  } catch (e) {
    toast.error(e.message, 'Rattachement refusé')
  } finally {
    enCours.value = null
  }
}

onMounted(async () => {
  if (!versionSuivante.value) return
  try {
    documents.value = (await rapportsService.documentsActifsFormulaire(f.value.id)).data || []
  } catch (e) {
    toast.error(e.message)
  }
})
</script>

<template>
  <div class="rounded-xl border border-blue-200 bg-blue-50/40 p-4 flex flex-col gap-4 text-sm">
    <div v-if="demande" class="rounded-lg bg-white border border-blue-100 p-3 flex flex-col gap-1">
      <p class="m-0 font-semibold text-slate-800">{{ libelleDemande(demande) }}</p>
      <p v-if="demande.motif" class="m-0 text-slate-600">« {{ demande.motif }} »</p>
      <p class="m-0 text-slate-500">{{ effet }}</p>
      <ul v-if="versionSuivante && documents?.length" class="m-0 mt-1 pl-5 text-slate-600">
        <li v-for="d in documents" :key="d.id">{{ d.typeDocument }} · {{ d.nom }} · version {{ d.version }} → version {{ d.version + 1 }}</li>
      </ul>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
      <label v-if="role.modeles.length > 1" class="flex flex-col gap-1 font-semibold text-slate-600">
        Modèle de ce type de document
        <select v-model="codeModele" @change="changerModele" class="min-h-[44px] px-3 rounded-lg border border-slate-300 bg-white">
          <option v-for="m in role.modeles" :key="m.code" :value="m.code">{{ m.nom }}</option>
        </select>
      </label>
      <div class="flex flex-col gap-1 font-semibold text-slate-600">
        Fichier
        <div class="flex flex-wrap gap-4 min-h-[44px] items-center font-normal">
          <label class="flex items-center gap-2 cursor-pointer"><input v-model="source" type="radio" value="FICHIER" /> Envoyer un fichier</label>
          <label class="flex items-center gap-2 cursor-pointer" :class="{ 'opacity-50': !versions.length }">
            <input v-model="source" type="radio" value="VERSION" :disabled="!versions.length" /> Reprendre un modèle de ce formulaire
          </label>
        </div>
      </div>
    </div>

    <div v-if="source === 'FICHIER'" class="grid grid-cols-1 md:grid-cols-2 gap-3">
      <div class="flex flex-col gap-1 font-semibold text-slate-600">
        Fichier Excel du modèle
        <ChoixFichierExcel v-model="fichier" @update:model-value="resultat = null" />
      </div>
      <label class="flex flex-col gap-1 font-semibold text-slate-600">
        Commentaire (facultatif)
        <input v-model="motif" maxlength="500" placeholder="ex. Logo 2026"
               class="min-h-[44px] px-3 rounded-lg border border-slate-300 bg-white" />
      </label>
      <div class="md:col-span-2">
        <button type="button" :disabled="!fichier || enCours === 'envoi'" @click="verifier"
                class="min-h-[44px] px-4 rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-bold border-0 cursor-pointer flex items-center gap-2">
          <i class="pi" :class="enCours === 'envoi' ? 'pi-spin pi-spinner' : 'pi-upload'"></i> Vérifier le fichier
        </button>
      </div>
    </div>

    <label v-else class="flex flex-col gap-1 font-semibold text-slate-600 md:max-w-md">
      Modèle de {{ f.codeReference }}
      <select v-model="versionId" class="min-h-[44px] px-3 rounded-lg border border-slate-300 bg-white">
        <option value="" disabled>Choisir un modèle…</option>
        <option v-for="v in versions" :key="v.id" :value="v.id">{{ v.nom }}{{ v.statut === 'DRAFT' ? ' · brouillon' : '' }}</option>
      </select>
    </label>

    <div v-if="resultat" class="rounded-xl border p-3 flex flex-col gap-2"
         :class="resultat.accepte && !dejaEnPlace ? 'bg-emerald-50 border-emerald-200' : 'bg-red-50 border-red-200'">
      <p class="m-0 font-bold" :class="resultat.accepte && !dejaEnPlace ? 'text-emerald-800' : 'text-red-800'">
        <template v-if="!resultat.accepte">Fichier refusé : corrigez ces points dans l'Excel puis renvoyez-le.</template>
        <template v-else-if="dejaEnPlace">Ce fichier est déjà le modèle actuel de {{ nomFormulaire }} : envoyez le nouveau fichier.</template>
        <template v-else>Fichier accepté : modèle « {{ resultat.version?.nom }} ». Vérifiez l'aperçu puis rattachez-le.</template>
      </p>
      <ul v-if="resultat.erreurs?.length" class="m-0 pl-5 text-red-800"><li v-for="e in resultat.erreurs" :key="e">{{ e }}</li></ul>
      <ul v-if="resultat.avertissements?.length" class="m-0 pl-5 text-amber-800"><li v-for="a in resultat.avertissements" :key="a">{{ a }}</li></ul>
    </div>

    <div class="flex flex-wrap gap-2">
      <button type="button" :disabled="!versionId || source === 'FICHIER' || dejaEnPlace || enCours === 'rattacher' || (versionSuivante && documents === null)" @click="rattacher"
              class="min-h-[44px] px-4 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white font-bold border-0 cursor-pointer flex items-center gap-2">
        <i class="pi" :class="enCours === 'rattacher' ? 'pi-spin pi-spinner' : 'pi-check'"></i>
        {{ demande ? 'Rattacher et traiter la demande' : `Rattacher à ${nomFormulaire}` }}
      </button>
      <button type="button" :disabled="!versionId || source === 'FICHIER'" @click="apercu"
              class="min-h-[44px] px-4 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-50 text-slate-700 font-semibold cursor-pointer flex items-center gap-2">
        <i class="pi pi-eye"></i> Aperçu vierge
      </button>
      <button type="button" :disabled="!versionId || source === 'FICHIER'" @click="telecharger"
              class="min-h-[44px] px-4 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-50 text-slate-700 font-semibold cursor-pointer flex items-center gap-2">
        <i class="pi pi-download"></i> Excel
      </button>
      <button type="button" @click="emit('fermer')"
              class="min-h-[44px] px-4 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 font-semibold cursor-pointer">Fermer</button>
    </div>
    <p v-if="!demande" class="m-0 text-xs text-slate-500">{{ effet }}</p>

    <!-- Champs utilisables dans ce modèle -->
    <div class="border-t border-blue-100 pt-3">
      <button type="button" @click="basculerChamps" :aria-expanded="afficherChamps"
              class="bg-transparent border-0 p-0 font-semibold text-blue-700 cursor-pointer flex items-center gap-1">
        <i class="pi" :class="afficherChamps ? 'pi-chevron-up' : 'pi-chevron-down'"></i> Champs à écrire dans l'Excel de ce formulaire
      </button>
      <div v-if="afficherChamps" class="mt-3 flex flex-col gap-4">
        <div v-for="groupe in champsParPortee" :key="groupe.code" class="flex flex-col gap-2">
          <div>
            <h3 class="m-0 text-xs font-black uppercase tracking-wider text-slate-500">{{ groupe.libelle }}</h3>
            <p class="m-0 text-xs text-slate-400">{{ groupe.aide }}</p>
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <div v-for="c in groupe.champs" :key="c.nom" class="flex items-start gap-3 rounded-lg border border-slate-100 bg-white p-2">
              <button type="button" @click="copier(c.syntaxe)" :title="`Copier ${c.syntaxe}`"
                      class="shrink-0 px-2 py-1 rounded-md bg-white border border-slate-200 font-mono text-xs text-blue-700 hover:bg-blue-50 cursor-pointer">
                {{ c.syntaxe }}
              </button>
              <p class="m-0 text-xs text-slate-700 min-w-0">{{ c.description }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
