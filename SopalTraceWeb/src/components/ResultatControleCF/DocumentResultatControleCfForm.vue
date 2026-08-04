<template>
  <div class="space-y-6 pb-16">

    <!-- ═══════════════════════════════════════════
         1. CONTEXTE DU POSTE
    ═══════════════════════════════════════════ -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 relative">
      <div class="flex items-center gap-2 mb-6">
        <i class="pi pi-check-circle text-teal-500"></i>
        <h2 class="text-sm font-black text-slate-700 uppercase tracking-widest">1. CONTEXTE DU POSTE</h2>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="md:col-span-2 lg:col-span-1">
          <div class="flex items-center gap-2 mb-2">
            <i class="pi pi-file-export text-teal-600"></i>
            <label class="text-xs font-black text-teal-800 uppercase tracking-widest">Réf. Formulaire <span class="text-red-500">*</span></label>
          </div>
          <div class="flex items-center gap-3">
            <Dropdown v-model="store.entete.formulaireId" 
                      :options="(referentielStore.formulairesReferencesByRole['RESULTAT_CONTROLE_CF'] || [])" 
                      optionLabel="designation"
                      optionValue="id"
                      @change="onFormulaireChange"
                      placeholder="Sélectionnez une référence"
                      :disabled="isReadOnly"
                      class="w-full border-teal-200 focus:border-teal-400" />
            <span class="text-[10px] text-teal-600/70 italic hidden md:inline">La sélection du formulaire remplira automatiquement le contexte.</span>
          </div>
        </div>
        <!-- Le champ Poste de Travail est maintenant visible pour permettre à l'utilisateur de choisir -->
        <div class="md:col-span-2 lg:col-span-1">
          <div class="flex items-center gap-2 mb-2">
            <i class="pi pi-cog text-teal-600"></i>
            <label class="text-xs font-black text-teal-800 uppercase tracking-widest">Poste de Travail</label>
          </div>
          <Dropdown v-model="store.entete.posteCode" 
                    :options="referentielStore.postes || referentielStore.postesTravail || []" 
                    optionLabel="libelle"
                    optionValue="code"
                    placeholder="-- Sélectionnez un poste (optionnel) --"
                    :disabled="isReadOnly"
                    filter
                    class="w-full border-teal-200 focus:border-teal-400" />
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mt-6">
        <div>
          <label class="block text-xs font-bold text-slate-500 mb-1">Désignation Formulaire</label>
          <input type="text" v-model="store.entete.nom" :disabled="isReadOnly"
                 class="w-full bg-white border border-slate-200 rounded-lg px-3 py-2 text-sm outline-none focus:border-teal-500 transition-colors" />
        </div>
      </div>

      <div class="flex items-center justify-end gap-3 mt-6 pt-4 border-t border-slate-100" v-if="!isReadOnly">
        <input type="file" ref="fileInput" @change="onFileSelected" accept=".xlsx, .xls" class="hidden" />
        <button @click="triggerFileInput" :disabled="isImporting"
                class="h-10 px-4 bg-teal-600 hover:bg-teal-700 text-white font-bold text-xs uppercase tracking-wider rounded-lg flex items-center gap-2 transition-colors shadow-sm">
          <i class="pi" :class="isImporting ? 'pi-spinner pi-spin' : 'pi-file-excel'"></i>
          Importer la structure Excel
        </button>

        <button @click="showColumnModal = true"
                class="h-10 px-4 bg-slate-800 hover:bg-slate-700 text-white font-bold text-xs uppercase tracking-wider rounded-lg flex items-center gap-2 transition-colors shadow-sm">
          <i class="pi pi-sliders-h"></i>
          Configurer Colonnes
        </button>
      </div>
    </div>

    <!-- ═══════════════════════════════════════════
         2. APERÇU DE LA FICHE ENCF (Mode Vue)
    ═══════════════════════════════════════════ -->
    <div class="mt-8 pt-4">
      <div class="flex items-center gap-2 mb-4">
        <i class="pi pi-eye text-blue-500"></i>
        <h2 class="text-sm font-black text-slate-700 uppercase tracking-widest">Résultats du contrôle en cours de fabrication</h2>
      </div>

            <div v-if="!store.entete.formulaireId" class="p-8 text-center bg-slate-50 border border-dashed border-slate-300 rounded-xl mb-6">
        <i class="pi pi-file-export text-4xl text-slate-300 mb-3"></i>
        <p class="text-sm font-bold text-slate-500 uppercase tracking-widest">Veuillez sélectionner une Réf. Formulaire pour afficher l'aperçu des tables.</p>
      </div>

      <!-- Le conteneur en opacité réduite pour montrer que c'est un aperçu inactif -->
      <div v-else class="opacity-90">
        
        <template v-for="(sec, index) in store.sections" :key="index">
        
          <!-- APPROBATION -->
          <div v-if="sec.sectionType === 'APPROBATION'" class="border border-slate-200 rounded-lg shadow-sm mb-6 bg-white overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <thead class="bg-[#0f1923] text-white">
                <tr class="text-[11px] font-black uppercase tracking-widest">
                  <th class="p-3 w-[25%] border-r border-slate-700">Heure</th>
                  <th class="p-3 w-[25%] text-center border-r border-slate-700">Résultat</th>
                  <th class="p-3 w-[50%]">Signature (Matricule)</th>
                </tr>
              </thead>
              <tbody>
                <PlanSectionHeader 
                  :section="sec"
                  :index="index"
                  :colspan="3"
                  label="SEC"
                  defaultTitle=""
                  :types-section="referentielStore.typesSection"
                  :periodicites="referentielStore.periodicites"
                  :regles-echantillonnage="referentielStore.reglesEchantillonnage"
                  :isReadOnly="isReadOnly"
                  hideAddLigne
                  hideRemove
                  @update:section="(newVal) => Object.assign(sec, newVal)"
                />
                <tr class="bg-white border-t border-slate-100">
                  <td colspan="3" class="p-8 text-center">
                    <div class="inline-flex items-center gap-2 px-6 py-4 rounded-xl bg-slate-50 border border-dashed border-slate-300 text-slate-400 text-sm font-black uppercase tracking-widest italic shadow-sm">
                      <i class="pi pi-pencil text-lg"></i>
                      À remplir par l'opérateur lors de l'exécution
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
            </div>
          </div>

          <!-- TRANCHES -->
          <div v-if="sec.sectionType === 'TRANCHES'" class="border border-slate-200 rounded-lg shadow-sm mb-6 bg-white overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <thead class="bg-[#0f1923] text-white">
                  <tr class="text-[11px] font-black uppercase tracking-widest">
                    <th rowspan="2" class="p-3 w-[12%] border-r border-slate-700 align-middle text-center">Fréquence</th>
                    <th colspan="2" class="p-2 text-center border-r border-b border-slate-700 w-[10%]">Résultats</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-700 align-middle">Non-conformité</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-700 align-middle">Actions de correction</th>
                    <th rowspan="2" class="p-3 w-[14%] align-middle text-center">Approbation</th>
                  </tr>
                  <tr class="text-[11px] font-black uppercase tracking-widest border-t border-slate-700">
                    <th class="p-2 text-center border-r border-slate-700 w-[5%]">C</th>
                    <th class="p-2 text-center border-r border-slate-700 w-[5%]">NC</th>
                  </tr>
                </thead>
                <tbody>
                  <PlanSectionHeader 
                    :section="sec"
                    :index="index"
                    :colspan="6"
                    label="SEC"
                    defaultTitle=""
                    :types-section="referentielStore.typesSection"
                    :periodicites="referentielStore.periodicites"
                    :regles-echantillonnage="referentielStore.reglesEchantillonnage"
                    :isReadOnly="isReadOnly"
                    hideAddLigne
                    hideRemove
                    @update:section="(newVal) => Object.assign(sec, newVal)"
                  />
                  <tr class="bg-white border-t border-slate-100">
                    <td colspan="6" class="p-8 text-center">
                      <div class="inline-flex items-center gap-2 px-6 py-4 rounded-xl bg-slate-50 border border-dashed border-slate-300 text-slate-400 text-sm font-black uppercase tracking-widest italic shadow-sm">
                        <i class="pi pi-pencil text-lg"></i>
                        La grille horaire (24h) sera remplie par l'opérateur lors de l'exécution
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- SECTIONS AVEC LIGNES DE CARACTÉRISTIQUES (REGLAGE, CUSTOM, LOT_POSTE, LOT_OF, etc.) -->
          <div v-if="sec.sectionType !== 'APPROBATION' && sec.sectionType !== 'TRANCHES'" class="border border-slate-200 rounded-lg shadow-sm mb-6 bg-white overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <thead class="bg-[#0f1923] text-white">
                  <tr class="text-[11px] font-black uppercase tracking-widest">
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-700 align-middle">Caractéristiques contrôlées</th>
                    <th colspan="2" class="p-2 text-center border-r border-b border-slate-700 w-[10%]">Résultat</th>
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-700 align-middle">Non-conformité</th>
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-700 align-middle">Actions de correction</th>
                    <th rowspan="2" class="p-3 w-[15%] align-middle text-center">Approbation Matricule</th>
                    <th v-for="col in modeleColumns.filter(c => c.isCustom)" :key="col.key" rowspan="2" class="p-3 border-l border-slate-700 align-middle text-center">
                      {{ col.label }} <span class="text-amber-400 text-[9px] uppercase ml-1">(Perso)</span>
                    </th>
                  </tr>
                  <tr class="text-[11px] font-black uppercase tracking-widest border-t border-slate-700">
                    <th class="p-2 text-center border-r border-slate-700 w-[5%]">C</th>
                    <th class="p-2 text-center border-r border-slate-700 w-[5%]">NC</th>
                  </tr>
                </thead>
                <tbody>
                  <PlanSectionHeader 
                    :section="sec"
                    :index="index"
                    :colspan="6 + modeleColumns.filter(c => c.isCustom).length"
                    label="SEC"
                    defaultTitle="Caractéristiques à contrôler"
                    :types-section="referentielStore.typesSection"
                    :periodicites="referentielStore.periodicites"
                    :regles-echantillonnage="referentielStore.reglesEchantillonnage"
                    :isReadOnly="isReadOnly"
                    :hideRemove="sec.sectionType === 'REGLAGE'"
                    @add-ligne="sec.lignes = sec.lignes || []; sec.lignes.push({ caracteristique: '' })"
                    @update:section="(newVal) => Object.assign(sec, newVal)"
                    @remove="removeSection(sec)"
                  />
                  <tr v-for="(ligne, lIdx) in sec.lignes" :key="lIdx" class="border-b border-slate-100 last:border-none hover:bg-slate-50/50">
                    <td class="p-1 border-r border-slate-100 align-middle">
                      <div class="flex items-center gap-2 group/ligne">
                        <input v-if="!isReadOnly" type="text" v-model="ligne.caracteristique" 
                               @input="ligne.libelleAffiche = ligne.caracteristique"
                               class="w-full bg-transparent border border-transparent rounded px-2 py-1.5 text-xs text-slate-800 font-semibold outline-none hover:bg-slate-200/50 focus:border-blue-400 focus:bg-white transition-colors"
                               placeholder="Saisissez la caractéristique..." />
                        <span v-else class="px-2 py-1.5 text-xs text-slate-800 font-semibold">{{ ligne.caracteristique || '-' }}</span>
                        <button v-if="!isReadOnly" @click="sec.lignes.splice(lIdx, 1)" class="opacity-0 group-hover/ligne:opacity-100 text-slate-400 hover:text-red-500 transition-opacity p-1" title="Supprimer la ligne">
                          <i class="pi pi-trash text-xs"></i>
                        </button>
                      </div>
                    </td>
                    <td class="p-3 border-r border-slate-100 text-center"></td>
                    <td class="p-3 border-r border-slate-100 text-center"></td>
                    <td class="p-3 border-r border-slate-100 text-center"></td>
                    <td class="p-3 border-r border-slate-100 text-center"></td>
                    <td class="p-3 border-r border-slate-100 text-center"></td>
                    <td v-for="col in modeleColumns.filter(c => c.isCustom)" :key="col.key" class="p-3 border-l border-slate-100 text-center text-slate-400 text-[10px] italic">
                      Sera rempli à l'exécution
                    </td>
                  </tr>
                  <tr v-if="!sec.lignes || sec.lignes.length === 0">
                    <td :colspan="6 + modeleColumns.filter(c => c.isCustom).length" class="p-8 text-center">
                      <div class="inline-flex items-center gap-2 px-6 py-4 rounded-xl bg-slate-50 border border-dashed border-slate-300 text-slate-400 text-sm font-black uppercase tracking-widest italic shadow-sm">
                        <i class="pi pi-pencil text-lg"></i>
                        À remplir par l'opérateur lors de l'exécution (Veuillez d'abord importer la structure ou ajouter manuellement)
                      </div>
                    </td>
                  </tr>
                  <tr v-if="!isReadOnly" class="bg-slate-50/50 border-t border-slate-200">
                    <td :colspan="6 + modeleColumns.filter(c => c.isCustom).length" class="p-2 text-center">
                      <button @click="sec.lignes = sec.lignes || []; sec.lignes.push({ caracteristique: '' })" class="inline-flex items-center gap-2 text-xs font-bold text-blue-600 hover:text-blue-700 bg-blue-50 hover:bg-blue-100 px-3 py-1.5 rounded transition-colors uppercase tracking-widest">
                        <i class="pi pi-plus"></i> Ajouter une caractéristique
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>
      </div>
    <!-- NOTES & LÉGENDES -->
    <div v-if="!isReadOnly" class="mt-4">
      <RemarquesLegendeBox 
        v-model:remarques="store.entete.notes"
        v-model:legendeMoyens="store.entete.legendeMoyens"
        :is-read-only="isReadOnly"
      />
    </div>

    <!-- ACTIONS (ENREGISTRER / ANNULER) -->
    <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-4 rounded-xl shadow-sm" v-if="!isReadOnly">
      <DocumentSaveManager 
        :plan-id="store.entete.id"
        :statut="store.entete.statut"
        :is-loading="store.isLoading"
        :is-read-only="isReadOnly"
        @save-direct="handleSaveDirect"
        @save-correction="handleSaveCorrection"
        @save-new-version="handleSaveNewVersion"
        @cancel="() => $router.push('/dev/hub')"
      />
    </div>
  </div>
    
    <!-- MODAL CONFIGURATION COLONNES -->
    <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationJson"
      :base-columns="baseModeleColumns"
      :hide-insert-after="false"
    />
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { usedocumentRccfStore } from '@/stores/documentRccfStore';
import { useReferentielStore } from '@/stores/referentielStore';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import Dropdown from 'primevue/dropdown';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import DocumentSaveManager from '@/components/Shared/DocumentSaveManager.vue';
import PlanSectionHeader from '@/components/Shared/PlanSectionHeader.vue';

defineProps({
  isReadOnly: { type: Boolean, default: false }
});

const store = usedocumentRccfStore();
const referentielStore = useReferentielStore();
const fabModeleStore = useFabModeleStore();
const toast = useToast();
const confirm = useConfirm();
const router = useRouter();



const fileInput = ref(null);
const isImporting = ref(false);
const showColumnModal = ref(false);

const baseModeleColumns = [
  { key: 'caracteristique', label: 'Caractéristiques contrôlées', width: 'w-[25%]' },
  { key: 'limite_spec', label: 'Limite Spécif.', width: 'w-[15%]' },
  { key: 'type_controle', label: 'Type de Contrôle', width: 'w-[15%]' },
  { key: 'moyen_controle', label: 'Moyen de Contrôle', width: 'w-[15%]' },
  { key: 'observations', label: 'Observations', width: 'flex-1' }
];

const modeleColumns = computed(() => {
  let cols = [...baseModeleColumns];
  const customCols = store.entete?.configurationJson || [];
  
  customCols.forEach(cc => {
    const insertIdx = cols.findIndex(c => c.key === cc.insertAfter);
    const newCol = { key: cc.key, label: cc.label, isCustom: true, width: 'w-[12%]' };
    if (insertIdx !== -1) {
      cols.splice(insertIdx + 1, 0, newCol);
    } else {
      cols.push(newCol);
    }
  });

  return cols;
});

const onFormulaireChange = (event) => {
  const selectedId = event.value || event;
  const val = (referentielStore.formulairesReferencesByRole['RESULTAT_CONTROLE_CF'] || []).find(f => f.id === selectedId);

  if (val && store.entete) {
    store.entete.nom = val.designation;
    store.entete.formulaireCodeReference = val.codeReference;

    const postesList = referentielStore.postes || referentielStore.postesTravail || [];
    if (postesList.length > 0) {
      const designationUpper = val.designation.toUpperCase();
      const match = designationUpper.match(/(PAS\s*\d+)/);
      if (match) {
        const posteCodeExtracted = match[1].replace(/\s+/g, '');
        const foundPoste = postesList.find(p => (p.code || '').toUpperCase() === posteCodeExtracted);
        if (foundPoste) {
          store.entete.posteCode = foundPoste.code;
        } else {
          store.entete.posteCode = null;
        }
      } else {
        store.entete.posteCode = null;
      }
    }
    
    // Load custom configuration from Formulaire if it exists
    if (val.configurationStructureJson) {
      try {
        const structure = JSON.parse(val.configurationStructureJson);
        const customCols = (structure.customCols && structure.customCols.length > 0)
            ? structure.customCols.map(c => ({
                key: c.key || c.cleColonne,
                label: c.label || c.labelAffiche || c.titre,
                type: c.type || c.typeValeur || 'Texte',
                insertAfter: c.insertAfter || 'observations'
              }))
            : [];
        store.entete.configurationJson = customCols;
      } catch (e) {
        console.error('Erreur parsing configurationStructureJson:', e);
      }
    } else {
      store.entete.configurationJson = [];
    }

    // Always reinitialize sections when the form explicitly changes via the dropdown
    if (val.designation.includes('Assemblage') || val.designation.includes('ASS')) {
      store.sections = [
        { id: null, sectionType: 'REGLAGE', nom: 'Caractéristiques à contrôler aux réglages (une série de 04 pièces)', libelleAffiche: 'Caractéristiques à contrôler aux réglages (une série de 04 pièces)', ordreAffiche: 1, lignes: [] },
        { id: null, sectionType: 'TRANCHES', nom: 'Caractéristiques à contrôler par échantillonnage en cours de production (Selon FE0591 : échantillon /poste...)', libelleAffiche: 'Caractéristiques à contrôler par échantillonnage en cours de production (Selon FE0591 : échantillon /poste...)', ordreAffiche: 2, lignes: [] },
        { id: null, sectionType: 'LOT_POSTE', nom: 'Caractéristiques à contrôler au niveau du POSTE (la première et la dernière pièce pour chaque poste)', libelleAffiche: 'Caractéristiques à contrôler au niveau du POSTE (la première et la dernière pièce pour chaque poste)', ordreAffiche: 3, lignes: [] }
      ];
    } else {
      store.sections = [
        { id: null, sectionType: 'APPROBATION', nom: 'Approbation des pièces types', libelleAffiche: 'Approbation des pièces types', ordreAffiche: 1, lignes: [] },
        { id: null, sectionType: 'TRANCHES', nom: 'Contrôle en cours de fabrication', libelleAffiche: 'Contrôle en cours de fabrication', ordreAffiche: 2, lignes: [] },
        { id: null, sectionType: 'LOT_POSTE', nom: 'Caractéristiques à contrôler au niveau du POSTE', libelleAffiche: 'Caractéristiques à contrôler au niveau du POSTE', ordreAffiche: 3, lignes: [] }
      ];
    }
  }
};


// Supprimés: secApprobation, secTranches, importedSections


onMounted(async () => {
  if (!referentielStore.isDicosFabricationLoaded) {
    await referentielStore.fetchDictionnaires('fabrication');
  }
  await Promise.all([
    referentielStore.formulairesReferencesByRole['RESULTAT_CONTROLE_CF']?.length
      ? Promise.resolve()
      : referentielStore.fetchFormulairesReferences('RESULTAT_CONTROLE_CF'),
    store.loadAllPlans()
  ]);

  // Note: Le champ "Poste de Travail" est optionnel pour les formulaires génériques (Usi/Esp/Trn, Assemblage...).
  // Il est auto-rempli par onFormulaireChange si la désignation contient un code PAS (ex: PAS78).
  
  // Initialiser les sections de base si le plan est nouveau
  if (!store.sections || store.sections.length === 0) {
    if (store.entete && store.entete.nom && (store.entete.nom.includes('Assemblage') || store.entete.nom.includes('ASS'))) {
      store.sections = [
        { id: null, sectionType: 'REGLAGE', nom: 'Caractéristiques à contrôler aux réglages (une série de 04 pièces)', libelleAffiche: 'Caractéristiques à contrôler aux réglages (une série de 04 pièces)', ordreAffiche: 1, lignes: [] },
        { id: null, sectionType: 'TRANCHES', nom: 'Caractéristiques à contrôler par échantillonnage en cours de production', libelleAffiche: 'Caractéristiques à contrôler par échantillonnage en cours de production', ordreAffiche: 2, lignes: [] },
        { id: null, sectionType: 'LOT_POSTE', nom: 'Caractéristiques à contrôler au niveau du POSTE (la première et la dernière pièce pour chaque poste)', libelleAffiche: 'Caractéristiques à contrôler au niveau du POSTE (la première et la dernière pièce pour chaque poste)', ordreAffiche: 3, lignes: [] }
      ];
    } else {
      store.sections = [
        { id: null, sectionType: 'APPROBATION', nom: 'Approbation des pièces types', libelleAffiche: 'Approbation des pièces types', ordreAffiche: 1, lignes: [] },
        { id: null, sectionType: 'TRANCHES', nom: 'Contrôle en cours de fabrication', libelleAffiche: 'Contrôle en cours de fabrication', ordreAffiche: 2, lignes: [] },
        { id: null, sectionType: 'LOT_POSTE', nom: 'Caractéristiques à contrôler au niveau du POSTE', libelleAffiche: 'Caractéristiques à contrôler au niveau du POSTE', ordreAffiche: 3, lignes: [] }
      ];
    }
  }
});

const triggerFileInput = () => {
  if (fileInput.value) fileInput.value.click();
};

const onFileSelected = async (event) => {
  const file = event.target.files[0];
  if (!file) return;

  event.target.value = '';
  isImporting.value = true;
  const res = await store.importExcel(file);
  isImporting.value = false;
  
  if (res.success) {
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Structure importée avec succès.', life: 3000 });
  } else {
    toast.add({ severity: 'error', summary: 'Erreur', detail: res.message, life: 3000 });
  }
};

const removeSection = (sec) => {
  if (confirm("Supprimer cette section ?")) {
    const storeIdx = store.sections.indexOf(sec);
    if (storeIdx !== -1) {
      store.sections.splice(storeIdx, 1);
    }
  }
};

const validatePlan = async () => {
  // Note: le poste est optionnel pour les formulaires génériques.
  // On ne force plus PAS71 : si aucun poste n'est sélectionné, le champ reste null.

  if (!store.entete.formulaireId) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Veuillez sélectionner un formulaire.', life: 3000 });
    return false;
  }
  
  if (!store.entete.id) {
    const currentRef = store.entete.formulaireCodeReference;
    const currentFormId = store.entete.formulaireId;
    const targetPoste = String(store.entete.posteCode || '').toLowerCase();

    const planActif = store.plans.find(p => {
      if (p.statut !== 'ACTIF') return false;
      if (String(p.posteCode || '').toLowerCase() !== targetPoste) return false;

      if (currentRef && p.formulaireCodeReference) {
        return String(p.formulaireCodeReference).toLowerCase() === String(currentRef).toLowerCase();
      }
      if (currentFormId && p.formulaireId) {
        return String(p.formulaireId).toLowerCase() === String(currentFormId).toLowerCase();
      }
      return !currentRef && !currentFormId && !p.formulaireId && !p.formulaireCodeReference;
    });
    
    if (planActif) {
        const isConfirmed = await new Promise((resolve) => {
            confirm.require({
                message: `Un plan RCCF actif (V${planActif.version || 1}) existe déjà pour ce formulaire. Voulez-vous l'archiver et activer cette nouvelle version ?`,
                header: 'Plan Actif Existant',
                icon: 'ri-error-warning-line text-amber-500',
                acceptLabel: 'Oui, archiver',
                rejectLabel: 'Annuler',
                accept: () => resolve(true),
                reject: () => resolve(false)
            });
        });
        if (!isConfirmed) return false;
        await new Promise(resolve => setTimeout(resolve, 200));
    }
  }
  return true;
};

const handleSaveDirect = async () => {
  const isValid = await validatePlan();
  if (!isValid) return;

  const res = await store.savePlan();
  if (res.success) {
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Le plan ENCF a été enregistré.', life: 3000 });
    setTimeout(() => {
      router.push('/dev/hub');
    }, 1500);
  } else {
    toast.add({ severity: 'error', summary: 'Erreur', detail: res.message, life: 3000 });
  }
};

const handleSaveCorrection = async () => {
  const isValid = await validatePlan();
  if (!isValid) return;

  const res = await store.savePlan(true);
  if (res.success) {
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Correction enregistrée avec succès.', life: 3000 });
    setTimeout(() => router.push('/dev/hub'), 1500);
  } else if (res.noChanges) {
    toast.add({ severity: 'info', summary: 'Info', detail: 'Pas de modification.', life: 3000 });
  } else {
    toast.add({ severity: 'error', summary: 'Erreur', detail: res.message, life: 3000 });
  }
};

const handleSaveNewVersion = async (motif) => {
  const isValid = await validatePlan();
  if (!isValid) return;

  const res = await store.createNewVersion(motif);
  if (res.success) {
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Nouvelle version créée.', life: 3000 });
    setTimeout(() => router.push('/dev/hub'), 1500);
  } else {
    toast.add({ severity: 'error', summary: 'Erreur', detail: res.message, life: 3000 });
  }
};
</script>
