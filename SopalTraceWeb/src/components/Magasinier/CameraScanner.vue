<template>
  <div v-show="visible" class="fixed inset-0 bg-black/95 z-50 flex flex-col items-center justify-between p-6 font-sans">
    <div class="w-full max-w-md flex items-center justify-between text-white border-b border-white/10 pb-4">
      <div class="flex items-center gap-3">
        <i class="pi pi-camera text-xl text-blue-400 animate-pulse"></i>
        <div>
          <h3 class="font-extrabold text-sm uppercase tracking-wide">Scanner par Caméra</h3>
          <p class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">
            {{ target === 'OF' ? "Étape 1 : Ordre de Fab (OF)" : "Étape 2 : Composant / Lot" }}
          </p>
        </div>
      </div>
      <button @click="closeScanner" class="p-2 bg-white/10 hover:bg-white/20 active:scale-95 text-white rounded-xl transition-all">
        <i class="pi pi-times text-lg"></i>
      </button>
    </div>

    <div class="relative w-full max-w-sm aspect-square bg-[#0f172a] rounded-3xl border border-white/20 overflow-hidden shadow-2xl">
      <!-- Scanner Code-Barres 1D (OF) -->
      <div v-show="target === 'OF'" id="camera-reader" class="w-full h-full"></div>
      <!-- Scanner QR Code Spécialisé Haute Performance (Composants) -->
      <video v-show="target === 'QR'" id="qr-video" class="w-full h-full object-cover rounded-3xl"></video>
      
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
        @click="closeScanner" 
        class="w-full py-4 bg-white/10 hover:bg-white/20 active:scale-95 text-white font-extrabold rounded-2xl uppercase tracking-wider text-xs border border-white/15 transition-all"
      >
        Annuler et Fermer
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, nextTick, onUnmounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import QrScanner from 'qr-scanner';

const props = defineProps({
  visible: {
    type: Boolean,
    required: true
  },
  target: {
    type: String,
    required: true // 'OF' ou 'QR'
  }
});

const emit = defineEmits(['scanned', 'close']);
const toast = useToast();

const hasZoom = ref(false);
const maxZoom = ref(1.0);
const currentZoom = ref(1.0);

let html5QrcodeInstance = null;
let qrScannerInstance = null;

const applyZoom = () => {
  let track = null;
  if (props.target === 'QR') {
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

const startScanner = () => {
  hasZoom.value = false;
  maxZoom.value = 1.0;
  currentZoom.value = 1.0;
  
  nextTick(() => {
    if (props.target === 'QR') {
      doStartQrScanner();
    } else {
      doStartBarcodeScanner();
    }
  });
};

const stopScanner = () => {
  if (qrScannerInstance) {
    try {
      qrScannerInstance.stop();
      qrScannerInstance.destroy();
    } catch {
      // Ignorer
    }
    qrScannerInstance = null;
  }

  if (html5QrcodeInstance) {
    if (html5QrcodeInstance.isScanning) {
      html5QrcodeInstance.stop().catch(err => {
        console.error("Erreur lors de l'arrêt du html5Qrcode :", err);
      });
    }
  }
};

const closeScanner = () => {
  stopScanner();
  emit('close');
};

const handleCameraScanSuccess = (decodedText) => {
  if (navigator.vibrate) {
    navigator.vibrate(100);
  }
  stopScanner();
  emit('scanned', decodedText);
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
  closeScanner();
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
    closeScanner();
    return;
  }

  if (qrScannerInstance) {
    try {
      qrScannerInstance.destroy();
    } catch (e) {
      console.warn(e);
    }
    qrScannerInstance = null;
  }

  qrScannerInstance = new QrScanner(
    videoElem,
    (result) => {
      handleCameraScanSuccess(result.data);
    },
    {
      onDecodeError: () => {},
      preferredCamera: 'environment',
      calculateScanRegion: (v) => {
        const smallestDimension = Math.min(v.videoWidth, v.videoHeight);
        const scanRegionSize = Math.round(smallestDimension * 0.6);
        return {
          x: Math.round((v.videoWidth - scanRegionSize) / 2),
          y: Math.round((v.videoHeight - scanRegionSize) / 2),
          width: scanRegionSize,
          height: scanRegionSize,
          downScaleTo: 1000
        };
      },
      highlightScanRegion: true,
      highlightCodeOutline: true,
      maxDetectedCodesPerFrame: 1
    }
  );

  try {
    const stream = await navigator.mediaDevices.getUserMedia({
      video: {
        facingMode: { ideal: "environment" },
        width: { ideal: 1920 },
        height: { ideal: 1080 },
        advanced: [
          { focusMode: "continuous" }
        ]
      }
    });

    await qrScannerInstance.start(stream);

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
          currentZoom.value = Math.min(capabilities.zoom.max, 8.0);
          advancedConstraints.push({ zoom: currentZoom.value });
        }
        if (advancedConstraints.length > 0) {
          await track.applyConstraints({ advanced: advancedConstraints }).catch(console.warn);
        }
      }
    }, 350);
  } catch (err) {
    console.warn("Fallback default QrScanner...", err);
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
    closeScanner();
    return;
  }

  if (html5QrcodeInstance && html5QrcodeInstance.isScanning) {
    html5QrcodeInstance.stop().then(() => {
      html5QrcodeInstance = null;
      nextTick(() => {
        initHtml5BarcodeScanner();
      });
    }).catch(err => {
      console.warn(err);
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
  }).catch(err => {
    console.warn("Fallback barcode camera selection...", err);
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

watch(() => props.visible, (newVal) => {
  if (newVal) {
    startScanner();
  } else {
    stopScanner();
  }
});

onUnmounted(() => {
  stopScanner();
});
</script>

<style scoped>
@keyframes laser {
  0% { top: 5%; }
  50% { top: 95%; }
  100% { top: 5%; }
}
.animate-laser {
  animation: laser 2.5s ease-in-out infinite;
}

:deep(#camera-reader video) {
  width: 100% !important;
  height: 100% !important;
  object-fit: cover !important;
  border-radius: 1.5rem !important;
  filter: contrast(1.2) brightness(1.1);
}

:deep(#camera-reader) {
  border: none !important;
}
</style>
