<script setup>
/**
 * Modèles Excel (administrateur) : le superviseur qualité définit les formulaires et décide des versions ;
 * l'administrateur rattache à chaque formulaire son modèle Excel dédié (fichier à champs {{…}}). Trois parties :
 *  - Demandes : ce qui attend un modèle — nouveau formulaire, nouvelle version aux colonnes changées (automatiques) ou
 *    demande du superviseur (logo, mise en page) avec son choix « même version » / « version suivante », appliqué tel
 *    quel au rattachement. Une nouvelle version aux mêmes colonnes, ou un changement de code, reprend le modèle seul.
 *  - Formulaires : chaque formulaire, son modèle (M1, M2… dans ce formulaire), l'historique « code.version → modèle »
 *    et le rattachement d'un formulaire encore sans modèle.
 *  - Documents sans formulaire : modèle par défaut de leur type.
 * Un document garde toujours le modèle en vigueur à sa création ; un OF garde celui de sa première utilisation.
 */
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import EnTetePage from '@/components/Shared/EnTetePage.vue'
import PanneauModeleFormulaire from '@/components/Qualite/PanneauModeleFormulaire.vue'
import ChoixFichierExcel from '@/components/Qualite/ChoixFichierExcel.vue'
import { useAppToast } from '@/composables/useAppToast'
import rapportsService, { ouvrirPdf, telechargerBlob } from '@/services/rapportsService'
import { NATURES_FORMULAIRE, STATUTS_DEMANDE, libelleDemande } from '@/constants/demandesModele'

const toast = useAppToast()
const route = useRoute()

const roles = ref([])          // [{ role, libelle, nature, modeles: [{ code, nom }], formulaires }]
const modeles = ref([])        // [{ code, nom, formRole, versionParDefaut, versions, affectations }]
const demandes = ref([])       // toutes les demandes de modèle, les plus récentes d'abord
const chargement = ref(true)
const actionEnCours = ref(null)
const onglet = ref('DEMANDES')
const filtreRole = ref('')
const filtreSansModele = ref(false)
const recherche = ref('')
// Panneau ouvert : `demande-<id>` (traitement d'une demande) ou `formulaire-<id>` (rattachement direct).
const panneau = ref(null)
const historiqueOuvert = ref(null)
const afficherTraitees = ref(false)
// Modèle par défaut d'un document sans formulaire : { codeModele, fichier, motif, resultat, versionId }
const parDefaut = ref(null)

const nomFormulaire = (f) => `${f.codeReference}.${f.version}`
const nature = (code) => NATURES_FORMULAIRE[code] ?? ''
const formaterDate = (d) => d ? new Date(d).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' }) : ''
const modeleParCode = (code) => modeles.value.find(m => m.code === code) ?? null
const roleDe = (code) => roles.value.find(r => r.role === code) ?? null
const formulaireDe = (d) => roleDe(d.role)?.formulaires.find(f => f.id === d.formulaireId) ?? null

// ── Demandes ─────────────────────────────────────────────────────────
const enAttente = computed(() => demandes.value.filter(d => d.statut === 'PENDING'))
const traitees = computed(() => demandes.value.filter(d => d.statut !== 'PENDING'))

// ── Formulaires ──────────────────────────────────────────────────────
const rolesAvecFormulaires = computed(() => roles.value.filter(r => r.formulaires.length))
const lignes = computed(() => {
  const texte = recherche.value.trim().toLowerCase()
  return rolesAvecFormulaires.value
    .filter(r => !filtreRole.value || r.role === filtreRole.value)
    .flatMap(r => r.formulaires.map(f => ({ role: r, formulaire: f })))
    .filter(({ formulaire: f }) => (!filtreSansModele.value || !f.modeleActuel)
      && (!texte || f.codeReference.toLowerCase().includes(texte) || (f.champApplication ?? '').toLowerCase().includes(texte)))
})
const sansModele = computed(() => rolesAvecFormulaires.value.reduce((n, r) => n + r.formulaires.filter(f => !f.modeleActuel).length, 0))
const ONGLETS = computed(() => [
  { code: 'DEMANDES', libelle: 'Demandes', icone: 'pi-inbox', compte: enAttente.value.length },
  { code: 'FORMULAIRES', libelle: 'Formulaires', icone: 'pi-list', compte: sansModele.value },
    { code: 'DEFAUT', libelle: 'Documents sans formulaire', icone: 'pi-file', compte: 0 }
].filter(o => o.code !== 'DEFAUT' || modelesParDefaut.value.length))

const nomModele = (role, code) => role.modeles.find(m => m.code === code)?.nom ?? modeleParCode(code)?.nom ?? code

// Historique « code.version → modèle » du formulaire (toutes ses versions et ses anciens codes), le plus récent d'abord.
const historiqueDe = (f) => modeles.value
  .flatMap(m => (m.affectations ?? []).filter(a => a.groupeFormulaire && a.groupeFormulaire === f.groupeId).map(a => ({ ...a, modele: m })))
  .sort((a, b) => new Date(b.affecteLe) - new Date(a.affecteLe))

const charger = async () => {
  chargement.value = true
  try {
    const [parRole, tous, toutes] = await Promise.all([
      rapportsService.getFormulairesParRole(), rapportsService.getModeles(), rapportsService.demandesModele()
    ])
    roles.value = parRole
    modeles.value = tous
    demandes.value = toutes
  } catch (e) {
    toast.error(e.message)
  } finally {
    chargement.value = false
  }
}

const rechargerModeles = async () => {
  try {
    modeles.value = await rapportsService.getModeles()
  } catch (e) {
    toast.error(e.message)
  }
}

const termine = async () => {
  panneau.value = null
  await charger()
}

const traiterDemande = (d) => {
  onglet.value = 'DEMANDES'
  panneau.value = `demande-${d.id}`
}

/** Aperçu : le modèle vide avec l'identité du formulaire (jamais les données d'un document). */
const apercu = async (versionId, formulaireId) => {
  actionEnCours.value = `apercu-${versionId}`
  try {
    await ouvrirPdf(() => rapportsService.apercuVersion(versionId, formulaireId ? { formulaireId } : undefined), 'Apercu.pdf')
  } catch (e) {
    toast.error(e.message, 'Aperçu impossible')
  } finally {
    actionEnCours.value = null
  }
}

/** Fichier du modèle, nommé « code du formulaire_M<n> ». */
const telecharger = async (nom, versionId) => {
  try {
    telechargerBlob(await rapportsService.fichierVersion(versionId), `${nom}.xlsx`)
  } catch (e) {
    toast.error(e.message)
  }
}

// ── Documents sans formulaire : modèle par défaut ────────────────────
const modelesParDefaut = computed(() => modeles.value.filter(m => !m.formRole))

const publierParDefaut = async () => {
  const p = parDefaut.value
  actionEnCours.value = 'par-defaut'
  p.resultat = null
  try {
    const envoi = await rapportsService.envoyerVersion(p.codeModele, p.fichier, p.motif.trim())
    const reponse = await rapportsService.publierParDefaut(envoi.data.version.id, p.motif.trim())
    toast.success(reponse.message)
    parDefaut.value = null
    await charger()
  } catch (e) {
    p.resultat = { erreurs: e.response?.data?.erreurs ?? [e.message] }
  } finally {
    actionEnCours.value = null
  }
}

onMounted(async () => {
  await charger()
  // Arrivée depuis un document sans modèle : sa demande, sinon le rattachement du formulaire visé.
  const vise = route.query.formulaire
  if (!vise) return
  const demande = enAttente.value.find(d => d.formulaireId === vise)
  if (demande) return traiterDemande(demande)
  const ligne = rolesAvecFormulaires.value.flatMap(r => r.formulaires.map(f => ({ r, f }))).find(x => x.f.id === vise)
  if (ligne) {
    onglet.value = 'FORMULAIRES'
    filtreRole.value = ligne.r.role
    if (!ligne.f.modeleActuel) panneau.value = `formulaire-${ligne.f.id}`
  }
})
</script>

<template>
  <div class="p-4 md:p-8 max-w-7xl mx-auto flex flex-col gap-6">
    <EnTetePage titre="Modèles Excel"
                sous-titre="Le superviseur qualité définit les formulaires et décide des versions ; vous rattachez à chacun son modèle Excel. Chaque document garde le modèle en vigueur à sa création." />

    <nav class="flex flex-wrap gap-2" aria-label="Parties">
      <button v-for="o in ONGLETS" :key="o.code" type="button" @click="onglet = o.code" :aria-pressed="onglet === o.code"
              class="min-h-[44px] px-4 rounded-xl border font-bold text-sm flex items-center gap-2 cursor-pointer"
              :class="onglet === o.code ? 'bg-slate-800 border-slate-800 text-white' : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'">
        <i class="pi" :class="o.icone"></i> {{ o.libelle }}
        <span v-if="o.compte" class="px-1.5 rounded-md text-white text-xs" :class="o.code === 'DEMANDES' ? 'bg-blue-600' : 'bg-red-600'">{{ o.compte }}</span>
      </button>
    </nav>

    <div v-if="chargement" class="flex items-center gap-3 text-slate-500"><i class="pi pi-spin pi-spinner"></i> Chargement…</div>

    <!-- ═════════════ Demandes ═════════════ -->
    <template v-else-if="onglet === 'DEMANDES'">
      <section v-if="!enAttente.length" class="bg-white rounded-2xl border border-dashed border-slate-300 p-8 text-center text-slate-500">
        <i class="pi pi-check-circle text-3xl text-emerald-400"></i>
        <p class="m-0 mt-3 font-semibold text-slate-700">Aucune demande en attente</p>
        <p class="m-0 mt-1 text-sm">Un nouveau formulaire, une nouvelle structure ou une demande du superviseur qualité apparaîtront ici.</p>
      </section>
      <section v-else class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
        <ul class="m-0 p-0 list-none divide-y divide-slate-100">
          <li v-for="d in enAttente" :key="d.id" class="px-5 py-3 flex flex-col gap-3 text-sm">
            <div class="flex flex-col md:flex-row md:items-center gap-2">
              <span class="md:w-56 shrink-0 font-bold text-slate-800 break-all">{{ d.codeFormulaire }}.{{ d.versionFormulaire }}</span>
              <span class="flex-1 min-w-0 text-slate-600">
                <span class="font-semibold">{{ libelleDemande(d) }}</span><template v-if="d.motif"> · « {{ d.motif }} »</template>
                <span class="block text-xs text-slate-400">
                  {{ d.roleLibelle }} · {{ nature(d.nature) }}{{ d.champApplication ? ` · ${d.champApplication}` : '' }}
                  · {{ formaterDate(d.demandeLe) }} par {{ d.demandeParNom || d.demandePar || "l'application" }}
                </span>
              </span>
              <button v-if="panneau !== `demande-${d.id}`" type="button" @click="traiterDemande(d)"
                      class="self-start md:self-auto min-h-[36px] px-3 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold border-0 cursor-pointer flex items-center gap-1">
                <i class="pi pi-link"></i> Traiter
              </button>
            </div>
            <template v-if="panneau === `demande-${d.id}`">
              <PanneauModeleFormulaire v-if="formulaireDe(d)" :formulaire="formulaireDe(d)" :role="roleDe(d.role)" :modeles="modeles" :demande="d"
                                       @fermer="panneau = null" @termine="termine" @modeles-modifies="rechargerModeles" />
              <p v-else class="m-0 text-red-700">Ce formulaire n'est plus en cours : rechargez la page.</p>
            </template>
          </li>
        </ul>
      </section>

      <section v-if="traitees.length" class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
        <button type="button" @click="afficherTraitees = !afficherTraitees" :aria-expanded="afficherTraitees"
                class="w-full px-5 py-4 bg-transparent border-0 flex items-center gap-2 text-left cursor-pointer">
          <i class="pi" :class="afficherTraitees ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
          <span class="flex-1 text-base font-bold text-slate-800">Demandes traitées ou annulées</span>
          <span class="text-xs font-semibold text-slate-500">{{ traitees.length }}</span>
        </button>
        <ul v-if="afficherTraitees" class="m-0 p-0 list-none divide-y divide-slate-100 border-t border-slate-100">
          <li v-for="d in traitees" :key="d.id" class="px-5 py-3 flex flex-col md:flex-row md:items-center gap-2 text-sm">
            <span class="md:w-56 shrink-0 font-bold text-slate-800 break-all">{{ d.codeFormulaire }}.{{ d.versionFormulaire }}</span>
            <span class="flex-1 min-w-0 text-slate-600">
              {{ libelleDemande(d) }}<template v-if="d.motif"> · « {{ d.motif }} »</template>
              <span class="block text-xs text-slate-400">
                demandée le {{ formaterDate(d.demandeLe) }} par {{ d.demandeParNom || d.demandePar || "l'application" }}
                · {{ d.statut === 'DONE' ? 'traitée' : 'annulée' }} le {{ formaterDate(d.traiteLe) }} par {{ d.traiteParNom || d.traitePar }}
                <template v-if="d.numeroModele"> · modèle M{{ d.numeroModele }}</template>
              </span>
            </span>
            <span class="self-start md:self-auto px-2 py-1 rounded-lg text-xs font-semibold"
                  :class="d.statut === 'DONE' ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'">{{ STATUTS_DEMANDE[d.statut] }}</span>
          </li>
        </ul>
      </section>
    </template>

    <!-- ═════════════ Formulaires ═════════════ -->
    <template v-else-if="onglet === 'FORMULAIRES'">
      <section v-if="!rolesAvecFormulaires.length" class="bg-white rounded-2xl border border-dashed border-slate-300 p-8 text-center text-slate-500">
        <i class="pi pi-file text-3xl text-slate-300"></i>
        <p class="m-0 mt-3 font-semibold text-slate-700">Aucun formulaire pour l'instant</p>
        <p class="m-0 mt-1 text-sm">Le superviseur qualité crée les formulaires (page Formulaires) ; ils apparaîtront ici pour recevoir leur modèle.</p>
      </section>

      <template v-else>
        <div class="flex flex-col gap-3">
          <div class="flex flex-wrap gap-2" role="group" aria-label="Type de document">
            <button type="button" @click="filtreRole = ''" :aria-pressed="!filtreRole"
                    class="min-h-[40px] px-3 rounded-lg border text-sm font-semibold cursor-pointer"
                    :class="!filtreRole ? 'bg-slate-800 border-slate-800 text-white' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'">
              Tous
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
            <label class="flex items-center gap-2 text-sm font-semibold text-slate-600 cursor-pointer">
              <input v-model="filtreSansModele" type="checkbox" class="w-4 h-4" /> Seulement les formulaires sans modèle ({{ sansModele }})
            </label>
          </div>
        </div>

        <section class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
          <p v-if="!lignes.length" class="m-0 px-5 py-4 text-sm text-slate-500">Aucun formulaire ne correspond.</p>
          <ul v-else class="m-0 p-0 list-none divide-y divide-slate-100">
            <li v-for="{ role, formulaire: f } in lignes" :key="f.id" class="px-5 py-3 flex flex-col gap-3 text-sm">
              <div class="flex flex-col md:flex-row md:flex-wrap md:items-center gap-2">
                <span class="font-bold text-slate-800 md:w-56 shrink-0 break-all">{{ nomFormulaire(f) }}</span>
                <span class="flex-1 min-w-[200px] text-slate-500">
                  <span class="font-semibold text-slate-600">{{ role.libelle }}</span> · {{ nature(role.nature) }}
                  · {{ f.champApplication || 'sans critère' }}
                  · {{ f.colonnes.length ? `${f.colonnes.length} colonne(s) propre(s)` : 'aucune colonne propre' }}
                </span>
                <span class="flex flex-wrap items-center gap-2 md:justify-end">
                  <span v-if="f.modeleActuel" class="max-w-[20rem] px-2 py-1 rounded-lg border text-xs font-semibold bg-emerald-50 text-emerald-700 border-emerald-200 flex items-center gap-1"
                        :title="f.modeleActuel.explication">
                    <i class="pi pi-check-circle shrink-0"></i>
                    <span class="truncate">{{ f.modeleActuel.nomModele || `${nomModele(role, f.modeleActuel.codeModele)} · M${f.modeleActuel.numeroVersion}` }}</span>
                  </span>
                  <span v-else class="px-2 py-1 rounded-lg border text-xs font-semibold bg-red-50 text-red-700 border-red-200" :title="f.messageModeleManquant">
                    {{ !role.modeles.length ? 'Aucun modèle pour ce type' : f.modeleManquant === 'STRUCTURE_MODIFIEE' ? 'Colonnes changées : modèle à rattacher' : 'Modèle à rattacher' }}
                  </span>
                  <button v-if="f.demandeEnAttente" type="button" @click="traiterDemande(f.demandeEnAttente)"
                          class="min-h-[36px] px-3 rounded-lg bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-bold border border-blue-200 cursor-pointer flex items-center gap-1"
                          :title="f.demandeEnAttente.motif || ''">
                    <i class="pi pi-inbox"></i> Demande en attente · traiter
                  </button>
                  <button v-if="f.modeleActuel" type="button" @click="apercu(f.modeleActuel.versionId, f.id)"
                          :disabled="actionEnCours === `apercu-${f.modeleActuel.versionId}`"
                          class="min-h-[36px] px-3 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold flex items-center gap-1 cursor-pointer">
                    <i class="pi pi-eye"></i> Aperçu
                  </button>
                  <button v-if="role.modeles.length && !f.modeleActuel && !f.demandeEnAttente && panneau !== `formulaire-${f.id}`" type="button"
                          @click="panneau = `formulaire-${f.id}`"
                          class="min-h-[36px] px-3 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold border-0 cursor-pointer flex items-center gap-1">
                    <i class="pi pi-link"></i> Rattacher
                  </button>
                  <button type="button" @click="historiqueOuvert = historiqueOuvert === f.id ? null : f.id" :aria-expanded="historiqueOuvert === f.id"
                          class="min-h-[36px] px-3 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold flex items-center gap-1 cursor-pointer">
                    <i class="pi pi-history"></i> Historique
                  </button>
                </span>
              </div>

              <!-- Historique « code.version → modèle » (toutes les versions et anciens codes du formulaire) -->
              <div v-if="historiqueOuvert === f.id" class="rounded-xl border border-slate-200 bg-slate-50 p-3 flex flex-col gap-2">
                <p v-if="!historiqueDe(f).length" class="m-0 text-slate-500">Aucun modèle n'a encore été rattaché à ce formulaire.</p>
                <ol v-else class="m-0 p-0 list-none flex flex-col gap-2">
                  <li v-for="(a, i) in historiqueDe(f)" :key="a.id" class="flex flex-col md:flex-row md:items-center gap-2 rounded-lg bg-white border border-slate-100 p-2">
                    <span class="md:w-32 shrink-0 text-xs text-slate-500">{{ formaterDate(a.affecteLe) }}</span>
                    <span class="flex-1 min-w-0">
                      <span class="font-semibold text-slate-800">{{ a.codeFormulaire }}.{{ a.versionFormulaire }} → M{{ a.numeroVersion }}</span>
                      · {{ a.nomModele || a.modele.nom }}
                      <span v-if="a.nature === 'FORM_REVISION'" class="ml-1 px-1.5 rounded bg-slate-100 text-slate-600 text-xs">documents créés ensuite</span>
                      <span v-if="i === 0" class="ml-1 px-1.5 rounded bg-emerald-100 text-emerald-700 text-xs font-semibold">le plus récent</span>
                      <span class="block text-xs text-slate-500">
                        {{ a.commentaire || 'Rattachement' }} · par {{ a.affecteParNom || a.affectePar || "l'application" }}
                      </span>
                    </span>
                    <span class="flex gap-2">
                      <button type="button" @click="apercu(a.versionId, f.id)"
                              class="min-h-[32px] px-2 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold flex items-center gap-1 cursor-pointer">
                        <i class="pi pi-eye"></i> Aperçu
                      </button>
                      <button type="button" @click="telecharger(`${a.codeFormulaire}_M${a.numeroVersion}`, a.versionId)"
                              class="min-h-[32px] px-2 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold flex items-center gap-1 cursor-pointer">
                        <i class="pi pi-download"></i> Excel
                      </button>
                    </span>
                  </li>
                </ol>
              </div>

              <PanneauModeleFormulaire v-if="panneau === `formulaire-${f.id}`" :formulaire="f" :role="role" :modeles="modeles"
                                       @fermer="panneau = null" @termine="termine" @modeles-modifies="rechargerModeles" />
            </li>
          </ul>
        </section>
      </template>
    </template>

    <!-- ═════════════ Documents sans formulaire ═════════════ -->
    <template v-else>
      <section class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
        <p class="m-0 px-5 py-4 text-sm text-slate-500 border-b border-slate-100">
          Ces documents n'ont pas de formulaire : leur modèle par défaut met en forme ceux créés à partir de sa publication.
        </p>
        <p v-if="!modelesParDefaut.length" class="m-0 px-5 py-4 text-sm text-slate-500">Aucun document sans formulaire.</p>
        <ul v-else class="m-0 p-0 list-none divide-y divide-slate-100">
          <li v-for="m in modelesParDefaut" :key="m.code" class="px-5 py-3 flex flex-col gap-3 text-sm">
            <div class="flex flex-col md:flex-row md:items-center gap-2">
              <span class="flex-1 font-bold text-slate-800">{{ m.nom }}</span>
              <span class="px-2 py-1 rounded-lg border text-xs font-semibold"
                    :class="m.versionParDefaut ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-red-50 text-red-700 border-red-200'">
                {{ m.versionParDefaut ? `Par défaut : M${m.versionParDefaut}` : 'Aucun modèle par défaut' }}
              </span>
              <span class="flex gap-2">
                <button v-if="m.versionParDefaut" type="button" @click="apercu(m.versions.find(v => v.numero === m.versionParDefaut)?.id)"
                        class="min-h-[36px] px-3 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold flex items-center gap-1 cursor-pointer">
                  <i class="pi pi-eye"></i> Aperçu
                </button>
                <button v-if="parDefaut?.codeModele !== m.code" type="button"
                        @click="parDefaut = { codeModele: m.code, fichier: null, motif: '', resultat: null }"
                        class="min-h-[36px] px-3 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold border-0 cursor-pointer flex items-center gap-1">
                  <i class="pi pi-upload"></i> Nouveau fichier
                </button>
              </span>
            </div>
            <div v-if="parDefaut?.codeModele === m.code" class="rounded-xl border border-blue-200 bg-blue-50/40 p-4 flex flex-col md:flex-row md:items-end gap-3">
              <ChoixFichierExcel v-model="parDefaut.fichier" @update:model-value="parDefaut.resultat = null" />
              <input v-model="parDefaut.motif" maxlength="500" placeholder="Motif (facultatif)" aria-label="Motif"
                     class="min-h-[44px] px-3 rounded-lg border border-slate-300 bg-white" />
              <button type="button" :disabled="!parDefaut.fichier || !!actionEnCours" @click="publierParDefaut"
                      class="min-h-[44px] px-4 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white font-bold border-0 cursor-pointer">
                Vérifier et publier par défaut
              </button>
              <button type="button" @click="parDefaut = null"
                      class="min-h-[44px] px-4 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 font-semibold cursor-pointer">Annuler</button>
              <ul v-if="parDefaut.resultat?.erreurs?.length" class="m-0 pl-5 text-red-800 md:basis-full"><li v-for="e in parDefaut.resultat.erreurs" :key="e">{{ e }}</li></ul>
            </div>
            <p v-if="m.affectations?.length" class="m-0 text-xs text-slate-500">
              Dernière publication : M{{ m.affectations[0].numeroVersion }} le {{ formaterDate(m.affectations[0].affecteLe) }}
              par {{ m.affectations[0].affecteParNom || m.affectations[0].affectePar || "l'application" }}
            </p>
          </li>
        </ul>
      </section>
    </template>
  </div>
</template>
