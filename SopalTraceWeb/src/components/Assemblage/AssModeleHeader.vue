<template>
  <div class="mb-10">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between mb-4 gap-3">
      <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest">1. Informations générales</h3>
      <div class="flex items-center gap-3">
        <slot name="actions"></slot>
      </div>
    </div>
    <!-- NOUVEAU DESIGN POUR REF FORMULAIRE -->
    <div v-if="!isEditMode" class="bg-blue-50 border border-blue-100 p-4 rounded-xl flex flex-col md:flex-row gap-4 mb-6 shadow-inner">
      <div class="flex items-center text-blue-800 font-black tracking-widest text-xs min-w-[150px]">
        <i class="pi pi-file-import mr-2 text-lg text-blue-600"></i> RÉF. FORMULAIRE *
      </div>
      <div class="flex-1 flex gap-4 items-center relative">
        <select 
          v-model="refFormulaireSelected" 
          :disabled="isReadOnly" 
          class="w-full md:w-1/2 rounded-lg px-4 py-2 text-sm font-bold shadow-sm focus:ring-2 focus:ring-blue-400 outline-none transition-shadow bg-white border border-blue-200 text-blue-900 cursor-pointer">
          <option value="">-- Choisir un formulaire générique --</option>
          <option v-for="ref in formulairesReferences" :key="ref.id" :value="ref.id">
            {{ ref.codeReference }} - {{ ref.designation }}
          </option>
        </select>
        <span class="text-xs font-bold text-blue-500 italic hidden md:block">
          La sélection du formulaire remplira automatiquement les champs suivants.
        </span>
      </div>
    </div>

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
          @blur="onLibelleBlur"
          type="text" 
          placeholder="Ex: Modèle Standard Corps..." 
          :disabled="isEditMode || isReadOnly" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-300 text-slate-800']">
      </div>

      <div class="pt-4">
        <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Version de départ</label>
        <input 
          v-model.number="store.entete.versionInitiale" 
          type="number" 
          min="0" 
          placeholder="Ex: 0"
          :disabled="isEditMode || isReadOnly" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-300 text-slate-800']">
      </div>

    </div>


  </div>
</template>

<script setup>
import { computed, watch, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useAssModeleStore } from '@/stores/assModeleStore';
import { parseDesignation } from '@/utils/designationParser';

const store = useAssModeleStore();
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

const formulairesReferences = computed(() => store.formulairesReferences || []);
const refFormulaireSelected = ref('');
const isAutoFilling = ref(false);

  watch(refFormulaireSelected, async (newRefId) => {
  if (!newRefId) return;
  const refObj = formulairesReferences.value.find(r => r.id === newRefId);
  if (!refObj) return;

  const designation = refObj.designation || '';
  isAutoFilling.value = true;

  const parsed = parseDesignation(designation, store.famillesProduit || [], [], store.postes || []);

  if (parsed.familleCode !== '') store.entete.familleProduitCode = parsed.familleCode;
  else store.entete.familleProduitCode = '';

  store.entete.natureComposantCode = parsed.natureComposantCode;
  store.entete.operationCode = parsed.operationCode;
  store.entete.posteCode = parsed.posteCode;

  // DO NOT overwrite libelle with the designation of the reference form
  // The user should type their own libelle for the model (e.g. Modèle MOD-TRONC-CORPS V1)

  // ✅ Mémoriser le codeReference du formulaire sélectionné pour le versioning ciblé
  store.entete.refFormulaireCodeReference = refObj.codeReference || '';
  
  // Appliquer la configuration des colonnes du formulaire sélectionné
  if (refObj.configurationStructureJson) {
    try {
      store.entete.configurationColonnes = typeof refObj.configurationStructureJson === 'string' 
        ? JSON.parse(refObj.configurationStructureJson) 
        : refObj.configurationStructureJson;
    } catch (e) {
      console.error("Erreur parsing configuration colonnes:", e);
      store.entete.configurationColonnes = [];
    }
  } else {
    store.entete.configurationColonnes = [];
  }

  // Libérer le flag après la propagation de la réactivité
  setTimeout(() => {
    isAutoFilling.value = false;
  }, 100);
});

const onLibelleBlur = () => {
  if (props.isReadOnly || props.isEditMode) return;
  const lib = store.entete.libelle || '';
  if (!lib) return;
  
  isAutoFilling.value = true;
  const parsed = parseDesignation(lib, store.famillesProduit || [], [], store.postes || []);
  
  if (parsed.familleCode) store.entete.familleProduitCode = parsed.familleCode;
  if (parsed.natureComposantCode) store.entete.natureComposantCode = parsed.natureComposantCode;
  if (parsed.operationCode) store.entete.operationCode = parsed.operationCode;
  if (parsed.posteCode) store.entete.posteCode = parsed.posteCode;
  
  setTimeout(() => {
    isAutoFilling.value = false;
  }, 100);
};

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
  let toutesLesNatures = store.naturesComposant || [];
  
  toutesLesNatures = toutesLesNatures.filter(n => {
    const code = (n.code || '').trim().toUpperCase();
    return code === 'PISTON' || code === 'PF';
  });

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

  let gammesFiltrees = gammes.filter(g => {
    const nat = (g.natureComposantCode || '').trim().toUpperCase();
    return nat === 'PISTON' || nat === 'PF';
  });

  // Si une nature est sélectionnée, on filtre les opérations liées à cette nature
  if (selectedNature) {
    const opsPermises = gammesFiltrees
      .filter(g => (g.natureComposantCode || '').trim().toUpperCase() === selectedNature)
      .map(g => (g.operationCode || '').trim().toUpperCase());
    
    return toutesLesOperations.filter(op => opsPermises.includes((op.code || '').trim().toUpperCase()));
  }

  // Sinon, on montre toutes les opérations qui existent dans les gammes filtrées
  const toutesOpsDansGammes = [...new Set(gammesFiltrees.map(g => (g.operationCode || '').trim().toUpperCase()))];
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
  if (isAutoFilling.value || store.isBeingLoaded) return;  // ✅ Pas de cascade pendant le chargement
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
  if (isAutoFilling.value || store.isBeingLoaded) return;  // ✅ Pas de cascade pendant le chargement
  if (newVal !== oldVal) {
    // Évite la boucle si c'est un auto-remplissage PISTON
    if (newVal === 'ASS' && isPiston.value) return;

    store.entete.natureComposantCode = '';
    store.entete.posteCode = '';
  }
});

// Si Article change → Réinitialise Poste
watch(() => store.entete.natureComposantCode, (newVal, oldVal) => {
  if (isAutoFilling.value || store.isBeingLoaded) return;  // ✅ Pas de cascade pendant le chargement
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
