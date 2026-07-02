import apiClient from './apiClient';

const alertesService = {
  signalerPlanManquant(data) {
    // data = { operationCode, posteCode, articleCode, descriptionProbleme }
    return apiClient.post('/alertes/plan-manquant', data);
  },
  resoudreAlerte(id) {
    return apiClient.post(`/alertes/${id}/resoudre`);
  }
};

export default alertesService;
