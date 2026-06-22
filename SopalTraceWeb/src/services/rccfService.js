import apiClient from './apiClient';
import { documentService } from './documentService';

export const rccfService = {
  // Dictionnaires
  getDictionnaires: () => apiClient.get('/referentiels/plans-nc'),

  // CRUD Plans RCCF
  getTousLesPlans: () => documentService.getByFilters({ typeDocumentCode: 'RESULTAT_CF' }).then(data => ({ data })),

  creerPlan: (payload) => {
    payload.typeDocumentCode = 'RESULTAT_CF';
    return documentService.create(payload).then(response => ({ data: { id: response.id } }));
  },

  getPlan: (id) => documentService.getById(id).then(data => ({ data })),

  mettreAJourPlan: (id, payload) => apiClient.put(`/Document/${id}`, payload),

  creerNouvelleVersion: (payload) => {
    payload.typeDocumentCode = 'RESULTAT_CF';
    return documentService.createNewVersion(payload.ancienId || payload.documentId, payload).then(response => ({ data: { id: response.id } }));
  },

  restaurer: (payload) => documentService.restaurer(payload.documentArchiveId || payload.documentId, payload).then(response => ({ data: { id: response.id } })),

  importerExcel: (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.post('/excelimport/rccf', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },
};
