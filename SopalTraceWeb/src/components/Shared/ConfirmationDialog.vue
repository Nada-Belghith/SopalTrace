<template>
  <Dialog v-model:visible="isVisible" modal :style="{ width: '500px' }" @hide="onHide">
    <template #header>
      <div class="flex items-center gap-3">
        <i :class="[iconClass, 'text-xl']"></i>
        <span>{{ title }}</span>
      </div>
    </template>

    <div class="flex flex-col gap-4">
      <p v-if="description" class="text-slate-600 text-sm">{{ description }}</p>
      
      <div v-if="showMotif">
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-2 tracking-widest">
          {{ motifLabel }}
        </label>
        <Textarea
          v-model="motifValue"
          rows="4"
          :placeholder="motifPlaceholder"
          class="w-full border border-slate-300 rounded-lg p-3 outline-none focus:border-blue-500"
        />
      </div>
    </div>

    <template #footer>
      <Button label="Annuler" icon="pi pi-times" text @click="onCancel" class="p-button-secondary" />
      <Button 
        :label="confirmLabel"
        :icon="confirmIcon"
        @click="onConfirm"
        :loading="isLoading"
        class="p-button-primary"
      />
    </template>
  </Dialog>
</template>

<script setup>
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import Button from 'primevue/button';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  title: {
    type: String,
    required: true
  },
  description: {
    type: String,
    default: ''
  },
  confirmLabel: {
    type: String,
    default: 'Confirmer'
  },
  confirmIcon: {
    type: String,
    default: 'pi pi-check'
  },
  iconClass: {
    type: String,
    default: 'pi pi-info-circle text-blue-600'
  },
  showMotif: {
    type: Boolean,
    default: false
  },
  motifLabel: {
    type: String,
    default: 'Motif'
  },
  motifPlaceholder: {
    type: String,
    default: 'Veuillez justifier cette action...'
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['confirm', 'cancel', 'hide', 'update:visible']);

const isVisible = ref(false);
const motifValue = ref('');

watch(() => props.visible, (newVal) => {
  isVisible.value = newVal;
  if (!newVal) motifValue.value = '';
});

const onConfirm = () => {
  emit('confirm', motifValue.value);
};

const onCancel = () => {
  isVisible.value = false;
  emit('cancel');
};

const onHide = () => {
  emit('hide');
  emit('update:visible', false);
};
</script>
