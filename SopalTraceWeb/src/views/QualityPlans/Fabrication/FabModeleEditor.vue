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
            <span class="text-[10px] font-black text-slate-400 uppercase">Code Modèle:</span>
            <span class="font-mono font-bold text-sm text-slate-700">{{ codeAffiche }}</span>
          </div>
        </template>
      </PlanHeader>
      
      <div class="bg-white rounded-xl shadow-xl border border-slate-200 overflow-hidden">
        <div class="bg-[#1e293b] text-white px-5 py-3.5 flex justify-between items-center">
          <div class="flex items-center gap-3 font-bold tracking-wide">
            <i class="pi pi-book text-lg"></i> Éditeur de Structure
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

            <div v-if="!isReadOnly && (store.entete.natureComposantCode === 'PISTON' || (store.entete.operationCode === 'ASS' && store.entete.natureComposantCode === 'PF'))" class="ml-8 w-64 relative shrink-0">
              <input type="file" ref="fileInput" @change="onFileSelected" accept=".xlsx" class="hidden" />
              <button @click="$refs.fileInput.click()" class="w-full p-3 bg-emerald-50 text-center border-2 border-dashed border-emerald-300 hover:border-emerald-500 rounded-xl hover:bg-emerald-100 transition-colors text-emerald-600 font-black uppercase tracking-widest flex flex-col items-center justify-center gap-2 text-[10px] shadow-sm">
                <i class="pi pi-file-excel text-2xl mb-1"></i> 
                <span>Importer un fichier Excel</span>
              </button>
            </div>
          </div>

          <div class="mb-4">
            <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest">2. Structure des lignes de contrôle</h3>
          </div>

          <template v-if="groupes.length === 0">
            <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
              Cliquez sur "Créer une nouvelle section" pour commencer.
            </div>
          </template>

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

          <RemarquesLegendeBox 
            v-model:remarques="store.entete.notes"
            v-model:legendeMoyens="store.entete.legendeMoyens"
            :show-validation="showLegendValidation"
            :has-custom-instruments="hasCustomInstrumentsGlobal"
            :is-read-only="isReadOnly"
          />
        </div>

        <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end" v-if="!isForcedView">
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

import PlanHeader from '@/components/Shared/PlanHeader.vue';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import FabModeleHeader from '@/components/Fabrication/FabModeleHeader.vue';
import FabTableHeader from '@/components/Fabrication/FabTableHeader.vue';
import FabSectionCard from '@/components/Fabrication/FabSectionCard.vue';
import FabLigneControl from '@/components/Fabrication/FabLigneControl.vue'; 
import VersioningDialog from '@/components/Shared/VersioningDialog.vue';
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
const version = ref(1);
const showVersioningDialog = ref(false);
const versioningMode = ref('FAB');

const { 
  showLegendValidation, 
  hasCustomInstrumentsGlobal, 
  validerLegendeMoyens, 
  validerSaisieValeurs 
} = useEditorValidation(groupes, computed(() => store.entete.legendeMoyens), toast);

const { isDirty, updateCurrentSnapshot, initializeSnapshot } = useDirtyChecking();
const { restaurerModele, creerNouvelleVersionModele } = useModeleVersioning();

// 👁️ NOUVEAU : DÉTECTION DU MODE LECTURE SEULE DEPUIS L'URL
const isForcedView = computed(() => route.query.view === 'true');

watch(
  [() => store.entete, () => groupes.value],
  ([newEntete, newGroupes]) => {
    // Ne pas tracer la dirty checking si on est juste en mode vue
    if (modeleEditionId.value && !isForcedView.value) {
      const enteteClone = JSON.parse(JSON.stringify(newEntete));
      const groupesClone = JSON.parse(JSON.stringify(newGroupes));
      updateCurrentSnapshot(createModeleSnapshot(enteteClone, groupesClone));
    }
  },
  { deep: true }
);

// ============================================================================
// COLONNES RÉUTILISABLES
// ============================================================================
const modeleColumns = [
  { label: 'Caractéristique contrôlée', width: 'w-[22%]' },
  { label: 'Limite spécif.', width: 'w-[12%]', textAlign: 'center' },
  { label: 'Type de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { label: 'Moyen de contrôle', width: 'w-[15%]', textAlign: 'center' },
  { label: 'Code instrument', width: 'w-[15%]', textAlign: 'center' },
  { label: 'Observations', width: 'flex-1' },
  { label: '', width: 'w-12', textAlign: 'center' }
];

const isLoading = computed(() => store.isLoading);
const isEditMode = computed(() => !!modeleEditionId.value);
const isArchived = computed(() => statut.value === 'ARCHIVE');

// 🔒 NOUVEAU : On verrouille tout si c'est une archive OU si on est en mode aperçu (view)
const isReadOnly = computed(() => (isEditMode.value && isArchived.value) || isForcedView.value);


const codeAffiche = computed(() => {
  if (isEditMode.value && codeOriginal.value) {
    return `${codeOriginal.value.replace(/-V\d+$/i, '')}-V${version.value + 1}`;
  }
  return store.codeModeleAuto;
});

const headerTitle = computed(() => {
  if (isForcedView.value) return 'Consultation du Modèle'; // Titre adapté
  if (isEditMode.value) return isArchived.value ? 'Restauration d\'Archive' : `Édition du Modèle`;
  return 'Création d\'un Modèle';
});

const headerSubtitle = computed(() => {
  if (isForcedView.value) return 'Mode lecture seule (Aperçu de la structure).'; // Sous-titre adapté
  if (isEditMode.value) {
    return isArchived.value 
      ? 'Vous consultez une archive. Enregistrer restaurera cette version en production.'
      : 'Modifiez la structure. L\'ancienne version sera archivée automatiquement.';
  }
  return 'Configurez la structure des plans du contrôle.';
});

const actionButtonLabel = computed(() => {
  if (isLoading.value) return 'Enregistrement...';
  if (isArchived.value) return 'Restaurer ce Modèle';
  if (isEditMode.value) return 'Enregistrer les modifications';
  return 'Enregistrer le Modèle';
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

  try {
    store.isLoading = true;
    const response = await qualityPlansService.importExcel(formData);
    const parsedData = response.data.data;

    if (parsedData && parsedData.sections) {
      groupes.value = parsedData.sections.map(sec => ({
        id: sec.id || crypto.randomUUID(),
        isFromDb: false,
        libelleSection: sec.nom,
        typeSectionId: sec.typeSectionId,
        modeFreq: sec.modeFreq,
        periodiciteId: sec.periodiciteId,
        freqNum: sec.freqNum,
        typeVariable: sec.typeVariable,
        freqHours: sec.freqHours,
        lignes: sec.lignes.map(lig => ({
          id: lig.id || crypto.randomUUID(),
          isFromDb: false,
          typeCaracteristiqueId: lig.typeCaracteristiqueId,
          typeControleId: lig.typeControleId,
          moyenControleId: lig.moyenControleId,
          instrumentCode: lig.instrumentCode,
          valeurNominale: lig.valeurNominale,
          toleranceSuperieure: lig.toleranceSuperieure,
          toleranceInferieure: lig.toleranceInferieure,
          unite: lig.unite || '',
          limiteSpecTexte: lig.limiteSpecTexte,
          observations: lig.observations,
          instruction: lig.instruction,
          estCritique: lig.estCritique,
          libelleAffiche: lig.libelleAffiche
        }))
      }));

      await store.fetchDictionnaires();
      toast.add({ severity: 'success', summary: 'Import réussi', detail: 'Les données ont été chargées depuis le fichier Excel.', life: 4000 });
    }
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur d\'import', detail: error.response?.data?.message || 'Impossible de lire le fichier.', life: 4000 });
  } finally {
    store.isLoading = false;
    if (fileInput.value) fileInput.value.value = '';
  }
};

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    
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
  try {
    const res = await qualityPlansService.getModeleById(id);
    const data = res.data.data || res.data;
    
    modeleEditionId.value = data.id;
    codeOriginal.value = data.code;
    statut.value = data.statut;
    version.value = data.version;
    
    store.entete.operationCode = data.operationCode;
    store.entete.natureComposantCode = data.natureComposantCode;
    store.entete.typeRobinetCode = data.typeRobinetCode;
    store.entete.libelle = data.libelle;
    store.entete.notes = data.notes || '';
    store.entete.legendeMoyens = data.legendeMoyens || '';

    const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
      (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
    );

    groupes.value = sectionsTriees.map(sec => {
      let modeFreq = 'SANS';
      let periodiciteId = null;
      if (sec.frequenceLibelle) {
        const perMatch = store.periodicites.find(p => p.libelle === sec.frequenceLibelle);
        if (perMatch) {
          modeFreq = 'FIXE';
          periodiciteId = perMatch.id;
        } else {
          modeFreq = 'VARIABLE';
        }
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
        modeFreq,
        periodiciteId,
        freqNum: 1,
        typeVariable: 'HEURE',
        freqHours: 1,
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
          valeurNominale: lig.valeurNominale ?? null,
          toleranceSuperieure: lig.toleranceSuperieure ?? null,
          toleranceInferieure: lig.toleranceInferieure ?? null,
          unite: lig.unite || '',
          limiteSpecTexte: lig.limiteSpecTexte || '',
          observations: lig.observations || '',
          moyenTexteLibre: lig.moyenTexteLibre || ''
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
    store.sections = await preparerDonneesEtFrequences();
    const id = await store.saveModele(store.entete.legendeMoyens);
    
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Modèle créé et activé !', life: 3000 });
    setTimeout(() => router.push('/dev/hub'), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.message, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const sauvegarderV2 = async (motif) => {
  store.isLoading = true;
  try {
    const sectionsPrepared = await preparerDonneesEtFrequences();

    const sectionsForPayload = sectionsPrepared.map((s, idx) => ({
      id: s.id,
      ordreAffiche: idx + 1,
      typeSectionId: s.typeSectionId,
      periodiciteId: s.periodiciteId,
      libelleSection: s.libelleSection || 'SECTION SANS NOM',
      frequenceLibelle: s.frequenceLibelle || '',
      notes: s.notes || '',
      lignes: (s.lignes || []).map((l, lIdx) => ({
        id: l.id,
        ordreAffiche: l.ordreAffiche ?? (lIdx + 1),
        typeCaracteristiqueId: l.typeCaracteristiqueId,
        libelleAffiche: l.libelleAffiche,
        typeControleId: l.typeControleId,
        moyenControleId: l.moyenControleId,
        instrumentCode: l.instrumentCode,
        periodiciteId: l.periodiciteId,
        instruction: l.instruction,
        estCritique: l.estCritique,
        valeurNominale: l.valeurNominale ?? null,
        toleranceSuperieure: l.toleranceSuperieure ?? null,
        toleranceInferieure: l.toleranceInferieure ?? null,
        unite: l.unite || '',
        limiteSpecTexte: l.limiteSpecTexte || '',
        observations: l.observations || '',
        moyenTexteLibre: l.moyenTexteLibre || ''
      }))
    }));

    const payloadV2 = {
      ancienId: modeleEditionId.value,
      modifiePar: 'ADMIN_QUALITE',
      motifModification: motif || 'Création d\'une nouvelle version',
      libelle: store.entete.libelle,
      notes: store.entete.notes || '',
      legendeMoyens: store.entete.legendeMoyens || '',

      natureComposantCode: store.entete.natureComposantCode || null,
      typeRobinetCode: store.entete.typeRobinetCode || null,
      sections: sectionsForPayload
    };

    await creerNouvelleVersionModele(payloadV2);
    toast.add({ severity: 'success', summary: `V${version.value + 1} Activée !`, detail: 'L\'ancienne version a été archivée.', life: 3000 });
    setTimeout(() => router.push('/dev/hub'), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.message, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const restaurerArchive = async (motif) => {
  store.isLoading = true;
  try {
    const payloadRestore = { modeleArchiveId: modeleEditionId.value, restaurePar: 'ADMIN_QUALITE', motifRestoration: motif };
    await restaurerModele(payloadRestore);
    toast.add({ severity: 'success', summary: 'Modèle Restauré !', detail: 'L\'archive a été réactivée en tant que nouvelle version.', life: 4000 });
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

const onEditorSubmit = () => {
  if (isArchived.value) {
    versioningMode.value = 'restore';
    showVersioningDialog.value = true;
  } else if (statut.value === 'ACTIF') {
    versioningMode.value = 'new-version';
    showVersioningDialog.value = true;
  }
};

const onVersioningConfirm = async (motif) => {
  showVersioningDialog.value = false;
  
  if (versioningMode.value === 'new-version') {
    if (!validerSaisieValeurs()) return;
    if (!validerLegendeMoyens()) return;
    if (!isDirty.value) {
      toast.add({ severity: 'info', summary: 'Aucune modification', detail: 'Vous n\'avez effectué aucun changement sur la structure du modèle.', life: 4000 });
      return;
    }
    await sauvegarderV2(motif);
  } else if (versioningMode.value === 'restore') {
    await restaurerArchive(motif);
  }
};

const resetForNewModele = () => {
  modeleEditionId.value = null;
  codeOriginal.value = '';
  statut.value = 'BROUILLON';
  version.value = 0;
  store.entete = { operationCode: '', natureComposantCode: '', typeRobinetCode: '', libelle: '', notes: '', legendeMoyens: '' };
  groupes.value = [];
};
</script>
