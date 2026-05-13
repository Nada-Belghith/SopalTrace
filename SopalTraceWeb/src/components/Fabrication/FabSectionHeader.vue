<template>
  <tr class="bg-[#f1f5f9] border-t-4 border-slate-300">
    <td :colspan="colspan" class="p-3 px-4 relative">
      <div class="flex flex-col gap-3 pr-40">
        
        <!-- LIGNE D'INPUTS -->
        <div class="flex items-center gap-3">
          <span class="bg-blue-50 text-blue-600 text-[10px] font-black px-2 py-1.5 rounded border border-blue-100 uppercase tracking-widest shrink-0 shadow-sm">
              SEC {{ index + 1 }}
          </span>
          
          <select v-model="localSection.typeSectionId" :disabled="isArchived" class="w-64 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-xs font-bold text-slate-800 outline-none focus:border-blue-500 shadow-sm cursor-pointer disabled:bg-slate-100 disabled:text-slate-500 disabled:cursor-not-allowed">
            <option value="" disabled>--Caractéristiques à contrôler--</option>
            <option v-for="ts in (store.typesSection || [])" :key="ts.id" :value="ts.id">{{ ts.libelle }}</option>
          </select>

          <select v-model="localSection.modeFreq" @change="handleModeFreqChange" :disabled="isArchived" class="bg-slate-100 border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-600 outline-none cursor-pointer">
            <option value="SANS">Sans fréquence</option>
            <option value="VARIABLE">➕ Fréquence des pièces</option>
            <option value="FIXE">➕ Règle d'Échantillonnage</option>
          </select>

          <!-- SI VARIABLE -->
          <div v-if="localSection.modeFreq === 'VARIABLE'" class="flex items-center gap-2 animate-in fade-in slide-in-from-left-4 bg-white border border-slate-300 rounded-lg p-1 shadow-sm px-2">
              <template v-if="is100Percent">
                <div class="flex items-center gap-2 py-0.5 px-1">
                  <span class="bg-blue-600 text-white text-[10px] font-black px-2 py-1 rounded shadow-sm flex items-center gap-1">
                    <i class="pi pi-check-circle text-[9px]"></i>
                    100% DES PIÈCES
                  </span>
                  <button @click="switchToVariableManual" 
                          :disabled="isArchived"
                          type="button" 
                          class="text-slate-400 hover:text-blue-500 transition-colors ml-1 p-1 hover:bg-slate-50 rounded disabled:opacity-30 disabled:cursor-not-allowed" 
                          title="Passer en saisie manuelle">
                    <i class="pi pi-pencil text-[10px]"></i>
                  </button>
                </div>
              </template>
              <template v-else>
                  <input type="number" v-model.number="localSection.freqNum" @change="verifierVariables" :disabled="isArchived" min="1" max="1000" class="w-12 bg-slate-100 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs disabled:bg-slate-200" />
                  <span class="text-[10px] font-bold text-slate-500 uppercase">Pièce(s)</span>
                  
                  <select v-model="localSection.typeVariable" @change="verifierVariables" :disabled="isArchived" class="bg-transparent text-slate-700 font-bold outline-none cursor-pointer text-xs ml-1 border-l border-slate-200 pl-2 disabled:text-slate-400">
                    <option value="HEURE">/ Heure(s)</option>
                    <option value="SERIE">par Série</option>
                    <option value="ECHANTILLON">Échantillons</option>
                  </select>

                  <template v-if="localSection.typeVariable === 'HEURE'">
                      <span class="text-[9px] font-bold text-slate-400 uppercase ml-1">Toutes les</span>
                      <input type="number" v-model.number="localSection.freqHours" @change="verifierVariables" :disabled="isArchived" min="1" class="w-10 bg-slate-100 text-blue-700 font-black text-center rounded px-1 py-1 text-xs border border-slate-200" />
                      <span class="text-[9px] font-bold text-slate-400 uppercase">H</span>
                  </template>

                  <!-- BOUTON RACCOURCI 100% (Visible uniquement si PAS déjà 100%) -->
                  <button @click="set100Percent" 
                          type="button"
                          class="flex items-center ml-2 pl-2 border-l border-slate-200 transition-all hover:scale-110 active:scale-95 text-slate-300 hover:text-blue-400"
                          title="Appliquer 100% (100% des pièces/h)">
                    <span class="font-black text-[11px]">100%</span>
                  </button>
              </template>
          </div>

          <!-- SI FIXE -->
          <div v-if="localSection.modeFreq === 'FIXE'" class="flex items-center animate-in fade-in slide-in-from-left-4">
              <select v-model="localSection.regleEchantillonnageId" @change="verifierVariables" :disabled="isArchived" class="w-80 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-700 outline-none focus:border-blue-500 shadow-sm cursor-pointer">
                <option v-for="r in (store.reglesEchantillonnage || [])" :key="r.id" :value="r.id">{{ r.libelle }}</option>
              </select>
          </div>
        </div>

        <!-- BARRE D'APERÇU STYLE PF -->
        <div class="w-full border text-[11px] font-black tracking-widest rounded px-3 py-2 flex items-center shadow-inner transition-colors bg-white border-slate-200 text-slate-700">
            <span class="text-blue-500 mr-2 uppercase">Aperçu :</span> {{ apercu }}
        </div>
      </div>

      <!-- BOUTONS DE DROITE -->
      <div class="flex items-center gap-4 absolute right-4 top-1/2 -translate-y-1/2">
        <button v-if="!isArchived" @click="$emit('add-ligne')" class="text-blue-600 text-[11px] font-black uppercase tracking-widest hover:text-blue-800 flex items-center gap-1 transition-colors">
          <i class="pi pi-plus"></i> Ajouter ligne
        </button>
        <button v-if="!isArchived" @click="$emit('remove')" class="text-slate-400 hover:text-red-600 transition-colors ml-2" title="Supprimer la section">
          <i class="pi pi-times-circle text-base"></i>
        </button>
      </div>
    </td>
  </tr>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';

const props = defineProps({
  section: { type: Object, required: true },
  index: { type: Number, required: true },
  colspan: { type: Number, default: 8 },
  label: { type: String, default: 'SEC' },
  periodicites: { type: Array, default: () => [] },
  isArchived: { type: Boolean, default: false }
});

const emit = defineEmits(['add-ligne', 'remove', 'update:section']);
const store = useFabModeleStore();

const localSection = ref({ ...props.section });

watch(() => props.section, (newSection) => {
  // Only update if the ID changed to avoid deep recursive loops
  if (newSection.id !== localSection.value.id) {
    localSection.value = { ...newSection };
  }
}, { deep: true });

watch(localSection, (newVal) => {
  emit('update:section', newVal);
}, { deep: true });

const handleModeFreqChange = () => {
  if (localSection.value.modeFreq === 'VARIABLE') {
    // Par défaut si on bascule sur perso
    if (!localSection.value.freqNum) localSection.value.freqNum = 1;
    if (!localSection.value.typeVariable) localSection.value.typeVariable = 'HEURE';
    if (!localSection.value.freqHours) localSection.value.freqHours = 1;
  }
  verifierVariables();
};

// ✅ MISE À JOUR DU LIBELLÉ RÉEL (Pour la base de données)
// ⚠️ IMPORTANT: Pour les sections importées d'Excel, on PRÉSERVE le libellé complet original
const verifierVariables = () => {
  // Vérifier si c'est une section avec un libellé déjà complet (importée ou déjà bien formée en base)
  const currentLib = (localSection.value.libelleSection || '').toLowerCase();
  const isCompleteLabel = currentLib.includes('caractéristiques à contrôler') 
                       || currentLib.includes('caractéristique')
                       || currentLib.includes('contrôle');

  // Si c'est une section avec un libellé déjà complet, on évite de le régénérer brutalement
  // SAUF si l'utilisateur change la nature (typeSectionId) - on peut affiner ici si besoin
  if (isCompleteLabel && localSection.value.isFromDb) {
     // Pour les sections en base, on ne touche à rien sauf si modif explicite
     // (On pourrait ajouter un flag isModified si on veut être parfait)
     return;
  }

  // Sinon, reconstruire le libellé à partir des composants
  // ✅ On récupère le titre (Nature)
  const typeSec = (store.typesSection || []).find(ts => ts.id === localSection.value.typeSectionId);
  let titre;
  
  if (typeSec) {
    titre = typeSec.libelle;
  } else {
    // FALLBACK : Utiliser le nom stocké (qui contient la nature perso extraite)
    titre = localSection.value.nom || localSection.value.libelleSection || 'Section';
    // On nettoie UNIQUEMENT le préfixe si doublé, on ne touche à rien d'autre (Pas de nettoyage de fréquence)
    titre = titre.replace(/caractéristiques à contrôler/gi, "").trim();
  }
  
  // S'assurer du préfixe propre
  const prefix = "Caractéristiques à contrôler";
  titre = titre ? `${prefix} ${titre}` : prefix;

  let libelleFreq = "";
  if (localSection.value.modeFreq === 'VARIABLE') {
    if (is100Percent.value) {
      const period100 = (store.periodicites || []).find(p => p.frequenceNum === 100 || (p.code === '100PCT_1H'));
      libelleFreq = period100 ? period100.libelle : "100% des pièces/h";
    } else {
      const freqNum = localSection.value.freqNum || 0;
      const sP = freqNum > 1 ? 's' : '';
      if (localSection.value.typeVariable === 'HEURE') {
        const h = localSection.value.freqHours || 1;
        const sH = h > 1 ? 's' : '';
        libelleFreq = h === 1 ? `${freqNum} pièce${sP} / heure` : `${freqNum} pièce${sP} / ${h} heure${sH}`;
      } else if (localSection.value.typeVariable === 'ECHANTILLON') {
        libelleFreq = `${freqNum} échantillon${sP}`;
      } else {
        libelleFreq = `une série de ${freqNum} pièces`;
      }
    }
  } else if (localSection.value.periodiciteId) {
    const period = (store.periodicites || []).find(p => p.id === localSection.value.periodiciteId);
    if (period) libelleFreq = period.libelle;
  }

  // ✅ On récupère aussi la règle d'échantillonnage
  let regleLibelle = "";
  if (localSection.value.regleEchantillonnageId) {
    const regle = (store.reglesEchantillonnage || []).find(r => r.id === localSection.value.regleEchantillonnageId);
    if (regle) regleLibelle = regle.libelle;
  } else {
    // FALLBACK : si pas d'ID (ex: import Excel inconnu), on utilise le libellé texte direct
    regleLibelle = localSection.value.regleEchantillonnageLibelle || "";
  }

  // ✅ Concaténation unique (Fréq + Règle)
  const complements = [];
  if (libelleFreq) complements.push(libelleFreq);
  if (regleLibelle && regleLibelle !== libelleFreq) complements.push(regleLibelle);

  const detail = complements.join(" - ");
  localSection.value.libelleSection = detail ? `${titre} (${detail})` : titre;
  localSection.value.nom = titre;
  localSection.value.frequenceLibelle = libelleFreq;
  localSection.value.regleEchantillonnageLibelle = regleLibelle;
};

// ✅ Détecter si on est à 100% (soit par la nature choisie, soit par les chiffres, soit par le libellé)
const is100Percent = computed(() => {
  // 1. Par la Nature sélectionnée
  const typeSec = (store.typesSection || []).find(ts => ts.id === localSection.value.typeSectionId);
  if (typeSec && typeSec.libelle.toUpperCase().includes('100%')) return true;

  // 2. Par le libellé de fréquence ou de périodicité
  const libFreq = (localSection.value.frequenceLibelle || '').toLowerCase();
  if (libFreq.includes('100%')) return true;

  if (localSection.value.periodiciteId) {
    const p = store.periodicites.find(per => per.id === localSection.value.periodiciteId);
    if (p && p.libelle.toLowerCase().includes('100%')) return true;
  }

  // 3. Par les chiffres saisis (Sécurité)
  return localSection.value.freqNum === 100 && 
         localSection.value.typeVariable === 'HEURE' && 
         (localSection.value.freqHours === 1 || !localSection.value.freqHours);
});

// ✅ Auto-configurer la fréquence si la nature choisie contient "100%"
watch(() => localSection.value.typeSectionId, (newId) => {
  const typeSec = (store.typesSection || []).find(ts => ts.id === newId);
  if (typeSec && typeSec.libelle.toUpperCase().includes('100%')) {
    localSection.value.modeFreq = 'VARIABLE';
    localSection.value.freqNum = 100;
    localSection.value.typeVariable = 'HEURE';
    localSection.value.freqHours = 1;
  }
  verifierVariables();
});

const set100Percent = () => {
  localSection.value.modeFreq = 'VARIABLE';
  localSection.value.freqNum = 100;
  localSection.value.typeVariable = 'HEURE';
  localSection.value.freqHours = 1;
  verifierVariables();
};

const switchToVariableManual = () => {
  localSection.value.modeFreq = 'VARIABLE';
  localSection.value.freqNum = 2; // Forcer à 2 pour sortir du badge automatique
  localSection.value.typeVariable = 'HEURE';
  localSection.value.freqHours = 1;
  
  // Vider les libellés forcés pour que le calcul automatique reprenne
  localSection.value.frequenceLibelle = "";
  localSection.value.periodiciteId = null;
  
  verifierVariables();
};

const apercu = computed(() => {
  const typeSec = (store.typesSection || []).find(ts => ts.id === localSection.value.typeSectionId);
  
  // ✅ Titre de base : soit la nature officielle, soit le nom personnalisé
  let txt;
  if (typeSec) {
    txt = `Caractéristiques à contrôler ${typeSec.libelle}`;
  } else {
    // Utiliser le nom brut (Pas de nettoyage de fréquence comme demandé)
    const rawNom = (localSection.value.nom || '').replace(/caractéristiques à contrôler/gi, "").trim();
    
    txt = rawNom ? `Caractéristiques à contrôler ${rawNom}` : "Caractéristiques à contrôler";
  }

  // ✅ Ajouter les compléments (Fréquence / Règle)
  let complement = "";
  if (localSection.value.modeFreq === 'FIXE' && localSection.value.regleEchantillonnageId) {
    const regle = (store.reglesEchantillonnage || []).find(r => r.id === localSection.value.regleEchantillonnageId);
    if (regle) complement = regle.libelle;
  } else if (localSection.value.modeFreq === 'VARIABLE') {
    const freqNum = localSection.value.freqNum || 0;
    const is100 = freqNum === 100 && localSection.value.typeVariable === 'HEURE' && (localSection.value.freqHours === 1 || !localSection.value.freqHours);

    if (is100Percent.value || is100) {
      const period100 = (store.periodicites || []).find(p => p.frequenceNum === 100 || (p.code === '100PCT_1H'));
      complement = period100 ? period100.libelle : "100% des pièces/h";
    } else {
      if (freqNum === 0 && localSection.value.frequenceLibelle) {
        complement = localSection.value.frequenceLibelle;
      } else {
        const sP = freqNum > 1 ? 's' : '';
        complement = localSection.value.typeVariable === 'HEURE'
          ? (() => {
              const h = localSection.value.freqHours || 1;
              const sH = h > 1 ? 's' : '';
              return h === 1 ? `${freqNum || 1} pièce${sP} / heure` : `${freqNum || 1} pièce${sP} / ${h} heure${sH}`;
            })()
          : (localSection.value.typeVariable === 'ECHANTILLON' ? `${freqNum || 1} échantillon${sP}` : `une série de ${freqNum || 1} pièces`);
      }
    }
  }

  // ✅ SÉCURITÉ ANTI-DOUBLON : On n'ajoute le complément que s'il n'est pas déjà présent dans le texte
  if (complement && !txt.includes(complement)) {
    return `${txt} (${complement})`;
  }
  return txt;
});
</script>
