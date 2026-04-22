<template>
  <div class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
    <table class="w-full text-left border-collapse min-w-[1200px]">
      
      <!-- EN-TÊTE GLOBAL (Réutilisable) -->
      <FabTableHeader v-if="index === 0" :columns="planColumns" />
      
     <tbody class="divide-y divide-slate-200">
  
  <!-- RÉUTILISATION DU HEADER -->
  <FabSectionHeader
    :section="localSection"
    :index="index"
    :colspan="currentColspan"
    label="GROUPE"
    :periodicites="periodicites"
    :is-archived="isArchived"
    @add-ligne="addLigneLocal"
    @remove="() => emit('remove')"
    @update:section="(hdr) => { Object.assign(localSection, hdr); emit('update:section', { ...localSection }); }"
  />

  <!-- LIGNES AVEC FabPlanLigneControl -->
  <FabPlanLigneControl
    v-for="ligne in localSection.lignes"
    :key="ligne.id"
    :ligne="ligne"
    :section="localSection"
    :is-archived="isArchived"
    :operation-code="operationCode"
    @remove="(id) => { localSection.lignes = localSection.lignes.filter(l => l.id !== id); emit('remove-ligne', id); }"
    @update="(updated) => {
      const idx = localSection.lignes.findIndex(l => l.id === updated.id);
      if (idx !== -1) localSection.lignes.splice(idx, 1, updated);
    }"
  />
  
</tbody>
    </table>

  </div>
</template>

<script setup>
import { defineProps, defineEmits, ref, watch, nextTick, computed } from 'vue';
import FabTableHeader from './FabTableHeader.vue';
import FabSectionHeader from './FabSectionHeader.vue';
import FabPlanLigneControl from './FabPlanLigneControl.vue';

const props = defineProps({
  section: { type: Object, required: true },
  index: { type: Number, required: true },
  periodicites: { type: Array, default: () => [] },
  isArchived: { type: Boolean, default: false },
  operationCode: { type: String, default: '' }
});

const emit = defineEmits(['add-ligne', 'remove', 'remove-ligne', 'update:section']);

// Local copy to avoid mutating props directly from child components
const localSection = ref({ ...props.section });
const isSyncingFromParent = ref(false);

watch(() => props.section, (newVal) => {
  // Avoid overwriting local edits unnecessarily; only sync when id changes or parent produced a different object reference
  if (!newVal) return;
  if (newVal.id !== localSection.value.id) {
    isSyncingFromParent.value = true;
    localSection.value = { ...newVal };
    // release flag on next tick
    nextTick(() => { isSyncingFromParent.value = false; });
  }
}, { deep: true });

// NOTE: We intentionally do NOT emit on every deep change of localSection to avoid
// recursive update loops. Updates are emitted explicitly from handlers below when
// const store = useFabModeleStore(); // Removed because it was unused

const isAssTrn = computed(() => ['ASS', 'TRN'].includes((props.operationCode || '').toUpperCase()));

const planColumns = computed(() => [
  { label: 'Caractéristique contrôlée', width: 'w-[22%]' },
  { label: 'Valeur', width: isAssTrn.value ? 'w-[10%]' : 'w-[12%]', bgColor: '#2563eb', textAlign: 'center' },
  { label: 'Limite spécif.', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Type de contrôle', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Moyen de contrôle', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Code instrument', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Observations', width: 'flex-1' },
  { label: '', width: 'w-8', textAlign: 'center' }
]);

const currentColspan = computed(() => 8);

// Helper to generate an id that works in all environments

const generateId = () => {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) return crypto.randomUUID();
  // fallback: simple guid-like
  return 'id-' + Math.random().toString(36).slice(2, 10) + '-' + Date.now().toString(36);
};

const addLigneLocal = () => {
  const nouvelleLigne = { id: generateId(), typeCaracteristiqueId: null, typeControleId: null, moyenControleId: null, moyenTexteLibre: '', instrumentCode: null, valeurNominale: null, toleranceInferieure: null, toleranceSuperieure: null, unite: '', limiteSpecTexte: '', instruction: '', observations: '', estCritique: false };
  localSection.value.lignes = [ ...(localSection.value.lignes || []), nouvelleLigne ];
  emit('update:section', { ...localSection.value });
};
</script>