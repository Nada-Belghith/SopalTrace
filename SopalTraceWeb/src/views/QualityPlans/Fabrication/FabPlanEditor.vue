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
              <span class="font-bold text-sm text-slate-700">{{ plan?.operationCode || wizard.operationCode.value || 'NON DÉFINIE' }}</span>
            </div>
          </template>
        </PlanHeader>

        <PlanWizardStep v-if="!isEditMode"
                        :wizard="wizard"
                        @load-model="onWizardGenerate"
                        @excel-selected="onExcelSelected" />

        <div v-else class="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
          <div class="bg-[#1e293b] text-white px-5 py-4 flex justify-between items-center">
            <div class="flex items-center gap-3 font-bold tracking-wide text-sm">
              <i :class="isReadOnly ? 'pi pi-eye text-blue-400' : 'pi pi-sliders-v text-blue-400'"></i>
              {{ isReadOnly ? 'Visualisation du plan' : 'Éditeur de Structure du Plan' }}
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

            <!-- Mode LECTURE -->
            <div v-if="isReadOnly" class="p-4 md:p-6">
              <PlanReadView
                :sections="sections"
                :remarques="remarques"
                :legende-moyens="legendeMoyens"
                :types-section="store.typesSection || []"
                :types-caracteristique="store.typesCaracteristique || []"
                :types-controle="store.typesControle || []"
                :moyens-controle="store.moyensControle || []"
                :periodicites="store.periodicites || []"
              />
            </div>

            <!-- Mode EDITION -->
            <FabPlanSectionCard v-else v-for="(section, index) in sections"
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

            <div v-if="!isReadOnly">
              <RemarquesLegendeBox 
                v-model:remarques="remarques"
                v-model:legendeMoyens="legendeMoyens"
                :show-validation="showLegendValidation"
                :has-custom-instruments="hasCustomInstrumentsGlobal"
                :is-read-only="isReadOnly"
              />
            </div>
          </div>

          <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end">
            <!-- Mode CRÉATION ou BROUILLON : 3 boutons (Annuler, Brouillon, Activer) -->
            <template v-if="(planId === 'nouveau' || plan?.statut === 'BROUILLON') && !isForcedView">
              <div class="flex gap-4">
                <button @click="onEditorCancel" class="px-5 py-2.5 text-slate-600 bg-white border border-slate-300 rounded-lg font-bold hover:bg-slate-50 transition-colors">
                  Annuler
                </button>
                <button @click="onSaveDraft"
                        :disabled="isSaving || isVersioningSaving"
                        class="px-5 py-2.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 shadow-sm font-bold">
                  <i v-if="isSaving && !isVersioningSaving" class="pi pi-spin pi-spinner"></i>
                  <i v-else class="pi pi-save"></i>
                  Enregistrer Brouillon
                </button>
                <button @click="onActivatePlan"
                        :disabled="isSaving || isVersioningSaving"
                        class="px-5 py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 shadow-sm font-bold">
                  <i v-if="isSaving && !isVersioningSaving" class="pi pi-spin pi-spinner"></i>
                  <i v-else class="pi pi-check-circle"></i>
                  Enregistrer & Activer le Plan
                </button>
              </div>
            </template>
            <!-- Mode ARCHIVE ou MISE À JOUR ACTIF : Bouton unique via EditorActions (Clonage/Version) -->
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
  import PlanReadView from '@/components/Shared/PlanReadView.vue';
  import EditorActions from '@/components/Shared/EditorActions.vue';
  import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
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
  const { creerNouvelleVersionPlan, restaurerPlan } = usePlanVersioning();

  const wizard = usePlanWizard();
  const isGeneratingPlan = ref(false);

  const isFromWizard = ref(false);
  const planId = ref(route.params.id === 'nouveau' ? null : route.params.id);
  const isForcedView = ref(route.query.view === 'true');
  const plan = ref(null);
  const legendeMoyens = ref('');
  const remarques = ref('');
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
    if (isForcedView.value) return 'Visualisation';
    if (isArchived.value) return "Restauration d'Archive";
    return 'Plan de Fabrication';
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

  const codeAffiche = computed(() => plan.value?.codeArticleSage || wizard.codeArticleSage.value || '');

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
      operationCode: data.operationCode,
      posteCode: wizard.posteCode.value || null
    };
    planCreationPayload.value = {
      modeleSourceId: modeleId,
      codeArticleSage: codeArticle,
      operationCode: data.operationCode,
      posteCode: wizard.posteCode.value || null,
      designation: wizard.designationArticle.value,
      creePar: 'ADMIN_QUALITE'
    };
    sections.value = mapModeleDataToSections(data);
    isFromWizard.value = true;
    planId.value = "nouveau";
    isGeneratingPlan.value = false;
  };

  // 🔥 SURVEILLANCE DES BROUILLONS EN AMONT
  // Dès que le couple Article/Opération est saisi, on vérifie s'il y a un brouillon
  watch([wizard.codeArticleSage, wizard.operationCode, wizard.posteCode], async ([code, op, poste]) => {
    if (code && op && (!wizard.requiertPoste.value || poste)) {
      try {
        const res = await qualityPlansService.verifierEtatPlan(code, null, op, poste);
        const etat = res.data;

        if (etat.hasBrouillon) {
          confirm.require({
            message: `Un brouillon existe déjà pour cet article/opération. Voulez-vous reprendre le brouillon existant ou créer un nouveau plan ? (Note: Créer un nouveau supprimera définitivement le brouillon actuel).`,
            header: 'Brouillon Existant',
            icon: 'pi pi-copy text-amber-500',
            acceptLabel: 'Reprendre le Brouillon',
            rejectLabel: 'Créer un Nouveau',
            acceptClass: 'p-button-warning',
            rejectClass: 'p-button-secondary p-button-outlined',
            accept: async () => {
              planId.value = etat.brouillonId;
              isFromWizard.value = true;
              router.replace(`/dev/fab/plans/editer/${etat.brouillonId}`);
              await chargerPlan(etat.brouillonId);
            },
            reject: async () => {
              try {
                await qualityPlansService.deletePlan(etat.brouillonId);
                toast.add({ severity: 'info', summary: 'Nouveau Plan', detail: 'Ancien brouillon supprimé. Vous pouvez choisir votre méthode.' });
              } catch (err) {
                console.error(err);
              }
            }
          });
        }
      } catch (err) {
        console.error("Erreur check draft amont", err);
      }
    }
  });

  const onWizardGenerate = async () => {
    if (isGeneratingPlan.value) return;
    isGeneratingPlan.value = true; // ⚠️ DÉCLENCHE LE BOUCLIER AUTOSAVE PENDANT L'EXÉCUTION

    try {
      const sourceType = wizard.sourceType.value;
      const modeleId = sourceType === 'MODELE' ? wizard.selectedSourceId.value : null;
      const codeArticle = wizard.codeArticleSage.value;
      const operationCode = wizard.operationCode.value;

      // Unifié: on vérifie l'état peu importe qu'on clone ou utilise un modèle
      const resVal = await qualityPlansService.verifierEtatPlan(codeArticle, modeleId, operationCode, wizard.posteCode.value);
      const etat = resVal.data;

      if (etat.hasBrouillon) {
        // Redondant car géré par le watcher plus haut, mais conservé par sécurité
        return;
      } 
      else if (etat.hasActif) {
        confirm.require({
          message: `Un plan ACTIF (Version ${etat.actifVersion}) existe déjà pour cette opération. Voulez-vous créer un nouveau plan ? Si vous l'activez plus tard, le plan actuel sera automatiquement archivé.`,
          header: 'Plan Actif Existant',
          icon: 'pi pi-exclamation-triangle text-blue-500',
          acceptLabel: 'Confirmer & Créer',
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
    const sourceType = wizard.sourceType.value;
    const sourceId = (sourceType === 'CLONE') ? wizard.selectedSourceId.value : modeleId;

    console.log(`[WIZARD] Génération - Type: ${sourceType}, SourceID: ${sourceId}`);

    try {
      if (sourceType === 'CLONE') {
        if (!sourceId) {
          toast.add({ severity: 'warn', summary: 'Attention', detail: 'Veuillez sélectionner un plan à cloner.', life: 4000 });
          isGeneratingPlan.value = false;
          return;
        }

        // Clonage en MÉMOIRE
        const res = await qualityPlansService.getPlanById(sourceId);
        const data = res.data.data;
        
        if (!data) throw new Error("Plan source introuvable.");

        plan.value = {
          statut: 'BROUILLON',
          codeArticleSage: codeArticle || data.codeArticleSage,
          designation: wizard.designationArticle.value || data.designation,
          version: 1,
          operationCode: data.operationCode,
          posteCode: wizard.posteCode.value || data.posteCode
        };

        // On charge les sections en mode "Clonage" (nettoyage des IDs)
        await chargerPlan(data);
        
        planId.value = 'nouveau';
        isFromWizard.value = true;
        
        // Préparer le payload pour la future création lors du clic Enregistrer
        planCreationPayload.value = {
            codeArticleSage: plan.value.codeArticleSage,
            designation: plan.value.designation,
            operationCode: plan.value.operationCode,
            posteCode: plan.value.posteCode,
            creePar: 'ADMIN_QUALITE',
            sections: sections.value.map((s, idx) => ({
              ordreAffiche: idx + 1,
              libelleSection: s.nom || s.libelleSection || '',
              frequenceLibelle: s.frequenceLibelle || '',
              typeSectionId: (s.typeSectionId && s.typeSectionId !== "") ? s.typeSectionId : null,
              periodiciteId: (s.periodiciteId && s.periodiciteId !== "") ? s.periodiciteId : null,
              regleEchantillonnageId: (s.regleEchantillonnageId && s.regleEchantillonnageId !== "") ? s.regleEchantillonnageId : null,
              lignes: (s.lignes || []).map((l, lIdx) => ({
                ordreAffiche: lIdx + 1,
                typeCaracteristiqueId: (l.typeCaracteristiqueId && l.typeCaracteristiqueId !== "") ? l.typeCaracteristiqueId : null,
                typeControleId: (l.typeControleId && l.typeControleId !== "") ? l.typeControleId : null,
                moyenControleId: (l.moyenControleId && l.moyenControleId !== "") ? l.moyenControleId : null,
                instrumentCode: l.instrumentCode || '',
                moyenTexteLibre: l.moyenTexteLibre || '',
                valeurNominale: l.valeurNominale,
                toleranceSuperieure: l.toleranceSuperieure,
                toleranceInferieure: l.toleranceInferieure,
                unite: l.unite || '',
                limiteSpecTexte: l.limiteSpecTexte || '',
                instruction: l.instruction || '',
                observations: l.observations || '',
                estCritique: l.estCritique || false,
                libelleAffiche: l.libelleAffiche || ''
              }))
            }))
        };

        toast.add({ severity: 'success', summary: 'Succès', detail: 'Structure clonée chargée en mémoire.', life: 3000 });
      } else {
        // Nouveau depuis Modèle
        if (!sourceId) {
          toast.add({ severity: 'warn', summary: 'Attention', detail: 'Veuillez sélectionner un modèle.', life: 4000 });
          isGeneratingPlan.value = false;
          return;
        }
        await preparerNouveauBrouillon(sourceId, codeArticle);
      }
    } catch(err) {
      console.error("[WIZARD ERROR]", err);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de générer la structure : ' + (err.message || 'ID invalide'), life: 4000 });
    } finally {
      isGeneratingPlan.value = false;
    }
  };

  const onExcelSelected = async (event) => {
    const file = event.target?.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    try {
      wizard.isGenerating.value = true;
      const response = await qualityPlansService.importExcel(formData);
      const parsedData = response.data.data;

      if (parsedData) {
        if (parsedData.remarques && parsedData.remarques.trim() !== '') {
          remarques.value = (remarques.value ? remarques.value + '\n' : '') + parsedData.remarques.trim();
        }

        // ✅ Recharger les dictionnaires AVANT le mapping pour que les nouvelles
        // periodicites/règles créées par le backend soient disponibles localement
        await store.fetchDictionnaires();

        if (parsedData.sections) {
          const mappedSections = parsedData.sections.map((sec, idx) => {
            // --- RÉSOLUTION INTELLIGENTE DE LA FRÉQUENCE (même logique que mapModeleDataToSections) ---
            let modeFreq = 'SANS';
            let regleEchantillonnageId = null;
            let periodiciteId = null;
            let freqNum = sec.freqNum || 1;
            let typeVariable = sec.typeVariable || 'HEURE';
            let freqHours = sec.freqHours || 1;

            if (sec.frequenceLibelle) {
              // Priorité 1 : Si le backend a déjà résolu le regleEchantillonnageId, on l'utilise
              if (sec.regleEchantillonnageId) {
                modeFreq = 'FIXE';
                regleEchantillonnageId = sec.regleEchantillonnageId;
              } else {
                // Priorité 2 : Chercher par libellé exact dans la liste locale des RÈGLES
                const regMatch = (store.reglesEchantillonnage || []).find(r => r.libelle === sec.frequenceLibelle);
                if (regMatch) {
                  modeFreq = 'FIXE';
                  regleEchantillonnageId = regMatch.id;
                } else {
                  // Priorité 3 : Parser la fréquence variable
                  modeFreq = 'VARIABLE';
                  const libelle = sec.frequenceLibelle.toLowerCase();

                  if (/100\s*%/.test(libelle) && libelle.includes('pièce')) {
                    // ✅ Cas spécial "100% des pièces/h" = toutes les pièces/heure
                    // L'UI représente cela par freqNum=1, freqHours=1 (convention interne)
                    typeVariable = 'HEURE';
                    freqNum = 1;
                    freqHours = 1;
                  } else if (libelle.includes('pièce') && (libelle.includes('heure') || libelle.includes('/h'))) {
                    typeVariable = 'HEURE';
                    // ✅ .*? permet de gérer "100% des pièces/h" (texte entre nombre et "pièce")
                    const match = libelle.match(/(\d+)(?:%)?.*?(?:pièce|piece).*?\/\s*(?:(\d+)\s*)?(?:heure|h\b)/);
                    if (match) {
                      freqNum = parseInt(match[1]);
                      freqHours = match[2] ? parseInt(match[2]) : 1;
                    } else {
                      const pieceMatch = libelle.match(/(\d+)(?:%)?.*?(?:pièce|piece)/);
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
                    const serieMatch = libelle.match(/série de (\d+) pièces?/);
                    if (serieMatch) freqNum = parseInt(serieMatch[1]);
                  } else if (libelle.includes('lot') || libelle.includes('of')) {
                    typeVariable = 'SERIE';
                    const lotMatch = libelle.match(/(\d+)/);
                    if (lotMatch) freqNum = parseInt(lotMatch[1]);
                  }

                  // ✅ Si aucun chiffre trouvé (règle complexe comme "première et dernière pièce"),
                  // on remet freqNum à 0 pour indiquer que c'est un libellé texte libre
                  if (!freqNum || freqNum === 1 && !sec.freqNum) {
                    // Vérifier si backend a renvoyé freqNum=0 explicitement
                    if (sec.freqNum === 0 || sec.freqNum === undefined) {
                      freqNum = 0;
                    }
                  }
                }
              }
            } else if (sec.modeFreq) {
              modeFreq = sec.modeFreq;
            }

            // --- RÉSOLUTION INTELLIGENTE DE LA NATURE DE SECTION ---
            let typeSectionId = sec.typeSectionId || '';
            if (!typeSectionId && (sec.nom || sec.libelleSection)) {
              let secLib = (sec.nom || sec.libelleSection || '').trim().toLowerCase();
              const cleanLib = secLib.split(' (')[0].replace("caractéristiques à contrôler ", "").trim();
              
              const match = store.typesSection.find(t => {
                const tLib = (t.libelle || '').trim().toLowerCase();
                return tLib === cleanLib;
              });

              if (match) typeSectionId = match.id;
            }

            return {
              id: sec.id || crypto.randomUUID(),
              isFromDb: false,
              ordreAffiche: idx + 1,
              typeSectionId: typeSectionId || null,
              libelleSection: sec.nom || sec.libelleSection || '',
              nom: sec.nom || sec.libelleSection || '',
              notes: sec.notes || '',
              modeFreq,
              frequenceLibelle: sec.frequenceLibelle || '',
              periodiciteId: periodiciteId || null,
              regleEchantillonnageId: regleEchantillonnageId || null,
              freqNum,
              typeVariable,
              freqHours,
              isNewFreq: false,
              lignes: (sec.lignes || []).map((lig, lIdx) => ({
                id: lig.id || crypto.randomUUID(),
                isFromDb: false,
                modeleLigneSourceId: lig.modeleLigneSourceId || null,
                ordreAffiche: lig.ordreAffiche || lIdx + 1,
                typeCaracteristiqueId: lig.typeCaracteristiqueId || null,
                typeControleId: lig.typeControleId || null,
                moyenControleId: lig.moyenControleId || null,
                moyenTexteLibre: lig.moyenTexteLibre || '',
                instrumentCode: lig.instrumentCode || '',
                unite: lig.unite || '',
                limiteSpecTexte: lig.limiteSpecTexte || '',
                instruction: lig.instruction || '',
                observations: lig.observations || '',
                estCritique: lig.estCritique || false,
                libelleAffiche: lig.libelleAffiche || ''
              }))
            };
          });

          sections.value = mappedSections;

          // Mise à jour du wizard pour que le header affiche les bonnes infos
          if (parsedData.codeArticleSage) wizard.codeArticleSage.value = parsedData.codeArticleSage;
          if (parsedData.designation) wizard.designationArticle.value = parsedData.designation;
          if (parsedData.operationCode) wizard.operationCode.value = parsedData.operationCode;

          // Bascule dans l'éditeur comme brouillon en mémoire
          planCreationPayload.value = {
            codeArticleSage: parsedData.codeArticleSage || wizard.codeArticleSage.value,
            designation: parsedData.designation || wizard.designationArticle.value,
            operationCode: parsedData.operationCode || wizard.operationCode.value,
            posteCode: wizard.posteCode.value || null,
            nom: '',
            creePar: 'ADMIN_QUALITE',
            sections: mappedSections.map(s => ({
              ...s,
              typeSectionId: (s.typeSectionId && s.typeSectionId !== "") ? s.typeSectionId : null,
              periodiciteId: (s.periodiciteId && s.periodiciteId !== "") ? s.periodiciteId : null,
              regleEchantillonnageId: (s.regleEchantillonnageId && s.regleEchantillonnageId !== "") ? s.regleEchantillonnageId : null,
              lignes: (s.lignes || []).map(l => ({
                ...l,
                typeCaracteristiqueId: (l.typeCaracteristiqueId && l.typeCaracteristiqueId !== "") ? l.typeCaracteristiqueId : null,
                typeControleId: (l.typeControleId && l.typeControleId !== "") ? l.typeControleId : null,
                moyenControleId: (l.moyenControleId && l.moyenControleId !== "") ? l.moyenControleId : null,
              }))
            }))
          };

          isFromWizard.value = true;
          planId.value = 'nouveau';

          // Initialisation du statut pour l'UI
          plan.value = {
            statut: 'BROUILLON',
            codeArticleSage: parsedData.codeArticleSage || wizard.codeArticleSage.value,
            designation: parsedData.designation || wizard.designationArticle.value,
            operationCode: parsedData.operationCode || wizard.operationCode.value,
            posteCode: wizard.posteCode.value || null,
            version: 1
          };

          toast.add({ severity: 'success', summary: 'Import réussi', detail: 'Fichier Excel chargé en mémoire.', life: 3000 });
        }
      }
    } catch (error) {
      console.error('Erreur import Excel:', error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Erreur lors de la lecture du fichier.', life: 5000 });
    } finally {
      wizard.isGenerating.value = false;
      event.target.value = ''; // Reset file input
    }
  };

  const mapModeleDataToSections = (modeleModel) => {
    return (modeleModel.sections || []).map(sec => {
      let modeFreq = 'SANS';
      let regleEchantillonnageId = null;
      let freqNum = 1;
      let typeVariable = 'HEURE';
      let freqHours = 1;

      if (sec.frequenceLibelle) {
        const regMatch = (store.reglesEchantillonnage || []).find(r => r.libelle === sec.frequenceLibelle);
        if (regMatch) {
          modeFreq = 'FIXE';
          regleEchantillonnageId = regMatch.id;
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
        regleEchantillonnageId,
        freqNum,
        typeVariable,
        freqHours,
        isNewFreq: false,
        frequenceLibelle: sec.frequenceLibelle || '', // Indispensable pour la visualisation
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

  const chargerPlan = async (idOrData) => {
    isLoadingData.value = true;
    try {
      let data;
      const isClone = typeof idOrData === 'object';
      
      if (isClone) {
        data = idOrData;
      } else {
        const res = await qualityPlansService.getPlanById(idOrData);
        data = res.data.data;
      }

      if (!isClone) {
        plan.value = data;
        legendeMoyens.value = data.legendeMoyens || '';
        remarques.value = data.remarques || '';
      }

      const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
        (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
      );

      sections.value = sectionsTriees.map(sec => {
        let modeFreq = 'SANS';
        let regleEchantillonnageId = sec.regleEchantillonnageId || null;
        let freqNum = sec.freqNum || 1;
        let typeVariable = sec.typeVariable || 'HEURE';
        let freqHours = sec.freqHours || 1;

        if (sec.regleEchantillonnageId) {
          modeFreq = 'FIXE';
        } else if (sec.frequenceLibelle) {
          const regMatch = (store.reglesEchantillonnage || []).find(r => r.libelle === sec.frequenceLibelle);
          if (regMatch) {
            modeFreq = 'FIXE';
            regleEchantillonnageId = regMatch.id;
          } else {
            modeFreq = 'VARIABLE';
            const libelle = sec.frequenceLibelle.toLowerCase();

            if ((libelle.includes('pièce') || libelle.includes('p/h')) && (libelle.includes('heure') || libelle.includes(' h'))) {
              typeVariable = 'HEURE';
              const match = libelle.match(/(\d+)\s*(?:pièce|p).*?\/\s*(?:(\d+)\s*)?(?:heure|h\b)/i);
              if (match) {
                freqNum = parseInt(match[1]);
                freqHours = match[2] ? parseInt(match[2]) : 1;
              } else {
                const simpleMatch = libelle.match(/(\d+)\s*pièce/i);
                if (simpleMatch) freqNum = parseInt(simpleMatch[1]);
              }
            } else if (libelle.includes('série') || libelle.includes('serie')) {
              typeVariable = 'SERIE';
              const serieMatch = libelle.match(/(?:série|serie).*?(\d+)/i) || libelle.match(/(\d+)\s*pièce/i);
              if (serieMatch) freqNum = parseInt(serieMatch[1]);
            } else if (libelle.includes('échantillon')) {
              typeVariable = 'ECHANTILLON';
              const echMatch = libelle.match(/(\d+)\s*échantillon/i);
              if (echMatch) freqNum = parseInt(echMatch[1]);
            }
          }
        }

        let typeSectionId = sec.typeSectionId || '';
        let extractedNature = "";
        let originalNom = (sec.nom || sec.libelleSection || '').trim();

        let finalNom = sec.libelleSection || '';

        // 2. Nettoyer le préfixe pour trouver la "Nature"
        let candidateNature = originalNom.replace(/caractéristiques à contrôler/gi, "").trim();

        // 3. Chercher si cette nature existe dans le dictionnaire
        if (!typeSectionId && candidateNature) {
          const secLibLower = candidateNature.toLowerCase();
          const match = (store.typesSection || []).find(t => (t.libelle || '').toLowerCase() === secLibLower);
          if (match) {
            typeSectionId = match.id;
            extractedNature = match.libelle;
          } else {
            // Nature personnalisée (ex: test test)
            extractedNature = candidateNature;
          }
        } else if (typeSectionId) {
          const match = (store.typesSection || []).find(t => t.id === typeSectionId);
          if (match) extractedNature = match.libelle;
        }

        // 4. Titre final (Source de vérité = libelleSection original)
        finalNom = finalNom || (extractedNature ? `Caractéristiques à contrôler ${extractedNature}` : "Section sans nom");

        const lignesTriees = [...(sec.lignes || [])]
          .filter(lig => lig != null)
          .sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0));

        return {
          id: isClone ? crypto.randomUUID() : sec.id,
          isFromDb: !isClone,
          typeSectionId,
          libelleSection: sec.libelleSection || finalNom, // On garde le libellé complet
          modeFreq,
          regleEchantillonnageId,
          freqNum,
          typeVariable,
          freqHours,
          isNewFreq: false,
          frequenceLibelle: sec.frequenceLibelle || '',
          nom: finalNom,
          lignes: lignesTriees.map(lig => ({
            id: isClone ? crypto.randomUUID() : lig.id,
            isFromDb: !isClone,
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

    } catch (err) {
      console.error(err);
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

  const construirePayloadService = (isDraft) => {
    return sections.value.map((originalSection, idx) => {
      // 1. Calcul de la fréquence pour le libellé technique
      let finalFrequenceLibelle = '';
      if (originalSection.modeFreq === 'VARIABLE') {
        const is100 = originalSection.freqNum === 100 && originalSection.typeVariable === 'HEURE';
        if (is100) {
          const p100 = (store.periodicites || []).find(p => p.frequenceNum === 100 || p.code === '100PCT_1H');
          finalFrequenceLibelle = p100 ? p100.libelle : "100% des pièces/h";
        } else {
          finalFrequenceLibelle = originalSection.frequenceLibelle || '';
        }
      } else if (originalSection.periodiciteId) {
        finalFrequenceLibelle = (store.periodicites || []).find(p => p.id === originalSection.periodiciteId)?.libelle || '';
      }

      // 2. Libellé de règle d'échantillonnage
      let regleEchLibelle = '';
      const regleEchId = originalSection.regleEchantillonnageId;
      if (regleEchId) {
        regleEchLibelle = (store.reglesEchantillonnage || []).find(r => r.id === regleEchId)?.libelle || '';
      }

      const typeSectionId = originalSection.typeSectionId;

      return {
        id: originalSection.isFromDb ? normalizeId(originalSection.id) : null,
        modeleSectionId: originalSection.modeleSectionId,
        ordreAffiche: idx + 1,
        typeSectionId: (typeSectionId && typeSectionId !== "") ? typeSectionId : null,
        libelleSection: originalSection.nom || originalSection.libelleSection || 'SECTION SANS NOM',
        notes: originalSection.notes || '',
        frequenceLibelle: finalFrequenceLibelle,
        regleEchantillonnageLibelle: regleEchLibelle,
        periodiciteId: (originalSection.periodiciteId && originalSection.periodiciteId !== "") ? originalSection.periodiciteId : null,
        regleEchantillonnageId: (regleEchId && regleEchId !== "") ? regleEchId : null,
        lignes: (originalSection.lignes || []).map((l, lIdx) => {
          const caractMatch = (store.typesCaracteristique || store.caracteristiques || []).find(c => c.id === l.typeCaracteristiqueId);
          const nomCaract = caractMatch?.libelle || 'Caractéristique sans nom';
          
          const mesurements = sanitizeMesurements(l, isDraft);
          const hasNumeric = mesurements.valeurNominale != null || mesurements.toleranceInferieure != null || mesurements.toleranceSuperieure != null;
          
          return {
            id: l.isFromDb ? normalizeId(l.id) : null,
            modeleLigneSourceId: l.modeleLigneSourceId,
            ordreAffiche: lIdx + 1,
            typeCaracteristiqueId: (l.typeCaracteristiqueId && l.typeCaracteristiqueId !== "") ? l.typeCaracteristiqueId : null,
            typeControleId: (l.typeControleId && l.typeControleId !== "") ? l.typeControleId : null,
            moyenControleId: (l.moyenControleId && l.moyenControleId !== "") ? l.moyenControleId : null,
            moyenTexteLibre: l.moyenTexteLibre || '',
            instrumentCode: l.instrumentCode || '',
            valeurNominale: hasNumeric ? mesurements.valeurNominale : null,
            toleranceSuperieure: hasNumeric ? mesurements.toleranceSuperieure : null,
            toleranceInferieure: hasNumeric ? mesurements.toleranceInferieure : null,
            unite: l.unite || '',
            limiteSpecTexte: !hasNumeric && l.limiteSpecTexte ? String(l.limiteSpecTexte).trim() : '',
            instruction: l.instruction || '',
            observations: l.observations || '',
            estCritique: l.estCritique || false,
            libelleAffiche: (l.libelleAffiche || nomCaract).trim()
          };
        })
      };
    });
  };

  const sauvegarderBrouillonSilencieux = async (afficherToast = false, force = false) => {
    // 🛡️ BLOCAGE DE SÉCURITÉ : Ne pas sauvegarder si on est en train de charger, de générer ou si on quitte
    if (!force && (isLoadingData.value || isGeneratingPlan.value || isCanceling.value || isSaving.value || plan.value?.statut === 'ACTIF' || isArchived.value)) return;

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

      await prepareModeleDataAndFrequencies(sections.value, store.periodicites, async (payload) => {
        const res = await qualityPlansService.createPeriodicite(payload);
        store.periodicites.push({ id: res.data.periodiciteId || res.data.id, ...payload });
        return res;
      });

      const payload = construirePayloadService(true);

      await qualityPlansService.mettreAJourValeurs(currentPlanId, payload, legendeMoyens.value, remarques.value, false, plan.value?.nom, 'Admin');

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

      const payload = construirePayloadService(false);

      await qualityPlansService.mettreAJourValeurs(currentPlanId, payload, legendeMoyens.value, remarques.value, true, plan.value?.nom, 'Admin');

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
      if (!validerSaisieValeurs()) {
        isSaving.value = false;
        return;
      }
      if (!validerLegendeMoyens()) {
        isSaving.value = false;
        return;
      }

      // Vérification finale si un plan actif existe déjà avant d'activer celui-ci
      const codeArticle = plan.value?.codeArticleSage || wizard.codeArticleSage.value;
      const opCode = plan.value?.operationCode || wizard.operationCode.value;
      const pCode = plan.value?.posteCode || wizard.posteCode.value;
      
      const resVal = await qualityPlansService.verifierEtatPlan(codeArticle, null, opCode, pCode);
      const etat = resVal.data;

      if (etat.hasActif && (!plan.value?.id || etat.actifId !== plan.value.id)) {
        confirm.require({
          message: `Attention : Un plan ACTIF (Version ${etat.actifVersion}) existe déjà. L'activation de ce nouveau plan archivera automatiquement l'ancien. Voulez-vous continuer ?`,
          header: 'Confirmation d\'Activation',
          icon: 'pi pi-exclamation-triangle text-amber-500',
          acceptLabel: 'Oui, Archiver & Activer',
          rejectLabel: 'Annuler',
          acceptClass: 'p-button-success',
          accept: async () => {
            await declencherSauvegarde();
          },
          reject: () => {
            isSaving.value = false;
          }
        });
      } else {
        await declencherSauvegarde();
      }
    } catch (error) {
      console.error(error);
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
        versioningMode.value = 'restore';
        showVersioningDialog.value = true;
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

  const restaurerArchive = async (motif) => {
    try {
      await restaurerPlan({
        planArchiveId: planId.value,
        restaurePar: 'ADMIN',
        motifRestoration: motif
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
        await restaurerArchive(motif);
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
