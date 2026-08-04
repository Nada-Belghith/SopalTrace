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
          <div class="min-w-max flex flex-col w-full">
            <!-- Table 1 : Test de conformité -->
            <VerifMachineTableConformite 
              v-if="store.entete.afficheConformite && !isMachineSansConformite"
              :isReadOnly="props.isReadOnly"
              :showEtalonColumns="false"
              @add-piece="openAddPieceModalFromEvent"
            />

            <!-- Pour MAS22 : Affichage en 2 tables distinctes pour Risques Normaux et Risques Étalon (Table 2 & Table 3) -->
            <template v-if="isMAS22">
              <!-- Table 2 : Section Risques & Défauts (sans fuite) -->
              <VerifMachineTableRisques 
                :isReadOnly="props.isReadOnly"
                :showEtalonColumns="false"
                filterType="normal"
                title="Section Risques & Défauts"
                @add-piece="openAddPieceModalFromEvent"
              />

              <!-- Table 3 : Section Contrôle d'Étanchéité / Fuite Étalon & Pression (avec fuite/pression/dp) -->
              <VerifMachineTableRisques 
                :isReadOnly="props.isReadOnly"
                :showEtalonColumns="true"
                filterType="etalon"
                title="Section Contrôle d'Étanchéité / Fuite Étalon & Pression"
                @add-piece="openAddPieceModalFromEvent"
              />
            </template>

            <template v-else>
              <VerifMachineTableRisques 
                v-if="store.lignesRisques.length > 0"
                :isReadOnly="props.isReadOnly"
                filterType="all"
                @add-piece="openAddPieceModalFromEvent"
              />
            </template>
          </div>
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
      <DocumentSaveManager 
          :plan-id="store.entete.id"
          :statut="store.entete.statut"
          :is-loading="store.isLoading"
          :is-read-only="props.isReadOnly"
          @save-direct="handleSaveDirect"
          @save-correction="handleSaveCorrection"
          @save-new-version="handleSaveNewVersion"
          @cancel="onCancel"
      />

    </template>
  </div>

  <!-- ============================================================ -->
  <!-- MODAL DE CONFIGURATION DES COLONNES -->
  <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
      :baseColumns="vmBaseColumns"
      :showTargetTable="isMAS26"
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
          <div v-if="(store.entete.afficheConformite && !isMachineSansConformite) && (!isMAS26 || previewTarget === 'conformite')" class="border border-slate-300 rounded overflow-hidden">
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
          <div v-if="!isMAS26 || previewTarget === 'risques'" class="border border-slate-300 rounded overflow-hidden">
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

import { useVerifMachineStore } from '@/stores/verifMachineStore';
import { useReferentielStore } from '@/stores/referentielStore';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import DocumentSaveManager from '@/components/Shared/DocumentSaveManager.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useActivePlanConfirmation } from '@/composables/useActivePlanConfirmation';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import AutoComplete from 'primevue/autocomplete';
import { parseDesignation } from '@/utils/designationParser';
import { MachineStrategyFactory } from './strategies/MachineStrategyFactory';
import VerifMachineHeader from './partials/VerifMachineHeader.vue';
import VerifMachineTableConformite from './partials/VerifMachineTableConformite.vue';
import VerifMachineTableRisques from './partials/VerifMachineTableRisques.vue';
import AddPieceModal from './partials/AddPieceModal.vue';

const store = useVerifMachineStore();
const refStore = useReferentielStore();
const toast = useToast();
const { confirmArchivagePlanActif } = useActivePlanConfirmation();

const props = defineProps({
  isReadOnly: { type: Boolean, default: false }
});

const emit = defineEmits(['saved']);

const router = useRouter();

const onNomBlur = () => {};

// --- GESTION FAMILLES POUR COLUMN CONFIGURATOR ---
const selectedFamilleObj = ref(null);
const filteredFamilles = ref([]);

const allPossibleFamilles = computed(() => {
  const fCorps = (refStore.famillesCorps || []).map(f => ({ id: f.id, label: f.code + ' - ' + f.libelle, type: 'famille' }));
  const pRef = (refStore.piecesReference || []).map(p => ({ id: p.id, label: p.code, type: 'piece' }));
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

onMounted(async () => {
  if (!refStore.isDicosVerifMachineLoaded) {
    await refStore.fetchDictionnaires('verif-machine');
  }
  if (!props.isReadOnly && !store.entete.id) {
    await refStore.fetchFormulairesReferences('VERIF_MACHINE');
  }
});

watch(refFormulaireSelected, async (newRefId) => {
  if (!newRefId) {
    store.entete.configurationColonnes = [];
    return;
  }
  const refObj = (refStore.formulairesReferencesByRole['VERIF_MACHINE'] || []).find(r => r.id === newRefId);
  console.log('[DEBUG] refFormulaireSelected changed to:', newRefId, 'Found refObj:', refObj);
  if (!refObj) return;

  store.entete.refFormulaireCodeReference = refObj.codeReference;
  const designation = refObj.designation || '';
  const parsed = parseDesignation(designation, [], refStore.machines || []);

  if (parsed.machineCode) {
    selectedMachineCode.value = parsed.machineCode;
    await store.initialiserPlan(parsed.machineCode);
    store.entete.nom = designation;
  }

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

const onMachineChange = async (machineCode) => {
  if (machineCode) {
    selectedMachineCode.value = machineCode;
    await store.initialiserPlan(machineCode);
  }
};

// Synchroniser le code machine local avec le store
watch(() => store.entete.machineCode, (newVal) => {
  selectedMachineCode.value = newVal || '';
}, { immediate: true });

const isMachineSansConformite = computed(() => {
  if (!store.entete.machineCode) return false;
  const code = store.entete.machineCode.toUpperCase().replace('-', '').replace(' ', '').trim();
  return code.includes('BEE46') || code.includes('BEE47') || 
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

import { useDocumentSaveManager } from '@/composables/useDocumentSaveManager';
import { useExcelStructureImporter } from '@/composables/useExcelStructureImporter';

const { handleExcelImport } = useExcelStructureImporter(store, toast);

const { handleSaveDirect, handleSaveCorrection, handleSaveNewVersion } = useDocumentSaveManager({
  callbacks: {
    onSaveDirect: async () => {
        const result = await store.sauvegarderPlanVerif();
        if (result.error) throw new Error(result.error);
        emit('saved', result);
        return result;
    },
    onSaveCorrection: async () => {
        const result = await store.sauvegarderPlanVerif();
        if (result.error) throw new Error(result.error);
        emit('saved', result);
        return result;
    },
    onSaveNewVersion: async (motif) => {
        const result = await store.createNewVersion(motif);
        if (result.success) emit('saved', result);
        else throw new Error(result.message);
        return result;
    },
    preSaveHook: async () => {
        if (!store.entete.id) {
            await store.fetchTousLesPlans();
            const planActif = (store.plansExistants || []).find(p => p.statut === 'ACTIF' && p.machineCode === selectedMachineCode.value);
            
            if (planActif) {
                const isConfirmed = await confirmArchivagePlanActif({
                typeDocument: 'plan de vérification',
                identifiant: `la machine ${selectedMachineCode.value}`,
                version: planActif.version
                });
                
                if (!isConfirmed) return false;
                await new Promise(resolve => setTimeout(resolve, 200));
            }
        }
        return true;
    }
  },
  toast,
  router: null,
  returnUrl: null
});

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    if (!props.isReadOnly) {
      await store.fetchTousLesPlans();
    }
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

