<template>
  <div class="mb-10">
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

watch(() => store.entete.formulaireId, (newRefId) => {
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
