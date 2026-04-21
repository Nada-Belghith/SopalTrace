<template>
  <tr v-if="localLigne" class="hover:bg-blue-50/20 transition-colors group">

    <td class="p-2 align-top">
      <div class="flex items-center gap-1">
        <!-- Si la ligne vient du modèle : lecture seule -->
        <div v-if="localLigne.modeleLigneSourceId" class="w-full bg-slate-50 border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 font-bold overflow-hidden text-ellipsis whitespace-nowrap cursor-not-allowed" :title="getCaracteristiqueLibelle(localLigne.typeCaracteristiqueId)">
          {{ getCaracteristiqueLibelle(localLigne.typeCaracteristiqueId) }}
        </div>

        <!-- Sinon : sélection libre + création à la volée -->
        <template v-else>
          <select v-model="localLigne.typeCaracteristiqueId" :disabled="isArchived" :class="['w-full rounded px-2 py-1.5 text-[11px] outline-none focus:border-blue-500', isArchived ? 'bg-slate-100 border-slate-200 text-slate-500 cursor-not-allowed' : 'bg-white border border-slate-200 text-slate-700 cursor-pointer']">
            <option :value="null">-- Aucune caractéristique --</option>
            <option v-for="car in safeCaracteristiques" :key="car.id" :value="car.id">
              {{ car.libelle }}
            </option>
          </select>

          <button v-if="!isArchived"
                  @click="showAddCaractModal = true"
                  class="p-1.5 bg-blue-50 text-blue-600 hover:bg-blue-500 hover:text-white rounded border border-blue-200 transition-colors"
                  title="Créer une nouvelle caractéristique">
            <i class="pi pi-plus text-xs"></i>
          </button>

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
        </template>
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 bg-blue-50/40 align-top text-center w-[12%]">
          <input v-model="localLigne.valeurNominale" type="text" placeholder="Ex: Ø13"
             :disabled="isVisuel || isArchived"
             :class="isVisuel || isArchived ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white'"
             class="w-full text-center font-semibold text-slate-700 border border-slate-300 rounded px-2 py-1 outline-none focus:border-blue-500 text-sm transition-opacity">
    </td>

    <td class="p-2 border-r border-slate-200 align-top min-w-[150px]">
      <div class="flex flex-col gap-1">

        <div v-if="!isArchived" class="flex items-center justify-center gap-3 bg-slate-100 rounded p-1 mb-1" :class="{'opacity-50 pointer-events-none': isVisuel}">
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
                 :disabled="isArchived"
                 :class="isArchived ? 'bg-slate-100 border-slate-200 text-slate-900 font-black cursor-not-allowed' : 'bg-white border-slate-300 focus:border-blue-500'"
                 class="w-full text-center border rounded px-2 py-1.5 outline-none text-[11px]">
        </div>

        <div v-if="typeLimite === 'NUM'">
          <div class="flex items-center gap-1">
            <input v-model.number="localLigne.toleranceInferieure"
                   type="number" step="any" placeholder="Min"
                   :disabled="isVisuel || isArchived"
                   :class="isVisuel || isArchived ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white border-slate-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-slate-700'"
                   class="w-1/2 text-center border rounded px-1 py-1 outline-none text-[11px]">
            <input v-model.number="localLigne.toleranceSuperieure"
                   type="number" step="any" placeholder="Max"
                   :disabled="isVisuel || isArchived"
                   :class="isVisuel || isArchived ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white border-slate-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 text-slate-700'"
                   class="w-1/2 text-center border rounded px-1 py-1 outline-none text-[11px]">
          </div>
        </div>

      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top w-[12%]">
      <select v-model="localLigne.typeControleId" :disabled="isArchived" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer disabled:opacity-60 disabled:bg-slate-50">
        <option :value="null" disabled>-- Type * --</option>
        <option v-for="tco in (store.typesControle || [])" :key="tco.id" :value="tco.id">{{ tco.code }}</option>
      </select>
    </td>

    <td class="p-2 border-r border-slate-200 align-top w-[12%]">
      <select v-model="localLigne.moyenControleId" :disabled="isArchived" class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 cursor-pointer disabled:opacity-60 disabled:bg-slate-50">
        <option :value="null">-- Moyen --</option>
        <option v-for="mco in (store.moyensControle || [])" :key="mco.id" :value="mco.id">{{ mco.libelle }}</option>
      </select>
    </td>

    <!-- Code instrument (Éditable - Combobox) -->
    <td class="p-2 border-r border-slate-200 align-top w-[12%]">
      <div class="flex gap-1 items-center">
        <select @change="(e) => { if (e.target.value) localLigne.instrumentCode = e.target.value; e.target.value = ''; }"
                :disabled="isVisuel"
                :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white cursor-pointer'"
                class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[11px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
          <option value="">-- Outil --</option>
          <option v-for="ins in (store.instruments || [])" :key="ins.codeInstrument" :value="ins.codeInstrument">
            {{ ins.codeInstrument }}
          </option>
        </select>
        <input v-model="localLigne.instrumentCode" type="text" placeholder="Perso"
               :disabled="isVisuel"
               :class="isVisuel ? 'opacity-50 cursor-not-allowed bg-slate-100' : 'bg-white'"
               class="flex-1 border border-slate-200 rounded px-2 py-1.5 text-[10px] text-slate-700 outline-none focus:border-blue-500 transition-opacity">
      </div>
    </td>

    <td class="p-2 border-r border-slate-200 align-top w-[18%] min-w-[180px]">
      <input v-model="localLigne.observations" type="text" placeholder="Infos (Optionnel)..." :disabled="isArchived"
             class="w-full bg-white border border-slate-200 rounded px-2 py-1.5 text-xs text-slate-600 outline-none focus:border-blue-500 disabled:opacity-60 disabled:bg-slate-50">
    </td>

    <td v-if="!isArchived" class="p-2 align-middle text-center opacity-0 group-hover:opacity-100 transition-opacity w-8">
      <button @click="$emit('remove', localLigne.id)" class="text-slate-300 hover:text-red-500 transition-colors p-2 rounded-lg hover:bg-red-50" title="Supprimer cette ligne">
        <i class="pi pi-trash"></i>
      </button>
    </td>
    <td v-else class="p-2 w-8"></td>
  </tr>
</template>

<script setup>
import { ref, computed, watch, nextTick, onMounted } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { qualityPlansService } from '@/services/qualityPlansService';
import { useToast } from 'primevue/usetoast';

// PrimeVue components (utilisés dans le template)
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';

const props = defineProps({
  ligne: { type: Object, required: true },
  section: { type: Object, required: true },
  isArchived: { type: Boolean, default: false }
});

const emit = defineEmits(['remove', 'update']);

const store = useFabModeleStore();
const toast = useToast();

// Local reactive copy to avoid mutating prop directly
const localLigne = ref({ ...props.ligne });
const isSyncingFromParent = ref(false);

// --- Logique Limite spécif. (NUM / TEXT) ---
const typeLimite = ref('NUM');

onMounted(() => {
  if (localLigne.value.limiteSpecTexte && localLigne.value.limiteSpecTexte.trim() !== '') {
    typeLimite.value = 'TEXT';
  }
});

watch(typeLimite, (newType) => {
  if (isSyncingFromParent.value) return;
  if (newType === 'TEXT') {
    localLigne.value.toleranceInferieure = null;
    localLigne.value.toleranceSuperieure = null;
  } else {
    localLigne.value.limiteSpecTexte = '';
  }
});

watch(() => props.ligne, (n) => {
  if (!n) return;
  isSyncingFromParent.value = true;
  localLigne.value = { ...n };
  // Sync typeLimite uniquement si limiteSpecTexte est rempli (mode TEXT)
  // Ne jamais forcer NUM depuis le parent : typeLimite est un état UI local
  if (n.limiteSpecTexte && n.limiteSpecTexte.trim() !== '') {
    typeLimite.value = 'TEXT';
  }
  nextTick(() => { isSyncingFromParent.value = false; });
}, { deep: true });

watch(localLigne, (n) => { if (!isSyncingFromParent.value) emit('update', { ...n }); }, { deep: true });

// --- Création de caractéristique à la volée (comme dans FabLigneControl) ---
const showAddCaractModal = ref(false);
const isSavingCaract = ref(false);
const newCaract = ref({ libelle: '' });

const safeCaracteristiques = computed(() => store.typesCaracteristique || store.caracteristiques || []);

const creerCaracteristique = async () => {
  if (!newCaract.value.libelle || isSavingCaract.value) return;
  isSavingCaract.value = true;

  try {
    const payload = { libelle: newCaract.value.libelle.trim() };
    const res = await qualityPlansService.createCaracteristique(payload);
    const created = res.data.data || res.data;

    const item = {
      id: created.id || created.Id || created.caracteristiqueId,
      libelle: created.libelle || created.Libelle || newCaract.value.libelle
    };

    // Ajouter au store puis sélectionner
    store.typesCaracteristique.push(item);
    localLigne.value.typeCaracteristiqueId = item.id;

    showAddCaractModal.value = false;
    newCaract.value.libelle = '';

    toast.add({ severity: 'success', summary: 'Caractéristique créée', detail: 'Nouvelle caractéristique ajoutée.', life: 3000 });
  } catch (err) {
    console.error('Erreur création caractéristique', err);
    toast.add({ severity: 'error', summary: 'Erreur', detail: err.response?.data?.message || 'Impossible de créer la caractéristique.', life: 4000 });
  } finally {
    isSavingCaract.value = false;
  }
};

// ⚠️ UTILITAIRE : Trouver le nom d'une caractéristique par son ID
const getCaracteristiqueLibelle = (id) => {
  const list = store.typesCaracteristique || store.caracteristiques || [];
  const tc = list.find(c => c.id === id);
  return tc ? tc.libelle : '--Sans caractéristique--';
};

// ⚠️ SECURITE
const isVisuel = computed(() => {
  if (!localLigne.value) return false;
  const typeCtrl = (store.typesControle || []).find(t => t.id === localLigne.value.typeControleId);
  return typeCtrl?.code === 'VISUEL';
});

watch(isVisuel, (devenuVisuel) => {
  if (devenuVisuel && localLigne.value && !isSyncingFromParent.value) {
    typeLimite.value = 'TEXT';
    localLigne.value.instrumentCode = null;
    localLigne.value.moyenTexteLibre = '';
    localLigne.value.valeurNominale = null;
    localLigne.value.toleranceInferieure = null;
    localLigne.value.toleranceSuperieure = null;
    localLigne.value.limiteSpecTexte = '';
  }
});


</script>
