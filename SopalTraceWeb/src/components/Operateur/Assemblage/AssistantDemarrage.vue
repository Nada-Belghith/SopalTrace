<template>
  <div class="mb-6">
    <div class="bg-white border rounded-2xl p-5 shadow-sm relative overflow-hidden" :class="borderClass">
      <!-- Background glow -->
      <div class="absolute -top-10 -right-10 w-40 h-40 rounded-full opacity-20 blur-3xl pointer-events-none" :class="glowClass"></div>
      
      <div class="flex items-start md:items-center justify-between gap-4 flex-col md:flex-row relative z-10">
        
        <div class="flex items-center gap-4">
          <div class="w-14 h-14 rounded-full flex items-center justify-center shrink-0 shadow-sm" :class="iconBgClass">
            <i :class="[iconClass, 'text-2xl', iconTextColorClass]"></i>
          </div>
          <div>
            <div class="flex items-center gap-2 mb-1">
              <span class="text-xs font-black tracking-wider uppercase px-2 py-0.5 rounded-md" :class="badgeClass">
                {{ stepBadgeText }}
              </span>
              <span v-if="!isProduction" class="text-xs text-slate-500 font-bold"><i class="pi pi-arrow-right text-[10px]"></i> Flux de Démarrage</span>
              <span v-else class="text-xs text-emerald-600 font-bold flex items-center gap-1"><i class="pi pi-check-circle text-[12px]"></i> Production en cours</span>
            </div>
            <h3 class="text-xl font-bold text-slate-800 m-0">{{ title }}</h3>
            <p class="text-slate-500 text-sm m-0 mt-1">{{ description }}</p>
          </div>
        </div>

        <div class="flex flex-wrap gap-2">
          <button 
            v-if="currentStep === 1"
            @click="onActionClick('echantillonnage')"
            class="shrink-0 px-6 py-3 rounded-xl font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg flex items-center gap-2 cursor-pointer border-0 bg-red-600 hover:bg-red-700"
          >
            <span>Commencer l'échantillonnage</span>
            <i class="pi pi-arrow-right"></i>
          </button>

          <template v-if="currentStep === 2">
            <button 
              v-if="vmCat"
              @click="onActionClick('verifMachine')"
              class="shrink-0 px-4 py-2.5 rounded-xl font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg flex items-center gap-2 cursor-pointer border-0"
              :class="isVmDemarrageDone ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-orange-600 hover:bg-orange-700'"
            >
              <i :class="isVmDemarrageDone ? 'pi pi-check-circle text-emerald-200' : 'pi pi-cog'"></i>
              <span>{{ isVmDemarrageDone ? 'Vérif. Machine (Fait)' : 'Vérif. Machine' }}</span>
            </button>

            <button 
              v-if="planAssCat"
              @click="onActionClick('planAss')"
              class="shrink-0 px-4 py-2.5 rounded-xl font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg flex items-center gap-2 cursor-pointer border-0"
              :class="isPlanAssDemarrageDone ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-orange-600 hover:bg-orange-700'"
            >
              <i :class="isPlanAssDemarrageDone ? 'pi pi-check-circle text-emerald-200' : 'pi pi-sitemap'"></i>
              <span>{{ isPlanAssDemarrageDone ? 'Plan Assemblage (Répondu)' : 'Plan Assemblage' }}</span>
            </button>
          </template>

          <template v-if="currentStep === 4">
            <button 
              v-if="vmCat"
              @click="onActionClick('verifMachine', { finPoste: true })"
              class="shrink-0 px-4 py-2.5 rounded-xl font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg flex items-center gap-2 cursor-pointer border-0"
              :class="isVmFinPosteDone ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-orange-600 hover:bg-orange-700'"
            >
              <i :class="isVmFinPosteDone ? 'pi pi-check-circle text-emerald-200' : 'pi pi-cog'"></i>
              <span>{{ isVmFinPosteDone ? 'Vérif. Machine (Répondu)' : 'Vérif. Machine (Fin Poste)' }}</span>
            </button>

            <button 
              v-if="planAssCat"
              @click="onActionClick('planAss', { finPoste: true })"
              class="shrink-0 px-4 py-2.5 rounded-xl font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg flex items-center gap-2 cursor-pointer border-0"
              :class="isPlanAssFinPosteDone ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-orange-600 hover:bg-orange-700'"
            >
              <i :class="isPlanAssFinPosteDone ? 'pi pi-check-circle text-emerald-200' : 'pi pi-sitemap'"></i>
              <span>{{ isPlanAssFinPosteDone ? 'Plan Assemblage (Répondu)' : 'Plan Assemblage (Fin Poste)' }}</span>
            </button>
            
            <button 
              v-if="cpCat"
              @click="onActionClick('documentControlePoste', { finPoste: true })"
              class="shrink-0 px-4 py-2.5 rounded-xl font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg flex items-center gap-2 cursor-pointer border-0"
              :class="isCpDone ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-orange-600 hover:bg-orange-700'"
            >
              <i :class="isCpDone ? 'pi pi-check-circle text-emerald-200' : 'pi pi-calendar-plus'"></i>
              <span>{{ isCpDone ? 'Résultat Contrôle Poste (Fait)' : 'Résultat Contrôle Poste' }}</span>
            </button>

            <button 
              @click="onActionClick('tracabilite', { finPoste: true })"
              class="shrink-0 px-4 py-2.5 rounded-xl font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg flex items-center gap-2 cursor-pointer border-0"
              :class="tracabiliteCat?.isTermine ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-blue-600 hover:bg-blue-700'"
            >
              <i :class="tracabiliteCat?.isTermine ? 'pi pi-check-circle text-emerald-200' : 'pi pi-file-edit'"></i>
              <span>{{ tracabiliteCat?.isTermine ? 'Registre Traçabilité (Répondu)' : 'Registre Traçabilité' }}</span>
            </button>
            
            <button 
              @click="canCloturerPoste ? emit('cloturer-poste') : onBlockedCloture()"
              class="shrink-0 px-4 py-2.5 rounded-xl font-bold text-white shadow-md transition-all flex items-center gap-2 border-0"
              :class="canCloturerPoste ? 'bg-red-600 hover:bg-red-700 cursor-pointer shadow-lg animate-pulse' : 'bg-slate-400 opacity-70 cursor-not-allowed'"
            >
              <i :class="canCloturerPoste ? 'pi pi-stop-circle' : 'pi pi-lock'"></i>
              <span>{{ canCloturerPoste ? 'Terminer le poste définitivement' : 'Clôture (Répondre aux contrôles)' }}</span>
            </button>
          </template>
        </div>

      </div>

      <!-- Bandeau de rappel horaire & boutons d'action interactifs en Étape 3 -->
      <div v-if="isProduction" class="mt-4 pt-4 border-t border-emerald-100 flex flex-col gap-3">
        <div class="flex items-center justify-between flex-wrap gap-2">
          <div class="flex items-center gap-2">
            <span class="px-2.5 py-1 bg-amber-100 text-amber-900 font-black rounded-lg flex items-center gap-1.5 text-xs shadow-xs uppercase tracking-wider">
              <i class="pi pi-clock text-xs text-amber-700"></i> Rappel Horaire & Suivi
            </span>
            <span class="text-xs text-slate-600 font-medium">Cliquez directement sur un bouton ci-dessous pour effectuer la saisie :</span>
          </div>
        </div>

        <div class="flex flex-wrap gap-3 items-center">
          <!-- 1. Bouton Plan Assemblage & CF avec Compteur d'occurrences -->
          <button 
            @click="onActionClick('planAss')"
            class="px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs rounded-xl shadow-sm hover:shadow-md transition-all border-0 cursor-pointer flex items-center gap-2"
          >
            <i class="pi pi-sitemap text-sm"></i>
            <span>Plan Assemblage & CF</span>
            <span class="ml-1 px-2 py-0.5 bg-blue-800 text-white rounded-full text-[11px] font-black">
              {{ planAssPendingCount > 0 ? `${planAssPendingCount} occ.` : 'À jour' }}
            </span>
          </button>


          <!-- 2. Bouton Vérif Machine Après Pause -->
          <button 
            v-if="vmCat"
            @click="onActionClick('verifMachine', { apresPause: true })"
            class="px-4 py-2.5 bg-indigo-500 hover:bg-indigo-600 text-white font-bold text-xs rounded-xl shadow-sm hover:shadow-md transition-all border-0 cursor-pointer flex items-center gap-2"
          >
            <i class="pi pi-cog text-sm"></i>
            <span>Vérif. Machine (Après Pause)</span>
          </button>

          <!-- 3. Bouton Résultat Contrôle Poste -->
          <button 
            v-if="cpCat"
            @click="onActionClick('documentControlePoste')"
            class="px-4 py-2.5 bg-amber-500 hover:bg-amber-600 text-white font-bold text-xs rounded-xl shadow-sm hover:shadow-md transition-all border-0 cursor-pointer flex items-center gap-2"
          >
            <i class="pi pi-check-square text-sm"></i>
            <span>Contrôle Poste (1h)</span>
            <span class="ml-1 px-2 py-0.5 rounded-full text-[11px] font-black" :class="cpCat?.isTermine ? 'bg-amber-700 text-white' : 'bg-amber-800 text-white'">
              {{ cpStatusText }}
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  categories: {
    type: Array,
    required: true
  },
  documents: {
    type: Array,
    default: () => []
  },
  alertes: {
    type: Array,
    default: () => []
  },
  isFinPosteMode: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['action-click', 'cloturer-poste', 'annuler-fin-poste']);

const echCat = computed(() => props.categories.find(c => c.id === 'echantillonnage'));
const vmCat = computed(() => props.categories.find(c => c.id === 'verifMachine'));
const planAssCat = computed(() => props.categories.find(c => c.id === 'planAss'));
const cpCat = computed(() => props.categories.find(c => c.id === 'documentControlePoste'));
const tracabiliteCat = computed(() => props.categories.find(c => c.id === 'tracabilite'));

// Occurrences restantes pour Plan Assemblage & CF (hors réglage et hors 100%)
const planAssPendingCount = computed(() => {
  let count = 0;
  props.alertes.forEach(t => {
     if (!t.trancheHoraire?.startsWith('REGLAGE') && !t.trancheHoraire?.includes('100PCT')) {
         t.occurrences?.forEach(occ => {
            if (!occ.dateReponse && new Date(occ.heureNotifPrevue) <= new Date()) {
                count++;
            }
         });
     }
  });
  return count;
});

// Occurrences restantes pour 100%
const ech100pctPendingCount = computed(() => {
  let count = 0;
  props.alertes.forEach(t => {
     if (t.trancheHoraire?.includes('100PCT')) {
         t.occurrences?.forEach(occ => {
            if (!occ.dateReponse && new Date(occ.heureNotifPrevue) <= new Date()) {
                count++;
            }
         });
     }
  });
  return count;
});

// Statut Échantillonnage 100% Pièces
const echStatusText = computed(() => {
  if (ech100pctPendingCount.value > 0) return `${ech100pctPendingCount.value} occ.`;
  if (!echCat.value) return 'Non requis';
  return echCat.value.isTermine ? 'Fait' : 'À remplir';
});

// Statut Contrôle Poste (Horaire)
const isVmDemarrageDone = computed(() => {
  if (!props.documents || props.documents.length === 0) return true;
  const vmDocs = props.documents.filter(d => d.typeDocument === 'VERIF_MACHINE');
  if (vmDocs.length === 0) return true;
  return vmDocs.every(d => d.estDemarrageTermine || d.estTermine);
});

const isVmFinPosteDone = computed(() => {
  if (!props.documents || props.documents.length === 0) return true;
  const vmDocs = props.documents.filter(d => d.typeDocument === 'VERIF_MACHINE');
  if (vmDocs.length === 0) return true;
  return vmDocs.every(d => d.estTermine);
});

const isPlanAssDemarrageDone = computed(() => planAssCat.value ? (planAssCat.value.isDemarrageTermine ?? planAssCat.value.isTermine) : true);

const isPlanAssFinPosteDone = computed(() => planAssCat.value ? planAssCat.value.isTermine : true);

const isTracabiliteDone = computed(() => tracabiliteCat.value ? tracabiliteCat.value.isTermine : true);

const isCpDone = computed(() => cpCat.value ? cpCat.value.isTermine : true);

const canCloturerPoste = computed(() => {
  return isVmFinPosteDone.value && isPlanAssFinPosteDone.value && isTracabiliteDone.value && isCpDone.value;
});

const onBlockedCloture = () => {
  const missing = [];
  if (!isVmFinPosteDone.value) missing.push('Vérif. Machine (Fin Poste)');
  if (!isPlanAssFinPosteDone.value) missing.push('Plan Assemblage (Fin Poste)');
  if (!isCpDone.value) missing.push('Résultat Contrôle Poste');
  if (!isTracabiliteDone.value) missing.push('Registre Traçabilité');
  
  Swal.fire({
    title: 'Clôture impossible',
    text: `Veuillez répondre à toutes les tâches de fin de poste avant de clôturer : ${missing.join(', ')}`,
    icon: 'warning',
    confirmButtonColor: '#3085d6',
    confirmButtonText: 'D\'accord'
  });
};

const cpStatusText = computed(() => {
  if (!cpCat.value) return 'Non requis';
  return cpCat.value.isTermine ? 'Fait' : 'Rappel 1h';
});

const currentStep = computed(() => {
  if (props.isFinPosteMode) return 4;
  if (echCat.value && !echCat.value.isTermine) return 1;
  if (!isVmDemarrageDone.value || !isPlanAssDemarrageDone.value) return 2;
  return 3;
});

const isProduction = computed(() => currentStep.value === 3);

const stepBadgeText = computed(() => {
  if (currentStep.value === 1) return 'Étape 1';
  if (currentStep.value === 2) return 'Étape 2';
  if (currentStep.value === 4) return 'Étape 4';
  return 'En cours';
});

const title = computed(() => {
  if (currentStep.value === 1) return 'Remplir la fiche d\'échantillonnage';
  if (currentStep.value === 2) return 'Vérifications de Démarrage';
  if (currentStep.value === 4) return 'Clôture de Poste';
  return 'Production en cours';
});

const description = computed(() => {
  if (currentStep.value === 1) return 'L\'échantillonnage est obligatoire avant de pouvoir commencer les autres contrôles.';
  if (currentStep.value === 2) return 'Veuillez effectuer la vérification machine et le contrôle de démarrage (Plan d\'assemblage).';
  if (currentStep.value === 4) return 'Veuillez effectuer les vérifications de fin de poste, valider les plans et la traçabilité avant de clôturer.';
  return 'Toutes les tâches de démarrage sont terminées. Restez attentif aux alertes.';
});

const iconClass = computed(() => {
  if (currentStep.value === 1) return 'pi pi-chart-pie';
  if (currentStep.value === 2) return 'pi pi-list-check';
  if (currentStep.value === 4) return 'pi pi-power-off';
  return 'pi pi-check-circle';
});

const onActionClick = (type, opts = {}) => {
  if (type === 'echantillonnage') emit('action-click', echCat.value, opts);
  else if (type === 'verifMachine') emit('action-click', vmCat.value, opts);
  else if (type === 'planAss') emit('action-click', planAssCat.value, { isFromAssistant: true, ...opts });
  else if (type === 'controle100pct') emit('action-click', planAssCat.value, { isFromAssistant: true, section100pct: true, ...opts });
  else if (type === 'documentControlePoste') emit('action-click', cpCat.value, opts);
  else if (type === 'tracabilite') emit('action-click', tracabiliteCat.value || { id: 'tracabilite' }, opts);
};

// Styles
const borderClass = computed(() => {
  if (currentStep.value === 1) return 'border-red-200';
  if (currentStep.value === 2) return 'border-orange-200';
  return 'border-emerald-200';
});

const glowClass = computed(() => {
  if (currentStep.value === 1) return 'bg-red-400';
  if (currentStep.value === 2) return 'bg-orange-400';
  return 'bg-emerald-400';
});

const iconBgClass = computed(() => {
  if (currentStep.value === 1) return 'bg-red-100';
  if (currentStep.value === 2) return 'bg-orange-100';
  return 'bg-emerald-100';
});

const iconTextColorClass = computed(() => {
  if (currentStep.value === 1) return 'text-red-600';
  if (currentStep.value === 2) return 'text-orange-600';
  return 'text-emerald-600';
});

const badgeClass = computed(() => {
  if (currentStep.value === 1) return 'bg-red-600 text-white';
  if (currentStep.value === 2) return 'bg-orange-600 text-white';
  return 'bg-emerald-600 text-white';
});

const buttonClass = computed(() => {
  if (currentStep.value === 1) return 'bg-red-600 hover:bg-red-700 shadow-red-200';
  if (currentStep.value === 2) return 'bg-orange-600 hover:bg-orange-700 shadow-orange-200';
  return '';
});

</script>
