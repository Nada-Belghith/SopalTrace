<template>
  <tbody class="divide-y divide-slate-200">
    <PlanSectionHeader
      :section="localGroupe"
      :index="index"
      :colspan="12"
      label="SEC"
      :defaultTitle="defaultTitle"
      :typesSection="typesSection"
      :periodicites="periodicites"
      :reglesEchantillonnage="reglesEchantillonnage"
      :isReadOnly="isReadOnly"
      @add-ligne="ajouterLigne"
      @remove="() => $emit('remove')"
      @update:section="handleSectionUpdate"
    />
    <slot></slot>
  </tbody>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import { genererUid } from '@/utils/uuidUtils';
import PlanSectionHeader from '@/components/Shared/PlanSectionHeader.vue';

const props = defineProps({
  groupe: { type: Object, required: true },
  index: { type: Number, required: true },
  isReadOnly: { type: Boolean, default: false },
  defaultTitle: { type: String, default: 'Caractéristiques à contrôler' },
  typesSection: { type: Array, default: () => [] },
  periodicites: { type: Array, default: () => [] },
  reglesEchantillonnage: { type: Array, default: () => [] },
  operationCode: { type: String, default: '' }
});

const isReadOnly = computed(() => props.isReadOnly);

const emit = defineEmits(['remove', 'update-groupe', 'section-type-required']);

const localGroupe = ref({ ...props.groupe });
const isSyncingFromParent = ref(false);

watch(() => props.groupe, (newGroupe) => {
  if (!newGroupe) return;
  // Only sync from parent if it's a truly different section (id changed)
  // or if localGroupe is empty. Avoid overwriting our own emitted update.
  if (newGroupe.id !== localGroupe.value?.id) {
    isSyncingFromParent.value = true;
    localGroupe.value = JSON.parse(JSON.stringify(newGroupe));
    setTimeout(() => { isSyncingFromParent.value = false; }, 0);
  }
}, { deep: true });

const handleSectionUpdate = (updatedSection) => {
  if (isSyncingFromParent.value) return;
  const freshGroupe = JSON.parse(JSON.stringify(props.groupe));
  const headerProps = { ...updatedSection };
  delete headerProps.lignes;
  Object.assign(freshGroupe, headerProps);
  localGroupe.value = { ...freshGroupe };
  emit('update-groupe', freshGroupe);
};

watch(() => props.operationCode, (newOp) => {
  if (isReadOnly.value) return;
  if (newOp !== 'ASS' && newOp !== 'PF') {
    if (localGroupe.value.modeFreq === 'FIXE') {
      const freshGroupe = JSON.parse(JSON.stringify(props.groupe));
      freshGroupe.modeFreq = 'SANS';
      freshGroupe.regleEchantillonnageId = null;
      localGroupe.value = { ...freshGroupe };
      emit('update-groupe', freshGroupe);
    }
  }
});

const ajouterLigne = () => {
  if (isReadOnly.value) return;
  
  if (props.operationCode === 'PF' && !localGroupe.value.typeSectionId) {
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
  const freshGroupe = JSON.parse(JSON.stringify(props.groupe));
  freshGroupe.lignes = [...(freshGroupe.lignes || []), nouvelleLigne];
  localGroupe.value = { ...freshGroupe };
  emit('update-groupe', freshGroupe);
};
</script>
