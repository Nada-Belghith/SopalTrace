<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    <div class="grid grid-cols-1 md:grid-cols-5 gap-4">

      <!-- FAMILLE (Obligatoire) -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Famille *</label>
        <select 
          v-model="store.entete.familleProduitCode" 
          :disabled="isEditMode || isReadOnly || isPiston" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly || isPiston) ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">       
          <option value="">-- Sélectionner --</option>
          <option v-for="fam in famillesFiltrees" :key="fam.code" :value="fam.code">
            {{ fam.code }}
          </option>
        </select>
      </div>

      <!-- OPÉRATION (Obligatoire) -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Opération *</label>
        <select 
          v-model="store.entete.operationCode" 
          :disabled="isEditMode || isReadOnly" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">       
          <option value="">-- Sélectionner --</option>
          <option v-for="op in operationsFiltrees" :key="op.code" :value="op.code">{{ op.code }} - {{ op.libelle }}</option>
        </select>
      </div>

      <!-- ARTICLE (Obligatoire) -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Article *</label>
        <select 
          v-model="store.entete.natureComposantCode" 
          :disabled="isEditMode || isReadOnly" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">        
          <option value="">-- Sélectionner --</option>
          <option v-for="nat in composantsFiltres" :key="nat.code" :value="nat.code">{{ nat.libelle }}</option>
        </select>
      </div>

      <!-- POSTE (Visible UNIQUEMENT si Famille=soupape ET Article=PF) -->
      <div v-if="afficherPoste">
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Poste</label>
        <select 
          v-model="store.entete.posteCode" 
          :disabled="isEditMode || isReadOnly" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'cursor-not-allowed bg-gray-100 border-slate-200 text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer']">       
          <option value="">-- Tous les postes --</option>
          <option v-for="p in postesDisponibles" :key="p.code" :value="p.code">
           {{ p.libelle }}
          </option>
        </select>
      </div>

      <!-- LIBELLÉ -->
      <div>
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Libellé du Gabarit *</label>
        <input 
          v-model="store.entete.libelle" 
          type="text" 
          placeholder="Ex: Modèle Standard Corps..." 
          :disabled="isEditMode || isReadOnly" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-300 text-slate-800']">
      </div>

    </div>
  </div>
</template>

<script setup>
import { computed, watch } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';

const store = useFabModeleStore();
const props = defineProps({
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
// LOGIQUE PRINCIPALE : AFFICHER POSTE SI FAMILLE=SOUPAPE ET ARTICLE=PF
// =========================================================================

const afficherPoste = computed(() => {
  const famille = String(store.entete.familleProduitCode || '').trim().toLowerCase();
  const article = String(store.entete.natureComposantCode || '').trim().toUpperCase();

  // Afficher Poste UNIQUEMENT si :
  // 1. Famille contient "soupape"
  // 2. ET Article = "PF"
  return famille.includes('soupape') && article === 'PF';
});

const isPiston = computed(() => {
  return String(store.entete.natureComposantCode || '').trim().toUpperCase() === 'PISTON';
});

// =========================================================================
// FILTRES DYNAMIQUES
// =========================================================================

const composantsFiltres = computed(() => {
  const toutesLesNatures = store.naturesComposant || [];
  const selectedOp = (store.entete.operationCode || '').trim().toUpperCase();
  const gammes = store.gammesOperatoires || [];

  if (!selectedOp || !gammes.length) return toutesLesNatures;

  // Filtrer les natures autorisées pour l'opération sélectionnée
  const naturesPermises = gammes
    .filter(g => (g.operationCode || '').trim().toUpperCase() === selectedOp)
    .map(g => (g.natureComposantCode || '').trim().toUpperCase());

  return toutesLesNatures.filter(n => naturesPermises.includes((n.code || '').trim().toUpperCase()));
});

const famillesFiltrees = computed(() => {
  const allFamilies = store.famillesProduit || [];
  if (isPiston.value) {
    // On ajoute 'TOUS' au début mais on laisse les autres familles accessibles
    return [{ code: 'TOUS' }, ...allFamilies];
  }
  return allFamilies;
});

const operationsFiltrees = computed(() => {
  const toutesLesOperations = store.operations || [];
  const selectedNature = (store.entete.natureComposantCode || '').trim().toUpperCase();
  const gammes = store.gammesOperatoires || [];

  if (!gammes.length) return toutesLesOperations;

  // Si une nature est sélectionnée, on filtre les opérations liées à cette nature
  if (selectedNature) {
    const opsPermises = gammes
      .filter(g => (g.natureComposantCode || '').trim().toUpperCase() === selectedNature)
      .map(g => (g.operationCode || '').trim().toUpperCase());
    
    return toutesLesOperations.filter(op => opsPermises.includes((op.code || '').trim().toUpperCase()));
  }

  // Sinon, on montre toutes les opérations qui existent dans les gammes (pour éviter les erreurs)
  const toutesOpsDansGammes = [...new Set(gammes.map(g => (g.operationCode || '').trim().toUpperCase()))];
  return toutesLesOperations.filter(op => toutesOpsDansGammes.includes((op.code || '').trim().toUpperCase()));
});

const postesDisponibles = computed(() =>
  (store.postes || [])
    .map(p => ({
      code: p.code || p.Code || p.codePoste || p.CodePoste,
      libelle: p.libelle || p.Libelle || p.designation || p.Designation
    }))
    .filter(p => p.code)
);

// =========================================================================
// WATCHERS POUR CASCADE DE RÉINITIALISATION
// =========================================================================

// Si Famille change → Réinitialise Opération, Article, Poste
watch(() => store.entete.familleProduitCode, (newVal, oldVal) => {
  if (newVal !== oldVal) {
    // Évite la boucle si c'est un auto-remplissage PISTON
    if (newVal === 'TOUS' && isPiston.value) return;

    store.entete.operationCode = '';
    store.entete.natureComposantCode = '';
    store.entete.posteCode = '';
  }
});

// Si Opération change → Réinitialise Article, Poste
watch(() => store.entete.operationCode, (newVal, oldVal) => {
  if (newVal !== oldVal) {
    // Évite la boucle si c'est un auto-remplissage PISTON
    if (newVal === 'ASS' && isPiston.value) return;

    store.entete.natureComposantCode = '';
    store.entete.posteCode = '';
  }
});

// Si Article change → Réinitialise Poste
watch(() => store.entete.natureComposantCode, (newVal, oldVal) => {
  if (newVal !== oldVal) {
    store.entete.posteCode = '';
    
    const isPistonNew = String(newVal || '').trim().toUpperCase() === 'PISTON';
    const isPistonOld = String(oldVal || '').trim().toUpperCase() === 'PISTON';

    // Règle spécifique PISTON : Force la famille et l'opération
    if (isPistonNew) {
      // Note: On utilise 'TOUS' ou 'ASS AUTO' selon ce qui est défini dans famillesFiltrees
      store.entete.familleProduitCode = 'TOUS'; 
      store.entete.operationCode = 'ASS';
    } else if (isPistonOld) {
      // Si on quitte le mode PISTON, on vide les champs forcés
      store.entete.familleProduitCode = '';
      store.entete.operationCode = '';
    }
  }
});

// Si condition Poste change (soupape check) → Réinitialise Poste
watch(afficherPoste, (val) => {
  if (!val) {
    store.entete.posteCode = '';
  }
});

// Auto-remplissage du code modèle
watch(() => store.codeModeleAuto, (newVal, oldVal) => {
  if (!props.isEditMode && (!store.entete.code || store.entete.code === oldVal)) {
    store.entete.code = newVal;
  }
}, { immediate: true });
</script>
