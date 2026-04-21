<template>
  <tr class="hover:bg-blue-50/20 transition-colors group">
    <td class="p-2 align-top">
      <div class="flex items-center gap-1">
        <select v-model="localLigne.typeCaracteristiqueId" :disabled="isReadOnly" :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none focus:border-blue-500', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-700 cursor-pointer']">
          <option :value="null" disabled>-- Caractéristique * --</option>
          <option v-for="car in (store.caracteristiques || store.typesCaracteristiques || store.typesCaracteristique || [])" :key="car.id" :value="car.id">
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
    <td class="p-2 border-r border-slate-200 align-middle bg-slate-50/50">
      <div v-if="!isVisuel" class="text-[10px] text-slate-400 font-bold text-center italic select-none">
          Défini au plan article
      </div>
      <div v-else class="text-[12px] text-slate-300 font-black text-center select-none" title="Pas de valeur numérique pour un contrôle visuel">
          -
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
        <select v-model="localLigne.typeControleId" :disabled="isReadOnly" :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none focus:border-blue-500', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-700 cursor-pointer']">
        <option :value="null" disabled>-- Type de contrôle * --</option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.libelle }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
            <select v-model="localLigne.moyenControleId" :disabled="isReadOnly" 
              :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none transition-opacity', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'cursor-pointer bg-white border border-slate-200 text-slate-700']">
        <option :value="null" disabled>-- Moyen de contrôle * --</option>
        <option v-for="mc in (store.moyensControle || [])" :key="mc.id" :value="mc.id">{{ mc.libelle }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
      <div class="flex gap-1">
          <select 
          @change="(e) => { if (e.target.value) localLigne.instrumentCode = e.target.value; e.target.value = ''; }"
          :disabled="isVisuel || isReadOnly"
          :class="(isVisuel || isReadOnly) ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'cursor-pointer bg-white'"
          class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
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
          :class="(isVisuel || isReadOnly) ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white'"
          class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity" />
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top">
            <input v-model="localLigne.instruction" type="text" placeholder="Observations (Optionnel)..." :disabled="isReadOnly"
              :class="['w-full rounded px-2 py-1.5 text-xs outline-none focus:border-blue-500', isReadOnly ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-600']">
    </td>

    <td class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity">
      <button v-if="!isReadOnly" @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
        <i class="pi pi-trash"></i>
      </button>
    </td>
  </tr>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { qualityPlansService } from '@/services/qualityPlansService';
import { useToast } from 'primevue/usetoast';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';

const toast = useToast();

const props = defineProps({
  ligne: { type: Object, required: true },
  isReadOnly: { type: Boolean, default: false }
});

const isReadOnly = computed(() => props.isReadOnly);

const emit = defineEmits(['remove', 'update']);

// Local copy to avoid mutating prop directly
const localLigne = ref({ ...props.ligne });

const store = useFabModeleStore();

// =========================================================================
// LOGIQUE DE CRÉATION DE CARACTÉRISTIQUE À LA VOLÉE
// =========================================================================
const showAddCaractModal = ref(false);
const isSavingCaract = ref(false);

const newCaract = ref({
  libelle: '',
  estNumerique: true,
  uniteDefaut: ''
});

const creerCaracteristique = async () => {
  if (!newCaract.value.libelle) return;
  isSavingCaract.value = true;

  try {
    const payload = {
      libelle: newCaract.value.libelle.trim(),
      estNumerique: newCaract.value.estNumerique,
      uniteDefaut: newCaract.value.estNumerique ? newCaract.value.uniteDefaut.trim() : null
    };

    const response = await qualityPlansService.createCaracteristique(payload);
    const caractCree = response.data.data;
    
    // ⚠️ CORRECTION ICI : On utilise le bon nom de variable du store
    store.typesCaracteristique.push(caractCree);

    // Sélectionne automatiquement la nouvelle caractéristique sur la copie locale
    localLigne.value.typeCaracteristiqueId = caractCree.id;
    emit('update', { ...localLigne.value });

    toast.add({ severity: 'success', summary: 'Créée !', detail: 'La caractéristique a été ajoutée avec succès.', life: 3000 });
    
    // Reset et fermeture
    showAddCaractModal.value = false;
    newCaract.value = { libelle: '', estNumerique: true, uniteDefaut: '' };

  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Impossible de créer.', life: 5000 });
  } finally {
    isSavingCaract.value = false;
  }
};


// =========================================================================
// INTELLIGENCE : DÉTECTION ET NETTOYAGE DU MODE VISUEL
// =========================================================================

// 1. Détecte en temps réel si la ligne est un contrôle visuel UNIQUEMENT via le Type de Contrôle
const isVisuel = computed(() => {
  const typeCtrl = (store.typesControle || []).find(t => t.id === localLigne.value.typeControleId);
  return (typeCtrl?.code === 'VISUEL');
});

// 2. Si on bascule sur Visuel, on nettoie automatiquement les instruments physiques uniquement
watch(isVisuel, (devenuVisuel) => {
  if (devenuVisuel) {
    localLigne.value.instrumentCode = null;
  }
});

// =========================================================================
// CORRECTION DE LA BOUCLE INFINIE DE REACTIVITÉ
// =========================================================================

// Watch prop updates (from parent) and sync to local copy
watch(() => props.ligne, (newVal) => {
  if (!newVal) return;
  const sourceSource = JSON.stringify(newVal);
  const sourceLocale = JSON.stringify(localLigne.value);
  
  // Ne met à jour que si les données venant du parent sont différentes
  if (sourceSource !== sourceLocale) {
    localLigne.value = JSON.parse(sourceSource);
  }
}, { deep: true });

// Watch local copy and emit updates to parent
watch(localLigne, (newVal) => {
  const sourceLocale = JSON.stringify(newVal);
  const sourceSource = JSON.stringify(props.ligne);
  
  // N'émet un changement que si l'utilisateur a réellement modifié la valeur
  if (sourceLocale !== sourceSource) {
    emit('update', JSON.parse(sourceLocale));
  }
}, { deep: true });

</script>
