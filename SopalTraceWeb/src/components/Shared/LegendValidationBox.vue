<template>
  <div
    v-if="hasCustomInstruments && !isForcedView"
    :class="[
      'mt-6 px-4 py-3 rounded-lg',
      showValidation && !modelValue
        ? 'bg-yellow-50 border-2 border-yellow-300'
        : 'bg-slate-50 border border-slate-200'
    ]"
  >
    <div class="flex items-start gap-2 mb-2">
      <i
        :class="[
          'pi text-base mt-0.5',
          showValidation && !modelValue
            ? 'pi-exclamation-circle text-yellow-600'
            : 'pi-info-circle text-slate-500'
        ]"
      ></i>
      <div>
        <label
          :class="[
            'block text-[10px] font-black uppercase tracking-widest',
            showValidation && !modelValue ? 'text-yellow-800' : 'text-slate-700'
          ]"
        >
          {{ showValidation && !modelValue ? '⚠️ Légende OBLIGATOIRE' : 'Légende des moyens' }}
        </label>
        <p
          :class="[
            'text-[9px] mt-0.5',
            showValidation && !modelValue ? 'text-yellow-700' : 'text-slate-500'
          ]"
        >
          Texte personnalisé utilisé - veuillez documenter les abréviations
        </p>
      </div>
    </div>
    <textarea
      :value="modelValue"
      @input="$emit('update:modelValue', $event.target.value)"
      placeholder="Ex: PAC*=PAC512,PAC612"
      rows="2"
      :disabled="isReadOnly"
      :class="[
        'w-full border rounded px-3 py-2 text-xs outline-none shadow-sm transition-opacity',
        isReadOnly ? 'opacity-60 bg-slate-100 cursor-not-allowed text-slate-500' : 'bg-white text-slate-700',
        showValidation && !modelValue && !isReadOnly
          ? 'bg-red-50 border-red-300 focus:border-red-500'
          : 'border-slate-300 focus:border-blue-500'
      ]"
    ></textarea>
    <p v-if="showValidation && !modelValue && !isReadOnly" class="text-[9px] text-red-600 font-bold mt-1">❌ Remplissez la légende avant d'enregistrer!</p>
  </div>
</template>

<script setup>
defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  showValidation: {
    type: Boolean,
    default: false
  },
  hasCustomInstruments: {
    type: Boolean,
    default: false
  },
  isReadOnly: {
    type: Boolean,
    default: false
  },
  isForcedView: {
    type: Boolean,
    default: false
  }
});

defineEmits(['update:modelValue']);
</script>
