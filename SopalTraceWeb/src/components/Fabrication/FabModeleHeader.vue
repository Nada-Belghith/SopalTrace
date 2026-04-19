<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      
      <!-- OPÉRATION (Obligatoire) -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Opération *</label>
        <select v-model="store.entete.operationCode" :disabled="isEditMode || isReadOnly" :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">       
          <option value="">-- Sélectionner --</option>
          <option v-for="op in operationsFiltrees" :key="op.code" :value="op.code">{{ op.code }} - {{ op.libelle }}</option>
        </select>
      </div>

      <!-- COMPOSANT (Obligatoire, filtré dynamiquement) -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Composant *</label>
        <select v-model="store.entete.natureComposantCode" :disabled="isEditMode || isReadOnly" :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">        
          <option value="">-- Sélectionner --</option>
          <option v-for="nat in composantsFiltres" :key="nat.code" :value="nat.code">{{ nat.libelle }}</option>
        </select>
      </div>

      <!-- TYPE ROBINET (Optionnel, Masqué si Piston) -->
      <div v-if="store.entete.natureComposantCode !== 'PISTON'">
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Type Robinet *</label>
        <select v-model="store.entete.typeRobinetCode" :disabled="isEditMode || isReadOnly" :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">       
          <option value="">-- Tous les types --</option>
          <option v-for="typ in (store.typesRobinet || [])" :key="typ.code" :value="typ.code">{{ typ.libelle }}</option>
        </select>
      </div>

      <!-- LIBELLÉ (Optionnel, pas d'étoile) -->
      <div :class="store.entete.natureComposantCode === 'PISTON' ? 'md:col-span-2' : ''">
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Libellé du Gabarit</label>
        <input v-model="store.entete.libelle" type="text" placeholder="Ex: Modèle Standard Corps..." :disabled="isReadOnly" :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-300 text-slate-800']">
      </div>

    </div>
  </div>
</template>

<script setup>
import { computed, watch } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';

const store = useFabModeleStore();
defineProps({
  isEditMode: {
    type: Boolean,
    default: false
  },
  isReadOnly: {
    type: Boolean,
    default: false
  }
});

// =========================================================================
// FILTRES INTELLIGENTS DATA-DRIVEN (Basés sur la BDD)
// =========================================================================

// 1. Filtre les opérations possibles selon le composant sélectionné
const operationsFiltrees = computed(() => {
  const nat = store.entete.natureComposantCode;
  const toutesLesOperations = store.operations || [];
  const gammes = store.gammesOperatoires || [];

  if (!nat) return toutesLesOperations;

  // On cherche dans la BDD quelles opérations sont liées à ce composant
  const operationsPermises = gammes.filter(g => g.natureComposantCode === nat).map(g => g.operationCode);
  
  return toutesLesOperations.filter(op => operationsPermises.includes(op.code));
});

// 2. Filtre les composants possibles selon l'opération sélectionnée
const composantsFiltres = computed(() => {
  const op = store.entete.operationCode;
  const tousLesComposants = store.naturesComposant || [];
  const gammes = store.gammesOperatoires || [];

  if (!op) return tousLesComposants;

  // On cherche dans la BDD quels composants sont liés à cette opération
  const composantsPermis = gammes.filter(g => g.operationCode === op).map(g => g.natureComposantCode);
  
  return tousLesComposants.filter(n => composantsPermis.includes(n.code));
});

// =========================================================================
// SÉCURITÉS ET AFFECTATIONS AUTOMATIQUES (Réactives)
// =========================================================================

// Si on change l'opération, on vérifie si le composant actuel est toujours compatible
watch(() => store.entete.operationCode, (newOp) => {
  const nat = store.entete.natureComposantCode;
  const gammes = store.gammesOperatoires || [];

  if (newOp && nat) {
    const estCompatible = gammes.some(g => g.operationCode === newOp && g.natureComposantCode === nat);
    if (!estCompatible) {
      store.entete.natureComposantCode = ''; // On vide le composant si la matrice BDD dit non
    }
  }
});

// Si on change le composant, on vérifie l'opération
watch(() => store.entete.natureComposantCode, (newNat) => {
  const op = store.entete.operationCode;
  const gammes = store.gammesOperatoires || [];
  
  if (newNat) {
    const operationsCompatibles = gammes.filter(g => g.natureComposantCode === newNat).map(g => g.operationCode);
    
    // 1. Si l'opération sélectionnée n'est pas compatible avec la BDD, on la vide
    if (op && !operationsCompatibles.includes(op)) {
      store.entete.operationCode = ''; 
    }

    // 2. AUTO-SÉLECTION : S'il n'y a qu'une seule opération possible dans la BDD, on la choisit tout de suite !
    // (ex: Si "PF" n'a que "ASS", la liste déroulante se met sur "ASS" toute seule)
    if (!store.entete.operationCode && operationsCompatibles.length === 1) {
      store.entete.operationCode = operationsCompatibles[0];
    }
  }
  
  // SI PISTON : On vide le Type de Robinet car la case disparaît
  if (newNat === 'PISTON') {
    store.entete.typeRobinetCode = ''; 
  }

  watch(() => store.entete.natureComposantCode, (nouvelleNature, ancienneNature) => {
  if (nouvelleNature === 'VOLANT') {
    // Si VOLANT est sélectionné, on force automatiquement le type de robinet sur MAN (Manuelle)
    store.entete.typeRobinetCode = 'MAN';
  } 
  else if (ancienneNature === 'VOLANT' && ['CORPS', 'PF'].includes(nouvelleNature)) {
    // Si on quitte VOLANT pour revenir sur CORPS ou PF, on vide le champ pour obliger l'utilisateur à choisir
    store.entete.typeRobinetCode = '';
  }
});
});
</script>