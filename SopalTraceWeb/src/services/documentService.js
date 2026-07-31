/**
 * Service d'accès à l'API pour les documents centralisés.
 * Types concernés : CTRL_POSTE, RESULTAT_CF, PLAN_ASS, PLAN_PF.
 *
 * Droits :
 * - Lecture          : tous les utilisateurs authentifiés
 * - Création/Version/Correction : SuperviseurQualite uniquement
 * - Suppression      : Admin uniquement
 */
import apiClient from './apiClient'

export const documentService = {

  // ── Lecture ──────────────────────────────────────────────────────────────

  /** Récupère un document par son ID. */
  async getById(id) {
    const response = await apiClient.get(`/Document/${id}`)
    return response.data
  },

  /** Recherche de documents par filtres (typeDocumentCode requis). */
  async getByFilters(params) {
    const response = await apiClient.get(`/Document`, { params })
    return response.data
  },

  // ── Écriture (SuperviseurQualite) ─────────────────────────────────────────

  /**
   * Crée un nouveau document (V1, ACTIF).
   * Si un actif existe déjà pour ce contexte, il est archivé côté serveur.
   */
  async createDocument(payload) {
    const response = await apiClient.post(`/Document`, payload)
    return response.data
  },

  /**
   * Crée une nouvelle version (V+1, ACTIF).
   * L'ancien document (ancienId) est archivé côté serveur.
   * @param {string|object} ancienId - ID du document actif à archiver ou payload complet
   * @param {object} [payload]  - Doit contenir ancienId + tous les champs du nouveau document
   */
  async createNewVersion(ancienId, payload) {
    const targetId = (typeof ancienId === 'object' && ancienId !== null) ? (ancienId.ancienId || ancienId.id) : ancienId;
    const body = (typeof ancienId === 'object' && ancienId !== null) ? ancienId : payload;
    const response = await apiClient.post(`/Document/${targetId}/version`, body)
    return response.data
  },

  /**
   * Correction mineure : modifie le document en place.
   * Aucun changement de version, aucun archivage.
   */
  async updateDocument(id, payload) {
    const response = await apiClient.put(`/Document/${id}`, payload)
    return response.data
  },

  // ── Utilitaires ───────────────────────────────────────────────────────────

  /** Crée une periodicité (référentiel). */
  async createPeriodicite(payload) {
    const response = await apiClient.post('/referentiels/periodicites', payload)
    return response.data
  },

  /** Import Excel d'un plan. */
  async importExcel(formData) {
    const response = await apiClient.post('/ExcelImport/plan', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    return response.data
  }
}
