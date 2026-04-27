<template>
  <div class="p-6">
    <Toast position="top-right" />
    <ConfirmDialog />
    <PlanHeader 
      :id="store.entete.id"
      title="Fiche de Contrôle"
      :subtitle="store.entete.nom"
      icon="pi pi-list"
      iconColorClass="text-teal-500"
      :is-read-only="isReadOnly"
      :version="store.entete.version"
      :statut="store.entete.statut"
      :is-restoring="store.isLoading"
      @restaurer="confirmRestaurer"
    />

    <ResultatControleForm :is-read-only="isReadOnly" />
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { usePlanNcStore } from '@/stores/planNcStore';
import ResultatControleForm from '@/components/ResultatControle/ResultatControleForm.vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';

const store = usePlanNcStore();
const route = useRoute();
const router = useRouter();
const confirm = useConfirm();
const toast = useToast();

const id = computed(() => route.params.id);
const isReadOnly = computed(() => route.query.view === 'true');

const confirmRestaurer = () => {
  confirm.require({
    message: 'Voulez-vous vraiment restaurer cette version archivée ? Elle deviendra la nouvelle version active.',
    header: 'Confirmation de restauration',
    icon: 'pi pi-history',
    acceptClass: 'p-button-warning',
    acceptLabel: 'Oui, restaurer',
    rejectLabel: 'Annuler',
    accept: async () => {
      try {
        const res = await store.restaurerPlan();
        if (res.success) {
          toast.add({ severity: 'success', summary: 'Succès', detail: 'Modèle restauré avec succès.', life: 3000 });
          if (res.planId) {
             router.replace(`/dev/resultat-controle/editer/${res.planId}`);
          }
        }
      } catch (error) {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'Échec de la restauration', life: 3000 });
      }
    }
  });
};
</script>
