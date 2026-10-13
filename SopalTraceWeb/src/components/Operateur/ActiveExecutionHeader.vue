<template>
  <EnTetePage class="mb-6 pb-4 border-b border-slate-100"
              sur-titre="Fabrication · Ordre de fabrication"
              :titre="activeOfContext.numeroOf || '—'"
              :complement="activeOfContext.operationCode"
              retour libelle-retour="Retour à la liste des OF" @retour="$emit('retour')">
    <template #sous-titre>
      Machine <strong class="text-slate-700">{{ activeOfContext.machineCode || '—' }}</strong>
      <span class="mx-2 text-slate-300">|</span> Statut
      <strong :class="{ 'text-amber-600': enReglage, 'text-emerald-600': activeOfContext.statut === 'EN_COURS' && !enReglage, 'text-slate-700': activeOfContext.statut === 'EN_PAUSE' }">
        {{ libelleStatut }}
      </strong>
    </template>

    <template #actions>
      <button type="button" @click="$emit('rafraichir')"
              class="min-h-[48px] px-4 rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 font-semibold cursor-pointer">
        <i class="pi pi-refresh mr-2"></i>Rafraîchir
      </button>
      <button v-if="activeOfContext.statut === 'EN_PAUSE' || enReglage" type="button" @click="$emit('reprendre')"
              class="min-h-[48px] px-5 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold border-0 cursor-pointer">
        <i class="pi pi-play mr-2"></i>Reprendre la production
      </button>
      <button v-if="activeOfContext.statut === 'EN_COURS' && activeOfContext.a_Des_Controles_Reglage && !activeOfContext.estEnReglage" type="button" @click="$emit('mettre-en-reglage')"
              class="min-h-[48px] px-5 rounded-xl bg-amber-500 hover:bg-amber-600 text-white font-bold border-0 cursor-pointer">
        <i class="pi pi-cog mr-2"></i>Mettre aux réglages
      </button>
      <button v-if="enReglage" type="button" @click="$emit('mettre-en-reglage')"
              class="min-h-[48px] px-5 rounded-xl bg-amber-600 hover:bg-amber-700 text-white font-bold border-0 cursor-pointer">
        <i class="pi pi-replay mr-2"></i>Nouveau réglage
      </button>
      <button v-if="activeOfContext.statut === 'EN_COURS' && !activeOfContext.estEnReglage" type="button" @click="$emit('mettre-en-pause')"
              class="min-h-[48px] px-5 rounded-xl bg-slate-600 hover:bg-slate-700 text-white font-bold border-0 cursor-pointer">
        <i class="pi pi-pause mr-2"></i>Pause
      </button>
      <button type="button" @click="$emit('cloturer')"
              class="min-h-[48px] px-5 rounded-xl bg-slate-900 hover:bg-black text-white font-bold border-0 cursor-pointer">
        <i class="pi pi-stop-circle mr-2"></i>Clôturer l'opération
      </button>
    </template>
  </EnTetePage>
</template>

<script setup>
import { computed } from 'vue';
import EnTetePage from '@/components/Shared/EnTetePage.vue';

const props = defineProps({
  activeOfContext: {
    type: Object,
    required: true
  }
});

defineEmits(['retour', 'rafraichir', 'reprendre', 'mettre-en-reglage', 'mettre-en-pause', 'cloturer']);

const LIBELLES_STATUT = { EN_COURS: 'En cours', EN_PAUSE: 'En pause', REGLAGE: 'En réglage', CLOTURE: 'Clôturé' };

const enReglage = computed(() => props.activeOfContext.statut === 'REGLAGE' || props.activeOfContext.estEnReglage);
const libelleStatut = computed(() => enReglage.value
  ? LIBELLES_STATUT.REGLAGE
  : LIBELLES_STATUT[props.activeOfContext.statut] ?? props.activeOfContext.statut ?? '—');
</script>
