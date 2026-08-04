<template>
  <div @click="$emit('select', ofItem)"
       class="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 w-[400px] cursor-pointer hover:shadow-md hover:border-blue-300 transition-all flex flex-col justify-between">
    
    <div class="flex justify-between items-start mb-4">
      <span class="text-xs font-bold text-blue-600 tracking-wider">OF: {{ ofItem.numeroOf }}</span>
      <span class="px-2 py-1 font-bold text-[10px] rounded uppercase tracking-wider"
            :class="statutReel === 'EN COURS' ? 'bg-green-50 text-green-600' : 'bg-gray-100 text-gray-500'">
        {{ statutReel }}
      </span>
    </div>

    <div class="mb-4">
      <h3 class="text-lg font-bold text-slate-800 leading-tight mb-1">{{ ofItem.designationArticle }}</h3>
      <p class="text-sm text-slate-400 font-medium">{{ ofItem.codeArticle }}</p>
    </div>

    <!-- Timeline Gamme Opératoire -->
    <div v-if="ofItem.gammeOperatoire && ofItem.gammeOperatoire.length > 0" class="mb-4 mt-2">
      <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-3">Progression des opérations</p>
      <div class="relative flex items-center justify-between px-2">
        <!-- Ligne de fond -->
        <div class="absolute top-2 left-4 right-4 h-1 bg-slate-100 z-0 rounded-full"></div>
        
        <div v-for="(op, idx) in ofItem.gammeOperatoire" :key="idx" class="relative z-10 flex flex-col items-center group">
          <!-- Point -->
          <div class="w-4 h-4 rounded-full border-2 transition-all duration-300 relative flex items-center justify-center shadow-sm"
               :class="[
                 op.activeExecStatut === 'CLOTURE' ? 'border-green-500 bg-green-500' :
                 op.activeExecStatut === 'EN_COURS' ? 'border-blue-500 bg-blue-500 ring-4 ring-blue-100' :
                 op.activeExecStatut === 'EN_PAUSE' ? 'border-yellow-500 bg-yellow-400 ring-4 ring-yellow-50' :
                 'border-slate-300 bg-white'
               ]">
               <!-- Checkmark pour CLOTURE -->
               <svg v-if="op.activeExecStatut === 'CLOTURE'" class="w-2.5 h-2.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="4" d="M5 13l4 4L19 7"></path></svg>
               <!-- Point clignotant pour EN_COURS -->
               <div v-else-if="op.activeExecStatut === 'EN_COURS'" class="w-1.5 h-1.5 bg-white rounded-full animate-pulse"></div>
               <!-- Pause icon pour EN_PAUSE -->
               <svg v-else-if="op.activeExecStatut === 'EN_PAUSE'" class="w-2 h-2 text-white" fill="currentColor" viewBox="0 0 24 24"><path d="M6 19h4V5H6v14zm8-14v14h4V5h-4z"></path></svg>
          </div>
          
          <!-- Code Opération -->
          <span class="text-[9px] font-bold mt-2 text-center truncate max-w-[55px]"
                :class="[
                  op.activeExecStatut === 'EN_COURS' ? 'text-blue-700 font-extrabold' : 
                  op.activeExecStatut === 'CLOTURE' ? 'text-green-600' :
                  op.activeExecStatut === 'EN_PAUSE' ? 'text-yellow-600' :
                  'text-slate-400'
                ]">
            {{ op.operationCode }}
          </span>

          <!-- Tooltip au survol -->
          <div class="absolute bottom-full mb-2 left-1/2 -translate-x-1/2 px-3 py-1.5 bg-slate-800 text-white text-[11px] font-medium rounded-md opacity-0 group-hover:opacity-100 transition-all pointer-events-none whitespace-nowrap z-50 shadow-xl border border-slate-700">
            {{ op.libelle }}
            <div class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-800"></div>
          </div>
        </div>
      </div>
    </div>

    <div class="flex justify-between items-end border-t border-slate-100 pt-4 mt-auto">
      <div>
        <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Qté lancée</p>
        <p class="text-sm font-bold text-slate-700">{{ ofItem.quantiteLancee }} / {{ ofItem.quantitePrevue }}</p>
      </div>
      <div class="text-right">
        <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Date début</p>
        <p class="text-sm font-medium text-slate-600">{{ statutReel === 'EN COURS' && ofItem.dateDebut ? formatDate(ofItem.dateDebut) : '-' }}</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  ofItem: {
    type: Object,
    required: true
  }
});

defineEmits(['select']);

const statutReel = computed(() => {
  const of = props.ofItem;
  if (!of.gammeOperatoire || of.gammeOperatoire.length === 0) return 'NON COMMENCÉ';
  const aUneOperationEnCours = of.gammeOperatoire.some(
    op => op.activeExecControleOfId != null && op.activeExecStatut !== 'CLOTURE'
  );
  return aUneOperationEnCours ? 'EN COURS' : 'NON COMMENCÉ';
});

const formatDate = (dateStr) => {
  if (!dateStr) return 'N/A';
  const d = new Date(dateStr);
  return d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
};
</script>
