import apiClient from './apiClient';

export const fabPlanService = {
  // Récupérer les dictionnaires
  getDictionnaires() {
    return apiClient.get('/referentiels/fabrication');
  },

  // Sauvegarder le modèle
  creerModele(payload) {
    return apiClient.post('/plans-fabrication/modeles', payload);
  },

  getModeleById(id) {
    return apiClient.get(`/plans-fabrication/modeles/${id}`);
  },

  nouvelleVersionModele(payload) {
    return apiClient.post('/plans-fabrication/modeles/nouvelle-version', payload);
  }
};
