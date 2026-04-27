<template>
  <div class="max-w-[1400px] mx-auto animate-in fade-in duration-500">
    <Toast position="top-right" />
    <ConfirmDialog></ConfirmDialog>

    <div class="flex flex-col lg:flex-row justify-between items-start lg:items-end mb-10 gap-6">
      <div>
        <h1 class="text-3xl font-black text-slate-900 tracking-tight">Modèles de Vérification Machine</h1>
        <p class="text-slate-500 mt-1 font-medium text-sm">Gérez vos modèles de rapports de vérification (BEE, MAS, SER, etc.)</p>
      </div>

      <div class="flex items-center bg-slate-200/50 p-1.5 rounded-xl border border-slate-200 shadow-inner">
        <button 
          @click="vueActuelle = 'ACTIFS'" 
          :class="vueActuelle === 'ACTIFS' ? 'bg-white shadow text-orange-600 ring-1 ring-slate-900/5' : 'text-slate-500 hover:text-slate-700'" 
          class="px-6 py-2.5 rounded-lg text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2"
        >
          <i class="pi pi-check-circle"></i> Actifs
        </button>
        <button 
          @click="vueActuelle = 'ARCHIVES'" 
          :class="vueActuelle === 'ARCHIVES' ? 'bg-white shadow text-slate-800 ring-1 ring-slate-900/5' : 'text-slate-500 hover:text-slate-700'" 
          class="px-6 py-2.5 rounded-lg text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2"
        >
          <i class="pi pi-box"></i> Archives
        </button>
      </div>
    </div>

    <div v-if="isLoading" class="flex flex-col items-center justify-center py-20 text-orange-500">
      <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
      <p class="text-sm font-bold text-slate-500 uppercase tracking-widest">Chargement des modèles...</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">

      <div 
        v-for="modele in modelesFiltres" 
        :key="modele.id"
        @click.self="consulter(modele.id)"
        class="bg-white rounded-2xl border border-slate-200 shadow-sm hover:shadow-xl transition-all overflow-hidden flex flex-col group cursor-pointer"
        :class="modele.statut === 'ARCHIVE' ? 'opacity-75 grayscale-[30%]' : 'hover:border-orange-400'">

        <div @click="consulter(modele.id)" class="p-6 flex-1">
          <div class="flex justify-between items-start mb-4">
            <h3 class="text-base font-black text-slate-900 leading-tight group-hover:text-orange-600 transition-colors">
              {{ modele.nom }}
            </h3>
            <div class="flex flex-col gap-1 items-end shrink-0">
              <span class="px-2 py-1 bg-slate-100 text-slate-500 text-[10px] font-bold uppercase rounded-md tracking-wider">
                v{{ modele.version }}
              </span>
              <span 
                v-if="modele.statut === 'ARCHIVE'" 
                class="px-2 py-0.5 bg-red-100 text-red-600 border border-red-200 text-[9px] font-black uppercase rounded-md tracking-widest shadow-sm"
              >
                Archivé
              </span>
              <span 
                v-else 
                class="text-[9px] font-bold text-emerald-500 uppercase tracking-widest"
              >
                {{ modele.statut }}
              </span>
            </div>
          </div>

          <div class="flex flex-wrap gap-2 mb-6">
            <div class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-orange-50 text-orange-700 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-orange-200/60 shadow-sm">
              <i class="ri-settings-line text-orange-500 text-[11px]"></i> {{ modele.machineCode }}
            </div>
            <div class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-file text-slate-400 text-[11px]"></i> {{ modele.typeRapport || 'N/A' }}
            </div>
          </div>

          <p class="text-xs text-slate-500 flex items-center gap-2">
            <i class="pi pi-info-circle"></i> 
            {{ modele.creePar && modele.creeLe ? `Créé par ${modele.creePar} le ${formatDate(modele.creeLe)}` : 'Aucune info' }}
          </p>
        </div>

        <div class="p-3 bg-slate-50 border-t border-slate-100 flex gap-3" @click.stop>
          <template v-if="modele.statut !== 'ARCHIVE'">
            <button 
              @click="editer(modele.id)" 
              class="flex-1 bg-orange-600 text-white py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-orange-700 transition-colors"
            >
              <i class="pi pi-pencil"></i> Éditer
            </button>
            <button 
              @click="confirmArchivage(modele)" 
              class="w-10 shrink-0 bg-red-50 text-red-500 py-2 rounded-lg font-bold text-xs flex items-center justify-center hover:bg-red-500 hover:text-white border border-transparent hover:border-red-600 transition-colors" 
              title="Archiver le modèle"
            >
              <i class="pi pi-box"></i>
            </button>
          </template>

          <template v-else>
            <button 
              @click="confirmRestoration(modele)" 
              class="flex-1 bg-amber-100 text-amber-700 py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-amber-200 transition-colors"
            >
              <i class="pi pi-history"></i> Restaurer
            </button>
          </template>
        </div>
      </div>

      <div 
        v-if="modelesFiltres.length === 0" 
        class="col-span-full py-12 text-center bg-slate-50 rounded-2xl border-2 border-dashed border-slate-200"
      >
        <i class="pi pi-inbox text-4xl text-slate-300 mb-3"></i>
        <h3 class="text-lg font-bold text-slate-700">
          Aucun modèle {{ vueActuelle === 'ARCHIVES' ? 'archivé' : 'trouvé' }}
        </h3>
        <p class="text-sm text-slate-500 mt-1">
          {{ vueActuelle === 'ARCHIVES' ? 'Aucun modèle n\'a été archivé.' : 'Créez un nouveau modèle de vérification machine.' }}
        </p>
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

const vueActuelle = ref('ACTIFS');
const isLoading = ref(true);
const modeles = ref([]);

onMounted(async () => {
  await chargerModeles();
});

const chargerModeles = async () => {
  try {
    isLoading.value = true;
    const response = await apiClient.get('/modeles-verif-machine');
    modeles.value = response.data.data || [];
  } catch (error) {
    console.error(error);
    toast.add({ 
      severity: 'error', 
      summary: 'Erreur', 
      detail: 'Impossible de charger les modèles de vérification.', 
      life: 3000 
    });
  } finally {
    isLoading.value = false;
  }
};

const modelesFiltres = computed(() => {
  return modeles.value.filter(m => {
    const matchStatut = vueActuelle.value === 'ACTIFS' 
      ? m.statut === 'ACTIF' 
      : m.statut === 'ARCHIVE';
    return matchStatut;
  });
});

const formatDate = (date) => {
  if (!date) return '';
  return new Date(date).toLocaleDateString('fr-FR');
};

const consulter = (id) => {
  router.push(`/dev/verif-machine/${id}`);
};

const editer = (id) => {
  router.push(`/dev/verif-machine/${id}/editer`);
};

const confirmArchivage = (modele) => {
  confirm.require({
    message: `Voulez-vous vraiment archiver le modèle "${modele.nom}" ?`,
    header: 'Confirmation d\'archivage',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Archiver',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await archiver(modele);
    }
  });
};

const archiver = async (modele) => {
  try {
    isLoading.value = true;
    await apiClient.put(`/modeles-verif-machine/${modele.id}/statut?statut=ARCHIVE`);
    modele.statut = 'ARCHIVE';
    toast.add({ 
      severity: 'success', 
      summary: 'Archivé', 
      detail: `Le modèle "${modele.nom}" a été archivé.`, 
      life: 3000 
    });
  } catch (error) {
    console.error(error);
    toast.add({ 
      severity: 'error', 
      summary: 'Erreur', 
      detail: 'L\'archivage a échoué.', 
      life: 3000 
    });
  } finally {
    isLoading.value = false;
  }
};

const confirmRestoration = (modele) => {
  confirm.require({
    message: `Restaurer le modèle "${modele.nom}" ?`,
    header: 'Confirmation de restauration',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Restaurer',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-success',
    accept: async () => {
      await restaurer(modele);
    }
  });
};

const restaurer = async (modele) => {
  try {
    isLoading.value = true;
    await apiClient.post('/modeles-verif-machine/restaurer', {
      ancienId: modele.id,
      modifiePar: 'ADMIN',
      motifModification: 'Restauration depuis le hub'
    });
    modele.statut = 'ACTIF';
    toast.add({ 
      severity: 'success', 
      summary: 'Restauré', 
      detail: `Le modèle "${modele.nom}" a été restauré.`, 
      life: 3000 
    });
  } catch (error) {
    console.error(error);
    toast.add({ 
      severity: 'error', 
      summary: 'Erreur', 
      detail: 'La restauration a échoué.', 
      life: 3000 
    });
  } finally {
    isLoading.value = false;
  }
};
</script>
