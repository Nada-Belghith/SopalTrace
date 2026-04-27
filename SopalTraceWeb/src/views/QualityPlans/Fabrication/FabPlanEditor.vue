<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
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
          icon="pi pi-file-edit"
          iconColorClass="text-blue-500"
          :is-read-only="isReadOnly"
          :version="plan?.version"
          :statut="plan?.statut"
          :is-restoring="isVersioningSaving"
          @restaurer="onEditorSubmit"
        >
          <template #actions>
            <div v-if="isEditMode" class="flex items-center gap-2 bg-slate-50 px-3 py-1.5 rounded-lg border border-slate-200 ml-4 hidden md:flex">
              <span class="text-[10px] font-black text-slate-400 uppercase">Code Article:</span>
              <span class="font-mono font-bold text-sm text-slate-700">{{ codeAffiche }}</span>
            </div>
            <div v-if="isEditMode" class="flex items-center gap-2 bg-slate-50 px-3 py-1.5 rounded-lg border border-slate-200 ml-2 hidden md:flex">
              <span class="text-[10px] font-black text-slate-400 uppercase">Opération:</span>
              <span class="font-bold text-sm text-slate-700">{{ plan?.operationCode || 'NON DÉFINIE' }}</span>
            </div>
          </template>
        </PlanHeader>

        <PlanWizardStep v-if="!isEditMode"
                        :wizard="wizard"
                        @load-model="onWizardGenerate" />

        <div v-else class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
          <div class="bg-[#1e293b] text-white px-5 py-4 flex justify-between items-center">
            <div class="flex items-center gap-3 font-bold tracking-wide text-sm">
              <i class="pi pi-sliders-v text-blue-400"></i> Éditeur de Structure du Plan
            </div>
          </div>

          <div v-if="isLoadingData" class="py-20 text-center text-blue-500">
            <i class="pi pi-spin pi-spinner text-4xl mb-4"></i>
            <p class="text-xs font-black uppercase tracking-widest">Chargement de l'arbre...</p>
          </div>

          <div v-else class="p-6 md:p-8">
            <div class="mb-4">
              <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest">Structure des lignes de contrôle</h3>
            </div>

            <template v-if="sections.length === 0">
              <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
                Cliquez sur "Créer une nouvelle section" pour commencer.
              </div>
            </template>

            <FabPlanSectionCard v-for="(section, index) in sections"
                    :key="section.id"
                    :section="section"
                    :index="index"
                    :periodicites="store.periodicites"
                    :is-archived="isReadOnly"
                    :operation-code="plan?.operationCode || wizard.operationCode.value"
                    @add-ligne="ajouterLigneASection(index)"
                    @remove="supprimerSection(section.id)"
                    @remove-ligne="(ligneId) => supprimerLigneASection(index, ligneId)"
                    @update:section="(updatedSection) => mettreAJourSection(index, updatedSection)" />

            <div class="mt-2" v-if="!isReadOnly">
              <button @click="ajouterSection" class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
                <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
              </button>
            </div>

            <LegendValidationBox 
              v-model="legendeMoyens"
              :show-validation="showLegendValidation"
              :has-custom-instruments="hasCustomInstrumentsGlobal"
              :is-read-only="isReadOnly"
              :is-forced-view="isForcedView"
            />
          </div>

          <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end">
            <template v-if="isEditMode && plan?.statut === 'BROUILLON' && !isForcedView">
              <div class="flex gap-3">
                <button @click="onSaveDraft"
                        :disabled="isSaving || isVersioningSaving"
                        class="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 shadow-sm">
                  <i v-if="isSaving && !isVersioningSaving" class="pi pi-spin pi-spinner"></i>
                  <i v-else class="pi pi-save"></i>
                  Enregistrer Brouillon
                </button>
                <button @click="onActivatePlan"
                        :disabled="isSaving || isVersioningSaving"
                        class="px-6 py-3 bg-emerald-500 text-white rounded-lg hover:bg-emerald-600 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 shadow-sm font-bold">
                  <i v-if="isSaving && !isVersioningSaving" class="pi pi-spin pi-spinner"></i>
                  <i v-else class="pi pi-check-circle"></i>
                  Activer le Plan
                </button>
              </div>
            </template>
            <template v-else-if="!isForcedView">
              <EditorActions :label="editorLabel"
                             loading-label="Traitement..."
                             :icon="editorIcon"
                             :variant="editorVariant"
                             :is-loading="isSaving && !isVersioningSaving && !showVersioningDialog"
                             @submit="onEditorSubmit"
                             @cancel="onEditorCancel" />
            </template>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted, watch, computed, onUnmounted } from 'vue';
  import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router';
  import { useToast } from 'primevue/usetoast';
  import { useConfirm } from 'primevue/useconfirm';

  import { qualityPlansService } from '@/services/qualityPlansService';
  import { usePlanVersioning } from '@/composables/useVersioning';
  import { usePlanWizard } from '@/composables/usePlanWizard';
  import { useFabModeleStore } from '@/stores/fabModeleStore';
  import { prepareModeleDataAndFrequencies } from '@/utils/modelMapper';

  import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
  import PlanWizardStep from '@/components/QualityPlans/PlanWizardStep.vue';
  import FabPlanSectionCard from '@/components/Fabrication/FabPlanSectionCard.vue';
  import EditorActions from '@/components/Shared/EditorActions.vue';
  import LegendValidationBox from '@/components/Shared/LegendValidationBox.vue';
  import ConfirmDialog from 'primevue/confirmdialog';
  import PlanHeader from '@/components/Shared/PlanHeader.vue';

  import { useEditorSections } from '@/composables/useEditorSections';
  import { useEditorValidation } from '@/composables/useEditorValidation';
  import { usePlanAutosave } from '@/composables/usePlanAutosave';
  const route = useRoute();
  const router = useRouter();
  const toast = useToast();
  const confirm = useConfirm();
  const store = useFabModeleStore();
  const { creerNouvelleVersionPlan, mettreAJourValeurs, restaurerPlan } = usePlanVersioning();

  const wizard = usePlanWizard();
  const isGeneratingPlan = ref(false);

  const isFromWizard = ref(false);
  const planId = ref(route.params.id === 'nouveau' ? null : route.params.id);
  const isForcedView = ref(route.query.view === 'true');
  const plan = ref(null);
  const legendeMoyens = ref('');
  const isLoadingData = ref(false);
  const isVersioningSaving = ref(false);

  const {
    sections,
    ajouterSection,
    supprimerSection,
    mettreAJourSection,
    ajouterLigneASection,
    supprimerLigneASection
    // mettreAJourLigne (inutilisé)
  } = useEditorSections();
  const {
    showLegendValidation,
    hasCustomInstrumentsGlobal,
    validerLegendeMoyens,
    validerSaisiePlan: validerSaisieValeurs
  } = useEditorValidation(sections, legendeMoyens, toast);

  const isEditMode = computed(() => !!planId.value);
  const isArchived = computed(() => plan.value?.statut === 'ARCHIVE');
  const isReadOnly = computed(() => isForcedView.value || isArchived.value);

  const headerTitle = computed(() => {
    if (isForcedView.value) return 'Consultation du Plan';
    if (isFromWizard.value) return "Création d'un plan de Fabrication";
    if (plan.value && plan.value.statut === 'BROUILLON' && (plan.value.version || 0) <= 1) {
      return "Création d'un plan de Fabrication";
    }
    if (!isEditMode.value) return "Création d'un plan de Fabrication";
    if (isArchived.value) return "Restauration d'Archive";
    return 'Édition du Plan de Fabrication';
  });

  const headerSubtitle = computed(() => {
    if (isForcedView.value) return 'Mode lecture seule (Aperçu de la structure).';
    if (isFromWizard.value) return "Configurez la structure du plan de fabrication.";
    if (plan.value && plan.value.statut === 'BROUILLON' && (plan.value.version || 0) <= 1) {
      return "Configurez la structure du plan de fabrication.";
    }
    if (!isEditMode.value) return "Configurez la structure du plan de Fabrication.";
    if (isArchived.value) return "Vous consultez une archive. Restaurer réactivera cette version en production.";
    return "Modifiez la structure. L'ancienne version sera archivée automatiquement.";
  });

  const codeAffiche = computed(() => plan.value?.codeArticleSage || wizard.codeArticleSage || '');

  const editorLabel = computed(() => {
    if (!isEditMode.value) return 'Générer le Plan';
    if (isArchived.value) return 'Restaurer ce Plan';
    if (plan.value?.statut === 'BROUILLON' && (plan.value?.version || 0) <= 1) return 'Enregistrer & Activer le Plan';
    if (plan.value?.statut === 'ACTIF') return 'Créer une Nouvelle Version';
    return 'Enregistrer & Activer le Plan';
  });

  const editorIcon = computed(() => {
    if (!isEditMode.value) return 'pi pi-check';
    if (isArchived.value) return 'pi pi-history';
    if (plan.value?.statut === 'BROUILLON' && (plan.value?.version || 0) <= 1) return 'pi pi-save';
    return plan.value?.statut === 'ACTIF' ? 'pi pi-history' : 'pi pi-save';
  });

  const editorVariant = computed(() => {
    if (!isEditMode.value) return 'success';
    if (isArchived.value) return 'warning';
    if (plan.value?.statut === 'BROUILLON' && (plan.value?.version || 0) <= 1) return 'success';
    if (plan.value?.statut === 'ACTIF') return 'warning';
    return 'success';
  });

  const isExitingEditor = ref(false);
  const isCanceling = ref(false);
  const planCreationPayload = ref(null);
  const aEteCreePendantCetteSession = ref(false);

  const onEditorCancel = () => {
    if (plan.value?.statut !== 'BROUILLON') {
      isExitingEditor.value = true;
      router.push('/dev/hub-plans');
      return;
    }

    confirm.require({
      message: 'Êtes-vous sûr de vouloir abandonner ce travail ? Ce brouillon et toutes ses données seront DÉFINITIVEMENT supprimés.',
      header: 'Supprimer le Brouillon',
      icon: 'pi pi-trash text-red-500',
      acceptLabel: 'Oui, Supprimer',
      rejectLabel: 'Annuler',
      acceptClass: 'p-button-danger',
      accept: async () => {
        isCanceling.value = true;
        isExitingEditor.value = true;

        try {
          if (planId.value && planId.value !== 'nouveau') {
            await qualityPlansService.deletePlan(planId.value);
            toast.add({ severity: 'success', summary: 'Brouillon effacé', detail: 'La base de données a été nettoyée avec succès.', life: 4000 });
          }
        } catch (error) {
          console.error("Erreur lors de la suppression du brouillon", error);
          toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de supprimer le brouillon.', life: 4000 });
        } finally {
          router.push('/dev/hub-plans');
        }
      }
    });
  };

  onBeforeRouteLeave(async () => {
    if (isCanceling.value || isExitingEditor.value) {
      return true;
    }
    await sauvegarderBrouillonSilencieux(true);
    return true;
  });

  const showVersioningDialog = ref(false);
  const versioningMode = ref('new-version');
  // Assure la cohérence des flags de chargement entre l'éditeur et la boîte de versioning

  const { isSaving, startAutoSave, stopAutoSave } = usePlanAutosave(async () => {
    if (plan.value?.statut === 'BROUILLON' || planCreationPayload.value) {
      await sauvegarderBrouillonSilencieux(false);
    }
  }, 30000);

  onMounted(async () => {
    if (!store.isDicosLoaded) await store.fetchDictionnaires();
    if (planId.value && planId.value !== 'nouveau') await chargerPlan(planId.value);
    startAutoSave();
  });

  onUnmounted(() => {
    stopAutoSave();
  });


  const preparerNouveauBrouillon = async (modeleId, codeArticle) => {
    const modRes = await qualityPlansService.getModeleById(modeleId);
    const data = modRes.data.data;
    plan.value = {
      statut: 'BROUILLON',
      codeArticleSage: codeArticle,
      designation: wizard.designationArticle.value,
      version: 1,
      operationCode: data.operationCode
    };
    planCreationPayload.value = {
      modeleSourceId: modeleId,
      codeArticleSage: codeArticle,
      designation: wizard.designationArticle.value,
      nom: '',
      creePar: 'ADMIN_QUALITE'
    };
    sections.value = mapModeleDataToSections(data);
    isFromWizard.value = true;
    planId.value = "nouveau";
    isGeneratingPlan.value = false;
  };

  const onWizardGenerate = async () => {
    if (isGeneratingPlan.value) return;
    isGeneratingPlan.value = true; // ⚠️ DÉCLENCHE LE BOUCLIER AUTOSAVE PENDANT L'EXÉCUTION

    try {
      const sourceType = wizard.sourceType.value;
      const modeleId = sourceType === 'MODELE' ? wizard.selectedSourceId.value : null;
      const codeArticle = wizard.codeArticleSage.value;
      const operationCode = wizard.operationCode.value;

      // Unifié: on vérifie l'état peu importe qu'on clone ou utilise un modèle
      const resVal = await qualityPlansService.verifierEtatPlan(codeArticle, modeleId, operationCode);
      const etat = resVal.data;

      if (etat.hasBrouillon) {
        toast.add({ severity: 'info', summary: 'Reprise', detail: 'Un brouillon existe déjà pour cette opération. Reprise en cours...', life: 4000 });
        planId.value = etat.brouillonId;
        isFromWizard.value = true;
        router.replace(`/dev/fab/plans/editer/${etat.brouillonId}`);
        await chargerPlan(etat.brouillonId);
        isGeneratingPlan.value = false;
      } 
      else if (etat.hasActif) {
        confirm.require({
          message: `Un plan ACTIF (Version ${etat.actifVersion}) tourne actuellement en production pour cette opération. Voulez-vous créer une nouvelle version ? L'ancienne sera archivée après activation de la nouvelle.`,
          header: 'Confirmation',
          icon: 'pi pi-exclamation-circle text-blue-500',
          acceptLabel: 'Oui, Continuer',
          rejectLabel: 'Annuler',
          acceptClass: 'p-button-primary',
          accept: async () => {
            await executerGenerationWizard(modeleId, codeArticle);
          },
          reject: () => {
            isGeneratingPlan.value = false;
          }
        });
      } 
      else {
        // Archivé ou rien => Création Libre
        await executerGenerationWizard(modeleId, codeArticle);
      }
    } catch (error) {
      console.error('Erreur génération:', error);
      isGeneratingPlan.value = false;
    }
  };

  const executerGenerationWizard = async (modeleId, codeArticle) => {
    try {
      if (wizard.sourceType.value === 'MODELE') {
        await preparerNouveauBrouillon(modeleId, codeArticle);
      } else {
        const res = await wizard.genererPlan();
        const newPlanId = res.data.planId;
        toast.add({ severity: 'success', summary: 'Succès', detail: wizard.sourceType.value === 'CLONE' ? 'Plan cloné.' : 'Plan créé.', life: 3000 });
        planId.value = newPlanId;
        router.replace(`/dev/fab/plans/editer/${newPlanId}`);
        await chargerPlan(newPlanId);
        isGeneratingPlan.value = false;
      }
    } catch(err) {
      isGeneratingPlan.value = false;
      throw err;
    }
  };

  const mapModeleDataToSections = (modeleModel) => {
    return (modeleModel.sections || []).map(sec => {
      let modeFreq = 'SANS';
      let periodiciteId = null;
      let freqNum = 1;
      let typeVariable = 'HEURE';
      let freqHours = 1;

      if (sec.frequenceLibelle) {
        const perMatch = store.periodicites.find(p => p.libelle === sec.frequenceLibelle);
        if (perMatch) {
          modeFreq = 'FIXE';
          periodiciteId = perMatch.id;
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
      if (!typeSectionId && sec.libelleSection) {
        const secLib = sec.libelleSection.trim().toLowerCase();
        let bestMatch = null;
        let maxLength = -1;

        store.typesSection.forEach(t => {
          const tLib = (t.libelle || '').trim().toLowerCase();
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

      return {
        id: crypto.randomUUID(),
        isFromDb: false,
        modeleSectionId: sec.id,
        typeSectionId,
        modeFreq,
        periodiciteId,
        freqNum,
        typeVariable,
        freqHours,
        isNewFreq: false,
        nom: sec.libelleSection,
        // ⚠️ BOURCLIER ANTI-CRASH: Filtre les lignes nulles
        lignes: (sec.lignes || []).filter(lig => lig != null).map(lig => ({
          id: crypto.randomUUID(),
          isFromDb: false,
          modeleLigneSourceId: lig.id,
          typeCaracteristiqueId: lig.typeCaracteristiqueId,
          typeControleId: lig.typeControleId,
          moyenControleId: lig.moyenControleId,
          instrumentCode: lig.instrumentCode,
          moyenTexteLibre: lig.moyenTexteLibre || '',
          valeurNominale: lig.valeurNominale ?? null,
          toleranceSuperieure: lig.toleranceSuperieure ?? null,
          toleranceInferieure: lig.toleranceInferieure ?? null,
          unite: lig.unite || '',
          limiteSpecTexte: lig.limiteSpecTexte || '',
          instruction: lig.instruction || '',
          observations: lig.observations || '',
          estCritique: lig.estCritique,
          libelleAffiche: lig.libelleAffiche
        }))
      };
    });
  };

  const chargerPlan = async (id) => {
    isLoadingData.value = true;
    try {
      const res = await qualityPlansService.getPlanById(id);
      const data = res.data.data;
      plan.value = data;
      legendeMoyens.value = data.legendeMoyens || '';

      const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
        (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
      );

      sections.value = sectionsTriees.map(sec => {

        let modeFreq = 'SANS';
        let periodiciteId = null;
        let freqNum = 1;
        let typeVariable = 'HEURE';
        let freqHours = 1;

        if (sec.frequenceLibelle) {
          const perMatch = store.periodicites.find(p => p.libelle === sec.frequenceLibelle);
          if (perMatch) {
            modeFreq = 'FIXE';
            periodiciteId = perMatch.id;
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
        if (!typeSectionId && sec.libelleSection) {
          const secLib = sec.libelleSection.trim().toLowerCase();
          let bestMatch = null;
          let maxLength = -1;

          store.typesSection.forEach(t => {
            const tLib = (t.libelle || '').trim().toLowerCase();
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

        // ⚠️ BOURCLIER ANTI-CRASH: Filtre les lignes nulles
        const lignesTriees = [...(sec.lignes || [])]
          .filter(lig => lig != null)
          .sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0));

        return {
          id: sec.id,
          isFromDb: true,
          typeSectionId,
          modeFreq,
          periodiciteId,
          freqNum,
          typeVariable,
          freqHours,
          isNewFreq: false,
          nom: sec.libelleSection,
          lignes: lignesTriees.map(lig => ({
            id: lig.id,
            isFromDb: true,
            typeCaracteristiqueId: lig.typeCaracteristiqueId,
            typeControleId: lig.typeControleId,
            moyenControleId: lig.moyenControleId,
            instrumentCode: lig.instrumentCode,
            moyenTexteLibre: lig.moyenTexteLibre || '',
            valeurNominale: lig.valeurNominale,
            toleranceSuperieure: lig.toleranceSuperieure,
            toleranceInferieure: lig.toleranceInferieure,
            unite: lig.unite || '',
            limiteSpecTexte: lig.limiteSpecTexte || '',
            instruction: lig.instruction || '',
            observations: lig.observations || '',
            estCritique: lig.estCritique,
            libelleAffiche: lig.libelleAffiche
          }))
        };
      });

    } catch {
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les données.', life: 4000 });
    } finally {
      isLoadingData.value = false;
    }
  };

  const normalizeId = (id) => (typeof id === 'string' && id.length <= 36 ? id : null);

  const syncIdsFromDb = (dbPlanData) => {
    if (!dbPlanData || !dbPlanData.sections) return;

    sections.value.forEach((sec, sIdx) => {
      const dbSec = dbPlanData.sections.find(ds => ds.ordreAffiche === (sIdx + 1));

      if (dbSec) {
        sec.id = dbSec.id;
        sec.isFromDb = true;
        sec.modeleSectionId = dbSec.modeleSectionId;

        (sec.lignes || []).forEach((lig, lIdx) => {
          const dbLig = (dbSec.lignes || []).find(dl => dl.ordreAffiche === (lIdx + 1));

          if (dbLig) {
            lig.id = dbLig.id;
            lig.isFromDb = true;
            lig.modeleLigneSourceId = dbLig.modeleLigneSourceId;
          } else {
            lig.isFromDb = false;
          }
        });
      } else {
        sec.isFromDb = false;
      }
    });
  };

  // ⭐ FONCTION UTILITAIRE
  // Nettoie / prépare les valeurs numériques avant envoi.
  // Pour un brouillon (isDraft = true) on PRESERVE la `valeurNominale` même si les tolérances
  // sont absentes (l'utilisateur peut être en train de saisir). Pour l'activation (isDraft = false)
  // on neutralise la valeur si les tolérances sont incomplètes afin d'éviter des erreurs serveur.
  const sanitizeMesurements = (ligne, isDraft = false) => {
    const hasValeur = ligne.valeurNominale != null && ligne.valeurNominale !== '';
    const hasTolSup = ligne.toleranceSuperieure != null && ligne.toleranceSuperieure !== '';
    const hasTolInf = ligne.toleranceInferieure != null && ligne.toleranceInferieure !== '';

    if (isDraft) {
      return {
        valeurNominale: hasValeur ? ligne.valeurNominale : null,
        toleranceSuperieure: hasTolSup ? ligne.toleranceSuperieure : null,
        toleranceInferieure: hasTolInf ? ligne.toleranceInferieure : null
      };
    }

    // Si valeur nominale est présente MAIS tolérances manquent = neutraliser tout pour activation
    if (hasValeur && (!hasTolSup || !hasTolInf)) {
      return {
        valeurNominale: null,
        toleranceSuperieure: null,
        toleranceInferieure: null
      };
    }

    return {
      valeurNominale: hasValeur ? ligne.valeurNominale : null,
      toleranceSuperieure: hasTolSup ? ligne.toleranceSuperieure : null,
      toleranceInferieure: hasTolInf ? ligne.toleranceInferieure : null
    };
  };

  const sauvegarderBrouillonSilencieux = async (afficherToast = false, force = false) => {
    if (!force && (isGeneratingPlan.value || isCanceling.value || isSaving.value || plan.value?.statut === 'ACTIF' || isArchived.value)) return;

    let currentPlanId = planId.value;

    try {
      if (currentPlanId === 'nouveau' && planCreationPayload.value) {
        const instRes = await qualityPlansService.instantiatePlan(planCreationPayload.value);
        currentPlanId = instRes.data.planId;
        planId.value = currentPlanId;
        aEteCreePendantCetteSession.value = true;
        const newPlanRes = await qualityPlansService.getPlanById(currentPlanId);
        syncIdsFromDb(newPlanRes.data.data);
        planCreationPayload.value = null;
      }

      if (!currentPlanId || currentPlanId === 'nouveau') return;

      const sectionsPreparees = await prepareModeleDataAndFrequencies(sections.value, store.periodicites, async (payload) => {
        const res = await qualityPlansService.createPeriodicite(payload);
        store.periodicites.push({ id: res.data.periodiciteId || res.data.id, ...payload });
        return res;
      });

      const payload = sectionsPreparees.map((s, idx) => {
        const originalSection = sections.value[idx];
        const tsMatch = store.typesSection.find(t => t.id === s.typeSectionId);

        const lignesMappees = (s.lignes || []).map((l, lIdx) => {
          const originalLigne = originalSection.lignes[lIdx];

          // Prépare valeurs numériques en mode brouillon (préserver saisies partielles)
          const mesurements = sanitizeMesurements(l, true);

          // Si tolérances / valeur nominale numériques présentes => PRIORITÉ numérique
          const hasNumeric = mesurements.valeurNominale != null || mesurements.toleranceInferieure != null || mesurements.toleranceSuperieure != null;

          const valeurNominale = hasNumeric ? mesurements.valeurNominale : null;
          const toleranceSuperieure = hasNumeric ? mesurements.toleranceSuperieure : null;
          const toleranceInferieure = hasNumeric ? mesurements.toleranceInferieure : null;

          // Limite texte n'est utilisée QUE si PAS de valeurs numériques (et si l'utilisateur a saisi un texte)
          const limiteSpecTexte = !hasNumeric && l.limiteSpecTexte ? String(l.limiteSpecTexte).trim() : '';

          const caractMatch = (store.typesCaracteristique || store.caracteristiques || []).find(c => c.id === l.typeCaracteristiqueId);
          const nomCaract = caractMatch?.libelle || 'Caractéristique sans nom';

          return {
            id: originalLigne.isFromDb ? normalizeId(originalLigne.id) : null,
            modeleLigneSourceId: l.modeleLigneSourceId,
            ordreAffiche: lIdx + 1,
            typeCaracteristiqueId: l.typeCaracteristiqueId || null,
            typeControleId: l.typeControleId || null,
            moyenControleId: l.moyenControleId || null,
            instrumentCode: l.instrumentCode,
            moyenTexteLibre: l.moyenTexteLibre,
            valeurNominale: valeurNominale,
            toleranceSuperieure: toleranceSuperieure,
            toleranceInferieure: toleranceInferieure,
            unite: l.unite || '',
            limiteSpecTexte: limiteSpecTexte,
            instruction: l.instruction || '',
            observations: l.observations || '',
            estCritique: l.estCritique,
            libelleAffiche: (l.libelleAffiche || nomCaract).trim()
          };
        });

        let finalFrequenceLibelle = '';
        if (originalSection.modeFreq === 'VARIABLE') {
          if (originalSection.typeVariable === 'HEURE') {
            finalFrequenceLibelle = `${originalSection.freqNum} pièce${originalSection.freqNum > 1 ? 's' : ''} / ${originalSection.freqHours} heure${originalSection.freqHours > 1 ? 's' : ''}`;
          } else {
            finalFrequenceLibelle = `série de ${originalSection.freqNum} pièce${originalSection.freqNum > 1 ? 's' : ''}`;
          }
        } else if (s.periodiciteId || originalSection.periodiciteId) {
          finalFrequenceLibelle = store.periodicites.find(p => p.id === (s.periodiciteId || originalSection.periodiciteId))?.libelle || '';
        }

        return {
          id: originalSection.isFromDb ? normalizeId(originalSection.id) : null,
          modeleSectionId: originalSection.modeleSectionId,
          ordreAffiche: idx + 1,
          typeSectionId: s.typeSectionId || originalSection.typeSectionId || null,
          libelleSection: tsMatch ? tsMatch.libelle : (s.libelleSection || originalSection.nom || 'SECTION SANS NOM'),
          frequenceLibelle: finalFrequenceLibelle,
          periodiciteId: s.periodiciteId || originalSection.periodiciteId,
          lignes: lignesMappees
        };
      });

      await mettreAJourValeurs(currentPlanId, payload, legendeMoyens.value, false);

      if (afficherToast) {
        toast.add({ severity: 'info', summary: 'Brouillon enregistré', detail: 'Vos données sont sauvegardées.', life: 3000 });
      }
    } catch (error) {
      console.error("L'auto-save a échoué.", error);
    }
  };

  const declencherSauvegarde = async () => {
    let currentPlanId = planId.value;

    if (currentPlanId === 'nouveau' && planCreationPayload.value) {
      try {
        const instRes = await qualityPlansService.instantiatePlan(planCreationPayload.value);
        currentPlanId = instRes.data.planId;
        planId.value = currentPlanId;
        router.replace(`/dev/fab/plans/editer/${currentPlanId}`);

        const newPlanRes = await qualityPlansService.getPlanById(currentPlanId);
        syncIdsFromDb(newPlanRes.data.data);

        planCreationPayload.value = null;
      } catch (err) {
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible d\'instancier le brouillon', life: 6000 });
        throw err;
      }
    }

    await enregistrerValeurs(currentPlanId, true);
  };

  const enregistrerValeurs = async (currentPlanId, redirectToHub = true) => {
    try {
      const sectionsPreparees = await prepareModeleDataAndFrequencies(
        sections.value,
        store.periodicites || [],
        async (payloadFreq) => {
          const res = await qualityPlansService.createPeriodicite(payloadFreq);
          store.periodicites.push({ id: res.data.periodiciteId || res.data.id, ...payloadFreq });
          return res;
        }
      );

      const payload = sectionsPreparees.map((s, idx) => {
        const originalSection = sections.value[idx];
        const tsMatch = store.typesSection.find(t => t.id === (s.typeSectionId || originalSection.typeSectionId));
        const generatedLibelle = tsMatch ? tsMatch.libelle : (s.libelleSection || originalSection.nom || 'SECTION SANS NOM');

        let finalFrequenceLibelle = '';
        if (originalSection.modeFreq === 'VARIABLE') {
          if (originalSection.typeVariable === 'HEURE') {
            finalFrequenceLibelle = `${originalSection.freqNum} pièce${originalSection.freqNum > 1 ? 's' : ''} / ${originalSection.freqHours} heure${originalSection.freqHours > 1 ? 's' : ''}`;
          } else {
            finalFrequenceLibelle = `série de ${originalSection.freqNum} pièce${originalSection.freqNum > 1 ? 's' : ''}`;
          }
        } else if (s.periodiciteId || originalSection.periodiciteId) {
          finalFrequenceLibelle = store.periodicites.find(p => p.id === (s.periodiciteId || originalSection.periodiciteId))?.libelle || '';
        }

        return {
          id: originalSection.isFromDb ? normalizeId(originalSection.id) : null,
          modeleSectionId: originalSection.modeleSectionId,
          ordreAffiche: idx + 1,
          typeSectionId: s.typeSectionId || originalSection.typeSectionId || null,
          libelleSection: generatedLibelle,
          frequenceLibelle: finalFrequenceLibelle,
          periodiciteId: s.periodiciteId || originalSection.periodiciteId,
          lignes: (s.lignes || []).map((l, lIdx) => {
            const originalLigne = originalSection.lignes[lIdx];
            const caractMatch = (store.typesCaracteristique || store.caracteristiques || []).find(c => c.id === l.typeCaracteristiqueId);
            const nomCaract = caractMatch?.libelle || 'Caractéristique sans nom';

            // Préparer mesures pour activation (isDraft = false -> validation stricte)
            const mesurements = sanitizeMesurements(l, false);

            // PRIORITÉ NUMÉRIQUE : si valeur nominale ou tolérances sont présentes on enregistre les numériques
            const hasNumeric = mesurements.valeurNominale != null || mesurements.toleranceInferieure != null || mesurements.toleranceSuperieure != null;

            const valeurNominale = hasNumeric ? mesurements.valeurNominale : null;
            const toleranceSuperieure = hasNumeric ? mesurements.toleranceSuperieure : null;
            const toleranceInferieure = hasNumeric ? mesurements.toleranceInferieure : null;

            // Sinon on enregistre le texte saisi (s'il existe)
            const limiteSpecTexte = !hasNumeric && l.limiteSpecTexte ? String(l.limiteSpecTexte).trim() : '';

            return {
              id: originalLigne.isFromDb ? normalizeId(originalLigne.id) : null,
              modeleLigneSourceId: l.modeleLigneSourceId,
              ordreAffiche: lIdx + 1,
              typeCaracteristiqueId: l.typeCaracteristiqueId,
              typeControleId: l.typeControleId,
              moyenControleId: l.moyenControleId,
              moyenTexteLibre: l.moyenTexteLibre,
              instrumentCode: l.instrumentCode,
              valeurNominale: valeurNominale,
              toleranceSuperieure: toleranceSuperieure,
              toleranceInferieure: toleranceInferieure,
              unite: l.unite,
              limiteSpecTexte: limiteSpecTexte,
              observations: l.observations,
              instruction: l.instruction,
              estCritique: l.estCritique,
              libelleAffiche: (l.libelleAffiche || nomCaract).trim()
            };
          })
        };
      });

      await mettreAJourValeurs(currentPlanId, payload, legendeMoyens.value, true);

      toast.add({ severity: 'success', summary: 'Plan Activé', detail: 'Le plan est maintenant en production.', life: 4000 });

      if (redirectToHub) {
        isExitingEditor.value = true;
        router.push('/dev/hub-plans');
      } else {
        await chargerPlan(currentPlanId);
      }
    } catch (error) {
      console.error('Erreur sauvegarde:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Une erreur est survenue lors de la sauvegarde.', life: 4000 });
      throw error;
    }
  };

  const onSaveDraft = async () => {
    if (isSaving.value) return;
    isSaving.value = true;

    try {
      // Forcer la sauvegarde manuelle (bypass le flag isSaving interne)
      await sauvegarderBrouillonSilencieux(true, true);
      isExitingEditor.value = true;
      router.push('/dev/hub-plans');
    } catch (error) {
      console.error(error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible d\'enregistrer le brouillon.', life: 4000 });
    } finally {
      isSaving.value = false;
    }
  };

  const onActivatePlan = async () => {
    if (isSaving.value) return;
    isSaving.value = true;

    try {
      if (!validerSaisieValeurs()) return;
      if (!validerLegendeMoyens()) return;

      await declencherSauvegarde();
    } catch (error) {
      console.error(error);
    } finally {
      isSaving.value = false;
    }
  };

  const onEditorSubmit = async () => {
    if (isSaving.value) return;
    isSaving.value = true;

    try {
      if (!isEditMode.value && planId.value !== 'nouveau') {
        await onWizardGenerate();
        isSaving.value = false;
        return;
      }

      if (isArchived.value) {
        await restaurerArchive();
        isSaving.value = false;
        return;
      }

      if (!validerSaisieValeurs()) {
        isSaving.value = false;
        return;
      }
      if (!validerLegendeMoyens()) {
        isSaving.value = false;
        return;
      }

      if (plan.value?.statut === 'ACTIF') {
        versioningMode.value = 'new-version';
        showVersioningDialog.value = true;
        // Ne pas laisser le bouton principal en état "isSaving" pendant que la boîte de versioning est affichée
        // L'utilisateur doit cliquer sur "Publier la Nouvelle Version" dans la boîte pour lancer l'opération.
        isSaving.value = false;
      } else {
        await declencherSauvegarde();
      }
    } catch (error) {
      console.error(error);
    } finally {
      // Si on affiche la boîte de versioning, on laisse le flag `isSaving` à true
      // pour que le composant affiche l'état en cours jusqu'à confirmation.
      if (!showVersioningDialog.value) {
        isSaving.value = false;
      }
    }
  };

  const restaurerArchive = async () => {
    try {
      await restaurerPlan({
        planArchiveId: planId.value,
        restaurePar: 'ADMIN',
        motifRestoration: 'Restauration manuelle depuis l\'éditeur'
      });

      toast.add({ severity: 'success', summary: 'Plan restauré', detail: 'Le plan a été réactivé avec succès.', life: 4000 });
      isExitingEditor.value = true;
      router.push('/dev/hub-plans');
    } catch (error) {
      console.error('Erreur restauration:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Une erreur est survenue lors de la restauration.', life: 4000 });
    }
  };

  const onVersioningConfirm = async (motif) => {
    isVersioningSaving.value = true;
    showVersioningDialog.value = false;

    try {
      if (versioningMode.value === 'new-version') {
        const newVersionPlan = await creerNouvelleVersionPlan({
          ancienId: planId.value,
          modifiePar: 'ADMIN',
          motifModification: motif || 'Modification de la structure du plan'
        });
        const newPlanId = newVersionPlan.data.planId;
        const clonedPlanRes = await qualityPlansService.getPlanById(newPlanId);
        syncIdsFromDb(clonedPlanRes.data.data);
        await enregistrerValeurs(newPlanId, true);
      } else if (versioningMode.value === 'restore') {
        await restaurerArchive();
      }
    } catch (error) {
      console.error('Erreur versioning:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Une erreur est survenue lors de l\'opération de versioning.', life: 4000 });
    } finally {
      isVersioningSaving.value = false;
      // Toujours s'assurer que le flag global de sauvegarde est remis à false
      isSaving.value = false;
    }
  };

  // Si l'utilisateur ferme la boîte de versioning sans confirmer, réinitialiser les flags
  watch(showVersioningDialog, (visible) => {
    if (!visible) {
      isVersioningSaving.value = false;
      isSaving.value = false;
    }
  });
</script>

<style scoped>
  .page-transition {
    transition: opacity 0.5s ease, transform 0.5s ease;
  }

  .fade-enter, .fade-leave-to {
    opacity: 0;
    transform: translateY(10px);
  }

  .zoom-enter, .zoom-leave-to {
    transform: scale(0.95);
  }
</style>
