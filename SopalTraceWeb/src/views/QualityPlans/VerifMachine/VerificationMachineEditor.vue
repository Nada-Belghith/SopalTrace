<template>
  <div class="p-6">
    <Toast position="top-right" />
    <VersioningDialog :visible="showVersioningDialog"
                      :mode="versioningMode"
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
      :showRestaurerBtn="false"
    />
    <VerifMachineForm :isReadOnly="isReadOnly" @saved="onSaved" />
  </div>
</template>

<script setup>
import { useRouter, useRoute } from 'vue-router';
import { onMounted, computed, ref } from 'vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import { useAppToast } from '@/composables/useAppToast';
import Toast from 'primevue/toast';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import VerifMachineForm from '@/components/VerifMachine/VerifMachineForm.vue';
import { useVerifMachineStore } from '@/stores/verifMachineStore';
import { useDocumentHeaderMeta } from '@/composables/useDocumentHeaderMeta';

const router = useRouter();
const route = useRoute();
const toast = useAppToast();
const store = useVerifMachineStore();

const { isReadOnly } = useDocumentHeaderMeta(route, store);
const showVersioningDialog = ref(false);
const versioningMode = ref('new-version');
const isRestoring = ref(false);

onMounted(async () => {
  const id = route.params.id;
  if (id && id !== 'nouveau') {
    try {
      await store.loadDocumentById(id);
    } catch {
      toast.error('Impossible de charger le plan.');
    }
  } else {
    store.resetPlan();
  }
});

const onSaved = (result) => {
  if (result.noChanges) {
    toast.info('Le plan est identique à la version actuelle. Aucune nouvelle version n\'a été créée.', 'Aucun changement');
    setTimeout(() => {
      router.push('/dev/hub');
    }, 1500);
    return;
  }

  toast.success(result.isNew 
    ? 'Le plan de vérification a été créé avec succès.'
    : `Nouvelle version (V${result.version ?? store.entete.version}) créée et activée.`, 
    'Plan Enregistré');
  
  setTimeout(() => {
    router.push('/dev/hub');
  }, 1500);
};

const onRestaurerClick = () => {
  versioningMode.value = 'restore';
  showVersioningDialog.value = true;
};

const onVersioningConfirm = async (payload) => {
  isRestoring.value = true;
  showVersioningDialog.value = false;
  try {
    if (versioningMode.value === 'restore') {
      const res = await store.restoreDocument(store.entete.id, payload); // payload est le motif
      if (res.success) {
        toast.success('Modèle restauré avec succès.', 'Succès');
        if (res.planId) {
            router.replace('/dev/hub');
        }
      }
    }
  } catch (err) {
    console.error('[VerificationMachineEditor] Exception lors de onVersioningConfirm :', err);
    toast.error('Échec de l\'opération', 'Erreur');
  } finally {
    isRestoring.value = false;
  }
};
</script>
