import apiClient from './apiClient'

/**
 * Convertit l'état de l'éditeur (nom, désignation…) au format attendu par l'API (code, libellé…).
 * Utilisé par la création, la correction et la nouvelle version.
 */
const versRequeteModele = (payload) => ({
  code: payload.nom,
  libelle: payload.designation,
  typeRobinetCode: "",
  natureComposantCode: payload.natureArticleCode || payload.natureComposantCode || "",
  operationCode: payload.operationCode,
  posteCode: payload.posteCode,
  familleProduitCode: payload.familleProduitFiniCode,
  legendeMoyens: payload.legendeMoyens,
  notes: payload.notes || payload.designation,
  refFormulaireCodeReference: payload.refFormulaireCodeReference || 'PRC',
  sections: payload.sections ? payload.sections.map(s => ({
    id: s.id || undefined,
    libelleSection: s.libelleSection,
    ordreAffiche: s.ordreAffiche,
    typeSectionId: s.typeSectionId,
    periodiciteId: s.periodiciteId,
    lignes: s.lignes ? s.lignes.map(l => ({
      id: l.id || undefined,
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
});

export const modeleFabricationService = {
  async getModelsByFilters(typeRobinet, natureComposantCode, operationCode, posteCode = null) {
    let params;
    if (typeof typeRobinet === 'object' && typeRobinet !== null) {
      params = {
        natureComposantCode: typeRobinet.natureComposantCode,
        operationCode: typeRobinet.operationCode,
        familleProduitCode: typeRobinet.familleProduitCode || typeRobinet.typeRobinet,
        posteCode: typeRobinet.posteCode,
        statut: typeRobinet.statut
      };
    } else {
      params = { natureComposantCode, operationCode, familleProduitCode: typeRobinet, posteCode };
    }
    const response = await apiClient.get('/ModeleFabrication', { params });
    return response.data;
  },

  async getModelById(id) {
    const response = await apiClient.get(`/ModeleFabrication/${id}`);
    return response.data;
  },

  async createModel(payload) {
    const req = versRequeteModele(payload);
    const response = await apiClient.post(`/ModeleFabrication`, req);
    return { data: { modeleId: response.data.id, version: 1 } };
  },

  async createNewVersion(payload) {
    const req = {
      ...versRequeteModele(payload),
      ancienId: payload.ancienId,
      motifModification: payload.motifModification,
      rowVersion: payload.rowVersion
    };
    const response = await apiClient.post(`/ModeleFabrication/nouvelle-version`, req);
    return { data: { modeleId: response.data.id } };
  },

  async upgradeModel(id) {
    const response = await apiClient.post(`/ModeleFabrication/nouvelle-version`, { ancienId: id });
    return { data: { modeleId: response.data.id } };
  },

  async updateModel(id, payload) {
    const response = await apiClient.put(`/ModeleFabrication/${id}`, { ...versRequeteModele(payload), rowVersion: payload.rowVersion });
    return { data: { success: true, rowVersion: response.data.rowVersion } };
  },

  async activerModele(id) {
    return Promise.resolve({ data: { success: true } });
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
