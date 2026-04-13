<template>
  <div class="border border-slate-200 rounded-lg overflow-x-auto shadow-sm mb-6 bg-white">
    <table class="w-full text-left border-collapse min-w-[1200px]">
      
      <!-- EN-TÊTE GLOBAL -->
      <thead v-if="index === 0">
        <tr class="bg-[#1e293b] text-white">
          <th class="p-3 text-[10px] font-black uppercase w-[22%] tracking-widest border-r border-slate-700">Caractéristique contrôlée</th>
          <th class="p-3 text-[10px] font-black uppercase w-[12%] tracking-widest border-r border-slate-700 text-center">Limite spécif.</th>
          <th class="p-3 text-[10px] font-black uppercase w-[12%] tracking-widest border-r border-slate-700 text-center">Type de contrôle</th>
          <th class="p-3 text-[10px] font-black uppercase w-[15%] tracking-widest border-r border-slate-700 text-center">Moyen de contrôle</th>
          <th class="p-3 text-[10px] font-black uppercase w-[15%] tracking-widest border-r border-slate-700 text-center">Code instrument</th>
          <th class="p-3 text-[10px] font-black uppercase tracking-widest border-r border-slate-700">Observations</th>
          <th class="p-3 text-[10px] font-black uppercase text-center w-12"><i class="pi pi-trash"></i></th>
        </tr>
      </thead>
      
      <tbody class="divide-y divide-slate-200">
        
        <!-- BANDEAU DU GROUPE (GÉNÉRATEUR DE PHRASE) -->
        <tr class="bg-[#f1f5f9] border-t-4 border-slate-300">
          <td colspan="7" class="p-3 px-4 relative">
            <div class="flex flex-col gap-3 pr-40">
              
              <!-- LIGNE 1 : Les Choix Métier -->
              <div class="flex items-center gap-3">
                <span class="bg-blue-100 text-blue-800 text-[10px] font-black px-2 py-1.5 rounded-md uppercase tracking-widest shrink-0 shadow-sm">
                    Sec {{ index + 1 }}
                </span>
                
                <select v-model="groupe.typeSectionId" @change="verifierVariables" class="w-64 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-xs font-bold text-slate-800 outline-none focus:border-blue-500 shadow-sm cursor-pointer">
                  <option value="" disabled>--Nature de la section--</option>
                  <option v-for="ts in (store.typesSection || [])" :key="ts.id" :value="ts.id">{{ ts.libelle }}</option>
                </select>

                <select v-model="groupe.modeFreq" @change="verifierVariables" class="bg-slate-100 border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-600 outline-none focus:border-blue-500 shadow-sm cursor-pointer">
                  <option value="SANS">Sans fréquence</option>
                  <option value="VARIABLE">➕ Fréquence des pièces par heure</option>
                  <option value="FIXE">➕ Règle d'Échantillonnage</option>
                </select>

                <!-- SI VARIABLE : Saisie du nombre de pièces ET du nombre d'heures -->
                <div v-if="groupe.modeFreq === 'VARIABLE'" class="flex items-center gap-2 animate-in fade-in slide-in-from-left-4 bg-white border border-slate-300 rounded-lg p-1 shadow-sm">
                    
                    <input type="number" v-model="groupe.freqNum" @input="verifierVariables" min="1" max="1000" class="w-12 bg-slate-100 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs" title="Nb Pièces" />
                    <span class="text-[10px] font-bold text-slate-500 uppercase">Pièce(s)</span>
                    
                    <select v-model="groupe.typeVariable" @change="verifierVariables" class="bg-transparent text-slate-700 font-bold outline-none cursor-pointer text-xs ml-1 border-l border-slate-200 pl-2">
                      <option value="HEURE">/ Heure(s)</option>
                      <option value="SERIE">par Série</option>
                    </select>

                    <!-- Affichage du choix d'heure uniquement si le mode est HEURE -->
                    <template v-if="groupe.typeVariable === 'HEURE'">
                        <span class="text-[10px] font-bold text-slate-500 uppercase ml-1">Toutes les</span>
                        <input type="number" v-model="groupe.freqHours" @input="verifierVariables" min="1" max="24" class="w-12 bg-slate-100 text-blue-700 font-black text-center rounded px-1 py-1 outline-none focus:ring-1 focus:ring-blue-500 text-xs" title="Nb Heures" />
                        <span class="text-[10px] font-bold text-slate-500 uppercase pr-1">H</span>
                    </template>

                    <i v-if="groupe.isNewFreq" class="pi pi-sparkles text-emerald-500 ml-1" title="Sera sauvegardée dans le dictionnaire."></i>
                </div>

                <!-- SI FIXE : Liste textes -->
                <div v-if="groupe.modeFreq === 'FIXE'" class="flex items-center animate-in fade-in slide-in-from-left-4">
                    <select v-model="groupe.periodiciteId" @change="verifierVariables" class="w-64 bg-white border border-slate-300 rounded-lg px-2 py-1.5 text-[11px] font-bold text-slate-700 outline-none focus:border-blue-500 shadow-sm cursor-pointer">
                      <option :value="null" disabled>Sélectionner la règle...</option>
                      <option v-for="p in periodesFixes" :key="p.id" :value="p.id" :title="p.libelle">{{ p.libelle.substring(0, 35) }}{{ p.libelle.length > 35 ? '...' : '' }}</option>
                    </select>
                </div>
              </div>

              <!-- LIGNE 2 : Aperçu final -->
              <div class="w-full border text-[11px] font-black tracking-widest rounded px-3 py-2 flex items-center shadow-inner transition-colors bg-white border-slate-200 text-slate-700">
                  <span class="text-blue-500 mr-2 uppercase">Aperçu :</span> {{ groupe.nom || 'SÉLECTIONNEZ UNE NATURE POUR GÉNÉRER LE TITRE' }}
              </div>
            </div>

            <!-- Boutons de droite -->
            <div class="flex items-center gap-4 absolute right-4 top-1/2 -translate-y-1/2">
              <button @click="ajouterLigne" class="text-blue-600 text-[11px] font-black uppercase tracking-widest hover:text-blue-800 flex items-center gap-1 transition-colors">
                <i class="pi pi-plus"></i> Ajouter ligne
              </button>
              <button @click="$emit('remove')" class="text-slate-400 hover:text-red-600 transition-colors ml-2" title="Supprimer la section">
                <i class="pi pi-times-circle text-base"></i>
              </button>
            </div>
          </td>
        </tr>

        <!-- SOUS-COMPOSANTS : Les Lignes -->
        <FabLigneControl 
            v-for="ligne in groupe.lignes" 
            :key="ligne.id" 
            :ligne="ligne" 
            @remove="supprimerLigne" 
        />
        
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useFabModeleStore } from '@/stores/fabModeleStore';
import FabLigneControl from './FabLigneControl.vue';

const props = defineProps({
  groupe: { type: Object, required: true },
  index: { type: Number, required: true }
});

const emit = defineEmits(['remove']);
const store = useFabModeleStore();

const periodesFixes = computed(() => (store.periodicites || []).filter(p => 
  (p.frequenceNum === null || p.frequenceNum === undefined) && 
  p.frequenceUnite !== 'MACHINE'
));

// --- MOTEUR INTELLIGENT ---
const verifierVariables = () => {
  const groupe = props.groupe; 
  const typeSec = (store.typesSection || []).find(ts => ts.id === groupe.typeSectionId);
  let texteBase = typeSec ? typeSec.libelle : "???";
  let titre = `Caractéristiques à contrôler ${texteBase}`;

  if (groupe.modeFreq === 'SANS') {
      groupe.periodiciteId = null;
      groupe.isNewFreq = false;
      groupe.nom = titre;
  }
  else if (groupe.modeFreq === 'FIXE') {
      const perio = (store.periodicites || []).find(p => p.id === groupe.periodiciteId);
      groupe.isNewFreq = false;
      groupe.nom = perio ? `${titre} (${perio.libelle})` : `${titre} (Veuillez choisir une règle)`;
  }
  else if (groupe.modeFreq === 'VARIABLE') {
      
      let libelleFreq = "";
      const sP = groupe.freqNum > 1 ? 's' : '';
      
      if (groupe.typeVariable === 'HEURE') {
          const sH = groupe.freqHours > 1 ? 's' : '';
          if (groupe.freqHours == 1) {
              libelleFreq = `${groupe.freqNum} pièce${sP} / heure`;
          } else {
              libelleFreq = `${groupe.freqNum} pièce${sP} / ${groupe.freqHours} heure${sH}`;
          }
      } else {
          libelleFreq = `une série de ${groupe.freqNum} pièces`;
      }

      // Recherche dans la base si ce libellé exact existe déjà
      const perio = (store.periodicites || []).find(p => p.libelle.toLowerCase() === libelleFreq.toLowerCase());
      
      if (perio) {
          groupe.periodiciteId = perio.id;
          groupe.isNewFreq = false;
          groupe.nom = `${titre} (${perio.libelle})`;
      } else {
          groupe.periodiciteId = null;
          groupe.isNewFreq = true;
          groupe.nom = `${titre} (${libelleFreq})`;
      }
  }
};

// --- ACTIONS SUR L'ARBRE LOCAL ---
const ajouterLigne = () => {
  props.groupe.lignes.push({
    id: crypto.randomUUID(),
    typeCaracteristiqueId: '', // Forcer le placeholder
    libelleAffiche: '',
    typeControleId: null,      // Forcer le placeholder
    moyenControleId: null,
    groupeInstrumentId: null,
    instrumentCode: null,
    instruction: '',
    estCritique: false
  });
};

const supprimerLigne = (ligneId) => {
  const idx = props.groupe.lignes.findIndex(l => l.id === ligneId);
  if (idx !== -1) props.groupe.lignes.splice(idx, 1);
};
</script>