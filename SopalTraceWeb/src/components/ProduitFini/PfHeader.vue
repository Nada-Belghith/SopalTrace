<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-8 bg-slate-50/50 p-6 rounded-xl border border-slate-100">
      
      <!-- TYPE ROBINET (Obligatoire) -->
      <div>
        <label class="block text-[10px] font-black text-slate-500 uppercase tracking-[0.1em] mb-2">Type Robinet</label>
        <div v-if="isReadOnly" class="text-sm font-black text-blue-900 bg-blue-50/50 px-4 py-2.5 rounded-lg border border-blue-100/50 flex items-center gap-2">
           <i class="pi pi-box text-blue-400"></i>
           {{ store.typesRobinet.find(t => t.code === store.entete.typeRobinetCode)?.libelle || store.entete.typeRobinetCode || 'Non spécifié' }}
        </div>
        <select v-else v-model="store.entete.typeRobinetCode" 
                class="w-full bg-white border-2 border-slate-200 rounded-xl px-4 py-2.5 text-sm font-bold text-slate-800 outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-400/10 transition-all cursor-pointer">
          <option value="">-- Sélectionner le type --</option>
          <option v-for="typ in (store.typesRobinet || [])" :key="typ.code" :value="typ.code">{{ typ.libelle }}</option>
        </select>
      </div>

      <!-- LIBELLÉ (Optionnel) -->
      <div>
        <label class="block text-[10px] font-black text-slate-500 uppercase tracking-[0.1em] mb-2">Désignation du Plan</label>
        <div v-if="isReadOnly" class="text-sm font-bold text-slate-700 bg-white px-4 py-2.5 rounded-lg border border-slate-200 shadow-sm flex items-center gap-2">
           <i class="pi pi-info-circle text-slate-400"></i>
           {{ store.entete.designation || 'Aucune désignation' }}
        </div>
        <input v-else v-model="store.entete.designation" type="text" placeholder="Ex: Produit Fini Standard..." 
               class="w-full bg-white border-2 border-slate-200 rounded-xl px-4 py-2.5 text-sm font-bold text-slate-800 outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-400/10 transition-all">
      </div>

    </div>
  </div>
</template>

<script setup>
import { usePfPlanStore } from '@/stores/pfPlanStore';

const store = usePfPlanStore();
defineProps({
  isReadOnly: {
    type: Boolean,
    default: false
  }
});
</script>
