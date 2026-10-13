<template>
  <div class="min-h-screen bg-slate-50 text-slate-800 p-3 md:p-6 lg:p-8 font-sans">
    <div class="w-full space-y-6 md:space-y-8 animate-in fade-in duration-500">

      <!-- En-tête -->
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between bg-white border border-slate-200 p-4 md:p-6 rounded-2xl md:rounded-3xl shadow-sm gap-4">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 md:w-14 md:h-14 bg-gradient-to-tr from-indigo-600 to-purple-500 rounded-2xl flex items-center justify-center shadow-md shadow-indigo-500/10 rotate-3 shrink-0">
            <i class="pi pi-truck text-xl md:text-2xl text-white"></i>
          </div>
          <div>
            <h1 class="text-xl md:text-2xl font-black text-slate-900 tracking-tight">Expédition <span class="text-indigo-600">Produits Finis</span></h1>
            <p class="text-slate-500 text-[10px] md:text-xs font-bold uppercase tracking-widest flex items-center gap-2 mt-0.5">
              <span class="w-2 h-2 bg-green-500 rounded-full animate-pulse"></span>
              Session : {{ authStore.userName }}
            </p>
          </div>
        </div>
        <div class="flex items-center bg-slate-100 border border-slate-200 p-1 rounded-xl text-[10px] md:text-xs font-bold text-slate-600">
          <span class="px-2.5 py-1 rounded-lg bg-white shadow-sm flex items-center gap-1" :class="enCours ? 'text-orange-500' : 'text-indigo-600'">
            <i class="pi" :class="enCours ? 'pi-spin pi-spinner' : 'pi-bolt'"></i>
            {{ enCours ? 'Enregistrement…' : 'Prêt à scanner' }}
          </span>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 md:gap-6">
        <!-- Étape 1 : BL -->
        <div class="bg-white border border-slate-200 p-5 md:p-8 rounded-2xl md:rounded-3xl shadow-sm relative overflow-hidden group">
          <div class="relative z-10 space-y-4">
            <label class="block text-[10px] md:text-[11px] font-black text-indigo-600 uppercase tracking-widest">Étape 1 : Scanner le BL</label>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-400 group-focus-within:text-indigo-500">
                <i class="pi pi-file text-xl md:text-2xl"></i>
              </span>
              <input :ref="el => (scanBl.champ.value = el)" v-model="scanBl.valeur.value" @keydown="scanBl.surTouche"
                     type="text" autocomplete="off" spellcheck="false" placeholder="Scanner N° BL…"
                     class="w-full bg-slate-50 border border-slate-300 text-slate-900 pl-12 pr-16 py-3.5 md:py-4 rounded-2xl focus:outline-none focus:border-indigo-500 focus:bg-white focus:ring-4 focus:ring-indigo-500/10 transition-all font-black text-base md:text-lg placeholder:text-slate-400" />
              <button @click="ouvrirCamera('BL')" title="Scanner avec la caméra"
                      class="absolute right-3 top-1/2 -translate-y-1/2 p-2 bg-indigo-50 hover:bg-indigo-100 text-indigo-600 rounded-xl border border-indigo-100">
                <i class="pi pi-camera text-base"></i>
              </button>
            </div>
            <p class="text-slate-500 text-[10px] md:text-[11px] font-bold italic flex items-center gap-1">
              <i class="pi pi-info-circle text-xs text-indigo-500"></i>
              Scanner le BL ouvre son expédition, ou la reprend.
            </p>
          </div>
        </div>

        <!-- Étape 2 : OF -->
        <div class="bg-white border p-5 md:p-8 rounded-2xl md:rounded-3xl shadow-sm relative overflow-hidden group transition-all"
             :class="peutScanner ? 'border-slate-200' : 'border-slate-100 opacity-40 pointer-events-none'">
          <div class="relative z-10 space-y-4">
            <label class="block text-[10px] md:text-[11px] font-black text-purple-600 uppercase tracking-widest">Étape 2 : Scanner les OF terminés</label>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-400 group-focus-within:text-purple-500">
                <i class="pi pi-barcode text-xl md:text-2xl"></i>
              </span>
              <input :ref="el => (scanOf.champ.value = el)" v-model="scanOf.valeur.value" @keydown="scanOf.surTouche"
                     type="text" autocomplete="off" spellcheck="false" :disabled="!peutScanner" placeholder="Scanner N° OF…"
                     class="w-full bg-slate-50 border border-slate-300 text-slate-900 pl-12 pr-16 py-3.5 md:py-4 rounded-2xl focus:outline-none focus:border-purple-500 focus:bg-white focus:ring-4 focus:ring-purple-500/10 transition-all font-black text-base md:text-lg placeholder:text-slate-400" />
              <button @click="ouvrirCamera('OF')" :disabled="!peutScanner" title="Scanner avec la caméra"
                      class="absolute right-3 top-1/2 -translate-y-1/2 p-2 bg-purple-50 hover:bg-purple-100 text-purple-600 rounded-xl border border-purple-100">
                <i class="pi pi-camera text-base"></i>
              </button>
            </div>
            <p class="text-slate-500 text-[10px] md:text-[11px] font-bold italic flex items-center gap-1">
              <i class="pi pi-info-circle text-xs text-purple-500"></i>
              Seuls les OF terminés en production sont acceptés ; un OF peut être livré sur plusieurs BL.
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

      <!-- Expédition -->
      <div v-if="expedition" class="bg-white border border-slate-200 rounded-2xl md:rounded-3xl shadow-sm overflow-hidden">
        <div class="bg-slate-50 p-4 md:p-6 border-b border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div class="flex items-center gap-4 flex-wrap">
            <div class="px-4 py-2 bg-indigo-50 border border-indigo-200 rounded-2xl text-indigo-700 font-extrabold text-base tracking-wider">
              BL : {{ expedition.numeroBl }}
            </div>
            <div class="text-xs font-bold text-slate-500">
              Client {{ expedition.codeClient }} · expédition prévue le {{ dateFr(expedition.dateExpedition) }}
            </div>
            <span class="px-2 py-0.5 text-[10px] uppercase rounded-md border font-bold"
                  :class="estEnCours ? 'bg-orange-50 text-orange-600 border-orange-200' : 'bg-emerald-50 text-emerald-700 border-emerald-200'">
              {{ estEnCours ? 'Expédition en cours' : 'Expédition validée' }}
            </span>
          </div>
          <button v-if="estEnCours" @click="terminer" :disabled="enCours || expedition.ofs.length === 0"
                  class="px-6 py-3.5 bg-indigo-600 hover:bg-indigo-700 disabled:bg-slate-200 text-white disabled:text-slate-400 rounded-2xl font-black uppercase text-xs tracking-wider shadow-md flex items-center justify-center gap-2">
            <i class="pi pi-check"></i> Valider l'expédition ({{ expedition.ofs.length }} OF)
          </button>
        </div>

        <div v-if="expedition.ofs.length === 0" class="p-10 text-center text-slate-500 text-sm font-semibold">
          Scannez les OF à expédier sur ce BL.
        </div>
        <table v-else class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-100/50 text-[10px] font-black text-slate-500 uppercase tracking-widest">
              <th class="px-6 py-4 border-b border-slate-200">OF</th>
              <th class="px-6 py-4 border-b border-slate-200">Article</th>
              <th class="px-6 py-4 border-b border-slate-200 text-right">Quantité</th>
              <th class="px-6 py-4 border-b border-slate-200">Scanné le</th>
              <th v-if="estEnCours" class="px-6 py-4 border-b border-slate-200 text-center">Action</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-for="of in expedition.ofs" :key="of.id" :class="of.numeroOf === ofMisEnAvant ? 'bg-emerald-50/60' : 'hover:bg-slate-50/50'">
              <td class="px-6 py-4"><span class="px-3 py-1.5 bg-slate-100 border border-slate-200 rounded-lg text-slate-800 font-extrabold text-sm">{{ of.numeroOf }}</span></td>
              <td class="px-6 py-4 text-sm"><p class="font-bold text-slate-800">{{ of.designation }}</p><p class="text-xs text-slate-500">{{ of.codeArticle }}</p></td>
              <td class="px-6 py-4 text-right font-black text-slate-900">{{ of.quantite }}</td>
              <td class="px-6 py-4 text-xs font-bold text-slate-500">{{ heureFr(of.dateScan) }}</td>
              <td v-if="estEnCours" class="px-6 py-4 text-center">
                <button @click="retirer(of)" title="Retirer cet OF" class="p-2 text-slate-400 hover:text-red-500 rounded-xl hover:bg-red-50">
                  <i class="pi pi-trash text-sm"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-else class="flex flex-col items-center justify-center py-24 bg-white border border-dashed border-slate-200 rounded-3xl shadow-sm text-center">
        <div class="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mb-5 text-slate-400">
          <i class="pi pi-search text-3xl"></i>
        </div>
        <h3 class="text-lg font-extrabold text-slate-700 uppercase tracking-widest">En attente de scan…</h3>
        <p class="text-slate-500 text-sm font-semibold mt-1">Scannez le code d'un BL pour commencer l'expédition</p>
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

const expedition = ref(null);
const dernierScan = ref(null);
const ofMisEnAvant = ref(null);
const cameraVisible = ref(false);
const cibleCamera = ref('');

const estEnCours = computed(() => expedition.value?.statut === 'EN_COURS');
const peutScanner = computed(() => estEnCours.value);

const dateFr = (d) => d ? new Date(d).toLocaleDateString('fr-FR') : '—';
const heureFr = (d) => d ? new Date(d).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' }) : '—';
const afficher = (ok, message) => { dernierScan.value = { ok, message }; };
const echec = (e) => { afficher(false, e.message || 'Scan refusé.'); throw e; };

const scanBl = useScanDouchette({
  estActif: () => !peutScanner.value,
  onScan: async (contenu) => {
    try {
      expedition.value = await magasinService.ouvrirExpedition(contenu);
      ofMisEnAvant.value = null;
      afficher(true, estEnCours.value
        ? `BL ${expedition.value.numeroBl} prêt : scannez les OF.`
        : `BL ${expedition.value.numeroBl} : expédition déjà validée (consultation).`);
      if (estEnCours.value) scanOf.focus();
    } catch (e) { echec(e); }
  }
});

const scanOf = useScanDouchette({
  estActif: () => peutScanner.value,
  onScan: async (contenu) => {
    try {
      const avant = new Set(expedition.value.ofs.map(o => o.numeroOf));
      expedition.value = await magasinService.scannerOf(expedition.value.id, contenu);
      const ajoute = expedition.value.ofs.find(o => !avant.has(o.numeroOf));
      ofMisEnAvant.value = ajoute?.numeroOf ?? null;
      afficher(true, `OF ${ajoute?.numeroOf ?? ''} ajouté au BL ${expedition.value.numeroBl}.`);
    } catch (e) { echec(e); }
  }
});

const enCours = computed(() => scanBl.enCours.value || scanOf.enCours.value);

const retirer = async (of) => {
  try {
    expedition.value = await magasinService.retirerOf(expedition.value.id, of.id);
    afficher(true, `OF ${of.numeroOf} retiré du BL.`);
  } catch (e) {
    afficher(false, e.message);
  } finally {
    scanOf.focus();
  }
};

const terminer = async () => {
  const { confirme } = await confirmer({
    titre: `Valider l'expédition du BL ${expedition.value.numeroBl} ?`,
    message: `${expedition.value.ofs.length} OF. L'expédition ne pourra plus être modifiée.`,
    libelleConfirmer: 'Valider',
    libelleAnnuler: 'Continuer à scanner'
  });
  if (!confirme) return scanOf.focus();
  try {
    expedition.value = await magasinService.terminerExpedition(expedition.value.id);
    afficher(true, `Expédition du BL ${expedition.value.numeroBl} validée.`);
    scanBl.focus();
  } catch (e) {
    afficher(false, e.message);
  }
};

const ouvrirCamera = (cible) => {
  cibleCamera.value = cible;
  cameraVisible.value = true;
};

const surScanCamera = (texte) => {
  cameraVisible.value = false;
  (cibleCamera.value === 'BL' ? scanBl : scanOf).soumettre(texte);
};

onMounted(() => scanBl.focus());
</script>
