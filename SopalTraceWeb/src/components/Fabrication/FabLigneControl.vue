<template>
  <tr class="hover:bg-blue-50/20 transition-colors group">
    <!-- Caractéristique contrôlée -->
    <td class="p-2 border-r border-slate-200 align-top">
      <div class="flex flex-col gap-2 w-full">
        <!-- 1. Catégorie -->
        <select v-model="ligne.typeCaracteristiqueId" class="w-full bg-slate-50 border border-slate-200 rounded px-2 py-1.5 text-[11px] font-bold text-slate-700 outline-none focus:border-blue-500 cursor-pointer">
          <option value="" disabled>Catégorie *</option>
          <option v-for="tc in (store.typesCaracteristique || [])" :key="tc.id" :value="tc.id">{{ tc.libelle }}</option>
        </select>
        
      </div>
    </td>

    <!-- Limite de spécification (Dynamique selon le type Visuel) -->
    <td class="p-2 border-r border-slate-200 align-middle bg-slate-50/50">
      <div v-if="!isVisuel" class="text-[10px] text-slate-400 font-bold text-center italic select-none">
          Défini au plan article
      </div>
      <div v-else class="text-[12px] text-slate-300 font-black text-center select-none" title="Pas de valeur numérique pour un contrôle visuel">
          -
      </div>
    </td>

    <!-- Type de contrôle -->
    <td class="p-2 border-r border-slate-200 align-top">
      <select v-model="ligne.typeControleId" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer">
        <option :value="null" disabled>-- Type de contrôle * --</option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.libelle }}</option>
      </select>
    </td>

    <!-- Moyen de contrôle (Désactivé si VISUEL) -->
    <td class="p-2 border-r border-slate-200 align-top">
      <select v-model="ligne.moyenControleId" 
              :disabled="isVisuel" 
              :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'cursor-pointer bg-white'"
              class="w-full border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
        <option :value="null" disabled>-- Moyen de contrôle * --</option>
        <option v-for="mc in (store.moyensControle || [])" :key="mc.id" :value="mc.id">{{ mc.libelle }}</option>
      </select>
    </td>

    <!-- Code instrument (FUSION DES 2 LISTES + Désactivé si VISUEL) -->
    <td class="p-2 border-r border-slate-200 align-top">
      <select v-model="instrumentSelection" 
              :disabled="isVisuel"
              :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'cursor-pointer bg-white'"
              class="w-full border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
        <option :value="null" disabled>-- Code instrument * --</option>
        
        <optgroup label="Groupes d'instruments">
          <option v-for="gi in (store.groupesInstruments || [])" :key="'grp_'+gi.id" :value="'GRP|'+gi.id">
            {{ gi.codeAlias }}
          </option>
        </optgroup>
        
        <optgroup label="Instruments">
          <option v-for="ins in (store.instruments || [])" :key="'ins_'+ins.codeInstrument" :value="'INS|'+ins.codeInstrument">
            {{ ins.codeInstrument }}
          </option>
        </optgroup>
      </select>
    </td>

    <!-- Observations -->
    <td class="p-2 border-r border-slate-200 align-top">
      <input v-model="ligne.instruction" type="text" placeholder="Observations (Optionnel)..." 
             class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-xs text-slate-600 outline-none focus:border-blue-500">
    </td>

    <!-- Actions (Masqué par défaut, apparaît au survol de la ligne grâce à 'group-hover') -->
    <td class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity">
      <button @click="$emit('remove', ligne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
        <i class="pi pi-trash"></i>
      </button>
    </td>
  </tr>
</template>

<script setup>
import { computed, watch } from 'vue';
import { computed } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';

const props = defineProps({
  ligne: { type: Object, required: true }
});

defineEmits(['remove']);

const store = useFabModeleStore();

// =========================================================================
// INTELLIGENCE : DÉTECTION ET NETTOYAGE DU MODE VISUEL
// =========================================================================

// 1. Détecte en temps réel si la ligne est un contrôle visuel
const isVisuel = computed(() => {
  const typeCtrl = (store.typesControle || []).find(t => t.id === props.ligne.typeControleId);
  const typeCarac = (store.typesCaracteristique || []).find(t => t.id === props.ligne.typeCaracteristiqueId);
  
  return (typeCtrl?.code === 'VISUEL') || (typeCarac?.code === 'VISUEL');
});

// 2. Si on bascule sur Visuel, on nettoie automatiquement les sélections d'instruments pour éviter les incohérences en base
watch(isVisuel, (devenuVisuel) => {
  if (devenuVisuel) {
    props.ligne.moyenControleId = null;
    props.ligne.groupeInstrumentId = null;
    props.ligne.instrumentCode = null;
  }
});

// =========================================================================
// GESTION DES LISTES D'INSTRUMENTS DYNAMIQUES
// =========================================================================

const instrumentSelection = computed({
  get: () => {
    if (props.ligne.groupeInstrumentId) return 'GRP|' + props.ligne.groupeInstrumentId;
    if (props.ligne.instrumentCode) return 'INS|' + props.ligne.instrumentCode;
    return null;
  },
  set: (val) => {
    if (!val) {
      props.ligne.groupeInstrumentId = null;
      props.ligne.instrumentCode = null;
    } else if (val.startsWith('GRP|')) {
      props.ligne.groupeInstrumentId = val.split('|')[1];
      props.ligne.instrumentCode = null;
    } else if (val.startsWith('INS|')) {
      props.ligne.groupeInstrumentId = null;
      props.ligne.instrumentCode = val.split('|')[1];
    }
  }
});
</script>