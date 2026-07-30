import apiClient from './apiClient';
import { documentService } from './documentService';

export const documentRccfService = {
  // Dictionnaires
  getDictionaries: () => apiClient.get('/referentiels/plans-nc'),

  // CRUD Plans RCCF
  getAllDocuments: () => documentService.getByFilters({ typeDocumentCode: 'RESULTAT_CF' }).then(data => ({ data })),

  createDocumentRccf: (payload) => {
    payload.typeDocumentCode = 'RESULTAT_CF';
    return documentService.createDocument(payload).then(response => ({ data: response }));
  },

  getDocumentRccf: (id) => documentService.getById(id).then(data => ({ data })),

  updateDocumentRccf: (id, payload) => documentService.updateDocument(id, payload),

  createNewVersion: (payload) => {
    payload.typeDocumentCode = 'RESULTAT_CF';
    return documentService.createNewVersion(payload.ancienId || payload.documentId, payload).then(response => ({ data: response }));
  },

  restore: (payload) => Promise.reject(new Error("La restauration n'est pas supportée pour ce type de document.")),

  importExcel: (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.post('/excelimport/rccf', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },
};
