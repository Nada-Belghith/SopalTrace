<template>
  <div class="space-y-6 max-w-[1200px] mx-auto animate-fade-in">
      <Toast />
      <ConfirmDialog />
      <section class="bg-white rounded-xl shadow-sm border border-slate-200 p-6 mb-6">
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <h2 class="text-sm font-bold text-slate-800 flex items-center gap-2 mb-4 uppercase tracking-wide">
              <i class="ri-map-pin-user-line text-emerald-500"></i> 1. Contexte du Poste
            </h2>
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6 items-end">
              <div>
                <label class="block text-xs font-semibold text-slate-500 mb-2 uppercase">Poste de travail concerné</label>
                <select v-model="store.entete.posteCode" @change="onSelectionChange" :disabled="isReadOnly || store.entete.id" class="w-full border border-slate-200 rounded-lg py-2.5 px-4 text-sm focus:ring-4 focus:ring-emerald-500/10 focus:border-emerald-500 outline-none transition-all font-medium bg-slate-50">
                    <option value="">-- Choisir un poste --</option>
                    <option v-for="p in store.postes" :key="p.code" :value="p.code">
                        Poste {{ p.code }} - {{ p.libelle }}
                    </option>
                </select>
              </div>
              
              <div v-if="!isReadOnly && !store.entete.id" class="flex justify-end">
                <button @click="$refs.fileInput.click()" 
                        :disabled="store.isLoading || !store.entete.posteCode"
                        class="flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg text-sm font-bold transition-all shadow-lg shadow-emerald-500/20">
                  <i v-if="!store.isLoading" class="ri-file-excel-2-line text-lg"></i>
                  <i v-else class="ri-loader-4-line animate-spin text-lg"></i>
                  Importer Excel
                </button>
              </div>
            </div>
          </div>
        </div>
        
        <input type="file" ref="fileInput" @change="handleExcelImport" accept=".xlsx,.xls" class="hidden">
      </section>

      <template v-if="store.planInitialise">
          <section class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden border-l-4 border-l-emerald-500">
              <div class="bg-slate-800 px-5 py-4 flex items-center relative min-h-[64px]">
                  <div class="flex-1 text-center">
                      <h2 class="text-white font-bold uppercase tracking-widest text-lg">
                        Résultat de contrôle <span v-if="store.entete.posteCode">- Poste {{ store.entete.posteCode }}</span>
                      </h2>
                  </div>
                  <div v-if="!isReadOnly" class="absolute right-5">
                      <button @click="store.ajouterLigne" class="bg-emerald-500 hover:bg-emerald-600 text-white text-xs font-bold py-2 px-4 rounded-lg flex items-center gap-2 transition-all shadow-lg shadow-emerald-500/20">
                          <i class="ri-add-line text-base"></i> Ajouter défaut
                      </button>
                  </div>
              </div>
              <div class="overflow-x-auto overflow-y-visible">
                  <table class="w-full text-left border-collapse text-sm">
                      <thead class="bg-slate-100 text-slate-700 text-[11px] uppercase tracking-wider font-bold border-b border-slate-300">
                          <tr>
                              <th class="p-3 border-r border-slate-300 w-12 text-center">N°</th>
                              <th class="p-3 border-r border-slate-300 w-[30%]">Machine/ Banc d'essai</th>
                              <th class="p-3 border-r border-slate-300 w-[70%]">Désignation du défaut</th>
                              <th v-if="!isReadOnly" class="p-3 w-12 text-center"></th>
                          </tr>
                      </thead>
                      <tbody class="bg-white">
                          <tr v-for="(defaut, index) in store.lignes" :key="defaut._uid" 
                              class="border-b border-slate-200 transition-colors"
                              :class="isReadOnly ? 'hover:bg-slate-50' : 'hover:bg-emerald-50/40'">
                              <td class="p-3 border-r text-center font-bold text-slate-500 bg-slate-50/50">{{ index + 1 }}</td>
                              
                              <!-- Sélection Machine -->
                              <td class="p-2 border-r align-middle">
                                  <div v-if="isReadOnly" class="px-3 py-2 text-xs font-bold text-slate-700">
                                      {{ defaut.machineCode || 'N/A' }}
                                  </div>
                                  <select v-else 
                                          v-model="defaut.machineCode" 
                                          class="w-full text-xs font-bold text-slate-700 border border-slate-200 focus:border-emerald-500 rounded-lg py-2.5 px-3 outline-none bg-white shadow-sm transition-all focus:ring-4 focus:ring-emerald-500/10">
                                      <option v-for="mac in machinesAssocieesAuPoste" :key="mac.code" :value="mac.code">
                                          {{ mac.code }} ({{ mac.libelle }})
                                      </option>
                                  </select>
                              </td>

                              <!-- Sélection Défaut -->
                              <td class="p-2 border-r align-middle">
                                  <div v-if="isReadOnly" class="px-3 py-2 text-xs font-semibold text-slate-800 uppercase">
                                      {{ store.risquesDefauts.find(r => r.id === defaut.risqueDefautId)?.libelle || 'Aucun défaut sélectionné' }}
                                  </div>
                                  <input v-else 
                                          :value="store.risquesDefauts.find(r => r.id === defaut.risqueDefautId)?.libelle || defaut._libelleDefautBrut || ''"
                                          @input="(e) => onDefautInput(defaut, e.target.value)"
                                          class="w-full text-xs font-semibold text-slate-800 border border-slate-200 focus:border-emerald-500 rounded-lg py-2.5 px-3 outline-none bg-white shadow-sm transition-all uppercase focus:ring-4 focus:ring-emerald-500/10"
                                          placeholder="Saisir la désignation du défaut...">
                              </td>

                              <!-- Actions -->
                              <td v-if="!isReadOnly" class="p-2 align-middle text-center bg-slate-50/30">
                                  <button @click="store.supprimerLigne(defaut._uid)" 
                                          class="w-8 h-8 rounded-full flex items-center justify-center text-slate-400 hover:text-red-600 hover:bg-red-50 transition-all active:scale-95"
                                          title="Supprimer cette ligne">
                                      <i class="pi pi-trash"></i>
                                  </button>
                              </td>
                          </tr>
                      </tbody>
                  </table>

                  <!-- État vide -->
                  <div v-if="store.lignes.length === 0" class="p-12 text-center bg-slate-50/50">
                      <div class="inline-flex items-center justify-center w-12 h-12 rounded-full bg-slate-100 mb-3">
                          <i class="pi pi-list text-slate-400"></i>
                      </div>
                      <p class="text-sm text-slate-500 font-medium italic">Aucun défaut configuré pour ce poste.</p>
                      <button v-if="!isReadOnly" @click="store.ajouterLigne" class="mt-4 text-emerald-600 font-bold text-xs uppercase tracking-widest hover:underline">
                          + Ajouter un premier défaut
                      </button>
                  </div>
              </div>
          </section>

          <RemarquesLegendeBox
              v-model:remarques="store.entete.remarques"
              v-model:legendeMoyens="store.entete.legendeMoyens"
              :is-read-only="isReadOnly"
          />

          <div v-if="!isReadOnly" class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-6 rounded-b-xl">
             <EditorActions 
                :label="store.entete.id ? 'Sauvegarder les Modifications' : 'Enregistrer le Plan'"
                loading-label="Enregistrement..."
                :icon="store.entete.id ? 'pi pi-save' : 'pi pi-check'"
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
import { onMounted, computed, ref } from 'vue';
import { usePlanNcStore } from '@/stores/planNcStore';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import { useRouter, useRoute } from 'vue-router';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';

defineProps({
    isReadOnly: { type: Boolean, default: false }
});

const store = usePlanNcStore();
const toast = useToast();
const confirm = useConfirm();
const router = useRouter();
const route = useRoute();
const fileInput = ref(null);

const onCancel = () => {
    router.push('/dev/hub');
};

onMounted(async () => {
  await store.fetchDictionnaires();
  await store.fetchTousLesPlans();
  
  if (route.params.id) {
      await store.chargerPlanNc(route.params.id);
  } else {
      store.resetState();
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

const onDefautInput = (defaut, val) => {
  defaut._libelleDefautBrut = val;
  const found = store.risquesDefauts.find(r => r.libelle.trim().toLowerCase() === val.trim().toLowerCase());
  if (found) {
    defaut.risqueDefautId = found.id;
  } else {
    defaut.risqueDefautId = null;
  }
};

const handleSauvegarder = async () => {
    // 1. Détection de changement
    if (!store.aDesModifications()) {
        toast.add({ severity: 'info', summary: 'Information', detail: 'Aucune modification à enregistrer.', life: 3000 });
        return;
    }

    // 2. Auto-résolution des IDs manquants avant sauvegarde
    store.lignes.forEach(l => {
        if (!l.risqueDefautId && l._libelleDefautBrut) {
            const found = store.risquesDefauts.find(rd => 
                rd.libelle.trim().toLowerCase() === l._libelleDefautBrut.trim().toLowerCase()
            );
            if (found) l.risqueDefautId = found.id;
        }
    });

    // 3. Validation
    // On filtre les lignes totalement vides avant de valider
    const lignesValides = store.lignes.filter(l => l.machineCode || l.risqueDefautId || l._libelleDefautBrut);
    
    if (lignesValides.length === 0) {
        toast.add({ severity: 'warn', summary: 'Attention', detail: 'Le plan ne contient aucune ligne valide.', life: 3000 });
        return;
    }

    // On prévient si des lignes sont totalement vides (pas de machine et pas de défaut)
    const lignesIncompletes = lignesValides.filter(l => !l.machineCode || (!l.risqueDefautId && !l._libelleDefautBrut));
    
    if (lignesIncompletes.length > 0) {
        toast.add({ 
            severity: 'warn', 
            summary: 'Attention', 
            detail: 'Certaines lignes sont incomplètes (Machine ou Désignation manquante).', 
            life: 3000 
        });
        return;
    }

    // 4. Confirmation d'archivage si on crée un nouveau plan et qu'un actif existe
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

const handleExcelImport = async (event) => {
    const file = event.target.files[0];
    if (!file) return;
    try {
        const result = await store.importerDepuisExcel(file);
        if (result.success) {
            toast.add({
                severity: 'success',
                summary: 'Importation terminée',
                detail: `${result.total} ligne(s) récupérée(s) depuis le fichier.`,
                life: 4000
            });
        }
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Échec de l\'import',
            detail: error.response?.data?.message || 'Impossible de lire le fichier Excel.',
            life: 5000
        });
    } finally {
        // Reset input pour permettre une nouvelle sélection du même fichier
        if (fileInput.value) fileInput.value.value = '';
    }
};


</script>

<style scoped>
textarea { resize: none; overflow: hidden; }
</style>
