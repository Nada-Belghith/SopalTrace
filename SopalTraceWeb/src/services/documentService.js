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

  async updateDocument(id, payload) {
    const response = await apiClient.put(`/Document/${id}`, payload)
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

  // Added missing method for Periodicite
  async createPeriodicite(payload) {
    const response = await apiClient.post('/referentiels/periodicites', payload);
    return response.data;
  },

  async importExcel(formData) {
    const response = await apiClient.post('/ExcelImport/plan', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  }
}
