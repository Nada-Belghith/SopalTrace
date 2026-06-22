<template>
  <div class="p-6">
    <Toast position="top-right" />
    <VersioningDialog :visible="showVersioningDialog"
                      mode="restore"
                      :is-loading="isRestoring"
                      @confirm="onVersioningConfirm"
                      @cancel="showVersioningDialog = false"
                      @update:visible="showVersioningDialog = $event" />
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
      :is-restoring="isRestoring"
      @restaurer="onRestaurerClick"
    />

    <ControlePosteForm :is-read-only="isReadOnly" />
  </div>
</template>

<script setup>
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useControlePosteStore } from '@/stores/controlePosteStore';
import ControlePosteForm from '@/components/ControlePoste/ControlePosteForm.vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';

const store = useControlePosteStore();
const route = useRoute();
const router = useRouter();
const toast = useToast();

const isReadOnly = computed(() => route.query.view === 'true');
const showVersioningDialog = ref(false);
const isRestoring = ref(false);

const onRestaurerClick = () => {
  showVersioningDialog.value = true;
};

const onVersioningConfirm = async (motif) => {
  isRestoring.value = true;
  showVersioningDialog.value = false;
  try {
    const res = await store.restaurerPlan(motif);
    if (res.success) {
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Modèle restauré avec succès.', life: 3000 });
      if (res.planId) {
          router.replace('/dev/hub');
      }
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Échec de la restauration', life: 3000 });
  } finally {
    isRestoring.value = false;
  }
};
</script>
