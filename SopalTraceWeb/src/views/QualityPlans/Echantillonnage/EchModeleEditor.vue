<template>
  <div class="p-6 bg-slate-50 min-h-screen">
    <Toast position="top-right" />
    <ConfirmDialog />

    <VersioningDialog :visible="showVersioningDialog"
                      mode="ECHAN"
                      :is-loading="isVersioningSaving"
                      @confirm="onVersioningConfirm"
                      @cancel="showVersioningDialog = false"
                      @update:visible="showVersioningDialog = $event" />

    <div class="max-w-[1400px] mx-auto">
      <div class="animate-in fade-in zoom-in-95 duration-500">

        <PlanHeader 
          :id="store.entete.id"
          :title="headerTitle"
          :subtitle="headerSubtitle"
          icon="pi pi-check-square"
          iconColorClass="text-emerald-500"
          :is-read-only="isReadOnly"
          :version="store.entete.version"
          :statut="store.entete.statut"
          :is-restoring="isVersioningSaving"
          @restaurer="onSave"
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
                  <input v-model="nqaInput" type="number" step="0.01" 
                    :readonly="isReadOnly"
                    class="w-full py-2 px-3 bg-white border-2 border-emerald-100 rounded-xl outline-none focus:border-emerald-500 font-black text-slate-700 shadow-sm transition-all text-center text-sm" 
                    placeholder="0.65" />
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
                    <input v-model="r.critereAcceptationAc" type="number" :readonly="isReadOnly"
                      class="w-full text-center py-2 text-sm font-black text-emerald-700 bg-transparent outline-none focus:bg-white rounded border border-transparent focus:border-emerald-300">
                  </td>
                  <td class="p-2 border-r border-slate-200">
                    <input v-model="r.critereRejetRe" type="number" :readonly="isReadOnly"
                      class="w-full text-center py-2 text-sm font-black text-rose-700 bg-transparent outline-none focus:bg-white rounded border border-transparent focus:border-rose-300">
                  </td>

                  <td v-if="!isReadOnly" class="p-2 text-center">
                    <button @click="store.supprimerRegle(r._uid)" class="w-8 h-8 rounded-full flex items-center justify-center text-slate-300 hover:text-rose-600 hover:bg-rose-50 transition-all opacity-0 group-hover:opacity-100">
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
        <div v-if="!isReadOnly" class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-6 rounded-b-xl">
          <EditorActions 
            :label="editorLabel"
            loading-label="Traitement..."
            :icon="editorIcon"
            :variant="editorVariant"
            :is-loading="isSaving && !showVersioningDialog"
            @submit="onSave"
          />
        </div>
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
import EditorActions from '@/components/Shared/EditorActions.vue';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import { usePlanEchanStore } from '@/stores/planEchanStore';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const store = usePlanEchanStore();

const isSaving = ref(false);
const isVersioningSaving = ref(false);
const showVersioningDialog = ref(false);
const isReadOnly = computed(() => route.query.view === 'true' || store.entete.statut === 'ARCHIVE');
const isArchived = computed(() => store.entete.statut === 'ARCHIVE');
const isEditMode = computed(() => !!store.entete.id);

const headerTitle = computed(() => {
  if (isArchived.value) return "Archive Plan d'Échantillonnage";
  if (isEditMode.value) return "Édition du Plan d'Échantillonnage";
  return "Création d'un Plan d'Échantillonnage Standard";
});

const headerSubtitle = computed(() => {
  if (isArchived.value) return "Vous consultez une archive. Restaurez-la pour l'activer.";
  return "Configuration des niveaux de contrôle ISO 2859-1";
});

const editorLabel = computed(() => {
  if (isArchived.value) return 'Restaurer ce Plan';
  if (isEditMode.value) return 'Enregistrer le Plan';
  return 'Enregistrer le Plan';
});

const editorIcon = computed(() => {
  if (isArchived.value) return 'pi pi-history';
  if (isEditMode.value) return 'pi pi-save';
  return 'pi pi-check';
});

const editorVariant = computed(() => {
  if (isArchived.value) return 'warning';
  return 'primary';
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

  const id = route.params.id;
  if (id && id !== 'nouveau') {
    await store.chargerPlan(id);
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
    store.ajouterRegle();
  }
});

const onSave = async () => {
  if (isArchived.value) {
    // Archive: need restoration flow
    showVersioningDialog.value = true;
    return;
  }

  // For new plan creation or edit (create new version)
  isSaving.value = true;
  try {
    if (isEditMode.value) {
      // Automatically create a new version with a default motif
      await store.creerNouvelleVersion('Modification via UI');
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Nouvelle version créée et activée.', life: 3000 });
    } else {
      await store.sauvegarderPlan();
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Plan créé et activé.', life: 3000 });
    }
    router.push('/dev/hub-plans');
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: err.response?.data?.message || 'Échec de la sauvegarde.', life: 3000 });
  } finally {
    isSaving.value = false;
  }
};

const onVersioningConfirm = async (motif) => {
  showVersioningDialog.value = false;
  isVersioningSaving.value = true;
  try {
    if (isArchived.value) {
      await store.restaurerPlan(motif);
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Version restaurée et activée.', life: 3000 });
    } else {
      await store.creerNouvelleVersion(motif);
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Nouvelle version générée en brouillon.', life: 3000 });
    }
    router.push('/dev/hub-plans');
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Erreur lors du traitement.', life: 3000 });
  } finally {
    isVersioningSaving.value = false;
  }
};
</script>
