<!-- Wrapper Produit Fini — utilise le composant partagé -->
<template>
  <PlanReadView
    :sections="sections"
    :remarques="remarques"
    :legende-moyens="legendeMoyens"
    :configuration-colonnes="store.entete?.configurationColonnes || []"
    :types-section="refStore.typesSection || []"
    :types-caracteristique="refStore.typesCaracteristique || []"
    :types-controle="refStore.typesControle || []"
    :moyens-controle="refStore.moyensControle || []"
    :periodicites="refStore.periodicites || []"
    :regles-echantillonnage="refStore.reglesEchantillonnage || []"
  />
</template>

<script setup>
import PlanReadView from '@/components/Shared/PlanReadView.vue';
import { usedocumentProduitFiniStore } from '@/stores/documentProduitFiniStore';
import { useReferentielStore } from '@/stores/referentielStore';

const store = usedocumentProduitFiniStore();
const refStore = useReferentielStore();

defineProps({
  sections:      { type: Array,  default: () => [] },
  remarques:     { type: String, default: '' },
  legendeMoyens: { type: String, default: '' },
});
</script>
