import apiClient from './apiClient';

class OperateurService {
  getPostesDisponibles() {
    return apiClient.get(`/Operateur/postes`);
  }

  getMachinesPourPoste(posteCode) {
    return apiClient.get(`/Operateur/poste/${posteCode}/machines`);
  }

  getAllOfOperations() {
    return apiClient.get(`/Operateur/ofs/operations`);
  }

  verifierPlan(articleCode) {
    return apiClient.get(`/Operateur/plan/existe?articleCode=${articleCode}`);
  }

  updatePlanLigne(ligneId, data) {
    return apiClient.put(`/Operateur/plan/ligne/${ligneId}`, data);
  }

  demarrerOf(data) {
    return apiClient.post(`/Operateur/of/start`, data);
  }

  mettreEnReglage(execControleOfId) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/mettre-en-reglage`, {});
  }

  cloturerOf(execControleOfId) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/close`, {});
  }

  getAlertesActives(execControleOfId) {
    return apiClient.get(`/Operateur/of/${execControleOfId}/alertes-actives`);
  }

  repondreOccurrence(occurrenceId, data) {
    return apiClient.post(`/Operateur/occurrence/${occurrenceId}/repondre`, data);
  }
}

export default new OperateurService();
