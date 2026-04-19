<template>
  <div class="flex justify-end items-center gap-4">
    <button
      @click="$emit('cancel')"
      class="px-8 py-3 bg-white border border-slate-300 text-slate-700 rounded-xl text-sm font-black uppercase tracking-widest hover:bg-slate-100 transition-colors shadow-sm"
    >
      Annuler
    </button>

    <button
      @click="$emit('submit')"
      :disabled="isLoading || isDisabled"
      :class="[
        buttonColorClass,
        'px-10 py-3 text-white rounded-xl text-sm font-black uppercase tracking-widest transition-all shadow-lg flex items-center gap-3 disabled:opacity-50 disabled:cursor-not-allowed'
      ]"
    >
      <i v-if="isLoading" class="pi pi-spin pi-spinner"></i>
      <i v-else :class="icon"></i>
      {{ isLoading ? loadingLabel : label }}
    </button>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  label: {
    type: String,
    required: true
  },
  loadingLabel: {
    type: String,
    default: 'Traitement...'
  },
  icon: {
    type: String,
    default: 'pi pi-check'
  },
  variant: {
    type: String,
    enum: ['primary', 'success', 'warning', 'danger'],
    default: 'primary'
  },
  isLoading: {
    type: Boolean,
    default: false
  },
  isDisabled: {
    type: Boolean,
    default: false
  }
});

defineEmits(['submit', 'cancel']);

const buttonColorClass = computed(() => {
  switch (props.variant) {
    case 'success':
      return 'bg-emerald-600 hover:bg-emerald-700';
    case 'warning':
      return 'bg-amber-600 hover:bg-amber-700';
    case 'danger':
      return 'bg-red-600 hover:bg-red-700';
    default:
      return 'bg-slate-800 hover:bg-slate-900';
  }
});
</script>
