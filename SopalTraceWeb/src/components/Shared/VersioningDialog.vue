<template>
  <!-- Assurez-vous que votre composant ConfirmationDialog accepte bien une prop de largeur (ex: style="{width: '40rem'}") -->
  <ConfirmationDialog
    :visible="visible"
    :title="titleDialog"
    :description="descriptionDialog"
    :confirm-label="confirmLabelDialog"
    :confirm-icon="confirmIconDialog"
    :icon-class="iconClassDialog"
    :motif-label="motifLabelDialog"
    :motif-placeholder="placeholderDialog"
    :is-loading="isLoading"
    @confirm="onConfirm"
    @cancel="onCancel"
    @hide="$emit('update:visible', false)"
  />
</template>

<script setup>
import { computed } from 'vue';
import ConfirmationDialog from './ConfirmationDialog.vue';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  mode: {
    type: String,
    enum: ['new-version', 'restore'],
    default: 'new-version'
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['confirm', 'cancel', 'update:visible']);

const titleDialog = computed(() => {
  return props.mode === 'new-version' 
    ? 'Créer une nouvelle version du plan' 
    : 'Confirmer la Restauration';
});

const descriptionDialog = computed(() => {
  if (props.mode === 'new-version') {
    return 'Ce plan est actuellement ACTIF. Toute modification entraînera l\'archivage de la version actuelle et la publication d\'une nouvelle version de production.';
  }
  return 'Cette action restaurera cette archive comme nouvelle version en production.';
});

const confirmLabelDialog = computed(() => {
  return props.mode === 'new-version' ? 'Publier la Nouvelle Version' : 'Restaurer ce Modèle';
});

const confirmIconDialog = computed(() => {
  return props.mode === 'new-version' ? 'pi pi-arrow-right' : 'pi pi-history';
});

const iconClassDialog = computed(() => {
  if (props.mode === 'new-version') return 'pi pi-arrow-right text-blue-600 text-xl';
  return 'pi pi-history text-amber-600 text-xl';
});

const motifLabelDialog = computed(() => {
  return props.mode === 'new-version' 
    ? 'Justification de la modification (ISO 9001)' 
    : 'Motif de la restauration';
});

const placeholderDialog = computed(() => {
  if (props.mode === 'new-version') {
    return 'Ex: Demande client, Tolérances resserrées, Amélioration process...';
  }
  return 'Ex: Restauration suite à erreur, Retour version précédente...';
});

const onConfirm = (motif) => {
  emit('confirm', motif);
};

const onCancel = () => {
  emit('cancel');
};
</script>