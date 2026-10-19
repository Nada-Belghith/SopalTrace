<template>
  <div class="min-h-screen bg-slate-50 text-slate-800 p-3 md:p-6 lg:p-8 font-sans">
    <div class="w-full space-y-6 md:space-y-8 animate-in fade-in duration-500">

      <!-- En-tête -->
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between bg-white border border-slate-200 p-4 md:p-6 rounded-2xl md:rounded-3xl shadow-sm gap-4">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 md:w-14 md:h-14 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-2xl flex items-center justify-center shadow-md shadow-blue-500/10 rotate-3 shrink-0">
            <i class="pi pi-box text-xl md:text-2xl text-white"></i>
          </div>
          <div>
            <h1 class="text-xl md:text-2xl font-black text-slate-900 tracking-tight">Magasinier <span class="text-blue-600">Préparation des OF</span></h1>
            <p class="text-slate-500 text-[10px] md:text-xs font-bold uppercase tracking-widest flex items-center gap-2 mt-0.5">
              <span class="w-2 h-2 bg-green-500 rounded-full animate-pulse"></span>
              Session : {{ authStore.userName }}
            </p>
          </div>
        </div>
        <div class="flex items-center bg-slate-100 border border-slate-200 p-1 rounded-xl text-[10px] md:text-xs font-bold text-slate-600">
          <span class="px-2.5 py-1 rounded-lg bg-white shadow-sm flex items-center gap-1" :class="enCours ? 'text-orange-500' : 'text-blue-600'">
            <i class="pi" :class="enCours ? 'pi-spin pi-spinner' : 'pi-qrcode'"></i>
            {{ enCours ? 'Enregistrement…' : 'Prêt à scanner' }}
          </span>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 md:gap-6">
        <!-- Étape 1 : OF -->
        <div class="bg-white border border-slate-200 p-5 md:p-8 rounded-2xl md:rounded-3xl shadow-sm relative overflow-hidden group">
          <div class="relative z-10 space-y-4">
            <div class="flex items-center justify-between">
              <label class="block text-[10px] md:text-[11px] font-black text-blue-600 uppercase tracking-widest">Étape 1 : Scanner l'OF</label>
              <span class="px-2 py-0.5 bg-blue-50 text-blue-600 text-[9px] md:text-[10px] font-bold rounded-md">Douchette ou caméra</span>
            </div>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-400 group-focus-within:text-blue-500">
                <i class="pi pi-barcode text-xl md:text-2xl"></i>
              </span>
              <input :ref="el => (scanOf.champ.value = el)" v-model="scanOf.valeur.value" @keydown="scanOf.surTouche"
                     type="text" autocomplete="off" spellcheck="false" placeholder="Scanner l'OF…"
                     class="w-full bg-slate-50 border border-slate-300 text-slate-900 pl-12 pr-16 py-3.5 md:py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:bg-white focus:ring-4 focus:ring-blue-500/10 transition-all font-black text-base md:text-lg placeholder:text-slate-400" />
              <button @click="ouvrirCamera('OF')" title="Scanner avec la caméra"
                      class="absolute right-3 top-1/2 -translate-y-1/2 p-2 bg-blue-50 hover:bg-blue-100 text-blue-600 rounded-xl border border-blue-100">
                <i class="pi pi-camera text-base"></i>
              </button>
            </div>
            <p class="text-slate-500 text-[10px] md:text-[11px] font-bold italic flex items-center gap-1">
              <i class="pi pi-info-circle text-xs text-blue-500"></i>
              Scanner l'OF ouvre sa préparation, ou la reprend là où elle s'est arrêtée.
            </p>
          </div>
        </div>

        <!-- Étape 2 : composants -->
        <div class="bg-white border p-5 md:p-8 rounded-2xl md:rounded-3xl shadow-sm relative overflow-hidden group transition-all"
             :class="peutScanner ? 'border-slate-200' : 'border-slate-100 opacity-40 pointer-events-none'">
          <div class="relative z-10 space-y-4">
            <div class="flex items-center justify-between">
              <label class="block text-[10px] md:text-[11px] font-black text-indigo-600 uppercase tracking-widest">Étape 2 : Scanner les étiquettes des composants</label>
              <span v-if="scanComposant.origine.value" class="px-2 py-0.5 bg-indigo-50 text-indigo-600 text-[9px] md:text-[10px] font-bold rounded-md">
                {{ scanComposant.origine.value === 'DOUCHETTE' ? 'Lu à la douchette' : 'Saisi au clavier' }}
              </span>
            </div>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-400 group-focus-within:text-indigo-500">
                <i class="pi pi-qrcode text-xl md:text-2xl"></i>
              </span>
              <input :ref="el => (scanComposant.champ.value = el)" v-model="scanComposant.valeur.value" @keydown="scanComposant.surTouche"
                     type="text" autocomplete="off" spellcheck="false" :disabled="!peutScanner" placeholder="Scanner l'étiquette du lot…"
                     class="w-full bg-slate-50 border border-slate-300 text-slate-900 pl-12 pr-16 py-3.5 md:py-4 rounded-2xl focus:outline-none focus:border-indigo-500 focus:bg-white focus:ring-4 focus:ring-indigo-500/10 transition-all font-black text-base md:text-lg placeholder:text-slate-400" />
              <button @click="ouvrirCamera('COMPOSANT')" :disabled="!peutScanner" title="Scanner avec la caméra"
                      class="absolute right-3 top-1/2 -translate-y-1/2 p-2 bg-indigo-50 hover:bg-indigo-100 text-indigo-600 rounded-xl border border-indigo-100">
                <i class="pi pi-camera text-base"></i>
              </button>
            </div>
            <p class="text-slate-500 text-[10px] md:text-[11px] font-bold italic flex items-center gap-1">
              <i class="pi pi-info-circle text-xs text-indigo-500"></i>
              Formats : cd:ARTICLE/lot:LOT/rpt:RAPPORT/qty:QTE ou ARTICLE;LOT;RAPPORT;QTE
            </p>
          </div>
        </div>
      </div>

      <!-- Dernier scan -->
      <div v-if="dernierScan" class="flex items-center gap-3 px-5 py-3 rounded-2xl border font-bold text-sm"
           :class="dernierScan.ok ? 'bg-emerald-50 border-emerald-200 text-emerald-700' : 'bg-red-50 border-red-200 text-red-700'">
        <i class="pi text-lg" :class="dernierScan.ok ? 'pi-check-circle' : 'pi-times-circle'"></i>
        <span>{{ dernierScan.message }}</span>
      </div>

      <!-- Préparation -->
      <div v-if="preparation" class="space-y-6">
        <div class="bg-white border border-slate-200 rounded-2xl md:rounded-3xl shadow-sm overflow-hidden">
          <div class="bg-slate-50 p-4 md:p-6 border-b border-slate-200 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div class="flex flex-col sm:flex-row sm:items-center gap-4">
              <div class="px-4 py-2 bg-blue-50 border border-blue-200 rounded-2xl text-blue-700 font-extrabold text-base md:text-lg tracking-wider self-start">
                OF : {{ preparation.numeroOf }}
              </div>
              <div>
                <h2 class="text-slate-900 font-extrabold text-base md:text-lg leading-tight">{{ preparation.designationArticle }}</h2>
                <div class="flex items-center gap-2 mt-1 flex-wrap text-xs font-bold">
                  <span class="text-slate-500 tracking-wider">{{ preparation.codeArticle }}</span>
                  <span v-if="preparation.natureArticle" class="px-2 py-0.5 text-[10px] uppercase rounded-md bg-blue-50 text-blue-700 border border-blue-200">{{ preparation.natureArticle }}</span>
                  <span class="px-2 py-0.5 text-[10px] uppercase rounded-md border" :class="statut.classes">{{ statut.libelle }}</span>
                </div>
              </div>
            </div>

            <div class="flex items-center justify-between lg:justify-end gap-4 border-t border-slate-200 lg:border-none pt-4 lg:pt-0">
              <div class="text-left lg:text-right">
                <p class="text-slate-400 text-[10px] font-black uppercase">Composants servis</p>
                <p class="text-slate-900 font-black text-xl md:text-2xl mt-0.5">{{ preparation.composantsServis }} / {{ preparation.composants.length }}</p>
              </div>
              <template v-if="estEnCours">
                <button @click="annuler" :disabled="enCours"
                        class="px-4 py-3 bg-white border border-slate-300 hover:bg-red-50 hover:text-red-600 text-slate-600 rounded-2xl font-black uppercase text-[10px] md:text-xs tracking-wider">
                  Annuler
                </button>
                <button @click="terminer" :disabled="enCours"
                        class="px-4 md:px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-slate-200 text-white disabled:text-slate-400 rounded-2xl font-black uppercase text-[10px] md:text-xs tracking-wider shadow-md flex items-center gap-2">
                  <i class="pi pi-check"></i> Terminer la préparation
                </button>
              </template>
            </div>
          </div>

          <div v-if="preparation.motifIncomplet" class="px-6 py-3 bg-orange-50 border-b border-orange-100 text-sm font-semibold text-orange-700">
            Préparation incomplète : {{ preparation.motifIncomplet }}
          </div>

          <div class="divide-y divide-slate-100">
            <div v-for="comp in preparation.composants" :key="comp.codeArticle" class="p-4 md:px-6 flex flex-col md:flex-row md:items-start gap-4"
                 :class="comp.codeArticle === articleMisEnAvant ? 'bg-emerald-50/60' : ''">
              <div class="md:w-1/3 flex items-start gap-3">
                <i class="pi text-lg mt-0.5" :title="comp.servi ? 'Quantité requise servie' : comp.lots.length ? 'Quantité servie inférieure à la quantité requise' : ''"
                   :class="comp.servi ? 'pi-check-circle text-emerald-500' : comp.lots.length ? 'pi-exclamation-circle text-orange-500' : 'pi-circle text-slate-300'"></i>
                <div>
                  <p class="text-slate-900 font-extrabold text-sm">{{ comp.designation }}</p>
                  <p class="text-xs font-bold text-slate-500">{{ comp.codeArticle }} · {{ comp.nature || 'Composant' }}</p>
                </div>
              </div>
              <div class="md:w-40 text-sm">
                <p class="text-[10px] font-black text-slate-400 uppercase">Requis / servi</p>
                <p class="font-black" :class="comp.lots.length && !comp.servi ? 'text-orange-600' : 'text-slate-900'">{{ comp.quantiteRequise }} / {{ comp.quantiteServie ?? '—' }}</p>
              </div>
              <div class="flex-1 flex flex-wrap gap-2">
                <span v-if="comp.lots.length === 0" class="text-xs font-semibold text-slate-400 italic">En attente de scan</span>
                <span v-for="lot in comp.lots" :key="lot.id"
                      class="inline-flex items-center gap-2 px-3 py-1.5 bg-slate-100 border border-slate-200 rounded-lg text-xs font-bold text-slate-700">
                  Lot {{ lot.numeroLot }}
                  <span v-if="lot.quantite" class="text-slate-500">× {{ lot.quantite }}</span>
                  <span v-if="lot.rapportControle" class="text-indigo-600">QC {{ lot.rapportControle }}</span>
                  <button v-if="estEnCours" @click="retirer(lot)" title="Retirer ce scan" class="text-slate-400 hover:text-red-500">
                    <i class="pi pi-times text-[10px]"></i>
                  </button>
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="flex flex-col items-center justify-center py-24 bg-white border border-dashed border-slate-200 rounded-3xl shadow-sm text-center">
        <div class="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mb-5 text-slate-400">
          <i class="pi pi-search text-3xl"></i>
        </div>
        <h3 class="text-lg font-extrabold text-slate-700 uppercase tracking-widest">En attente de scan…</h3>
        <p class="text-slate-500 text-sm font-semibold mt-1">Scannez le code de l'OF sur le bon de sortie</p>
      </div>
    </div>

    <CameraScanner :visible="cameraVisible" :target="cibleCamera" @scanned="surScanCamera" @close="cameraVisible = false" />
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { confirmer } from '@/composables/useConfirmation';
import { useAuthStore } from '@/stores/authStore';
import magasinService from '@/services/magasinService';
import { useScanDouchette } from '@/composables/Magasinier/useScanDouchette';
import CameraScanner from '@/components/Magasinier/CameraScanner.vue';

const authStore = useAuthStore();

const STATUTS = {
  EN_COURS: { libelle: 'Préparation en cours', classes: 'bg-orange-50 text-orange-600 border-orange-200' },
  TERMINE: { libelle: 'Préparation terminée', classes: 'bg-emerald-50 text-emerald-700 border-emerald-200' }
};

const preparation = ref(null);
const dernierScan = ref(null);
const articleMisEnAvant = ref(null);
const cameraVisible = ref(false);
const cibleCamera = ref('');

const estEnCours = computed(() => preparation.value?.statut === 'EN_COURS');
const peutScanner = computed(() => estEnCours.value);
const statut = computed(() => STATUTS[preparation.value?.statut] || STATUTS.EN_COURS);

const afficher = (ok, message) => { dernierScan.value = { ok, message }; };

/** Erreur d'un scan : message affiché, puis relancée pour le signal sonore d'échec. */
const echec = (e) => {
  afficher(false, e.message || 'Scan refusé.');
  throw e;
};

const scanOf = useScanDouchette({
  estActif: () => !peutScanner.value,
  onScan: async (contenu) => {
    try {
      preparation.value = await magasinService.ouvrirPreparation(contenu);
      articleMisEnAvant.value = null;
      afficher(true, preparation.value.statut === 'TERMINE'
        ? `OF ${preparation.value.numeroOf} : préparation déjà terminée (consultation).`
        : `OF ${preparation.value.numeroOf} prêt : scannez les composants.`);
      if (estEnCours.value) scanComposant.focus();
    } catch (e) { echec(e); }
  }
});

const scanComposant = useScanDouchette({
  estActif: () => peutScanner.value,
  onScan: async (contenu) => {
    try {
      const resultat = await magasinService.scannerComposant(preparation.value.id, contenu);
      preparation.value = resultat.preparation;
      articleMisEnAvant.value = resultat.codeArticle;
      afficher(true, `Lot ${resultat.lot.numeroLot} enregistré pour ${resultat.codeArticle}.`);
    } catch (e) { echec(e); }
  }
});

const enCours = computed(() => scanOf.enCours.value || scanComposant.enCours.value);

const retirer = async (lot) => {
  try {
    preparation.value = await magasinService.retirerLot(preparation.value.id, lot.id);
    afficher(true, `Scan du lot ${lot.numeroLot} retiré.`);
  } catch (e) {
    afficher(false, e.message);
  } finally {
    scanComposant.focus();
  }
};

const terminer = async () => {
  const manquants = preparation.value.composants.filter(c => !c.servi);
  let motif = null;
  if (manquants.length > 0) {
    const reponse = await confirmer({
      titre: 'Préparation incomplète',
      message: 'La quantité requise de ces composants n\'est pas servie. Indiquez le motif pour terminer quand même.',
      details: manquants.map(c => `${c.codeArticle} (${c.quantiteServie ?? '—'} / ${c.quantiteRequise})`),
      saisie: { libelle: 'Le motif', obligatoire: true, longueurMax: 300 },
      libelleConfirmer: 'Terminer',
      libelleAnnuler: 'Continuer à scanner'
    });
    if (!reponse.confirme) return scanComposant.focus();
    motif = reponse.valeur;
  }
  try {
    preparation.value = await magasinService.terminerPreparation(preparation.value.id, motif);
    afficher(true, `Préparation de l'OF ${preparation.value.numeroOf} terminée : l'OF est disponible pour la production.`);
    scanOf.focus();
  } catch (e) {
    afficher(false, e.message);
  }
};

const annuler = async () => {
  const { confirme } = await confirmer({
    titre: 'Annuler la préparation ?',
    message: 'Les scans enregistrés sont conservés dans l\'historique, une nouvelle préparation pourra être ouverte.',
    danger: true,
    libelleConfirmer: 'Annuler la préparation',
    libelleAnnuler: 'Retour'
  });
  if (!confirme) return;
  try {
    await magasinService.annulerPreparation(preparation.value.id);
    afficher(true, `Préparation de l'OF ${preparation.value.numeroOf} annulée.`);
    preparation.value = null;
    scanOf.focus();
  } catch (e) {
    afficher(false, e.message);
  }
};

const ouvrirCamera = (cible) => {
  cibleCamera.value = cible === 'OF' ? 'OF' : 'QR';
  cameraVisible.value = true;
};

const surScanCamera = (texte) => {
  cameraVisible.value = false;
  (cibleCamera.value === 'OF' ? scanOf : scanComposant).soumettre(texte);
};

onMounted(() => scanOf.focus());
</script>
