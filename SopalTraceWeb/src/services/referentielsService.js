import apiClient from './apiClient';

export const referentielsService = {
  getDictionnairesFabrication() {
    return apiClient.get('/referentiels/fabrication');
  },

  getDictionnairesVerifMachine() {
    return apiClient.get('/referentiels/verif-machine');
  },

  getInstruments() {
    return apiClient.get('/referentiels/instruments');
  },

  getDictionnairesControlePoste() {
    return apiClient.get('/referentiels/plans-nc');
  },

  /** Équipes de l'usine (E1, E2, E3…) pour définir les équipes d'un document. */
  getEquipes() {
    return apiClient.get('/referentiels/equipes');
  },

  getFormulairesListByRole(role) {
    return apiClient.get(`/referentiels/formulaires/liste/${role}`);
  },

  getArticleFromERP(codeArticle) {
    return apiClient.get(`/referentiels/article/${codeArticle}`);
  },

  searchArticlesSf(query) {
    return apiClient.get(`/referentiels/articles-sf/search`, { params: { q: query } });
  }
};
