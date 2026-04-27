<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
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
          <FabModeleHeader :is-edit-mode="isEditMode" :is-read-only="isReadOnly" />

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

          <LegendValidationBox 
            v-model="legendeMoyens"
            :show-validation="showLegendValidation"
            :has-custom-instruments="hasCustomInstrumentsGlobal"
            :is-read-only="isReadOnly"
            :is-forced-view="isForcedView"
          />
        </div>

        <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end gap-4">
          <EditorActions
            v-if="!isForcedView"
            :label="actionButtonLabel"
            loading-label="Enregistrement..."
            :icon="actionButtonIcon"
            :variant="actionButtonVariant"
            :is-loading="isLoading"
            @submit="declencherSauvegarde"
            @cancel="$router.push('/dev/hub')"
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
import LegendValidationBox from '@/components/Shared/LegendValidationBox.vue';
import FabModeleHeader from '@/components/Fabrication/FabModeleHeader.vue';
import FabTableHeader from '@/components/Fabrication/FabTableHeader.vue';
import FabSectionCard from '@/components/Fabrication/FabSectionCard.vue';
import FabLigneControl from '@/components/Fabrication/FabLigneControl.vue'; 

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
const legendeMoyens = ref('');

const { 
  showLegendValidation, 
  hasCustomInstrumentsGlobal, 
  validerLegendeMoyens, 
  validerSaisieValeurs 
} = useEditorValidation(groupes, legendeMoyens, toast);

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
    legendeMoyens.value = data.legendeMoyens || '';

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
        nom: sec.libelleSection,
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



const declencherSauvegarde = async () => {
  if (isEditMode.value && isArchived.value) {
    await restaurerArchive();
    return;
  }
  
  if (!validerSaisieValeurs()) {
    return;
  }
  
  if (!validerLegendeMoyens()) {
    return;
  }
  
  if (isEditMode.value) {
    if (!isDirty.value) {
      toast.add({ severity: 'info', summary: 'Aucune modification', detail: 'Vous n\'avez effectué aucun changement sur la structure du modèle.', life: 4000 });
      return;
    }
    await sauvegarderV2();
  } else {
    await sauvegarderV1();
  }
};

const sauvegarderV1 = async () => {
  store.isLoading = true;
  try {
    store.sections = await preparerDonneesEtFrequences();
    await store.saveModele(legendeMoyens.value);
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Modèle V1 créé et activé !', life: 3000 });
    setTimeout(() => router.push('/dev/hub'), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.message, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const sauvegarderV2 = async () => {
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
      motifModification: 'Mise à jour directe',
      libelle: store.entete.libelle,
      notes: store.entete.notes || '',
      legendeMoyens: legendeMoyens.value || '',

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

const restaurerArchive = async () => {
  store.isLoading = true;
  try {
    const payloadRestore = { modeleArchiveId: modeleEditionId.value, restaurePar: 'ADMIN_QUALITE', motifRestoration: 'Restauration d\'archive' };
    await restaurerModele(payloadRestore);
    toast.add({ severity: 'success', summary: 'Modèle Restauré !', detail: 'L\'archive a été réactivée en tant que nouvelle version.', life: 4000 });
    setTimeout(() => router.push('/dev/hub'), 1500);
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur de restauration', detail: error.message, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};

const resetForNewModele = () => {
  modeleEditionId.value = null;
  codeOriginal.value = '';
  statut.value = 'BROUILLON';
  version.value = 0;
  store.entete = { operationCode: '', natureComposantCode: '', typeRobinetCode: '', libelle: '', notes: '' };
  legendeMoyens.value = '';
  groupes.value = [];
};
</script>