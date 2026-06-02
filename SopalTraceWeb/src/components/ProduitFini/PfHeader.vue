<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    <div class="grid grid-cols-1 md:grid-cols-1 gap-8 bg-slate-50/50 p-6 rounded-xl border border-slate-100">
      
      <!-- CHOIX RÉFÉRENCE FORMULAIRE -->
      <div v-if="!isReadOnly && !isEditMode" class="col-span-full mb-4 bg-blue-50/50 border border-blue-200 p-4 rounded-xl flex flex-col md:flex-row items-start md:items-center gap-4">
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
          La sélection du formulaire remplira automatiquement les champs suivants.
        </p>
      </div>

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

    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue';
import { usePfPlanStore } from '@/stores/pfPlanStore';
import { parseDesignation } from '@/utils/designationParser';

const store = usePfPlanStore();
const refFormulaireSelected = ref('');

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
});
</script>
