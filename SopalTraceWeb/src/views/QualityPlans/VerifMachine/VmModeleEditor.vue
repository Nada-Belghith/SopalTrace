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
      @restaurer="onRestaurerClick"
    />
    
    <VerifMachineForm :isReadOnly="isReadOnly" @saved="onSaved" @trigger-versioning="onTriggerVersioning" />
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

const router = useRouter();
const route = useRoute();
const toast = useAppToast();
const store = useVerifMachineStore();

const isReadOnly = computed(() => route.query.view === 'true' || store.entete.statut === 'ARCHIVE');
const showVersioningDialog = ref(false);
const versioningMode = ref('new-version');
const isRestoring = ref(false);

onMounted(async () => {
  const id = route.params.id;
  if (id && id !== 'nouveau') {
    try {
      await store.chargerPlanVerif(id);
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

const onTriggerVersioning = () => {
  versioningMode.value = 'new-version';
  showVersioningDialog.value = true;
};

const onVersioningConfirm = async (payload) => {
  isRestoring.value = true;
  showVersioningDialog.value = false;
  try {
    if (versioningMode.value === 'restore') {
      const res = await store.restaurerPlanVerif(store.entete.id, payload); // payload est le motif
      if (res.success) {
        toast.success('Modèle restauré avec succès.', 'Succès');
        if (res.planId) {
            router.replace('/dev/hub');
        }
      }
    } else {
      const { action, motif } = payload;
      if (action === 'correction') {
        const res = await store.sauvegarderPlanVerif();
        if (res.noChanges) {
          toast.info('Pas de modification.', 'Info');
        } else if (res.id) {
          toast.success('Correction enregistrée avec succès.', 'Succès');
          router.replace('/dev/hub');
        } else {
          toast.error(res.error || 'Erreur lors de la sauvegarde.', 'Erreur');
        }
      } else {
        const res = await store.creerNouvelleVersion(motif);
        if (res.success) {
          toast.success('Nouvelle version créée.', 'Succès');
          router.replace('/dev/hub');
        } else {
          toast.error(res.message || 'Erreur lors de la création de la nouvelle version.', 'Erreur');
        }
      }
    }
  } catch (err) {
    console.error('[VmModeleEditor] Exception lors de onVersioningConfirm :', err);
    toast.error('Échec de l\'opération', 'Erreur');
  } finally {
    isRestoring.value = false;
  }
};
</script>
