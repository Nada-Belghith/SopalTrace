<template>
  <div class="space-y-6">
    <!-- ============================================================ -->
    <!-- ÉTAPE 1 : SÉLECTION MACHINE                                   -->
    <!-- ============================================================ -->
    <section v-if="!isReadOnly" class="bg-white rounded-xl shadow-sm border border-slate-200 p-5">
      <div class="flex flex-col md:flex-row md:justify-between md:items-center gap-4 mb-4">
        <h2 class="text-sm font-bold text-slate-700 flex items-center gap-2 uppercase tracking-wide">
          <i class="ri-map-pin-line text-slate-500"></i> 1. Informations générales & Machine Concernée
        </h2>

        <!-- ACTIONS -->
        <div class="flex items-center gap-3">
          <!-- Import Excel -->
          <div v-if="!isReadOnly && !store.entete.id" class="flex-shrink-0">
            <input type="file" ref="fileInput" @change="onFileChange" class="hidden" accept=".xlsx, .xls">
            <button @click="$refs.fileInput.click()" 
              class="h-[38px] px-4 flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-bold transition-colors shadow-sm"
              :disabled="store.isLoading">
              <i v-if="!store.isLoading" class="ri-file-excel-2-line text-lg"></i>
              <i v-else class="ri-loader-4-line animate-spin text-lg"></i>
              Importer la structure Excel
            </button>
          </div>

          <!-- Configurer Colonnes -->
          <button v-if="!isReadOnly && store.planInitialise" @click="$emit('configure-columns')" 
            class="h-[38px] px-4 flex items-center gap-2 bg-slate-900 hover:bg-slate-800 text-white rounded-lg text-xs font-bold transition-colors shadow-sm">
            <i class="pi pi-sliders-h"></i> Configurer Colonnes
          </button>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 items-end mb-4">
        <!-- CHOIX RÉFÉRENCE FORMULAIRE -->
        <div v-if="!isReadOnly && !store.entete.id" class="col-span-full mb-4 bg-blue-50/50 border border-blue-200 p-4 rounded-xl flex flex-col md:flex-row items-start md:items-center gap-4">
          <label class="block text-[11px] font-black text-blue-800 uppercase tracking-widest shrink-0">
            <i class="pi pi-file-import mr-1 text-blue-600"></i> Réf. Formulaire  *
          </label>
          <select 
            v-model="refFormulaireSelected" 
            class="w-full md:w-1/3 rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow bg-white border border-slate-300 text-slate-800 cursor-pointer shadow-sm">
            <option value="">-- Choisir un formulaire générique --</option>
            <option v-for="ref in store.formulairesReferences" :key="ref.id" :value="ref.id">
              {{ ref.codeReference }} - {{ ref.designation }}
            </option>
          </select>
          <p class="text-xs text-blue-600/80 font-medium italic">
            La sélection du formulaire remplira automatiquement la machine ciblée.
          </p>
        </div>
        <div class="flex-1">
          <label class="block text-xs font-bold text-slate-500 mb-1">Machine</label>
          <select v-model="selectedMachineCode" @change="onMachineChange" :disabled="isReadOnly || store.entete.id"
            class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-slate-500 focus:ring-1 focus:ring-slate-500 outline-none bg-slate-50 cursor-pointer disabled:opacity-75 disabled:bg-slate-100 font-semibold text-slate-700 transition-all">
            <option value="">-- Choisir une machine --</option>
            <option v-for="mac in store.machines" :key="mac.code" :value="mac.code">
              {{ mac.code }} - {{ mac.libelle }}
            </option>
          </select>
        </div>
      </div>
    </section>

    <!-- ============================================================ -->
    <!-- ÉTAPE 2 : CONFIGURATION DE LA MATRICE                        -->
    <!-- ============================================================ -->
    <section v-if="store.planInitialise && !isReadOnly" class="bg-white rounded-xl shadow-sm border border-slate-200 p-5 border-l-4 border-l-slate-900">
      <div class="flex justify-between items-start mb-4">
        <h2 class="text-sm font-bold text-slate-700 flex items-center gap-2 uppercase tracking-wide">
          <i class="ri-table-line text-slate-500"></i> 2. Configuration du Plan
        </h2>
      </div>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <label class="block text-xs font-bold text-slate-500 mb-1">Titre du Rapport</label>
          <input v-model="store.entete.nom" type="text" :disabled="isReadOnly" @blur="$emit('nom-blur')"
            class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-slate-900 font-semibold text-slate-800 outline-none disabled:bg-slate-50">
        </div>
        
        <div v-if="!hasExistingVersion && !store.entete.id">
          <label class="block text-xs font-bold text-slate-500 mb-1">Version de départ</label>
          <input v-model.number="store.entete.versionInitiale" type="number" min="0" placeholder="0" :disabled="isReadOnly"
            class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-slate-900 font-semibold text-slate-800 outline-none disabled:bg-slate-50">
          <p class="text-[10px] text-slate-400 mt-1 italic">Définit la version initiale de ce formulaire.</p>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import { useVerifMachineStore } from '@/stores/verifMachineStore';
import { parseDesignation } from '@/utils/designationParser';

defineProps({
  isReadOnly: { type: Boolean, default: false }
});

const emit = defineEmits(['import-excel', 'configure-columns', 'nom-blur', 'machine-changed']);

const store = useVerifMachineStore();
const fileInput = ref(null);
const refFormulaireSelected = ref('');
const selectedMachineCode = ref('');

const hasExistingVersion = computed(() => {
  if (!refFormulaireSelected.value) return false;
  const refObj = store.formulairesReferences.find(r => r.id === refFormulaireSelected.value);
  if (!refObj) return false;
  return refObj.version > 0 || refObj.Version > 0 || refObj.configurationStructureJson !== null;
});

// Synchroniser le code machine local avec le store
watch(() => store.entete.machineCode, (newVal) => {
  selectedMachineCode.value = newVal || '';
}, { immediate: true });

watch(refFormulaireSelected, async (newRefId) => {
  if (!newRefId) {
    store.entete.configurationColonnes = [];
    return;
  }
  const refObj = store.formulairesReferences.find(r => r.id === newRefId);
  if (!refObj) return;

  const designation = refObj.designation || '';
  const parsed = parseDesignation(designation, [], store.machines || []);

  if (parsed.machineCode) {
    selectedMachineCode.value = parsed.machineCode;
    await store.initialiserPlan(parsed.machineCode);
    store.entete.nom = designation;
  }

  if (refObj.configurationStructureJson) {
    try {
      store.entete.configurationColonnes = typeof refObj.configurationStructureJson === 'string' 
        ? JSON.parse(refObj.configurationStructureJson) 
        : refObj.configurationStructureJson;
    } catch {
      store.entete.configurationColonnes = [];
    }
  } else {
    store.entete.configurationColonnes = [];
  }
});

const onMachineChange = () => {
  emit('machine-changed', selectedMachineCode.value);
};

const onFileChange = (event) => {
  emit('import-excel', event);
};
</script>
