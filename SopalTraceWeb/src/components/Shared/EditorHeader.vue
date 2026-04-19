<template>
  <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
    <div>
      <h1 class="text-2xl font-black text-slate-900 tracking-tight flex items-center gap-3">
        {{ title }}
        
      </h1>
      <p class="text-sm text-slate-500 mt-1">{{ subtitle }}</p>
      <div class="mt-2 flex items-center gap-4 text-sm text-slate-600">
        <div class="flex items-center gap-2">
          <span class="text-[10px] font-black uppercase tracking-widest text-slate-400">Statut</span>
          <span :class="['px-3 py-1 rounded-full font-black text-sm border', codeClass]">{{ statut || 'NOUVEAU' }}</span>
        </div>
      </div>
    </div>
    <div class="flex items-center gap-3 bg-white p-2 rounded-xl shadow-sm border border-slate-200">
      <span class="text-[10px] font-black text-slate-400 uppercase px-2">
        {{ codeLabel }}:
      </span>
      <div 
        class="px-4 py-1.5 font-mono font-bold rounded-lg border"
        :class="codeClass"
      >
        {{ code }}
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  title: {
    type: String,
    required: true
  },
  subtitle: {
    type: String,
    required: true
  },
  code: {
    type: String,
    required: true
  },
  codeLabel: {
    type: String,
    default: 'Code'
  },
  operation: {
    type: String,
    default: ''
  },
  designation: {
    type: String,
    default: ''
  },
  statut: {
    type: String,
    enum: ['BROUILLON', 'ACTIF', 'ARCHIVE'],
    default: 'BROUILLON'
  },
  isEditMode: {
    type: Boolean,
    default: false
  }
});

const codeClass = computed(() => {
  if (props.statut === 'ARCHIVE') {
    return 'bg-amber-50 text-amber-700 border-amber-200';
  }
  if (props.isEditMode) {
    return 'bg-blue-50 text-blue-700 border-blue-200';
  }
  return 'bg-slate-50 text-slate-700 border-slate-200';
});
</script>
