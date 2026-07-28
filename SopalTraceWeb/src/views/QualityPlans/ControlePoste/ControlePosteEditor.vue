<template>
  <div class="p-6">
    <Toast position="top-right" />
    <ConfirmDialog />
    <VersioningDialog :visible="showVersioningDialog"
                      :mode="versioningMode"
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

    <ControlePosteForm v-if="store.entete" :is-read-only="isReadOnly" @trigger-versioning="onTriggerVersioning" />
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useControlePosteStore } from '@/stores/controlePosteStore';
import ControlePosteForm from '@/components/ControlePoste/ControlePosteForm.vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';

const store = useControlePosteStore();
const route = useRoute();
const router = useRouter();
const toast = useToast();

const isReadOnly = computed(() => route.query.view === 'true');
const showVersioningDialog = ref(false);
const isRestoring = ref(false);
const versioningMode = ref('restore');

onMounted(() => {
  const planId = route.params.id;
  if (!planId || planId === 'nouveau') {
    store.resetState();
  }
});

const onRestaurerClick = () => {
  versioningMode.value = 'restore';
  showVersioningDialog.value = true;
};

const onTriggerVersioning = () => {
  versioningMode.value = 'new-version';
  showVersioningDialog.value = true;
};

const onVersioningConfirm = async (payload) => {
  isRestoring.value = true;
  showVersioningDialog.value = false;
  try {
    if (versioningMode.value === 'restore') {
      const res = await store.restaurerPlan(payload); // payload est le motif
      if (res.success) {
        toast.add({ severity: 'success', summary: 'Succès', detail: 'Modèle restauré avec succès.', life: 3000 });
        if (res.planId) {
            router.replace('/dev/hub');
        }
      }
    } else {
      const { action, motif } = payload;
      if (action === 'correction') {
        const res = await store.sauvegarderPlan(true, true); // forceSave = true
        if (res.success) {
          toast.add({ severity: 'success', summary: 'Succès', detail: 'Correction enregistrée avec succès.', life: 3000 });
          router.replace('/dev/hub');
        } else if (res.noChanges) {
          toast.add({ severity: 'info', summary: 'Info', detail: 'Pas de modification.', life: 3000 });
        } else {
          toast.add({ severity: 'error', summary: 'Erreur', detail: res.message, life: 3000 });
        }
      } else {
        const res = await store.creerNouvelleVersion(motif);
        if (res.success) {
          toast.add({ severity: 'success', summary: 'Succès', detail: 'Nouvelle version créée.', life: 3000 });
          router.replace('/dev/hub');
        } else {
          toast.add({ severity: 'error', summary: 'Erreur', detail: res.message, life: 3000 });
        }
      }
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Échec de l\'opération', life: 3000 });
  } finally {
    isRestoring.value = false;
  }
};
</script>
