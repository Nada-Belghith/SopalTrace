import apiClient from './apiClient';

export const qualityPlansService = {
  // Dictionnaires pour fabrication
  getDictionnaires() {
    return apiClient.get('/referentiels/fabrication');
  },

  // --- Modèles de fabrication ---
  createModele(payload) {
    return apiClient.post('/modeles-fabrication', payload);
  },

  getModeleById(id) {
    return apiClient.get(`/modeles-fabrication/${id}`);
  },

  updateModeleValeurs(id, payload) {
    return apiClient.put(`/modeles-fabrication/${id}/valeurs`, payload);
  },

  activerModele(id) {
    return apiClient.post(`/modeles-fabrication/${id}/activer`);
  },

  newModeleVersion(payload) {
    return apiClient.post('/modeles-fabrication/nouvelle-version', payload);
  },

  restoreModele(payload) {
    return apiClient.post('/modeles-fabrication/restaurer', payload);
  },

  // --- Périodicités liés aux plans ---
  createPeriodicite(payload) {
    return apiClient.post('/plans-fabrication/periodicites', payload);
  },

  // --- Sections des plans (scoped by planId) ---
  ajouterSections(planId, payload) {
    return apiClient.post(`/plans-fabrication/${planId}/sections`, payload);
  },

  updateSections(planId, payload) {
    return apiClient.put(`/plans-fabrication/${planId}/sections`, payload);
  },

  // --- Plans de fabrication ---
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

  // Modification de la méthode pour passer l'operationCode
  verifierEtatPlan(articleCode, modeleId, operationCode = null) {
    return apiClient.get('/plans-fabrication/verifier-etat', {
      params: { 
        articleCode, 
        modeleId: modeleId || '', 
        operationCode: operationCode || '' 
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

  getArticleFromERP(codeArticle) {
    return apiClient.get(`/referentiels/article/${codeArticle}`);
  },

  getModelesByFilters(typeRobinetCode, natureComposantCode, operationCode) {
    const params = new URLSearchParams();
    if (typeRobinetCode) params.append('typeRobinet', typeRobinetCode);
    if (natureComposantCode) params.append('natureComposant', natureComposantCode);
    if (operationCode) params.append('operation', operationCode); // Ajout décisif
    
    return apiClient.get(`/modeles-fabrication/liste?${params.toString()}`);
  },

  getPlansByFilters(typeRobinetCode, natureComposantCode, operationCode) {
    const params = new URLSearchParams();
    if (typeRobinetCode) params.append('typeRobinet', typeRobinetCode);
    if (natureComposantCode) params.append('natureComposant', natureComposantCode);
    if (operationCode) params.append('operation', operationCode);
    
    return apiClient.get(`/plans-fabrication/liste?${params.toString()}`);
  },
  // --- Création à la volée ---
  createCaracteristique(payload) {
    return apiClient.post('/plans-fabrication/caracteristiques', payload);
  },
};
