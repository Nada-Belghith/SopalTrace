import apiClient from './apiClient';

export const pfPlanService = {
  getDictionnaires: () => apiClient.get('/referentiels/fabrication'),
  
  getTousLesPlans: () => apiClient.get('/plans-pf'),

  getPlan: (id) => apiClient.get(`/plans-pf/${id}`),

  creerPlan: (payload) => apiClient.post('/plans-pf', payload),

  mettreAJourValeurs: (id, payload) => apiClient.put(`/plans-pf/${id}/valeurs`, payload),

  activerPlan: (id) => apiClient.post(`/plans-pf/${id}/activer`),

  creerNouvelleVersion: (id, payload) => apiClient.post(`/plans-pf/${id}/nouvelle-version`, payload),

  restaurerPlan: (payload) => apiClient.post('/plans-pf/restaurer', payload),

  archiverPlan: (id) => apiClient.put(`/plans-pf/${id}/statut?statut=ARCHIVE`),

  importExcel: (formData) => apiClient.post('/plans-pf/import-excel', formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  })
};
