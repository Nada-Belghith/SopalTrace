<template>
  <div v-if="!isEditMode && !isReadOnly && showBanner" class="bg-blue-50 border border-blue-100 p-4 rounded-xl flex flex-col md:flex-row gap-4 mb-6 shadow-inner w-full">
    <div class="flex items-center text-blue-800 font-black tracking-widest text-xs min-w-[150px]">
      <i class="pi pi-file-import mr-2 text-lg text-blue-600"></i> RÉF. FORMULAIRE *
    </div>
    <div class="flex-1 flex gap-4 items-center relative">
      <template v-if="useDropdown">
         <Dropdown 
            v-model="internalValue" 
            :options="formulairesReferences" 
            optionLabel="codeReference" 
            optionValue="id" 
            placeholder="Sélectionner un formulaire générique" 
            class="w-full md:w-1/2" 
            :disabled="disableSelection"
            :pt="{ overlay: { class: 'shadow-xl border border-slate-200 z-[9999]', style: { backgroundColor: '#ffffff' } }, listContainer: { style: { backgroundColor: '#ffffff' } } }"
            @change="emitUpdate">
            <template #value="slotProps">
                <div v-if="slotProps.value" class="text-sm font-semibold text-slate-800">
                    {{ getFormulaireName(slotProps.value) }}
                </div>
                <span v-else class="text-sm">Sélectionner un formulaire générique</span>
            </template>
            <template #option="slotProps">
                <div class="text-sm">{{ slotProps.option.codeReference }} - {{ slotProps.option.designation }}</div>
            </template>
        </Dropdown>
      </template>
      <template v-else>
        <select 
          v-model="internalValue" 
          :disabled="disableSelection" 
          @change="emitUpdate"
          class="w-full md:w-1/2 rounded-lg px-4 py-2 text-sm font-bold shadow-sm focus:ring-2 focus:ring-blue-400 outline-none transition-shadow bg-white border border-blue-200 text-blue-900 cursor-pointer disabled:opacity-70 disabled:cursor-not-allowed">
          <option value="">-- Choisir un formulaire générique --</option>
          <option v-for="ref in formulairesReferences" :key="ref.id" :value="ref.id">
            {{ ref.codeReference }} - {{ ref.designation }}
          </option>
        </select>
      </template>
      <span class="text-xs font-bold text-blue-500 italic hidden md:block">
        {{ helpText }}
      </span>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue';
import Dropdown from 'primevue/dropdown';

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  formulairesReferences: {
    type: Array,
    default: () => []
  },
  isEditMode: {
    type: Boolean,
    default: false
  },
  isReadOnly: {
    type: Boolean,
    default: false
  },
  showBanner: {
    type: Boolean,
    default: true
  },
  disableSelection: {
    type: Boolean,
    default: false
  },
  useDropdown: {
    type: Boolean,
    default: false
  },
  helpText: {
    type: String,
    default: 'La sélection du formulaire remplira automatiquement les champs suivants.'
  }
});

const emit = defineEmits(['update:modelValue']);

const internalValue = ref(props.modelValue);

watch(() => props.modelValue, (newVal) => {
  internalValue.value = newVal;
});

const emitUpdate = () => {
  emit('update:modelValue', internalValue.value);
};

const getFormulaireName = (id) => {
  const form = props.formulairesReferences.find(r => r.id === id);
  if (form) {
    return `${form.codeReference} - ${form.designation}`;
  }
  return '';
};
</script>
