<template>
  <div class="max-w-[1400px] mx-auto animate-in fade-in duration-500">
    <Toast position="top-right" />
    <ConfirmDialog></ConfirmDialog>

    <div class="flex flex-col lg:flex-row justify-between items-start lg:items-end mb-10 gap-6">
      <div>
        <h1 class="text-3xl font-black text-slate-900 tracking-tight">Gestion des Plans</h1>
        <p class="text-slate-500 mt-1 font-medium text-sm">Visualisez et gérez vos plans de contrôle par article.</p>
      </div>

      <div class="flex items-center bg-slate-200/50 p-1.5 rounded-xl border border-slate-200 shadow-inner">
        <button @click="vueActuelle = 'ACTIFS'" :class="vueActuelle === 'ACTIFS' ? 'bg-white shadow text-blue-600 ring-1 ring-slate-900/5' : 'text-slate-500 hover:text-slate-700'" class="px-6 py-2.5 rounded-lg text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2">
          <i class="pi pi-check-circle"></i> Actifs
        </button>
        <button @click="vueActuelle = 'BROUILLONS'" :class="vueActuelle === 'BROUILLONS' ? 'bg-white shadow text-amber-600 ring-1 ring-slate-900/5' : 'text-slate-500 hover:text-slate-700'" class="px-6 py-2.5 rounded-lg text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2">
          <i class="pi pi-pencil"></i> Brouillons
        </button>
        <button @click="vueActuelle = 'ARCHIVES'" :class="vueActuelle === 'ARCHIVES' ? 'bg-white shadow text-slate-800 ring-1 ring-slate-900/5' : 'text-slate-500 hover:text-slate-700'" class="px-6 py-2.5 rounded-lg text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2">
          <i class="pi pi-box"></i> Archives
        </button>
      </div>
    </div>

    <div class="flex flex-wrap gap-2 mb-8 p-1.5 bg-slate-200/50 rounded-2xl w-max border border-slate-200">
      <button
        v-for="cat in categories"
        :key="cat.id"
        @click="filtreActif = cat.id"
        class="px-5 py-2.5 rounded-xl text-xs font-black uppercase tracking-wider transition-all"
        :class="filtreActif === cat.id ? 'bg-white text-blue-600 shadow-sm' : 'text-slate-500 hover:text-slate-700'"
      >
        {{ cat.label }}
      </button>
    </div>

    <div v-if="isLoading" class="flex flex-col items-center justify-center py-20 text-blue-500">
      <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
      <p class="text-sm font-bold text-slate-500 uppercase tracking-widest">Chargement des plans...</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
      
      <div v-for="plan in plansFiltres" :key="plan.id"
           @click.self="consulter(plan.category, plan.id)"
           class="bg-white rounded-2xl border border-slate-200 shadow-sm hover:shadow-xl transition-all overflow-hidden flex flex-col group cursor-pointer"
           :class="plan.statut === 'ARCHIVE' ? 'opacity-75 grayscale-[30%]' : 'hover:border-blue-400'">

        <div @click="consulter(plan.category, plan.id)" class="p-6 flex-1">
          <div class="flex justify-between items-start mb-4">
            <h3 class="text-base font-black text-slate-900 leading-tight group-hover:text-blue-600 transition-colors">{{ plan.libelle }}</h3>
            <div class="flex flex-col gap-1 items-end shrink-0">
              <span class="px-2 py-1 bg-slate-100 text-slate-500 text-[10px] font-bold uppercase rounded-md tracking-wider">v{{ plan.version }}</span>
              <span v-if="plan.statut === 'ARCHIVE'" class="px-2 py-0.5 bg-red-100 text-red-600 border border-red-200 text-[9px] font-black uppercase rounded-md tracking-widest shadow-sm">Archivé</span>
              <span v-else-if="plan.statut === 'BROUILLON'" class="px-2 py-0.5 bg-amber-100 text-amber-700 border border-amber-200 text-[9px] font-black uppercase rounded-md tracking-widest shadow-sm">Brouillon</span>
              <span v-else-if="plan.statut" class="text-[9px] font-bold text-emerald-500 uppercase tracking-widest">{{ plan.statut }}</span>
            </div>
          </div>

          <div class="flex flex-wrap gap-2 mb-6 mt-auto">
            <div v-if="plan.nature && plan.nature !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-box text-slate-400 text-[11px]"></i> {{ plan.nature }}
            </div>
            <div v-if="plan.type && plan.type !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-tag text-slate-400 text-[11px]"></i> {{ plan.type }}
            </div>
            <div v-if="plan.poste && plan.poste !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-map-marker text-slate-400 text-[11px]"></i> {{ plan.poste }}
            </div>
          </div>

          <p class="text-xs text-slate-500 flex items-center gap-2">
            <i class="pi pi-list"></i> {{ plan.description }}
          </p>
        </div>

        <div class="p-3 bg-slate-50 border-t border-slate-100 flex gap-3" @click.stop>
          <template v-if="plan.statut !== 'ARCHIVE'">
            <button @click="editer(plan.category, plan.id)" class="flex-1 bg-slate-800 text-white py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-slate-900 transition-colors">
              <i class="pi pi-pencil"></i> Éditer
            </button>
            <button v-if="plan.statut === 'BROUILLON'" @click="confirmSuppressionBrouillon(plan)" class="w-10 shrink-0 bg-red-50 text-red-500 py-2 rounded-lg font-bold text-xs flex items-center justify-center hover:bg-red-500 hover:text-white border border-transparent hover:border-red-600 transition-colors" title="Supprimer définitivement le brouillon">
              <i class="pi pi-trash"></i>
            </button>
            <button v-else @click="confirmArchivage(plan)" class="w-10 shrink-0 bg-red-50 text-red-500 py-2 rounded-lg font-bold text-xs flex items-center justify-center hover:bg-red-500 hover:text-white border border-transparent hover:border-red-600 transition-colors" title="Archiver le plan">
              <i class="pi pi-box"></i>
            </button>
          </template>

          <template v-else>
            <button @click="editer(plan.category, plan.id)" class="w-full bg-amber-100 text-amber-700 py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-amber-200 transition-colors">
              <i class="pi pi-history"></i> Ouvrir
            </button>
          </template>
        </div>
      </div>

      <div v-if="plansFiltres.length === 0" class="col-span-full py-12 text-center bg-slate-50 rounded-2xl border-2 border-dashed border-slate-200">
        <i class="pi pi-inbox text-4xl text-slate-300 mb-3"></i>
        <h3 class="text-lg font-bold text-slate-700">Aucun plan {{ vueActuelle === 'ARCHIVES' ? 'archivé' : 'trouvé' }}</h3>
        <p class="text-sm text-slate-500 mt-1">Modifiez vos filtres ou créez un nouveau plan.</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import apiClient from '@/services/apiClient';

const router = useRouter();
const toast = useToast();
const confirm = useConfirm();

const filtreActif = ref('ALL');
const vueActuelle = ref('ACTIFS');
const isLoading = ref(true);
const plans = ref([]);

const categories = [
  { id: 'ALL', label: 'Tous les plans' },
  { id: 'FAB', label: 'Fabrication' }
];

onMounted(async () => {
  await chargerPlans();
});

const chargerPlans = async () => {
  try {
    isLoading.value = true;
    const response = await apiClient.get('/hub/plans');
    plans.value = response.data.data || response.data || [];
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les plans', life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

const confirmSuppressionBrouillon = (plan) => {
  confirm.require({
    message: `Retirer le brouillon "${plan.libelle}" de la liste ?`,
    header: 'Confirmation de suppression',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Supprimer',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await supprimerBrouillon(plan);
    }
  });
};

const supprimerBrouillon = async (plan) => {
  try {
    isLoading.value = true;
    await apiClient.delete(`/hub/plans/${plan.category}/${plan.id}`);
    plans.value = plans.value.filter(p => p.id !== plan.id);
    toast.add({ severity: 'success', summary: 'Supprimé', detail: 'Le brouillon a été retiré de la liste.', life: 3000 });
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'La suppression du brouillon a échoué.', life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

const plansFiltres = computed(() => {
  return plans.value.filter(p => {
    const matchCategory = filtreActif.value === 'ALL' || p.category === filtreActif.value;
    const matchStatut = vueActuelle.value === 'ACTIFS'
      ? p.statut === 'ACTIF'
      : vueActuelle.value === 'BROUILLONS'
        ? p.statut === 'BROUILLON'
        : p.statut === 'ARCHIVE';
    return matchCategory && matchStatut;
  });
});

const confirmArchivage = (plan) => {
  confirm.require({
    message: `Voulez-vous vraiment archiver le plan "${plan.libelle}" ?`,
    header: 'Confirmation d\'archivage',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Archiver',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await archiver(plan);
    }
  });
};

const archiver = async (plan) => {
  try {
    isLoading.value = true;
    await apiClient.put(`/hub/plans/${plan.category}/${plan.id}/statut?statut=ARCHIVE`);
    plan.statut = 'ARCHIVE';
    toast.add({ severity: 'success', summary: 'Archivé', detail: 'Le plan a été placé dans les archives.', life: 3000 });
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: "L'archivage a échoué.", life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

const editer = (category, id) => {
  if (category === 'FAB') {
    router.push(`/dev/fab/plans/editer/${id}`);
    return;
  }

  toast.add({ severity: 'warn', summary: 'Catégorie inconnue', detail: 'Redirection non disponible.', life: 3000 });
};

// Fonction de consultation (Ajoute ?view=true dans l'URL)
const consulter = (category, id) => {
  if (category === 'FAB') {
    router.push({ path: `/dev/fab/plans/editer/${id}`, query: { view: 'true' } });
    return;
  }

  toast.add({ severity: 'warn', summary: 'Catégorie inconnue', detail: 'Consultation non disponible.', life: 3000 });
};
</script>