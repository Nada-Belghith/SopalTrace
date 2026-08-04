<template>
  <div class="flex justify-between items-start mb-6 pb-4 border-b">
    <div>
      <h2 class="text-2xl font-bold text-slate-800">
        OF: {{ activeOfContext.numeroOf }} 
        <span class="text-lg text-slate-500 font-normal">({{ activeOfContext.operationCode }})</span>
      </h2>
      <p class="text-gray-500">
        Machine Réelle: <span class="font-bold text-slate-700">{{ activeOfContext.machineCode }}</span>
      </p>
      <p class="text-gray-500">Statut: 
        <span class="font-semibold" 
              :class="{'text-yellow-600': activeOfContext.statut === 'REGLAGE', 'text-green-600': activeOfContext.statut === 'EN_COURS'}">
          {{ activeOfContext.statut }}
        </span>
      </p>
    </div>
    
    <div class="flex items-center">
      <button v-if="activeOfContext.statut === 'EN_PAUSE' || activeOfContext.statut === 'REGLAGE' || activeOfContext.estEnReglage" 
              @click="$emit('reprendre')" 
              class="px-4 py-2 bg-green-600 text-white font-semibold rounded-lg hover:bg-green-700 shadow-sm transition mr-2">
        ▶️ Reprendre la production
      </button>
      
      <button v-if="activeOfContext.statut === 'EN_COURS' && activeOfContext.a_Des_Controles_Reglage && !activeOfContext.estEnReglage" 
              @click="$emit('mettre-en-reglage')" 
              class="px-4 py-2 bg-yellow-500 text-white font-semibold rounded-lg hover:bg-yellow-600 shadow-sm transition mr-2">
        ⚙️ Mettre Aux Réglages
      </button>

      <button v-if="activeOfContext.statut === 'REGLAGE' || activeOfContext.estEnReglage" 
              @click="$emit('mettre-en-reglage')" 
              class="px-4 py-2 bg-yellow-600 text-white font-semibold rounded-lg hover:bg-yellow-700 shadow-sm transition mr-2">
        🔄 Nouveau Réglage
      </button>
      
      <button v-if="activeOfContext.statut === 'EN_COURS' && !activeOfContext.estEnReglage" 
              @click="$emit('mettre-en-pause')" 
              class="px-4 py-2 bg-slate-600 text-white font-semibold rounded-lg hover:bg-slate-700 shadow-sm transition mr-2">
        ⏸️ Pause
      </button>

      <button @click="$emit('cloturer')" 
              class="px-4 py-2 bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700 shadow-sm transition">
        ⏹️ Clôturer l'opération
      </button>
    </div>
  </div>
</template>

<script setup>
defineProps({
  activeOfContext: {
    type: Object,
    required: true
  }
});

defineEmits(['reprendre', 'mettre-en-reglage', 'mettre-en-pause', 'cloturer']);
</script>
