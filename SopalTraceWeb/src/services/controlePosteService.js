import apiClient from './apiClient';
import { documentService } from './documentService';

export const controlePosteService = {
  // Dictionnaires (postes, machines, risques)
  getDictionnaires: () => apiClient.get('/referentiels/plans-nc'),

  // CRUD Plans de Non-Conformité
  getTousLesPlans: () => documentService.getByFilters({ typeDocumentCode: 'CTRL_POSTE' }).then(data => ({ data })),

  creerControlePoste: (payload) => {
    payload.typeDocumentCode = 'CTRL_POSTE';
    return documentService.create(payload).then(id => ({ data: { id } }));
  },

  getControlePoste: (id) => documentService.getById(id).then(data => ({ data })),

  mettreAJourPlan: (id, payload) => apiClient.put(`/Document/${id}`, payload),

  creerNouvelleVersion: (payload) => {
    payload.typeDocumentCode = 'CTRL_POSTE';
    return documentService.createNewVersion(payload.ancienId || payload.documentId, payload).then(id => ({ data: { id } }));
  },

  restaurer: (payload) => documentService.restaurer(payload.documentArchiveId || payload.documentId, payload).then(id => ({ data: { id } })),

  importerExcel: (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.post('/ExcelImport/controle-poste', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },
};
