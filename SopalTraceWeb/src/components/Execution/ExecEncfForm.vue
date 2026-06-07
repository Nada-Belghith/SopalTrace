<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">

    <!-- ═══════════════════════════════════════════
         LOADING STATE
    ═══════════════════════════════════════════ -->
    <div v-if="store.loading" class="flex flex-col items-center justify-center py-24 gap-4">
      <i class="pi pi-spinner pi-spin text-4xl text-emerald-500"></i>
      <p class="text-slate-500 text-sm font-medium">Chargement de la fiche d'exécution...</p>
    </div>

    <!-- ═══════════════════════════════════════════
         MAIN CONTENT
    ═══════════════════════════════════════════ -->
    <template v-else-if="store.execData">

      <!-- ─── HEADER CARD ─── -->
      <section class="bg-white rounded-xl shadow-sm border border-slate-200 p-6">
        <div class="flex items-start justify-between gap-6">
          <div class="flex-1">
            <h2 class="text-sm font-black text-slate-400 uppercase tracking-widest mb-1 flex items-center gap-2">
              <i class="pi pi-cog text-emerald-500"></i>
              Fiche d'Exécution
            </h2>
            <h1 class="text-2xl font-black text-slate-800 mb-4">
              FE-RC-ENCF
              <span class="text-sm font-normal text-slate-500 ml-2">— Résultats du contrôle en cours de fabrication</span>
            </h1>

            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div class="bg-slate-50 rounded-lg border border-slate-100 p-3">
                <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Date</p>
                <p class="text-sm font-bold text-slate-800">{{ formattedDate }}</p>
              </div>
              <div class="bg-emerald-50 rounded-lg border border-emerald-100 p-3">
                <p class="text-[10px] font-black text-emerald-600 uppercase tracking-widest mb-1">OF</p>
                <p class="text-sm font-black text-emerald-800">{{ store.execData.numeroOf || '—' }}</p>
              </div>
              <div class="bg-slate-50 rounded-lg border border-slate-100 p-3">
                <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Article</p>
                <p class="text-sm font-bold text-slate-800">{{ store.execData.codeArticle || '—' }}</p>
              </div>
              <div class="bg-slate-50 rounded-lg border border-slate-100 p-3">
                <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Poste</p>
                <p class="text-sm font-bold text-slate-800">{{ store.execData.posteCode || '—' }}</p>
              </div>
            </div>
          </div>

          <button @click="saveExecution" :disabled="store.saving"
                  class="shrink-0 h-10 px-6 flex items-center gap-2 bg-slate-900 hover:bg-slate-700 text-white rounded-lg text-xs font-black uppercase tracking-wider transition-colors shadow-sm disabled:opacity-50">
            <i class="pi" :class="store.saving ? 'pi-spinner pi-spin' : 'pi-save'"></i>
            Enregistrer
          </button>
        </div>
      </section>

      <template v-if="store.planSourceData && store.planSourceData.sections">
        <template v-for="(sec, index) in store.planSourceData.sections" :key="index">
          
          <!-- ─── APPROBATION PIÈCES TYPES ─── -->
          <section v-if="sec.sectionType === 'APPROBATION'" class="bg-white rounded-xl shadow-sm border border-slate-200 mb-6">
            <div class="bg-slate-800 px-4 py-3 flex items-center justify-between">
              <div class="flex items-center gap-3">
                <span class="bg-emerald-500/20 text-emerald-300 text-[10px] font-black px-2 py-1 rounded border border-emerald-500/30 uppercase tracking-widest">
                  SEC {{ index + 1 }}
                </span>
                <h3 class="text-sm font-bold text-slate-100 uppercase tracking-wide">{{ sec.libelleAffiche || 'Approbation des pièces types' }}</h3>
              </div>
              <button @click="addPieceType"
                      class="text-emerald-400 hover:text-emerald-300 text-[11px] font-black uppercase tracking-widest flex items-center gap-1 transition-colors">
                <i class="pi pi-plus text-xs"></i> Ajouter ligne
              </button>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <thead class="bg-[#0f1923] text-white border-b border-slate-700 sticky top-0 z-10">
                  <tr class="text-[11px] font-black uppercase tracking-wider">
                    <th class="p-3 w-[20%] border-r border-slate-200">Heure</th>
                    <th class="p-3 w-[20%] text-center border-r border-slate-200">Résultat</th>
                    <th class="p-3 w-[30%] border-r border-slate-200">Signature (Matricule)</th>
                    <th class="p-3">Remarque</th>
                    <th class="p-3 w-10 text-center"></th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr v-for="(pt, idx) in piecesTypesDisplay" :key="idx"
                      class="hover:bg-slate-50/60 transition-colors group">
                    <td class="p-2 border-r border-slate-100">
                      <input type="text" v-model="pt.heureValidation"
                             placeholder="ex: 10:00"
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1.5 text-sm outline-none focus:bg-white transition-all" />
                    </td>
                    <td class="p-2 border-r border-slate-100 text-center">
                      <div class="flex items-center justify-center gap-2">
                        <button @click="pt.resultat = pt.resultat === 'C' ? null : 'C'"
                                :class="pt.resultat === 'C'
                                  ? 'bg-emerald-100 text-emerald-700 border-emerald-300 font-black'
                                  : 'bg-white text-slate-400 border-slate-200 hover:border-emerald-300 hover:text-emerald-600'"
                                class="w-10 h-8 rounded border text-xs font-bold transition-all">C</button>
                        <button @click="pt.resultat = pt.resultat === 'NC' ? null : 'NC'"
                                :class="pt.resultat === 'NC'
                                  ? 'bg-red-100 text-red-700 border-red-300 font-black'
                                  : 'bg-white text-slate-400 border-slate-200 hover:border-red-300 hover:text-red-500'"
                                class="w-10 h-8 rounded border text-xs font-bold transition-all">NC</button>
                      </div>
                    </td>
                    <td class="p-2 border-r border-slate-100">
                      <input type="text" v-model="pt.matriculeOperateur"
                             placeholder="Matricule..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1.5 text-sm outline-none focus:bg-white transition-all" />
                    </td>
                    <td class="p-2">
                      <input type="text" v-model="pt.remarque"
                             placeholder="Observation..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1.5 text-sm outline-none focus:bg-white transition-all" />
                    </td>
                    <td class="p-2 text-center">
                      <button v-if="pt._isReal" @click="removePieceType(idx)"
                              class="opacity-0 group-hover:opacity-100 text-slate-300 hover:text-red-500 transition-all p-1 rounded hover:bg-red-50">
                        <i class="pi pi-trash text-xs"></i>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="px-4 py-2 bg-slate-50 border-t border-slate-100">
              <p class="text-[10px] text-slate-400 italic">
                NB: L'approbation de la pièce type se fait : au démarrage série, après intervention maintenance et après arrêt pour réglage.
              </p>
            </div>
          </section>

          <!-- ─── REGLAGE (Caractéristiques à contrôler aux réglages) ─── -->
          <section v-if="sec.sectionType === 'REGLAGE' || sec.sectionType === 'CUSTOM' || sec.sectionType === 'LOT_POSTE'" class="bg-white rounded-xl shadow-sm border border-slate-200 mb-6">
            <div class="bg-slate-800 px-4 py-3 flex items-center gap-3">
              <span class="bg-emerald-500/20 text-emerald-300 text-[10px] font-black px-2 py-1 rounded border border-emerald-500/30 uppercase tracking-widest">
                SEC {{ index + 1 }}
              </span>
              <h3 class="text-sm font-bold text-slate-100 uppercase tracking-wide">{{ sec.libelleAffiche || 'Caractéristiques à contrôler' }}</h3>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <thead class="bg-[#0f1923] text-white border-b border-slate-700 sticky top-0 z-10">
                  <tr class="text-[11px] font-black uppercase tracking-wider">
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-200 align-middle">Caractéristiques contrôlées</th>
                    <th colspan="2" class="p-2 text-center border-r border-b border-slate-200 w-[10%]">Résultat</th>
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-200 align-middle">Non-conformité</th>
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-200 align-middle">Actions de correction</th>
                    <th rowspan="2" class="p-3 w-[15%] align-middle text-center">Approbation Matricule</th>
                  </tr>
                  <tr class="text-[11px] font-black uppercase tracking-wider border-b border-slate-200">
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-emerald-50 text-emerald-700">C</th>
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-red-50 text-red-600">NC</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr v-for="(ligne, lIdx) in sec.lignes" :key="lIdx"
                      :class="[
                        'transition-colors group hover:bg-slate-50/50'
                      ]">
                    <!-- Caractéristiques contrôlées -->
                    <td class="p-3 border-r border-slate-100">
                      <span class="text-xs font-semibold text-slate-800">{{ ligne.caracteristique }}</span>
                    </td>

                    <!-- Case C -->
                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setReglageResultat(ligne.id, 'C')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             getReglageResultat(ligne.id) === 'C'
                               ? 'bg-emerald-500 border-emerald-500 shadow-sm'
                               : 'border-slate-200 hover:border-emerald-400 bg-white'
                           ]">
                        <i v-if="getReglageResultat(ligne.id) === 'C'" class="pi pi-check text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <!-- Case NC -->
                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setReglageResultat(ligne.id, 'NC')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             getReglageResultat(ligne.id) === 'NC'
                               ? 'bg-red-500 border-red-500 shadow-sm'
                               : 'border-slate-200 hover:border-red-400 bg-white'
                           ]">
                        <i v-if="getReglageResultat(ligne.id) === 'NC'" class="pi pi-times text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <!-- Non-conformité -->
                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="getReglageData(ligne.id).nonConformite"
                                rows="1"
                                placeholder="Détails NC..."
                                class="w-full bg-transparent border border-transparent focus:border-red-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"
                                :disabled="getReglageResultat(ligne.id) !== 'NC'"></textarea>
                    </td>

                    <!-- Actions de correction -->
                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="getReglageData(ligne.id).actionsCorrection"
                                rows="1"
                                placeholder="Actions..."
                                class="w-full bg-transparent border border-transparent focus:border-emerald-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"></textarea>
                    </td>

                    <!-- Approbation Matricule -->
                    <td class="p-1.5">
                      <input type="text" v-model="getReglageData(ligne.id).approbationMatricule"
                             placeholder="Matricule..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1 text-xs text-center font-bold text-slate-700 outline-none focus:bg-white transition-all" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- ─── CONTRÔLE EN COURS DE FABRICATION (Tranches) ─── -->
          <section v-if="sec.sectionType === 'TRANCHES'" class="bg-white rounded-xl shadow-sm border border-slate-200 mb-6">
            <div class="bg-slate-800 px-4 py-3 flex items-center gap-3">
              <span class="bg-emerald-500/20 text-emerald-300 text-[10px] font-black px-2 py-1 rounded border border-emerald-500/30 uppercase tracking-widest">
                SEC {{ index + 1 }}
              </span>
              <h3 class="text-sm font-bold text-slate-100 uppercase tracking-wide">{{ sec.libelleAffiche || 'Contrôle en cours de fabrication' }}</h3>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <thead class="bg-[#0f1923] text-white border-b border-slate-700 sticky top-0 z-10">
                  <tr class="text-[11px] font-black uppercase tracking-wider">
                    <th rowspan="2" class="p-3 w-[12%] border-r border-slate-200 align-middle text-center">Fréquence</th>
                    <th colspan="2" class="p-2 text-center border-r border-b border-slate-200 w-[10%]">Résultats</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-200 align-middle">Non-conformité</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-200 align-middle">Actions de correction</th>
                    <th rowspan="2" class="p-3 w-[14%] align-middle text-center">Approbation</th>
                  </tr>
                  <tr class="text-[11px] font-black uppercase tracking-wider border-b border-slate-200">
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-emerald-50 text-emerald-700">C</th>
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-red-50 text-red-600">NC</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr v-for="(tranche, idx) in store.execData.tranches" :key="idx"
                      :class="[
                        'transition-colors group',
                        tranche.resultatFinal === 'NC' ? 'bg-red-50/40' : 'hover:bg-slate-50/50'
                      ]">
                    <td class="p-2 border-r border-slate-100 text-center">
                      <span class="text-xs font-bold text-slate-600">{{ tranche.trancheHoraire }}</span>
                    </td>

                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setResultat(tranche, 'C')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             tranche.resultatFinal === 'C'
                               ? 'bg-emerald-500 border-emerald-500 shadow-sm'
                               : 'border-slate-200 hover:border-emerald-400 bg-white'
                           ]">
                        <i v-if="tranche.resultatFinal === 'C'" class="pi pi-check text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setResultat(tranche, 'NC')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             tranche.resultatFinal === 'NC'
                               ? 'bg-red-500 border-red-500 shadow-sm'
                               : 'border-slate-200 hover:border-red-400 bg-white'
                           ]">
                        <i v-if="tranche.resultatFinal === 'NC'" class="pi pi-times text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="tranche.detailsNC"
                                rows="1"
                                placeholder="Détails NC..."
                                class="w-full bg-transparent border border-transparent focus:border-red-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"
                                :disabled="tranche.resultatFinal !== 'NC'"></textarea>
                    </td>

                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="tranche.actionsCorrection"
                                rows="1"
                                placeholder="Actions..."
                                class="w-full bg-transparent border border-transparent focus:border-emerald-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"></textarea>
                    </td>

                    <td class="p-1.5">
                      <input type="text" v-model="tranche.matriculeApprobateur"
                             placeholder="Matricule..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1 text-xs text-center font-bold text-slate-700 outline-none focus:bg-white transition-all" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

        </template>
      </template>
      
      <div v-else class="text-center text-slate-500 py-12">
        <i class="pi pi-exclamation-triangle text-4xl mb-4 text-orange-400"></i>
        <p>Les données du plan source (modèle) n'ont pas pu être chargées.</p>
      </div>

    </template>

    <!-- ═══════════════════════════════════════════
         ERROR STATE
    ═══════════════════════════════════════════ -->
    <div v-else-if="store.error"
         class="p-8 text-center bg-red-50 rounded-xl border border-red-200 shadow-sm">
      <i class="pi pi-exclamation-triangle text-red-400 text-4xl mb-4 block"></i>
      <p class="text-red-700 font-bold">{{ store.error }}</p>
    </div>

  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useExecEncfStore } from '@/stores/execution/execEncfStore';
import { useToast } from 'primevue/usetoast';

const route  = useRoute();
const store  = useExecEncfStore();
const toast  = useToast();

// ─── Computed ────────────────────────────────────────────────────────────────
const formattedDate = computed(() => {
  if (!store.execData?.dateDebut) return new Date().toLocaleDateString('fr-FR');
  return new Date(store.execData.dateDebut).toLocaleDateString('fr-FR', {
    day: '2-digit', month: '2-digit', year: 'numeric'
  });
});

// Affiche toujours au moins 3 lignes vides (comme le papier)
const piecesTypesDisplay = computed(() => {
  if (!store.execData) return [];
  const real = store.execData.piecesTypes.map(p => ({ ...p, _isReal: true }));
  while (real.length < 3) {
    real.push({ heureValidation: '', resultat: null, matriculeOperateur: '', remarque: '', _isReal: false });
  }
  return real;
});

// ─── Lifecycle ───────────────────────────────────────────────────────────────
onMounted(async () => {
  const id = route.params.id;
  const { of: of_, poste } = route.query;

  if (id && id !== 'new') {
    await store.loadExecById(id);
  } else if (of_ && poste) {
    await store.loadOrCreateExecByOf(of_, poste);
  } else {
    store.error = "Paramètres manquants : fournir un ID ou bien ?of=XX&poste=YY";
  }
});

// ─── Actions ─────────────────────────────────────────────────────────────────
const addPieceType = () => {
  if (!store.execData) return;
  store.execData.piecesTypes.push({
    heureValidation: '',
    resultat: null,
    matriculeOperateur: '',
    remarque: ''
  });
};

const removePieceType = (idx) => {
  const realItems = store.execData.piecesTypes;
  // idx is the index in the display array; filter only real items count
  const realIdx = store.execData.piecesTypes.length - (piecesTypesDisplay.value.length - idx);
  if (realIdx >= 0) realItems.splice(realIdx, 1);
};

const setResultat = (tranche, val) => {
  tranche.resultatFinal = tranche.resultatFinal === val ? null : val;
  if (tranche.resultatFinal !== 'NC') {
    tranche.detailsNC       = '';
    tranche.actionsCorrection = '';
  }
};

const getReglageData = (ligneId) => {
  if (!store.execData) return {};
  if (!store.execData.reglages) store.execData.reglages = {};
  if (!store.execData.reglages[ligneId]) {
    store.execData.reglages[ligneId] = {
      resultat: null,
      nonConformite: '',
      actionsCorrection: '',
      approbationMatricule: ''
    };
  }
  return store.execData.reglages[ligneId];
};

const getReglageResultat = (ligneId) => {
  return getReglageData(ligneId).resultat;
};

const setReglageResultat = (ligneId, val) => {
  const data = getReglageData(ligneId);
  data.resultat = data.resultat === val ? null : val;
  if (data.resultat !== 'NC') {
    data.nonConformite = '';
    data.actionsCorrection = '';
  }
};

const saveExecution = async () => {
  // Sync only rows that have data before saving
  store.execData.piecesTypes = piecesTypesDisplay.value
    .filter(pt => pt._isReal || pt.heureValidation || pt.resultat || pt.matriculeOperateur)
    .map(({ _isReal, ...rest }) => rest);

  const result = await store.saveExec();
  if (result.success) {
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Fiche ENCF enregistrée.', life: 3000 });
  } else {
    toast.add({ severity: 'error', summary: 'Erreur', detail: result.message, life: 4000 });
  }
};
</script>
