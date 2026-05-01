<template>
  <tr class="hover:bg-blue-50/10 transition-colors group">
    
    <td class="p-2 align-top">
      <div class="flex items-center gap-1">
        <select v-model="localLigne.typeCaracteristiqueId" :disabled="isReadOnly" 
                :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-700 cursor-pointer focus:border-blue-500']">
          <option :value="null" disabled>-- Caractéristique * --</option>
          <option v-for="car in safeCaracteristiques" :key="car.id" :value="car.id">
            {{ car.libelle }}
          </option>
        </select>
        
        <button v-if="!isReadOnly"
          @click="showAddCaractModal = true" 
          class="p-1.5 bg-blue-50 text-blue-600 hover:bg-blue-500 hover:text-white rounded border border-blue-200 transition-colors"
          title="Créer une nouvelle caractéristique"
        >
          <i class="pi pi-plus text-xs"></i>
        </button>
      </div>

      <Dialog v-model:visible="showAddCaractModal" header="Nouvelle Caractéristique" :modal="true" :style="{ width: '400px' }">
        <div class="flex flex-col gap-4 pt-2">
          <div class="flex flex-col gap-1">
            <label class="text-xs font-bold text-slate-700">Libellé (Nom) <span class="text-red-500">*</span></label>
            <InputText v-model="newCaract.libelle" placeholder="Ex: Rayon de courbure" class="w-full text-sm" />
          </div>
        </div>
        <template #footer>
          <Button label="Annuler" icon="pi pi-times" text @click="showAddCaractModal = false" class="p-button-sm text-slate-500" />
          <Button label="Créer et Sélectionner" icon="pi pi-check" @click="creerCaracteristique" :loading="isSavingCaract" :disabled="!newCaract.libelle" class="p-button-sm p-button-primary" />
        </template>
      </Dialog>
    </td>

    <td class="p-2 border-r border-slate-200 align-top min-w-[150px]">
      
      <div v-if="!isModeleGenerique" class="h-full w-full flex items-center justify-center p-2 bg-slate-50 rounded border border-dashed border-slate-200">
        <span class="text-[10px] font-bold text-slate-500 italic text-center leading-tight select-none">
          Les limites seront définies<br>lors de la création du Plan
        </span>
      </div>

      <div v-else class="flex flex-col gap-1">
        
        <div v-if="!isReadOnly" class="flex items-center justify-center gap-3 bg-slate-100 rounded p-1 mb-1" :class="{'opacity-50 pointer-events-none': isVisuel}">
          <label class="flex items-center gap-1 cursor-pointer text-[10px] font-bold text-slate-600">
            <input type="radio" v-model="typeLimite" value="NUM" class="accent-blue-600"> Chiffres
          </label>
          <label class="flex items-center gap-1 cursor-pointer text-[10px] font-bold text-slate-600">
            <input type="radio" v-model="typeLimite" value="TEXT" class="accent-blue-600"> Texte
          </label>
        </div>

        <div v-if="typeLimite === 'TEXT'">
          <input v-model="localLigne.limiteSpecTexte" 
                 type="text" 
                 placeholder="Ex: Selon plan..."
                 :disabled="isReadOnly"
                 :class="isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 focus:border-blue-500'"
                 class="w-full text-center border rounded px-2 py-1.5 outline-none text-[11px]">
        </div>

        <div v-if="typeLimite === 'NUM'">
          <div class="flex items-center gap-1">
            <input v-model.number="localLigne.toleranceInferieure" 
                   type="number" step="any" placeholder="Min"
                   :disabled="isReadOnly"
                   :class="isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-slate-700'"
                   class="w-1/2 text-center border rounded px-1 py-1 outline-none text-[11px]">
                   
            <input v-model.number="localLigne.toleranceSuperieure" 
                   type="number" step="any" placeholder="Max"
                   :disabled="isReadOnly"
                   :class="isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-slate-700'"
                   class="w-1/2 text-center border rounded px-1 py-1 outline-none text-[11px]">
          </div>
        </div>

      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
        <select v-model="localLigne.typeControleId" :disabled="isReadOnly" 
                :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-700 cursor-pointer focus:border-blue-500']">
        <option :value="null" disabled>-- Type * --</option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.libelle }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
      <select v-model="localLigne.moyenControleId" :disabled="isReadOnly" 
              :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'cursor-pointer bg-white border border-slate-200 text-slate-700 focus:border-blue-500']">
        <option :value="null" disabled>-- Moyen  --</option>
        <option v-for="mc in (store.moyensControle || [])" :key="mc.id" :value="mc.id">{{ mc.libelle }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
      <div class="flex gap-1">
          <select 
          @change="(e) => { if (e.target.value) localLigne.instrumentCode = e.target.value; e.target.value = ''; }"
          :disabled="isVisuel || isReadOnly"
          :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-50' : (isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-700 cursor-pointer focus:border-blue-500')"
          class="flex-1 border rounded px-2 py-1.5 text-[11px] outline-none transition-opacity">
          <option value="">-- Outil --</option>
          <option v-for="ins in (store.instruments || [])" :key="ins.codeInstrument" :value="ins.codeInstrument">
            {{ ins.codeInstrument }}
          </option>
        </select>
        <input 
          v-model="localLigne.instrumentCode" 
          type="text" 
          placeholder="Perso"
          :disabled="isVisuel || isReadOnly"
          :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-50' : (isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-bold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-700 focus:border-blue-500')"
          class="flex-1 border rounded px-2 py-1.5 text-[11px] outline-none transition-opacity" />
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
      <input v-model="localLigne.observations" type="text" placeholder="Observations (Optionnel)..." :disabled="isReadOnly"
             :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none border', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-900 font-semibold cursor-not-allowed' : 'bg-white border-slate-200 text-slate-600 focus:border-blue-500']">
    </td>

    <td class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity w-8">
      <button v-if="!isReadOnly" @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
        <i class="pi pi-trash"></i>
      </button>
    </td>
  </tr>
</template>

<script setup>
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { usePfPlanStore } from '@/stores/pfPlanStore';
import { qualityPlansService } from '@/services/qualityPlansService';
import { useToast } from 'primevue/usetoast';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';

const toast = useToast();

const props = defineProps({
  ligne: { type: Object, required: true },
  isReadOnly: { type: Boolean, default: false },
  operationCode: { type: String, default: '' } 
});

const isReadOnly = computed(() => props.isReadOnly);
const emit = defineEmits(['remove', 'update']);
const fabStore = useFabModeleStore();
const pfStore = usePfPlanStore();
const store = props.operationCode === 'PF' ? pfStore : fabStore;

const localLigne = ref({ ...props.ligne });
const isSyncingFromParent = ref(false);

const safeCaracteristiques = computed(() => store.caracteristiques || store.typesCaracteristiques || store.typesCaracteristique || []);

const currentOp = computed(() => props.operationCode || store.entete?.operationCode || '');
const isModeleGenerique = computed(() => {
  return ['ASS', 'TRN', 'TRONC', 'PF'].includes(currentOp.value.toUpperCase());
});

const typeLimite = ref('NUM');

onMounted(() => {
  if (localLigne.value.limiteSpecTexte && localLigne.value.limiteSpecTexte.trim() !== '') {
    typeLimite.value = 'TEXT';
  }
});

watch(typeLimite, (newType) => {
  if (isSyncingFromParent.value) return;

  if (newType === 'TEXT') {
    localLigne.value.valeurNominale = null; 
    localLigne.value.toleranceInferieure = null;
    localLigne.value.toleranceSuperieure = null;
  } else if (newType === 'NUM') {
    localLigne.value.limiteSpecTexte = '';
    localLigne.value.valeurNominale = null; 
  }
});

const isVisuel = computed(() => {
  const typeCtrl = (store.typesControle || []).find(t => t.id === localLigne.value.typeControleId);
  return (typeCtrl?.code === 'VISUEL');
});

watch(isVisuel, (devenuVisuel) => {
  if (devenuVisuel && localLigne.value && !isSyncingFromParent.value) {
    typeLimite.value = 'TEXT';
    localLigne.value.instrumentCode = null;
    localLigne.value.valeurNominale = null;
  }
});

const showAddCaractModal = ref(false);
const isSavingCaract = ref(false);
const newCaract = ref({ libelle: '' });

const creerCaracteristique = async () => {
  if (!newCaract.value.libelle) return;
  isSavingCaract.value = true;

  try {
    const payload = { libelle: newCaract.value.libelle.trim(), estNumerique: true };
    const response = await qualityPlansService.createCaracteristique(payload);
    const caractCree = response.data.data;
    
    if (store.typesCaracteristique) store.typesCaracteristique.push(caractCree);

    localLigne.value.typeCaracteristiqueId = caractCree.id;
    emit('update', { ...localLigne.value });

    toast.add({ severity: 'success', summary: 'Créée !', detail: 'La caractéristique a été ajoutée.', life: 3000 });
    showAddCaractModal.value = false;
    newCaract.value = { libelle: '' };
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Impossible de créer.', life: 5000 });
  } finally {
    isSavingCaract.value = false;
  }
};

watch(() => props.ligne, (newVal) => {
  if (!newVal) return;
  const sourceSource = JSON.stringify(newVal);
  const sourceLocale = JSON.stringify(localLigne.value);
  
  if (sourceSource !== sourceLocale) {
    isSyncingFromParent.value = true;
    localLigne.value = JSON.parse(sourceSource);
    
    localLigne.value.valeurNominale = null;
    
    if (localLigne.value.limiteSpecTexte && localLigne.value.limiteSpecTexte.trim() !== '') {
      typeLimite.value = 'TEXT';
    } else {
      typeLimite.value = 'NUM';
    }
    
    nextTick(() => { isSyncingFromParent.value = false; });
  }
}, { deep: true });

watch(localLigne, (newVal) => {
  if (isSyncingFromParent.value) return;
  newVal.valeurNominale = null; 
  const sourceLocale = JSON.stringify(newVal);
  const sourceSource = JSON.stringify(props.ligne);
  if (sourceLocale !== sourceSource) {
    emit('update', JSON.parse(sourceLocale));
  }
}, { deep: true });

</script>
