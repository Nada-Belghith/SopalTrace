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
    :show-motif="false"
    @confirm="onConfirm"
    @cancel="onCancel"
    @hide="$emit('update:visible', false)"
  >
    <!-- Options de mise à jour pour les documents ACTIFS -->
    <div v-if="mode === 'new-version'" class="flex flex-col gap-3 my-2 bg-slate-50 p-4 rounded-lg border border-slate-200">
      <div class="flex items-start gap-2">
        <RadioButton v-model="updateAction" inputId="action1" name="action" value="correction" />
        <label for="action1" class="cursor-pointer text-sm font-medium text-slate-700 leading-tight">
          Correction mineure <br/>
          <span class="text-xs font-normal text-slate-500">Mettre à jour le document actuel sans changer de version (maintient le statut ACTIF).</span>
        </label>
      </div>
      <div class="flex items-start gap-2">
        <RadioButton v-model="updateAction" inputId="action2" name="action" value="new-version" />
        <label for="action2" class="cursor-pointer text-sm font-medium text-slate-700 leading-tight">
          Nouvelle version <br/>
          <span class="text-xs font-normal text-slate-500">Archiver le document actuel et créer une nouvelle version de production.</span>
        </label>
      </div>
    </div>
  </ConfirmationDialog>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import ConfirmationDialog from './ConfirmationDialog.vue';
import RadioButton from 'primevue/radiobutton';

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

const updateAction = ref('correction');

watch(() => props.visible, (newVal) => {
  if (newVal) {
    updateAction.value = 'correction';
  }
});

const titleDialog = computed(() => {
  return props.mode === 'new-version' 
    ? 'Modification d\'un document ACTIF' 
    : 'Confirmer la Restauration';
});

const descriptionDialog = computed(() => {
  if (props.mode === 'new-version') {
    return 'Ce document est actuellement en production. Souhaitez-vous faire une simple correction ou créer une nouvelle version ?';
  }
  return 'Cette action restaurera cette archive comme nouvelle version en production.';
});

const confirmLabelDialog = computed(() => {
  if (props.mode === 'new-version') {
    return updateAction.value === 'correction' ? 'Sauvegarder la correction' : 'Publier la Nouvelle Version';
  }
  return 'Restaurer ce Modèle';
});

const confirmIconDialog = computed(() => {
  if (props.mode === 'new-version') {
    return updateAction.value === 'correction' ? 'pi pi-save' : 'pi pi-arrow-right';
  }
  return 'pi pi-history';
});

const iconClassDialog = computed(() => {
  if (props.mode === 'new-version') return 'pi pi-info-circle text-blue-600 text-xl';
  return 'pi pi-history text-amber-600 text-xl';
});

const motifLabelDialog = computed(() => {
  return props.mode === 'new-version' 
    ? 'Justification de la modification (ISO 9001)' 
    : 'Motif de la restauration';
});

const placeholderDialog = computed(() => {
  if (props.mode === 'new-version') {
    return updateAction.value === 'correction' 
      ? 'Ex: Correction orthographique, ajustement mineur...' 
      : 'Ex: Demande client, Tolérances resserrées, Amélioration process...';
  }
  return 'Ex: Restauration suite à erreur, Retour version précédente...';
});

const onConfirm = (motif) => {
  if (props.mode === 'new-version') {
    emit('confirm', { action: updateAction.value, motif });
  } else {
    emit('confirm', motif);
  }
};

const onCancel = () => {
  emit('cancel');
};
</script>