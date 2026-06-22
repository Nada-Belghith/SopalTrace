import apiClient from './apiClient';
import { documentService } from './documentService';

export const verifMachineService = {
  getDictionnaires() {
    return apiClient.get('/referentiels/verif-machine');
  },

  getFamillesParMachine(machineCode) {
    // Si l'API backend n'a pas encore de endpoint pour les familles par machine, 
    // on peut renvoyer un tableau vide pour ne pas crasher.
    // return apiClient.get(`/referentiels/machines/${machineCode}/familles`);
    return Promise.resolve({ data: { data: [] } });
  },

  getTousLesPlans() {
    return apiClient.get('/PlanVerifMachine');
  },

  getPlanVerif(id) {
    return apiClient.get(`/PlanVerifMachine/${id}`);
  },

  creerPlanVerif(payload) {
    return apiClient.post('/PlanVerifMachine', payload);
  },

  mettreAJourPlanVerif(id, payload) {
    // Le backend PlanVerifMachineController renvoie 204 NoContent, donc on mock la réponse pour le store
    return apiClient.put(`/PlanVerifMachine/${id}`, payload).then(() => {
      return { data: { id: id, version: payload.version } };
    });
  },

  restaurerPlanVerif(payload) {
    return apiClient.post(`/PlanVerifMachine/${payload.AncienId}/restaurer`, payload);
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
