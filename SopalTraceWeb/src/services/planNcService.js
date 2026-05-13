import apiClient from './apiClient';

export const planNcService = {
  // Dictionnaires (postes, machines, risques)
  getDictionnaires: () => apiClient.get('/referentiels/plans-nc'),

  // CRUD Plans de Non-Conformité
  getTousLesPlans: () => apiClient.get('/plans-nc'),

  creerPlanNc: (payload) => apiClient.post('/plans-nc', payload),

  getPlanNc: (id) => apiClient.get(`/plans-nc/${id}`),

  mettreAJourPlan: (id, payload) => apiClient.put(`/plans-nc/${id}`, payload),

  creerNouvelleVersion: (payload) => apiClient.post('/plans-nc/nouvelle-version', payload),

  restaurer: (payload) => apiClient.post('/plans-nc/restaurer', payload),

  importerExcel: (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.post('/plans-nc/import-excel', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },
};
