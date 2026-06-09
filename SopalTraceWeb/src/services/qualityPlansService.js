import apiClient from './apiClient';

export const qualityPlansService = {
  // Dictionnaires pour fabrication
  getDictionnaires() {
    return apiClient.get('/referentiels/fabrication');
  },

  getFormulairesListByRole(role) {
    return apiClient.get(`/referentiels/formulaires/liste/${role}`);
  },

  // --- Modèles de fabrication ---
  _isAss(payload) {
    if (payload?.operationCode === 'ASS') return true;
    const n = payload?.natureComposantCode?.trim().toUpperCase();
    if (n === 'PISTON' || n === 'PF') return true;
    const t = payload?.typeRobinetCode?.trim().toUpperCase();
    if (t === 'PISTON') return true;
    return false;
  },

  createModele(payload, isAssExplicit = null) {
    const isAss = isAssExplicit !== null ? isAssExplicit : this._isAss(payload);
    const route = isAss ? '/modeles-assemblage' : '/modeles-fabrication';
    return apiClient.post(route, payload);
  },

  getModeleById(id, type = null) {
    const route = type === 'ASS' ? '/modeles-assemblage' : '/modeles-fabrication';
    return apiClient.get(`${route}/${id}`);
  },

  updateModeleValeurs(id, payload, isAssExplicit = null) {
    const isAss = isAssExplicit !== null ? isAssExplicit : this._isAss(payload);
    const route = isAss ? '/modeles-assemblage' : '/modeles-fabrication';
    return apiClient.put(`${route}/${id}/valeurs`, payload);
  },

  activerModele(id, type = null) {
    const route = type === 'ASS' ? '/modeles-assemblage' : '/modeles-fabrication';
    return apiClient.post(`${route}/${id}/activer`);
  },

  newModeleVersion(payload, isAssExplicit = null) {
    const isAss = isAssExplicit !== null ? isAssExplicit : this._isAss(payload);
    const route = isAss ? '/modeles-assemblage' : '/modeles-fabrication';
    return apiClient.post(`${route}/nouvelle-version`, payload);
  },

  restoreModele(payload, isAssExplicit = null) {
    const isAss = isAssExplicit !== null ? isAssExplicit : this._isAss(payload);
    const route = isAss ? '/modeles-assemblage' : '/modeles-fabrication';
    return apiClient.post(`${route}/restaurer`, payload);
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

  // Modification de la méthode pour passer l'operationCode et le posteCode
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

  getArticleFromERP(codeArticle) {
    return apiClient.get(`/referentiels/article/${codeArticle}`);
  },

  searchArticlesSf(query) {
    return apiClient.get(`/referentiels/articles-sf/search`, { params: { q: query } });
  },

  getModelesByFilters(typeRobinetCode, natureComposantCode, operationCode, posteCode = null, familleProduitCode = null) {
    const params = new URLSearchParams();
    if (typeRobinetCode) params.append('typeRobinet', typeRobinetCode);
    if (natureComposantCode) params.append('natureComposant', natureComposantCode);
    if (operationCode) params.append('operation', operationCode); 
    if (posteCode) params.append('poste', posteCode);
    if (familleProduitCode) params.append('familleProduit', familleProduitCode);
    
    // Fallback to FAB list, wait: DevModelHub actually relies on ModeleFabricationController
    // returning BOTH ass and fab if operationCode is not provided.
    // So for 'liste', maybe we still want to call a unified endpoint, OR
    // we just use the backend logic in `qualityPlansService.js` to call both and merge.
    const isAss = this._isAss({ operationCode, natureComposantCode, typeRobinetCode });
    const route = (operationCode && isAss) ? '/modeles-assemblage' : '/modeles-fabrication';
    
    return apiClient.get(`${route}/liste?${params.toString()}`);
  },

  getPlansByFilters(typeRobinetCode, natureComposantCode, operationCode, posteCode = null) {
    const params = new URLSearchParams();
    if (typeRobinetCode) params.append('typeRobinet', typeRobinetCode);
    if (natureComposantCode) params.append('natureComposant', natureComposantCode);
    if (operationCode) params.append('operation', operationCode);
    if (posteCode) params.append('poste', posteCode);
    
    return apiClient.get(`/plans-fabrication/liste?${params.toString()}`);
  },
  // --- Création à la volée ---
  createCaracteristique(payload) {
    return apiClient.post('/plans-fabrication/caracteristiques', payload);
  },
};
