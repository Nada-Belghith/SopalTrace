<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Opération</label>
        <select v-model="store.entete.operationCode" class="w-full bg-white border border-slate-300 rounded px-3 py-2 text-sm font-semibold text-slate-800 outline-none focus:border-blue-500 transition-shadow cursor-pointer">
          <option value="">-- Toutes les opérations --</option>
          <option v-for="op in (store.operations || [])" :key="op.code" :value="op.code">{{ op.code }} - {{ op.libelle }}</option>
        </select>
      </div>
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Composant *</label>
        <select v-model="store.entete.natureComposantCode" class="w-full bg-white border border-slate-300 rounded px-3 py-2 text-sm font-semibold text-slate-800 outline-none focus:border-blue-500 transition-shadow cursor-pointer">
          <option value="" disabled>--Sélectionner--</option>
          <option v-for="nat in (store.naturesComposant || [])" :key="nat.code" :value="nat.code">{{ nat.libelle }}</option>
        </select>
      </div>
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Type Robinet *</label>
        <select v-model="store.entete.typeRobinetCode" class="w-full bg-white border border-slate-300 rounded px-3 py-2 text-sm font-semibold text-slate-800 outline-none focus:border-blue-500 transition-shadow cursor-pointer">
          <option value="" disabled>--Sélectionner--</option>
          <option v-for="typ in (store.typesRobinet || [])" :key="typ.code" :value="typ.code">{{ typ.libelle }}</option>
        </select>
      </div>
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Libellé du Gabarit *</label>
        <input v-model="store.entete.libelle" type="text" placeholder="Ex: Modèle Standard Corps..." class="w-full bg-white border border-slate-300 rounded px-3 py-2 text-sm font-semibold text-slate-800 outline-none focus:border-blue-500 transition-shadow">
      </div>
    </div>
  </div>
</template>

<script setup>
import { useFabModeleStore } from '@/stores/fabModeleStore';

const store = useFabModeleStore();
</script>