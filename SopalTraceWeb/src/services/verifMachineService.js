import apiClient from './apiClient';

export const verifMachineService = {
  // Dictionnaires (machines, périodicités, etc.)
  getDictionnaires: () => apiClient.get('/referentiels/verif-machine'),

  // CRUD Plans de vérification machine
  getTousLesPlans: () => apiClient.get('/plans-verif-machine'),

  creerPlanVerif: (payload) => apiClient.post('/plans-verif-machine', payload),

  getPlanVerif: (id) => apiClient.get(`/plans-verif-machine/${id}`),

  mettreAJourPlanVerif: (id, payload) => apiClient.put(`/plans-verif-machine/${id}`, payload),

  creerNouvelleVersion: (id, payload) => apiClient.post(`/plans-verif-machine/${id}/nouvelle-version`, payload),

  archiverPlanVerif: (id) => apiClient.put(`/plans-verif-machine/${id}/statut?statut=ARCHIVE`),

  restaurerPlanVerif: (payload) => apiClient.post('/plans-verif-machine/restaurer', payload),
};
