import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:9091/api';

export const authService = {
  /**
   * Connexion avec matricule et mot de passe
   */
  async login(matricule, motDePasse) {
    const response = await axios.post(`${API_URL}/auth/login`, {
      matricule,
      motDePasse
    });
    return response.data;
  },

  /**
   * Inscription d'un nouvel utilisateur
   */
  async register(matricule, email, motDePasse) {
    const response = await axios.post(`${API_URL}/auth/register`, {
      matricule,
      email,
      motDePasse
    });
    return response.data;
  },

  /**
   * Déconnexion
   */
  async logout() {
    // L'intercepteur ajoutera le token automatiquement
    const response = await axios.post(`${API_URL}/auth/logout`);
    return response.data;
  },

  /**
   * Rafraîchissement du token
   */
  async refreshToken(token) {
    const response = await axios.post(`${API_URL}/auth/refresh`, { token });
    return response.data;
  },

  /**
   * Demande de réinitialisation de mot de passe (envoi code)
   */
  async forgotPassword(email) {
    const response = await axios.post(`${API_URL}/auth/forgot-password`, { email });
    return response.data;
  },

  /**
   * Réinitialisation effective du mot de passe avec le code reçu
   */
  async resetPassword(email, code, nouveauMotDePasse) {
    const response = await axios.post(`${API_URL}/auth/reset-password`, {
      email,
      code,
      nouveauMotDePasse
    });
    return response.data;
  }
};
