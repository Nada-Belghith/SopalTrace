import apiClient from './apiClient';

export const fabModeleService = {
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

  upgradeModele(id) {
    return apiClient.post(`/modeles-fabrication/${id}/upgrade`);
  },

  getModelesByFilters(typeRobinetCode, natureComposantCode, operationCode, posteCode = null, familleProduitCode = null) {
    const params = new URLSearchParams();
    if (typeRobinetCode) params.append('typeRobinet', typeRobinetCode);
    if (natureComposantCode) params.append('natureComposant', natureComposantCode);
    if (operationCode) params.append('operation', operationCode); 
    if (posteCode) params.append('poste', posteCode);
    if (familleProduitCode) params.append('familleProduit', familleProduitCode);
    
    return apiClient.get(`/modeles-fabrication/liste?${params.toString()}`);
  }
};
