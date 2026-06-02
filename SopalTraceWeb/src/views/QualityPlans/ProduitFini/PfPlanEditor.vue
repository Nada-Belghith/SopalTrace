<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <Toast position="top-right" />
    <ConfirmDialog />

    <VersioningDialog :visible="showVersioningDialog"
                      :mode="versioningMode"
                      :is-loading="isVersioningSaving"
                      @confirm="onVersioningConfirm"
                      @cancel="showVersioningDialog = false"
                      @update:visible="showVersioningDialog = $event" />

    <div class="max-w-[1600px] mx-auto">
      <div class="animate-in fade-in zoom-in-95 duration-500">

        <PlanHeader 
          :id="planId"
          :title="headerTitle"
          :subtitle="headerSubtitle"
          icon="pi pi-box"
          iconColorClass="text-blue-500"
          :is-read-only="isReadOnly"
          :version="store.entete?.version"
          :statut="statut"
          :is-restoring="isVersioningSaving"
          @restaurer="onEditorSubmit"
        >
          <template #actions>
            <div class="flex items-center gap-2 bg-slate-50 px-3 py-1.5 rounded-lg border border-slate-200 ml-4 hidden md:flex">
              <span class="text-[10px] font-black text-slate-400 uppercase">Identifiant PF:</span>
              <span class="font-mono font-bold text-sm text-slate-700">{{ codeAffiche }}</span>
            </div>
          </template>
        </PlanHeader>

        <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden mb-6">
          <div class="p-6 md:p-8">
            <div class="flex justify-between items-start mb-6">
              <div class="flex-1">
                <PfHeader :is-read-only="isReadOnly" />
              </div>

              <div v-if="!isReadOnly" class="ml-8 shrink-0 flex items-center">
                <input type="file" ref="fileInput" @change="onFileSelected" accept=".xlsx,.csv" class="hidden" />
                <button @click="$refs.fileInput.click()" 
                  class="h-10 px-5 flex items-center gap-3 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition-all shadow-md hover:shadow-emerald-500/20 active:scale-95"
                  :disabled="isLoadingData">
                  <i v-if="!isLoadingData" class="ri-file-excel-2-line text-xl"></i>
                  <i v-else class="ri-loader-4-line animate-spin text-xl"></i>
                  <span>Importer la structure Excel</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
          <div class="bg-[#1e293b] text-white px-5 py-4 flex justify-between items-center">
            <div class="flex items-center gap-3 font-bold tracking-wide text-sm">
              <i :class="isReadOnly ? 'pi pi-eye text-blue-400' : 'pi pi-sliders-v text-blue-400'"></i>
              {{ isReadOnly ? 'Visualisation du plan' : 'Éditeur de Structure' }}
            </div>
            <!-- CONFIGURATION COLONNES -->
            <button v-if="!isReadOnly" @click="showColumnModal = true" class="bg-blue-600 hover:bg-blue-500 text-white px-3 py-1.5 rounded font-bold text-xs flex items-center gap-2 transition-colors">
              <i class="pi pi-sliders-h"></i> Configurer Colonnes
            </button>
          </div>

          <div v-if="isLoadingData" class="py-20 text-center text-blue-500">
            <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
            <p class="text-xs font-black uppercase tracking-widest">Chargement...</p>
          </div>

          <div v-else class="p-6 md:p-8">
            <template v-if="sections.length === 0">
              <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
                Cliquez sur "Créer une nouvelle section" pour commencer.
              </div>
            </template>

            <!-- Mode LECTURE : tableau clair -->
            <div v-if="isReadOnly" class="p-4 md:p-6">
              <PfPlanReadView
                :sections="sections"
                :remarques="store.entete.remarques"
                :legende-moyens="store.entete.legendeMoyens"
              />
            </div>

            <!-- Mode EDITION : ancienne vue cartes -->
            <div v-else class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
              <table class="w-full text-left border-collapse min-w-[1200px]">
                <FabTableHeader :columns="modeleColumns" />
                <PfSectionCard 
                  v-for="(section, index) in sections" 
                  :key="section.id" 
                  :groupe="section" 
                  :index="index"
                  :is-read-only="isReadOnly"
                  @remove="supprimerSection(section.id)"
                  @update-groupe="(updatedSection) => mettreAJourSection(index, updatedSection)"
                  @section-type-required="() => toast.warn('Veuillez définir la nature de la section.', 'Nature requise')"
                >
                  <FabLigneControl 
                    v-for="ligne in section.lignes" 
                    :key="ligne.id" 
                    :ligne="ligne"
                    :columns="modeleColumns"
                    :is-read-only="isReadOnly"
                    :operation-code="'PF'"
                    @remove="(ligneId) => supprimerLigneASection(index, ligneId)"
                    @update="(updatedLigne) => mettreAJourLigne(index, updatedLigne)"
                  />
                </PfSectionCard>
              </table>
            </div>

            <div class="mt-4 flex justify-between" v-if="!isReadOnly">
              <button @click="ajouterSection" class="w-full md:w-auto px-6 py-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
                <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
              </button>
            </div>

            <!-- Notes & Légende en mode éditeur seulement -->
            <div v-if="!isReadOnly" class="px-6 md:px-8 pb-6">
              <RemarquesLegendeBox
                v-model:remarques="store.entete.remarques"
                v-model:legendeMoyens="store.entete.legendeMoyens"
                :show-validation="showLegendValidation"
                :has-custom-instruments="hasCustomInstrumentsGlobal"
                :is-read-only="isReadOnly"
              />
            </div>
          </div>

          <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end" v-if="!isForcedView">
            <EditorActions 
              :label="editorLabel"
              :icon="editorIcon"
              :variant="editorVariant"
              :is-loading="isSaving || isVersioningSaving"
              @submit="onEditorSubmitClick"
              @cancel="onCloseEditor"
            />
          </div>
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
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAppToast } from '@/composables/useAppToast';
import ConfirmDialog from 'primevue/confirmdialog';

import { usePfPlanStore } from '@/stores/pfPlanStore';
import { useEditorSections } from '@/composables/useEditorSections';
import { useEditorValidation } from '@/composables/useEditorValidation';

import PlanHeader from '@/components/Shared/PlanHeader.vue';
import PfHeader from '@/components/ProduitFini/PfHeader.vue';
import PfSectionCard from '@/components/ProduitFini/PfSectionCard.vue';
import PfPlanReadView from '@/components/ProduitFini/PfPlanReadView.vue';
import FabLigneControl from '@/components/Fabrication/FabLigneControl.vue';
import FabTableHeader from '@/components/Fabrication/FabTableHeader.vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import Toast from 'primevue/toast';
import { pfPlanService } from '@/services/pfPlanService';

const route = useRoute();
const router = useRouter();
const toast = useAppToast();
const store = usePfPlanStore();

const planId = ref(route.params.id === 'nouveau' ? null : route.params.id);
const isForcedView = ref(route.query.view === 'true');
const isLoadingData = ref(false);
const isSaving = ref(false);
const isVersioningSaving = ref(false);
const showVersioningDialog = ref(false);
const showColumnModal = ref(false);
const versioningMode = ref('PF');
const fileInput = ref(null);

const {
  sections,
  ajouterSection,
  supprimerSection,
  mettreAJourSection,
  supprimerLigneASection,
  mettreAJourLigne
  // ajouterLigneASection (inutilisé)
} = useEditorSections();

const {
  showLegendValidation,
  hasCustomInstrumentsGlobal,
  validerLegendeMoyens,
  validerSaisiePlan: validerSaisieValeurs
} = useEditorValidation(sections, computed(() => store.entete.legendeMoyens), toast);

const onFileSelected = async (event) => {
  const file = event.target.files[0];
  if (!file) return;

  const formData = new FormData();
  formData.append('file', file);

  try {
    isLoadingData.value = true;
    const response = await pfPlanService.importExcel(formData);
    const parsedData = response.data.data;
    
    if (parsedData) {
      if (parsedData.remarques && parsedData.remarques.trim() !== '') {
        store.entete.remarques = (store.entete.remarques ? store.entete.remarques + '\n' : '') + parsedData.remarques.trim();
      }

      if (parsedData.sections) {
        sections.value = parsedData.sections.map(sec => {
          let modeFreq = sec.modeFreq || 'SANS';
          let regleEchantillonnageId = sec.regleEchantillonnageId || null;
          let freqNum = sec.freqNum || 1;
          let typeVariable = sec.typeVariable || 'HEURE';
          let freqHours = sec.freqHours || 1;

          // LOGIQUE IDENTIQUE AU PLAN FABRICATION/PISTON
          if (sec.frequenceLibelle) {
            const perMatch = (store.reglesEchantillonnage || []).find(p => p.libelle === sec.frequenceLibelle);
            if (perMatch) {
              modeFreq = 'FIXE';
              regleEchantillonnageId = perMatch.id;
            } else {
              modeFreq = 'VARIABLE';
              const libelle = sec.frequenceLibelle.toLowerCase();

              if (libelle.includes('pièce') && libelle.includes('heure')) {
                typeVariable = 'HEURE';
                const match = libelle.match(/(\d+)\s*pièce.*\/\s*(\d+)\s*heure/);
                if (match) {
                  freqNum = parseInt(match[1]);
                  freqHours = parseInt(match[2]);
                } else {
                  const pieceMatch = libelle.match(/(\d+)\s*pièce/);
                  if (pieceMatch) {
                    freqNum = parseInt(pieceMatch[1]);
                    freqHours = 1;
                  }
                }
              } else if (libelle.includes('échantillon')) {
                 typeVariable = 'ECHANTILLON';
                 const match = libelle.match(/(\d+)\s*échantillon/);
                 if (match) freqNum = parseInt(match[1]);
              } else if (libelle.includes('série')) {
                typeVariable = 'SERIE';
                const serieMatch = libelle.match(/série de (\d+) pièces/);
                if (serieMatch) {
                  freqNum = parseInt(serieMatch[1]);
                }
              }
            }
          }

          let typeSectionId = sec.typeSectionId || '';

          return {
            id: sec.id || crypto.randomUUID(),
            isFromDb: false,
            nom: sec.nom || '',  // Texte brut Excel (nature personnalisée si typeSectionId est null)
            libelleSection: sec.nom,
            typeSectionId,  // null/vide si non trouvé en base
            notes: sec.notes || '',
            regleEchantillonnageId,
            regleEchantillonnageLibelle: sec.frequenceLibelle,
            modeFreq,
            freqNum,
            typeVariable,
            freqHours,
            lignes: (sec.lignes || []).map(lig => ({
              id: lig.id || crypto.randomUUID(),
              isFromDb: false,
              typeCaracteristiqueId: lig.typeCaracteristiqueId,
              typeControleId: lig.typeControleId,
              moyenControleId: lig.moyenControleId,
              instrumentCode: lig.instrumentCode,
              unite: lig.unite || '',
              limiteSpecTexte: lig.limiteSpecTexte,
              observations: lig.observations,
              estCritique: lig.estCritique,
              libelleAffiche: lig.libelleAffiche
            }))
          };
        });
      }
      
      // On recharge les dictionnaires pour s'assurer que les nouvelles caractéristiques créées par le backend soient disponibles dans les select.
      await store.fetchDictionnaires();

      toast.success('Les données ont été chargées depuis le fichier Excel.', 'Import réussi');
    }
  } catch (error) {
    toast.error(error.response?.data?.message || 'Impossible de lire le fichier.', 'Erreur d\'import');
  } finally {
    isLoadingData.value = false;
    if (fileInput.value) fileInput.value.value = '';
  }
};

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

const isEditMode = computed(() => !!planId.value);
const isArchived = computed(() => store.entete?.statut === 'ARCHIVE');
const isReadOnly = computed(() => isForcedView.value || isArchived.value);

const statut = computed(() => {
  if (!isEditMode.value) return 'BROUILLON';
  return store.entete?.statut || 'ACTIF';
});

const codeAffiche = computed(() => {
  const v = isEditMode.value ? (store.entete.version + 1) : (store.entete?.version || 1);
  
  if (store.entete?.familleProduitFiniCode) {
    return `${store.entete.familleProduitFiniCode}-PF-V${v}`;
  }
  
  return `Nouveau-PF-V${v}`;
});

const headerTitle = computed(() => {
  return "Plan de contrôle de produit fini";
});

const headerSubtitle = computed(() => {
  if (isForcedView.value) return 'Mode lecture seule.';
  if (!isEditMode.value) return "Sélectionnez une famille de produit et configurez les sections de contrôle.";
  if (isArchived.value) return "Vous consultez une archive.";
  return "Modifiez la structure. Ce plan est générique par famille de produit fini.";
});

const editorLabel = computed(() => {
  if (isArchived.value) return 'Restaurer ce Plan';
  if (isEditMode.value) return 'Créer une Nouvelle Version';
  return 'Enregistrer le Plan';
});

const editorIcon = computed(() => {
  if (isArchived.value) return 'pi pi-history';
  if (isEditMode.value) return 'pi pi-save';
  return 'pi pi-check';
});

const editorVariant = computed(() => {
  if (isArchived.value) return 'warning';
  if (isEditMode.value) return 'primary';
  return 'primary';
});

onMounted(async () => {
  if (!store.isDicosLoaded) {
    await store.fetchDictionnaires();
  }

  if (planId.value) {
    isLoadingData.value = true;
    try {
      await store.getPlan(planId.value);
      // Synchroniser le state local des sections
      sections.value = JSON.parse(JSON.stringify(store.sections));
    } catch (error) {
      console.error("Erreur chargement plan PF :", error);
      toast.error('Impossible de charger le plan PF');
      router.push('/dev/hub');
    } finally {
      isLoadingData.value = false;
    }
  } else {
    // Mode Nouveau
    store.entete = {
      familleProduitFiniCode: '',
      familleProduitFiniLibelle: '',
      commentaireVersion: '',
      version: 1,
      statut: 'ACTIF'
    };
    sections.value = [];
    ajouterSection();
    
    // Fallback PF : On pré-sélectionne "Contrôle Produit Fini" pour la première section
    if (sections.value.length > 0) {
      const fallback = (store.typesSection || []).find(t => {
        const lib = (t.libelle || '').toLowerCase();
        return lib.includes('produit fini') || lib.includes('contrôle final');
      });
      if (fallback) sections.value[0].typeSectionId = fallback.id;
    }
  }
});

const onCloseEditor = () => {
  router.push('/dev/hub');
};

const sauvegarderDirectement = async () => {
  if (!store.entete.familleProduitFiniCode) {
    toast.warn('La famille de produit est obligatoire.', 'Champs requis');
    return;
  }
  if (!validerSaisieValeurs()) return;

  isSaving.value = true;
  try {
    store.sections = sections.value;
    await store.createPlan();
    toast.success('Plan créé et activé.');
    router.push('/dev/hub');
  } catch (error) {
    toast.error(error.response?.data?.message || 'Erreur lors de la sauvegarde.');
  } finally {
    isSaving.value = false;
  }
};

const onEditorSubmitClick = async () => {
  if (!isEditMode.value) {
    await sauvegarderDirectement();
  } else {
    await onEditorSubmit();
  }
};

const onEditorSubmit = async () => {
  if (isArchived.value) {
    showVersioningDialog.value = true;
    return;
  }
  
  if (isEditMode.value) {
    if (!validerSaisieValeurs()) return;
    if (!validerLegendeMoyens()) return;
    if (statut.value === 'ACTIF') {
       versioningMode.value = 'new-version';
       showVersioningDialog.value = true;
       return;
    }
  }
};

const onVersioningConfirm = async (motif) => {
  showVersioningDialog.value = false;
  isVersioningSaving.value = true;
  try {
    store.sections = sections.value;
    
    if (isArchived.value) {
      // Cas Restauration : on appelle l'endpoint dédié
      await store.restaurerPlan(motif);
      toast.success('Version restaurée et activée.');
    } else {
      // Cas Nouvelle Version (depuis un plan Actif)
      await store.creerNouvelleVersion(motif);
      toast.success('Nouvelle version activée.');
    }
    
    router.push(`/dev/hub`);

  } catch (error) {
    const action = isArchived.value ? 'la restauration' : 'le versioning';
    toast.error(error.response?.data?.message || `Erreur lors de ${action}.`);
  } finally {
    isVersioningSaving.value = false;
  }
};
</script>
