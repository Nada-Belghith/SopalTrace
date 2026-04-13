/**
 * Service centralisé de journalisation (Logging)
 * Permet de désactiver les logs en production ou de les envoyer vers l'API.
 */
const isDev = import.meta.env.DEV; // Vite.js détecte si on est en développement

class LoggerService {
  info(message, data = null) {
    if (isDev) {
      console.info(`[📘 INFO] ${message}`, data ? data : '');
    }
  }

  warn(message, data = null) {
    if (isDev) {
      console.warn(`[📙 WARN] ${message}`, data ? data : '');
    }
  }

  error(message, errorObj = null) {
    // Les erreurs s'affichent toujours, même en prod (dans la console du navigateur)
    console.error(`[📕 ERROR] ${message}`, errorObj ? errorObj : '');

    if (!isDev) {
      // TODO (Optionnel) : Envoyer l'erreur à votre API C# pour garder une trace
      // apiClient.post('/logs/frontend', { message, error: errorObj?.stack });
    }
  }

  debug(message, data = null) {
    if (isDev) {
      console.debug(`[📓 DEBUG] ${message}`, data ? data : '');
    }
  }
}

export const logger = new LoggerService();