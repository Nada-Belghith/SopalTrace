import apiClient from './apiClient';

export const referentielsService = {
  getDictionnairesFabrication() {
    return apiClient.get('/referentiels/fabrication');
  },

  getDictionnairesVerifMachine() {
    return apiClient.get('/referentiels/verif-machine');
  },

  getDictionnairesControlePoste() {
    return apiClient.get('/referentiels/plans-nc');
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
