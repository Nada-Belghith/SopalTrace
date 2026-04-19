<template>
  <div class="flex items-center gap-2">
    <span 
      v-if="statut === 'BROUILLON'"
      class="px-4 py-1.5 bg-amber-50 text-amber-700 font-black text-[10px] uppercase rounded-lg border border-amber-200 tracking-widest flex items-center gap-2"
    >
      <i class="pi pi-pencil"></i> Brouillon
    </span>
    <span 
      v-else-if="statut === 'ACTIF'"
      class="px-4 py-1.5 bg-emerald-50 text-emerald-700 font-black text-[10px] uppercase rounded-lg border border-emerald-200 tracking-widest flex items-center gap-2"
    >
      <i class="pi pi-check-circle"></i> Actif
    </span>
    <span 
      v-else-if="statut === 'ARCHIVE'"
      class="px-4 py-1.5 bg-slate-100 text-slate-600 font-black text-[10px] uppercase rounded-lg border border-slate-200 tracking-widest flex items-center gap-2"
    >
      <i class="pi pi-inbox"></i> Archive
    </span>
    
    <div v-if="showVersion" class="px-4 py-1.5 bg-slate-100 text-slate-600 font-black text-[10px] uppercase rounded-lg border border-slate-200 tracking-widest">
      VERSION {{ version }}
    </div>
  </div>
</template>

<script setup>
defineProps({
  statut: {
    type: String,
    enum: ['BROUILLON', 'ACTIF', 'ARCHIVE'],
    required: true
  },
  version: {
    type: [Number, String],
    default: 1
  },
  showVersion: {
    type: Boolean,
    default: true
  }
});
</script>
