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
      title="Plan de Vérification Machine"
      :subtitle="store.entete.nom"
      icon="pi pi-desktop"
      iconColorClass="text-emerald-500"
      :is-read-only="isReadOnly"
      :version="store.entete.version"
      :statut="store.entete.statut"
      :is-restoring="isRestoring"
      @restaurer="onRestaurerClick"
    />
    
    <VerifMachineForm :isReadOnly="isReadOnly" @saved="onSaved" />
  </div>
</template>

<script setup>
import { useRouter, useRoute } from 'vue-router';
import { onMounted, computed, ref } from 'vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import VerifMachineForm from '@/components/VerifMachine/VerifMachineForm.vue';
import { useVerifMachineStore } from '@/stores/verifMachineStore';

const router = useRouter();
const route = useRoute();
const toast = useToast();
const store = useVerifMachineStore();

const isReadOnly = computed(() => route.query.view === 'true' || store.entete.statut === 'ARCHIVE');
const showVersioningDialog = ref(false);
const isRestoring = ref(false);

onMounted(async () => {
  const id = route.params.id;
  if (id && id !== 'nouveau') {
    try {
      await store.chargerPlanVerif(id);
    } catch {
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger le plan.', life: 3000 });
    }
  } else {
    store.resetPlan();
  }
});

const onSaved = (result) => {
  if (result.noChanges) {
    toast.add({
      severity: 'info',
      summary: 'Aucun changement',
      detail: 'Le plan est identique à la version actuelle. Aucune nouvelle version n\'a été créée.',
      life: 3000
    });
    return;
  }

  toast.add({
    severity: 'success',
    summary: 'Plan Enregistré',
    detail: result.isNew 
      ? 'Le plan de vérification a été créé avec succès.'
      : `Nouvelle version (V${store.entete.version}) créée et activée.`,
    life: 4000
  });
  
  if (result.id && result.id !== route.params.id) {
    router.replace(`/dev/verif-machine/editer/${result.id}`);
  }
};

const onRestaurerClick = () => {
  showVersioningDialog.value = true;
};

const onVersioningConfirm = async (motif) => {
  isRestoring.value = true;
  showVersioningDialog.value = false;
  try {
    const res = await store.restaurerPlanVerif(store.entete.id, motif);
    if (res.success) {
      toast.add({ severity: 'success', summary: 'Restauré', detail: 'Le plan a été restauré avec succès.', life: 3000 });
      router.replace(`/dev/verif-machine/editer/${res.planId}`);
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Échec de la restauration.', life: 3000 });
  } finally {
    isRestoring.value = false;
  }
};
</script>
