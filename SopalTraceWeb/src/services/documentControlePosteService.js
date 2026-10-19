import apiClient from './apiClient';
import { documentService } from './documentService';

export const documentControlePosteService = {
  // Dictionnaires (postes, machines, risques)
  getDictionaries: () => apiClient.get('/referentiels/plans-nc'),

  // CRUD Plans de Non-Conformité
  getAllDocuments: () => documentService.getByFilters({ typeDocumentCode: 'CTRL_POSTE' }).then(data => ({ data })),

  createDocumentControlePoste: (payload) => {
    payload.typeDocumentCode = 'CTRL_POSTE';
    return documentService.createDocument(payload).then(response => ({ data: response }));
  },

  getDocumentControlePoste: (id) => documentService.getById(id).then(data => ({ data })),

  updateDocumentControlePoste: (id, payload) => documentService.updateDocument(id, payload),

  createNewVersion: (payload) => {
    payload.typeDocumentCode = 'CTRL_POSTE';
    return documentService.createNewVersion(payload.ancienId || payload.documentId, payload).then(response => ({ data: response }));
  },

  importExcel: (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.post('/ExcelImport/controle-poste', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },
};
