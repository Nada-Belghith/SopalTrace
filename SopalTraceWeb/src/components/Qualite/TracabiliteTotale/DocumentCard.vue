<template>
  <div
    class="relative overflow-hidden rounded-xl border p-4 transition-all duration-300 cursor-pointer shadow-sm hover:shadow-md hover:-translate-y-1 bg-white"
    :class="etat.bordure"
    @click="$emit('click', { title, status, icon, imagePath })"
  >
    <!-- Background Gradient for visual flair -->
    <div class="absolute -right-10 -top-10 w-32 h-32 rounded-full opacity-10 blur-2xl pointer-events-none" :class="etat.halo"></div>

    <div class="flex items-start gap-4 relative z-10">
      <div class="w-12 h-12 rounded-lg flex items-center justify-center shrink-0 shadow-sm" :class="etat.fondIcone">
        <i :class="[icon, 'text-xl', etat.couleurIcone]"></i>
      </div>
      <div class="flex-1 min-w-0">
        <div class="flex items-center justify-between gap-2 mb-1">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500 truncate">{{ category }}</span>
          <span class="shrink-0 text-[10px] font-black uppercase tracking-wider px-2 py-0.5 rounded-full" :class="etat.badge">
            <i :class="['pi mr-1', etat.iconeBadge]"></i>{{ etat.libelle }}
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
  // 'disponible' | 'manquant' (document introuvable) | 'sans-modele' (document présent, pas encore de modèle Excel)
  status: { type: String, default: 'disponible' },
  icon: { type: String, default: 'pi pi-file' },
  imagePath: { type: String, default: '' }
});

defineEmits(['click']);

const ETATS = {
  disponible: {
    libelle: 'Disponible', iconeBadge: 'pi-check-circle', bordure: 'border-emerald-200 hover:border-emerald-400', halo: 'bg-emerald-500',
    fondIcone: 'bg-emerald-50 border border-emerald-100', couleurIcone: 'text-emerald-600', badge: 'bg-emerald-100 text-emerald-700'
  },
  'sans-modele': {
    libelle: 'Modèle manquant', iconeBadge: 'pi-file-excel', bordure: 'border-amber-200 hover:border-amber-400', halo: 'bg-amber-500',
    fondIcone: 'bg-amber-50 border border-amber-100', couleurIcone: 'text-amber-600', badge: 'bg-amber-100 text-amber-700'
  },
  manquant: {
    libelle: 'Manquant', iconeBadge: 'pi-exclamation-triangle', bordure: 'border-red-200 hover:border-red-400', halo: 'bg-red-500',
    fondIcone: 'bg-red-50 border border-red-100', couleurIcone: 'text-red-500', badge: 'bg-red-100 text-red-700'
  }
};

const etat = computed(() => ETATS[props.status] ?? ETATS.manquant);
</script>
