import apiClient from './apiClient';
import { documentService } from './documentService';

export const verifMachineService = {
  getDictionaries() {
    return apiClient.get('/referentiels/verif-machine');
  },

  getFamiliesByMachine(machineCode) {
    // Si l'API backend n'a pas encore de endpoint pour les familles par machine, 
    // on peut renvoyer un tableau vide pour ne pas crasher.
    // return apiClient.get(`/referentiels/machines/${machineCode}/familles`);
    return Promise.resolve({ data: { data: [] } });
  },

  getAllDocuments() {
    return apiClient.get('/DocumentVerifMachine');
  },

  getDocumentById(id) {
    return apiClient.get(`/DocumentVerifMachine/${id}`);
  },

  createDocument(payload) {
    return apiClient.post('/DocumentVerifMachine', payload);
  },

  updateDocument(id, payload) {
    // Le backend DocumentVerifMachineController renvoie 204 NoContent, donc on mock la réponse pour le store
    return apiClient.put(`/DocumentVerifMachine/${id}`, payload).then(() => {
      return { data: { id: id, version: payload.version } };
    });
  },

  creerNouvelleVersion(payload) {
    return apiClient.post('/DocumentVerifMachine/nouvelle-version', payload);
  },

  restoreDocument(payload) {
    return apiClient.post(`/DocumentVerifMachine/${payload.AncienId}/restaurer`, payload);
  },

  importExcel(file, configColonnesJson) {
    const formData = new FormData();
    formData.append('file', file);
    if (configColonnesJson) {
      formData.append('configurationColonnesJson', configColonnesJson);
    }
    return apiClient.post('/ExcelImport/verif-machine', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    }).then(res => {
      // Le store attend response.data.data pour l'import Excel
      return { data: { data: res.data } };
    });
  }
};
