import apiClient from './apiClient';

/**
 * Magasin : chaque scan est un appel enregistré immédiatement. Le contenu brut est envoyé tel quel :
 * le serveur le lit, le valide et identifie le magasinier par son jeton.
 */
const magasinService = {
  // Préparation des OF
  getOf: (numeroOf) => apiClient.get(`/magasinier/of/${encodeURIComponent(numeroOf)}`).then(r => r.data),
  ouvrirPreparation: (numeroOf) => apiClient.post('/magasinier/preparations', { numeroOf }).then(r => r.data),
  scannerComposant: (preparationId, contenu) => apiClient.post(`/magasinier/preparations/${preparationId}/scans`, { contenu }).then(r => r.data),
  retirerLot: (preparationId, lotId) => apiClient.delete(`/magasinier/preparations/${preparationId}/scans/${lotId}`).then(r => r.data),
  terminerPreparation: (preparationId, motif) => apiClient.post(`/magasinier/preparations/${preparationId}/terminer`, { motif }).then(r => r.data),
  annulerPreparation: (preparationId) => apiClient.post(`/magasinier/preparations/${preparationId}/annuler`).then(r => r.data),

  // Expédition des BL
  getBl: (numeroBl) => apiClient.get(`/magasinier/bl/${encodeURIComponent(numeroBl)}`).then(r => r.data),
  ouvrirExpedition: (numeroBl) => apiClient.post('/magasinier/expeditions', { numeroBl }).then(r => r.data),
  scannerOf: (expeditionId, contenu) => apiClient.post(`/magasinier/expeditions/${expeditionId}/scans`, { contenu }).then(r => r.data),
  retirerOf: (expeditionId, scanId) => apiClient.delete(`/magasinier/expeditions/${expeditionId}/scans/${scanId}`).then(r => r.data),
  terminerExpedition: (expeditionId) => apiClient.post(`/magasinier/expeditions/${expeditionId}/terminer`).then(r => r.data)
};

export default magasinService;
