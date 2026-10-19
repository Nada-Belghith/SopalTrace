import apiClient from './apiClient';

const enc = encodeURIComponent;

class OperateurService {
  getPostesDisponibles() {
    return apiClient.get('/Operateur/postes');
  }

  getMachines() {
    return apiClient.get('/Operateur/machines');
  }

  getMachinesPourPoste(posteCode) {
    return apiClient.get(`/Operateur/poste/${enc(posteCode)}/machines`);
  }

  getAllOfOperations() {
    return apiClient.get('/Operateur/ofs/operations');
  }

  verifierPlan(articleCode, operationCode = '') {
    return apiClient.get('/Operateur/plan/existe', { params: { articleCode, operationCode: operationCode || undefined } });
  }

  demarrerOf(data) {
    return apiClient.post('/Operateur/of/start', data);
  }

  mettreEnReglage(execControleOfId) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/mettre-en-reglage`, {});
  }

  /** Assemblage : pause de l'équipe donnée (les autres équipes de l'OF continuent). */
  mettreEnPause(execControleOfId, raison, typePause = 'AUTRE', equipe = null) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/pause`, { raison, typePause, equipe: equipe || undefined });
  }

  /** Reprise de l'équipe donnée (plusieurs équipes peuvent être en pause sur un OF d'assemblage). */
  reprendreDepuisPause(execControleOfId, equipe = null) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/reprendre`, {}, { params: { equipe: equipe || undefined } });
  }

  terminerPoste(execControleOfId) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/fin-poste`, {});
  }

  cloturerOf(execControleOfId) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/close`, {});
  }

  ignorerTranche(execControleOfId, trancheHoraire, raison) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/tranche/${enc(trancheHoraire)}/ignorer`, { raison });
  }

  declarerTrancheReglage(execControleOfId, trancheHoraire) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/tranche/${enc(trancheHoraire)}/reglage`, {});
  }

  /** Contrôles à faire ; assemblage : ceux de l'équipe (par défaut celle qui a la main) sur le poste. */
  getAlertesActives(execControleOfId, posteCode = null, equipe = null) {
    return apiClient.get(`/Operateur/of/${execControleOfId}/alertes-actives`, { params: { posteCode: posteCode || undefined, equipe: equipe || undefined } });
  }

  repondreOccurrence(occurrenceId, data) {
    return apiClient.post(`/Operateur/occurrence/${occurrenceId}/repondre`, data);
  }
}

export default new OperateurService();
