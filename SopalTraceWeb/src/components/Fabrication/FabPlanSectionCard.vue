<template>
  <div class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
    <table class="w-full text-left border-collapse min-w-[1200px]">
      
      <!-- EN-TÊTE GLOBAL (Réutilisable) -->
      <FabTableHeader v-if="index === 0" :columns="planColumns" />
      
      <tbody class="divide-y divide-slate-200">
        
        <!-- RÉUTILISATION DU HEADER -->
        <FabSectionHeader
          :section="section"
          :index="index"
          :colspan="8"
          label="GROUPE"
          :periodicites="periodicites"
          @add-ligne="$emit('add-ligne')"
          @remove="$emit('remove')"
          @update:section="$emit('update:section', $event)"
        />

        <!-- LIGNES AVEC FabPlanLigneControl -->
        <FabPlanLigneControl
          v-for="ligne in section.lignes"
          :key="ligne.id"
          :ligne="ligne"
          :section="section"
          @remove="$emit('remove-ligne', $event)"
        />
        
      </tbody>
    </table>

  </div>
</template>

<script setup>
import { defineProps, defineEmits } from 'vue';
import FabTableHeader from './FabTableHeader.vue';
import FabSectionHeader from './FabSectionHeader.vue';
import FabPlanLigneControl from './FabPlanLigneControl.vue';

const props = defineProps({
  section: { type: Object, required: true },
  index: { type: Number, required: true },
  periodicites: { type: Array, default: () => [] }
});

defineEmits(['add-ligne', 'remove', 'remove-ligne', 'update:section']);

const planColumns = [
  { label: 'Caractéristique contrôlée', width: 'w-[22%]' },
  { label: 'Valeur', width: 'w-[12%]', bgColor: '#2563eb', textAlign: 'center' },
  { label: 'Limite spécif.', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Type de contrôle', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Moyen de contrôle', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Code instrument', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Observations', width: 'flex-1' },
  { label: '', width: 'w-8', textAlign: 'center' }
];
</script>
