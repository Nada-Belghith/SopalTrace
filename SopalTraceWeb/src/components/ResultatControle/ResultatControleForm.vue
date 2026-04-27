<template>
  <div class="space-y-6 max-w-[1200px] mx-auto animate-fade-in">
      <Toast />
      <ConfirmDialog />
      <section class="bg-white rounded-xl shadow-sm border border-slate-200 p-5">
          <h2 class="text-sm font-bold text-slate-700 mb-4 flex items-center gap-2 uppercase tracking-wide">
              <i class="ri-map-pin-user-line text-emerald-500"></i> 1. Contexte du Poste
          </h2>
          <div class="max-w-md">
              <label class="block text-xs font-bold text-slate-500 mb-1">Poste de travail concerné</label>
              <select v-model="store.entete.posteCode" @change="onSelectionChange" :disabled="isReadOnly || store.entete.id" class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none bg-slate-50 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed">
                  <option value="">-- Choisir un poste --</option>
                  <option v-for="poste in store.postes" :key="poste.code" :value="poste.code">{{ poste.libelle }}</option>
              </select>
          </div>
      </section>
 
      <template v-if="store.planInitialise">
          <section class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden border-l-4 border-l-emerald-500">
              <div class="bg-slate-800 px-5 py-4 flex justify-between items-center">
                  <h2 class="text-sm font-bold text-white flex items-center gap-2 uppercase tracking-wide">
                      <i class="ri-list-check-2 text-emerald-400"></i> 2. Liste des Défauts à pointer (Lignes de la fiche)
                  </h2>
                  <div class="flex gap-2">
                      <input v-model="store.entete.nom" type="text" :disabled="isReadOnly" placeholder="Titre du document..." class="w-64 border-none rounded px-3 py-1.5 text-sm focus:ring-2 focus:ring-emerald-500 bg-slate-700 text-white disabled:bg-slate-800/50">
                      <button v-if="!isReadOnly" @click="store.ajouterLigne" class="bg-emerald-500 hover:bg-emerald-600 text-white text-xs font-bold py-1.5 px-3 rounded flex items-center gap-1 transition-colors">
                          <i class="ri-add-line"></i> Ajouter défaut
                      </button>
                  </div>
              </div>
              <div class="overflow-x-auto">
                  <table class="w-full text-left border-collapse text-sm">
                      <thead class="bg-slate-100 text-slate-700 text-[11px] uppercase tracking-wider font-bold border-b border-slate-300">
                          <tr>
                              <th class="p-3 border-r border-slate-300 w-12 text-center">N°</th>
                              <th class="p-3 border-r border-slate-300 w-[30%]">Machine (Source du défaut)</th>
                              <th class="p-3 border-r border-slate-300 w-[70%]">Désignation du défaut</th>
                              <th v-if="!isReadOnly" class="p-3 w-12 text-center">⚙️</th>
                          </tr>
                      </thead>
                      <tbody>
                          <tr v-for="(defaut, index) in store.lignes" :key="defaut._uid" class="border-b border-slate-200 hover:bg-emerald-50/30">
                              <td class="p-3 border-r text-center font-bold text-slate-500 bg-slate-50">{{ index + 1 }}</td>
                               <td class="p-2 border-r align-middle">
                                  <select v-model="defaut.machineCode" :disabled="isReadOnly" class="w-full text-xs font-bold text-slate-700 border border-slate-200 focus:border-emerald-500 rounded py-2 px-2 outline-none bg-transparent disabled:opacity-100 disabled:cursor-default">
                                      <option v-for="mac in machinesAssocieesAuPoste" :key="mac.code" :value="mac.code">{{ mac.code }} ({{ mac.libelle }})</option>
                                  </select>
                              </td>
                              <td class="p-2 border-r align-middle">
                                  <select v-model="defaut.risqueDefautId" :disabled="isReadOnly" class="w-full text-xs font-semibold text-slate-800 border border-transparent focus:border-emerald-500 rounded p-2 outline-none uppercase bg-transparent disabled:opacity-100 disabled:cursor-default">
                                      <option :value="null">-- Choisir un défaut --</option>
                                      <option v-for="rd in store.risquesDefauts" :key="rd.id" :value="rd.id">{{ rd.libelle }}</option>
                                  </select>
                              </td>
                              <td v-if="!isReadOnly" class="p-2 align-middle text-center bg-slate-50"><button @click="store.supprimerLigne(defaut._uid)" class="text-slate-400 hover:text-red-500 p-1"><i class="ri-delete-bin-fill text-lg"></i></button></td>
                          </tr>
                      </tbody>
                  </table>
              </div>
          </section>
          <div v-if="!isReadOnly" class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-6 rounded-b-xl">
             <EditorActions 
                :label="store.entete.id ? 'Sauvegarder les Modifications' : 'Créer et Activer le Plan'"
                loading-label="Sauvegarde..."
                :icon="store.entete.id ? 'pi pi-save' : 'pi pi-plus'"
                variant="primary"
                :is-loading="store.isLoading"
                @submit="handleSauvegarder"
                @cancel="onCancel"
             />
          </div>
      </template>
  </div>
</template>

<script setup>
import { onMounted, computed } from 'vue';
import { usePlanNcStore } from '@/stores/planNcStore';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import { useRouter, useRoute } from 'vue-router';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import EditorActions from '@/components/Shared/EditorActions.vue';

defineProps({
    isReadOnly: { type: Boolean, default: false }
});

const store = usePlanNcStore();
const toast = useToast();
const confirm = useConfirm();
const router = useRouter();
const route = useRoute();

const onCancel = () => {
    router.push('/dev/hub');
};

onMounted(async () => {
  await store.fetchDictionnaires();
  await store.fetchTousLesPlans();
  
  if (route.params.id) {
      await store.chargerPlanNc(route.params.id);
  }
});

const machinesAssocieesAuPoste = computed(() => 
  store.machines.filter(m => m.posteCode === store.entete.posteCode)
);

const onSelectionChange = () => {
  if (store.entete.posteCode) {
      store.initialiserNouveauPlan(store.entete.posteCode);
  } else {
      store.planInitialise = false;
  }
};

const handleSauvegarder = async () => {
    // 1. Détection de changement
    if (!store.aDesModifications()) {
        toast.add({ severity: 'info', summary: 'Information', detail: 'Aucun changement détecté.', life: 3000 });
        return;
    }

    // 2. Validation simple
    if (store.lignes.some(l => !l.machineCode || !l.risqueDefautId)) {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'Veuillez remplir toutes les machines et les défauts.', life: 3000 });
        return;
    }

    // 3. Confirmation d'archivage si on crée un nouveau plan et qu'un actif existe
    if (!store.entete.id) {
        const planActif = store.plansExistants.find(p => p.statut === 'ACTIF' && p.posteCode === store.entete.posteCode);
        if (planActif) {
            const isConfirmed = await new Promise((resolve) => {
                confirm.require({
                    message: `Une fiche de contrôle active existe déjà pour ce poste (${store.entete.posteCode}). Voulez-vous l'archiver et activer cette nouvelle version ?`,
                    header: 'Fiche Active Existante',
                    icon: 'ri-error-warning-line text-amber-500',
                    acceptLabel: 'Oui, archiver',
                    rejectLabel: 'Annuler',
                    accept: () => resolve(true),
                    reject: () => resolve(false)
                });
            });
            if (!isConfirmed) return;
        }
    }

    try {
        const res = await store.sauvegarderPlan();
        if (res.noChanges) {
             toast.add({ severity: 'info', summary: 'Info', detail: 'Pas de modification.', life: 3000 });
             return;
        }
        
        if (res.success) {
            toast.add({ severity: 'success', summary: 'Succès', detail: res.message || 'Sauvegarde réussie.', life: 3000 });
            await store.fetchTousLesPlans();
        }
    } catch {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'Une erreur est survenue lors de la sauvegarde.', life: 3000 });
    }
};

</script>

<style scoped>
textarea { resize: none; overflow: hidden; }
</style>
