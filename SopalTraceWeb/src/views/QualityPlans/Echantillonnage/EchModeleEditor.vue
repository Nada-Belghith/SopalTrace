<template>
  <div class="p-6">
    <PlanHeader 
      title="Plan d'Échantillonnage"
      subtitle="Module en construction"
      icon="pi pi-check-square"
      iconColorClass="text-purple-500"
    />

    <div class="bg-indigo-50 border-l-4 border-indigo-500 p-6 rounded-lg">
      <div class="flex items-center gap-4">
        <div class="w-12 h-12 bg-indigo-100 rounded-full flex items-center justify-center text-indigo-600">
          <i class="pi pi-info-circle text-2xl"></i>
        </div>
        <div>
          <h3 class="font-bold text-indigo-900">Module en cours de développement</h3>
          <p class="text-indigo-700 text-sm">Cet éditeur permettra de configurer les niveaux d'échantillonnage ISO 9001.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import PlanHeader from '@/components/Shared/PlanHeader.vue';
</script>
