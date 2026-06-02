<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
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

      <!-- CONFIGURATION COLONNES -->
      <div v-if="!isReadOnly" class="flex justify-end mt-4">
        <button @click="showColumnModal = true" class="bg-slate-800 hover:bg-slate-700 text-white px-4 py-2 rounded font-bold text-sm flex items-center gap-2 transition-colors">
          <i class="pi pi-sliders-h"></i> Configurer Colonnes
        </button>
      </div>

    </div>

    <!-- MODAL DE CONFIGURATION DES COLONNES -->
    <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { usePfPlanStore } from '@/stores/pfPlanStore';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';

const store = usePfPlanStore();
const showColumnModal = ref(false);
defineProps({
  isReadOnly: {
    type: Boolean,
    default: false
  }
});
</script>
