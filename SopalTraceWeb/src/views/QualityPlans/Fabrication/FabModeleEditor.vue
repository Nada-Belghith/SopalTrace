<template>
  <div class="bg-slate-50 min-h-screen p-4 md:p-8 font-sans text-slate-800">
    <Toast position="top-right" />

    <div class="max-w-[1600px] mx-auto">
      
      <!-- HEADER DE LA PAGE -->
      <div class="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
        <div>
          <h1 class="text-2xl font-black text-slate-900 tracking-tight">Gestion des Modèles</h1>
          <p class="text-sm text-slate-500 mt-1">Configurez la structure des plans du contrôle.</p>
        </div>
        <div class="flex items-center gap-3 bg-white p-2 rounded-xl shadow-sm border border-slate-200">
            <span class="text-[10px] font-black text-slate-400 uppercase px-2">Code Modèle :</span>
            <div class="px-4 py-1.5 bg-blue-50 text-blue-700 font-mono font-bold rounded-lg border border-blue-200">
               {{ store.codeModeleAuto }}
            </div>
        </div>
      </div>

      <!-- PANNEAU ÉDITEUR -->
      <div class="bg-white rounded-xl shadow-xl border border-slate-200 overflow-hidden">
        
        <!-- Header sombre -->
        <div class="bg-[#1e293b] text-white px-5 py-3.5 flex justify-between items-center">
          <div class="flex items-center gap-3 font-bold tracking-wide">
            <i class="pi pi-book text-lg"></i> Éditeur de Structure
          </div>
          <button @click="$router.back()" class="text-slate-400 hover:text-white transition-colors">
            <i class="pi pi-times text-lg"></i>
          </button>
        </div>

        <div class="p-6 md:p-8">
          
          <!-- BRIQUE 1 : L'ENTÊTE (Composant) -->
          <FabModeleHeader />

          <!-- ZONE DE L'ARBRE -->
          <div class="mb-4">
            <h3 class="text-[11px] font-black text-slate-500 uppercase tracking-widest">2. Structure des lignes de contrôle</h3>
          </div>

          <template v-if="groupes.length === 0">
            <div class="p-8 text-center text-slate-400 text-sm italic bg-slate-50 rounded-lg border border-slate-200 mb-6">
              Cliquez sur "Créer une nouvelle section" pour commencer.
            </div>
          </template>

          <!-- BRIQUE 2 : LES SECTIONS (Composant) -->
          <FabSectionCard 
            v-for="(groupe, index) in groupes" 
            :key="groupe.id" 
            :groupe="groupe" 
            :index="index" 
            @remove="supprimerGroupe(groupe.id)"
            @update-groupe="mettreAJourGroupe(groupe.id, $event)"
          />
          
          <!-- BOUTON D'AJOUT -->
          <div class="mt-2">
            <button @click="ajouterGroupe" class="w-full p-4 bg-slate-50 text-center border border-dashed border-slate-300 hover:border-blue-400 rounded-lg hover:bg-blue-50 transition-colors text-slate-500 hover:text-blue-600 text-xs font-black uppercase tracking-widest flex items-center justify-center gap-2">
              <i class="pi pi-plus-circle text-lg"></i> Créer une nouvelle section
            </button>
          </div>

        </div>

        <!-- FOOTER DU PANNEAU (SAUVEGARDE) -->
        <div class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end items-center gap-4">
          <button @click="$router.back()" class="px-8 py-3 bg-white border border-slate-300 text-slate-700 rounded-xl text-sm font-black uppercase tracking-widest hover:bg-slate-100 transition-colors shadow-sm">
            Annuler
          </button>
          <button @click="sauvegarder" :disabled="store.isLoading" class="px-10 py-3 bg-[#2563eb] hover:bg-blue-800 text-white rounded-xl text-sm font-black uppercase tracking-widest transition-all shadow-lg flex items-center gap-3 disabled:opacity-50 disabled:cursor-not-allowed">
            <i v-if="store.isLoading" class="pi pi-spin pi-spinner"></i>
            <i v-else class="pi pi-save"></i> 
            {{ store.isLoading ? 'Enregistrement...' : 'Enregistrer le Modèle' }}
          </button>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import apiClient from '@/services/apiClient';

// Import de nos briques réutilisables
import FabModeleHeader from '@/components/Fabrication/FabModeleHeader.vue';
import FabSectionCard from '@/components/Fabrication/FabSectionCard.vue';

const store = useFabModeleStore();
const toast = useToast();

const groupes = ref([]);

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    if(groupes.value.length === 0) ajouterGroupe();
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur réseau', detail: error.message, life: 5000 });
  }
});

const ajouterGroupe = () => {
  groupes.value.push({ 
    id: crypto.randomUUID(), 
    typeSectionId: '',
    modeFreq: 'SANS',
    periodiciteId: null,
    freqNum: 1,           // 1 Pièce
    typeVariable: 'HEURE',// Par heure
    freqHours: 1,         // Toutes les 1 heures
    isNewFreq: false,
    nom: '', 
    lignes: [] 
  });
};

const supprimerGroupe = (id) => {
  groupes.value = groupes.value.filter(g => g.id !== id);
};

const mettreAJourGroupe = (id, updatedGroupe) => {
  const index = groupes.value.findIndex(g => g.id === id);
  if (index !== -1) {
    groupes.value[index] = updatedGroupe;
  }
};

// Orchestration de la sauvegarde et des requêtes On-The-Fly
const sauvegarder = async () => {
  store.isLoading = true;
  
  try {
    // 1. CRÉATION DES FRÉQUENCES A LA VOLÉE (Générateur Dynamique)
    for (const g of groupes.value) {
        // On vérifie les modes VARIABLES qui n'ont pas encore d'ID
        if (g.modeFreq === 'VARIABLE' && !g.periodiciteId) {
            
            let libelleFreq = "";
            let codeFreq = "";
            const sP = g.freqNum > 1 ? 's' : '';

            // Construction du libellé affiché à l'utilisateur
            if (g.typeVariable === 'HEURE') {
                const sH = g.freqHours > 1 ? 's' : '';
                if (g.freqHours == 1) {
                    libelleFreq = `${g.freqNum} pièce${sP} / heure`;
                } else {
                    libelleFreq = `${g.freqNum} pièce${sP} / ${g.freqHours} heure${sH}`;
                }
                codeFreq = `${g.freqNum}P_${g.freqHours}H`;
            } else if (g.typeVariable === 'SERIE') {
                libelleFreq = `une série de ${g.freqNum} pièces`;
                codeFreq = `SERIE_${g.freqNum}P`;
            }

            const perioExistante = store.periodicites.find(p => p.libelle.toLowerCase() === libelleFreq.toLowerCase());

            if (perioExistante) {
                g.periodiciteId = perioExistante.id;
                g.isNewFreq = false;
            } else {
                const payloadFreq = {
                    code: codeFreq,
                    libelle: libelleFreq,
                    frequenceNum: g.freqNum,
                    frequenceUnite: g.typeVariable === 'HEURE' ? `${g.freqHours}_HEURE` : 'SERIE',
                    ordreAffichage: 5
                };

                const res = await apiClient.post('/plans-fabrication/periodicites', payloadFreq);
                g.periodiciteId = res.data.periodiciteId || res.data.id; 
                g.isNewFreq = false;
                
                // On l'ajoute immédiatement au Store local pour éviter les doublons lors de la prochaine itération
                store.periodicites.push({ id: g.periodiciteId, ...payloadFreq });
            }
        }
    }

    // 2. MAPPING ET ENVOI AU STORE
    const sectionsFormatees = groupes.value.map((g, idx) => ({
      ordreAffiche: idx + 1,
      typeSectionId: g.typeSectionId, 
      libelleSection: g.nom, 
      frequenceLibelle: g.periodiciteId ? (store.periodicites || []).find(p => p.id === g.periodiciteId)?.libelle : '',
      
      lignes: g.lignes.map((l, lIdx) => ({
        ordreAffiche: lIdx + 1,
        typeCaracteristiqueId: l.typeCaracteristiqueId, 
        libelleAffiche: l.libelleAffiche,
        typeControleId: l.typeControleId,
        moyenControleId: l.moyenControleId,
        groupeInstrumentId: l.groupeInstrumentId,
        instrumentCode: l.instrumentCode, // Utilisé pour les alias (INS|)
        periodiciteId: g.periodiciteId, 
        instruction: l.instruction,
        estCritique: l.estCritique
      }))
    }));

    store.sections = sectionsFormatees;

    await store.saveModele();
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Structure enregistrée !', life: 3000 });
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.message, life: 6000 });
  } finally {
    store.isLoading = false;
  }
};
</script>