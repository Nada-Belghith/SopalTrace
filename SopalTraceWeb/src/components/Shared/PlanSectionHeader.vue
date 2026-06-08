<template>
  <tr class="bg-[#f1f5f9] border-t-4 border-slate-300">
    <td colspan="100" class="p-3 px-4 relative">
      <div class="flex flex-col gap-3 pr-40">
        
        <!-- LIGNE D'INPUTS -->
        <div class="flex items-center gap-3">
          <span class="bg-blue-50 text-blue-600 text-[10px] font-black px-2 py-1.5 rounded border border-blue-100 uppercase tracking-widest shrink-0 shadow-sm">
              {{ label }} {{ index + 1 }}
          </span>
          
          <select v-model="localSection.typeSectionId" :disabled="isReadOnly" class="w-48 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-xs font-bold text-slate-800 outline-none focus:border-blue-500 shadow-sm cursor-pointer disabled:bg-slate-100 disabled:text-slate-500 disabled:cursor-not-allowed">
            <option value="" disabled>--{{ defaultTitle }}--</option>
            <option v-for="ts in (typesSection || [])" :key="ts.id" :value="ts.id">{{ ts.libelle }}</option>
          </select>

          <input v-model="localSection.nom" 
                 @input="verifierVariables"
                 :disabled="isReadOnly" 
                 type="text" 
                 placeholder="Nom / Suffixe (optionnel)..." 
                 class="w-48 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-xs font-bold text-slate-800 outline-none focus:border-blue-500 shadow-sm disabled:bg-slate-100 disabled:text-slate-500 disabled:cursor-not-allowed animate-in fade-in slide-in-from-left-2" />

          <select v-model="localSection.modeFreq" @change="handleModeFreqChange" :disabled="isReadOnly" class="bg-slate-100 border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-600 outline-none cursor-pointer disabled:text-slate-500 disabled:cursor-not-allowed">
            <option value="SANS">Sans fréquence</option>
            <option value="VARIABLE">➕ Fréquence des pièces</option>
            <option value="FIXE">➕ Règle d'Échantillonnage</option>
          </select>

          <!-- SI VARIABLE -->
          <div v-if="localSection.modeFreq === 'VARIABLE'" class="flex items-center gap-2 animate-in fade-in slide-in-from-left-4 bg-white border border-slate-300 rounded-lg p-1 shadow-sm px-2" :class="{ 'bg-slate-100 border-slate-200': isReadOnly }">
              <template v-if="est100Pourcent">
                <div class="flex items-center gap-2 py-0.5 px-1">
                  <span class="bg-blue-600 text-white text-[10px] font-black px-2 py-1 rounded shadow-sm flex items-center gap-1">
                    <i class="pi pi-check-circle text-[9px]"></i>
                    100% DES PIÈCES
                  </span>
                  <button @click="switchToVariableManual" 
                          v-if="!isReadOnly"
                          type="button" 
                          class="text-slate-400 hover:text-blue-500 transition-colors ml-1 p-1 hover:bg-slate-50 rounded disabled:opacity-30 disabled:cursor-not-allowed" 
                          title="Passer en saisie manuelle">
                    <i class="pi pi-pencil text-[10px]"></i>
                  </button>
                </div>
              </template>
              <template v-else>
                  <input type="number" v-model.number="localSection.freqNum" @change="verifierVariables" :disabled="isReadOnly" min="1" max="1000" class="w-12 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs disabled:text-slate-500 disabled:cursor-not-allowed" :class="isReadOnly ? 'bg-slate-100' : 'bg-slate-100'" />
                  <span class="text-[10px] font-bold text-slate-500 uppercase">Pièce(s)</span>
                  
                  <select v-model="localSection.typeVariable" @change="verifierVariables" :disabled="isReadOnly" class="bg-transparent text-slate-700 font-bold outline-none cursor-pointer text-xs ml-1 border-l border-slate-200 pl-2 disabled:text-slate-400 disabled:cursor-not-allowed">
                    <option value="HEURE">/ Heure(s)</option>
                    <option value="SERIE">par Série</option>
                    <option value="ECHANTILLON">Échantillons</option>
                  </select>

                  <template v-if="localSection.typeVariable === 'HEURE'">
                      <span class="text-[9px] font-bold text-slate-400 uppercase ml-1">Toutes les</span>
                      <input type="number" v-model.number="localSection.freqHours" @change="verifierVariables" :disabled="isReadOnly" min="1" class="w-10 text-blue-700 font-black text-center rounded px-1 py-1 text-xs border border-slate-200 disabled:text-slate-500 disabled:cursor-not-allowed" :class="isReadOnly ? 'bg-slate-100' : 'bg-slate-100'" />
                      <span class="text-[9px] font-bold text-slate-400 uppercase">H</span>
                  </template>

                  <!-- BOUTON RACCOURCI 100% -->
                  <button @click="set100Pourcent" 
                          v-if="!isReadOnly"
                          type="button"
                          class="flex items-center ml-2 pl-2 border-l border-slate-200 transition-all hover:scale-110 active:scale-95 text-slate-300 hover:text-blue-400"
                          title="Appliquer 100% (100% des pièces/h)">
                    <span class="font-black text-[11px]">100%</span>
                  </button>
              </template>
          </div>

          <!-- SI FIXE -->
          <div v-if="localSection.modeFreq === 'FIXE'" class="flex items-center animate-in fade-in slide-in-from-left-4">
              <select v-model="localSection.regleEchantillonnageId" @change="verifierVariables" :disabled="isReadOnly" class="w-80 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-700 outline-none focus:border-blue-500 shadow-sm cursor-pointer disabled:bg-slate-100 disabled:text-slate-500 disabled:cursor-not-allowed">
                <option :value="null" disabled>Sélectionner la règle...</option>
                <option v-for="r in (reglesEchantillonnage || [])" :key="r.id" :value="r.id">{{ r.libelle.substring(0, 45) }}{{ r.libelle.length > 45 ? '...' : '' }}</option>
              </select>
          </div>
        </div>

        <!-- BARRE D'APERÇU -->
        <div class="w-full border text-[11px] font-black tracking-widest rounded px-3 py-2 flex items-center shadow-inner transition-colors bg-white border-slate-200 text-slate-700">
            <span class="text-blue-500 mr-2 uppercase">Aperçu :</span> {{ apercu }}
        </div>
      </div>

      <!-- BOUTONS DE DROITE -->
      <div class="flex items-center gap-4 absolute right-4 top-1/2 -translate-y-1/2">
        <button v-if="!isReadOnly" @click="$emit('add-ligne')" class="text-blue-600 text-[11px] font-black uppercase tracking-widest hover:text-blue-800 flex items-center gap-1 transition-colors">
          <i class="pi pi-plus"></i> Ajouter ligne
        </button>
        <button v-if="!isReadOnly" @click="$emit('remove')" class="text-slate-400 hover:text-red-600 transition-colors ml-2" title="Supprimer la section">
          <i class="pi pi-times-circle text-base"></i>
        </button>
      </div>
    </td>
  </tr>
</template>

<script setup>
import { ref, watch, computed, onMounted } from 'vue';

const props = defineProps({
  section: { type: Object, required: true },
  index: { type: Number, required: true },
  colspan: { type: Number, default: 8 },
  label: { type: String, default: 'SEC' },
  defaultTitle: { type: String, default: 'Caractéristiques à contrôler' },
  isReadOnly: { type: Boolean, default: false },
  typesSection: { type: Array, default: () => [] },
  periodicites: { type: Array, default: () => [] },
  reglesEchantillonnage: { type: Array, default: () => [] }
});

const emit = defineEmits(['add-ligne', 'remove', 'update:section']);

const localSection = ref({ ...props.section });
const isSyncingFromParent = ref(false);

watch(() => props.section, (newSection) => {
  if (!newSection) return;
  const sourceSource = JSON.stringify(newSection);
  const sourceLocale = JSON.stringify(localSection.value);
  
  if (sourceSource !== sourceLocale) {
    isSyncingFromParent.value = true;
    localSection.value = JSON.parse(sourceSource);
    
    // Auto-populate 'nom' from 'libelleSection' for custom sections
    if (!localSection.value.typeSectionId && !localSection.value.nom && localSection.value.libelleSection) {
      localSection.value.nom = localSection.value.libelleSection;
    }

    setTimeout(() => { isSyncingFromParent.value = false; }, 0);
  }
}, { deep: true, immediate: true });

onMounted(() => {
  verifierVariables(false);
});

watch(localSection, (newVal) => {
  if (isSyncingFromParent.value) return;
  emit('update:section', newVal);
}, { deep: true });

// --- PROPRIÉTÉS CALCULÉES (Logique Métier Unifiée) ---

const est100Pourcent = computed(() => {
  const typeSec = (props.typesSection || []).find(ts => ts.id === localSection.value.typeSectionId);
  if (typeSec && typeSec.libelle.toUpperCase().includes('100%')) return true;

  const libFreq = (localSection.value.frequenceLibelle || '').toLowerCase();
  if (libFreq.includes('100%')) return true;

  if (localSection.value.periodiciteId) {
    const p = (props.periodicites || []).find(per => per.id === localSection.value.periodiciteId);
    if (p && p.libelle.toLowerCase().includes('100%')) return true;
  }

  return localSection.value.freqNum === 100 && 
         localSection.value.typeVariable === 'HEURE' && 
         (localSection.value.freqHours === 1 || !localSection.value.freqHours);
});

const titreCalcule = computed(() => {
  // 1. Si l'utilisateur a saisi un texte personnalisé, on l'utilise EXACTEMENT tel quel
  // (sans forcer le préfixe par défaut, pour permettre une personnalisation totale)
  if (localSection.value.nom) {
    const cleanNom = localSection.value.nom.replace(/\s*\([^)]*\)\s*$/, '').trim();
    if (cleanNom) {
      return cleanNom;
    }
  }

  // 2. Sinon, on génère le titre par défaut
  const typeSec = (props.typesSection || []).find(ts => ts.id === localSection.value.typeSectionId);
  let baseTitle = localSection.value.libelleSection || props.defaultTitle;
  if (typeSec) {
    // Eviter la duplication si le typeSec.libelle inclut déjà le defaultTitle
    if (typeSec.libelle.toLowerCase().includes(props.defaultTitle.toLowerCase())) {
      baseTitle = typeSec.libelle;
    } else if (props.defaultTitle.toLowerCase().includes(typeSec.libelle.toLowerCase())) {
      baseTitle = props.defaultTitle;
    } else {
      baseTitle = `${props.defaultTitle} ${typeSec.libelle}`;
    }
  }

  return baseTitle;
});

const frequenceCalculee = computed(() => {
  if (localSection.value.modeFreq === 'VARIABLE') {
    if (est100Pourcent.value) {
      const period100 = (props.periodicites || []).find(p => p.frequenceNum === 100 || p.code === '100PCT_1H');
      return period100 ? period100.libelle : "100% des pièces/h";
    } 
    
    const freqNum = localSection.value.freqNum || 0;
    if (freqNum === 0 && localSection.value.frequenceLibelle) {
      return localSection.value.frequenceLibelle;
    }
    
    const sP = freqNum > 1 ? 's' : '';
    if (localSection.value.typeVariable === 'HEURE') {
      const h = localSection.value.freqHours || 1;
      const sH = h > 1 ? 's' : '';
      return h === 1 ? `${freqNum || 1} pièce${sP} / heure` : `${freqNum || 1} pièce${sP} / ${h} heure${sH}`;
    } 
    if (localSection.value.typeVariable === 'ECHANTILLON') {
      return `${freqNum || 1} échantillon${sP}`;
    }
    return `une série de ${freqNum || 1} pièces`;
  } 
  
  if (localSection.value.periodiciteId) {
    const period = (props.periodicites || []).find(p => p.id === localSection.value.periodiciteId);
    if (period) return period.libelle;
  }
  
  return "";
});

const regleCalculee = computed(() => {
  if (localSection.value.regleEchantillonnageId) {
    const regle = (props.reglesEchantillonnage || []).find(r => r.id === localSection.value.regleEchantillonnageId);
    if (regle) return regle.libelle;
  }
  return localSection.value.regleEchantillonnageLibelle || "";
});

const libelleSectionComplet = computed(() => {
  const titre = titreCalcule.value;
  const freq = frequenceCalculee.value;
  const regle = regleCalculee.value;
  
  const complements = [];
  if (freq) complements.push(freq);
  if (regle && regle !== freq) complements.push(regle);
  
  const detail = complements.filter(c => {
    if (!c) return false;
    const normC = c.toLowerCase();
    const normT = titre.toLowerCase();
    
    // Si le titre contient déjà "100%" ou "100 pièces", on exclut les compléments décrivant le 100% ou du 1 pièce/h
    if ((normT.includes('100%') || normT.includes('100 pièces') || normT.includes('100 pieces')) &&
        (normC.includes('100') || normC.includes('1 pièce') || normC.includes('1 piece'))) {
      return false;
    }
    
    // Si le titre contient déjà mot pour mot ce complément, on l'exclut
    if (normT.includes(normC)) {
      return false;
    }
    
    return true;
  }).join(" - ");
  
  if (detail) {
    return `${titre} (${detail})`;
  }
  return titre;
});

const apercu = computed(() => libelleSectionComplet.value);

// --- ACTIONS & ÉVÉNEMENTS ---

const verifierVariables = (isUserAction = true) => {
  // If called from an event (like @change or @input), isUserAction is the Event object, which is truthy
  if (isUserAction) {
    localSection.value.isFromDb = false;
    // Si l'utilisateur modifie manuellement les variables, on doit s'assurer que periodiciteId est vidé
    // sinon le backend risque de conserver l'ancienne périodicité de la base de données.
    if (localSection.value.modeFreq === 'VARIABLE') {
        localSection.value.periodiciteId = null;
        localSection.value.regleEchantillonnageId = null;
    }
  }

  if (localSection.value.isFromDb) return;

  localSection.value.libelleSection = libelleSectionComplet.value;
  localSection.value.frequenceLibelle = frequenceCalculee.value;
  localSection.value.regleEchantillonnageLibelle = regleCalculee.value;
};

const handleModeFreqChange = () => {
  if (localSection.value.modeFreq === 'VARIABLE') {
    if (!localSection.value.freqNum) localSection.value.freqNum = 1;
    if (!localSection.value.typeVariable) localSection.value.typeVariable = 'HEURE';
    if (!localSection.value.freqHours) localSection.value.freqHours = 1;
  } else if (localSection.value.modeFreq === 'SANS') {
    localSection.value.regleEchantillonnageId = null;
    localSection.value.periodiciteId = null;
  }
  verifierVariables();
};

watch(() => localSection.value.typeSectionId, (newId) => {
  if (newId === 'custom') {
    localSection.value.typeSectionId = null;
    return;
  }
  const typeSec = (props.typesSection || []).find(ts => ts.id === newId);
  if (typeSec && typeSec.libelle.toUpperCase().includes('100%')) {
    localSection.value.modeFreq = 'VARIABLE';
    localSection.value.freqNum = 100;
    localSection.value.typeVariable = 'HEURE';
    localSection.value.freqHours = 1;
  }
  verifierVariables();
});

const set100Pourcent = () => {
  if (props.isReadOnly) return;
  localSection.value.modeFreq = 'VARIABLE';
  localSection.value.freqNum = 100;
  localSection.value.typeVariable = 'HEURE';
  localSection.value.freqHours = 1;
  verifierVariables();
};

const switchToVariableManual = () => {
  if (props.isReadOnly) return;
  localSection.value.modeFreq = 'VARIABLE';
  localSection.value.freqNum = 2; 
  localSection.value.typeVariable = 'HEURE';
  localSection.value.freqHours = 1;
  
  localSection.value.frequenceLibelle = "";
  localSection.value.periodiciteId = null;
  
  verifierVariables();
};
</script>
