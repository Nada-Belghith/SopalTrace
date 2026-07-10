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

  verifierPlan(articleCode, operationCode = '') {
    const opParam = operationCode ? `&operationCode=${operationCode}` : '';
    return apiClient.get(`/Operateur/plan/existe?articleCode=${articleCode}${opParam}`);
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

  mettreEnPause(execControleOfId, raison) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/pause`, { raison });
  }

  reprendreDepuisPause(execControleOfId) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/reprendre`, {});
  }

  cloturerOf(execControleOfId) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/close`, {});
  }

  ignorerTranche(execControleOfId, trancheHoraire, matriculeOperateur, raison) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/tranche/${trancheHoraire}/ignorer`, { matriculeOperateur, raison });
  }

  declarerTrancheReglage(execControleOfId, trancheHoraire, matriculeOperateur) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/tranche/${trancheHoraire}/reglage`, { matriculeOperateur });
  }

  getAlertesActives(execControleOfId) {
    return apiClient.get(`/Operateur/of/${execControleOfId}/alertes-actives`);
  }

  repondreOccurrence(occurrenceId, data) {
    return apiClient.post(`/Operateur/occurrence/${occurrenceId}/repondre`, data);
  }
}

export default new OperateurService();
