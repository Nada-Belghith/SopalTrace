<template>
  <div class="p-6">
    <Toast position="top-right" />
    <ConfirmDialog />
    <PlanHeader 
      v-if="store.entete"
      :id="store.entete.id"
      title="Plan de contrôle de Poste"
      :subtitle="store.entete.nom"
      icon="pi pi-list"
      iconColorClass="text-teal-500"
      :is-read-only="isReadOnly"
      :version="store.entete.version"
      :statut="store.entete.statut"
      :showRestaurerBtn="false"
    />

    <documentControlePosteForm v-if="store.entete" :is-read-only="isReadOnly" />
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { usedocumentControlePosteStore } from '@/stores/documentControlePosteStore';
import DocumentControlePosteForm from '@/components/DocumentControlePoste/DocumentControlePosteForm.vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import { useDocumentHeaderMeta } from '@/composables/useDocumentHeaderMeta';

const store = usedocumentControlePosteStore();
const route = useRoute();
const router = useRouter();
const toast = useToast();

const { isReadOnly } = useDocumentHeaderMeta(route, store);

onMounted(() => {
  const planId = route.params.id;
  if (!planId || planId === 'nouveau') {
    store.resetState();
  }
});
</script>
