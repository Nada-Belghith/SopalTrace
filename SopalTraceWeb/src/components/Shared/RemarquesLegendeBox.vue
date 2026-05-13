<template>
  <div v-if="!isReadOnly || remarques || legendeMoyens" 
       class="mt-6 bg-white border border-slate-200 rounded-xl shadow-sm overflow-hidden transition-all hover:shadow-md">
    <!-- Header -->
    <div class="bg-gradient-to-r from-slate-800 to-slate-900 px-5 py-2.5 flex items-center justify-between border-b border-slate-700/50">
      <div class="flex items-center gap-2.5">
        <div class="p-1.5 bg-slate-700/50 rounded-lg">
          <i class="pi pi-comment text-blue-400 text-xs"></i>
        </div>
        <div>
          <h3 class="text-[11px] font-black text-white uppercase tracking-[0.1em]">Notes & Légendes</h3>
        </div>
      </div>
      <div v-if="isReadOnly" class="px-2 py-0.5 bg-blue-500/10 border border-blue-500/20 rounded-full">
        <span class="text-[8px] font-black text-blue-400 uppercase tracking-widest">Consultation</span>
      </div>
    </div>

    <div class="p-4 grid grid-cols-1 md:grid-cols-2 gap-6 bg-slate-50/20">
      <!-- Remarques -->
      <div class="group">
        <label class="flex items-center gap-2 mb-2">
          <div class="w-1 h-3 bg-blue-500 rounded-full"></div>
          <span class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Remarques</span>
        </label>
        
        <div v-if="isReadOnly" 
             class="min-h-[4rem] w-full border border-slate-200 bg-white rounded-lg px-4 py-3 text-[12px] text-slate-600 italic whitespace-pre-wrap leading-relaxed shadow-sm">
          <template v-if="remarques">{{ remarques }}</template>
          <div v-else class="flex flex-col items-center justify-center h-full py-4 text-slate-400 opacity-60">
             <i class="pi pi-info-circle text-xl mb-2"></i>
             <span class="font-medium text-xs">Aucune remarque particulière.</span>
          </div>
        </div>
        <textarea
          v-else
          :value="remarques"
          @input="$emit('update:remarques', $event.target.value)"
          rows="3"
          placeholder="Remarques..."
          class="w-full border-2 border-slate-200 rounded-lg px-3 py-2 text-[12px] text-slate-700 resize-none focus:border-blue-400 outline-none transition-all"
        ></textarea>
      </div>

      <!-- Légende des instruments / moyens -->
      <div class="group">
        <label class="flex items-center gap-2 mb-2">
          <div class="w-1 h-3 bg-slate-300 rounded-full"></div>
          <span class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Légende Instruments</span>
        </label>

        <div v-if="isReadOnly" 
             class="min-h-[4rem] w-full border border-slate-200 bg-white rounded-lg px-4 py-3 text-[12px] text-slate-600 font-mono whitespace-pre-wrap leading-relaxed shadow-sm border-l-4 border-l-blue-500/20">
          <template v-if="legendeMoyens">{{ legendeMoyens }}</template>
          <div v-else class="flex flex-col items-center justify-center h-full py-4 text-slate-400 opacity-60">
             <i class="pi pi-info-circle text-lg mb-1 font-sans"></i>
             <span class="font-medium text-[10px] font-sans">Aucune légende.</span>
          </div>
        </div>
        <textarea
          v-else
          :value="legendeMoyens"
          @input="$emit('update:legendeMoyens', $event.target.value)"
          rows="3"
          placeholder="Ex: A = Pied à coulisse..."
          class="w-full border-2 border-slate-200 rounded-lg px-3 py-2 text-[12px] text-slate-700 resize-none outline-none transition-all font-mono"
        ></textarea>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  remarques: { type: String, default: '' },
  legendeMoyens: { type: String, default: '' },
  isReadOnly: { type: Boolean, default: false },
  showValidation: { type: Boolean, default: false },
  hasCustomInstruments: { type: Boolean, default: false }
});

defineEmits(['update:remarques', 'update:legendeMoyens']);
</script>

<style scoped>
@keyframes pulse-subtle {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.98; transform: scale(0.998); }
}
.animate-pulse-subtle {
  animation: pulse-subtle 3s infinite ease-in-out;
}
</style>
