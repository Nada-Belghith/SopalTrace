<template>
  <div class="max-w-4xl mx-auto bg-white rounded-3xl shadow-2xl border border-slate-200 overflow-hidden mt-12 animate-in fade-in slide-in-from-bottom-8">
    <!-- HEADER -->
    <div class="bg-slate-900 p-10 text-white relative overflow-hidden">
      <div class="absolute -right-10 -top-10 text-white/5">
        <i class="pi pi-file-edit" style="font-size: 14rem;"></i>
      </div>
      <h1 class="text-3xl font-black tracking-tight relative z-10">Création d'un Plan de Fabrication</h1>
      <p class="text-slate-400 mt-2 text-lg relative z-10">Associez un gabarit ou clonez un plan existant pour un article précis.</p>
    </div>

    <!-- CONTENT -->
    <div class="p-10 space-y-10">
      
      <!-- 1. IDENTIFICATION DE L'ARTICLE -->
      <div class="bg-slate-50 p-8 rounded-2xl border border-slate-200">
        <h3 class="text-sm font-black text-slate-500 uppercase tracking-widest mb-6">
          <i class="pi pi-box mr-2"></i> 1. Identification de l'Article SAGE
        </h3>
        <div class="flex flex-col sm:flex-row gap-4 items-end">
          <div class="flex-1 w-full">
            <label class="block text-xs font-bold text-slate-700 uppercase mb-2">Code Article *</label>
            <div class="relative">
              <!-- Retrait de l'icône barcode, agrandissement de l'input et du padding -->
              <input 
                v-model="wizard.codeArticleSage.value" 
                @keydown.enter="wizard.verifierArticleERP()"
                type="text" 
                placeholder="Ex: 2576A01-1" 
                class="w-full px-5 py-4 bg-white border-2 border-slate-300 rounded-xl outline-none focus:border-blue-500 transition-all font-mono text-lg font-bold text-slate-800 uppercase shadow-sm"
              >
            </div>
          </div>
          <button 
            @click="wizard.verifierArticleERP()" 
            :disabled="!wizard.codeArticleSage.value || wizard.isCheckingArticle.value" 
            class="w-full sm:w-auto px-8 py-4 bg-slate-800 text-white rounded-xl font-bold text-lg hover:bg-slate-700 transition-all disabled:opacity-50 flex items-center justify-center gap-3 shadow-sm"
          >
            <i :class="wizard.isCheckingArticle.value ? 'pi pi-spin pi-spinner text-xl' : 'pi pi-search text-xl'"></i> Vérifier
          </button>
        </div>

        <div v-if="wizard.isArticleValid.value" class="mt-6 p-5 bg-emerald-50 border-2 border-emerald-200 rounded-xl animate-in fade-in space-y-4">
          <div class="flex items-center gap-4">
            <div class="w-10 h-10 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center shrink-0"><i class="pi pi-check text-xl"></i></div>
            <div class="flex-1 overflow-hidden">
              <p class="text-xs font-black text-emerald-600 uppercase tracking-widest">Article Identifié</p>
              <p class="text-lg font-bold text-slate-800 truncate mt-1">{{ wizard.designationArticle.value }}</p>
            </div>
          </div>
          
          <!-- Article Details (Read-only) -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mt-4 bg-white/70 p-4 rounded-lg">
            <div>
              <p class="text-xs font-bold text-slate-600 uppercase mb-1">Type Robinet</p>
              <p class="text-base font-semibold text-slate-800 bg-white px-3 py-2 rounded">{{ wizard.getLibelleType ? wizard.getLibelleType(wizard.typeRobinetCode.value) : wizard.typeRobinetCode.value || '--' }}</p>
            </div>
            <div>
              <p class="text-xs font-bold text-slate-600 uppercase mb-1">Nature Composant</p>
              <p class="text-base font-semibold text-slate-800 bg-white px-3 py-2 rounded">{{ wizard.getLibelleNature ? wizard.getLibelleNature(wizard.natureComposantCode.value) : wizard.natureComposantCode.value || '--' }}</p>
            </div>
          </div>
        </div>

        <!-- MESSAGE BLOCAGE POUR ARTICLE GÉNÉRIQUE -->
        <div v-if="wizard.isGenerique.value" class="mt-6 p-6 bg-blue-50 border-2 border-blue-300 rounded-xl animate-in fade-in flex gap-4">
          <div class="shrink-0">
            <i class="pi pi-info-circle text-3xl text-blue-600"></i>
          </div>
          <div>
            <p class="text-sm font-black text-blue-600 uppercase tracking-widest mb-2">Article Générique</p>
            <p class="text-base font-semibold text-blue-800">La nature de cet article est pilotée par un plan de contrôle Maître (Générique). Il n'est pas nécessaire d'instancier un plan de fabrication spécifique.</p>
          </div>
        </div>
      </div>

      <!-- 2. CHOIX DE L'OPÉRATION (Affiché si article valide ET NON générique) -->
      <div v-if="wizard.isArticleValid.value && !wizard.isGenerique.value" class="animate-in fade-in slide-in-from-bottom-4 pt-4 border-t border-slate-100">
        <h3 class="text-sm font-black text-slate-500 uppercase tracking-widest mb-6">
          <i class="pi pi-cog mr-2"></i> 2. Choix de l'opération
        </h3>
        <div class="flex flex-col sm:flex-row gap-4 items-center">
          <!-- Si une seule opération, l'afficher directement -->
          <div v-if="wizard.operationsFiltrees.value.length === 1" class="w-full sm:w-1/2">
            <div class="p-4 bg-white border-2 border-slate-300 rounded-xl text-lg font-bold text-slate-800 shadow-sm">
              {{ wizard.operationsFiltrees.value[0].libelle || wizard.operationsFiltrees.value[0].code }}
            </div>
          </div>
          <!-- Sinon afficher un dropdown -->
          <select 
            v-else
            v-model="wizard.operationCode.value" 
            class="w-full sm:w-1/2 p-4 bg-white border-2 border-slate-300 rounded-xl outline-none focus:border-blue-500 text-lg font-bold text-slate-800 shadow-sm cursor-pointer transition-colors hover:bg-slate-50"
          >
            <option value="" disabled>-- Sélectionner l'opération --</option>
            <option v-for="op in wizard.operationsFiltrees.value" :key="op.code" :value="op.code">
              {{ op.libelle || op.code }}
            </option>
          </select>
          <div v-if="wizard.operationCode.value" class="hidden sm:flex items-center gap-2 text-sm font-bold text-blue-600 bg-blue-50 px-4 py-3 rounded-xl border border-blue-100">
             <i class="pi pi-info-circle"></i> Opération validée
          </div>
        </div>
      </div>

      <!-- 3. MÉTHODE DE CRÉATION (Bloqué si l'opération n'est pas choisie OU si article générique) -->
      <div v-if="!wizard.isGenerique.value" :class="wizard.operationCode.value ? 'opacity-100' : 'opacity-40 pointer-events-none'" class="transition-opacity duration-300 pt-4 border-t border-slate-100">
        <h3 class="text-sm font-black text-slate-500 uppercase tracking-widest mb-6">
          <i class="pi pi-sitemap mr-2"></i> 3. Méthode de création
        </h3>
        
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-6 mb-8">
          <!-- Option 1: Vierge -->
          <label 
            :class="wizard.sourceType.value === 'VIERGE' ? 'border-purple-500 bg-purple-50 ring-4 ring-purple-500/20' : 'border-slate-200 hover:bg-slate-50'" 
            class="p-6 rounded-2xl border-2 cursor-pointer transition-all flex flex-col items-center text-center gap-3"
          >
            <input type="radio" v-model="wizard.sourceType.value" value="VIERGE" class="hidden">
            <i class="pi pi-file text-3xl" :class="wizard.sourceType.value === 'VIERGE' ? 'text-purple-600' : 'text-slate-400'"></i>
            <div>
              <p class="font-bold text-slate-800 text-lg">Plan Vierge</p>
              <p class="text-xs text-slate-500 mt-2">Partir de zéro (vide)</p>
            </div>
          </label>

          <!-- Option 2: From Template (Modèle) -->
          <label 
            @click="handleModeleCardClick" 
            :class="[
              wizard.sourceType.value === 'MODELE' ? 'border-blue-500 bg-blue-50 ring-4 ring-blue-500/20' : 'border-slate-200 hover:bg-slate-50',
              (wizard.isGenerating?.value || wizard.isGeneratingPlan?.value) ? 'opacity-60 cursor-not-allowed' : ''
            ]" 
            class="p-6 rounded-2xl border-2 cursor-pointer transition-all flex flex-col items-center text-center gap-3"
          >
            <input type="radio" v-model="wizard.sourceType.value" value="MODELE" class="hidden">
            <i :class="[
              'pi pi-window-maximize text-3xl',
              wizard.sourceType.value === 'MODELE' ? 'text-blue-600' : 'text-slate-400',
              (wizard.isGenerating?.value || wizard.isGeneratingPlan?.value) ? 'pi-spin' : ''
            ]"></i>
            <div>
              <p class="font-bold text-slate-800 text-lg">Depuis un Modèle</p>
              <p class="text-xs text-slate-500 mt-2">Structure vierge à remplir</p>
            </div>
          </label>

          <!-- Option 3: Clone Existing -->
          <label :class="wizard.sourceType.value === 'CLONE' ? 'border-emerald-500 bg-emerald-50 ring-4 ring-emerald-500/20' : 'border-slate-200 hover:bg-slate-50'" class="p-6 rounded-2xl border-2 cursor-pointer transition-all flex flex-col items-center text-center gap-3">
            <input type="radio" v-model="wizard.sourceType.value" value="CLONE" class="hidden">
            <i class="pi pi-copy text-3xl" :class="wizard.sourceType.value === 'CLONE' ? 'text-emerald-600' : 'text-slate-400'"></i>
            <div>
              <p class="font-bold text-slate-800 text-lg">Cloner un Plan Existant</p>
              <p class="text-xs text-slate-500 mt-2">Récupère aussi les tolérances depuis n'importe quel article</p>
            </div>
          </label>
        </div>

        <!-- SOURCE SELECTION -->
        <!-- VIERGE -->
        <div v-if="wizard.sourceType.value === 'VIERGE'" class="animate-in fade-in">
          <div class="p-6 bg-purple-50 border border-purple-200 rounded-xl text-purple-800 text-sm mb-4">
            <i class="pi pi-info-circle text-xl mb-2 block"></i>
            Vous allez créer un plan de contrôle totalement vide. Vous devrez configurer manuellement l'intégralité de la structure (sections, contrôles).
          </div>
          <button 
            @click="$emit('load-model', { wizard })"
            :disabled="!wizard.canGeneratePlan()"
            class="w-full px-6 py-3 bg-purple-600 text-white rounded-lg font-black uppercase tracking-widest hover:bg-purple-700 transition-all shadow-lg flex items-center justify-center gap-3 text-sm"
          >
            <i class="pi pi-file-edit"></i>
            Créer le Plan Vierge
          </button>
        </div>

        <div v-if="wizard.sourceType.value === 'MODELE'" class="animate-in fade-in">
          <!-- Si un seul modèle disponible, l'afficher et le sélectionner automatiquement -->
          <label v-if="wizard.availableModeles.value.length === 1" class="block text-xs font-bold text-slate-700 uppercase mb-2">Choisir le Modèle de base *</label>
          <label v-else class="block text-xs font-bold text-slate-700 uppercase mb-2">Choisir le Modèle de base *</label>
          
          <div v-if="wizard.availableModeles.value.length === 0" class="p-4 bg-amber-50 border border-amber-200 rounded-xl text-amber-800 text-sm">
            <i class="pi pi-info-circle mr-2"></i>
            <span v-if="!wizard.isArticleValid.value">Veuillez d'abord vérifier l'article.</span>
            <span v-else>Aucun modèle disponible pour cette combinaison article.</span>
          </div>
          
          <div v-else-if="wizard.availableModeles.value.length === 1" class="p-4 bg-white border-2 border-slate-300 rounded-xl text-base font-semibold text-slate-700 shadow-sm">
            {{ wizard.availableModeles.value[0].code }} - {{ wizard.availableModeles.value[0].libelle || wizard.availableModeles.value[0].designation }}
          </div>
          
          <select 
            v-else
            v-model="wizard.selectedSourceId.value" 
            class="w-full p-4 bg-white border-2 border-slate-300 rounded-xl outline-none focus:border-blue-500 text-base font-semibold text-slate-700 shadow-sm cursor-pointer"
          >
            <option :value="null" disabled>-- Sélectionner le Modèle dans la liste --</option>
            <option v-for="modele in wizard.availableModeles.value" :key="modele.id" :value="modele.id">
              {{ modele.code }} - {{ modele.libelle || modele.designation }}
            </option>
          </select>

        </div>

        <div v-if="wizard.sourceType.value === 'CLONE'" class="animate-in fade-in">
          <label class="block text-xs font-bold text-slate-700 uppercase mb-2">Choisir le Plan à dupliquer *</label>
          
          <div v-if="wizard.availablePlans.value.length === 0" class="p-4 bg-amber-50 border border-amber-200 rounded-xl text-amber-800 text-sm">
            <i class="pi pi-info-circle mr-2"></i>
            <span v-if="!wizard.isArticleValid.value">Veuillez d'abord vérifier l'article.</span>
            <span v-else>Aucun plan disponible pour cette combinaison article.</span>
          </div>
          
          <!-- Si un seul plan disponible, l'afficher directement -->
          <div v-else-if="wizard.availablePlans.value.length === 1" class="p-4 bg-white border-2 border-slate-300 rounded-xl text-base font-semibold text-slate-700 shadow-sm">
            {{ wizard.availablePlans.value[0].nom }} - v{{ wizard.availablePlans.value[0].versionActuelle }} ({{ wizard.availablePlans.value[0].codeArticleSage }} - {{ wizard.availablePlans.value[0].designation }})
          </div>
          
          <select 
            v-else
            v-model="wizard.selectedSourceId.value" 
            class="w-full p-4 bg-white border-2 border-slate-300 rounded-xl outline-none focus:border-emerald-500 text-base font-semibold text-slate-700 shadow-sm cursor-pointer"
          >
            <option :value="null" disabled>-- Sélectionner le Plan dans la liste --</option>
            <option v-for="plan in wizard.availablePlans.value" :key="plan.id" :value="plan.id">
              {{ plan.nom }} - v{{ plan.versionActuelle }} ({{ plan.codeArticleSage }} - {{ plan.designation }})
            </option>
          </select>

          <!-- Bouton Cloner le Plan -->
          <button 
            v-if="wizard.availablePlans.value.length > 0"
            @click="$emit('load-model', { wizard })"
            :disabled="!wizard.selectedSourceId.value"
            class="w-full mt-4 px-6 py-3 bg-emerald-600 text-white rounded-lg font-black uppercase tracking-widest hover:bg-emerald-700 transition-all disabled:opacity-50 disabled:cursor-not-allowed shadow-lg flex items-center justify-center gap-3 text-sm"
          >
            <i class="pi pi-copy"></i>
            Cloner le Plan
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { nextTick, watch } from 'vue';

const props = defineProps({
  wizard: {
    type: Object,
    required: true
  }
});
const wizard = props.wizard;
const emit = defineEmits(['load-model']);

// Clicking the MODELE card triggers generation only when a modele is selected
const handleModeleCardClick = async () => {
  // Only emit if:
  // - a modele is selected
  // - an operation is selected  
  // - not already generating
  const isGenerate = wizard.isGenerating?.value || wizard.isGeneratingPlan?.value;
  if (!isGenerate && wizard.selectedSourceId?.value && wizard.operationCode?.value) {
    await nextTick();
    emit('load-model', { wizard });
  }
};

// If there is exactly one modele available, pre-select it (no auto-generation)
watch(() => wizard.availableModeles.value, (list) => {
  if (Array.isArray(list) && list.length === 1 && !wizard.selectedSourceId?.value) {
    wizard.selectedSourceId.value = list[0].id;
  }
});
</script>
