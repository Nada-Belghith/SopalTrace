<template>
  <tbody class="divide-y divide-slate-200">
    <PlanSectionHeader
      :section="localSection"
      :index="index"
      :colspan="12"
      label="SEC"
      :defaultTitle="defaultTitle"
      :typesSection="typesSection"
      :periodicites="periodicites"
      :reglesEchantillonnage="reglesEchantillonnage"
      :isReadOnly="isReadOnly"
      @add-ligne="ajouterLigne"
      @remove="() => $emit('remove', section.id)"
      @update:section="handleSectionUpdate"
    />
    
    <BaseLigneControl
      v-for="ligne in (localSection.lignes || [])"
      :key="ligne.id"
      :ligne="ligne"
      :columns="columns"
      :is-read-only="isReadOnly"
      :types-controle="typesControle"
      :moyens-controle="moyensControle"
      :instruments="instruments"
      @remove="(ligneId) => supprimerLigneASection(ligneId)"
      @update="(updatedLigne) => mettreAJourLigne(updatedLigne)"
    />
  </tbody>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import { genererUid } from '@/utils/uuidUtils';
import PlanSectionHeader from '@/components/Shared/PlanSectionHeader.vue';
import BaseLigneControl from '@/components/Shared/BaseLigneControl.vue';

const props = defineProps({
  section: { type: Object, required: true },
  index: { type: Number, required: true },
  isReadOnly: { type: Boolean, default: false },
  defaultTitle: { type: String, default: 'Caractéristiques à contrôler' },
  columns: { type: Array, default: () => [] },
  typesSection: { type: Array, default: () => [] },
  periodicites: { type: Array, default: () => [] },
  reglesEchantillonnage: { type: Array, default: () => [] },
  typesControle: { type: Array, default: () => [] },
  moyensControle: { type: Array, default: () => [] },
  instruments: { type: Array, default: () => [] },
  operationCode: { type: String, default: '' }
});

const isReadOnly = computed(() => props.isReadOnly);

const emit = defineEmits(['remove', 'update-section', 'section-type-required']);

const localSection = ref({ ...props.section });
const isSyncingFromParent = ref(false);

function stableStringify(obj) {
  if (obj === undefined) return undefined;
  if (obj === null || typeof obj !== 'object') return JSON.stringify(obj);
  if (Array.isArray(obj)) return '[' + obj.map(stableStringify).join(',') + ']';
  const keys = Object.keys(obj).sort();
  return '{' + keys.map(k => JSON.stringify(k) + ':' + stableStringify(obj[k])).join(',') + '}';
}

watch(() => props.section, (newSection) => {
  if (!newSection) return;
  if (stableStringify(newSection) !== stableStringify(localSection.value)) {
    isSyncingFromParent.value = true;
    localSection.value = JSON.parse(JSON.stringify(newSection));
    setTimeout(() => { isSyncingFromParent.value = false; }, 0);
  }
}, { deep: true });

const emitUpdate = (freshSection) => {
  localSection.value = { ...freshSection };
  emit('update-section', freshSection);
};

const handleSectionUpdate = (updatedHeaderProps) => {
  if (isSyncingFromParent.value) return;
  const freshSection = JSON.parse(JSON.stringify(props.section));
  const headerProps = { ...updatedHeaderProps };
  delete headerProps.lignes; // Prevent overriding lines
  Object.assign(freshSection, headerProps);
  emitUpdate(freshSection);
};

watch(() => props.operationCode, (newOp) => {
  if (isReadOnly.value) return;
  if (newOp !== 'ASS' && newOp !== 'PF') {
    if (localSection.value.modeFreq === 'FIXE') {
      const freshSection = JSON.parse(JSON.stringify(props.section));
      freshSection.modeFreq = 'SANS';
      freshSection.regleEchantillonnageId = null;
      emitUpdate(freshSection);
    }
  }
});

const ajouterLigne = () => {
  if (isReadOnly.value) return;
  
  if (props.operationCode === 'PF' && !localSection.value.typeSectionId) {
    emit('section-type-required');
    return;
  }

  const nouvelleLigne = {
    id: genererUid(),
    typeCaracteristiqueId: null,
    typeControleId: null,
    moyenControleId: null,
    moyenTexteLibre: '',
    instrumentCode: null,
    limiteSpecTexte: '', 
    unite: '',          
    instruction: '',
    observations: '',
    estCritique: false,
    valeursColonnesSpecifiques: {}
  };
  const freshSection = JSON.parse(JSON.stringify(props.section));
  freshSection.lignes = [...(freshSection.lignes || []), nouvelleLigne];
  emitUpdate(freshSection);
};

const supprimerLigneASection = (ligneId) => {
  const freshSection = JSON.parse(JSON.stringify(props.section));
  freshSection.lignes = freshSection.lignes.filter(l => l.id !== ligneId);
  emitUpdate(freshSection);
};

const mettreAJourLigne = (updatedLigne) => {
  const freshSection = JSON.parse(JSON.stringify(props.section));
  const idx = freshSection.lignes.findIndex(l => l.id === updatedLigne.id);
  if (idx !== -1) {
    freshSection.lignes.splice(idx, 1, updatedLigne);
    emitUpdate(freshSection);
  }
};
</script>
