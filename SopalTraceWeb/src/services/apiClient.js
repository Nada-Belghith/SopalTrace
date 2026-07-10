import axios from 'axios';
import { logger } from '@/utils/logger'; // <-- 1. IMPORT DU LOGGER

// Configuration de base : Utilisation de chemins relatifs /api pour éviter les problèmes de CORS et d'adresses en dur
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  },
  withCredentials: true, // IMPORTANT : Pour envoyer le cookie refreshToken
  timeout: 10000
});

let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Intercepteur de requêtes
apiClient.interceptors.request.use(
  (config) => {
    // Log des requêtes sortantes
    logger.debug(`➡️ REQUÊTE : ${config.method?.toUpperCase()} ${config.url}`);

    // Injection du Token JWT si présent
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

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
  async (error) => {
    const originalRequest = error.config;

    // Si on reçoit 401 et ce n'est pas déjà un retry ni un call auth
    if (error.response && error.response.status === 401 && !originalRequest._retry && !originalRequest.url.includes('/auth/')) {

      // Vérifier si un OF est EN_COURS
      let hasActiveOf = false;
      try {
        const { useOperateurStore } = await import('@/stores/execution/operateurStore');
        const store = useOperateurStore();
        if (store && store.ofs) {
          hasActiveOf = store.ofs.some(of =>
            of.gammeOperatoire && of.gammeOperatoire.some(op => op.activeExecStatut === 'EN_COURS')
          );
        }
      } catch (e) {
        console.warn("Impossible de vérifier le statut des OFs pour le refresh token", e);
      }

      // S'il n'y a PAS d'OF en cours (càd en pause, clôturé, ou vide), on procède à la déconnexion normale
      if (!hasActiveOf) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');

        if (window.location.pathname !== '/login' && window.location.pathname !== '/forgot-password') {
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise(function (resolve, reject) {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return apiClient(originalRequest);
        }).catch(err => {
          return Promise.reject(err);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const oldToken = localStorage.getItem('token');

      try {
        const { data } = await axios.post((import.meta.env.VITE_API_URL || '/api') + '/auth/refresh', { token: oldToken }, { withCredentials: true });

        const newToken = data.token;
        localStorage.setItem('token', newToken);

        apiClient.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
        originalRequest.headers.Authorization = `Bearer ${newToken}`;

        processQueue(null, newToken);

        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);

        // Échec du refresh -> vraie déconnexion
        localStorage.removeItem('token');
        localStorage.removeItem('user');

        if (window.location.pathname !== '/login' && window.location.pathname !== '/forgot-password') {
          window.location.href = '/login';
        }

        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

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
          logger.warn(`Validation échouée (${apiError.status})`, apiError.details);
          break;
        case 401:
          // Géré par le retry au-dessus, on n'arrive ici que si le refresh échoue
          apiError.message = "Session expirée ou code invalide.";
          logger.warn("Non autorisé (401).");
          break;
        case 403:
          apiError.message = "Vous n'avez pas les droits nécessaires pour cette action.";
          logger.error("Accès refusé (403)");
          break;
        case 500:
          apiError.message = "Erreur interne du serveur SopalTrace.";
          logger.error("Crash serveur (500)", data);
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
