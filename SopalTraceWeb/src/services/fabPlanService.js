import apiClient from './apiClient';

export const fabPlanService = {
  createPeriodicite(payload) {
    return apiClient.post('/plans-fabrication/periodicites', payload);
  },

  ajouterSections(planId, payload) {
    return apiClient.post(`/plans-fabrication/${planId}/sections`, payload);
  },

  updateSections(planId, payload) {
    return apiClient.put(`/plans-fabrication/${planId}/sections`, payload);
  },

  instantiatePlan(payload) {
    return apiClient.post('/plans-fabrication/instancier', payload);
  },

  annulerBrouillonPlan(planId) {
    return apiClient.delete(`/hub/plans/FAB/${planId}`);
  },

  clonePlan(payload) {
    return apiClient.post('/plans-fabrication/clone', payload);
  },

  getPlanById(id) {
    return apiClient.get(`/plans-fabrication/${id}`);
  },

  newPlanVersion(payload) {
    return apiClient.post('/plans-fabrication/nouvelle-version', payload);
  },

  verifierEtatPlan(articleCode, modeleId, operationCode = null, posteCode = null) {
    return apiClient.get('/plans-fabrication/verifier-etat', {
      params: { 
        articleCode, 
        modeleId: modeleId || '', 
        operationCode: operationCode || '',
        posteCode: posteCode || ''
      }
    });
  },

  mettreAJourValeurs(planId, sectionsPayload, legendeMoyens, remarques, finaliser = true, nom = null, modifiePar = 'Admin') {
    return apiClient.put(`/plans-fabrication/${planId}/valeurs`, {
      sections: sectionsPayload,
      legendeMoyens: legendeMoyens, 
      remarques: remarques,
      finaliser,
      nom,
      modifiePar
    });
  },

  importExcel(formData) {
    return apiClient.post('/plans-fabrication/import-excel', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
  },

  deletePlan(planId) {
    return apiClient.delete(`/plans-fabrication/${planId}`);
  },

  restorePlan(payload) {
    return apiClient.post('/plans-fabrication/restaurer', payload);
  },

  upgradePlan(id) {
    return apiClient.post(`/plans-fabrication/${id}/upgrade`);
  },

  getPlansByFilters(typeRobinetCode, natureComposantCode, operationCode, posteCode = null) {
    const params = new URLSearchParams();
    if (typeRobinetCode) params.append('typeRobinet', typeRobinetCode);
    if (natureComposantCode) params.append('natureComposant', natureComposantCode);
    if (operationCode) params.append('operation', operationCode);
    if (posteCode) params.append('poste', posteCode);
    
    return apiClient.get(`/plans-fabrication/liste?${params.toString()}`);
  },

  createCaracteristique(payload) {
    return apiClient.post('/plans-fabrication/caracteristiques', payload);
  }
};
