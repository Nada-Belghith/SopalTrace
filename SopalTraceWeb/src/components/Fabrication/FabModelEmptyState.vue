<template>
  <div>
    <template v-if="!hasValidStructure">
      <div class="p-8 text-center bg-amber-50 rounded-lg border border-amber-200 mb-6 flex flex-col items-center justify-center">
        <i class="pi pi-file-excel text-amber-500 text-4xl mb-3"></i>
        <h4 class="text-sm font-bold text-amber-800 mb-1">Structure PRC non définie</h4>
        <p class="text-sm text-amber-700 max-w-lg">Le formulaire sélectionné est à l'état de brouillon. Le Superviseur Qualité doit définir la structure du plan avant que vous puissiez créer des modèles ou des plans par article.</p>
      </div>
    </template>
    
    <template v-else>
      <template v-if="groupesLength === 0">
        <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
          Cliquez sur "Créer une nouvelle section" pour commencer.
        </div>
      </template>
    </template>
  </div>
</template>

<script setup>
defineProps({
  hasValidStructure: {
    type: Boolean,
    required: true
  },
  groupesLength: {
    type: Number,
    required: true
  }
});
</script>
