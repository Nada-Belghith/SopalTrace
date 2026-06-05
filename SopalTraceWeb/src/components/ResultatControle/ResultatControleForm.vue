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
              <!-- CHOIX RÉFÉRENCE FORMULAIRE -->
              <div v-if="!isReadOnly && (!store.entete.id || store.entete.isModeleTemplate)" class="col-span-full mb-4 bg-emerald-50/50 border border-emerald-200 p-4 rounded-xl flex flex-col md:flex-row items-start md:items-center gap-4">
                <label class="block text-[11px] font-black text-emerald-800 uppercase tracking-widest shrink-0">
                  <i class="pi pi-file-import mr-1 text-emerald-600"></i> Réf. Formulaire *
                </label>
                <select 
                  v-model="refFormulaireSelected"
                  :disabled="store.entete.isModeleTemplate"
                  class="w-full md:w-1/3 rounded px-3 py-2 text-sm font-semibold outline-none focus:border-emerald-500 transition-shadow bg-white border border-slate-300 text-slate-800 cursor-pointer shadow-sm disabled:opacity-70">
                  <option value="">-- Choisir un formulaire générique --</option>
                  <option v-for="ref in store.formulairesReferences" :key="ref.id" :value="ref.id">
                    {{ ref.codeReference }} - {{ ref.designation }}
                  </option>
                </select>
                <p class="text-xs text-emerald-600/80 font-medium italic">
                  La sélection du formulaire remplira automatiquement le poste.
                </p>
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-500 mb-2 uppercase">Poste de travail concerné</label>
                <select v-model="store.entete.posteCode" @change="onSelectionChange" :disabled="isReadOnly || store.entete.id" class="w-full border border-slate-200 rounded-lg py-2.5 px-4 text-sm focus:ring-4 focus:ring-emerald-500/10 focus:border-emerald-500 outline-none transition-all font-medium bg-slate-50">
                    <option value="">-- Choisir un poste --</option>
                    <option v-for="p in store.postes" :key="p.code" :value="p.code">
                        Poste {{ p.code }} - {{ p.libelle }}
                    </option>
                </select>
              </div>

              <div>
                <label class="block text-[10px] font-bold text-slate-700 uppercase mb-1.5">Version de départ</label>
                <input type="number" v-model.number="store.entete.versionInitiale" :disabled="isReadOnly || !!store.entete.id" min="0" max="99"
                  class="w-full border border-slate-200 rounded-lg py-2.5 px-4 text-sm focus:ring-4 focus:ring-emerald-500/10 focus:border-emerald-500 outline-none transition-all font-medium bg-white shadow-sm disabled:opacity-70" />
                  <p class="text-[10px] text-slate-400 mt-1 italic">Définit la version initiale de ce formulaire.</p>
              </div>
              
              <div v-if="!isReadOnly" class="col-span-full flex justify-end gap-3 mt-4">
                  <button @click="$refs.fileInput.click()" 
                          class="h-[38px] px-4 flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-bold transition-colors shadow-sm w-full md:w-auto justify-center">
                    <i class="pi pi-file-excel"></i>
                    Importer la structure Excel
                  </button>
                  <input type="file" ref="fileInput" @change="handleExcelImport" class="hidden" accept=".xlsx, .xls" />
                  
                  <button v-if="store.entete.posteCode"
                          @click="showColumnModal = true"
                        class="h-[38px] px-4 flex items-center gap-2 bg-slate-900 hover:bg-slate-800 text-white rounded-lg text-xs font-bold transition-colors shadow-sm w-full md:w-auto justify-center">
                  <i class="pi pi-sliders-h"></i>
                  Configurer Colonnes
                </button>
              </div>
            </div>
          </div>
        </div>
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
                        <thead class="bg-slate-100 text-slate-700 text-[11px] font-bold border-b border-slate-300">
                            <tr v-if="hasGroups">
                                <th class="p-2 border-r border-slate-300 w-10 text-center" rowspan="2">N°</th>
                                <th class="p-2 border-r border-b border-slate-300 text-center align-middle" colspan="2">
                                    <div class="text-[12px] font-black text-slate-800 tracking-wide uppercase">Test de Non-conformité</div>
                                </th>
                                <template v-for="(g, idx) in headerGroups" :key="'g'+idx">
                                    <th v-if="g.name" :colspan="g.colspan" class="border-r border-b border-slate-300 text-center align-top relative p-2">
                                        <div class="mb-2 text-[13px] font-black text-slate-800">{{ g.name }}</div>
                                        <div v-if="g.name === 'Equipe 1' || g.name === 'Equipe 2'" class="flex items-center justify-center gap-4 mt-2 px-2 pb-1 text-[10px] normal-case font-semibold">
                                            <div class="flex items-center gap-1.5 whitespace-nowrap">
                                                <label class="text-slate-500">Nom et prénom :</label>
                                                <input type="text" v-model="store.entete[g.name === 'Equipe 1' ? 'equipe1Nom' : 'equipe2Nom']" :disabled="isReadOnly"
                                                    class="border-b border-slate-300 bg-transparent px-1 py-0.5 outline-none focus:border-emerald-500 text-emerald-700 font-bold w-32" />
                                            </div>
                                            <div class="flex items-center gap-1.5 whitespace-nowrap">
                                                <label class="text-slate-500">Matricule :</label>
                                                <input type="text" v-model="store.entete[g.name === 'Equipe 1' ? 'equipe1Matricule' : 'equipe2Matricule']" :disabled="isReadOnly"
                                                    class="border-b border-slate-300 bg-transparent px-1 py-0.5 outline-none focus:border-emerald-500 text-emerald-700 font-bold w-20" />
                                            </div>
                                        </div>
                                    </th>
                                    <template v-else>
                                        <th v-for="(col, i) in g.columns" :key="col.id" rowspan="2" class="p-2 border-r border-slate-300 text-center align-middle min-w-[75px] max-w-[90px] leading-snug">
                                            {{ col.header }}
                                        </th>
                                    </template>
                                </template>
                                <th v-if="!isReadOnly" class="p-2 w-10 text-center" rowspan="2"></th>
                            </tr>
                            <tr v-if="hasGroups">
                                <th class="p-2 border-r border-slate-300 min-w-[120px] text-center bg-slate-50/50">Machine<br/>Banc d'essai</th>
                                <th class="p-2 border-r border-slate-300 min-w-[150px] text-center bg-slate-50/50">Désignation du défaut</th>
                                <template v-for="col in allColumns" :key="'sub'+col.id">
                                    <th v-if="col.group" class="p-2 border-r border-slate-300 text-center text-[10px]">
                                        {{ col.header }}
                                    </th>
                                </template>
                            </tr>
                            <tr v-else>
                                <th class="p-2 border-r border-slate-300 w-10 text-center">N°</th>
                                <th class="p-2 border-r border-slate-300 min-w-[120px]">Machine/ Banc d'essai</th>
                                <th class="p-2 border-r border-slate-300 min-w-[150px]">Désignation du défaut</th>
                                <th v-for="col in allColumns" :key="col.id" class="p-2 border-r border-slate-300 text-center min-w-[70px]">
                                    {{ col.header }}
                                </th>
                                <th v-if="!isReadOnly" class="p-2 w-10 text-center"></th>
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
                                  <div v-else>
                                      <input type="text" v-model="defaut.machineCode"
                                             class="w-full border border-slate-200 rounded-lg px-3 py-2 text-xs focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-500 outline-none"
                                             placeholder="Saisir la machine..." />
                                  </div>
                              </td>

                              <!-- Sélection Défaut -->
                              <td class="p-2 border-r align-middle">
                                   <div v-if="isReadOnly" class="px-3 py-2 text-xs font-semibold text-slate-800 uppercase">
                                       {{ store.risquesDefauts.find(r => r.id === defaut.risqueDefautId)?.libelle || defaut._libelleDefautBrut || 'Aucun défaut sélectionné' }}
                                   </div>
                                  <input v-else 
                                          :value="store.risquesDefauts.find(r => r.id === defaut.risqueDefautId)?.libelle || defaut._libelleDefautBrut || ''"
                                          @input="(e) => onDefautInput(defaut, e.target.value)"
                                          class="w-full text-xs font-semibold text-slate-800 border border-slate-200 focus:border-emerald-500 rounded-lg py-2.5 px-3 outline-none bg-white shadow-sm transition-all uppercase focus:ring-4 focus:ring-emerald-500/10"
                                          placeholder="Saisir la désignation du défaut...">
                              </td>

                                <!-- Colonnes dynamiques fusionnées -->
                                <td v-if="index === 0" :colspan="allColumns.length" :rowspan="store.lignes.length" class="p-2 border-r align-middle text-center bg-slate-50/50">
                                    <span class="text-xs font-bold text-slate-400 italic tracking-wider uppercase">À remplir lors de la production</span>
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

      <!-- MODAL DE CONFIGURATION DES COLONNES -->
      <ColumnConfigurator 
        v-model:visible="showColumnModal"
        v-model="store.entete.configurationColonnes.customCols"
        :base-columns="baseColumnsMapped"
        :show-target-table="false"
      >
        <template #extra-config>
          <div class="bg-[#1e293b] text-slate-100 rounded-xl p-6 shadow-lg border border-slate-700 mb-6">
            <h3 class="text-sm font-bold text-emerald-400 uppercase tracking-wider mb-4 flex items-center gap-2">
              <i class="pi pi-users"></i>
              Configuration des Équipes et Horaires
            </h3>
            <div class="space-y-3">
              <div v-for="(eq, idx) in store.entete.configurationColonnes.equipes" :key="idx" class="flex items-center gap-4 bg-slate-800 p-3 rounded-lg border border-slate-700">
                <div class="flex-1">
                  <label class="block text-[10px] font-bold text-slate-400 uppercase mb-1">Nom de l'équipe</label>
                  <input type="text" v-model="eq.nom" class="w-full bg-slate-900 border border-slate-600 rounded px-3 py-1.5 text-sm outline-none focus:border-emerald-500" placeholder="Ex: Equipe 1" />
                </div>
                <div class="w-24">
                  <label class="block text-[10px] font-bold text-slate-400 uppercase mb-1">Heure début</label>
                  <input type="number" v-model="eq.debut" min="0" max="23" class="w-full bg-slate-900 border border-slate-600 rounded px-3 py-1.5 text-sm outline-none focus:border-emerald-500" />
                </div>
                <div class="w-24">
                  <label class="block text-[10px] font-bold text-slate-400 uppercase mb-1">Heure fin</label>
                  <input type="number" v-model="eq.fin" min="0" max="23" class="w-full bg-slate-900 border border-slate-600 rounded px-3 py-1.5 text-sm outline-none focus:border-emerald-500" />
                </div>
                <div class="pt-5">
                  <button @click="store.entete.configurationColonnes.equipes.splice(idx, 1)" class="p-2 text-rose-400 hover:bg-rose-500/10 rounded-lg transition-colors" title="Supprimer l'équipe">
                    <i class="pi pi-trash"></i>
                  </button>
                </div>
              </div>
            </div>
            <button @click="store.entete.configurationColonnes.equipes.push({ nom: 'Nouvelle équipe', debut: 6, fin: 14 })" class="mt-4 px-4 py-2 bg-slate-700 hover:bg-slate-600 text-white text-xs font-bold rounded-lg border border-slate-600 transition-colors flex items-center gap-2">
              <i class="pi pi-plus"></i> Ajouter une équipe
            </button>
          </div>
        </template>
        <template #preview="{ previewColumns }">
          <table class="w-full text-left border-collapse text-sm">
            <thead class="bg-slate-100 text-slate-700 text-[11px] font-bold border-b border-slate-300">
                <tr v-if="hasGroups">
                    <th class="p-2 border-r border-b border-slate-300 text-center align-middle" colspan="2">
                        <div class="text-[12px] font-black text-slate-800 tracking-wide uppercase">Test de Non-conformité</div>
                    </th>
                    <template v-for="(g, idx) in getPreviewGroups(previewColumns)" :key="'prevg'+idx">
                        <th v-if="g.name" :colspan="g.colspan" class="border-r border-b border-slate-300 text-center align-top relative p-2">
                            <div class="mb-2 text-[13px] font-black text-slate-800">{{ g.name }}</div>
                            <div v-if="g.name === 'Equipe 1' || g.name === 'Equipe 2'" class="flex items-center justify-center gap-4 mt-2 px-2 pb-1 text-[10px] normal-case font-semibold">
                                <div class="flex items-center gap-1.5 whitespace-nowrap">
                                    <label class="text-slate-500">Nom et prénom :</label>
                                    <input type="text" disabled class="border-b border-slate-300 bg-transparent px-1 py-0.5 outline-none w-24" />
                                </div>
                                <div class="flex items-center gap-1.5 whitespace-nowrap">
                                    <label class="text-slate-500">Matricule :</label>
                                    <input type="text" disabled class="border-b border-slate-300 bg-transparent px-1 py-0.5 outline-none w-16" />
                                </div>
                            </div>
                        </th>
                        <template v-else>
                            <th v-for="(col, i) in g.columns" :key="col.key" rowspan="2" class="p-2 border-r border-slate-300 text-center align-middle min-w-[75px] max-w-[90px] leading-snug">
                                <span :class="col.key.startsWith('custom_') ? 'text-amber-600 font-medium bg-amber-50 px-2 py-1 rounded border border-amber-200 inline-block' : ''">
                                    {{ col.label }} <span v-if="col.key.startsWith('custom_')">(Auto)</span>
                                </span>
                            </th>
                        </template>
                    </template>
                </tr>
                <tr v-if="hasGroups">
                    <th class="p-2 border-r border-slate-300 min-w-[120px] text-center bg-slate-50/50">Machine<br/>Banc d'essai</th>
                    <th class="p-2 border-r border-slate-300 min-w-[150px] text-center bg-slate-50/50">Désignation du défaut</th>
                    <template v-for="col in previewColumns.filter(c => c.key !== 'col_machine' && c.key !== 'col_designation')" :key="'prevsub'+col.key">
                        <th v-if="col.group" class="p-2 border-r border-slate-300 text-center text-[10px]">
                            <span :class="col.key.startsWith('custom_') ? 'text-amber-600 font-medium bg-amber-50 px-1 py-0.5 rounded border border-amber-200 inline-block' : ''">
                                {{ col.label }} <span v-if="col.key.startsWith('custom_')">(Auto)</span>
                            </span>
                        </th>
                    </template>
                </tr>
            </thead>
            <tbody class="bg-white">
                <tr class="hover:bg-slate-50">
                    <td class="p-2 border-r border-b align-middle bg-white text-center">
                        <div class="px-3 py-2 text-xs font-bold text-slate-700">Machine...</div>
                    </td>
                    <td class="p-2 border-r border-b align-middle bg-white text-center">
                        <div class="px-3 py-2 text-xs font-semibold text-slate-800 uppercase">Désignation...</div>
                    </td>
                    <td :colspan="previewColumns.length - 2" class="p-2 border-r border-b align-middle text-center bg-slate-50/50">
                        <span class="text-xs font-bold text-slate-400 italic tracking-wider uppercase">À remplir lors de la production</span>
                    </td>
                </tr>
            </tbody>
          </table>
        </template>
      </ColumnConfigurator>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { usePlanNcStore } from '@/stores/planNcStore';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import { parseDesignation } from '@/utils/designationParser';

const props = defineProps({
    isReadOnly: { type: Boolean, default: false }
});

const store = usePlanNcStore();
const toast = useToast();
const confirm = useConfirm();
const router = useRouter();
const route = useRoute();
const fileInput = ref(null);
const refFormulaireSelected = ref('');
const showColumnModal = ref(false);

const standardColumns = computed(() => {
  const cols = [];
  const equipes = store.entete.configurationColonnes?.equipes || [];
  
  equipes.forEach((eq, idx) => {
    let cur = eq.debut;
    let steps = eq.fin > eq.debut ? (eq.fin - eq.debut) : (24 - eq.debut + eq.fin);
    for (let i = 0; i < steps; i++) {
      let start = cur % 24;
      let end = (cur + 1) % 24;
      cols.push({
        id: `col_${idx}_${start}_${end}`,
        header: `${start}-${end}`,
        type: 'TEXT',
        group: eq.nom
      });
      cur++;
    }
  });
  
  cols.push({ id: 'col_tot_defauts', header: 'Total des défauts', type: 'TEXT' });
  cols.push({ id: 'col_tot_pieces', header: 'Total des pièces testées', type: 'TEXT' });
  cols.push({ id: 'col_taux_nc', header: 'Taux de NC', type: 'TEXT' });
  
  return cols;
});

const baseColumnsMapped = computed(() => {
  const base = [
    { key: 'col_machine', label: "Machine Banc d'essai", type: 'TEXT', group: null },
    { key: 'col_designation', label: "Désignation du défaut", type: 'TEXT', group: null }
  ];
  const std = standardColumns.value.map(c => ({
    key: c.id,
    label: c.header,
    type: 'TEXT',
    group: c.group
  }));
  return [...base, ...std];
});

const customColumns = computed(() => {
  const standardIds = standardColumns.value.map(c => c.id);
  const customCols = store.entete.configurationColonnes?.customCols || [];
  return customCols.filter(c => {
    const colId = c.id || c.key;
    return !standardIds.includes(colId) && colId !== 'col_machine' && colId !== 'col_designation';
  });
});

const allColumns = computed(() => {
  let cols = [...standardColumns.value];
  customColumns.value.forEach(cc => {
    const mappedCc = {
      id: cc.id || cc.key,
      header: cc.header || cc.label,
      type: cc.type || 'TEXT',
      group: null,
      isCustom: true
    };
    
    let insertIdx = -1;
    if (cc.insertAfter) {
      if (cc.insertAfter === 'col_machine' || cc.insertAfter === 'col_designation') {
         insertIdx = 0;
      } else {
         insertIdx = cols.findIndex(c => c.id === cc.insertAfter);
         if (insertIdx !== -1) insertIdx += 1;
      }
    }
    
    if (insertIdx !== -1) {
      cols.splice(insertIdx, 0, mappedCc);
    } else {
      cols.push(mappedCc);
    }
  });
  return cols;
});

const headerGroups = computed(() => {
  const groups = [];
  let currentGroup = null;
  let currentGroupCols = [];
  let colspan = 0;
  
  const cols = allColumns.value;
  for (const col of cols) {
    if (col.group === currentGroup) {
      colspan++;
      currentGroupCols.push(col);
    } else {
      if (colspan > 0) {
        groups.push({ name: currentGroup, colspan, columns: currentGroupCols });
      }
      currentGroup = col.group;
      currentGroupCols = [col];
      colspan = 1;
    }
  }
  if (colspan > 0) {
    groups.push({ name: currentGroup, colspan, columns: currentGroupCols });
  }
  return groups;
});

const getPreviewGroups = (cols) => {
  const groups = [];
  let currentGroup = null;
  let currentGroupCols = [];
  let colspan = 0;
  
  const filteredCols = cols.filter(c => c.key !== 'col_machine' && c.key !== 'col_designation');
  for (const col of filteredCols) {
    if (col.group === currentGroup) {
      colspan++;
      currentGroupCols.push(col);
    } else {
      if (colspan > 0) {
        groups.push({ name: currentGroup, colspan, columns: currentGroupCols });
      }
      currentGroup = col.group;
      currentGroupCols = [col];
      colspan = 1;
    }
  }
  if (colspan > 0) {
    groups.push({ name: currentGroup, colspan, columns: currentGroupCols });
  }
  return groups;
};

const hasGroups = computed(() => headerGroups.value.some(g => g.name));

const emit = defineEmits(['close']);

const onCancel = () => {
    router.push('/dev/hub');
};

onMounted(async () => {
  if (!props.isReadOnly && !store.entete.id) {
    store.fetchFormulairesReferences('RESULTAT_CONTROLE_POSTE');
  }

  await store.fetchDictionnaires();
  await store.fetchTousLesPlans();
  
  if (route.params.id) {
      await store.chargerPlanNc(route.params.id);
  } else {
      store.resetState();
  }
});

// Pré-remplir le sélecteur Réf Formulaire si on est sur un modèle template
watch(() => store.entete.isModeleTemplate, (newVal) => {
  if (newVal && store.entete.formulaireId) {
    refFormulaireSelected.value = store.entete.formulaireId;
  }
});

// Lorsqu'on sélectionne une réf. formulaire depuis un template générique
watch(refFormulaireSelected, (newRefId) => {
  if (newRefId && !store.entete.isModeleTemplate) {
    const selectedForm = store.formulairesReferences.find(f => f.id === newRefId);
    if (!selectedForm) return;

    const designation = selectedForm.designation || '';
    const parsed = parseDesignation(designation, [], [], store.postes || []);

    if (parsed.posteCode) {
      store.entete.posteCode = parsed.posteCode;
      store.entete.formulaireId = newRefId;
      store.entete.formulaireCodeReference = selectedForm.codeReference || null;
      onSelectionChange();
    }
  }
});

const machinesAssocieesAuPoste = computed(() => 
  store.machines.filter(m => m.posteCode === store.entete.posteCode)
);

const onSelectionChange = () => {
  if (store.entete.posteCode) {
      store.initialiserNouveauPlan(
        store.entete.posteCode,
        store.entete.formulaireId,
        store.entete.formulaireCodeReference
      );
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
        // Comparer par formulaireId si disponible (FE-RC-PAS71 != FE-RC-PAS71_SOUPAPE = plans indépendants)
        let planActif = null;
        if (store.entete.formulaireId) {
            planActif = store.plansExistants.find(p => p.statut === 'ACTIF' && p.formulaireId === store.entete.formulaireId);
        } else {
            planActif = store.plansExistants.find(p => p.statut === 'ACTIF' && p.posteCode === store.entete.posteCode && !p.formulaireId);
        }
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
            router.push('/dev/hub');
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
