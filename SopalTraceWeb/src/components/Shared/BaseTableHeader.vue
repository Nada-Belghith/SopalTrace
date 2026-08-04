<template>
  <thead>
    <tr class="bg-[#0f1923] text-white">
      <th 
        v-for="(col, idx) in columns" 
        :key="col.key || idx"
        :style="{ 
          ...{ backgroundColor: col.bgColor },
          ...(col.textAlign && { textAlign: col.textAlign })
        }"
        :class="[
          'p-3 text-[10px] font-black uppercase tracking-widest border-r border-slate-700',
          col.width ? col.width : 'flex-1'
        ]"
      >
        {{ col.label }}
      </th>
    </tr>
  </thead>
</template>

<script setup>
defineProps({
  columns: {
    type: Array,
    required: true,
    description: 'Array of column configs: { key, label, width, bgColor, textAlign }'
  }
});
</script>
