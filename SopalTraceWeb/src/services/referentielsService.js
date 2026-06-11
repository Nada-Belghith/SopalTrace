import apiClient from './apiClient';

export const referentielsService = {
  getDictionnaires() {
    return apiClient.get('/referentiels/fabrication');
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
