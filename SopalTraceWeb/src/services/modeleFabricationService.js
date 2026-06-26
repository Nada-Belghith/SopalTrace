import apiClient from './apiClient'

export const modeleFabricationService = {
  async getModelesByFilters(typeRobinet, natureComposantCode, operationCode, posteCode = null) {
    const params = { natureComposantCode, operationCode, familleProduitCode: typeRobinet, posteCode };
    const response = await apiClient.get('/ModeleFabrication', { params });
    return response.data;
  },

  async getModeleById(id) {
    const response = await apiClient.get(`/ModeleFabrication/${id}`);
    return response.data;
  },

  async createModele(payload) {
    const req = {
      code: payload.nom,
      libelle: payload.designation,
      typeRobinetCode: "",
      natureComposantCode: payload.natureArticleCode || payload.natureComposantCode || "",
      operationCode: payload.operationCode,
      posteCode: payload.posteCode,
      familleProduitCode: payload.familleProduitFiniCode,
      versionInitiale: payload.versionInitiale || 1,
      legendeMoyens: payload.legendeMoyens,
      notes: payload.notes || payload.designation,
      refFormulaireCodeReference: payload.refFormulaireCodeReference || 'PRC',
      sections: payload.sections ? payload.sections.map(s => ({
        libelleSection: s.libelleSection,
        ordreAffiche: s.ordreAffiche,
        typeSectionId: s.typeSectionId,
        periodiciteId: s.periodiciteId,
        regleEchantillonnageId: s.regleEchantillonnageId,
        lignes: s.lignes ? s.lignes.map(l => ({
          ordreAffiche: l.ordreAffiche,
          typeCaracteristiqueId: l.typeCaracteristiqueId,
          libelleAffiche: l.libelleAffiche,
          typeControleId: l.typeControleId,
          moyenControleId: l.moyenControleId,
          moyenTexteLibre: l.moyenTexteLibre,
          instrumentCode: l.instrumentCode,
          periodiciteId: l.periodiciteId,
          limiteSpecTexte: l.limiteSpecTexte,
          estCritique: l.estCritique,
          instruction: l.instruction,
          observations: l.observations,
          imageBase64: l.imageBase64,
          extraColonnes: l.extraColonnes || []
        })) : []
      })) : []
    };

    const response = await apiClient.post(`/ModeleFabrication`, req);
    return { data: { modeleId: response.data.id, version: 1 } };
  },

  async newModeleVersion(payload) {
    const req = {
      ...payload
    };
    const response = await apiClient.post(`/ModeleFabrication/nouvelle-version`, req);
    return { data: { modeleId: response.data.id } };
  },

  async upgradeModele(id) {
    const response = await apiClient.post(`/ModeleFabrication/nouvelle-version`, { ancienId: id });
    return { data: { modeleId: response.data.id } };
  },

  async updateModeleValeurs(id, payload) {
    const response = await apiClient.put(`/ModeleFabrication/${id}`, payload);
    return { data: { success: true } };
  },

  async activerModele(id) {
    return Promise.resolve({ data: { success: true } });
  },

  async deleteModele(id) {
    const response = await apiClient.delete(`/ModeleFabrication/${id}`);
    return response.data;
  },

  async restoreModele(payload) {
    const req = {
      modeleArchiveId: payload.documentArchiveId || payload.DocumentArchiveId || payload.documentId,
      motifRestoration: payload.motifRestoration || 'Restoration',
      restaurePar: payload.creePar || 'SYSTEM'
    };
    const response = await apiClient.post(`/ModeleFabrication/restaurer`, req);
    return response.data;
  },

  async importExcel(formData) {
    const response = await apiClient.post('/ExcelImport/plan', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  }
}
