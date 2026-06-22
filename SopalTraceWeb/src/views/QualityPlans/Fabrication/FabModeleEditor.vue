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
        :version="isArchiveEditing ? version + 1 : version"
        :statut="isArchiveEditing ? 'BROUILLON' : statut"
        :is-restoring="isLoading"
        :show-restaurer-btn="!isArchiveEditing"
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
              <FabModeleHeader :is-edit-mode="isEditMode" :is-read-only="isReadOnly" />
            </div>
          </div>

          <div class="mb-4 mt-6 flex items-center justify-between">
            <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest">2. Structure des lignes de contrle</h3>
          </div>

          <template v-if="!hasValidStructure">
            <div class="p-8 text-center bg-amber-50 rounded-lg border border-amber-200 mb-6 flex flex-col items-center justify-center">
              <i class="pi pi-file-excel text-amber-500 text-4xl mb-3"></i>
              <h4 class="text-sm font-bold text-amber-800 mb-1">Structure PRC non définie</h4>
              <p class="text-sm text-amber-700 max-w-lg">Le formulaire sélectionné est à l'état de brouillon. Le Superviseur Qualité doit définir la structure du plan avant que vous puissiez créer des modèles ou des plans par article.</p>
            </div>
          </template>
          
          <template v-else>
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
              :configuration-colonnes="store.effectiveConfigurationColonnes"
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
                  @update-groupe="(updatedSection) => mettreAJourGroupe(index, updatedSection)"
                  @section-type-required="() => toast.add({ severity: 'warn', summary: 'Type de section requis', detail: 'Veuillez définir la nature de la section avant d\'ajouter une ligne.', life: 4000 })"
                >
                <FabLigneControl 
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
          </template>

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
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { useToast } from 'primevue/usetoast';

import { documentService as fabModeleService } from '@/services/documentService';
import { documentService as fabPlanService } from '@/services/documentService';
import { useFabModeleVersioning } from '@/composables/useVersioning';
import { useDirtyChecking } from '@/composables/useDirtyChecking';
import { createModeleSnapshot, prepareModeleDataAndFrequencies } from '@/utils/modelMapper';
import { parseFrequenceLibelle } from '@/utils/frequencyUtils';
import {
  nettoyerNomSection,
  normalizeTypeSectionId,
  resolveSectionDisplayTitle
} from '@/utils/sectionTitleUtils';

import PlanHeader from '@/components/Shared/PlanHeader.vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import PlanReadView from '@/components/Shared/PlanReadView.vue';
import FabModeleHeader from '@/components/Fabrication/FabModeleHeader.vue';
import FabTableHeader from '@/components/Fabrication/FabTableHeader.vue';
import SharedSectionCard from '@/components/Shared/SharedSectionCard.vue';
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

const returnUrl = computed(() => '/dev/fab/modeles');

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
const isArchiveEditing = ref(route.query.draft === 'true');

const { 
  showLegendValidation, 
  hasCustomInstrumentsGlobal, 
  validerLegendeMoyens, 
  validerSaisieValeurs 
} = useEditorValidation(groupes, computed(() => store.entete.legendeMoyens), toast);

const { isDirty, updateCurrentSnapshot, initializeSnapshot } = useDirtyChecking();
const { restaurerModele } = useFabModeleVersioning();

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
// Délégation au store : store.tableColumns est un computed réactif qui lit
// toujours la dernière version ACTIF du formulaire de référence (rôle
// EN_COURS_DE_FABRICATION), indépendamment du codeReference ("PRC" ou autre).
// ============================================================================

// Structure valide si le store a chargé au moins un formulaire actif
// pour le rôle EN_COURS_DE_FABRICATION (pas de dépendance au nom "PRC")
const hasValidStructure = computed(() => {
  const refs = store.formulairesReferences || [];
  if (refs.length === 0) return false;
  return refs.some(r => {
    const s = String(r.statut || r.Statut || '').trim().toUpperCase();
    return s === 'ACTIF';
  });
});

// Colonnes réactives : toujours à jour avec la dernière version du formulaire
const modeleColumns = computed(() => store.tableColumns);

const isLoading = computed(() => store.isLoading);
const isEditMode = computed(() => !!modeleEditionId.value);
const isArchived = computed(() => statut.value === 'ARCHIVE');

// 🔒 NOUVEAU : On verrouille tout si c'est une archive non éditée OU si on est en mode aperçu (view)
const isReadOnly = computed(() => (isEditMode.value && isArchived.value && !isArchiveEditing.value) || isForcedView.value);


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
    return `Plan en cours de fabrication ${nature}`;
  }

  if (isEditMode.value) {
    if (isArchived.value && !isArchiveEditing.value) return 'Mise à jour d\'Archive';
    if (isArchiveEditing.value) return 'Création : Nouvelle Version';
    return `Édition du Plan Générique`;
  }
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
    if (isArchived.value && !isArchiveEditing.value) {
      return 'Vous consultez une archive. Mettre à jour vous permettra de préparer une nouvelle version.';
    }
    if (isArchiveEditing.value) {
      return 'Modifiez les valeurs. L\'ancienne version sera archivée automatiquement lors de la sauvegarde.';
    }
    return 'Modifiez la structure. L\'ancienne version sera archivée automatiquement.';
  }
  return 'Configurez la structure des plans du contrôle.';
});

const actionButtonLabel = computed(() => {
  if (isLoading.value) return 'Enregistrement...';
  if (isArchived.value && !isArchiveEditing.value) return 'Éditer et Mettre à jour ce Plan';
  if (isArchived.value && isArchiveEditing.value) return 'Enregistrer la Nouvelle Version';
  if (!isEditMode.value) return 'Créer et Activer le Modèle';
  return 'Créer Nouvelle Version';
});

const actionButtonIcon = computed(() => {
  if (isArchived.value) return 'pi pi-sync';
  if (isEditMode.value) return 'pi pi-save';
  return 'pi pi-check';
});

const actionButtonVariant = computed(() => {
  if (isArchived.value) return 'warning';
  if (isEditMode.value) return 'primary';
  return 'primary';
});

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    await store.fetchFormulairesReferences('EN_COURS_DE_FABRICATION');
    
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
    store.isLoading = true;
    store.loadingMessage = "Chargement du modèle...";
    const res = await fabModeleService.getModeleById(id);
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
    store.entete.refFormulaireCodeReference = data.refFormulaireCodeReference || data.codeReferenceFormulaire || 'PRC';
    
    if (isArchiveEditing.value) {
      store.syncConfigurationFromFormulaire();
    } else {
      if (data.colonneDefs && data.colonneDefs.length > 0) {
        store.entete.configurationColonnes = data.colonneDefs.map(c => ({
          key: c.cleColonne || c.key,
          label: c.labelAffiche || c.label,
          type: c.typeValeur || c.type || 'Texte',
          insertAfter: c.insertAfter || 'code_instrument'
        }));
      } else {
        store.entete.configurationColonnes = [];
      }
    }

    const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
      (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
    );

    groupes.value = sectionsTriees.map(sec => {
      let freqData = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
      
      const texteParse = sec.frequenceLibelle || sec.libelleSection || '';
      if (texteParse) {
        freqData = parseFrequenceLibelle(texteParse, []);
      }
      
      if (freqData.modeFreq === 'VARIABLE') {
        freqData.periodiciteId = sec.periodiciteId || null;
      } else if (freqData.modeFreq === 'SANS' && sec.periodiciteId) {
        freqData.modeFreq = 'VARIABLE';
        freqData.periodiciteId = sec.periodiciteId;
      }
      
      if (sec.regleEchantillonnageId) {
        freqData.modeFreq = 'FIXE';
        freqData.regleEchantillonnageId = sec.regleEchantillonnageId;
      }

      let typeSectionId = normalizeTypeSectionId(sec.typeSectionId || '', store.typesSection);
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

      const libelleSection = resolveSectionDisplayTitle(
        { typeSectionId, libelleSection: sec.libelleSection, nom: sec.nom },
        store.typesSection
      );
      const nom = nettoyerNomSection(
        sec.libelleSection || libelleSection,
        typeSectionId,
        store.typesSection,
        sec.frequenceLibelle || '',
        sec.regleEchantillonnageLibelle || ''
      );

      const lignesTriees = [...(sec.lignes || [])].sort((a, b) =>
        (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
      );

      return {
        id: sec.id,
        isFromDb: true,
        typeSectionId,
        nom,
        ...freqData,
        isNewFreq: false,
        libelleSection,
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



const sauvegarderDirectement = async () => {
  if (!validerSaisieValeurs()) return;
  if (!validerLegendeMoyens()) return;

  store.isLoading = true;
  try {
    // Vérifier si un modèle actif existe déjà pour ces critères (Nature, Opération, Poste)
    const resExist = await fabModeleService.getModelesByFilters(
      null,
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

    await prepareModeleDataAndFrequencies(
      groupes.value,
      store.periodicites,
      async (payloadFreq) => {
        const res = await fabPlanService.createPeriodicite(payloadFreq);
        store.periodicites.push({ id: res.data.periodiciteId || res.data.id, ...payloadFreq });
        return res;
      }
    );
    store.sections = groupes.value;
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
    // 1. Résoudre/créer les périodicités variables et récupérer les sections enrichies
    await prepareModeleDataAndFrequencies(
      groupes.value,
      store.periodicites,
      async (payloadFreq) => {
        const res = await fabPlanService.createPeriodicite(payloadFreq);
        store.periodicites.push({ id: res.data.periodiciteId || res.data.id, ...payloadFreq });
        return res;
      }
    );
    // Après l'appel, groupes.value a été muté in-place avec les periodiciteId résolus.
    // On les recopie dans store.sections pour que mapPayload() les lise correctement lors de l'envoi.
    store.sections = groupes.value;

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

const onEditorSubmitClick = () => {
  if (!isEditMode.value) {
    sauvegarderDirectement();
  } else {
    onEditorSubmit();
  }
};

const onEditorSubmit = async () => {
  if (isArchived.value && !isArchiveEditing.value) {
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
    // Cas inattendu (BROUILLON) - Normalement les Modèles n'ont pas de brouillon
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Les modèles ne gèrent pas de brouillons. Veuillez recréer le modèle.', life: 6000 });
  }
};

const restaurerArchive = async (motif) => {
  store.isLoading = true;
  try {
    const payloadRestore = { modeleArchiveId: modeleEditionId.value, restaurePar: 'ADMIN_QUALITE', motifRestoration: motif };
    await restaurerModele(payloadRestore);
    toast.add({ severity: 'success', summary: 'Modèle Restauré !', detail: `L'archive a été réactivée en tant que nouvelle version.`, life: 4000 });
    setTimeout(() => router.push(returnUrl.value), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur de restauration', detail: error.message, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const onVersioningConfirm = async (motif) => {
  showVersioningDialog.value = false;
  
  if (versioningMode.value === 'new-version') {
    if (!validerSaisieValeurs()) return;
    if (!validerLegendeMoyens()) return;
    
    // Si c'est un auto-versioning (venant d'une nouvelle création), on bypass le check isDirty
    // Si on vient d'une ARCHIVE (lecture seule), on bypass aussi : l'utilisateur ne peut rien modifier,
    // isDirty sera toujours false mais il faut quand même créer la nouvelle version.
    if (!isAutoVersioning.value && !isArchived.value && !isDirty.value) {
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
  const preservedRef = store.entete.refFormulaireCodeReference;
  const preservedCols = store.entete.configurationColonnes;

  store.entete = { 
    ...store.entete,
    operationCode: '', 
    natureComposantCode: '', 
    typeRobinetCode: '', 
    libelle: '', 
    notes: '', 
    legendeMoyens: '', 
    posteCode: '',
    familleProduitCode: '',
    refFormulaireCodeReference: preservedRef || '',
    configurationColonnes: preservedCols || []
  };
  groupes.value = [];

  store.applyFormulaireConfiguration();
  
  // Initialiser le snapshot pour un nouveau modèle (état vide)
  setTimeout(() => {
    initializeSnapshot(createModeleSnapshot(store.entete, groupes.value));
  }, 100);
};
</script>
