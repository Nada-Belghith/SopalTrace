<template>
  <tbody class="divide-y divide-slate-200">
    
    <tr class="bg-[#f1f5f9] border-t-4 border-slate-300">
      <td colspan="12" class="p-3 px-4 relative">
        <div class="flex flex-col gap-3 pr-40">
          
          <div class="flex items-center gap-3">
            <span class="bg-blue-100 text-blue-800 text-[10px] font-black px-2 py-1.5 rounded-md uppercase tracking-widest shrink-0 shadow-sm">
                SEC {{ index + 1 }}
            </span>
            
            <select :value="localGroupe.typeSectionId" @change="(e) => { updateGroupe('typeSectionId', e.target.value); }" :disabled="isReadOnly" class="w-64 rounded-lg px-2 py-1.5 text-xs font-bold outline-none focus:border-blue-500 shadow-sm" :class="isReadOnly ? 'bg-slate-100 border-slate-200 cursor-not-allowed text-slate-500' : 'bg-white border border-slate-300 text-slate-800 cursor-pointer'">
              <option value="" disabled>--Caractéristiques à contrôler--</option>
              <option v-for="ts in (store.typesSection || [])" :key="ts.id" :value="ts.id">{{ ts.libelle }}</option>
            </select>

            <select :value="localGroupe.modeFreq" @change="(e) => { updateGroupe('modeFreq', e.target.value); }" :disabled="isReadOnly" :class="['rounded-lg px-2 py-1.5 text-[11px] font-bold outline-none focus:border-blue-500 shadow-sm', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-slate-100 border border-slate-300 text-slate-600 cursor-pointer']">
              <option value="SANS">Sans fréquence</option>
              <option value="VARIABLE">➕ Fréquence des pièces</option>
              <option value="FIXE" v-if="(store.entete || {}).operationCode === 'ASS'">➕ Règle d'Échantillonnage</option>
            </select>

            <div v-if="localGroupe.modeFreq === 'VARIABLE'" class="flex items-center gap-2 animate-in fade-in slide-in-from-left-4" :class="isReadOnly ? 'bg-slate-100 border-slate-200 rounded-lg p-1' : 'bg-white border border-slate-300 rounded-lg p-1 shadow-sm'">
              <!-- Option spéciale 100% -->
              <template v-if="localGroupe.freqNum === 1 && localGroupe.typeVariable === 'HEURE' && localGroupe.freqHours === 1">
                  <span class="bg-blue-50 text-blue-700 px-2 py-1 rounded text-[10px] font-black border border-blue-200 uppercase tracking-tighter">100% des pièces / H</span>
                  <button v-if="!isReadOnly" @click="updateGroupe('freqNum', 2)" class="text-slate-400 hover:text-blue-500 transition-colors ml-1" title="Changer la fréquence">
                    <i class="pi pi-pencil text-[10px]"></i>
                  </button>
              </template>
              <template v-else>
                <input type="number" :value="localGroupe.freqNum" @input="(e) => { updateGroupe('freqNum', parseInt(e.target.value)); }" min="1" max="1000" :disabled="isReadOnly" class="w-12 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs" :class="isReadOnly ? 'bg-slate-100 cursor-not-allowed text-slate-500' : 'bg-slate-100'" />
                  <span class="text-[10px] font-bold text-slate-500 uppercase">Pièce(s)</span>

                  <select :value="localGroupe.typeVariable" @change="(e) => { updateGroupe('typeVariable', e.target.value); }" :disabled="isReadOnly" :class="['text-slate-700 font-bold outline-none text-xs ml-1 border-l border-slate-200 pl-2', isReadOnly ? 'cursor-not-allowed text-slate-500' : 'cursor-pointer']">
                    <option value="HEURE">/ Heure(s)</option>
                    <option value="SERIE">par Série</option>
                    <option value="ECHANTILLON">Échantillons</option>
                  </select>

                  <template v-if="localGroupe.typeVariable === 'HEURE'">
                      <span class="text-[10px] font-bold text-slate-500 uppercase ml-1">Toutes les</span>
                      <input type="number" :value="localGroupe.freqHours" @input="(e) => { updateGroupe('freqHours', parseInt(e.target.value)); }" min="1" max="24" :disabled="isReadOnly" class="w-12 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs" :class="isReadOnly ? 'bg-slate-100 cursor-not-allowed text-slate-500' : 'bg-slate-100'" />
                      <span class="text-[10px] font-bold text-slate-500 uppercase pr-1">H</span>
                  </template>
              </template>

              <button v-if="!isReadOnly && !(localGroupe.freqNum === 1 && localGroupe.typeVariable === 'HEURE' && localGroupe.freqHours === 1)" 
                      @click="() => { updateGroupe('freqNum', 1); updateGroupe('typeVariable', 'HEURE'); updateGroupe('freqHours', 1); }" 
                      class="text-[9px] font-bold text-slate-400 hover:text-blue-500 ml-1 border-l pl-2 border-slate-200" title="Passer à 100%">
                  100%
              </button>

                <i v-if="localGroupe.isNewFreq" class="pi pi-sparkles text-emerald-500 ml-1" title="Nouvelle fréquence"></i>
            </div>

            <div v-if="localGroupe.modeFreq === 'FIXE'" class="flex items-center animate-in fade-in slide-in-from-left-4">
                <select :value="localGroupe.regleEchantillonnageId" @change="(e) => { updateGroupe('regleEchantillonnageId', e.target.value); }" :disabled="isReadOnly" :class="['w-96 rounded-lg px-2 py-1.5 text-[11px] font-bold outline-none focus:border-blue-500 shadow-sm', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-300 text-slate-700 cursor-pointer']">
                  <option :value="null" disabled>-- Sélectionner la règle d'échantillonnage --</option>
                  <option v-for="r in (store.reglesEchantillonnage || [])" :key="r.id" :value="r.id">
                    {{ r.libelle }}
                  </option>
                </select>
            </div>
          </div>

          <div class="w-full border text-[11px] font-black tracking-widest rounded px-3 py-2 flex items-center shadow-inner transition-colors bg-white border-slate-200 text-slate-700">
              <span class="text-blue-500 mr-2 uppercase">Aperçu :</span> {{ apercu || 'Caractéristiques à contrôler' }}
          </div>
        </div>

        <div v-if="!isReadOnly" class="flex items-center gap-4 absolute right-4 top-1/2 -translate-y-1/2">
          <button @click="ajouterLigne" class="text-blue-600 text-[11px] font-black uppercase tracking-widest hover:text-blue-800 flex items-center gap-1 transition-colors">
            <i class="pi pi-plus"></i> Ajouter ligne
          </button>
          <button @click="$emit('remove')" class="text-slate-400 hover:text-red-600 transition-colors ml-2" title="Supprimer la section">
            <i class="pi pi-times-circle text-base"></i>
          </button>
        </div>
      </td>
    </tr>

    <slot></slot>
    
  </tbody>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';

const props = defineProps({
  groupe: { type: Object, required: true },
  index: { type: Number, required: true },
  isReadOnly: { type: Boolean, default: false }
});

const isReadOnly = computed(() => props.isReadOnly);

const emit = defineEmits(['remove', 'update-groupe', 'section-type-required']);
const store = useFabModeleStore();

// Copie locale
const localGroupe = ref({ ...props.groupe });

// ⚠️ CORRECTION : Empêche la boucle de mise à jour de Vue.js
watch(() => props.groupe, (newGroupe) => {
  const sourceSource = JSON.stringify(newGroupe);
  const sourceLocale = JSON.stringify(localGroupe.value);
  if (sourceSource !== sourceLocale) {
    localGroupe.value = JSON.parse(sourceSource);
  }
}, { deep: true });

const updateGroupe = (key, value) => {
  localGroupe.value[key] = value;
  verifierVariables();
  emit('update-groupe', JSON.parse(JSON.stringify(localGroupe.value)));
};



const apercu = computed(() => {
  // ✅ Si la section vient de l'import (libelleSection présent mais pas de typeSectionId)
  if (!localGroupe.value.typeSectionId && localGroupe.value.libelleSection) {
      return localGroupe.value.libelleSection;
  }

  const typeSec = (store.typesSection || []).find(ts => ts.id === localGroupe.value.typeSectionId);
  if (!typeSec) return localGroupe.value.libelleSection || 'Caractéristiques à contrôler';

  let txt = typeSec.libelle;

  // Si le libellé ne commence pas déjà par "Caractéristiques...", on l'ajoute pour contexte (optionnel, mais propre)
  if (!txt.toLowerCase().startsWith('caract')) {
      txt = `Caractéristiques à contrôler ${txt}`;
  }

  if (localGroupe.value.modeFreq === 'FIXE' && localGroupe.value.regleEchantillonnageId) {
    const regle = (store.reglesEchantillonnage || []).find(r => r.id === localGroupe.value.regleEchantillonnageId);
    if (regle) txt += ` (${regle.libelle})`;
  } else if (localGroupe.value.modeFreq === 'VARIABLE') {
    const sP = localGroupe.value.freqNum > 1 ? 's' : '';
    const libelleFreq = localGroupe.value.typeVariable === 'HEURE'
      ? (() => {
          if (localGroupe.value.freqNum === 1 && localGroupe.value.freqHours === 1) {
              return "100% des pièces/h";
          }
          const sH = localGroupe.value.freqHours > 1 ? 's' : '';
          return localGroupe.value.freqHours === 1
            ? `${localGroupe.value.freqNum} pièce${sP} / heure`
            : `${localGroupe.value.freqNum} pièce${sP} / ${localGroupe.value.freqHours} heure${sH}`;
        })()
      : (localGroupe.value.typeVariable === 'ECHANTILLON' ? `${localGroupe.value.freqNum} échantillon${sP}` : `une série de ${localGroupe.value.freqNum} pièces`);
    txt += ` (${libelleFreq})`;
  }
  
  return txt;
});

const verifierVariables = () => {
  const groupe = localGroupe.value; 
  const typeSec = (store.typesSection || []).find(ts => ts.id === groupe.typeSectionId);
  let texteBase = typeSec ? typeSec.libelle : "???";
  let titre = `Caractéristiques à contrôler ${texteBase}`;

  if (groupe.modeFreq === 'SANS') {
      localGroupe.value.periodiciteId = null;
      localGroupe.value.regleEchantillonnageId = null;
      localGroupe.value.isNewFreq = false;
      localGroupe.value.libelleSection = titre;
  }
  else if (groupe.modeFreq === 'FIXE') {
      localGroupe.value.isNewFreq = false;
      const regle = (store.reglesEchantillonnage || []).find(r => r.id === groupe.regleEchantillonnageId);
      if (regle) {
          localGroupe.value.libelleSection = `${titre} (${regle.libelle})`;
      } else {
          localGroupe.value.libelleSection = titre;
      }
  }
  else if (groupe.modeFreq === 'VARIABLE') {
      let libelleFreq = "";
      const sP = groupe.freqNum > 1 ? 's' : '';
      
      if (groupe.typeVariable === 'HEURE') {
          const sH = groupe.freqHours > 1 ? 's' : '';
          if (groupe.freqNum === 1 && groupe.freqHours === 1) {
              libelleFreq = "100% des pièces/h";
          } else if (groupe.freqHours === 1) {
              libelleFreq = `${groupe.freqNum} pièce${sP} / heure`;
          } else {
              libelleFreq = `${groupe.freqNum} pièce${sP} / ${groupe.freqHours} heure${sH}`;
          }
      } else if (groupe.typeVariable === 'ECHANTILLON') {
          libelleFreq = `${groupe.freqNum} échantillon${sP}`;
      } else {
          libelleFreq = `une série de ${groupe.freqNum} pièces`;
      }

      const perio = (store.periodicites || []).find(p => p.libelle.toLowerCase() === libelleFreq.toLowerCase());
      
      if (perio) {
          localGroupe.value.periodiciteId = perio.id;
          localGroupe.value.isNewFreq = false;
          localGroupe.value.libelleSection = `${titre} (${perio.libelle})`;
      } else {
          localGroupe.value.periodiciteId = null;
          localGroupe.value.isNewFreq = true;
          localGroupe.value.libelleSection = `${titre} (${libelleFreq})`;
      }
  }
};

watch(() => store.entete.operationCode, (newOp) => {
  if (isReadOnly.value) return;
  if (newOp !== 'ASS') {
    if (localGroupe.value.modeFreq === 'FIXE') {
      updateGroupe('modeFreq', 'SANS');
      updateGroupe('regleEchantillonnageId', null);
    }
  }
});

const ajouterLigne = () => {
  if (isReadOnly.value) return;
  // Suppression de la vérification du typeSectionId pour permettre l'ajout de lignes même sur une section libre
  
  const nouvelleLigne = {
    id: crypto.randomUUID(),
    typeCaracteristiqueId: null,
    typeControleId: null,
    moyenControleId: null,
    moyenTexteLibre: '',
    instrumentCode: null,
    limiteSpecTexte: '', 
    unite: '',          
    instruction: '',
    observations: '',
    estCritique: false
  };
  
  localGroupe.value.lignes = [...localGroupe.value.lignes, nouvelleLigne];
  emit('update-groupe', JSON.parse(JSON.stringify(localGroupe.value)));
};
</script>
