<template>
  <div class="min-h-screen bg-slate-50 text-slate-800 p-3 md:p-6 lg:p-8 font-sans">
    <div class="max-w-6xl mx-auto space-y-6 md:space-y-8 animate-in fade-in duration-500">
      
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between bg-white border border-slate-200 p-4 md:p-6 rounded-2xl md:rounded-3xl shadow-sm gap-4">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 md:w-14 md:h-14 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-2xl flex items-center justify-center shadow-md shadow-blue-500/10 rotate-3 shrink-0">
            <i class="pi pi-box text-xl md:text-2xl text-white"></i>
          </div>
          <div>
            <h1 class="text-xl md:text-2xl font-black text-slate-900 tracking-tight">Magasinier <span class="text-blue-600">Scan & Traçabilité</span></h1>
            <p class="text-slate-500 text-[10px] md:text-xs font-bold uppercase tracking-widest flex items-center gap-2 mt-0.5">
              <span class="w-2 h-2 bg-green-500 rounded-full animate-pulse"></span>
              Session : {{ authStore.userName }}
            </p>
          </div>
        </div>
        
        <div class="flex items-center justify-between sm:justify-end w-full sm:w-auto gap-3 border-t sm:border-t-0 border-slate-100 pt-3 sm:pt-0">
          <div class="flex items-center bg-slate-100 border border-slate-200 p-1 rounded-xl text-[10px] md:text-xs font-bold text-slate-600">
            <span class="px-2.5 py-1 rounded-lg bg-white shadow-sm text-blue-600 flex items-center gap-1">
              <i class="pi pi-qrcode"></i> Automatique
            </span>
          </div>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 md:gap-6">
        
        <div class="bg-white border border-slate-200 p-5 md:p-8 rounded-2xl md:rounded-3xl shadow-sm relative overflow-hidden group">
          <div class="absolute top-0 right-0 w-32 h-32 bg-blue-600/5 rounded-full blur-3xl -mr-16 -mt-16"></div>
          
          <div class="relative z-10 space-y-4">
            <div class="flex items-center justify-between">
              <label class="block text-[10px] md:text-[11px] font-black text-blue-600 uppercase tracking-widest">Étape 1 : Scanner l'OF</label>
              <span class="px-2 py-0.5 bg-blue-50 text-blue-600 text-[9px] md:text-[10px] font-bold rounded-md">Entrée Auto</span>
            </div>
            
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-400 group-focus-within:text-blue-500">
                <i class="pi pi-barcode text-xl md:text-2xl"></i>
              </span>
              <input 
                ref="ofInput"
                v-model="scannedOf"
                @keyup.enter="handleOfScan"
                type="text" 
                placeholder="Scanner l'OF..."
                class="w-full bg-slate-50 border border-slate-300 text-slate-900 pl-12 pr-24 py-3.5 md:py-4.5 rounded-2xl focus:outline-none focus:border-blue-500 focus:bg-white focus:ring-4 focus:ring-blue-500/10 transition-all font-black text-base md:text-lg placeholder:text-slate-400 uppercase"
              />
              <div class="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1.5">
                <button 
                  @click="startCameraScanner('OF')"
                  class="p-2 bg-blue-50 hover:bg-blue-100 text-blue-600 rounded-xl transition-all flex items-center justify-center border border-blue-100 active:scale-95 shadow-sm"
                  title="Scanner avec la caméra"
                >
                  <i class="pi pi-camera text-base"></i>
                </button>
                <div v-if="isLoadingOf">
                  <i class="pi pi-spin pi-spinner text-blue-500 text-lg"></i>
                </div>
                <button 
                  v-else-if="scannedOf" 
                  @click="scannedOf = ''; focusOfInput()" 
                  class="text-slate-400 hover:text-slate-600"
                >
                  <i class="pi pi-times-circle text-lg"></i>
                </button>
              </div>
            </div>
            <p class="text-slate-500 text-[10px] md:text-[11px] font-bold italic flex items-center gap-1">
              <i class="pi pi-info-circle text-xs text-blue-500"></i>
              Flashez le code-barres de votre bon de sortie.
            </p>
          </div>
        </div>

        <div 
          class="bg-white border p-5 md:p-8 rounded-2xl md:rounded-3xl shadow-sm relative overflow-hidden group transition-all"
          :class="currentOf ? 'border-slate-200 opacity-100' : 'border-slate-100 opacity-40 pointer-events-none'"
        >
          <div class="absolute top-0 right-0 w-32 h-32 bg-indigo-600/5 rounded-full blur-3xl -mr-16 -mt-16"></div>
          
          <div class="relative z-10 space-y-4">
            <div class="flex items-center justify-between">
              <label class="block text-[10px] md:text-[11px] font-black text-indigo-600 uppercase tracking-widest">Étape 2 : Scanner QR Composant</label>
              <span class="px-2 py-0.5 bg-indigo-50 text-indigo-600 text-[9px] md:text-[10px] font-bold rounded-md">Lecture instantanée</span>
            </div>
            
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-400 group-focus-within:text-indigo-500">
                <i class="pi pi-qrcode text-xl md:text-2xl"></i>
              </span>
              <input 
                ref="qrInput"
                v-model="scannedQr"
                @keyup.enter="handleQrScan"
                type="text" 
                :disabled="!currentOf"
                placeholder="Scanner QR Lot..."
                class="w-full bg-slate-50 border border-slate-300 text-slate-900 pl-12 pr-24 py-3.5 md:py-4.5 rounded-2xl focus:outline-none focus:border-indigo-500 focus:bg-white focus:ring-4 focus:ring-indigo-500/10 transition-all font-black text-base md:text-lg placeholder:text-slate-400"
              />
              <div class="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1.5">
                <button 
                  @click="startCameraScanner('QR')"
                  :disabled="!currentOf"
                  class="p-2 bg-indigo-50 hover:bg-indigo-100 disabled:bg-slate-100 text-indigo-600 disabled:text-slate-400 rounded-xl transition-all flex items-center justify-center border border-indigo-100 disabled:border-slate-200 active:scale-95 shadow-sm"
                  title="Scanner avec la caméra"
                >
                  <i class="pi pi-camera text-base"></i>
                </button>
                <button 
                  v-if="scannedQr" 
                  @click="scannedQr = ''" 
                  class="text-slate-400 hover:text-slate-600"
                >
                  <i class="pi pi-times-circle text-lg"></i>
                </button>
              </div>
            </div>
            <p class="text-slate-500 text-[10px] md:text-[11px] font-bold italic flex items-center gap-1">
              <i class="pi pi-info-circle text-xs text-indigo-500"></i>
              Format : CodeArticle ; NumLot ; [RapportQC]
            </p>
          </div>
        </div>
      </div>

      <div v-if="currentOf" class="animate-in slide-in-from-bottom-6 duration-500 space-y-6">
        <div class="bg-white border border-slate-200 rounded-2xl md:rounded-3xl shadow-sm overflow-hidden">
          
          <div class="bg-slate-50 p-4 md:p-6 border-b border-slate-200 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div class="flex flex-col sm:flex-row sm:items-center gap-4">
              <div class="px-4 py-2 bg-blue-50 border border-blue-200 rounded-2xl text-blue-700 font-extrabold text-base md:text-lg tracking-wider self-start sm:self-auto">
                OF: {{ currentOf.numeroOF }}
              </div>
              
              <div>
                <h2 class="text-slate-900 font-extrabold text-base md:text-lg leading-tight">{{ currentOf.articleDesignation }}</h2>
                <div class="flex items-center gap-2 mt-1 flex-wrap">
                  <span class="text-slate-500 text-xs font-bold tracking-wider">{{ currentOf.articleCode }}</span>
                  <span class="h-3 w-px bg-slate-300"></span>
                  
                  <span 
                    class="px-2 py-0.5 text-[10px] font-black uppercase rounded-md"
                    :class="currentOf.articleNatureCode === 'PF' ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-blue-50 text-blue-700 border border-blue-200'"
                  >
                    {{ currentOf.articleNatureLibelle || (currentOf.articleNatureCode === 'PF' ? 'Produit Fini (PF)' : 'Semi-Fini (SF)') }}
                  </span>
                </div>
              </div>
            </div>

            <div class="flex items-center justify-between lg:justify-end gap-6 border-t border-slate-200 lg:border-none pt-4 lg:pt-0">
              <div class="text-left lg:text-right">
                <p class="text-slate-400 text-[10px] font-black uppercase">Qté Planifiée</p>
                <p class="text-slate-900 font-black text-xl md:text-2xl mt-0.5">{{ currentOf.quantitePrevue }} <span class="text-slate-400 text-xs md:text-sm font-bold">pcs</span></p>
              </div>

              <button 
                @click="savePreparation"
                :disabled="isSaving || !hasPreparedItems"
                class="px-4 md:px-6 py-3 md:py-3.5 bg-blue-600 hover:bg-blue-700 disabled:bg-slate-200 text-white disabled:text-slate-400 rounded-2xl font-black uppercase text-[10px] md:text-xs tracking-wider shadow-md hover:shadow-lg disabled:shadow-none transition-all active:scale-95 flex items-center gap-2 shrink-0"
              >
                <i v-if="isSaving" class="pi pi-spin pi-spinner"></i>
                <i v-else class="pi pi-check"></i>
                Valider Sortie
              </button>
            </div>
          </div>

          <div v-if="preparedItems.length === 0" class="p-8 md:p-14 text-center flex flex-col items-center justify-center space-y-4 bg-slate-50/50">
            <div class="w-16 h-16 bg-white border border-slate-200/80 rounded-2xl flex items-center justify-center text-slate-400 shadow-inner rotate-3">
              <i class="pi pi-qrcode text-3xl animate-pulse text-indigo-500"></i>
            </div>
            <div class="max-w-md">
              <h3 class="text-slate-900 font-extrabold text-base md:text-lg">Prêt pour le scan de lot</h3>
              <p class="text-slate-500 text-xs md:text-sm mt-1 font-semibold leading-relaxed">
                Le panier de préparation est actuellement vide. Scannez un lot de composant ou de matière première avec la caméra 📷 ou via le champ ci-dessus pour l'ajouter automatiquement au panier.
              </p>
            </div>
          </div>

          <div v-else class="hidden md:block overflow-x-auto">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="bg-slate-100/50">
                  <th class="px-6 py-4 text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-200">Composant / MP</th>
                  <th class="px-6 py-4 text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-200">Nature</th>
                  <th class="px-6 py-4 text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-200">Désignation</th>
                  <th class="px-6 py-4 text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-200 text-center">Qté Requise</th>
                  <th class="px-6 py-4 text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-200">Lots Scannés</th>
                  <th class="px-6 py-4 text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-200">Rapport de Contrôle (QC)</th>
                  <th class="px-6 py-4 text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-200 text-center">Action</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="(comp, index) in preparedItems" :key="comp.code" class="hover:bg-slate-50/50 transition-colors">
                  <td class="px-6 py-4.5">
                    <span class="px-3 py-1.5 bg-slate-100 border border-slate-200 rounded-lg text-slate-800 font-extrabold text-sm tracking-wide">{{ comp.code }}</span>
                  </td>
                  
                  <td class="px-6 py-4.5">
                    <span class="text-xs font-bold text-slate-500 uppercase">{{ comp.nature || 'COMPOSANT' }}</span>
                  </td>
 
                  <td class="px-6 py-4.5">
                    <p class="text-slate-800 font-bold text-sm">{{ comp.designation }}</p>
                  </td>
 
                  <td class="px-6 py-4.5 text-center">
                    <span class="text-slate-900 font-black text-base">{{ comp.quantite }}</span>
                  </td>
 
                  <td class="px-6 py-4.5">
                    <div class="relative group max-w-[240px]">
                      <input 
                        v-model="comp.lots"
                        type="text"
                        placeholder="Attente de scan QR..."
                        class="w-full bg-slate-50 border border-slate-200 text-slate-800 px-4 py-2.5 rounded-xl focus:outline-none focus:border-blue-500 transition-all font-bold text-sm pr-10"
                      />
                      <i v-if="comp.isScanned" class="pi pi-check-circle absolute right-3 top-1/2 -translate-y-1/2 text-emerald-500 text-lg"></i>
                    </div>
                  </td>
 
                  <td class="px-6 py-4.5">
                    <input 
                      v-model="comp.rapportQC"
                      type="text"
                      placeholder="Aucun rapport"
                      class="w-full bg-slate-50 border border-slate-200 text-slate-800 px-4 py-2.5 rounded-xl focus:outline-none focus:border-indigo-500 transition-all font-bold text-sm max-w-[180px]"
                    />
                  </td>

                  <td class="px-6 py-4.5 text-center">
                    <button 
                      @click="removePreparedItem(index)" 
                      class="p-2 text-slate-400 hover:text-red-500 active:scale-90 transition-all rounded-xl hover:bg-red-50"
                      title="Retirer ce composant"
                    >
                      <i class="pi pi-trash text-sm"></i>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div v-if="preparedItems.length > 0" class="block md:hidden divide-y divide-slate-100">
            <div v-for="(comp, index) in preparedItems" :key="comp.code" class="p-4 space-y-3 bg-white hover:bg-slate-50/50 transition-colors">
              <div class="flex items-start justify-between gap-4">
                <div class="space-y-1">
                  <p class="text-[9px] font-black text-slate-400 uppercase tracking-widest">{{ comp.nature || 'COMPOSANT' }}</p>
                  <h4 class="text-slate-900 font-extrabold text-sm leading-snug">{{ comp.designation }}</h4>
                  <span class="inline-block mt-1 px-2.5 py-1 bg-slate-100 border border-slate-200 rounded-lg text-slate-800 font-extrabold text-[10px] tracking-wide">{{ comp.code }}</span>
                </div>
                <div class="text-right shrink-0 flex flex-col items-end gap-2">
                  <div>
                    <p class="text-[8px] font-black text-slate-400 uppercase">Qté Requise</p>
                    <span class="inline-block mt-0.5 text-slate-900 font-black text-sm bg-slate-100 border border-slate-200 px-2.5 py-1 rounded-lg">{{ comp.quantite }}</span>
                  </div>
                  <button 
                    @click="removePreparedItem(index)" 
                    class="p-1.5 text-slate-400 hover:text-red-500 rounded-lg hover:bg-red-50 active:scale-95"
                  >
                    <i class="pi pi-trash text-sm"></i>
                  </button>
                </div>
              </div>
              
              <div class="space-y-1">
                <label class="block text-[9px] font-black text-slate-500 uppercase tracking-wide">Lots Scannés</label>
                <div class="relative">
                  <input 
                    v-model="comp.lots"
                    type="text"
                    placeholder="Attente de scan QR..."
                    class="w-full bg-slate-50 border border-slate-200 text-slate-800 px-3 py-2 rounded-xl focus:outline-none focus:border-blue-500 transition-all font-bold text-xs pr-9"
                  />
                  <i v-if="comp.isScanned" class="pi pi-check-circle absolute right-3 top-1/2 -translate-y-1/2 text-emerald-500 text-base"></i>
                </div>
              </div>

              <div class="space-y-1">
                <label class="block text-[9px] font-black text-slate-500 uppercase tracking-wide">Rapport de Contrôle (QC)</label>
                <input 
                  v-model="comp.rapportQC"
                  type="text"
                  placeholder="Saisir rapport QC..."
                  class="w-full bg-slate-50 border border-slate-200 text-slate-800 px-3 py-2 rounded-xl focus:outline-none focus:border-indigo-500 transition-all font-bold text-xs"
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="flex flex-col items-center justify-center py-24 bg-white border border-dashed border-slate-200 rounded-3xl shadow-sm text-center">
        <div class="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mb-5 text-slate-400">
          <i class="pi pi-search text-3xl"></i>
        </div>
        <h3 class="text-lg font-extrabold text-slate-700 uppercase tracking-widest">En attente de scan...</h3>
        <p class="text-slate-500 text-sm font-semibold mt-1">Flashez le code-barres de l'OF sur votre bon de sortie</p>
      </div>

    </div>

    <div v-show="showCameraScanner" class="fixed inset-0 bg-black/95 z-50 flex flex-col items-center justify-between p-6 font-sans">
      <div class="w-full max-w-md flex items-center justify-between text-white border-b border-white/10 pb-4">
        <div class="flex items-center gap-3">
          <i class="pi pi-camera text-xl text-blue-400 animate-pulse"></i>
          <div>
            <h3 class="font-extrabold text-sm uppercase tracking-wide">Scanner par Caméra</h3>
            <p class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">
              {{ scannerTarget === 'OF' ? "Étape 1 : Ordre de Fab (OF)" : "Étape 2 : Composant / Lot" }}
            </p>
          </div>
        </div>
        <button @click="stopCameraScanner" class="p-2 bg-white/10 hover:bg-white/20 active:scale-95 text-white rounded-xl transition-all">
          <i class="pi pi-times text-lg"></i>
        </button>
      </div>

      <div class="relative w-full max-w-sm aspect-square bg-[#0f172a] rounded-3xl border border-white/20 overflow-hidden shadow-2xl">
        <!-- Scanner Code-Barres 1D (OF) -->
        <div v-show="scannerTarget === 'OF'" id="camera-reader" class="w-full h-full"></div>
        <!-- Scanner QR Code Spécialisé Haute Performance (Composants) -->
        <video v-show="scannerTarget === 'QR'" id="qr-video" class="w-full h-full object-cover rounded-3xl"></video>
        
        <div class="absolute inset-0 pointer-events-none border-2 border-blue-500/30 rounded-3xl flex items-center justify-center">
          <div class="w-64 h-64 border-2 border-dashed border-blue-500/60 rounded-2xl relative flex items-center justify-center">
            <div class="absolute -top-1.5 -left-1.5 w-6 h-6 border-t-4 border-l-4 border-blue-500 rounded-tl-md"></div>
            <div class="absolute -top-1.5 -right-1.5 w-6 h-6 border-t-4 border-r-4 border-blue-500 rounded-tr-md"></div>
            <div class="absolute -bottom-1.5 -left-1.5 w-6 h-6 border-b-4 border-l-4 border-blue-500 rounded-bl-md"></div>
            <div class="absolute -bottom-1.5 -right-1.5 w-6 h-6 border-b-4 border-r-4 border-blue-500 rounded-br-md"></div>
            
            <div class="absolute w-[95%] h-0.5 bg-red-500 shadow-md shadow-red-500/80 animate-laser"></div>
          </div>
        </div>
      </div>
      
      <!-- Contrôle Zoom Matériel (si disponible) -->
      <div v-show="hasZoom" class="w-full max-w-xs flex flex-col items-center gap-1.5 bg-white/10 backdrop-blur-md px-4 py-3 rounded-2xl border border-white/10 shadow-lg">
        <span class="text-[10px] text-white/60 font-black uppercase tracking-wider">Ajuster le Zoom</span>
        <div class="w-full flex items-center gap-3">
          <i class="pi pi-search-minus text-white/50 text-xs"></i>
          <input 
            type="range" 
            :min="1.0" 
            :max="maxZoom" 
            step="0.1" 
            v-model="currentZoom" 
            @input="applyZoom"
            class="w-full h-1 bg-white/20 rounded-lg appearance-none cursor-pointer accent-blue-500"
          />
          <i class="pi pi-search-plus text-white/50 text-xs"></i>
          <span class="text-white text-xs font-black w-8 text-right">{{ Number(currentZoom).toFixed(1) }}x</span>
        </div>
      </div>

      <div class="w-full max-w-md text-center text-slate-400 space-y-4">
        <p class="text-xs font-semibold px-4">
          Positionnez le code-barres ou le QR code dans le cadre pour le scanner automatiquement.
        </p>
        <button 
          @click="stopCameraScanner" 
          class="w-full py-4 bg-white/10 hover:bg-white/20 active:scale-95 text-white font-extrabold rounded-2xl uppercase tracking-wider text-xs border border-white/15 transition-all"
        >
          Annuler et Fermer
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, nextTick, computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from 'primevue/usetoast';
import apiClient from '@/services/apiClient';
// CHANGEMENT MAJEUR : Import des formats supportés
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import QrScanner from 'qr-scanner';

const authStore = useAuthStore();
const toast = useToast();

const scannedOf = ref('');
const scannedQr = ref('');
const ofInput = ref(null);
const qrInput = ref(null);
const currentOf = ref(null);
const nomenclature = ref([]);
const preparedItems = ref([]);
const isLoadingOf = ref(false);
const isSaving = ref(false);

// State Caméra Scanner
const showCameraScanner = ref(false);
const scannerTarget = ref(''); // 'OF' ou 'QR'
let html5QrcodeInstance = null;
let qrScannerInstance = null;

// Zoom matériel
const hasZoom = ref(false);
const maxZoom = ref(1.0);
const currentZoom = ref(1.0);

const applyZoom = () => {
  let track = null;
  if (scannerTarget.value === 'QR') {
    const videoElem = document.getElementById("qr-video");
    if (videoElem && videoElem.srcObject) {
      track = videoElem.srcObject.getVideoTracks()[0];
    }
  } else {
    const videoElem = document.querySelector("#camera-reader video");
    if (videoElem && videoElem.srcObject) {
      track = videoElem.srcObject.getVideoTracks()[0];
    }
  }
  
  if (track) {
    track.applyConstraints({ advanced: [{ zoom: Number(currentZoom.value) }] }).catch(console.warn);
  }
};

const startCameraScanner = (target) => {
  scannerTarget.value = target;
  showCameraScanner.value = true;
  
  // Réinitialiser les états de zoom
  hasZoom.value = false;
  maxZoom.value = 1.0;
  currentZoom.value = 1.0;
  
  nextTick(() => {
    if (target === 'QR') {
      doStartQrScanner();
    } else {
      doStartBarcodeScanner();
    }
  });
};

const doStartQrScanner = async () => {
  const videoElem = document.getElementById("qr-video");
  if (!videoElem) {
    console.error("L'élément qr-video est introuvable dans le DOM.");
    toast.add({
      severity: 'error',
      summary: 'Rendu Échoué',
      detail: "Le conteneur vidéo du lecteur QR n'a pas pu s'initialiser.",
      life: 4000
    });
    showCameraScanner.value = false;
    return;
  }

  // Nettoyage de toute instance précédente
  if (qrScannerInstance) {
    try {
      qrScannerInstance.destroy();
    } catch (e) {
      console.warn("Erreur lors de la destruction de l'ancienne instance QR :", e);
    }
    qrScannerInstance = null;
  }

  // Instanciation du lecteur QR Code ultra-performant (Nimiq QrScanner)
  qrScannerInstance = new QrScanner(
    videoElem,
    (result) => {
      handleCameraScanSuccess(result.data);
    },
    {
      onDecodeError: () => {
        // Mode passif et silencieux pour les frames sans décodage
      },
      preferredCamera: 'environment',
      calculateScanRegion: (v) => {
        // Cadrer un carré de 60% au centre de la vidéo
        const smallestDimension = Math.min(v.videoWidth, v.videoHeight);
        const scanRegionSize = Math.round(smallestDimension * 0.6);
        return {
          x: Math.round((v.videoWidth - scanRegionSize) / 2),
          y: Math.round((v.videoHeight - scanRegionSize) / 2),
          width: scanRegionSize,
          height: scanRegionSize,
          downScaleTo: 1000 // Préserver une TRÈS haute densité de pixels pour détecter les QR codes très petits / lointains !
        };
      },
      highlightScanRegion: true,
      highlightCodeOutline: true,
      maxDetectedCodesPerFrame: 1
    }
  );

  try {
    // Demander manuellement le flux avec une résolution FULL HD et autofocus continu poussé
    // Cela force le téléphone à faire le point (netteté) en continu sur le QR code
    const stream = await navigator.mediaDevices.getUserMedia({
      video: {
        facingMode: { ideal: "environment" },
        width: { ideal: 1920 }, // Force la haute résolution Full HD
        height: { ideal: 1080 },
        advanced: [
          { focusMode: "continuous" }
        ]
      }
    });

    await qrScannerInstance.start(stream);

    // Attendre un court instant (350ms) pour laisser le capteur mobile s'initialiser complètement
    setTimeout(async () => {
      const track = stream.getVideoTracks()[0];
      if (track) {
        const capabilities = track.getCapabilities ? track.getCapabilities() : {};
        const advancedConstraints = [];
        if (capabilities.focusMode && capabilities.focusMode.includes('continuous')) {
          advancedConstraints.push({ focusMode: 'continuous' });
        }
        if (capabilities.zoom) {
          hasZoom.value = true;
          maxZoom.value = capabilities.zoom.max;
          // Zoom initial automatique de 8.0x (ou le max disponible s'il est inférieur)
          currentZoom.value = Math.min(capabilities.zoom.max, 8.0);
          advancedConstraints.push({ zoom: currentZoom.value });
        }
        if (advancedConstraints.length > 0) {
          await track.applyConstraints({ advanced: advancedConstraints }).catch(console.warn);
        }
      }
    }, 350);
  } catch (err) {
    console.warn("Échec du démarrage avec contraintes Full HD manuelles, tentative par défaut de QrScanner...", err);
    // Fallback : laisser QrScanner ouvrir la caméra par défaut
    qrScannerInstance.start().then(() => {
      setTimeout(() => {
        try {
          const stream = videoElem.srcObject;
          if (stream) {
            const track = stream.getVideoTracks()[0];
            if (track) {
              const capabilities = track.getCapabilities ? track.getCapabilities() : {};
              const advancedConstraints = [];
              if (capabilities.focusMode && capabilities.focusMode.includes('continuous')) {
                advancedConstraints.push({ focusMode: 'continuous' });
              }
              if (capabilities.zoom) {
                hasZoom.value = true;
                maxZoom.value = capabilities.zoom.max;
                // Zoom initial automatique de 8.0x (ou le max disponible s'il est inférieur)
                currentZoom.value = Math.min(capabilities.zoom.max, 8.0);
                advancedConstraints.push({ zoom: currentZoom.value });
              }
              if (advancedConstraints.length > 0) {
                track.applyConstraints({ advanced: advancedConstraints }).catch(console.warn);
              }
            }
          }
        } catch (e) {
          console.warn(e);
        }
      }, 350);
    }).catch(defaultErr => {
      console.error("Échec complet du démarrage du lecteur QR :", defaultErr);
      handleCameraError(defaultErr);
    });
  }
};

const doStartBarcodeScanner = () => {
  const element = document.getElementById("camera-reader");
  if (!element) {
    console.error("L'élément camera-reader est introuvable dans le DOM.");
    toast.add({
      severity: 'error',
      summary: 'Rendu Échoué',
      detail: "Le conteneur du lecteur code-barres n'a pas pu s'initialiser.",
      life: 4000
    });
    showCameraScanner.value = false;
    return;
  }

  if (html5QrcodeInstance && html5QrcodeInstance.isScanning) {
    html5QrcodeInstance.stop().then(() => {
      html5QrcodeInstance = null;
      nextTick(() => {
        initHtml5BarcodeScanner();
      });
    }).catch(err => {
      console.warn("Échec d'arrêt propre du scanner de code-barres :", err);
      html5QrcodeInstance = null;
      nextTick(() => {
        initHtml5BarcodeScanner();
      });
    });
  } else {
    html5QrcodeInstance = null;
    initHtml5BarcodeScanner();
  }
};

const initHtml5BarcodeScanner = () => {
  if (!html5QrcodeInstance) {
    html5QrcodeInstance = new Html5Qrcode("camera-reader");
  }

  // Configuration optimisée spécifiquement pour le scan de codes-barres 1D (OF)
  const config = {
    fps: 25,
    qrbox: (width, height) => {
      const minEdge = Math.min(width, height);
      const qrboxSize = Math.floor(minEdge * 0.85);
      return { width: qrboxSize, height: qrboxSize };
    },
    aspectRatio: 1.0,
    formatsToSupport: [
      Html5QrcodeSupportedFormats.CODE_128,
      Html5QrcodeSupportedFormats.CODE_39,
      Html5QrcodeSupportedFormats.EAN_13
    ],
    experimentalFeatures: {
      useBarCodeDetectorIfSupported: true
    }
  };

  const videoConstraints = {
    facingMode: "environment",
    width: { ideal: 1280 },
    height: { ideal: 720 },
    advanced: [{ focusMode: "continuous" }]
  };

  html5QrcodeInstance.start(
    videoConstraints,
    config,
    (decodedText) => {
      handleCameraScanSuccess(decodedText);
    },
    () => {
      // scan passif silencieux
    }
  ).then(() => {
    try {
      const scannerVideo = document.querySelector("#camera-reader video");
      if (scannerVideo && scannerVideo.srcObject) {
        const stream = scannerVideo.srcObject;
        const track = stream.getVideoTracks()[0];
        if (track) {
          const capabilities = track.getCapabilities ? track.getCapabilities() : {};
          const constraints = {};
          if (capabilities.focusMode && capabilities.focusMode.includes('continuous')) {
            constraints.focusMode = 'continuous';
          }
          if (capabilities.zoom) {
            hasZoom.value = true;
            maxZoom.value = capabilities.zoom.max;
            currentZoom.value = Math.min(capabilities.zoom.max, 1.25);
            constraints.zoom = currentZoom.value;
          }
          if (Object.keys(constraints).length > 0) {
            track.applyConstraints({ advanced: [constraints] }).catch(console.warn);
          }
        }
      }
    } catch (e) {
      console.warn("Mise au point dynamique non supportée :", e);
    }
  }).catch(err => {
    console.warn("Échec du démarrage direct du code-barres, passage au fallback...", err);
    html5QrcodeInstance = null;
    nextTick(() => {
      if (!html5QrcodeInstance) {
        html5QrcodeInstance = new Html5Qrcode("camera-reader");
      }
      Html5Qrcode.getCameras().then(cameras => {
        if (cameras && cameras.length > 0) {
          const backCamera = cameras.find(cam => 
            cam.label.toLowerCase().includes('back') || 
            cam.label.toLowerCase().includes('rear') || 
            cam.label.toLowerCase().includes('arrière') ||
            cam.label.toLowerCase().includes('environment')
          );
          const cameraId = backCamera ? backCamera.id : cameras[cameras.length - 1].id;
          
          html5QrcodeInstance.start(
            cameraId,
            config,
            (decodedText) => {
              handleCameraScanSuccess(decodedText);
            },
            () => {}
          ).then(() => {
            try {
              const scannerVideo = document.querySelector("#camera-reader video");
              if (scannerVideo && scannerVideo.srcObject) {
                const stream = scannerVideo.srcObject;
                const track = stream.getVideoTracks()[0];
                if (track) {
                  const capabilities = track.getCapabilities ? track.getCapabilities() : {};
                  const constraints = {};
                  if (capabilities.focusMode && capabilities.focusMode.includes('continuous')) {
                    constraints.focusMode = 'continuous';
                  }
                  if (capabilities.zoom) {
                    hasZoom.value = true;
                    maxZoom.value = capabilities.zoom.max;
                    currentZoom.value = Math.min(capabilities.zoom.max, 1.25);
                    constraints.zoom = currentZoom.value;
                  }
                  if (Object.keys(constraints).length > 0) {
                    track.applyConstraints({ advanced: [constraints] }).catch(console.warn);
                  }
                }
              }
            } catch (e) {
              console.warn(e);
            }
          }).catch(fallbackErr => {
            handleCameraError(fallbackErr);
          });
        } else {
          handleCameraError(err);
        }
      }).catch(enumErr => {
        handleCameraError(enumErr);
      });
    });
  });
};

const handleCameraError = (err) => {
  console.error("Détails Erreur Caméra :", err);
  let detailMsg = "Impossible d'accéder à la caméra. ";
  
  if (window.location.protocol !== 'https:' && window.location.hostname !== 'localhost') {
    detailMsg += "⚠️ ATTENTION : Les navigateurs mobiles bloquent l'accès à la caméra sur les connexions non sécurisées. Vous devez impérativement ouvrir l'URL Ngrok en HTTPS !";
  } else {
    detailMsg += "Veuillez vérifier que l'autorisation d'appareil photo est accordée à ce site dans les paramètres de votre navigateur mobile.";
  }
  
  toast.add({
    severity: 'error',
    summary: 'Accès Caméra Refusé',
    detail: detailMsg,
    life: 12000
  });
  showCameraScanner.value = false;
};

const handleCameraScanSuccess = (decodedText) => {
  if (navigator.vibrate) {
    navigator.vibrate(100);
  }
  
  if (scannerTarget.value === 'OF') {
    scannedOf.value = decodedText;
    stopCameraScanner();
    handleOfScan();
  } else if (scannerTarget.value === 'QR') {
    scannedQr.value = decodedText;
    stopCameraScanner();
    handleQrScan();
  }
};

const stopCameraScanner = () => {
  // Arrêter le lecteur QR Code
  if (qrScannerInstance) {
    try {
      qrScannerInstance.stop();
      qrScannerInstance.destroy();
    } catch (e) {
      console.warn("Erreur lors de l'arrêt de qrScanner :", e);
    }
    qrScannerInstance = null;
  }

  // Arrêter le lecteur Code-barres
  if (html5QrcodeInstance) {
    if (html5QrcodeInstance.isScanning) {
      html5QrcodeInstance.stop().then(() => {
        showCameraScanner.value = false;
      }).catch(err => {
        console.error("Erreur lors de l'arrêt du html5Qrcode :", err);
        showCameraScanner.value = false;
      });
    } else {
      showCameraScanner.value = false;
    }
  } else {
    showCameraScanner.value = false;
  }
};

onUnmounted(() => {
  if (qrScannerInstance) {
    try {
      qrScannerInstance.stop();
      qrScannerInstance.destroy();
    } catch (e) {}
  }
  if (html5QrcodeInstance && html5QrcodeInstance.isScanning) {
    html5QrcodeInstance.stop().catch(console.error);
  }
});

// Autofocus l'entrée de l'OF au démarrage
onMounted(() => {
  focusOfInput();
});

const focusOfInput = () => {
  nextTick(() => {
    ofInput.value?.focus();
  });
};

const focusQrInput = () => {
  nextTick(() => {
    qrInput.value?.focus();
  });
};

// Vérifier si au moins un composant a été préparé pour autoriser la validation
const hasPreparedItems = computed(() => {
  return preparedItems.value.length > 0;
});

// Appel API pour charger l'OF scanné
const handleOfScan = async () => {
  if (!scannedOf.value) return;
  
  isLoadingOf.value = true;
  const ofNum = scannedOf.value.trim().toUpperCase();

  try {
    const response = await apiClient.get(`/magasinier/of/${ofNum}`);
    
    if (response.data && response.data.success) {
      const data = response.data.data;
      currentOf.value = {
        numeroOF: data.numeroOF,
        articleCode: data.articleCode,
        articleDesignation: data.articleDesignation,
        articleNatureCode: data.articleNatureCode,
        articleNatureLibelle: data.articleNatureLibelle,
        quantitePrevue: data.quantitePrevue
      };

      nomenclature.value = data.nomenclature.map(n => ({
        code: n.code,
        designation: n.designation,
        quantite: n.quantite,
        nature: n.nature
      }));

      preparedItems.value = [];

      toast.add({ 
        severity: 'success', 
        summary: 'OF Chargé avec succès', 
        detail: `L'OF ${ofNum} a été identifié dans l'ERP SAGE X3. Prêt pour la préparation !`, 
        life: 4000 
      });

      scannedOf.value = '';
      focusQrInput();
    }
  } catch (error) {
    console.error("Erreur lors du scan de l'OF :", error);
    
    toast.add({ 
      severity: 'error', 
      summary: 'OF Non Trouvé dans l\'ERP', 
      detail: error.response?.data?.message || `L'ordre de fabrication '${ofNum}' n'existe pas ou n'est pas actif dans SAGE X3.`, 
      life: 6000 
    });
    scannedOf.value = '';
    currentOf.value = null;
    nomenclature.value = [];
    preparedItems.value = [];
    focusOfInput();
  } finally {
    isLoadingOf.value = false;
  }
};

// Scan de composant / QR Code
const handleQrScan = () => {
  if (!scannedQr.value || !currentOf.value) return;

  const scannedVal = scannedQr.value.trim();
  let artCode = '';
  let lotNum = '';
  let qcReport = '';

  if (scannedVal.toLowerCase().includes('lot:') && scannedVal.toLowerCase().includes('/cd:')) {
    const slashParts = scannedVal.split('/');
    for (const part of slashParts) {
      const trimmedPart = part.trim();
      if (trimmedPart.toLowerCase().startsWith('lot:')) {
        lotNum = trimmedPart.substring(4).trim();
      } else if (trimmedPart.toLowerCase().startsWith('cd:')) {
        artCode = trimmedPart.substring(3).trim();
      } else if (trimmedPart.toLowerCase().startsWith('rpt:')) {
        qcReport = trimmedPart.substring(4).trim();
      }
    }
  } else {
    const parts = scannedVal.split(';').map(p => p.trim());
    if (parts.length >= 2) {
      artCode = parts[0];
      lotNum = parts[1];
      qcReport = parts.length > 2 ? parts[2] : '';
    }
  }

  if (!artCode || !lotNum) {
    toast.add({ 
      severity: 'error', 
      summary: 'Format de QR Inconnu', 
      detail: `Reçu: "${scannedVal}". Format attendu: "lot:XXXX/cd:YYYY/Rpt:ZZZZ" ou "CodeArticle;Lot;QC"`, 
      life: 8000 
    });
    scannedQr.value = '';
    return;
  }

  const refComponent = nomenclature.value.find(c => 
    c.code.toLowerCase() === artCode.toLowerCase() ||
    c.code.toLowerCase().startsWith(artCode.toLowerCase()) ||
    artCode.toLowerCase().startsWith(c.code.toLowerCase())
  );

  if (refComponent) {
    const existingPrepared = preparedItems.value.find(item => item.code.toLowerCase() === refComponent.code.toLowerCase());

    if (existingPrepared) {
      const existingLots = existingPrepared.lots.split(';').map(l => l.trim());
      if (!existingLots.includes(lotNum)) {
        existingPrepared.lots += `; ${lotNum}`;
        if (qcReport) {
          existingPrepared.rapportQC = qcReport;
        }
        toast.add({ 
          severity: 'success', 
          summary: 'Nouveau lot associé', 
          detail: `Lot ${lotNum} associé à ${refComponent.designation}`, 
          life: 3000 
        });
      } else {
        toast.add({ 
          severity: 'info', 
          summary: 'Lot Déjà Enregistré', 
          detail: `Le lot ${lotNum} a déjà été scanné pour cet article.`, 
          life: 3000 
        });
      }
    } else {
      preparedItems.value.push({
        code: refComponent.code,
        designation: refComponent.designation,
        nature: refComponent.nature,
        quantite: refComponent.quantite,
        lots: lotNum,
        rapportQC: qcReport,
        isScanned: true
      });

      toast.add({ 
        severity: 'success', 
        summary: 'Composant validé', 
        detail: `${refComponent.designation} (Lot: ${lotNum}) ajouté au panier.`, 
        life: 3000 
      });
    }
  } else {
    toast.add({ 
      severity: 'error', 
      summary: 'Article Invalide !', 
      detail: `L'article '${artCode}' ne fait pas partie de la nomenclature de cet ordre de fabrication !`, 
      life: 7000 
    });
    
    if (navigator.vibrate) {
      navigator.vibrate([200, 100, 200]);
    }
  }

  scannedQr.value = '';
  focusQrInput();
};

const removePreparedItem = (index) => {
  const item = preparedItems.value[index];
  preparedItems.value.splice(index, 1);
  toast.add({
    severity: 'info',
    summary: 'Article retiré',
    detail: `${item.designation} a été retiré de la préparation.`,
    life: 3000
  });
};

const savePreparation = async () => {
  isSaving.value = true;
  
  try {
    const payload = {
      numeroOF: currentOf.value.numeroOF,
      matriculeMagasinier: authStore.user?.matricule || 'MAG01',
      items: preparedItems.value.map(c => ({
        code: c.code,
        lots: c.lots,
        quantite: c.quantite,
        rapportQC: c.rapportQC || ''
      }))
    };

    const response = await apiClient.post('/magasinier/preparation', payload);

    if (response.data && response.data.success) {
      toast.add({ 
        severity: 'success', 
        summary: 'Sortie Enregistrée !', 
        detail: 'La préparation de sortie a été validée avec succès dans l\'ERP SAGE X3.', 
        life: 5000 
      });

      currentOf.value = null;
      nomenclature.value = [];
      preparedItems.value = [];
      focusOfInput();
    }
  } catch (error) {
    console.error('Erreur de validation de préparation :', error);
    toast.add({ 
      severity: 'error', 
      summary: 'Échec Enregistrement', 
      detail: error.response?.data?.message || 'Impossible de valider la sortie de stock.', 
      life: 5000 
    });
  } finally {
    isSaving.value = false;
  }
};

const handleLogout = () => {
  authStore.logout();
};
</script>

<style scoped>
/* Animations fluides */
.animate-pulse {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: .5; }
}

@keyframes laser {
  0% { top: 5%; }
  50% { top: 95%; }
  100% { top: 5%; }
}
.animate-laser {
  animation: laser 2.5s ease-in-out infinite;
}

/* CHANGEMENT MAJEUR : Custom styling for html5-qrcode video viewport avec booster de scan */
:deep(#camera-reader video) {
  width: 100% !important;
  height: 100% !important;
  object-fit: cover !important;
  border-radius: 1.5rem !important;
  filter: contrast(1.2) brightness(1.1); /* BOOSTER de scan */
}

:deep(#camera-reader) {
  border: none !important;
}
</style>