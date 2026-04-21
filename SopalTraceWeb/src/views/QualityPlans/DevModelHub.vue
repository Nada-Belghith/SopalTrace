<template>
  <div class="max-w-[1400px] mx-auto animate-in fade-in duration-500">
    <Toast position="top-right" />
    <ConfirmDialog></ConfirmDialog>

    <div class="flex flex-col lg:flex-row justify-between items-start lg:items-end mb-10 gap-6">
      <div>
        <h1 class="text-3xl font-black text-slate-900 tracking-tight">Gestion des Modèles</h1>
        <p class="text-slate-500 mt-1 font-medium text-sm">Visualisez et gérez vos modèles des plans du contrôle existants.</p>
      </div>

      <div class="flex items-center bg-slate-200/50 p-1.5 rounded-xl border border-slate-200 shadow-inner">
        <button @click="vueActuelle = 'ACTIFS'" :class="vueActuelle === 'ACTIFS' ? 'bg-white shadow text-blue-600 ring-1 ring-slate-900/5' : 'text-slate-500 hover:text-slate-700'" class="px-6 py-2.5 rounded-lg text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2">
          <i class="pi pi-check-circle"></i> Actifs
        </button>
        <button @click="vueActuelle = 'ARCHIVES'" :class="vueActuelle === 'ARCHIVES' ? 'bg-white shadow text-slate-800 ring-1 ring-slate-900/5' : 'text-slate-500 hover:text-slate-700'" class="px-6 py-2.5 rounded-lg text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2">
          <i class="pi pi-box"></i> Archives
        </button>
      </div>
    </div>

    <div class="flex flex-col md:flex-row gap-4 mb-8 items-start md:items-center justify-between">
      <div class="flex flex-wrap gap-2 p-1.5 bg-slate-200/50 rounded-2xl w-max border border-slate-200">
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

      <div v-if="operationsDisponibles.length > 0" class="flex items-center gap-3">
        <label class="text-xs font-bold text-slate-500 uppercase tracking-widest hidden md:block" title="Filtrer par opération">
          <i class="pi pi-filter mr-1"></i> Opération :
        </label>
        <select v-model="operationFiltre" class="px-4 py-2.5 rounded-xl text-xs font-bold uppercase tracking-wider border border-slate-200 bg-white text-slate-700 focus:ring-2 focus:ring-blue-500 outline-none cursor-pointer transition-all shadow-sm">
          <option value="">Toutes les opérations</option>
          <option v-for="op in operationsDisponibles" :key="op" :value="op">{{ op }}</option>
        </select>
      </div>
    </div>

    <div v-if="isLoading" class="flex flex-col items-center justify-center py-20 text-blue-500">
      <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
      <p class="text-sm font-bold text-slate-500 uppercase tracking-widest">Chargement des modèles depuis l'ERP...</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
      <div v-for="mod in modelesFiltres" :key="mod.id"
           @click.self="consulter(mod.category, mod.id)"
           class="bg-white rounded-2xl border border-slate-200 shadow-sm hover:shadow-xl transition-all overflow-hidden flex flex-col group cursor-pointer"
           :class="mod.statut === 'ARCHIVE' ? 'opacity-75 grayscale-[30%]' : getHoverBorderColor(mod.category)">

        <div @click="consulter(mod.category, mod.id)" class="p-6 flex-1">
          <div class="flex justify-between items-start mb-4">
            <h3 class="text-base font-black text-slate-900 leading-tight group-hover:text-blue-600 transition-colors">{{ mod.libelle }}</h3>
            <div class="flex flex-col gap-1 items-end shrink-0">
              <span class="px-2 py-1 bg-slate-100 text-slate-500 text-[10px] font-bold uppercase rounded-md tracking-wider">v{{ mod.version }}</span>
              <span v-if="mod.statut === 'ARCHIVE'" class="px-2 py-0.5 bg-red-100 text-red-600 border border-red-200 text-[9px] font-black uppercase rounded-md tracking-widest shadow-sm">Archivé</span>
              <span v-else-if="mod.statut" class="text-[9px] font-bold text-emerald-500 uppercase tracking-widest">{{ mod.statut }}</span>
            </div>
          </div>

          <div class="flex flex-wrap gap-2 mb-6 mt-auto">
            <div v-if="mod.nature && mod.nature !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-box text-slate-400 text-[11px]"></i> {{ mod.nature }}
            </div>
            <div v-if="mod.type && mod.type !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-tag text-slate-400 text-[11px]"></i> {{ mod.type }}
            </div>
            <div v-if="mod.poste && mod.poste !== 'N/A'" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 text-slate-600 rounded-lg text-[10px] font-bold uppercase tracking-wide border border-slate-200/60 shadow-sm">
              <i class="pi pi-map-marker text-slate-400 text-[11px]"></i> {{ mod.poste }}
            </div>
          </div>

          <p class="text-xs text-slate-500 flex items-center gap-2">
            <i class="pi pi-list"></i> {{ mod.description }}
          </p>
        </div>

        <div class="p-3 bg-slate-50 border-t border-slate-100 flex gap-3" @click.stop>
          <template v-if="mod.statut !== 'ARCHIVE'">
            <button @click="editer(mod.category, mod.id)" class="flex-1 bg-slate-800 text-white py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-slate-900 transition-colors">
              <i class="pi pi-pencil"></i> Éditer
            </button>
            <button @click="confirmArchivage(mod)" class="w-10 shrink-0 bg-red-50 text-red-500 py-2 rounded-lg font-bold text-xs flex items-center justify-center hover:bg-red-500 hover:text-white border border-transparent hover:border-red-600 transition-colors" title="Archiver le modèle">
              <i class="pi pi-box"></i>
            </button>
          </template>

          <template v-else>
            <button @click="editer(mod.category, mod.id)" class="w-full bg-amber-100 text-amber-700 py-2 rounded-lg font-bold text-xs flex items-center justify-center gap-2 hover:bg-amber-200 transition-colors">
              <i class="pi pi-history"></i> Ouvrir pour Restauration
            </button>
          </template>
        </div>
      </div>

      <div v-if="modelesFiltres.length === 0" class="col-span-full py-12 text-center bg-slate-50 rounded-2xl border-2 border-dashed border-slate-200">
        <i class="pi pi-inbox text-4xl text-slate-300 mb-3"></i>
        <h3 class="text-lg font-bold text-slate-700">Aucun modèle {{ vueActuelle === 'ARCHIVES' ? 'archivé' : 'trouvé' }}</h3>
        <p class="text-sm text-slate-500 mt-1">Modifiez vos filtres ou créez un nouveau modèle dans cette catégorie.</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import ConfirmDialog from 'primevue/confirmdialog';
import apiClient from '@/services/apiClient';

const router = useRouter();
const toast = useToast();
const confirm = useConfirm();

const filtreActif = ref('ALL');
const operationFiltre = ref('');
const vueActuelle = ref('ACTIFS');
const isLoading = ref(true);
const modeles = ref([]);

const categories = [
  { id: 'ALL', label: 'Tous les modèles' },
  { id: 'FAB', label: 'Fabrication' },
  { id: 'ASS', label: 'Assemblage' },
  { id: 'VM', label: 'Vérif Machine' },
  { id: 'ECH', label: 'Échantillonnage' }
];

onMounted(async () => {
  await chargerModeles();
});

const chargerModeles = async () => {
  try {
    isLoading.value = true;
    const response = await apiClient.get('/hub/modeles');
    modeles.value = response.data.data || response.data || [];
  } catch (error) {
    console.error(error);
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les modèles', life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

const modelesFiltres = computed(() => {
  return modeles.value.filter(m => {
    const matchCategory = filtreActif.value === 'ALL' || m.category === filtreActif.value;
    const matchStatut = vueActuelle.value === 'ACTIFS' ? m.statut !== 'ARCHIVE' : m.statut === 'ARCHIVE';
    const matchOperation = operationFiltre.value === '' || m.poste === operationFiltre.value;
    return matchCategory && matchStatut && matchOperation;
  });
});

const operationsDisponibles = computed(() => {
  const ops = new Set();
  modeles.value.forEach(m => {
    const matchCategory = filtreActif.value === 'ALL' || m.category === filtreActif.value;
    if (matchCategory && m.poste && m.poste !== 'N/A') {
      ops.add(m.poste);
    }
  });
  return Array.from(ops).sort();
});

watch(filtreActif, () => {
  operationFiltre.value = '';
});

const getHoverBorderColor = (category) => {
  const colors = {
    FAB: 'hover:border-blue-400',
    ASS: 'hover:border-indigo-400',
    VM: 'hover:border-orange-400',
    ECH: 'hover:border-purple-400'
  };
  return colors[category] || 'hover:border-slate-400';
};

const confirmArchivage = (mod) => {
  confirm.require({
    message: `Voulez-vous vraiment archiver le modèle "${mod.libelle}" ?`,
    header: 'Confirmation d\'archivage',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Annuler',
    acceptLabel: 'Archiver',
    rejectClass: 'p-button-secondary p-button-outlined',
    acceptClass: 'p-button-danger',
    accept: async () => {
      await archiver(mod);
    }
  });
};

const archiver = async (mod) => {
  try {
    isLoading.value = true;
    await apiClient.put(`/hub/modeles/${mod.category}/${mod.id}/statut?statut=ARCHIVE`);
    mod.statut = 'ARCHIVE';
    toast.add({ severity: 'success', summary: 'Archivé', detail: 'Le modèle a été placé dans les archives.', life: 3000 });
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: "L'archivage a échoué.", life: 3000 });
  } finally {
    isLoading.value = false;
  }
};

// ⚠️ MODIFIÉ : Édition normale (Sans query parameter)
const editer = (category, id) => {
  switch (category) {
    case 'FAB':
      router.push(`/dev/fab/editer/${id}`);
      break;
    case 'ASS':
      router.push(`/dev/assemblage/editer/${id}`);
      break;
    case 'VM':
      router.push(`/dev/verif-machine/editer/${id}`);
      break;
    case 'ECH':
      router.push(`/dev/echantillonnage/editer/${id}`);
      break;
    default:
      toast.add({ severity: 'warn', summary: 'Catégorie inconnue', detail: 'Redirection non disponible.', life: 3000 });
  }
};

// ⚠️ NOUVEAU : Fonction de consultation (AJOUTE LE ?view=true DANS L'URL)
const consulter = (category, id) => {
  switch (category) {
    case 'FAB':
      router.push({ path: `/dev/fab/editer/${id}`, query: { view: 'true' } });
      break;
    case 'ASS':
      router.push({ path: `/dev/assemblage/editer/${id}`, query: { view: 'true' } });
      break;
    case 'VM':
      router.push({ path: `/dev/verif-machine/editer/${id}`, query: { view: 'true' } });
      break;
    case 'ECH':
      router.push({ path: `/dev/echantillonnage/editer/${id}`, query: { view: 'true' } });
      break;
  }
};
</script>