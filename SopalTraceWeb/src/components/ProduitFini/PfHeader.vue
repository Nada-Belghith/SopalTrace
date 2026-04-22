<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      
      <!-- TYPE ROBINET (Obligatoire) -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Type Robinet *</label>
        <select v-model="store.entete.typeRobinetCode" :disabled="isReadOnly" :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', isReadOnly ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">       
          <option value="">-- Sélectionner le type --</option>
          <option v-for="typ in (store.typesRobinet || [])" :key="typ.code" :value="typ.code">{{ typ.libelle }}</option>
        </select>
      </div>

      <!-- LIBELLÉ (Optionnel) -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Désignation (Optionnel)</label>
        <input v-model="store.entete.designation" type="text" placeholder="Ex: Produit Fini Standard..." :disabled="isReadOnly" :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-300 text-slate-800']">
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
