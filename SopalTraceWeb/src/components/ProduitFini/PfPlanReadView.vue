<!-- Wrapper Produit Fini — utilise le composant partagé -->
<template>
  <PlanReadView
    :sections="sections"
    :remarques="remarques"
    :legende-moyens="legendeMoyens"
    :types-section="store.typesSection || []"
    :types-caracteristique="store.typesCaracteristique || []"
    :types-controle="store.typesControle || []"
    :moyens-controle="store.moyensControle || []"
    :periodicites="store.periodicites || []"
    :regles-echantillonnage="store.reglesEchantillonnage || []"
  />
</template>

<script setup>
import PlanReadView from '@/components/Shared/PlanReadView.vue';
import { usePfPlanStore } from '@/stores/pfPlanStore';

const store = usePfPlanStore();

defineProps({
  sections:      { type: Array,  default: () => [] },
  remarques:     { type: String, default: '' },
  legendeMoyens: { type: String, default: '' },
});
</script>
