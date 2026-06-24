import apiClient from './apiClient'

export const documentService = {
  // ----------------------------------------------------
  // BASE API CALLS (Unified TPH DocumentController)
  // ----------------------------------------------------
  async getById(id) {
    const response = await apiClient.get(`/Document/${id}`)
    return response.data
  },

  async getByFilters(params) {
    const response = await apiClient.get(`/Document`, { params })
    return response.data
  },

  async create(payload) {
    const response = await apiClient.post(`/Document`, payload)
    return response.data
  },

  async createNewVersion(id, payload) {
    const response = await apiClient.post(`/Document/${id}/version`, payload)
    return response.data
  },

  async restaurer(id, payload) {
    const response = await apiClient.post(`/Document/${id}/restaurer`, payload)
    return response.data
  },

  async deleteDocument(id) {
    const response = await apiClient.delete(`/Document/${id}`)
    return response.data
  },

  // ----------------------------------------------------
  // ----------------------------------------------------
  // ALIASES FOR PLAN_ASS (Used by AssPlan components)
  // ----------------------------------------------------
  async getPlansByFilters(typeRobinet, natureComposantCode, operationCode, posteCode = null) {
    return await this.getByFilters({ typeDocumentCode: 'PLAN_ASS', natureComposantCode, operationCode, posteCode });
  },

  async createPlan(payload) {
    payload.typeDocumentCode = 'PLAN_ASS';
    const id = await this.create(payload);
    return { data: { planId: id, version: 1 } };
  },

  async newPlanVersion(payload) {
    payload.typeDocumentCode = 'PLAN_ASS';
    const id = await this.createNewVersion(payload.ancienId, payload);
    return { data: { planId: id } };
  },

  async updatePlanValeurs(id, payload) {
    return await this.createNewVersion(id, payload);
  },

  async activerPlan(id) {
    return Promise.resolve({ data: { success: true } });
  },

  async getPlanById(id) {
    return this.getById(id);
  },

  async restorePlan(payload) {
    let dto = {
      documentArchiveId: payload.documentArchiveId || payload.DocumentArchiveId || payload.documentId,
      motifRestoration: payload.motifRestoration || 'Restoration'
    };
    return this.restaurer(dto.documentArchiveId, dto);
  },

  async deletePlan(id) {
    return this.deleteDocument(id);
  },

  // Added missing method for Periodicite
  async createPeriodicite(payload) {
    const response = await apiClient.post('/referentiels/periodicites', payload);
    return response.data;
  }
}
