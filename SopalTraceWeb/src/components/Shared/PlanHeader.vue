<template>
  <header class="bg-white min-h-[4rem] shadow-sm border border-slate-200 flex flex-col md:flex-row md:items-center justify-between px-6 py-3 mb-6 rounded-xl gap-4">
    <div class="flex items-center gap-4">
      <button @click="router.back()" class="p-2 hover:bg-slate-100 rounded-lg text-slate-400 hover:text-slate-600 transition-colors flex-shrink-0" title="Retour">
        <i class="pi pi-arrow-left text-lg"></i>
      </button>
      
      <div>
        <h2 class="text-slate-800 font-black tracking-wide text-base md:text-lg flex items-center gap-2">
          <i :class="[icon, iconColorClass]"></i>
          <span>
            <template v-if="isReadOnly">Consultation : </template>
            <template v-else-if="!id || id === 'nouveau' || statut === 'BROUILLON'">Création : </template>
            <template v-else>Édition : </template>
            {{ title }}
          </span>
        </h2>
        <p v-if="subtitle" class="text-xs font-medium text-slate-500 mt-0.5 ml-7">{{ subtitle }}</p>
      </div>
    </div>

    <div class="flex flex-wrap items-center gap-3">
      <template v-if="id">
        <span class="text-[10px] font-black uppercase tracking-widest border px-3 py-1 rounded-full shadow-sm" 
              :class="statut === 'ACTIF' ? 'text-emerald-700 border-emerald-200 bg-emerald-100' : 
                      statut === 'BROUILLON' ? 'text-amber-700 border-amber-200 bg-amber-100' : 
                      statut === 'ARCHIVE' ? 'text-slate-500 border-slate-300 bg-slate-200 grayscale' :
                      'text-slate-600 border-slate-200 bg-slate-100'">
          {{ statut || 'NOUVEAU' }}
        </span>
      </template>

      <template v-if="showRestaurerBtn && statut === 'ARCHIVE'">
        <button 
          @click="$emit('restaurer')"
          :disabled="isRestoring"
          class="flex items-center gap-2 bg-white border-2 border-blue-500 text-blue-600 px-4 py-1.5 rounded-lg font-bold text-xs hover:bg-blue-50 transition-colors shadow-sm disabled:opacity-50 disabled:cursor-not-allowed">
          <i class="pi pi-sync" :class="{ 'animate-spin': isRestoring }"></i>
          {{ isRestoring ? 'Mise à niveau...' : 'Mettre à niveau' }}
        </button>
      </template>

      <slot name="actions"></slot>
    </div>
  </header>
</template>

<script setup>
import { useRouter } from 'vue-router';

const router = useRouter();

defineProps({
  id: { type: [String, Number], default: null },
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  icon: { type: String, default: 'pi pi-file' },
  iconColorClass: { type: String, default: 'text-blue-500' },
  isReadOnly: { type: Boolean, default: false },
  version: { type: [String, Number], default: null },
  statut: { type: String, default: 'NOUVEAU' },
  isRestoring: { type: Boolean, default: false },
  showRestaurerBtn: { type: Boolean, default: true }
});

defineEmits(['restaurer']);
// Composant d'en-tête partagé pour les plans de qualité
</script>
