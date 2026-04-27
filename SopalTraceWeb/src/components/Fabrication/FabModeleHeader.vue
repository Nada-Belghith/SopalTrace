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
// FILTRES DYNAMIQUES BASÉS SUR LA GAMME OPÉRATOIRE (BDD)
// =========================================================================

// 1. Filtre les opérations possibles selon le composant sélectionné
const operationsFiltrees = computed(() => {
  const nat = store.entete.natureComposantCode;
  const toutesLesOperations = store.operations || [];
  const gammes = store.gammesOperatoires || [];

  if (!nat) return toutesLesOperations;

  // On cherche les opérations autorisées pour cette nature dans la table de gammes
  const operationsPermises = gammes
    .filter(g => (g.natureComposantCode || '').trim().toUpperCase() === nat.trim().toUpperCase())
    .map(g => (g.operationCode || '').trim().toUpperCase());
  
  if (operationsPermises.length > 0) {
    return toutesLesOperations.filter(op => operationsPermises.includes((op.code || '').trim().toUpperCase()));
  }

  // Fallback : Si aucune règle n'est définie en BDD, on affiche tout pour ne pas bloquer l'utilisateur
  return toutesLesOperations;
});

// 2. Filtre les composants possibles selon l'opération sélectionnée
const composantsFiltres = computed(() => {
  const op = store.entete.operationCode;
  const tousLesComposants = store.naturesComposant || [];
  const gammes = store.gammesOperatoires || [];

  if (!op) return tousLesComposants;

  // On cherche les natures autorisées pour cette opération dans la table de gammes
  const composantsPermis = gammes
    .filter(g => (g.operationCode || '').trim().toUpperCase() === op.trim().toUpperCase())
    .map(g => (g.natureComposantCode || '').trim().toUpperCase());
  
  if (composantsPermis.length > 0) {
    return tousLesComposants.filter(n => composantsPermis.includes((n.code || '').trim().toUpperCase()));
  }

  return tousLesComposants;
});

// =========================================================================
// SÉCURITÉS ET AFFECTATIONS AUTOMATIQUES (Réactives)
// =========================================================================

// Si on change l'opération, on vérifie si le composant actuel est toujours compatible via la BDD
watch(() => store.entete.operationCode, (newOp) => {
  const nat = store.entete.natureComposantCode;
  if (!newOp || !nat) return;

  const gammes = store.gammesOperatoires || [];
  // On vérifie si le couple (nat, newOp) existe dans la gamme
  const estCompatible = gammes.some(g => 
    (g.operationCode || '').trim().toUpperCase() === newOp.trim().toUpperCase() && 
    (g.natureComposantCode || '').trim().toUpperCase() === nat.trim().toUpperCase()
  );

  // Fallback : si aucune gamme n'est définie pour cette opération en BDD, on considère que c'est libre
  const opADesRegles = gammes.some(g => (g.operationCode || '').trim().toUpperCase() === newOp.trim().toUpperCase());

  if (opADesRegles && !estCompatible) {
    store.entete.natureComposantCode = '';
  }
});

// Si on change le composant, on vérifie l'opération et on tente une auto-sélection
watch(() => store.entete.natureComposantCode, (nouvelleNature, ancienneNature) => {
  const op = store.entete.operationCode;
  const gammes = store.gammesOperatoires || [];
  
  if (nouvelleNature) {
    const targetNature = nouvelleNature.trim().toUpperCase();
    
    // 1. Vérification de compatibilité de l'opération actuelle
    if (op) {
      const targetOp = op.trim().toUpperCase();
      const estCompatible = gammes.some(g => 
        (g.natureComposantCode || '').trim().toUpperCase() === targetNature && 
        (g.operationCode || '').trim().toUpperCase() === targetOp
      );
      
      const natADesRegles = gammes.some(g => (g.natureComposantCode || '').trim().toUpperCase() === targetNature);

      if (natADesRegles && !estCompatible) {
        store.entete.operationCode = ''; 
      }
    }

    // 2. AUTO-SÉLECTION : Si une SEULE opération est possible pour ce composant en BDD, on l'affecte
    if (!store.entete.operationCode) {
      const opsPossibles = gammes
        .filter(g => (g.natureComposantCode || '').trim().toUpperCase() === targetNature)
        .map(g => g.operationCode);
      
      if (opsPossibles.length === 1) {
        store.entete.operationCode = opsPossibles[0];
      }
    }
  }
  
  // LOGIQUE SPÉCIFIQUE (Non liée aux gammes)
  if (nouvelleNature === 'PISTON') {
    store.entete.typeRobinetCode = ''; 
  } else if (nouvelleNature === 'VOLANT') {
    store.entete.typeRobinetCode = 'MAN';
  } else if (ancienneNature === 'VOLANT' && ['CORPS', 'PF'].includes(nouvelleNature)) {
    store.entete.typeRobinetCode = '';
  }
});
</script>