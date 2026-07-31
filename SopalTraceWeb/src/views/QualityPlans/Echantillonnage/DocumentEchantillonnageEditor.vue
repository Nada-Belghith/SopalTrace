<template>
  <div class="p-6 bg-slate-50 min-h-screen">
    <Toast position="top-right" />
    <ConfirmDialog />

    <div class="max-w-[1400px] mx-auto">
      <div class="animate-in fade-in zoom-in-95 duration-500">

        <PlanHeader 
          v-if="store.entete"
          :id="store.entete.id"
          :title="headerTitle"
          :subtitle="headerSubtitle"
          icon="pi pi-check-square"
          iconColorClass="text-emerald-500"
          :is-read-only="isReadOnly"
          :version="store.entete.version"
          :statut="store.entete.statut"
          :is-restoring="isVersioningSaving"
        />

        <!-- SECTION 1 : CONFIGURATION GÉNÉRALE -->
        <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden mb-6">
          <div class="bg-[#1e293b] text-white px-6 py-4 flex items-center justify-between">
            <div class="flex items-center gap-3">
              <i class="pi pi-sliders-h text-emerald-400"></i>
              <h2 class="font-bold uppercase tracking-wider text-sm">Paramètres du Plan</h2>
            </div>
            
          </div>

          <div class="p-8">
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              <!-- Niveau de contrôle -->
              <div class="space-y-3">
                <label class="block text-[11px] font-black text-slate-500 uppercase tracking-widest">Niveau de contrôle</label>
                <div class="flex gap-2 p-1 bg-slate-100 rounded-xl border border-slate-200">
                  <button v-for="lvl in [{v:'I', l:'NIVEAU I'}, {v:'II', l:'NIVEAU II'}, {v:'III', l:'NIVEAU III'}]" :key="lvl.v"
                    @click="!isReadOnly && (store.entete.niveauControle = lvl.v)"
                    :class="[
                      'flex-1 py-2 px-1 text-[9px] font-black rounded-lg transition-all',
                      store.entete.niveauControle === lvl.v ? 'bg-emerald-600 text-white shadow-lg shadow-emerald-200' : 'text-slate-400 hover:bg-white hover:text-slate-600'
                    ]">
                    {{ lvl.l }}
                  </button>
                </div>
              </div>

              <!-- Plan d'échantillonnage -->
              <div class="space-y-3">
                <label class="block text-[11px] font-black text-slate-500 uppercase tracking-widest">Plan d'échantillonnage</label>
                <div class="flex gap-2 p-1 bg-slate-100 rounded-xl border border-slate-200">
                  <button v-for="p in ['SIMPLE', 'DOUBLE']" :key="p"
                    @click="!isReadOnly && (store.entete.typePlan = p)"
                    :class="[
                      'flex-1 py-2 px-1 text-[9px] font-black rounded-lg transition-all',
                      store.entete.typePlan === p ? 'bg-emerald-600 text-white shadow-lg shadow-emerald-200' : 'text-slate-400 hover:bg-white hover:text-slate-600'
                    ]">
                    {{ p }}
                  </button>
                </div>
              </div>

              <!-- Mode de contrôle -->
              <div class="space-y-3">
                <label class="block text-[11px] font-black text-slate-500 uppercase tracking-widest">Mode de contrôle</label>
                <div class="flex gap-2 p-1 bg-slate-100 rounded-xl border border-slate-200">
                  <button v-for="m in [{v:'REDUIT', l:'RÉDUIT'}, {v:'NORMAL', l:'NORMAL'}, {v:'RENFORCE', l:'RENFORCÉ'}]" :key="m.v"
                    @click="!isReadOnly && (store.entete.modeControle = m.v)"
                    :class="[
                      'flex-1 py-2 px-1 text-[9px] font-black rounded-lg transition-all',
                      store.entete.modeControle === m.v ? 'bg-emerald-600 text-white shadow-lg shadow-emerald-200' : 'text-slate-400 hover:bg-white hover:text-slate-600'
                    ]">
                    {{ m.l }}
                  </button>
                </div>
              </div>

              <!-- NQA -->
              <div class="space-y-3">
                <label class="block text-[11px] font-black text-slate-500 uppercase tracking-widest">NQA</label>
                <div class="relative w-full max-w-[120px]">
                  <select
                    v-model="nqaInput"
                    :disabled="isReadOnly"
                    class="w-full py-2 px-3 bg-white border-2 border-emerald-100 rounded-xl outline-none focus:border-emerald-500 font-black text-slate-700 shadow-sm transition-all text-center text-sm cursor-pointer appearance-none"
                  >
                    <option value="" disabled>-- NQA --</option>
                    <option v-for="nqa in store.nqaList" :key="nqa.id" :value="nqa.valeurNqa">
                      {{ nqa.valeurNqa }}
                    </option>
                  </select>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- SECTION 2 : TABLE DES RÈGLES -->
        <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden mb-6">
          <div class="bg-[#1e293b] text-white px-6 py-4 flex items-center justify-between">
            <div class="flex items-center gap-3">
              <i class="pi pi-table text-emerald-400"></i>
              <h2 class="font-bold uppercase tracking-wider text-sm">Tableau de détermination de l'échantillon</h2>
            </div>
            <button v-if="false" @click="store.ajouterRegle" 
              class="bg-emerald-600 hover:bg-emerald-700 text-white px-4 py-2 rounded-lg text-xs font-black uppercase tracking-widest flex items-center gap-2 transition-all active:scale-95 shadow-lg shadow-emerald-500/20">
              <i class="pi pi-plus-circle"></i> Ajouter une tranche
            </button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse min-w-[1000px]">
              <thead>
                <tr class="bg-slate-100 border-b-2 border-slate-200">
                  <th class="p-4 text-[10px] font-black text-slate-500 uppercase tracking-widest text-center border-r border-slate-200 w-16">N°</th>
                  <th class="p-4 text-[10px] font-black text-slate-500 uppercase tracking-widest text-center border-r border-slate-200 w-48">Effectif du lot (Tranche)</th>
                  <th class="p-4 text-[10px] font-black text-slate-500 uppercase tracking-widest text-center border-r border-slate-200 w-24">Lettre Code</th>
                  <th class="p-4 text-[10px] font-black text-slate-500 uppercase tracking-widest text-center border-r border-slate-200 bg-emerald-50/30">Échantillon Global (A)</th>
                  <th class="p-4 text-[10px] font-black text-slate-500 uppercase tracking-widest text-center border-r border-slate-200">Nb Postes (B)</th>
                  <th class="p-4 text-[10px] font-black text-slate-500 uppercase tracking-widest text-center border-r border-slate-200 bg-emerald-50/30">Échantillon /Poste (A/B)</th>
                  <th class="p-4 text-[10px] font-black text-emerald-600 uppercase tracking-widest text-center border-r border-slate-200">Critère d'acceptation</th>
                  <th class="p-4 text-[10px] font-black text-rose-600 uppercase tracking-widest text-center border-r border-slate-200">Critère de rejet</th>
                  <th v-if="!isReadOnly" class="p-4 text-[10px] font-black text-slate-400 uppercase tracking-widest text-center w-12"></th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="(r, idx) in store.regles" :key="r._uid" class="hover:bg-emerald-50/30 transition-colors group">
                  <td class="p-4 text-center font-black text-slate-400 text-xs bg-slate-50/30 border-r border-slate-200">{{ idx + 1 }}</td>
                  
                  <td colspan="5" class="p-4 text-center border-r border-slate-200 bg-slate-50/50">
                    <span class="text-[10px] font-bold text-slate-400 italic uppercase tracking-wider">À remplir lors de la production</span>
                  </td>

                  <td class="p-2 border-r border-slate-200">
                    <input v-model="store.entete.critereAcceptationAc" type="number" :readonly="isReadOnly"
                      class="w-full text-center py-2 text-sm font-black text-emerald-700 bg-transparent outline-none focus:bg-white rounded border border-transparent focus:border-emerald-300">
                  </td>
                  <td class="p-2 border-r border-slate-200">
                    <input v-model="store.entete.critereRejetRe" type="number" :readonly="isReadOnly"
                      class="w-full text-center py-2 text-sm font-black text-rose-700 bg-transparent outline-none focus:bg-white rounded border border-transparent focus:border-rose-300">
                  </td>

                  <td v-if="!isReadOnly" class="p-2 text-center">
                    <button @click="store.deleteRule(r._uid)" class="w-8 h-8 rounded-full flex items-center justify-center text-slate-300 hover:text-rose-600 hover:bg-rose-50 transition-all opacity-0 group-hover:opacity-100">
                      <i class="pi pi-trash"></i>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-if="store.regles.length === 0" class="p-20 text-center text-slate-400 bg-slate-50 italic text-sm">
            Aucune règle n'est configurée.
          </div>
        </div>

        <!-- REMARQUES & LÉGENDE -->
        <RemarquesLegendeBox 
          v-model:remarques="store.entete.remarques"
          v-model:legendeMoyens="store.entete.legendeMoyens"
          :is-read-only="isReadOnly"
        />

        <!-- ACTIONS FINALES -->
        <DocumentSaveManager 
          v-if="!isReadOnly"
          :plan-id="store.entete.id"
          :statut="store.entete.statut"
          :is-loading="isSaving"
          :is-read-only="isReadOnly"
          @save-direct="handleSaveDirect"
          @save-correction="handleSaveCorrection"
          @save-new-version="handleSaveNewVersion"
          @cancel="onCloseEditor"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import DocumentSaveManager from '@/components/Shared/DocumentSaveManager.vue';
import { useDocumentEchantillonnageStore } from '@/stores/documentEchantillonnageStore';

import { useDocumentHeaderMeta } from '@/composables/useDocumentHeaderMeta';
import { useDocumentSaveManager } from '@/composables/useDocumentSaveManager';
import { useActivePlanConfirmation } from '@/composables/useActivePlanConfirmation';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const confirm = useConfirm();
const store = useDocumentEchantillonnageStore();

const { confirmArchivagePlanActif } = useActivePlanConfirmation();

const isSaving = ref(false);
const isVersioningSaving = ref(false);

const { isReadOnly, isArchived, isEditMode } = useDocumentHeaderMeta(route, store);


const headerTitle = computed(() => {
  if (isArchived.value) return "Archive Plan d'Échantillonnage";
  if (isEditMode.value) return "Édition du Plan d'Échantillonnage";
  return "Création d'un Plan d'Échantillonnage Standard";
});

const headerSubtitle = computed(() => {
  if (isArchived.value) return "Vous consultez une archive. Restaurez-la pour l'activer.";
  return "Configuration des niveaux de contrôle ISO 2859-1";
});

const nqaInput = computed({
  get: () => {
    if (store.entete.nqaId) {
      const found = store.nqaList.find(n => n.id === store.entete.nqaId);
      if (found) return found.valeurNqa;
    }
    return store.entete.valeurNqa;
  },
  set: (val) => {
    store.entete.valeurNqa = val;
    if (val) {
      const found = store.nqaList.find(n => Number(n.valeurNqa) === Number(val));
      store.entete.nqaId = found ? found.id : null;
    } else {
      store.entete.nqaId = null;
    }
  }
});


onMounted(async () => {
  if (!store.isDicosLoaded) {
    await store.fetchDictionnaires();
  }
  await store.fetchPlansExistants();

  const id = route.params.id;
  if (id && id !== 'nouveau') {
    await store.loadDocument(id);
  } else {
    store.entete = {
      id: null,
      niveauControle: 'I',
      typePlan: 'SIMPLE',
      modeControle: 'NORMAL',
      nqaId: store.nqaList[0]?.id,
      valeurNqa: 0.65,
      version: 1,
      statut: 'ACTIF',
      remarques: '',
      legendeMoyens: ''
    };
    store.regles = [];
    store.addRule();
  }
});

const { handleSaveDirect, handleSaveCorrection, handleSaveNewVersion } = useDocumentSaveManager({
  callbacks: {
    validateForm: async () => {
      return true; // Ajouter une validation spécifique si nécessaire
    },
    preSaveHook: async () => {
      if (!store.entete.id) { // Création d'un nouveau plan
        const planActif = store.plansExistants && store.plansExistants.length > 0 ? store.plansExistants[0] : null;
        if (planActif) {
          const isConfirmed = await confirmArchivagePlanActif({
            typeDocument: "plan d'échantillonnage",
            identifiant: "standard",
            version: planActif.version || 1
          });
          if (!isConfirmed) return false;
          await new Promise(resolve => setTimeout(resolve, 200));
        }
      }
      return true;
    },
    onSaveDirect: async () => {
      isSaving.value = true;
      try {
        if (!store.entete.id) {
          await store.createDocument();
        } else {
          await store.updateDocument();
        }
        return { success: true };
      } finally {
        isSaving.value = false;
      }
    },
    onSaveCorrection: async () => {
      isSaving.value = true;
      try {
        await store.updateDocument();
        return { success: true };
      } finally {
        isSaving.value = false;
      }
    },
    onSaveNewVersion: async (motif) => {
      isSaving.value = true;
      try {
        await store.createNewVersion(motif);
        return { success: true };
      } finally {
        isSaving.value = false;
      }
    }
  },
  toast,
  router,
  returnUrl: '/dev/hub'
});

const onCloseEditor = () => {
  router.push('/dev/hub');
};
</script>
