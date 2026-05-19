<template>
  <div class="max-w-3xl mx-auto bg-white rounded-2xl shadow-md border border-slate-200 mt-6 animate-in fade-in duration-300">
    <!-- HEADER -->
    <div class="bg-slate-900 px-8 py-6 text-white relative overflow-hidden rounded-t-2xl">
      <div class="absolute -right-6 -top-6 text-white/5">
        <i class="pi pi-file-edit" style="font-size: 8rem;"></i>
      </div>
      <h1 class="text-xl font-black tracking-tight relative z-10">Création d'un Plan de Fabrication</h1>
      <p class="text-slate-400 mt-1 text-xs relative z-10">Associez un gabarit ou clonez un plan existant pour un article précis.</p>
    </div>

    <!-- CONTENT -->
    <div class="p-6 md:p-8 space-y-6">
      
      <!-- 1. IDENTIFICATION DE L'ARTICLE -->
      <div class="bg-slate-50 p-5 rounded-xl border border-slate-200">
        <h3 class="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-4">
          <i class="pi pi-box mr-1.5"></i> 1. Identification de l'Article SAGE
        </h3>
        <div class="flex flex-col sm:flex-row gap-3 items-end">
          <div class="flex-1 w-full">
            <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Code Article *</label>
            <div class="relative">
              <input 
                v-model="wizard.codeArticleSage.value" 
                @keydown.enter="wizard.verifierArticleERP()"
                type="text" 
                placeholder="Ex: 2576A01-1" 
                class="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-all font-mono text-sm font-bold text-slate-800 uppercase shadow-sm h-[42px]"
              >
            </div>
          </div>
          <button 
            @click="wizard.verifierArticleERP()" 
            :disabled="!wizard.codeArticleSage.value || wizard.isCheckingArticle.value" 
            class="w-full sm:w-auto px-5 py-2.5 bg-slate-800 text-white rounded-lg font-bold text-xs hover:bg-slate-700 transition-all disabled:opacity-50 flex items-center justify-center gap-2 shadow-sm shrink-0 h-[42px]"
          >
            <i :class="wizard.isCheckingArticle.value ? 'pi pi-spin pi-spinner' : 'pi pi-search'"></i> Vérifier
          </button>
        </div>

        <div v-if="wizard.isArticleValid.value" class="mt-4 p-4 bg-emerald-50 border border-emerald-200 rounded-xl animate-in fade-in space-y-3">
          <div class="flex items-center gap-3">
            <div class="w-8 h-8 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center shrink-0">
              <i class="pi pi-check text-sm"></i>
            </div>
            <div class="flex-1 overflow-hidden">
              <p class="text-[9px] font-black text-emerald-600 uppercase tracking-widest">Article Identifié</p>
              <p class="text-sm font-bold text-slate-800 truncate mt-0.5">{{ wizard.designationArticle.value }}</p>
            </div>
          </div>
          
          <!-- Article Details (Read-only) -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2 bg-white/70 p-3 rounded-lg">
            <div>
              <p class="text-[10px] font-bold text-slate-600 uppercase mb-1">Famille de Robinet</p>
              <p class="text-xs font-semibold text-slate-800 bg-white px-2.5 py-1.5 rounded border border-slate-200/80">{{ wizard.getLibelleType ? wizard.getLibelleType(wizard.typeRobinetCode.value) : wizard.typeRobinetCode.value || '--' }}</p>
            </div>
            <div>
              <p class="text-[10px] font-bold text-slate-600 uppercase mb-1">Nature Article</p>
              <p class="text-xs font-semibold text-slate-800 bg-white px-2.5 py-1.5 rounded border border-slate-200/80">{{ wizard.getLibelleNature ? wizard.getLibelleNature(wizard.natureComposantCode.value) : wizard.natureComposantCode.value || '--' }}</p>
            </div>
          </div>
        </div>

        <!-- MESSAGE BLOCAGE POUR ARTICLE GÉNÉRIQUE -->
        <div v-if="wizard.isGenerique.value" class="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-xl animate-in fade-in flex gap-3">
          <div class="shrink-0 mt-0.5">
            <i class="pi pi-info-circle text-lg text-blue-600"></i>
          </div>
          <div>
            <p class="text-[10px] font-black text-blue-600 uppercase tracking-widest mb-1">Article Générique</p>
            <p class="text-xs font-semibold text-blue-800 leading-relaxed">La nature de cet article est pilotée par un plan de contrôle Maître (Générique). Il n'est pas nécessaire d'instancier un plan de fabrication spécifique.</p>
          </div>
        </div>

        <!-- MESSAGE BLOCAGE POUR PISTON / PF -->
        <div v-if="wizard.isPlanCreationBlocked.value" class="mt-4 p-4 bg-amber-50 border border-amber-200 rounded-xl animate-in fade-in flex gap-3">
          <div class="shrink-0 mt-0.5">
            <i class="pi pi-exclamation-circle text-lg text-amber-600"></i>
          </div>
          <div>
            <p class="text-[10px] font-black text-amber-600 uppercase tracking-widest mb-1">Plan par article non autorisé</p>
            <p class="text-xs font-semibold text-amber-800 leading-relaxed">Les articles de nature <strong>PISTON</strong> ou <strong>PF</strong> ne disposent pas de plans de contrôle par article. La création est donc bloquée pour ce code.</p>
          </div>
        </div>
      </div>

      <!-- 2. CHOIX DE L'OPÉRATION (Affiché si article valide ET NON générique ET NON bloqué) -->
      <div v-if="wizard.isArticleValid.value && !wizard.isGenerique.value && !wizard.isPlanCreationBlocked.value" class="animate-in fade-in slide-in-from-bottom-4 pt-4 border-t border-slate-100">
        <h3 class="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-4">
          <i class="pi pi-cog mr-1.5"></i> 2. Choix de l'opération
        </h3>
        <div class="flex flex-col sm:flex-row gap-3 items-center">
          <!-- Si une seule opération, l'afficher directement -->
          <div v-if="wizard.operationsFiltrees.value.length === 1" class="w-full sm:w-1/2">
            <div class="px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg text-sm font-semibold text-slate-800 shadow-sm h-[42px] flex items-center">
              {{ wizard.operationsFiltrees.value[0].libelle || wizard.operationsFiltrees.value[0].code }}
            </div>
          </div>
          <!-- Sinon afficher un dropdown -->
          <select 
            v-else
            v-model="wizard.operationCode.value" 
            class="w-full sm:w-1/2 px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-sm font-semibold text-slate-800 shadow-sm cursor-pointer transition-colors hover:bg-slate-50 h-[42px]"
          >
            <option value="" disabled>-- Sélectionner l'opération --</option>
            <option v-for="op in wizard.operationsFiltrees.value" :key="op.code" :value="op.code">
              {{ op.libelle || op.code }}
            </option>
          </select>
          <div v-if="wizard.operationCode.value" class="hidden sm:flex items-center gap-1.5 text-xs font-bold text-blue-600 bg-blue-50 px-3.5 py-2.5 rounded-lg border border-blue-100 h-[42px]">
             <i class="pi pi-info-circle"></i> Opération validée
          </div>
        </div>
      </div>

      <!-- 2b. CHOIX DU POSTE (uniquement pour les articles qui le nécessitent, ex: Auto Soupape PF) -->
      <div
        v-if="wizard.isArticleValid.value && !wizard.isGenerique.value && wizard.operationCode.value && wizard.requiertPoste?.value"
        class="animate-in fade-in slide-in-from-bottom-4 pt-4 border-t border-slate-100"
      >
        <h3 class="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-4">
          <i class="pi pi-wrench mr-1.5"></i> 2b. Choix du poste de travail
        </h3>
        <p class="text-xs text-slate-500 mb-3 leading-relaxed">
          Ce type d'article nécessite de préciser le poste de fabrication, car le plan de contrôle varie selon le poste.
        </p>
        <div class="max-w-md">
          <select
            v-model="wizard.posteCode.value"
            class="w-full sm:w-1/2 px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-sm font-semibold text-slate-800 shadow-sm cursor-pointer transition-colors hover:bg-slate-50 h-[42px]"
          >
            <option value="">-- Choisir un poste --</option>
            <option v-for="poste in wizard.postesDisponibles.value" :key="poste.code" :value="poste.code">
              {{ poste.code }} - {{ poste.libelle || poste.code }}
            </option>
          </select>
        </div>
        <p v-if="!wizard.posteCode.value" class="mt-2 text-[11px] text-amber-600 font-bold flex items-center gap-1">
          <i class="pi pi-exclamation-triangle"></i> Veuillez sélectionner un poste pour continuer.
        </p>
      </div>

      <!-- 2c. CHOIX DE LA FAMILLE (Uniquement pour PF) -->
      <div
        v-if="wizard.isArticleValid.value && !wizard.isGenerique.value && wizard.operationCode.value && wizard.requiertFamille?.value"
        class="animate-in fade-in slide-in-from-bottom-4 pt-4 border-t border-slate-100"
      >
        <h3 class="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-4">
          <i class="pi pi-tags mr-1.5"></i> 2c. Choix de la famille d'article
        </h3>
        <p class="text-xs text-slate-500 mb-3 leading-relaxed">
          Pour les produits finis (PF), veuillez préciser la famille de produits associée.
        </p>
        <div class="max-w-md">
          <select
            v-model="wizard.familleCode.value"
            class="w-full sm:w-1/2 px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-sm font-semibold text-slate-800 shadow-sm cursor-pointer transition-colors hover:bg-slate-50 h-[42px]"
          >
            <option value="">-- Choisir une famille --</option>
            <option v-for="famille in wizard.famillesFiltrees.value" :key="famille.code" :value="famille.code">
              {{ famille.code }}
            </option>
          </select>
        </div>
        <p v-if="!wizard.familleCode.value" class="mt-2 text-[11px] text-amber-600 font-bold flex items-center gap-1">
          <i class="pi pi-exclamation-triangle"></i> Veuillez sélectionner une famille pour continuer.
        </p>
      </div>

      <!-- 3. MÉTHODE DE CRÉATION (Bloqué si l'opération n'est pas choisie OU si article générique/bloqué) -->
      <div v-if="!wizard.isGenerique.value && !wizard.isPlanCreationBlocked.value" :class="(wizard.operationCode.value && (!wizard.requiertPoste?.value || wizard.posteCode?.value) && (!wizard.requiertFamille?.value || wizard.familleCode?.value)) ? 'opacity-100' : 'opacity-40 pointer-events-none'" class="transition-opacity duration-300 pt-4 border-t border-slate-100">
        <h3 class="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-4">
          <i class="pi pi-sitemap mr-1.5"></i> 3. Méthode de création
        </h3>

        <!-- Méthodes principales: 3 options -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">

          <!-- Option 1: From Template (Modèle) -->
          <label
            @click="handleModeleCardClick"
            :class="[
              wizard.sourceType.value === 'MODELE' ? 'border-blue-500 bg-blue-50/50 ring-2 ring-blue-500/10' : 'border-slate-200 hover:bg-slate-50',
              (wizard.isGenerating?.value || wizard.isGeneratingPlan?.value) ? 'opacity-60 cursor-not-allowed' : ''
            ]"
            class="p-4 rounded-xl border cursor-pointer transition-all flex flex-col items-center text-center gap-2 bg-slate-50/30"
          >
            <input type="radio" v-model="wizard.sourceType.value" value="MODELE" class="hidden">
            <i :class="[
              'pi pi-window-maximize text-xl',
              wizard.sourceType.value === 'MODELE' ? 'text-blue-600' : 'text-slate-400',
              (wizard.isGenerating?.value || wizard.isGeneratingPlan?.value) ? 'pi-spin' : ''
            ]"></i>
            <div>
              <p class="font-bold text-slate-800 text-sm">Depuis un Modèle</p>
              <p class="text-[10px] text-slate-500 mt-1">Structure prédéfinie à remplir</p>
            </div>
          </label>

          <!-- Option 2: Clone Existing -->
          <label 
            :class="wizard.sourceType.value === 'CLONE' ? 'border-emerald-500 bg-emerald-50/50 ring-2 ring-emerald-500/10' : 'border-slate-200 hover:bg-slate-50'" 
            class="p-4 rounded-xl border cursor-pointer transition-all flex flex-col items-center text-center gap-2 bg-slate-50/30"
          >
            <input type="radio" v-model="wizard.sourceType.value" value="CLONE" class="hidden">
            <i class="pi pi-copy text-xl" :class="wizard.sourceType.value === 'CLONE' ? 'text-emerald-600' : 'text-slate-400'"></i>
            <div>
              <p class="font-bold text-slate-800 text-sm">Cloner un Plan</p>
              <p class="text-[10px] text-slate-500 mt-1">Copier tolérances d'un plan existant</p>
            </div>
          </label>

          <!-- Option 3: Import Excel -->
          <input type="file" ref="excelFileInput" @change="$emit('excel-selected', $event)" accept=".xlsx,.csv" class="hidden" />
          <label
            @click.prevent="handleExcelCardClick"
            :class="wizard.sourceType.value === 'EXCEL' ? 'border-orange-500 bg-orange-50/50 ring-2 ring-orange-500/10' : 'border-slate-200 hover:bg-slate-50'"
            class="p-4 rounded-xl border cursor-pointer transition-all flex flex-col items-center text-center gap-2 bg-slate-50/30"
          >
            <input type="radio" v-model="wizard.sourceType.value" value="EXCEL" class="hidden">
            <i class="pi pi-file-excel text-xl" :class="wizard.sourceType.value === 'EXCEL' ? 'text-orange-600' : 'text-slate-400'"></i>
            <div>
              <p class="font-bold text-slate-800 text-sm">Importer Excel</p>
              <p class="text-[10px] text-slate-500 mt-1">Charger la structure depuis un fichier</p>
            </div>
          </label>
        </div>

        <!-- Panneaux de détail selon la méthode choisie -->
        <div v-if="wizard.sourceType.value === 'MODELE'" class="animate-in fade-in">
          <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Choisir le Modèle de base *</label>

          <div v-if="wizard.availableModeles.value.length === 0" class="p-3 bg-amber-50 border border-amber-200 rounded-xl text-amber-800 text-xs">
            <i class="pi pi-info-circle mr-1.5"></i>
            <span v-if="!wizard.isArticleValid.value">Veuillez d'abord vérifier l'article.</span>
            <span v-else>Aucun modèle disponible pour cette combinaison article.</span>
          </div>

          <div v-else-if="wizard.availableModeles.value.length === 1" class="px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg text-sm font-semibold text-slate-700 shadow-sm h-[42px] flex items-center">
            {{ wizard.availableModeles.value[0].code }} - {{ wizard.availableModeles.value[0].libelle || wizard.availableModeles.value[0].designation }}
          </div>

          <select
            v-else
            v-model="wizard.selectedSourceId.value"
            class="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-sm font-semibold text-slate-700 shadow-sm cursor-pointer transition-colors hover:bg-slate-50 h-[42px]"
          >
            <option :value="null" disabled>-- Sélectionner le Modèle ({{ wizard.availableModeles.value.length }} trouvés) --</option>
            <option v-for="modele in wizard.availableModeles.value" :key="modele.id" :value="modele.id">
              {{ modele.libelle || modele.code }} ({{ modele.code }})
            </option>
          </select>
        </div>

        <div v-if="wizard.sourceType.value === 'CLONE'" class="animate-in fade-in">
          <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Choisir le Plan à dupliquer *</label>

          <div v-if="wizard.availablePlans.value.length === 0" class="p-3 bg-amber-50 border border-amber-200 rounded-xl text-amber-800 text-xs">
            <i class="pi pi-info-circle mr-1.5"></i>
            <span v-if="!wizard.isArticleValid.value">Veuillez d'abord vérifier l'article.</span>
            <span v-else>Aucun plan disponible pour cette combinaison article.</span>
          </div>

          <div v-else-if="wizard.availablePlans.value.length === 1" class="px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg text-sm font-semibold text-slate-700 shadow-sm h-[42px] flex items-center">
            {{ wizard.availablePlans.value[0].nom || 'Sans Nom' }} - v{{ wizard.availablePlans.value[0].version }} ({{ wizard.availablePlans.value[0].codeArticleSage }} - {{ wizard.availablePlans.value[0].designation }})
          </div>

          <select
            v-else
            v-model="wizard.selectedSourceId.value"
            class="w-full px-3.5 py-2.5 bg-white border border-slate-300 rounded-lg outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 text-sm font-semibold text-slate-700 shadow-sm cursor-pointer transition-colors hover:bg-slate-50 h-[42px]"
          >
            <option :value="null" disabled>-- Sélectionner le Plan ({{ wizard.availablePlans.value.length }} trouvés) --</option>
            <option v-for="plan in wizard.availablePlans.value" :key="plan.id" :value="plan.id">
              {{ plan.nom || plan.codeArticleSage }} - v{{ plan.version }}
            </option>
          </select>

          <button
            v-if="wizard.availablePlans.value.length > 0"
            @click="$emit('load-model', { wizard })"
            :disabled="!wizard.selectedSourceId.value || wizard.isGenerating.value"
            class="w-full mt-3 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-bold uppercase tracking-widest transition-all disabled:opacity-50 disabled:cursor-not-allowed shadow-md flex items-center justify-center gap-2 text-[10px] h-[42px]"
          >
            <i :class="wizard.isGenerating.value ? 'pi pi-spin pi-spinner' : 'pi pi-copy'"></i>
            {{ wizard.isGenerating.value ? 'Clonage en cours...' : 'Cloner le Plan' }}
          </button>
        </div>

        <!-- Option secondaire : Plan Vierge -->
        <div class="mt-6 pt-5 border-t border-dashed border-slate-200 text-center">
          <p class="text-[9px] text-slate-400 mb-2.5 uppercase tracking-widest font-bold">Option avancée</p>
          <button
            @click="wizard.sourceType.value = 'VIERGE'; $emit('load-model', { wizard })"
            :disabled="!wizard.isArticleValid.value || !wizard.operationCode.value"
            class="inline-flex items-center gap-1.5 px-4 py-2 text-slate-500 bg-white border border-slate-300 hover:border-slate-400 rounded-lg font-bold text-xs hover:bg-slate-50 hover:text-slate-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed h-[36px]"
          >
            <i class="pi pi-file text-sm"></i>
            Créer un plan complètement vierge
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { nextTick, watch, ref } from 'vue';

const props = defineProps({
  wizard: {
    type: Object,
    required: true
  }
});
const wizard = props.wizard;
const excelFileInput = ref(null);
const emit = defineEmits(['load-model', 'excel-selected']);

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

// Clicking the EXCEL card directly opens the file picker
const handleExcelCardClick = () => {
  wizard.sourceType.value = 'EXCEL';
  if (excelFileInput.value && wizard.isArticleValid.value && wizard.operationCode.value) {
    excelFileInput.value.click();
  }
};

// If there is exactly one modele available, pre-select it (no auto-generation)
watch(() => wizard.availableModeles.value, (list) => {
  if (Array.isArray(list) && list.length === 1 && !wizard.selectedSourceId?.value) {
    wizard.selectedSourceId.value = list[0].id;
  }
});
</script>
