import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import ConfirmationService from 'primevue/confirmationservice';
import { logger } from '@/utils/logger'; // <-- 1. IMPORT DU LOGGER

// --- Imports PrimeVue ---
import PrimeVue from 'primevue/config'
import Aura from '@primevue/themes/aura'
import ToastService from 'primevue/toastservice'
import 'primeicons/primeicons.css'

// --- Import Tailwind ---
import './assets/main.css' 

const app = createApp(App)

// <-- 2. GESTIONNAIRE D'ERREURS GLOBAL VUE 3 -->
// Attrape toutes les erreurs JS non gérées dans l'interface UI
app.config.errorHandler = (err, instance, info) => {
    logger.error(`[Vue Global Error] Info: ${info}`, err);
};
app.use(ConfirmationService);
app.use(createPinia())
app.use(router)

// Configuration de PrimeVue avec le thème Aura
app.use(PrimeVue, {
    theme: {
        preset: Aura,
        options: {
            darkModeSelector: '.fake-dark-selector', // Optionnel : désactive le dark mode auto
        }
    }
})
app.use(ToastService) // Activation des notifications

app.mount('#app')
