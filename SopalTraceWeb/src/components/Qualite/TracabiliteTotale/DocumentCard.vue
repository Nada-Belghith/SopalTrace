<template>
  <div 
    class="relative overflow-hidden rounded-xl border p-4 transition-all duration-300 cursor-pointer shadow-sm hover:shadow-md hover:-translate-y-1 bg-white"
    :class="[
      status === 'disponible' ? 'border-emerald-200 hover:border-emerald-400' : 'border-red-200 hover:border-red-400',
    ]"
    @click="$emit('click', { title, status, icon, imagePath })"
  >
    <!-- Background Gradient for visual flair -->
    <div class="absolute -right-10 -top-10 w-32 h-32 rounded-full opacity-10 blur-2xl pointer-events-none"
         :class="status === 'disponible' ? 'bg-emerald-500' : 'bg-red-500'"></div>

    <div class="flex items-start gap-4 relative z-10">
      <div 
        class="w-12 h-12 rounded-lg flex items-center justify-center shrink-0 shadow-sm"
        :class="iconBgClass"
      >
        <i :class="[icon, 'text-xl', iconColorClass]"></i>
      </div>
      <div class="flex-1 min-w-0">
        <div class="flex items-center justify-between gap-2 mb-1">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500">{{ category }}</span>
          <span 
            class="text-[10px] font-black uppercase tracking-wider px-2 py-0.5 rounded-full"
            :class="status === 'disponible' ? 'bg-emerald-100 text-emerald-700' : 'bg-red-100 text-red-700'"
          >
            <i :class="status === 'disponible' ? 'pi pi-check-circle mr-1' : 'pi pi-exclamation-triangle mr-1'"></i>
            {{ status === 'disponible' ? 'Disponible' : 'Manquant' }}
          </span>
        </div>
        <h4 class="text-sm font-bold text-slate-800 m-0 truncate" :title="title">{{ title }}</h4>
        <p v-if="subtitle" class="text-xs text-slate-500 m-0 mt-1 truncate" :title="subtitle">{{ subtitle }}</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  category: { type: String, required: true },
  status: { type: String, default: 'disponible' }, // 'disponible' | 'manquant'
  icon: { type: String, default: 'pi pi-file' },
  imagePath: { type: String, default: '' }
});

defineEmits(['click']);

const iconBgClass = computed(() => {
  return props.status === 'disponible' ? 'bg-emerald-50 border border-emerald-100' : 'bg-red-50 border border-red-100';
});

const iconColorClass = computed(() => {
  return props.status === 'disponible' ? 'text-emerald-600' : 'text-red-500';
});
</script>
