<template>
  <tbody class="divide-y divide-slate-200">
    <PlanSectionHeader
      :section="localGroupe"
      :index="index"
      :colspan="12"
      label="SEC"
      defaultTitle="Caractéristiques à contrôler"
      :typesSection="store.typesSection"
      :periodicites="store.periodicites"
      :reglesEchantillonnage="store.reglesEchantillonnage"
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
import { useAssModeleStore } from '@/stores/assModeleStore';
import { genererUid } from '@/utils/uuidUtils';
import PlanSectionHeader from '@/components/Shared/PlanSectionHeader.vue';

const props = defineProps({
  groupe: { type: Object, required: true },
  index: { type: Number, required: true },
  isReadOnly: { type: Boolean, default: false }
});

const isReadOnly = computed(() => props.isReadOnly);

const emit = defineEmits(['remove', 'update-groupe', 'section-type-required']);
const store = useAssModeleStore();

const localGroupe = ref({ ...props.groupe });

const isSyncingFromParent = ref(false);

watch(() => props.groupe, (newGroupe) => {
  if (!newGroupe) return;
  const sourceSource = JSON.stringify(newGroupe);
  const sourceLocale = JSON.stringify(localGroupe.value);
  
  if (sourceSource !== sourceLocale) {
    isSyncingFromParent.value = true;
    localGroupe.value = JSON.parse(sourceSource);
    setTimeout(() => { isSyncingFromParent.value = false; }, 0);
  }
}, { deep: true });

const handleSectionUpdate = (updatedSection) => {
  if (isSyncingFromParent.value) return;
  localGroupe.value = { ...updatedSection };
  emit('update-groupe', JSON.parse(JSON.stringify(localGroupe.value)));
};

watch(() => store.entete.operationCode, (newOp) => {
  if (isReadOnly.value) return;
  if (newOp !== 'ASS') {
    if (localGroupe.value.modeFreq === 'FIXE') {
      localGroupe.value.modeFreq = 'SANS';
      localGroupe.value.regleEchantillonnageId = null;
      emit('update-groupe', JSON.parse(JSON.stringify(localGroupe.value)));
    }
  }
});

const ajouterLigne = () => {
  if (isReadOnly.value) return;
  
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
    estCritique: false
  };
  
  localGroupe.value.lignes = [...(localGroupe.value.lignes || []), nouvelleLigne];
  emit('update-groupe', JSON.parse(JSON.stringify(localGroupe.value)));
};
</script>
