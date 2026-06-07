<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <ConfirmDialog />
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
          <button @click="$router.push('/dev/hub')" class="text-slate-400 hover:text-white transition-colors">
            <i class="pi pi-times text-lg"></i>
          </button>
        </div>

        <div class="p-6 md:p-8">
          <div class="flex justify-between items-start mb-6">
            <div class="flex-1">
              <FabModeleHeader :is-edit-mode="isEditMode" :is-read-only="isReadOnly" />
            </div>

            <div v-if="!isReadOnly && (store.entete.natureComposantCode === 'PISTON' || (store.entete.operationCode === 'ASS' && store.entete.natureComposantCode === 'PF'))" class="ml-8 shrink-0 flex items-center">
              <input type="file" ref="fileInput" @change="onFileSelected" accept=".xlsx,.csv" class="hidden" />
              <button @click="$refs.fileInput.click()" 
                class="h-10 px-5 flex items-center gap-3 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition-all shadow-md hover:shadow-emerald-500/20 active:scale-95"
                :disabled="isLoading">
                <i v-if="!isLoading" class="ri-file-excel-2-line text-xl"></i>
                <i v-else class="ri-loader-4-line animate-spin text-xl"></i>
                <span>Importer la structure Excel</span>
              </button>
            </div>
            
            <div v-if="!isReadOnly" class="ml-4 shrink-0 flex items-center">
              <button @click="showColumnModal = true" class="bg-slate-800 hover:bg-slate-700 text-white px-4 py-2 rounded-lg font-bold text-xs flex items-center gap-2 transition-colors h-10 shadow-md">
                <i class="pi pi-sliders-h"></i> Configurer Colonnes
              </button>
            </div>
          </div>

          <div class="mb-4 flex items-center justify-between">
            <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest">2. Structure des lignes de contrle</h3>
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
              <FabTableHeader :columns="modeleColumns" />
              
              <FabSectionCard 
                v-for="(groupe, index) in groupes" 
                :key="groupe.id" 
                :groupe="groupe" 
                :index="index"
                :is-read-only="isReadOnly"
                @remove="supprimerGroupe(groupe.id)"
                @update-groupe="(updatedGroupe) => mettreAJourGroupe(index, updatedGroupe)"
                @section-type-required="() => toast.add({ severity: 'warn', summary: 'Type de section requis', detail: 'Veuillez définir la nature de la section avant d\'ajouter une ligne.', life: 4000 })"
              >
                <FabLigneControl 
                  v-for="ligne in groupe.lignes" 
                  :key="ligne.id" 
                  :ligne="ligne"
                  :columns="modeleColumns"
                  :is-read-only="isReadOnly"
                  :operation-code="store.entete?.operationCode"
                  @remove="(ligneId) => supprimerLigneASection(index, ligneId)"
                  @update="(updatedLigne) => mettreAJourLigne(index, updatedLigne)"
                />
              </FabSectionCard>
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
            @cancel="() => $router.push('/dev/hub')"
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
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { useToast } from 'primevue/usetoast';

import { qualityPlansService } from '@/services/qualityPlansService';
import { useModeleVersioning } from '@/composables/useVersioning';
import { useDirtyChecking } from '@/composables/useDirtyChecking';
import { createModeleSnapshot, prepareModeleDataAndFrequencies } from '@/utils/modelMapper';
import { parseFrequenceLibelle } from '@/utils/frequencyUtils';

import PlanHeader from '@/components/Shared/PlanHeader.vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import PlanReadView from '@/components/Shared/PlanReadView.vue';
import FabModeleHeader from '@/components/Fabrication/FabModeleHeader.vue';
import FabTableHeader from '@/components/Fabrication/FabTableHeader.vue';
import FabSectionCard from '@/components/Fabrication/FabSectionCard.vue';
import FabLigneControl from '@/components/Fabrication/FabLigneControl.vue'; 
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import ConfirmDialog from 'primevue/confirmdialog';

import { useEditorSections } from '@/composables/useEditorSections';
import { useEditorValidation } from '@/composables/useEditorValidation';

const store = useFabModeleStore();
const toast = useToast();
const route = useRoute();
const router = useRouter();

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
const versioningMode = ref('FAB');
const isAutoVersioning = ref(false);

const { 
  showLegendValidation, 
  hasCustomInstrumentsGlobal, 
  validerLegendeMoyens, 
  validerSaisieValeurs 
} = useEditorValidation(groupes, computed(() => store.entete.legendeMoyens), toast);

const { isDirty, updateCurrentSnapshot, initializeSnapshot } = useDirtyChecking();
const { restaurerModele } = useModeleVersioning();

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
const isReadOnly = computed(() => (isEditMode.value && isArchived.value) || isForcedView.value);


const codeAffiche = computed(() => {
  if (isEditMode.value && codeOriginal.value) {
    // Si on est en simple lecture (Consultation), on affiche le code actuel
    if (isReadOnly.value) return codeOriginal.value;
    
    // Si on est en train d'éditer pour une nouvelle version, on affiche ce que sera le prochain code
    return `${codeOriginal.value.replace(/-V\d+$/i, '')}-V${version.value + 1}`;
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
    return `Plan en cours de fabrication ${nature}`;
  }

  if (isEditMode.value) return isArchived.value ? 'Restauration d\'Archive' : `Édition du Plan Générique`;
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
      ? 'Vous consultez une archive. Enregistrer restaurera cette version en production.'
      : 'Modifiez la structure. L\'ancienne version sera archivée automatiquement.';
  }
  return 'Configurez la structure des plans du contrôle.';
});

const actionButtonLabel = computed(() => {
  if (isLoading.value) return 'Enregistrement...';
  if (isArchived.value) return 'Restaurer ce Plan Générique';
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

  const formData = new FormData();
  formData.append('file', file);
  if (store.entete.configurationColonnes) {
    const configJson = typeof store.entete.configurationColonnes === 'string'
      ? store.entete.configurationColonnes
      : JSON.stringify(store.entete.configurationColonnes);
    formData.append('configurationColonnesJson', configJson);
  }

  try {
    store.isLoading = true;
    const response = await qualityPlansService.importExcel(formData);
    const parsedData = response.data.data;

    if (parsedData) {
      // Ajouter les remarques du fichier
      if (parsedData.remarques && parsedData.remarques.trim() !== '') {
        store.entete.notes = (store.entete.notes ? store.entete.notes + '\n' : '') + parsedData.remarques.trim();
      }

      // Ajouter les sections
      if (parsedData.sections && parsedData.sections.length > 0) {
        // Afficher un aperçu avant d'importer
        const sectionsApercu = parsedData.sections
          .filter(sec => sec && sec.nom) // Sections valides uniquement
          .map(sec => {
            const hasLines = sec.lignes && sec.lignes.length > 0;

            return {
              id: sec.id || crypto.randomUUID(),
              isFromDb: false,
              nom: sec.nom || '',  // Texte brut Excel (nature personnalisée si typeSectionId est null)
              libelleSection: sec.nom,
              typeSectionId: sec.typeSectionId || null,  // null si non trouvé en base
              notes: sec.notes || '',
              modeFreq: sec.modeFreq || 'SANS',
              periodiciteId: sec.periodiciteId || null,
              freqNum: sec.freqNum || 0,
              typeVariable: sec.typeVariable || '',
              freqHours: sec.freqHours || 1,
              regleEchantillonnageId: sec.regleEchantillonnageId || null,  // ✅ NULL si pas trouvé
              frequenceLibelle: sec.frequenceLibelle || null,
              lignes: hasLines 
                ? sec.lignes.map(lig => ({
                    id: lig.id || crypto.randomUUID(),
                    isFromDb: false,
                    typeCaracteristiqueId: lig.typeCaracteristiqueId || null,
                    typeControleId: lig.typeControleId || null,
                    moyenControleId: lig.moyenControleId || null,
                    instrumentCode: lig.instrumentCode || '',
                    unite: lig.unite || '',
                    limiteSpecTexte: lig.limiteSpecTexte || '',
                    observations: lig.observations || '',
                    instruction: lig.instruction || '',
                    estCritique: lig.estCritique || false,
                    libelleAffiche: lig.libelleAffiche || '',
                    valeursColonnesSpecifiques: lig.colonnesSupplementaires ? (typeof lig.colonnesSupplementaires === 'string' ? JSON.parse(lig.colonnesSupplementaires) : lig.colonnesSupplementaires) : (lig.valeursColonnesSpecifiques || {})
                  }))
                : [] // ✅ Sections sans lignes (complexes) restent vides pour édition
            };
          });

        // Remplacer les groupes par les sections importées
        groupes.value = sectionsApercu;

        // Afficher détails de l'import
        const totalSections = sectionsApercu.length;
        const sectionsAvecLignes = sectionsApercu.filter(s => s.lignes.length > 0).length;
        const sectionsSansLignes = totalSections - sectionsAvecLignes;

        let detailMessage = `${totalSections} section(s) importée(s)`;
        if (sectionsSansLignes > 0) {
          detailMessage += ` (${sectionsAvecLignes} avec lignes, ${sectionsSansLignes} pour aperçu)`;
        }

        toast.add({
          severity: 'success',
          summary: 'Import réussi',
          detail: detailMessage,
          life: 5000
        });
      } else {
        toast.add({
          severity: 'warn',
          summary: 'Import terminé',
          detail: 'Aucune section n\'a été trouvée dans le fichier.',
          life: 4000
        });
      }

      await store.fetchDictionnaires();
    }
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Erreur d\'import',
      detail: error.response?.data?.message || 'Impossible de lire le fichier.',
      life: 4000
    });
  } finally {
    store.isLoading = false;
    if (fileInput.value) fileInput.value.value = '';
    
    // Réinitialiser le snapshot après l'import pour que isDirty passe à true
    updateCurrentSnapshot(createModeleSnapshot(store.entete, groupes.value));
  }
};

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    if (route.query.mode === 'assembly') {
      await store.fetchFormulairesReferences('EN_COURS_DE_ASSEMBLAGE');
    }
    
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
    const res = await qualityPlansService.getModeleById(id);
    const data = res.data.data || res.data;
    
    modeleEditionId.value = data.id;
    codeOriginal.value = data.code;
    statut.value = data.statut;
    version.value = data.version;
    
    store.entete.code = data.code;
    store.entete.operationCode = data.operationCode;
    store.entete.natureComposantCode = data.natureComposantCode;
    store.entete.typeRobinetCode = data.typeRobinetCode;
    store.entete.libelle = data.libelle;
    store.entete.notes = data.notes || '';
    store.entete.legendeMoyens = data.legendeMoyens || '';
    store.entete.posteCode = data.posteCode || '';
    store.entete.familleProduitCode = data.familleProduitCode || '';
    store.entete.configurationColonnes = data.configurationColonnesJson ? (typeof data.configurationColonnesJson === 'string' ? JSON.parse(data.configurationColonnesJson) : data.configurationColonnesJson) : [];

    const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
      (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
    );

    groupes.value = sectionsTriees.map(sec => {
      let freqData = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
      
      if (sec.frequenceLibelle) {
        freqData = parseFrequenceLibelle(sec.frequenceLibelle, store.periodicites);
        // FIX: Toujours forcer FIXE si on a une règle d'échantillonnage
        if (sec.regleEchantillonnageId) {
          freqData.modeFreq = 'FIXE';
          freqData.regleEchantillonnageId = sec.regleEchantillonnageId;
        }
      } else if (sec.periodiciteId || sec.regleEchantillonnageId) {
        freqData.modeFreq = 'FIXE';
        if (sec.periodiciteId) freqData.periodiciteId = sec.periodiciteId;
        if (sec.regleEchantillonnageId) freqData.regleEchantillonnageId = sec.regleEchantillonnageId;
      }

      let typeSectionId = '';
      if (sec.libelleSection) {
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
          valeursColonnesSpecifiques: lig.colonnesSupplementaires ? (typeof lig.colonnesSupplementaires === 'string' ? JSON.parse(lig.colonnesSupplementaires) : lig.colonnesSupplementaires) : {}
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
    router.push('/dev/hub');
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
      const res = await qualityPlansService.createPeriodicite(payloadFreq);
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
    const resExist = await qualityPlansService.getModelesByFilters(
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
    setTimeout(() => router.push('/dev/hub'), 1500);
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
    setTimeout(() => router.push('/dev/hub'), 1500);
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
    const payloadRestore = { modeleArchiveId: modeleEditionId.value, restaurePar: 'ADMIN_QUALITE', motifRestoration: motif };
    await restaurerModele(payloadRestore);
    toast.add({ severity: 'success', summary: 'Modèle Restauré !', detail: `L'archive a été réactivée en tant que nouvelle version.`, life: 4000 });
    setTimeout(() => router.push('/dev/hub'), 1500);
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
  if (isArchived.value) {
    versioningMode.value = 'restore';
    showVersioningDialog.value = true;
  } else if (statut.value === 'ACTIF') {
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
    await store.activerModeleDraft(modeleEditionId.value);
    
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Le modèle a été activé avec succès (V0 ACTIF).', life: 5000 });
    
    // Mettre à jour l'état local
    statut.value = 'ACTIF';
    initializeSnapshot(createModeleSnapshot(store.entete, groupes.value));
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
