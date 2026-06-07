<template>
  <div class="space-y-6 max-w-[1400px] mx-auto pb-20">
    <Toast />
    <ConfirmDialog />

    <VerifMachineHeader 
      :isReadOnly="props.isReadOnly"
      @machine-changed="onMachineChange"
      @import-excel="handleExcelImport"
      @configure-columns="showColumnModal = true"
      @nom-blur="onNomBlur"
    />

    <template v-if="store.planInitialise">
      <section class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div class="overflow-x-auto w-full">
          <VerifMachineTableConformite 
            v-if="store.entete.afficheConformite && !isMachineSansConformite"
            :isReadOnly="props.isReadOnly"
            @add-piece="openAddPieceModalFromEvent"
          />

          <VerifMachineTableRisques 
            v-if="store.lignesRisques.length > 0"
            :isReadOnly="props.isReadOnly"
            @add-piece="openAddPieceModalFromEvent"
          />
        </div>
      </section>

      <!-- ============================================================ -->
      <!-- REMARQUES & LÉGENDE                                          -->
      <!-- ============================================================ -->
      <RemarquesLegendeBox
        v-model:remarques="store.entete.remarques"
        v-model:legendeMoyens="store.entete.legendeMoyens"
        :is-read-only="props.isReadOnly"
      />

      <!-- ============================================================ -->
      <!-- BARRE D'ACTIONS                                              -->
      <!-- ============================================================ -->
      <div v-if="!props.isReadOnly" class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-6 rounded-b-xl">
        <EditorActions 
          :label="store.entete.id ? 'Enregistrer les Modifications' : 'Enregistrer le Plan'"
          loading-label="Enregistrement..."
          :icon="store.entete.id ? 'pi pi-save' : 'pi pi-check'"
          variant="primary"
          :is-loading="store.isLoading"
          @submit="onSauvegarder"
          @cancel="onCancel"
        />
      </div>

    </template>
  </div>

  <!-- ============================================================ -->
  <!-- MODAL DE CONFIGURATION DES COLONNES -->
  <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
      :baseColumns="vmBaseColumns"
      :showTargetTable="isMAS22 || isMAS26"
  >
      <!-- GESTION DES FAMILLES DE CORPS -->
      <template #extra-configuration>
        <div v-if="store.entete.afficheFamilles" class="mt-6 border-t border-slate-700/50 pt-6">
          <h3 class="text-xs font-bold text-slate-300 uppercase tracking-wider mb-4 flex items-center gap-2">
            <i class="ri-node-tree text-emerald-500"></i> Familles de Corps / Réf.
          </h3>
          <div class="flex flex-col gap-3">
            <div class="relative">
              <AutoComplete 
                  v-model="selectedFamilleObj"
                  :suggestions="filteredFamilles"
                  @complete="searchFamille"
                  @item-select="onFamilleSelected"
                  optionLabel="label"
                  :dropdown="true"
                  placeholder="Rechercher pour ajouter une famille..."
                  class="w-full"
                  appendTo="body"
                  :inputStyle="{ width: '100%', padding: '0.6rem', fontSize: '0.875rem' }"
              />
            </div>
            
            <div class="bg-[#1e293b]/50 border border-slate-700/50 rounded-xl p-3 flex flex-wrap gap-2 max-h-[120px] overflow-y-auto">
              <div v-if="store.familles.length === 0" class="text-xs text-slate-500 italic w-full text-center py-2">
                Aucune famille sélectionnée.
              </div>
              <div v-for="fam in store.familles" :key="fam.id" class="flex items-center gap-2 bg-emerald-900/40 border border-emerald-700/50 text-emerald-100 px-3 py-1.5 rounded-lg text-xs font-semibold shadow-sm">
                <span>{{ fam.libelle }}</span>
                <button @click="store.supprimerFamille(fam.id)" class="text-emerald-400 hover:text-emerald-200 transition-colors ml-1" title="Retirer">
                  <i class="ri-close-line text-base leading-none"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- APERÇU COMPLET VERIF MACHINE -->
      <template #preview="{ previewColumns, previewTarget }">
        <div class="flex flex-col gap-6">
          <!-- Aperçu Conformité -->
          <div v-if="(store.entete.afficheConformite && !isMachineSansConformite) && ((!isMAS22 && !isMAS26) || previewTarget === 'conformite')" class="border border-slate-300 rounded overflow-hidden">
            <div class="bg-[#0f172a] text-slate-200 border-l-4 border-emerald-500 text-[10px] font-bold uppercase p-2">Section Conformité</div>
            <div class="overflow-x-auto">
              <table class="w-full text-left text-xs whitespace-nowrap">
                <thead class="bg-slate-800 text-white font-black text-[10px] uppercase tracking-wider text-center">
                  <tr>
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <th v-if="cCol.key === 'risque'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">Test de conformité</th>
                      <th v-else-if="cCol.key === 'methode'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">{{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}</th>
                      <th v-else-if="cCol.key === 'periodicite'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">Périodicité</th>
                      <th v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ (isArchitectureA || isBEEMachine || isMAS19 || isSER05) ? 'Moyen de contrôle' : 'Moyen de détection' }}
                      </th>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                          <th :colspan="store.familles.length" class="p-2 border-b border-r border-slate-600 bg-slate-700">
                            {{ (isBEEMachine || isMAS19) ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isSER05) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                          </th>
                        </template>
                        <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">
                          {{ (isBEEMachine || isMAS19) ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isSER05) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                        </th>
                      </template>
                      <th v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ isBEEMachine ? 'Numéro du fuite étalon' : 'Fuite Étalon' }}
                      </th>
                      <th v-else-if="cCol.key === 'pression_entree'" v-show="!hidePressionAndDp" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">Pression d'entrée affichée (en bar)</th>
                      <th v-else-if="cCol.key === 'dp_affichee'" v-show="!isMAS19 && !hidePressionAndDp" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">{{ store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)' }}</th>
                      <th v-else-if="cCol.key === 'resultat'" :colspan="hasSubHeaders ? 2 : 1" :rowspan="1" class="p-2 border-r border-slate-600 w-20">Résultats</th>
                      <th v-else-if="cCol.key === 'observation'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">{{ isSER05 ? 'Action en cas de non-conformité' : 'Observation en cas de non-conformité' }}</th>
                      <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24 text-amber-400 bg-slate-700/50">
                        {{ cCol.label }}
                      </th>
                    </template>
                  </tr>
                  <tr v-if="hasSubHeaders">
                    <template v-if="hasFamilleHeaders">
                      <th v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-20">{{ fam.libelle }}</th>
                    </template>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-emerald-400">C</th>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-rose-400">NC</th>
                  </tr>
                </thead>
                <tbody class="text-slate-600 bg-white border border-t-0 border-slate-200">
                  <tr class="hover:bg-slate-50 text-center">
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <td v-if="cCol.key === 'risque'" class="p-2 border-r border-slate-200 text-left font-bold">Aspect visuel</td>
                      <td v-else-if="cCol.key === 'methode'" class="p-2 border-r border-slate-200">Visuel</td>
                      <td v-else-if="cCol.key === 'periodicite'" class="p-2 border-r border-slate-200">1 / équipe</td>
                      <td v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" class="p-2 border-r border-slate-200">M.D.</td>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                           <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-200">PRC...</td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200">PRC...</td>
                      </template>
                      <td v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-200 text-blue-600 font-bold">FE...</td>
                      <td v-else-if="cCol.key === 'pression_entree'" v-show="!hidePressionAndDp" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <td v-else-if="cCol.key === 'dp_affichee'" v-show="!isMAS19 && !hidePressionAndDp" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <template v-else-if="cCol.key === 'resultat'">
                        <template v-if="hasSubHeaders">
                           <td class="p-2 border-r border-slate-200"></td>
                           <td class="p-2 border-r border-slate-200"></td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200 font-bold">C / NC</td>
                      </template>
                      <td v-else-if="cCol.key === 'observation'" class="p-2 border-r border-slate-200 text-slate-400 italic">Obs...</td>
                      <td v-else class="p-2 border-r border-slate-200 bg-amber-50">
                        <span class="text-amber-600 bg-amber-100 px-1 py-0.5 rounded border border-amber-200 text-[10px]">Auto</span>
                      </td>
                    </template>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Aperçu Risques -->
          <div v-if="(!isMAS22 && !isMAS26) || previewTarget === 'risques'" class="border border-slate-300 rounded overflow-hidden">
            <div class="bg-[#0f172a] text-slate-200 border-l-4 border-rose-500 text-[10px] font-bold uppercase p-2">Section Risques & Défauts</div>
            <div class="overflow-x-auto">
              <table class="w-full text-left text-xs whitespace-nowrap">
                <!-- Même Header -->
                <thead class="bg-slate-800 text-white font-black text-[10px] uppercase tracking-wider text-center">
                  <tr>
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <th v-if="cCol.key === 'risque'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">Risque/ Défaut</th>
                      <th v-else-if="cCol.key === 'methode'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">{{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}</th>
                      <th v-else-if="cCol.key === 'periodicite'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">Périodicité</th>
                      <th v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ (isArchitectureA || isBEEMachine || isMAS19 || isSER05) ? 'Moyen de contrôle' : 'Moyen de détection' }}
                      </th>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                          <th :colspan="store.familles.length" class="p-2 border-b border-r border-slate-600 bg-slate-700">
                            {{ (isBEEMachine || isMAS19) ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isSER05) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                          </th>
                        </template>
                        <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">
                          {{ (isBEEMachine || isMAS19) ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isSER05) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                        </th>
                      </template>
                      <th v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ isBEEMachine ? 'Numéro du fuite étalon' : 'Fuite Étalon' }}
                      </th>
                      <th v-else-if="cCol.key === 'pression_entree'" v-show="!hidePressionAndDp" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">Pression d'entrée affichée (en bar)</th>
                      <th v-else-if="cCol.key === 'dp_affichee'" v-show="!isMAS19 && !hidePressionAndDp" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">{{ store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)' }}</th>
                      <th v-else-if="cCol.key === 'resultat'" :colspan="hasSubHeaders ? 2 : 1" :rowspan="1" class="p-2 border-r border-slate-600 w-20">Résultats</th>
                      <th v-else-if="cCol.key === 'observation'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">{{ isSER05 ? 'Action en cas de non-conformité' : 'Observation en cas de non-conformité' }}</th>
                      <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24 text-amber-400 bg-slate-700/50">
                        {{ cCol.label }}
                      </th>
                    </template>
                  </tr>
                  <tr v-if="hasSubHeaders">
                    <template v-if="hasFamilleHeaders">
                      <th v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-20">{{ fam.libelle }}</th>
                    </template>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-emerald-400">C</th>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-rose-400">NC</th>
                  </tr>
                </thead>
                <tbody class="text-slate-600 bg-white border border-t-0 border-slate-200">
                  <tr class="hover:bg-slate-50 text-center">
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <td v-if="cCol.key === 'risque'" class="p-2 border-r border-slate-200 text-left font-bold text-red-700">Fissure</td>
                      <td v-else-if="cCol.key === 'methode'" class="p-2 border-r border-slate-200">Visuel</td>
                      <td v-else-if="cCol.key === 'periodicite'" class="p-2 border-r border-slate-200">1 / équipe</td>
                      <td v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" class="p-2 border-r border-slate-200">M.D.</td>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                           <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-200">PRC...</td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200">PRC...</td>
                      </template>
                      <td v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-200 text-blue-600 font-bold">FE...</td>
                      <td v-else-if="cCol.key === 'pression_entree'" v-show="!hidePressionAndDp" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <td v-else-if="cCol.key === 'dp_affichee'" v-show="!isMAS19 && !hidePressionAndDp" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <template v-else-if="cCol.key === 'resultat'">
                        <template v-if="hasSubHeaders">
                           <td class="p-2 border-r border-slate-200"></td>
                           <td class="p-2 border-r border-slate-200"></td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200 font-bold">C / NC</td>
                      </template>
                      <td v-else-if="cCol.key === 'observation'" class="p-2 border-r border-slate-200 text-slate-400 italic">Obs...</td>
                      <td v-else class="p-2 border-r border-slate-200 bg-amber-50">
                        <span class="text-amber-600 bg-amber-100 px-1 py-0.5 rounded border border-amber-200 text-[10px]">Auto</span>
                      </td>
                    </template>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </template>
  </ColumnConfigurator>

  <!-- MODAL INLINE : CRÉATION PIÈCE RÉFÉRENCE / ÉTALON FUITE     -->
  <!-- ============================================================ -->
  <AddPieceModal 
    v-model:visible="showAddPieceModal"
    :addPieceType="addPieceType"
    :addPieceContext="addPieceContext"
  />
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useVerifMachineStore } from '@/stores/verifMachineStore';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import AutoComplete from 'primevue/autocomplete';
import { parseDesignation } from '@/utils/designationParser';
import { MachineStrategyFactory } from './strategies/MachineStrategyFactory';
import VerifMachineHeader from './partials/VerifMachineHeader.vue';
import VerifMachineTableConformite from './partials/VerifMachineTableConformite.vue';
import VerifMachineTableRisques from './partials/VerifMachineTableRisques.vue';
import AddPieceModal from './partials/AddPieceModal.vue';

const props = defineProps({
  isReadOnly: { type: Boolean, default: false }
});

const store = useVerifMachineStore();
const confirm = useConfirm();
const toast = useToast();
const router = useRouter();

const onNomBlur = () => {};

// --- GESTION FAMILLES POUR COLUMN CONFIGURATOR ---
const selectedFamilleObj = ref(null);
const filteredFamilles = ref([]);

const allPossibleFamilles = computed(() => {
  const fCorps = store.famillesCorps.map(f => ({ id: f.id, label: f.code + ' - ' + f.libelle, type: 'famille' }));
  const pRef = store.piecesReference.map(p => ({ id: p.id, label: p.code, type: 'piece' }));
  return [...fCorps, ...pRef];
});

const searchFamille = (event) => {
  const q = event.query.toLowerCase();
  filteredFamilles.value = allPossibleFamilles.value.filter(f => 
    f.label.toLowerCase().includes(q) && 
    !store.familles.find(sf => sf.refFamilleCorpsId === f.id)
  );
};

const onFamilleSelected = (event) => {
  if (event.value && event.value.id) {
    store.ajouterFamille(event.value.id);
  }
  selectedFamilleObj.value = null;
};

const showColumnModal = ref(false);
const refFormulaireSelected = ref('');

onMounted(() => {
  if (!props.isReadOnly && !store.entete.id) {
    store.fetchFormulairesReferences('VERIF_MACHINE');
  }
});

watch(refFormulaireSelected, async (newRefId) => {
  if (!newRefId) {
    store.entete.configurationColonnes = [];
    return;
  }
  const refObj = store.formulairesReferences.find(r => r.id === newRefId);
  console.log('[DEBUG] refFormulaireSelected changed to:', newRefId, 'Found refObj:', refObj);
  if (!refObj) return;

  const designation = refObj.designation || '';
  const parsed = parseDesignation(designation, [], store.machines || []);

  if (parsed.machineCode) {
    selectedMachineCode.value = parsed.machineCode;
    // On force l'initialisation de la machine de manière asynchrone pour ne pas écraser les colonnes ensuite
    await store.initialiserPlan(parsed.machineCode);
    // On met aussi à jour le nom
    store.entete.nom = designation;
  }

  // Appliquer la configuration des colonnes du formulaire sélectionné
  if (refObj.configurationStructureJson) {
    try {
      store.entete.configurationColonnes = typeof refObj.configurationStructureJson === 'string' 
        ? JSON.parse(refObj.configurationStructureJson) 
        : refObj.configurationStructureJson;
    } catch (e) {
      console.error("Erreur parsing configuration colonnes:", e);
      store.entete.configurationColonnes = [];
    }
  } else {
    store.entete.configurationColonnes = [];
  }
});

const onCancel = () => {
  router.push('/dev/hub');
};

const selectedMachineCode = ref('');

// Synchroniser le code machine local avec le store
watch(() => store.entete.machineCode, (newVal) => {
  selectedMachineCode.value = newVal || '';
}, { immediate: true });

const isMachineSansConformite = computed(() => {
  if (!store.entete.machineCode) return false;
  const code = store.entete.machineCode.toUpperCase().replace('-', '').replace(' ', '').trim();
  return code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') || 
         code.includes('MAS19') || code.includes('MAS20') || code.startsWith('SER');
});

watch(isMachineSansConformite, (newVal) => {
  if (newVal) {
    store.entete.afficheConformite = false;
    store.lignesConformite = [];
  }
}, { immediate: true });

const machineStrategy = computed(() => MachineStrategyFactory.getStrategy(store.entete.machineCode, store));

const isArchitectureA = computed(() => machineStrategy.value.isArchitectureA);
const isBEEMachine = computed(() => machineStrategy.value.role === 'BEE');
const isMAS26 = computed(() => machineStrategy.value.isMAS26);
const isMAS19 = computed(() => machineStrategy.value.isMAS19);
const isMAS22 = computed(() => machineStrategy.value.isMAS22);
const isSER05 = computed(() => machineStrategy.value.isSER05);
const hidePressionAndDp = computed(() => machineStrategy.value.hidePressionAndDp);


const vmBaseColumns = computed(() => {
  const cols = [
    { 
      key: 'risque', 
      label: 'RISQUE/ DÉFAUT',
      labelConformite: 'TEST DE CONFORMITÉ',
      labelRisques: 'RISQUE/ DÉFAUT'
    },
    { key: 'methode', label: isBEEMachine.value ? 'MÉTHODE DE CONTRÔLE' : 'MOYEN/ MÉTHODE DE CONTRÔLE' },
    { key: 'periodicite', label: 'PÉRIODICITÉ' }
  ];
  if (store.entete.afficheMoyenDetectionRisques) {
    cols.push({ 
      key: 'moyen_detection', 
      label: (isArchitectureA.value || isBEEMachine.value || isMAS19.value || isSER05.value) ? 'MOYEN DE CONTRÔLE' : 'MOYEN DE DÉTECTION',
      hiddenInRisques: isMAS26.value
    });
  }
  cols.push({ key: 'piece_reference', label: (isBEEMachine.value || isMAS19.value) ? 'NUMÉRO DU MOYEN DE CONTRÔLE' : ((isArchitectureA.value || isSER05.value) ? 'N° MOYEN DE CONTRÔLE' : 'NUMÉRO DE LA PIÈCE RÉFÉRENCE') });
  if (store.entete.afficheFuiteEtalon || isBEEMachine.value) {
    cols.push({ key: 'fuite_etalon', label: isBEEMachine.value ? 'NUMÉRO DU FUITE ÉTALON' : 'FUITE ÉTALON' });
  }
  cols.push({ key: 'pression_entree', label: "PRESSION D'ENTRÉE AFFICHÉE (EN BAR)" });
  if (!isMAS19.value) {
    cols.push({ key: 'dp_affichee', label: store.entete.machineCode?.includes('BEE47') ? 'FUITE AFFICHÉE (EN PA)' : 'ΔP AFFICHÉE (EN PA)' });
  }
  cols.push({ key: 'resultat', label: 'RÉSULTAT (C/NC)' });
  cols.push({ key: 'observation', label: isSER05.value ? 'ACTION EN CAS DE NON-CONFORMITÉ' : 'OBSERVATION EN CAS DE NON-CONFORMITÉ' });
  return cols;
});



// Computed : est-ce qu'on affiche les en-têtes de familles ?
const hasFamilleHeaders = computed(() => store.entete.afficheFamilles && store.familles.length > 0);
const hasSubHeaders = computed(() => machineStrategy.value.hasSubHeaders);



const showAddPieceModal = ref(false);
const addPieceType = ref('PRC');
const addPieceContext = ref(null);

const openAddPieceModal = (mode, row, familleCorpsId, role) => {
  addPieceType.value = role;
  addPieceContext.value = { row, familleCorpsId, role };
  showAddPieceModal.value = true;
};

const openAddPieceModalFromEvent = (eventData) => {
  openAddPieceModal(eventData.type, eventData.row, eventData.familleCorpsId, eventData.role);
};

const fileInput = ref(null);
const handleExcelImport = async (event) => {
  const file = event.target.files[0];
  if (!file) return;

  try {
    const result = await store.importerDepuisExcel(file);
    if (result.success) {
      toast.add({ severity: 'success', summary: 'Import Réussi', detail: 'La structure du plan a été importée avec succès.', life: 3000 });
      selectedMachineCode.value = store.entete.machineCode;
    }
  } catch (error) {
    console.error("Erreur import Excel:", error);
    toast.add({ severity: 'error', summary: 'Échec de l\'import', detail: error.response?.data?.message || 'Une erreur est survenue lors de la lecture du fichier.', life: 5000 });
  } finally {
    if (fileInput.value) fileInput.value.value = '';
  }
};

// --- Événements ---
const onMachineChange = async (newMachineCode) => {
  if (newMachineCode) {
    const expectedCode = `FE-VM-${newMachineCode}`;
    const matchingRef = store.formulairesReferences.find(r => r.codeReference === expectedCode);
    if (matchingRef && refFormulaireSelected.value !== matchingRef.id) {
      // Auto-sélectionne le formulaire actif (ce qui déclenchera le watch et initialisera le plan avec les colonnes)
      refFormulaireSelected.value = matchingRef.id;
    } else {
      await store.initialiserPlan(newMachineCode);
    }
  } else {
    store.resetPlan();
    refFormulaireSelected.value = '';
  }
};



const emit = defineEmits(['saved']);
const onSauvegarder = async () => {
  if (store.isLoading) return;
  
  toast.removeAllGroups();
  // --- VALIDATION ---
  if (!store.entete.machineCode) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Veuillez sélectionner une machine.', life: 3000 });
    return;
  }
  if (!store.entete.nom || !store.entete.nom.trim()) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Le nom du plan est obligatoire.', life: 3000 });
    return;
  }

  const validateLignes = (lignes, sectionName) => {
    for (let i = 0; i < lignes.length; i++) {
      const l = lignes[i];
      const prefix = `${sectionName} (Ligne ${i + 1})`;
      
      if (!l.libelleRisque || !l.libelleRisque.trim()) {
        toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : Le libellé ${sectionName === 'Conformité' ? 'Test' : 'Risque'} est obligatoire.`, life: 4000 });
        return false;
      }
      if (!l.libelleMethode || !l.libelleMethode.trim()) {
        toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : Le Moyen/Méthode est obligatoire.`, life: 4000 });
        return false;
      }

      for (const group of l.groups) {
        if (!group.periodiciteId) {
          toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : La périodicité est obligatoire.`, life: 4000 });
          return false;
        }
        for (const row of group.rows) {
          const skipMoyenValidation = sectionName === 'Risque' && !isMachineSansConformite.value;
          if (store.entete.afficheMoyenDetectionRisques && !skipMoyenValidation && !row.refMoyenDetectionId) {
            toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : Le moyen de détection est obligatoire.`, life: 4000 });
            return false;
          }
        }
      }
    }
    return true;
  };

  if (store.entete.afficheConformite && !isMachineSansConformite.value) {
    if (!validateLignes(store.lignesConformite, 'Conformité')) return;
  }
  if (!validateLignes(store.lignesRisques, 'Risque')) return;

  store.isLoading = true;
  try {
    if (!store.entete.id) {
      await store.fetchTousLesPlans();
      const planActif = (store.plansExistants || []).find(p => p.statut === 'ACTIF' && p.machineCode === selectedMachineCode.value);
      
      if (planActif) {
        const isConfirmed = await new Promise((resolve) => {
          confirm.require({
            message: `Un plan actif existe déjà pour la machine ${selectedMachineCode.value} (Version ${planActif.version}).\n\nVoulez-vous archiver le plan actif existant et activer ce nouveau plan (Version ${planActif.version + 1}) ?`,
            header: 'Plan Actif Existant',
            icon: 'ri-error-warning-line text-amber-500',
            acceptLabel: 'Oui, archiver',
            rejectLabel: 'Annuler',
            acceptClass: 'p-button-danger',
            accept: () => resolve(true),
            reject: () => resolve(false),
            onHide: () => resolve(false)
          });
        });
        
        if (!isConfirmed) return;
      }
    }

    const result = await store.sauvegarderPlanVerif();
    if (result.error) {
       toast.add({ severity: 'warn', summary: 'Attention', detail: result.error, life: 3000 });
       return;
    }
    emit('saved', result);
  } catch (err) {
    console.error('Erreur sauvegarde:', err);
    toast.removeAllGroups();
    
    const backendData = err?.response?.data;
    if (backendData?.details && Array.isArray(backendData.details) && backendData.details.length > 0) {
      backendData.details.slice(0, 2).forEach(detail => {
        toast.add({ severity: 'error', summary: 'Validation Serveur', detail, life: 5000 });
      });
      if (backendData.details.length > 2) {
        toast.add({ severity: 'warn', summary: "Plus d'erreurs", detail: `Et ${backendData.details.length - 2} autres problèmes détectés...`, life: 5000 });
      }
    } else {
      const msg = backendData?.message || 'Une erreur est survenue lors de la sauvegarde.';
      toast.add({ severity: 'error', summary: 'Erreur Serveur', detail: msg, life: 5000 });
    }
  } finally {
    store.isLoading = false;
  }
};

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    await store.fetchTousLesPlans();
  } catch {
    // Fallback data
  }
});
</script>

<style scoped>
textarea { resize: none; overflow: hidden; }
textarea:disabled { color: #334155; }
select:disabled { color: #334155; opacity: 1; -webkit-appearance: none; -moz-appearance: none; appearance: none; }
</style>

