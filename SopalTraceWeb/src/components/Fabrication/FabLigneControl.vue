<template>
  <tr class="hover:bg-blue-50/20 transition-colors group">
    <!-- Caractéristique contrôlée -->
    <td class="p-2 border-r border-slate-200 align-top">
      <div class="flex flex-col gap-2 w-full">
        <!-- 1. Catégorie -->
        <select :value="localLigne.typeCaracteristiqueId" @change="(e) => updateLigne('typeCaracteristiqueId', e.target.value)" class="w-full bg-slate-50 border border-slate-200 rounded px-2 py-1.5 text-[11px] font-bold text-slate-700 outline-none focus:border-blue-500 cursor-pointer">
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
      <select :value="localLigne.typeControleId" @change="(e) => updateLigne('typeControleId', e.target.value)" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer">
        <option :value="null" disabled>-- Type de contrôle * --</option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.libelle }}</option>
      </select>
    </td>

    <!-- Moyen de contrôle (Désactivé si VISUEL) -->
    <td class="p-2 border-r border-slate-200 align-top">
      <select :value="localLigne.moyenControleId"
              @change="(e) => updateLigne('moyenControleId', e.target.value)"
              :disabled="isVisuel" 
              :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'cursor-pointer bg-white'"
              class="w-full border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
        <option :value="null" disabled>-- Moyen de contrôle * --</option>
        <option v-for="mc in (store.moyensControle || [])" :key="mc.id" :value="mc.id">{{ mc.libelle }}</option>
      </select>
    </td>

    <!-- Code instrument (FUSION DES 2 LISTES + Désactivé si VISUEL) -->
    <td class="p-2 border-r border-slate-200 align-top">
      <select :value="instrumentSelection"
              @change="(e) => updateInstrumentSelection(e.target.value)"
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
      <input :value="localLigne.instruction" @input="(e) => updateLigne('instruction', e.target.value)" type="text" placeholder="Observations (Optionnel)..." 
             class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-xs text-slate-600 outline-none focus:border-blue-500">
    </td>

    <!-- Actions (Masqué par défaut, apparaît au survol de la ligne grâce à 'group-hover') -->
    <td class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity">
      <button @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
        <i class="pi pi-trash"></i>
      </button>
    </td>
  </tr>
</template>

<script setup>
import { computed, watch, ref } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';

const props = defineProps({
  ligne: { type: Object, required: true }
});

const emit = defineEmits(['remove', 'update-ligne']);

const store = useFabModeleStore();

// État local synchronisé avec les props
const localLigne = ref({ ...props.ligne });

// Sync local state with props changes
watch(() => props.ligne, (newLigne) => {
  localLigne.value = { ...newLigne };
}, { deep: true });

const updateLigne = (key, value) => {
  localLigne.value[key] = value;
  emit('update-ligne', { ...localLigne.value });
};

// =========================================================================
// INTELLIGENCE : DÉTECTION ET NETTOYAGE DU MODE VISUEL
// =========================================================================

// 1. Détecte en temps réel si la ligne est un contrôle visuel
const isVisuel = computed(() => {
  const typeCtrl = (store.typesControle || []).find(t => t.id === localLigne.value.typeControleId);
  const typeCarac = (store.typesCaracteristique || []).find(t => t.id === localLigne.value.typeCaracteristiqueId);
  
  return (typeCtrl?.code === 'VISUEL') || (typeCarac?.code === 'VISUEL');
});

// 2. Si on bascule sur Visuel, on nettoie automatiquement les sélections d'instruments pour éviter les incohérences en base
watch(isVisuel, (devenuVisuel) => {
  if (devenuVisuel) {
    emit('update-ligne', {
      ...localLigne.value,
      moyenControleId: null,
      groupeInstrumentId: null,
      instrumentCode: null
    });
  }
});

// =========================================================================
// GESTION DES LISTES D'INSTRUMENTS DYNAMIQUES
// =========================================================================

const instrumentSelection = computed(() => {
  if (localLigne.value.groupeInstrumentId) return 'GRP|' + localLigne.value.groupeInstrumentId;
  if (localLigne.value.instrumentCode) return 'INS|' + localLigne.value.instrumentCode;
  return null;
});

const updateInstrumentSelection = (val) => {
  if (!val) {
    updateLigne('groupeInstrumentId', null);
    updateLigne('instrumentCode', null);
  } else if (val.startsWith('GRP|')) {
    updateLigne('groupeInstrumentId', val.split('|')[1]);
    updateLigne('instrumentCode', null);
  } else if (val.startsWith('INS|')) {
    updateLigne('groupeInstrumentId', null);
    updateLigne('instrumentCode', val.split('|')[1]);
  }
};
</script>