<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <Toast position="top-right" />
    <VersioningDialog :visible="showVersioningDialog"
                      :mode="versioningMode"
                      :is-loading="isLoading"
                      @confirm="onVersioningConfirm"
                      @cancel="showVersioningDialog = false"
                      @update:visible="showVersioningDialog = $event" />

    <div class="max-w-[1600px] mx-auto">
      <PlanHeader 
        :id="modeleEditionId"
        :title="headerTitle"
        :subtitle="headerSubtitle"
        icon="pi pi-cog"
        iconColorClass="text-amber-500"
        :is-read-only="isReadOnly"
        :version="version"
        :statut="statut"
        :is-restoring="isLoading"
        @restaurer="onEditorSubmit"
      >
        <template #actions>
          <div class="flex items-center gap-2 bg-slate-50 px-3 py-1.5 rounded-lg border border-slate-200 ml-4 hidden md:flex">
            <span class="text-[10px] font-black text-slate-400 uppercase">Code:</span>
            <span class="font-mono font-bold text-sm text-slate-700">{{ codeAffiche }}</span>
          </div>
        </template>
      </PlanHeader>
      
      <div class="bg-white rounded-xl shadow-xl border border-slate-200 overflow-hidden">
        <div class="bg-[#1e293b] text-white px-5 py-3.5 flex justify-between items-center">
          <div class="flex items-center gap-3 font-bold tracking-wide">
            <i :class="isReadOnly ? 'pi pi-eye' : 'pi pi-book'" class="text-lg"></i>
            {{ isReadOnly ? 'Visualisation du modèle' : 'Éditeur de Structure' }}
            <span v-if="isForcedView" class="bg-blue-500/20 text-blue-300 px-2 py-0.5 rounded text-xs ml-2 uppercase tracking-widest border border-blue-400/30">Mode Lecture</span>
          </div>
          <button @click="$router.push(returnUrl)" class="text-slate-400 hover:text-white transition-colors">
            <i class="pi pi-times text-lg"></i>
          </button>
        </div>

        <div class="p-6 md:p-8">
          <div class="flex flex-col md:flex-row items-center justify-between p-6 bg-slate-50 border-b border-slate-200">
            <div class="flex-1 w-full md:w-auto">
              <assPlanHeader :is-edit-mode="isEditMode" :is-read-only="isReadOnly">
                <template #actions>
                  <button v-if="!isReadOnly" @click="() => fileInput.click()" class="px-4 py-2 bg-[#059669] text-white hover:bg-[#047857] rounded-lg border border-[#059669] text-xs font-bold flex items-center gap-2 transition-colors shadow-sm">
                    <i class="pi pi-file-excel"></i>
                    <span>Importer la structure Excel</span>
                  </button>
                  <button v-if="!isReadOnly" @click="showColumnModal = true" class="text-xs font-bold px-4 py-2 bg-[#0f172a] text-white rounded-lg border border-[#0f172a] hover:bg-[#1e293b] transition-colors flex items-center gap-2 shadow-sm ml-2">
                    <i class="pi pi-sliders-h text-sm"></i>
                    <span>Configurer Colonnes</span>
                  </button>
                  <input type="file" ref="fileInput" @change="onFileSelected" accept=".xlsx,.xls" class="hidden" />
                </template>
              </assPlanHeader>
            </div>
          </div>

          <div class="mb-4 flex items-center justify-between">
            <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest mt-6">2. Structure des lignes de contrôle</h3>
          </div>

          <template v-if="groupes.length === 0">
            <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
              Cliquez sur "Créer une nouvelle section" pour commencer.
            </div>
          </template>

          <!-- Mode LECTURE -->
          <div v-if="isReadOnly" class="p-4 md:p-6">
            <PlanReadView
              :sections="groupes"
              :remarques="store.entete.notes"
              :legende-moyens="store.entete.legendeMoyens"
              :configuration-colonnes="store.entete.configurationColonnes"
              :types-section="store.typesSection || []"
              :types-caracteristique="store.typesCaracteristique || []"
              :types-controle="store.typesControle || []"
              :moyens-controle="store.moyensControle || []"
              :periodicites="store.periodicites || []"
            />
          </div>

          <!-- Mode EDITION -->
          <div v-else class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
            <table class="w-full text-left border-collapse min-w-[1200px]">
              <AssTableHeader :columns="modeleColumns" />
              
              <SharedSectionCard 
                v-for="(section, index) in groupes" 
                :key="section.id" 
                :groupe="section" 
                :index="index"
                :is-read-only="isReadOnly"
                :typesSection="store.typesSection"
                :periodicites="store.periodicites"
                :reglesEchantillonnage="store.reglesEchantillonnage"
                :operationCode="store.entete.operationCode"
                defaultTitle="Caractéristiques à contrôler"
                @remove="supprimerGroupe(section.id)"
                @update-groupe="(updatedGroupe) => mettreAJourGroupe(index, updatedGroupe)"
                @section-type-required="() => toast.add({ severity: 'warn', summary: 'Type de section requis', detail: 'Veuillez définir la nature de la section avant d\'ajouter une ligne.', life: 4000 })"
              >
                <AssLigneControl 
                  v-for="ligne in section.lignes" 
                  :key="ligne.id" 
                  :ligne="ligne"
                  :columns="modeleColumns"
                  :is-read-only="isReadOnly"
                  :operation-code="store.entete?.operationCode"
                  @remove="(ligneId) => supprimerLigneASection(index, ligneId)"
                  @update="(updatedLigne) => mettreAJourLigne(index, updatedLigne)"
                />
              </SharedSectionCard>
            </table>
          </div>
          
          <div class="mt-2" v-if="!isReadOnly">
            <button @click="ajouterGroupe" class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
              <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
            </button>
          </div>

          <!-- Notes & Légende en mode éditeur uniquement -->
          <div v-if="!isReadOnly" class="mt-2">
            <RemarquesLegendeBox 
              v-model:remarques="store.entete.notes"
              v-model:legendeMoyens="store.entete.legendeMoyens"
              :show-validation="showLegendValidation"
              :has-custom-instruments="hasCustomInstrumentsGlobal"
              :is-read-only="isReadOnly"
            />
          </div>
        </div>

        <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end" v-if="!isForcedView">
          <button v-if="statut === 'BROUILLON' && isEditMode" @click="activerPlanCourant" class="bg-emerald-600 hover:bg-emerald-700 text-white px-5 py-2.5 rounded-lg font-bold shadow-md transition-all active:scale-95 flex items-center gap-2 mr-4" :disabled="store.isLoading">
            <i v-if="store.isLoading" class="pi pi-spin pi-spinner"></i>
            <i v-else class="pi pi-check-circle"></i>
            Activer le modèle
          </button>
          <EditorActions 
            :label="actionButtonLabel"
            :icon="actionButtonIcon"
            :variant="actionButtonVariant"
            :is-loading="isLoading"
            @submit="onEditorSubmitClick"
            @cancel="() => $router.push(returnUrl)"
          />
        </div>
      </div>
    </div>
    
    <!-- MODAL DE CONFIGURATION DES COLONNES -->
    <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAssPlanStore } from '@/stores/assPlanStore';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';

import { documentService as assPlanService } from '@/services/documentService';
import { planFabricationService as fabPlanService } from '@/services/planFabricationService';
import { useAssPlanVersioning } from '@/composables/useVersioning';
import { createModeleSnapshot, prepareModeleDataAndFrequencies } from '@/utils/modelMapper';
import { parseFrequenceLibelle, resolveFrequencyFromPeriodiciteId } from '@/utils/frequencyUtils';

import PlanHeader from '@/components/Shared/PlanHeader.vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import PlanReadView from '@/components/Shared/PlanReadView.vue';
import assPlanHeader from '@/components/Assemblage/assPlanHeader.vue';
import AssTableHeader from '@/components/Assemblage/AssTableHeader.vue';
import SharedSectionCard from '@/components/Shared/SharedSectionCard.vue';
import AssLigneControl from '@/components/Assemblage/AssLigneControl.vue'; 
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';

import { useEditorSections } from '@/composables/useEditorSections';
import { useEditorValidation } from '@/composables/useEditorValidation';
import { useDirtyChecking } from '@/composables/useDirtyChecking';

const store = useAssPlanStore();
const toast = useToast();
const route = useRoute();
const router = useRouter();

const returnUrl = computed(() => '/dev/hub');

// ============================================================================
// ÉTAT LOCAL (Métier)
// ============================================================================
const { 
  sections: groupes, 
  ajouterSection: ajouterGroupe, 
  supprimerSection: supprimerGroupe, 
  mettreAJourSection: mettreAJourGroupe, 
  supprimerLigneASection, 
  mettreAJourLigne 
} = useEditorSections();

const modeleEditionId = ref(null);
const codeOriginal = ref('');
const statut = ref('BROUILLON');
const version = ref(0);
const showVersioningDialog = ref(false);
const showColumnModal = ref(false);
const versioningMode = ref('ASS');
const isAutoVersioning = ref(false);
const isArchiveEditing = ref(route.query.draft === 'true');
const isUpgradeMode = computed(() => route.query.upgrade === 'true');

const { 
  showLegendValidation, 
  hasCustomInstrumentsGlobal, 
  validerLegendeMoyens, 
  validerSaisieValeurs 
} = useEditorValidation(groupes, computed(() => store.entete.legendeMoyens), toast);

const { isDirty, updateCurrentSnapshot, initializeSnapshot } = useDirtyChecking();
const { restaurerPlan } = useAssPlanVersioning();

// 👁️ NOUVEAU : DÉTECTION DU MODE LECTURE SEULE DEPUIS L'URL
const isForcedView = computed(() => route.query.view === 'true');

watch(
  [() => store.entete, () => groupes.value],
  ([newEntete, newGroupes]) => {
    // Ne pas tracer la dirty checking si on est juste en mode vue
    if (!isForcedView.value) {
      const enteteClone = JSON.parse(JSON.stringify(newEntete));
      const groupesClone = JSON.parse(JSON.stringify(newGroupes));
      updateCurrentSnapshot(createModeleSnapshot(enteteClone, groupesClone));
    }
  },
  { deep: true }
);

// ============================================================================
// COLONNES RÉUTILISABLES ET DYNAMIQUES
// ============================================================================
const baseModeleColumns = [
  { key: 'caracteristique', label: 'Caractéristique contrôlée', width: 'w-[22%]' },
  { key: 'limite_spec', label: 'Limite spécif.', width: 'w-[12%]', textAlign: 'center' },
  { key: 'type_controle', label: 'Type de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { key: 'moyen_controle', label: 'Moyen de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { key: 'code_instrument', label: 'Code instrument', width: 'w-[15%]', textAlign: 'center' },
  { key: 'observations', label: 'Observations', width: 'flex-1' }
];

const modeleColumns = computed(() => {
  let cols = [...baseModeleColumns];
  const customCols = store.entete.configurationColonnes || [];
  
  customCols.forEach(cc => {
    const insertIdx = cols.findIndex(c => c.key === cc.insertAfter);
    const newCol = { key: cc.key, label: cc.label, width: 'w-[12%]', textAlign: 'center', isCustom: true };
    if (insertIdx !== -1) {
      cols.splice(insertIdx + 1, 0, newCol);
    } else {
      cols.push(newCol);
    }
  });

  // Always add the actions column at the end
  cols.push({ key: 'actions', label: '', width: 'w-12', textAlign: 'center' });
  
  return cols;
});

const isLoading = computed(() => store.isLoading);

const isEditMode = computed(() => !!modeleEditionId.value);
const isArchived = computed(() => statut.value === 'ARCHIVE');

// 🔒 NOUVEAU : On verrouille tout si c'est une archive OU si on est en mode aperçu (view)
const isReadOnly = computed(() => (isEditMode.value && isArchived.value && !isArchiveEditing.value && !isUpgradeMode.value) || isForcedView.value);


const codeAffiche = computed(() => {
  if (isEditMode.value && codeOriginal.value) {
    // Si on est en simple lecture (Consultation), on affiche le code actuel
    if (isReadOnly.value) return codeOriginal.value;
    
    // Si on est en train d'éditer pour une nouvelle version, on affiche ce que sera le prochain code
    return `${codeOriginal.value.replace(/(?:[-\s]+V\d+)+$/i, '')}-V${version.value + 1}`;
  }
  return store.entete.code || store.codeModeleAuto;
});

const headerTitle = computed(() => {
  const nature = store.entete.natureComposantCode;
  const famille = store.entete.familleProduitCode;
  const poste = store.entete.posteCode;

  if (isForcedView.value) return 'Consultation du Plan Générique';

  if (nature === 'PISTON') {
    return "Plan en cours d'assemblage PISTON";
  }

  if (nature === 'PF') {
    let title = "Plan en cours d'assemblage PF";
    if (famille) {
      title += ` - ${famille}`;
    }
    // Ajouter le poste si c'est avec soupape et qu'un poste est sélectionné
    const famObject = store.famillesProduit?.find(f => f.code === famille);
    const isSoupape = famObject?.libelle?.toLowerCase().includes('soupape');
    if (isSoupape && poste) {
      title += ` - Poste ${poste}`;
    }
    return title;
  }

  if (nature === 'CORPS' || nature === 'VOLANT') {
    return `Plan d'assemblage ${nature}`;
  }

  if (isEditMode.value) return isArchived.value ? 'Mise à jour d\'Archive' : `Édition du Plan Générique`;
  return 'Création d\'un Plan Générique';
});

const headerSubtitle = computed(() => {
  const nature = store.entete.natureComposantCode;

  if (isForcedView.value) return 'Mode lecture seule (Aperçu de la structure).';

  if (nature === 'PISTON') {
    return 'Configuration générique pour les PISTONS (sans choix de famille).';
  }

  if (nature === 'PF') {
    return 'Configurez le plan selon la famille de produit fini sélectionnée.';
  }

  if (isEditMode.value) {
    return isArchived.value 
      ? 'Vous consultez une archive. Mettre à jour créera une nouvelle version en brouillon.'
      : 'Modifiez la structure. L\'ancienne version sera archivée automatiquement.';
  }
  return 'Configurez la structure des plans du contrôle.';
});

const actionButtonLabel = computed(() => {
  if (isLoading.value) return 'Enregistrement...';
  if (isArchived.value) return 'Mettre à jour ce Plan Générique';
  if (isEditMode.value) return 'Enregistrer les modifications';
  return 'Enregistrer le Plan Générique';
});

const actionButtonIcon = computed(() => {
  if (isArchived.value) return 'pi pi-history';
  if (isEditMode.value) return 'pi pi-save';
  return 'pi pi-check';
});

const actionButtonVariant = computed(() => {
  if (isArchived.value) return 'warning';
  if (isEditMode.value) return 'primary';
  return 'primary';
});

const fileInput = ref(null);

const onFileSelected = async (event) => {
  const file = event.target.files[0];
  if (!file) return;

  event.target.value = '';
  const formData = new FormData();
  formData.append('file', file);

  try {
    store.isLoading = true;
    await store.importerDepuisExcel(file);
    groupes.value = JSON.parse(JSON.stringify(store.sections));
    toast.add({ severity: 'success', summary: 'Import réussi', detail: 'Les données ont été chargées depuis le fichier Excel.', life: 4000 });
    // Réinitialiser le snapshot après l'import pour que isDirty passe à true
    updateCurrentSnapshot(createModeleSnapshot(store.entete, store.sections));
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur d\'import', detail: error.response?.data?.message || 'Impossible de lire le fichier.', life: 4000 });
  } finally {
    store.isLoading = false;
    if (event.target) event.target.value = '';
  }
};

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    await store.fetchFormulairesReferences('EN_COURS_DE_ASSEMBLAGE');
    
    if (route.params.id && route.params.id !== 'nouveau') {
      await chargerModelePourEdition(route.params.id);
    } else {
      resetForNewModele();
      if (groupes.value.length === 0) ajouterGroupe();
    }
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur réseau', detail: error.message, life: 5000 });
  }
});

const chargerModelePourEdition = async (id) => {
  store.isLoading = true;
  store.isBeingLoaded = true;  // ✅ Désactive les watchers en cascade le temps du chargement
  try {
    const res = await assPlanService.getModeleById(id);
    const data = res?.data?.data || res?.data || res;
    
    modeleEditionId.value = data.id;
    codeOriginal.value = data.nom;
    statut.value = data.statut;
    version.value = data.version;
    
    store.entete.code = data.nom;
    store.entete.operationCode = data.operationCode;
    store.entete.natureComposantCode = data.natureArticleCode;
    store.entete.typeRobinetCode = data.libre1;
    store.entete.libelle = data.designation;
    store.entete.notes = data.remarques || '';
    store.entete.legendeMoyens = data.legendeMoyens || '';
    store.entete.posteCode = data.posteCode || '';
    store.entete.familleProduitCode = data.familleProduitFiniCode || '';
    store.entete.refFormulaireCodeReference = data.refFormulaireCodeReference || data.codeReferenceFormulaire || '';
    if (isArchiveEditing.value || isUpgradeMode.value) {
      store.syncConfigurationFromFormulaire();
    } else {
      store.entete.configurationColonnes = (data.colonneDefs || []).map(c => ({
        key: c.cleColonne || c.key,
        label: c.labelAffiche || c.label,
        type: c.typeValeur || c.type || 'Texte',
        insertAfter: c.insertAfter || 'code_instrument'
      }));
    }

    const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
      (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
    );

    groupes.value = sectionsTriees.map(sec => {
      let freqData = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
      if (sec.periodiciteId) {
        const resolved = resolveFrequencyFromPeriodiciteId(sec.periodiciteId, store.periodicites || []);
        if (resolved) {
          freqData = resolved;
        }
      }
      if (freqData.modeFreq === 'SANS') {
        const texteParse = sec.frequenceLibelle || sec.libelleSection || '';
        if (texteParse) {
          freqData = parseFrequenceLibelle(texteParse, store.periodicites || []);
        }
      }
      
      if (sec.regleEchantillonnageId) {
        freqData.modeFreq = 'FIXE';
        freqData.regleEchantillonnageId = sec.regleEchantillonnageId;
      }

      let typeSectionId = sec.typeSectionId || '';
      if (!typeSectionId && sec.libelleSection) {
        const secLib = sec.libelleSection.trim().toLowerCase();
        let bestMatch = null;
        let maxLength = -1;

        store.typesSection.forEach(t => {
          const tLib = (t.libelle || t.nom || '').trim().toLowerCase();
          if (!tLib || secLib === 'section sans nom') return;

          if (secLib.includes(tLib)) {
            if (tLib.length > maxLength) {
              maxLength = tLib.length;
              bestMatch = t;
            }
          }
        });

        if (bestMatch) {
          typeSectionId = bestMatch.id;
        }
      } 

      const lignesTriees = [...(sec.lignes || [])].sort((a, b) =>
        (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
      );

      return {
        id: sec.id,
        isFromDb: true,
        typeSectionId,
        ...freqData,
        isNewFreq: false,
        nom: sec.nom || '',
        libelleSection: sec.libelleSection,
        lignes: lignesTriees.map(lig => ({ 
          id: lig.id,
          isFromDb: true,
          typeCaracteristiqueId: lig.typeCaracteristiqueId,
          libelleAffiche: lig.libelleAffiche || '',
          typeControleId: lig.typeControleId,
          moyenControleId: lig.moyenControleId,
          instrumentCode: lig.instrumentCode,
          instruction: lig.instruction || '',
          estCritique: lig.estCritique,
          unite: lig.unite || '',
          limiteSpecTexte: lig.limiteSpecTexte || '',
          observations: lig.observations || '',
          moyenTexteLibre: lig.moyenTexteLibre || '',
          imageBase64: lig.imageBase64 || null,
          valeursColonnesSpecifiques: lig.extraColonnes ? Object.fromEntries(lig.extraColonnes.map(ec => [ec.cleColonne, ec.valeurColonne])) : {}
        }))
      };
    });
    
    if (!isForcedView.value) {
      setTimeout(() => {
        initializeSnapshot(createModeleSnapshot(store.entete, groupes.value));
      }, 100);
    }

  } catch (e) {
    console.error(e);
    toast.add({ severity: 'error', summary: 'Introuvable', detail: 'Modèle introuvable.', life: 5000 });
    router.push(returnUrl.value);
  } finally {
    store.isLoading = false;
    store.isBeingLoaded = false;  // ✅ Réactive les watchers après le chargement
  }
};



const preparerDonneesEtFrequences = async () => {
  const sections = await prepareModeleDataAndFrequencies(
    groupes.value,
    store.periodicites,
    async (payloadFreq) => {
      const res = await fabPlanService.createPeriodicite(payloadFreq);
      store.periodicites.push({ id: res.data.periodiciteId || res.data.id, ...payloadFreq });
      return res;
    }
  );
  return sections;
};



const sauvegarderDirectement = async () => {
  if (!validerSaisieValeurs()) return;
  if (!validerLegendeMoyens()) return;

  store.isLoading = true;
  try {
    // Vérifier si un modèle actif existe déjà pour ces critères (Nature, Opération, Poste)
    const resExist = await assPlanService.getModelesByFilters(
      null, // typeRobinet (optionnel)
      store.entete.natureComposantCode,
      store.entete.operationCode,
      store.entete.posteCode,
      store.entete.familleProduitCode  // ✅ filtre par famille pour éviter d'archiver BAC01 quand on crée BAC02
    );

    const activeModel = (resExist.data?.data || []).find(m => m.statut === 'ACTIF');

    if (activeModel) {
      // Un modèle actif existe déjà -> On propose d'archiver et créer une nouvelle version
      modeleEditionId.value = activeModel.id;
      version.value = activeModel.version;
      versioningMode.value = 'new-version';
      isAutoVersioning.value = true; // Flag pour bypasser isDirty car c'est un nouveau plan
      showVersioningDialog.value = true;
      store.isLoading = false;
      return;
    }

    store.sections = await preparerDonneesEtFrequences();
    const resData = await store.saveModele(store.entete.legendeMoyens);
    
    toast.add({ severity: 'success', summary: 'Succès', detail: `Modèle (V${resData?.version ?? 0}) créé et activé !`, life: 3000 });
    setTimeout(() => router.push(returnUrl.value), 1500);
  } catch (error) {
    const errorMsg = error.response?.data?.message || error.message;
    toast.add({ severity: 'error', summary: 'Erreur', detail: errorMsg, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const sauvegarderV2 = async (motif) => {
  store.isLoading = true;
  try {
    const sectionsPrepared = await preparerDonneesEtFrequences();
    store.sections = sectionsPrepared;

    const resData = await store.creerNouvelleVersion(modeleEditionId.value, motif, store.entete.legendeMoyens);
    toast.add({ severity: 'success', summary: `V${resData?.version ?? version.value + 1} Activée !`, detail: 'L\'ancienne version a été archivée.', life: 3000 });
    setTimeout(() => router.push(returnUrl.value), 1500);
  } catch (error) {
    const errorMsg = error.response?.data?.message || error.message;
    toast.add({ severity: 'error', summary: 'Erreur', detail: errorMsg, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const restaurerArchive = async (motif) => {
  store.isLoading = true;
  try {
    await restaurerPlan({ documentArchiveId: modeleEditionId.value, motifRestoration: motif });
    toast.add({ severity: 'success', summary: 'Modèle Restauré !', detail: `L'archive a été réactivée en tant que nouvelle version.`, life: 4000 });
    setTimeout(() => router.push(returnUrl.value), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur de restauration', detail: error.message, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const onEditorSubmitClick = () => {
  if (!isEditMode.value) {
    sauvegarderDirectement();
  } else {
    onEditorSubmit();
  }
};

const onEditorSubmit = async () => {
  if (isUpgradeMode.value) {
    versioningMode.value = 'new-version';
    showVersioningDialog.value = true;
  } else if (isArchived.value && !isArchiveEditing.value) {
    isArchiveEditing.value = true;
    
    // On retire 'view' pour sortir du mode consultation forcée
    const newQuery = { ...route.query, draft: 'true' };
    delete newQuery.view;
    
    router.replace({ query: newQuery });
    toast.add({ severity: 'info', summary: 'Mode Édition Activé', detail: 'Modifiez la structure, puis cliquez sur "Enregistrer la Nouvelle Version" en bas.', life: 5000 });
  } else if (isArchived.value || statut.value === 'ACTIF') {
    versioningMode.value = 'new-version';
    showVersioningDialog.value = true;
  } else {
    // Si c'est en BROUILLON, on met directement à jour
    if (!validerSaisieValeurs()) return;
    if (!validerLegendeMoyens()) return;

    try {
      await store.updateModele(modeleEditionId.value, store.entete.legendeMoyens);
      toast.add({ severity: 'success', summary: 'Succès', detail: 'Brouillon mis à jour avec succès', life: 3000 });
      initializeSnapshot(createModeleSnapshot(store.entete, groupes.value));
      setTimeout(() => router.push(returnUrl.value), 1500);
    } catch (error) {
      toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Erreur lors de la mise à jour', life: 6000 });
    }
  }
};

const activerPlanCourant = async () => {
  if (!validerSaisieValeurs()) return;
  if (!validerLegendeMoyens()) return;

  try {
    // Toujours sauvegarder la dernière version du brouillon avant activation
    await store.updateModele(modeleEditionId.value, store.entete.legendeMoyens);
    
    // Ensuite on active le modèle
    await assPlanService.activerModele(modeleEditionId.value);
    
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Le modèle a été activé avec succès (V0 ACTIF).', life: 5000 });
    
    // Mettre à jour l'état local
    statut.value = 'ACTIF';
    initializeSnapshot(createModeleSnapshot(store.entete, groupes.value));
    
    setTimeout(() => router.push(returnUrl.value), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Erreur lors de l\'activation', life: 6000 });
  }
};

const onVersioningConfirm = async (motif) => {
  showVersioningDialog.value = false;
  
  if (versioningMode.value === 'new-version') {
    if (!validerSaisieValeurs()) return;
    if (!validerLegendeMoyens()) return;
    
    // Si c'est un auto-versioning (venant d'une nouvelle création), on bypass le check isDirty
    if (!isAutoVersioning.value && !isDirty.value) {
      toast.add({ severity: 'info', summary: 'Aucune modification', detail: 'Vous n\'avez effectué aucun changement sur la structure du modèle.', life: 4000 });
      return;
    }
    
    await sauvegarderV2(motif);
  } else if (versioningMode.value === 'restore') {
    await restaurerArchive(motif);
  }
  
  isAutoVersioning.value = false; // Reset flag
};

const resetForNewModele = () => {
  modeleEditionId.value = null;
  codeOriginal.value = '';
  statut.value = 'BROUILLON';
  version.value = 0;
  store.entete = { 
    operationCode: '', 
    natureComposantCode: '', 
    typeRobinetCode: '', 
    libelle: '', 
    notes: '', 
    legendeMoyens: '', 
    posteCode: '',
    familleProduitCode: '' 
  };
  groupes.value = [];
  
  // Initialiser le snapshot pour un nouveau modèle (état vide)
  setTimeout(() => {
    initializeSnapshot(createModeleSnapshot(store.entete, groupes.value));
  }, 100);
};
</script>
