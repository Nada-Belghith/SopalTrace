import apiClient from './apiClient'

/**
 * Appels API de l'exécution des OF produits finis (assemblage) côté opérateur.
 * Les paramètres de requête passent par `params` (encodage géré par axios) :
 * aucune valeur saisie n'est concaténée dans une URL.
 */
const enc = encodeURIComponent

export default {
  // ── OF & sessions d'équipe ──
  getOfsStatut() {
    return apiClient.get('/Operateur/ofs/assemblage-statut')
  },
  demarrerOf(numeroOf, posteCodes, equipe) {
    return apiClient.post('/Operateur/of/start-assemblage', { numeroOf, posteCodes, equipe })
  },
  /**
   * Prise de l'OF par une équipe (une seule à la fois) : elle démarre les postes choisis, ou vient seulement
   * clôturer l'OF (aucun poste démarré).
   */
  ajouterPostes(execControleOfId, posteCodes, equipe, pourCloturer = false) {
    return apiClient.post(`/Operateur/of/${execControleOfId}/ajouter-postes`, { posteCodes, equipe, pourCloturer })
  },
  /** L'équipe prend un poste libre de l'OF : ses contrôles de démarrage puis de fin de poste deviennent dus. */
  demarrerPoste(execControleOfId, posteCode, equipe = null) {
    return apiClient.post(`/operateur/of/${execControleOfId}/assemblage/postes/${posteCode}/demarrer`, null, { params: { equipe: equipe || undefined } })
  },

  // ── Statuts des documents ──
  /** Documents de l'équipe donnée (par défaut celle qui a la main sur l'OF). */
  getDocuments(execControleOfId, equipe = null) {
    return apiClient.get(`/Operateur/of/${execControleOfId}/assemblage-documents`, { params: { equipe: equipe || undefined } })
  },

  // ── Vérifications machine du poste (par moment) ──
  /** @param {'DEMARRAGE'|'APRES_PAUSE'|'FIN_POSTE'} periodicite */
  getVerifMachinesPoste(execControleOfId, posteCode, periodicite) {
    return apiClient.get(`/operateur/of/${execControleOfId}/poste/${enc(posteCode)}/verif-machines`, { params: { periodicite } })
  },
  /** Ajoute une machine au poste pour l'équipe active : son document est créé (retourne { id }). */
  ajouterMachinePoste(execControleOfId, posteCode, machineCode) {
    return apiClient.post(`/operateur/of/${execControleOfId}/poste/${enc(posteCode)}/verif-machines`, { machineCode })
  },
  /** @param {{ machineCode?: string, equipe?: string, jour?: string }} filtres */
  getHistoriqueVerifMachines(execControleOfId, posteCode, filtres = {}) {
    return apiClient.get(`/operateur/of/${execControleOfId}/poste/${enc(posteCode)}/verif-machines/historique`, {
      params: { machineCode: filtres.machineCode || undefined, equipe: filtres.equipe || undefined, jour: filtres.jour || undefined }
    })
  },
  /** Enregistre les réponses et valide le moment ; refusé tant qu'un point du moment est sans réponse. */
  validerVerifMachine(statutId, periodicite, reponses) {
    return apiClient.post(`/operateur/verif-machine/${statutId}/valider`, { periodicite, reponses })
  },
  corrigerVerifMachine(statutId, periodicite) {
    return apiClient.post(`/operateur/verif-machine/${statutId}/corriger`, null, { params: { periodicite } })
  },

  // ── Vérification machine ──
  getVerifMachine(statutId) {
    return apiClient.get(`/Operateur/verif-machine/statut/${statutId}`)
  },
  saveVerifMachine(payload) {
    return apiClient.post('/Operateur/verif-machine/save', payload)
  },

  // ── Avancement de la session (étape, tâches, fin contrôlée) ──
  /** Avancement du poste pour l'équipe donnée (par défaut celle qui a la main sur l'OF). */
  getAvancement(execControleOfId, posteCode, equipe = null) {
    return apiClient.get(`/operateur/of/${execControleOfId}/assemblage/avancement`, { params: { posteCode, equipe: equipe || undefined } })
  },
  /** Réglage commencé sur le poste : les contrôles de production s'arrêtent jusqu'à un réglage conforme. */
  demarrerReglage(execControleOfId, posteCode) {
    return apiClient.post(`/operateur/of/${execControleOfId}/assemblage/reglage`, null, { params: { posteCode } })
  },
  /** Réglage abandonné avant toute saisie : la production du poste reprend. */
  annulerReglage(execControleOfId, posteCode) {
    return apiClient.delete(`/operateur/of/${execControleOfId}/assemblage/reglage`, { params: { posteCode } })
  },
  /**
   * Fin de poste (ce poste seulement : les autres postes de l'équipe continuent) ou clôture de l'OF (tous les
   * postes ; l'équipe doit être seule sur l'OF).
   * @param {'FIN_POSTE'|'FIN_OF'} mode
   */
  demanderCloture(execControleOfId, mode, posteCode, equipe = null) {
    return apiClient.post(`/operateur/of/${execControleOfId}/assemblage/demande-cloture`, { mode, posteCode, equipe: equipe || undefined })
  },
  /** Annule la clôture de l'OF demandée par l'équipe, sinon la fin de poste demandée sur le poste. */
  annulerDemandeCloture(execControleOfId, posteCode, equipe = null) {
    return apiClient.delete(`/operateur/of/${execControleOfId}/assemblage/demande-cloture`, { params: { posteCode, equipe: equipe || undefined } })
  },
  /** Termine le poste (ou clôture l'OF si elle est demandée) ; refusé par le serveur tant qu'une tâche de fin manque. */
  terminer(execControleOfId, posteCode, equipe = null) {
    return apiClient.post(`/operateur/of/${execControleOfId}/assemblage/terminer`, null, { params: { posteCode, equipe: equipe || undefined } })
  },

  // ── Plan d'assemblage (contrôles de lot : première / dernière pièce) ──
  getExecution(execControleOfId, posteCode) {
    return apiClient.get(`/ExecPlanAssemblage/${execControleOfId}`, { params: { posteCode } })
  },
  /** @param {{ posteCode: string, sectionId: string, moment: 'PREMIERE_PIECE'|'DERNIERE_PIECE', lignesControle: Array }} data */
  saveResultat(execControleOfId, data) {
    return apiClient.post(`/ExecPlanAssemblage/${execControleOfId}/resultat`, data)
  },

  // ── Résultat contrôle poste & échantillonnage ──
  getPlanRcPoste(execControleOfId, posteCode, equipe) {
    return apiClient.get(`/ExecRcPoste/${execControleOfId}`, { params: { posteCode, equipe } })
  },
  /** L'équipe en production valide ses heures sur le contrôle au poste du jour (document commun aux équipes). */
  validerHeuresRcPoste(execControleDocumentStatutId) {
    return apiClient.post(`/ExecRcPoste/${execControleDocumentStatutId}/valider-equipe`)
  },
  initEchantillonnage(execControleOfId, payload) {
    return apiClient.post(`/ExecEchantillonnage/init/${execControleOfId}`, payload)
  }
}
