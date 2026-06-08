<template>
  <Teleport to="body">
    <div v-if="visible" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div class="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-md mx-4 overflow-hidden">
        <!-- Header -->
        <div class="bg-slate-900 text-white px-6 py-4 flex items-center justify-between">
          <div class="flex items-center gap-3">
            <i :class="['text-xl', internalAddPieceType.startsWith('F') ? 'ri-drop-line text-blue-400' : 'ri-cube-line text-emerald-400']"></i>
            <div>
              <p class="text-xs font-bold uppercase tracking-widest text-slate-400">
                {{ internalAddPieceType.startsWith('F') ? 'Étalon Fuite' : 'Pièce Référence' }}
              </p>
              <p class="text-base font-black">Ajouter dans le référentiel</p>
            </div>
          </div>
          <button @click="closeModal" class="text-slate-400 hover:text-white transition-colors">
            <i class="ri-close-line text-2xl"></i>
          </button>
        </div>

        <!-- Body -->
        <div class="p-6 space-y-4">
          <!-- Type Pièce -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Type</label>
            <select v-model="internalAddPieceType" class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm font-semibold text-slate-800 outline-none focus:border-slate-600 bg-slate-50">
              <option v-for="opt in typePieceOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
            </select>
          </div>

          <!-- Code -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Code *</label>
            <input
              v-model="newPiece.code"
              type="text"
              placeholder="Ex: PR-001, ET-FUITE-A"
              class="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm font-bold text-slate-800 uppercase outline-none focus:border-slate-900 bg-white tracking-widest"
              @keydown.enter="confirmAddPiece"
            />
          </div>

          <!-- Désignation -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Désignation <span class="text-slate-400 font-normal normal-case">(optionnel)</span></label>
            <input
              v-model="newPiece.designation"
              type="text"
              placeholder="Description courte..."
              class="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm text-slate-700 outline-none focus:border-slate-900 bg-white"
            />
          </div>

          <!-- Machine (pré-remplie) -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Machine associée <span class="text-slate-400 font-normal normal-case">(optionnel)</span></label>
            <input
              v-model="newPiece.machineCode"
              type="text"
              placeholder="Code machine"
              class="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm text-slate-700 outline-none focus:border-slate-900 bg-white"
            />
          </div>
        </div>

        <!-- Footer -->
        <div class="bg-slate-50 border-t border-slate-200 px-6 py-4 flex justify-end gap-3">
          <button
            @click="closeModal"
            class="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-100 transition-colors"
          >
            Annuler
          </button>
          <button
            @click="confirmAddPiece"
            :disabled="isCreatingPiece || !newPiece.code.trim()"
            class="px-6 py-2 text-sm font-bold text-white bg-slate-900 rounded-lg hover:bg-slate-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            <i :class="isCreatingPiece ? 'ri-loader-4-line animate-spin' : 'ri-add-circle-fill'"></i>
            {{ isCreatingPiece ? 'Création...' : 'Créer & Sélectionner' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import { useToast } from 'primevue/usetoast';
import apiClient from '@/services/apiClient';
import { useVerifMachineStore } from '@/stores/verifMachineStore';

const props = defineProps({
  visible: { type: Boolean, default: false },
  addPieceType: { type: String, default: 'PRC' },
  addPieceContext: { type: Object, default: null }
});

const emit = defineEmits(['update:visible', 'update:addPieceType', 'piece-created']);

const toast = useToast();
const store = useVerifMachineStore();

const internalAddPieceType = ref(props.addPieceType);
const isCreatingPiece = ref(false);

const newPiece = ref({
  code: '',
  designation: '',
  machineCode: '',
});

watch(() => props.addPieceType, (val) => {
  internalAddPieceType.value = val;
});
watch(() => props.visible, (val) => {
  if (val) {
    newPiece.value = { code: '', designation: '', machineCode: store.entete.machineCode || '' };
  }
});

const typePieceOptions = computed(() => {
  if (internalAddPieceType.value === 'FEC' || internalAddPieceType.value === 'FENC') {
    return [{ label: 'Fuite Étalon Conforme (FEC)', value: 'FEC' }, { label: 'Fuite Étalon Non Conforme (FENC)', value: 'FENC' }];
  }
  return [{ label: 'Pièce Réf. Conforme (PRC)', value: 'PRC' }, { label: 'Pièce Réf. Non Conforme (PRNC)', value: 'PRNC' }];
});

const closeModal = () => {
  emit('update:visible', false);
};

const confirmAddPiece = async () => {
  if (!newPiece.value.code.trim()) {
    toast.add({ severity: 'warn', summary: 'Code requis', detail: 'Veuillez saisir un code pour la pièce.', life: 3000 });
    return;
  }

  isCreatingPiece.value = true;
  try {
    const payload = {
      code: newPiece.value.code.trim().toUpperCase(),
      typePiece: internalAddPieceType.value,
      designation: newPiece.value.designation.trim() || null,
      machineCode: newPiece.value.machineCode || null,
      familleDesc: null
    };

    const res = await apiClient.post('/referentiels/piece-reference', payload);
    const created = res.data.data;

    // Ajouter dans le store local selon le type
    const isFuite = ['FEC', 'FENC'].includes(internalAddPieceType.value);
    if (isFuite) {
      store.fuitesEtalon.push(created);
    } else {
      store.piecesReference.push(created);
    }

    // Auto-sélectionner la nouvelle pièce dans la cellule concernée
    const ctx = props.addPieceContext;
    if (ctx) {
      store.setPieceValue(ctx.row, ctx.familleCorpsId, internalAddPieceType.value, created.id);
    }

    toast.add({ severity: 'success', summary: 'Créé', detail: `"${created.code}" ajouté avec succès.`, life: 3000 });
    
    emit('piece-created', created);
    closeModal();
  } catch (err) {
    const msg = err?.message || 'Erreur lors de la création.';
    toast.add({ severity: 'error', summary: 'Erreur', detail: msg, life: 4000 });
  } finally {
    isCreatingPiece.value = false;
  }
};
</script>
