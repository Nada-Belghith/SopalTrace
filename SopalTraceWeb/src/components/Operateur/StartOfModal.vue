<template>
  <div v-if="showModal" class="fixed inset-0 bg-slate-900 bg-opacity-50 flex items-center justify-center z-50 p-4 backdrop-blur-sm transition-opacity">
    <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
      
      <div class="p-6 border-b border-slate-100 flex justify-between items-start">
        <div>
          <h2 class="text-2xl font-bold text-slate-800">Démarrer le contrôle</h2>
          <p class="text-sm text-slate-500 mt-1">OF: {{ selectedOf.numeroOf }} - {{ selectedOf.designationArticle }}</p>
        </div>
        <button @click="$emit('close')" class="text-slate-400 hover:text-slate-600">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
        </button>
      </div>

      <div v-if="errorMessage" class="m-6 p-4 bg-red-50 border border-red-200 rounded-xl flex items-start animate-fade-in-down">
        <svg class="w-6 h-6 text-red-500 mr-3 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
        <div>
          <h3 class="text-red-800 font-bold text-sm">Opération impossible</h3>
          <p class="text-red-600 text-sm mt-1">{{ errorMessage }}</p>
          
          <div v-if="errorMessage && errorMessage.includes('Aucun plan')">
            <p class="text-red-600 text-sm mt-1 font-semibold mb-3">Veuillez demander au responsable d'activer un plan pour cet article.</p>
            
            <button v-if="!planSignale" @click="$emit('signaler')" :disabled="isSignalingPlan" class="px-4 py-2 bg-red-600 text-white text-xs font-bold rounded-lg hover:bg-red-700 transition flex items-center shadow-sm">
              <svg v-if="!isSignalingPlan" class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path></svg>
              <svg v-else class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
              Signaler au Responsable DI
            </button>
            <div v-else class="text-green-700 font-bold text-sm flex items-center mt-2 bg-green-50 p-2 rounded-lg">
              <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
              Signalement envoyé avec succès !
            </div>
          </div>
        </div>
      </div>

      <div v-else class="p-6 overflow-y-auto flex-1">
        <p class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4">Gamme Opératoire</p>
        
        <div class="space-y-3">
          <div v-for="(op, idx) in selectedOf.gammeOperatoire" :key="idx"
               class="border rounded-xl transition-all"
               :class="form.operationCode === op.operationCode ? 'border-blue-200 bg-blue-50/30' : 'border-slate-200 hover:border-slate-300'">
            
            <label class="flex justify-between items-center p-4 cursor-pointer">
              <div class="flex items-center">
                <input type="radio" :value="op.operationCode" :checked="form.operationCode === op.operationCode" @change="$emit('operation-change', op)"
                       class="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer">
                <span class="ml-3 font-semibold text-slate-700">{{ op.libelle }}</span>
              </div>
              <div>
                <span v-if="op.activeExecStatut === 'EN_COURS'" class="px-2 py-1 bg-green-100 text-green-700 text-xs font-bold rounded">EN COURS</span>
                <span v-else-if="op.activeExecStatut === 'EN_PAUSE'" class="px-2 py-1 bg-yellow-100 text-yellow-700 text-xs font-bold rounded">EN PAUSE</span>
                <span v-else-if="op.activeExecStatut === 'CLOTURE'" class="px-2 py-1 bg-gray-100 text-gray-700 text-xs font-bold rounded">CLÔTURÉ</span>
                <span v-else class="px-2 py-1 bg-slate-100 text-slate-500 text-xs font-bold rounded">NON COMMENCÉ</span>
              </div>
            </label>

            <!-- Paramètres étendus si l'opération est sélectionnée -->
            <div v-if="form.operationCode === op.operationCode" class="px-4 pb-4 pt-1 animate-fade-in-down">
              <div class="flex gap-4">
                <div class="flex-1">
                  <label class="block text-[10px] font-bold text-slate-500 uppercase tracking-wider mb-1">Machine / Poste Prévu</label>
                  <input type="text" :value="op.machinePrevueCode || 'N/A'" disabled 
                         class="w-full border border-slate-200 rounded-lg p-2.5 bg-white text-slate-500 text-sm font-medium">
                </div>
                <div class="flex-1">
                  <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">
                    <span v-if="op.operationCode === 'ASS'">Postes Réels (séparés par virgule)</span>
                    <span v-else>Machine / Poste Réel</span>
                  </label>
                  <input type="text" v-model="form.machineCode" :placeholder="op.operationCode === 'ASS' ? 'Ex: P1, P2, P3' : 'Machine utilisée'"
                         class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none transition-all">
                </div>
                <div class="w-32">
                  <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Équipe</label>
                  <select v-model="form.numEquipe" class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none">
                    <option value="1">Matin</option>
                    <option value="2">A-Midi</option>
                    <option value="3">Nuit</option>
                  </select>
                </div>
              </div>

              <div v-if="form.operationCode === 'TRONC' || form.operationCode === 'TRN'" class="flex gap-4 mt-4 animate-fade-in-down border-t border-blue-100 pt-4">
                <div class="flex-1">
                  <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Longueur (mm)</label>
                  <input type="number" step="0.01" v-model="form.longueur" placeholder="Ex: 120.5" :disabled="isLongueurInitialized"
                         class="w-full border border-slate-200 rounded-lg p-2.5 text-sm font-medium focus:outline-none transition-all"
                         :class="isLongueurInitialized ? 'bg-slate-50 text-slate-500 cursor-not-allowed' : 'bg-white text-slate-800 focus:ring-2 focus:ring-blue-100 focus:border-blue-400'">
                </div>
                <div class="flex-1">
                  <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Diamètre (mm)</label>
                  <input type="number" step="0.01" v-model="form.diametre" placeholder="Ex: 15.2" :disabled="isDiametreInitialized"
                         class="w-full border border-slate-200 rounded-lg p-2.5 text-sm font-medium focus:outline-none transition-all"
                         :class="isDiametreInitialized ? 'bg-slate-50 text-slate-500 cursor-not-allowed' : 'bg-white text-slate-800 focus:ring-2 focus:ring-blue-100 focus:border-blue-400'">
                </div>
              </div>
            </div>
          </div>
          
          <div v-if="!selectedOf.gammeOperatoire || selectedOf.gammeOperatoire.length === 0" class="text-sm text-red-500 p-4 bg-red-50 rounded-lg">
            Aucune gamme opératoire définie pour cet article.
          </div>
        </div>
      </div>

      <div class="p-5 border-t border-slate-100 bg-slate-50 flex justify-end space-x-3">
        <button @click="$emit('close')" class="px-5 py-2.5 rounded-lg text-slate-600 font-semibold hover:bg-slate-200 transition-colors">
          {{ planSignale ? 'OK' : 'Annuler' }}
        </button>
        <button v-if="!errorMessage && (!selectedOpState || !selectedOpState.activeExecControleOfId)" @click="$emit('demarrer')" :disabled="!form.operationCode"
                class="px-5 py-2.5 rounded-lg bg-blue-600 text-white font-semibold flex items-center hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          Démarrer le contrôle
        </button>
        <button v-if="!errorMessage && selectedOpState && selectedOpState.activeExecStatut !== 'CLOTURE' && selectedOpState.activeExecControleOfId" @click="$emit('reprendre-existant')" :disabled="!form.operationCode"
                class="px-5 py-2.5 rounded-lg bg-green-600 text-white font-semibold flex items-center hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          Reprendre le contrôle
        </button>
        <button v-if="selectedOpState && selectedOpState.activeExecStatut === 'CLOTURE'" disabled
                class="px-5 py-2.5 rounded-lg bg-gray-200 text-gray-500 font-semibold flex items-center cursor-not-allowed">
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
          Opération Clôturée
        </button>
      </div>

    </div>
  </div>
</template>

<script setup>
defineProps({
  showModal: { type: Boolean, required: true },
  selectedOf: { type: Object, default: () => ({}) },
  form: { type: Object, required: true },
  errorMessage: { type: String, default: '' },
  isSignalingPlan: { type: Boolean, default: false },
  planSignale: { type: Boolean, default: false },
  selectedOpState: { type: Object, default: null },
  isLongueurInitialized: { type: Boolean, default: false },
  isDiametreInitialized: { type: Boolean, default: false }
});

defineEmits(['close', 'signaler', 'operation-change', 'demarrer', 'reprendre-existant']);
</script>

<style scoped>
.animate-fade-in-down {
  animation: fadeInDown 0.2s ease-out;
}
@keyframes fadeInDown {
  from { opacity: 0; transform: translateY(-5px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
