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

      // (La vérification hasActiveOf a été supprimée car elle empêchait le refresh token pour les utilisateurs normaux)


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
        // Le rôle fait foi côté serveur : on garde la copie locale alignée (menus et gardes de route).
        try {
          const user = JSON.parse(localStorage.getItem('user') || 'null');
          if (user && data.roleApp) localStorage.setItem('user', JSON.stringify({ ...user, role: data.roleApp, nom: data.nomComplet || user.nom }));
        } catch { /* copie locale illisible : elle sera réécrite à la prochaine connexion */ }

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
      const data = error.response.data || {};
      // Erreurs de validation ASP.NET (ProblemDetails) : { errors: { Champ: ["message"] } }
      const premiereErreurValidation = data.errors ? Object.values(data.errors).flat()[0] : null;
      switch (apiError.status) {
        case 400:
          // Erreurs FluentValidation : message générique + détail par règle → on affiche le détail.
          apiError.message = (Array.isArray(data.details) && data.details.length ? data.details.join(' ') : null)
            || data.error || data.message || premiereErreurValidation || "Données invalides.";
          apiError.details = data.details || (data.errors ? Object.values(data.errors).flat() : []);
          logger.warn(`Validation échouée (${apiError.status})`, apiError.details);
          break;
        case 404:
        case 409:
          apiError.message = data.message || data.error || apiError.message;
          logger.warn(`Erreur métier (${apiError.status})`, apiError.message);
          break;
        case 401:
          // Géré par le retry au-dessus, on n'arrive ici que si le refresh échoue
          apiError.message = data.error || data.message || "Session expirée ou code invalide.";
          logger.warn("Non autorisé (401).");
          break;
        case 403:
          apiError.message = "Vous n'avez pas les droits nécessaires pour cette action.";
          logger.error("Accès refusé (403)");
          break;
        case 429:
          apiError.message = "Trop de requêtes. Patientez quelques instants puis réessayez.";
          break;
        case 500:
          apiError.message = data.traceId
            ? `Erreur interne du serveur SopalTrace (trace ${data.traceId}).`
            : "Erreur interne du serveur SopalTrace.";
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

    // Forme compatible axios : les écrans qui lisent error.response.data.message reçoivent le message de l'API
    // (règle métier, validation, conflit de version) au lieu de leur texte générique.
    apiError.response = { status: apiError.status, data: { ...(error.response?.data || {}), message: apiError.message } };

    return Promise.reject(apiError);
  }
);

export default apiClient;
