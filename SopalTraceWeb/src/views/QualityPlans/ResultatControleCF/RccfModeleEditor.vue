<template>
  <div class="p-6">
    <Toast position="top-right" />
    <ConfirmDialog />
    <PlanHeader 
      v-if="store.entete"
      :id="store.entete.id"
      title="Création : Résultats du Contrôle en cours de fabrication"
      subtitle="FE-RC-ENCF - Résultats du contrôle en cours de fabrication (Usi/Esp/Trn)"
      icon="pi pi-file-edit"
      iconColorClass="text-teal-500"
      :is-read-only="isReadOnly"
      :version="store.entete.version"
      :statut="store.entete.statut"
    />

    <DocumentResultatControleCfForm v-if="store.entete" :is-read-only="isReadOnly" />
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { usedocumentRccfStore } from '@/stores/documentRccfStore';
import DocumentResultatControleCfForm from '@/components/ResultatControleCF/DocumentResultatControleCfForm.vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';

const store = usedocumentRccfStore();
const route = useRoute();
const router = useRouter();
const toast = useToast();

const isReadOnly = computed(() => route.query.view === 'true');



onMounted(async () => {
  const planId = route.params.id;
  if (planId && planId !== 'nouveau') {
    await store.chargerPlan(planId);
  } else {
    store.resetCurrentPlan();
  }
});
</script>
