import axios from 'axios';
import { logger } from '@/utils/logger'; // <-- 1. IMPORT DU LOGGER

// Configuration de base
const apiClient = axios.create({
  baseURL: 'http://localhost:9091/api',
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  },
  timeout: 10000 
});

// Intercepteur de requêtes
apiClient.interceptors.request.use(
  (config) => {
      // Log des requêtes sortantes
    logger.debug(`➡️ REQUÊTE : ${config.method?.toUpperCase()} ${config.url}`);
    
    return config;
  },
  (error) => Promise.reject(error)
);

// Intercepteur de réponses
apiClient.interceptors.response.use(
  (response) => {
    logger.debug(`✅ RÉPONSE : ${response.config.url}`, response.status);
    return response;
  },
  (error) => {
    const apiError = {
      message: "Une erreur inattendue est survenue.",
      status: error.response?.status || 500,
      details: []
    };

    if (error.response) {
      const data = error.response.data;
      switch (apiError.status) {
        case 400: 
          apiError.message = data.error || data.message || "Données invalides.";
          apiError.details = data.details || [];
          logger.warn(`Validation échouée (${apiError.status})`, apiError.details); // LOG AVERTISSEMENT
          break;
        case 401:
          apiError.message = "Session expirée, veuillez vous reconnecter.";
          logger.warn("Non autorisé (401). Déconnexion requise.");
          break;
        case 500:
          apiError.message = "Erreur interne du serveur SopalTrace.";
          logger.error("Crash serveur (500)", data); // LOG CRITIQUE
          break;
        default:
          logger.error(`Erreur HTTP ${apiError.status}`, data);
          break;
      }
    } else if (error.request) {
      apiError.message = "Impossible de joindre le serveur. Vérifiez votre connexion réseau.";
      logger.error("Timeout ou serveur injoignable", error.request);
    }

    return Promise.reject(apiError);
  }
);

export default apiClient;
