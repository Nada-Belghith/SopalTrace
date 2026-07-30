<template>
  <div v-if="!isReadOnly" class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-6 rounded-b-xl">
    <EditorActions 
      :label="planId ? 'Sauvegarder les Modifications' : 'Enregistrer le Plan'"
      loading-label="Enregistrement..."
      :icon="planId ? 'pi pi-save' : 'pi pi-check'"
      variant="primary"
      :is-loading="isLoading || isSaving"
      @submit="handleInitialSubmit"
      @cancel="$emit('cancel')"
    />
    
    <VersioningDialog 
      :visible="showVersioningDialog"
      mode="new-version"
      :is-loading="isSaving"
      @confirm="onVersioningConfirm"
      @cancel="showVersioningDialog = false"
      @update:visible="showVersioningDialog = $event" 
    />
  </div>
</template>

<script setup>
import { ref } from 'vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';

const props = defineProps({
  planId: { type: [String, Number], default: null },
  statut: { type: String, default: 'ACTIF' },
  isLoading: { type: Boolean, default: false },
  isReadOnly: { type: Boolean, default: false },
  isSaving: { type: Boolean, default: false }
});

const emit = defineEmits(['save-direct', 'save-correction', 'save-new-version', 'cancel']);

const showVersioningDialog = ref(false);

const handleInitialSubmit = () => {
    if (props.planId && props.statut === 'ACTIF') {
        showVersioningDialog.value = true;
    } else {
        emit('save-direct');
    }
};

const onVersioningConfirm = (payload) => {
    showVersioningDialog.value = false;
    const { action, motif } = payload;
    if (action === 'correction') {
        emit('save-correction');
    } else {
        emit('save-new-version', motif);
    }
};
</script>
