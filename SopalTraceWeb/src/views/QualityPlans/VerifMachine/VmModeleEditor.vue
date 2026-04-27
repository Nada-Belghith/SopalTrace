<template>
  <div class="p-6">
    <Toast position="top-right" />
    <ConfirmDialog />

    <PlanHeader 
      :id="store.entete.id"
      title="Plan de Vérification Machine"
      :subtitle="store.entete.nom"
      icon="pi pi-desktop"
      iconColorClass="text-emerald-500"
      :is-read-only="isReadOnly"
      :version="store.entete.version"
      :statut="store.entete.statut"
      :is-restoring="store.isLoading"
      @restaurer="confirmRestaurer"
    />
    
    <VerifMachineForm :isReadOnly="isReadOnly" @saved="onSaved" />
  </div>
</template>

<script setup>
import { useRouter, useRoute } from 'vue-router';
import { onMounted, computed } from 'vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import VerifMachineForm from '@/components/VerifMachine/VerifMachineForm.vue';
import { useVerifMachineStore } from '@/stores/verifMachineStore';

const router = useRouter();
const route = useRoute();
const toast = useToast();
const confirm = useConfirm();
const store = useVerifMachineStore();

const isReadOnly = computed(() => route.query.view === 'true' || store.entete.statut === 'ARCHIVE');

onMounted(async () => {
  const id = route.params.id;
  if (id && id !== 'nouveau') {
    try {
      await store.chargerPlanVerif(id);
    } catch (err) {
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

const confirmRestaurer = () => {
  confirm.require({
    message: `Cette action va archiver le plan actuel pour cette machine (${store.entete.machineCode}) et créer une nouvelle version active basée sur ce plan archivé. Voulez-vous continuer ?`,
    header: 'Confirmation de restauration',
    icon: 'ri-history-line',
    acceptLabel: 'Confirmer la Restauration',
    rejectLabel: 'Annuler',
    acceptClass: 'p-button-warning',
    accept: async () => {
      try {
        const res = await store.restaurerPlanVerif(store.entete.id);
        if (res.success) {
          toast.add({ severity: 'success', summary: 'Restauré', detail: 'Le plan a été restauré avec succès.', life: 3000 });
          router.replace(`/dev/verif-machine/editer/${res.planId}`);
        }
      } catch (err) {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'Échec de la restauration.', life: 3000 });
      }
    }
  });
};
</script>
