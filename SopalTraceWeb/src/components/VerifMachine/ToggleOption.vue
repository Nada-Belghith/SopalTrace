<template>
  <label class="flex items-center cursor-pointer bg-slate-100 px-3 py-1.5 rounded-lg border border-slate-200 hover:bg-slate-200 transition-colors gap-2">
    <div class="relative inline-flex items-center">
      <input type="checkbox" :checked="modelValue" @change="$emit('update:modelValue', $event.target.checked)" class="sr-only peer">
      <div class="w-9 h-5 rounded-full transition-colors bg-slate-300"
        :class="modelValue ? activeColor : 'bg-slate-300'">
      </div>
      <div class="absolute left-0.5 top-0.5 bg-white w-4 h-4 rounded-full transition-transform shadow-sm"
        :class="{ 'translate-x-4': modelValue }">
      </div>
    </div>
    <span class="text-xs font-bold text-slate-700 select-none whitespace-nowrap">{{ label }}</span>
  </label>
</template>

<script setup>
defineProps({
  modelValue: { type: Boolean, required: true },
  label: { type: String, required: true },
  activeColor: { type: String, default: 'bg-blue-500' },
});

defineEmits(['update:modelValue']);
</script>
