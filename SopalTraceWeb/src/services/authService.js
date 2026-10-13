import apiClient from './apiClient';

export const authService = {
  /**
   * Connexion avec matricule et mot de passe
   */
  async login(matricule, motDePasse) {
    const response = await apiClient.post('/auth/login', {
      matricule,
      motDePasse
    });
    return response.data;
  },

  /**
   * Demande d'accès : réponse identique dans tous les cas (le serveur ne confirme pas l'existence du matricule)
   */
  async register(matricule, email) {
    const response = await apiClient.post('/auth/register', {
      matricule,
      email
    });
    return response.data;
  },

  /**
   * Activation du compte : jeton du lien reçu par e-mail (usage unique) et mot de passe choisi
   */
  async setupPassword(token, nouveauMotDePasse) {
    const response = await apiClient.post('/auth/setup-password', {
      token,
      nouveauMotDePasse
    });
    return response.data;
  },

  /**
   * Déconnexion
   */
  async logout() {
    const response = await apiClient.post('/auth/logout');
    return response.data;
  },

  /**
   * Rafraîchissement du token
   */
  async refreshToken(token) {
    const response = await apiClient.post('/auth/refresh', { token });
    return response.data;
  },

  /**
   * Demande de réinitialisation de mot de passe (envoi code)
   */
  async forgotPassword(email) {
    const response = await apiClient.post('/auth/forgot-password', { email });
    return response.data;
  },

  /**
   * Réinitialisation effective du mot de passe avec le code reçu
   */
  async resetPassword(email, code, nouveauMotDePasse) {
    const response = await apiClient.post('/auth/reset-password', {
      email,
      code,
      nouveauMotDePasse
    });
    return response.data;
  }
};
