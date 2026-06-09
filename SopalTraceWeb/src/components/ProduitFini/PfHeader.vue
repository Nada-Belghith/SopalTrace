<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    <!-- NOUVEAU DESIGN POUR REF FORMULAIRE -->
    <div v-if="!isReadOnly && !isEditMode" class="bg-blue-50 border border-blue-100 p-4 rounded-xl flex flex-col md:flex-row gap-4 mb-6 shadow-inner">
      <div class="flex items-center text-blue-800 font-black tracking-widest text-xs min-w-[150px]">
        <i class="pi pi-file-import mr-2 text-lg text-blue-600"></i> RÉF. FORMULAIRE *
      </div>
      <div class="flex-1 flex gap-4 items-center relative">
        <select 
          v-model="refFormulaireSelected" 
          class="w-full md:w-1/2 rounded-lg px-4 py-2 text-sm font-bold shadow-sm focus:ring-2 focus:ring-blue-400 outline-none transition-shadow bg-white border border-blue-200 text-blue-900 cursor-pointer">
          <option value="">-- Choisir un formulaire générique --</option>
          <option v-for="ref in store.formulairesReferences" :key="ref.id" :value="ref.id">
            {{ ref.codeReference }} - {{ ref.designation }}
          </option>
        </select>
        <span class="text-xs font-bold text-blue-500 italic hidden md:block">
          La sélection du formulaire remplira automatiquement les champs suivants.
        </span>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-1 gap-8 bg-slate-50/50 p-6 rounded-xl border border-slate-100">
      
      <!-- FAMILLE PRODUIT (Obligatoire) -->
      <div>
        <label class="block text-[10px] font-black text-slate-500 uppercase tracking-[0.1em] mb-2">Famille de Produit Fini</label>
        <div v-if="isReadOnly" class="text-sm font-black text-blue-900 bg-blue-50/50 px-4 py-2.5 rounded-lg border border-blue-100/50 flex items-center gap-2">
           <i class="pi pi-box text-blue-400"></i>
           {{ store.entete.familleProduitFiniCode || 'Non spécifié' }}
        </div>
        <select v-else v-model="store.entete.familleProduitFiniCode" 
                class="w-full bg-white border-2 border-slate-200 rounded-xl px-4 py-2.5 text-sm font-bold text-slate-800 outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-400/10 transition-all cursor-pointer">
          <option value="">-- Sélectionner la famille --</option>
          <option v-for="fam in (store.famillesProduit || [])" :key="fam.code" :value="fam.code">{{ fam.code }}</option>
        </select>
      </div>

      <!-- VERSION INITIALE -->
      <div v-if="!isEditMode && !hasExistingVersion">
        <label class="block text-[10px] font-black text-slate-500 uppercase tracking-[0.1em] mb-2">Version de départ</label>
        <input v-model.number="store.entete.versionInitiale" type="number" min="0" placeholder="0" :disabled="isReadOnly"
          class="w-full bg-white border-2 border-slate-200 rounded-xl px-4 py-2.5 text-sm font-bold text-slate-800 outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-400/10 transition-all">
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch, computed } from 'vue';
import { usePfPlanStore } from '@/stores/pfPlanStore';
import { parseDesignation } from '@/utils/designationParser';

const store = usePfPlanStore();
const refFormulaireSelected = ref('');

const hasExistingVersion = computed(() => {
  if (!refFormulaireSelected.value) return false;
  const refObj = store.formulairesReferences.find(r => r.id === refFormulaireSelected.value);
  if (!refObj) return false;
  
  const hasVersion = refObj.version > 0 || refObj.Version > 0;
  const hasConfig = refObj.configurationStructureJson !== null && refObj.configurationStructureJson !== undefined;
  
  return hasVersion || hasConfig;
});

const props = defineProps({
  isReadOnly: {
    type: Boolean,
    default: false
  },
  isEditMode: {
    type: Boolean,
    default: false
  }
});

onMounted(() => {
  if (!props.isReadOnly && !props.isEditMode) {
    store.fetchFormulairesReferences('PRODUIT_FINI');
  }
});

watch(refFormulaireSelected, (newRefId) => {
  if (!newRefId) return;
  const refObj = store.formulairesReferences.find(r => r.id === newRefId);
  if (!refObj) return;

  const designation = refObj.designation || '';
  const parsed = parseDesignation(designation, store.famillesProduit || []);
  
  if (parsed.familleCode) {
    store.entete.familleProduitFiniCode = parsed.familleCode;
  }
  
  store.entete.refFormulaireCodeReference = refObj.codeReference;

  // Appliquer la configuration des colonnes du formulaire sélectionné
  if (refObj.configurationStructureJson) {
    try {
      store.entete.configurationColonnes = typeof refObj.configurationStructureJson === 'string' 
        ? JSON.parse(refObj.configurationStructureJson) 
        : refObj.configurationStructureJson;
    } catch (e) {
      console.error("Erreur parsing configuration colonnes:", e);
      store.entete.configurationColonnes = [];
    }
  } else {
    store.entete.configurationColonnes = [];
  }
});
</script>
