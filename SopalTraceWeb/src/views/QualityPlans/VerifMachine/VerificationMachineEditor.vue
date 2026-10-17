<template>
  <div class="p-6">
    <Toast position="top-right" />

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
      :showRestaurerBtn="false"
    />
    <!-- Fiche que rempliront les opérateurs, mise en forme avec son modèle (consultation) -->
    <PdfDocumentQualite v-if="store.entete?.id" :document-id="store.entete.id" class="mb-4" />
    <VerifMachineForm :isReadOnly="isReadOnly" @saved="onSaved" />
  </div>
</template>

<script setup>
import { useRouter, useRoute } from 'vue-router';
import { onMounted } from 'vue';
import PlanHeader from '@/components/Shared/PlanHeader.vue';
import PdfDocumentQualite from '@/components/Qualite/PdfDocumentQualite.vue';
import { useAppToast } from '@/composables/useAppToast';
import Toast from 'primevue/toast';
import VerifMachineForm from '@/components/VerifMachine/VerifMachineForm.vue';
import { useVerifMachineStore } from '@/stores/verifMachineStore';
import { useReferentielStore } from '@/stores/referentielStore';
import { useDocumentHeaderMeta } from '@/composables/useDocumentHeaderMeta';

const router = useRouter();
const route = useRoute();
const toast = useAppToast();
const store = useVerifMachineStore();
const refStore = useReferentielStore();

const { isReadOnly } = useDocumentHeaderMeta(route, store);

onMounted(async () => {
  if (!refStore.isDicosVerifMachineLoaded) {
    await refStore.fetchDictionnaires('verif-machine');
  }
  if (!refStore.formulairesReferencesByRole['VERIF_MACHINE']?.length) {
    await refStore.fetchFormulairesReferences('VERIF_MACHINE');
  }
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


</script>
