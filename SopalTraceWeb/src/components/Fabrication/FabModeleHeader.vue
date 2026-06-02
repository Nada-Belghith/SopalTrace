<template>
  <div class="mb-10">
    <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mb-4">1. Informations générales</h3>
    
    <!-- CHOIX RÉFÉRENCE FORMULAIRE (Mode Assemblage Uniquement) -->
    <div v-if="isAssemblyMode && !isEditMode" class="col-span-full mb-4 bg-blue-50/50 border border-blue-200 p-4 rounded-xl flex flex-col md:flex-row items-start md:items-center gap-4">
      <label class="block text-[11px] font-black text-blue-800 uppercase tracking-widest shrink-0">
        <i class="pi pi-file-import mr-1 text-blue-600"></i> Réf. Formulaire (Modèle) *
      </label>
      <select 
        v-model="refFormulaireSelected" 
        :disabled="isReadOnly" 
        class="w-full md:w-1/3 rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow bg-white border border-slate-300 text-slate-800 cursor-pointer shadow-sm">
        <option value="">-- Choisir un formulaire générique --</option>
        <option v-for="ref in formulairesReferences" :key="ref.id" :value="ref.id">
          {{ ref.codeReference }} - {{ ref.designation }}
        </option>
      </select>
      <p class="text-xs text-blue-600/80 font-medium italic">
        La sélection du formulaire remplira automatiquement les champs suivants.
      </p>
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
          type="text" 
          placeholder="Ex: Modèle Standard Corps..." 
          :disabled="isEditMode || isReadOnly" 
          :class="['w-full rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow', (isEditMode || isReadOnly) ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-300 text-slate-800']">
      </div>

      <!-- CONFIGURATION COLONNES -->
      <div v-if="!isReadOnly" class="col-span-full mt-4 flex justify-end">
        <button @click="showColumnModal = true" class="bg-slate-800 hover:bg-slate-700 text-white px-4 py-2 rounded font-bold text-sm flex items-center gap-2 transition-colors">
          <i class="pi pi-sliders-h"></i> Configurer Colonnes
        </button>
      </div>
    </div>

    <!-- MODAL DE CONFIGURATION DES COLONNES -->
    <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
    />
  </div>
</template>

<script setup>
import { computed, watch, ref, nextTick } from 'vue';
import { useRoute } from 'vue-router';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';

const showColumnModal = ref(false);
const store = useFabModeleStore();
const route = useRoute();
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

const isAssemblyMode = computed(() => route.query.mode === 'assembly');
const formulairesReferences = computed(() => store.formulairesReferences || []);
const refFormulaireSelected = ref('');
const isAutoFilling = ref(false);

watch(refFormulaireSelected, async (newRefId) => {
  if (!newRefId) return;
  const refObj = formulairesReferences.value.find(r => r.id === newRefId);
  if (!refObj) return;

  const designation = refObj.designation || '';
  const d = designation.toUpperCase();

  isAutoFilling.value = true;

  // 1. Famille (doit être définie avant l'opération pour éviter des problèmes de filtre Vue)
  const validFamilies = [...(store.famillesProduit || [])].sort((a, b) => {
    const codeA = (a.code || '').length;
    const codeB = (b.code || '').length;
    return codeB - codeA; // Trier par longueur décroissante pour matcher le plus précis d'abord
  });
  
  const foundFamily = validFamilies.find(f => d.includes((f.code || '').toUpperCase()));
  if (foundFamily) {
     store.entete.familleProduitCode = foundFamily.code;
  } else if (d.includes('MANU') || d.includes('MANUELLE')) {
     const manuFamily = validFamilies.find(f => f.code === 'RBGFM');
     if (manuFamily) store.entete.familleProduitCode = manuFamily.code;
     else store.entete.familleProduitCode = '';
  } else {
     store.entete.familleProduitCode = '';
  }

  // 2. Article (natureComposantCode)
  if (d.includes('PISTON')) store.entete.natureComposantCode = 'PISTON';
  else if (d.includes('PF') || d.includes('RBGFA-BAC')) store.entete.natureComposantCode = 'PF';
  else store.entete.natureComposantCode = '';
  
  // 3. Opération
  if (d.includes('ASS') || d.includes('ASSEMBLAGE') || d.includes('PF')) {
    store.entete.operationCode = 'ASS';
  } else {
    store.entete.operationCode = '';
  }

  // 4. Poste (if any PASxx)
  const posteMatch = d.match(/PAS\d+/);
  if (posteMatch) {
    store.entete.posteCode = posteMatch[0];
  } else {
    store.entete.posteCode = '';
  }

  // Set the gabarit title exactly as the designation to keep it as reference
  store.entete.libelle = designation;

  // Libérer le flag après la propagation de la réactivité
  setTimeout(() => {
    isAutoFilling.value = false;
  }, 100);
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
  let toutesLesNatures = store.naturesComposant || [];
  
  const mode = route.query.mode;
  if (mode === 'assembly') {
    toutesLesNatures = toutesLesNatures.filter(n => {
      const code = (n.code || '').trim().toUpperCase();
      return code === 'PISTON' || code === 'PF';
    });
  } else if (mode === 'fabrication') {
    toutesLesNatures = toutesLesNatures.filter(n => {
      const code = (n.code || '').trim().toUpperCase();
      return code === 'CORPS' || code === 'VOLANT';
    });
  }

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

  let gammesFiltrees = gammes;
  const mode = route.query.mode;
  if (mode === 'assembly') {
    gammesFiltrees = gammes.filter(g => {
      const nat = (g.natureComposantCode || '').trim().toUpperCase();
      return nat === 'PISTON' || nat === 'PF';
    });
  } else if (mode === 'fabrication') {
    gammesFiltrees = gammes.filter(g => {
      const nat = (g.natureComposantCode || '').trim().toUpperCase();
      return nat === 'CORPS' || nat === 'VOLANT';
    });
  }

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
  if (isAutoFilling.value) return;
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
  if (isAutoFilling.value) return;
  if (newVal !== oldVal) {
    // Évite la boucle si c'est un auto-remplissage PISTON
    if (newVal === 'ASS' && isPiston.value) return;

    store.entete.natureComposantCode = '';
    store.entete.posteCode = '';
  }
});

// Si Article change → Réinitialise Poste
watch(() => store.entete.natureComposantCode, (newVal, oldVal) => {
  if (isAutoFilling.value) return;
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
