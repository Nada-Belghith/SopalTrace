import apiClient from './apiClient';

export const assModeleService = {
  createModele(payload) {
    return apiClient.post('/modeles-assemblage', payload);
  },

  getModeleById(id) {
    return apiClient.get(`/modeles-assemblage/${id}`);
  },

  updateModeleValeurs(id, payload) {
    return apiClient.put(`/modeles-assemblage/${id}/valeurs`, payload);
  },

  activerModele(id) {
    return apiClient.post(`/modeles-assemblage/${id}/activer`);
  },

  newModeleVersion(payload) {
    return apiClient.post('/modeles-assemblage/nouvelle-version', payload);
  },

  restoreModele(payload) {
    return apiClient.post('/modeles-assemblage/restaurer', payload);
  },

  getModelesByFilters(typeRobinetCode, natureComposantCode, operationCode, posteCode = null, familleProduitCode = null) {
    const params = new URLSearchParams();
    if (typeRobinetCode) params.append('typeRobinet', typeRobinetCode);
    if (natureComposantCode) params.append('natureComposant', natureComposantCode);
    if (operationCode) params.append('operation', operationCode); 
    if (posteCode) params.append('poste', posteCode);
    if (familleProduitCode) params.append('familleProduit', familleProduitCode);
    
    return apiClient.get(`/modeles-assemblage/liste?${params.toString()}`);
  }
};
