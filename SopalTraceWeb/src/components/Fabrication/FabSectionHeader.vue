<template>
  <tr class="bg-[#f1f5f9] border-t-4 border-slate-300">
    <td :colspan="colspan" class="p-3 px-4 relative">
      <div class="flex flex-col gap-3 pr-40">
        
        <div class="flex items-center gap-3">
          <span class="bg-blue-100 text-blue-800 text-[10px] font-black px-2 py-1.5 rounded-md uppercase tracking-widest shrink-0 shadow-sm">
              {{ label }} {{ index + 1 }}
          </span>
          
          <select v-model="localSection.typeSectionId" class="w-64 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-xs font-bold text-slate-800 outline-none focus:border-blue-500 shadow-sm cursor-pointer">
            <option value="" disabled>--Nature de la section--</option>
            <option v-for="ts in (store.typesSection || [])" :key="ts.id" :value="ts.id">{{ ts.libelle }}</option>
          </select>

          <select v-model="localSection.modeFreq" class="bg-slate-100 border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-600 outline-none focus:border-blue-500 shadow-sm cursor-pointer">
            <option value="SANS">Sans fréquence</option>
            <option value="VARIABLE">➕ Fréquence des pièces par heure</option>
            <option value="FIXE">➕ Règle d'Échantillonnage</option>
          </select>

          <!-- SI VARIABLE -->
          <div v-if="localSection.modeFreq === 'VARIABLE'" class="flex items-center gap-2 animate-in fade-in slide-in-from-left-4 bg-white border border-slate-300 rounded-lg p-1 shadow-sm">
              <input type="number" v-model.number="localSection.freqNum" min="1" max="1000" class="w-12 bg-slate-100 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs" />
              <span class="text-[10px] font-bold text-slate-500 uppercase">Pièce(s)</span>
              
              <select v-model="localSection.typeVariable" class="bg-transparent text-slate-700 font-bold outline-none cursor-pointer text-xs ml-1 border-l border-slate-200 pl-2">
                <option value="HEURE">/ Heure(s)</option>
                <option value="SERIE">par Série</option>
              </select>

              <template v-if="localSection.typeVariable === 'HEURE'">
                  <span class="text-[10px] font-bold text-slate-500 uppercase ml-1">Toutes les</span>
                  <input type="number" v-model.number="localSection.freqHours" min="1" max="24" class="w-12 bg-slate-100 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs" />
                  <span class="text-[10px] font-bold text-slate-500 uppercase pr-1">H</span>
              </template>
          </div>

          <!-- SI FIXE -->
          <div v-if="localSection.modeFreq === 'FIXE'" class="flex items-center animate-in fade-in slide-in-from-left-4">
              <select v-model="localSection.periodiciteId" class="w-64 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-700 outline-none focus:border-blue-500 shadow-sm cursor-pointer">
                <option :value="null" disabled>Sélectionner la règle...</option>
                <option v-for="p in periodicites" :key="p.id" :value="p.id">{{ p.libelle.substring(0, 35) }}{{ p.libelle.length > 35 ? '...' : '' }}</option>
              </select>
          </div>
        </div>

        <div class="w-full border text-[11px] font-black tracking-widest rounded px-3 py-2 flex items-center shadow-inner transition-colors bg-white border-slate-200 text-slate-700">
            <span class="text-blue-500 mr-2 uppercase">Aperçu :</span> {{ apercu || 'SÉLECTIONNEZ UNE NATURE POUR GÉNÉRER LE TITRE' }}
        </div>
      </div>

      <!-- Boutons de droite -->
      <div class="flex items-center gap-4 absolute right-4 top-1/2 -translate-y-1/2">
        <button @click="$emit('add-ligne')" class="text-blue-600 text-[11px] font-black uppercase tracking-widest hover:text-blue-800 flex items-center gap-1 transition-colors">
          <i class="pi pi-plus"></i> Ajouter ligne
        </button>
        <button @click="$emit('remove')" class="text-slate-400 hover:text-red-600 transition-colors ml-2" title="Supprimer la section">
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
  periodicites: { type: Array, default: () => [] }
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

const apercu = computed(() => {
  const typeSec = (store.typesSection || []).find(ts => ts.id === localSection.value.typeSectionId);
  if (!typeSec) return '';

  let txt = `Caractéristiques à contrôler ${typeSec.libelle}`;

  if (localSection.value.modeFreq === 'FIXE' && localSection.value.periodiciteId) {
    const per = (props.periodicites || []).find(p => p.id === localSection.value.periodiciteId);
    if (per) txt += ` (${per.libelle})`;
  } else if (localSection.value.modeFreq === 'VARIABLE') {
    const sP = (localSection.value.freqNum || 0) > 1 ? 's' : '';
    let libelleFreq = '';

    if (localSection.value.typeVariable === 'HEURE') {
      const h = localSection.value.freqHours || 1;
      const sH = h > 1 ? 's' : '';
      libelleFreq = h === 1
        ? `${localSection.value.freqNum || 1} pièce${sP} / heure`
        : `${localSection.value.freqNum || 1} pièce${sP} / ${h} heure${sH}`;
    } else {
      libelleFreq = `une série de ${localSection.value.freqNum || 1} pièces`;
    }

    txt += ` (${libelleFreq})`;
  }

  return txt;
});
</script>
