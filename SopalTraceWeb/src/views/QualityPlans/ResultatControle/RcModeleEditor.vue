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
      title="Fiche de Contrôle de Poste"
      :subtitle="store.entete.nom"
      icon="pi pi-list"
      iconColorClass="text-teal-500"
      :is-read-only="isReadOnly"
      :version="store.entete.version"
      :statut="store.entete.statut"
      :is-restoring="isRestoring"
      @restaurer="onRestaurerClick"
    />

    <ResultatControleForm :is-read-only="isReadOnly" />
  </div>
</template>

<script setup>
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { usePlanNcStore } from '@/stores/planNcStore';
import ResultatControleForm from '@/components/ResultatControle/ResultatControleForm.vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';

const store = usePlanNcStore();
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
          router.replace(`/dev/resultat-controle/editer/${res.planId}`);
      }
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Échec de la restauration', life: 3000 });
  } finally {
    isRestoring.value = false;
  }
};
</script>
