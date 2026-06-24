import apiClient from './apiClient'

export const planFabricationService = {
  async getPlansByFilters(typeRobinet, natureComposantCode, operationCode, posteCode = null, articleCode = null) {
    const typeDocumentCode = operationCode === 'ASS' ? 'PLAN_ASS' : 'PLAN_FAB';
    const params = { typeDocumentCode, natureComposantCode, operationCode, posteCode, codeArticleSageVersionne: articleCode, familleProduitCode: typeRobinet };
    const response = await apiClient.get('/PlanFabrication', { params });
    return response.data;
  },

  async createPeriodicite(payload) {
    const response = await apiClient.post('/referentiels/periodicites', payload);
    return response.data;
  },

  async getPlanById(id) {
    const response = await apiClient.get(`/PlanFabrication/${id}`);
    return response.data;
  },

  async deletePlan(id) {
    const response = await apiClient.delete(`/PlanFabrication/${id}`);
    return response.data;
  },

  async newPlanVersion(payload) {
    let req = {
      ancienId: payload.ancienId || payload.DocumentId || payload.documentId,
      ...payload
    };
    const response = await apiClient.post(`/PlanFabrication/nouvelle-version`, req);
    return response.data;
  },

  async restorePlan(payload) {
    let req = {
      documentArchiveId: payload.documentArchiveId || payload.DocumentArchiveId || payload.documentId,
      motifRestoration: payload.motifRestoration || 'Restoration'
    };
    const response = await apiClient.post(`/PlanFabrication/restaurer`, req);
    return response.data;
  },

  async mettreAJourValeurs(planId, payloadData, legendeMoyens, remarques, finaliser, nomPlan, modifiePar, codeArticleSage) {
    const payload = {
      nom: nomPlan,
      legendeMoyens: legendeMoyens,
      remarques: remarques,
      libre1: codeArticleSage,
      sections: payloadData.sections || payloadData,
      colonneDefs: payloadData.colonneDefs || []
    };
    await apiClient.put(`/PlanFabrication/${planId}`, payload);
    if (finaliser) {
      await apiClient.put(`/hub/plans/FAB/${planId}/statut?statut=ACTIF`);
    }
  },

  async verifierEtatPlan(articleCode, familleCode, natureCode, modeleId, operationCode = null, posteCode = null) {
    const plans = await this.getPlansByFilters(familleCode, natureCode, operationCode, posteCode, articleCode);

    const actif = plans.find(p => p.statut === 'ACTIF' && p.codeArticleSageVersionne === articleCode);
    const brouillon = plans.find(p => p.statut === 'BROUILLON' && p.codeArticleSageVersionne === articleCode);

    return {
      data: {
        existeActif: !!actif,
        existeBrouillon: !!brouillon,
        brouillonId: brouillon ? brouillon.id : null,
        actifId: actif ? actif.id : null
      }
    };
  },

  async instantiatePlan(payload) {
    let newDoc = {
      nom: payload.codeArticleSage,
      designation: payload.designation,
      statut: payload.statut,
      versionInitiale: 1,
      operationCode: payload.operationCode,
      refFormulaireCodeReference: payload.refFormulaireCodeReference || 'PRC',
      natureArticleCode: payload.natureComposantCode,
      familleProduitFiniCode: payload.familleCode,
      posteCode: payload.posteCode,
      libre1: payload.codeArticleSage,
      colonneDefs: payload.colonneDefs || [],
      sections: []
    };

    if (payload.modeleSourceId) {
      // NOTE: Here we need to get the model sections. We will use the Modele endpoint.
      // Since we don't want cyclic dependency or mix, we fetch from ModeleFabrication
      const modeleRes = await apiClient.get(`/ModeleFabrication/${payload.modeleSourceId}`);
      const modele = modeleRes.data;

      newDoc.sections = modele.sections.map(s => ({
        ordreAffiche: s.ordreAffiche,
        libelleSection: s.libelleSection,
        typeSectionId: s.typeSectionId,
        periodiciteId: s.periodiciteId,
        regleEchantillonnageId: s.regleEchantillonnageId,
        notes: s.notes,
        normeReference: s.normeReference,
        nqaId: s.nqaId,
        lignes: s.lignes.map(l => ({
          ordreAffiche: l.ordreAffiche,
          caracteristiqueId: l.caracteristiqueId,
          libelleAffiche: l.libelleAffiche,
          typeCaracteristiqueId: l.typeCaracteristiqueId,
          typeControleId: l.typeControleId,
          moyenControleId: l.moyenControleId,
          moyenTexteLibre: l.moyenTexteLibre,
          instrumentCode: l.instrumentCode,
          periodiciteId: l.periodiciteId,
          limiteSpecTexte: l.limiteSpecTexte,
          estCritique: l.estCritique,
          instruction: l.instruction,
          observations: l.observations,
          machineCode: l.machineCode,
          estVerifPresence: l.estVerifPresence,
          defauthequeId: l.defauthequeId,
          refPlanProduit: l.refPlanProduit,
          machineCodeCtrlPoste: l.machineCodeCtrlPoste,
          risqueDefautId: l.risqueDefautId,
          libre1: l.codeArticleSage,
          extraColonnes: l.extraColonnes || []
        }))
      }));
    }

    const response = await apiClient.post(`/PlanFabrication`, newDoc);
    // Usually API returns { id: guid }
    return { data: { id: response.data.id || response.data } };
  },

  async clonePlan(payload) {
    const sourceRes = await apiClient.get(`/PlanFabrication/${payload.planExistantId}`);
    const source = sourceRes.data;

    let newDoc = {
      typeDocumentCode: source.typeDocumentCode,
      nom: `PC-${payload.nouveauCodeArticleSage}`,
      designation: payload.nouvelleDesignation,
      versionInitiale: 1,
      operationCode: source.operationCode,
      refFormulaireCodeReference: source.formulaireCodeReference || 'PRC',
      natureArticleCode: source.natureArticleCode,
      familleProduitFiniCode: source.familleProduitFiniCode,
      posteCode: source.posteCode,
      colonneDefs: source.colonneDefs?.map(c => ({
        cleColonne: c.cleColonne,
        labelAffiche: c.labelAffiche,
        typeValeur: c.typeValeur,
        ordreAffiche: c.ordreAffiche
      })) || [],
      sections: source.sections.map(s => ({
        ordreAffiche: s.ordreAffiche,
        libelleSection: s.libelleSection,
        typeSectionId: s.typeSectionId,
        periodiciteId: s.periodiciteId,
        regleEchantillonnageId: s.regleEchantillonnageId,
        notes: s.notes,
        normeReference: s.normeReference,
        nqaId: s.nqaId,
        lignes: s.lignes.map(l => ({
          ordreAffiche: l.ordreAffiche,
          caracteristiqueId: l.caracteristiqueId,
          libelleAffiche: l.libelleAffiche,
          typeCaracteristiqueId: l.typeCaracteristiqueId,
          typeControleId: l.typeControleId,
          moyenControleId: l.moyenControleId,
          moyenTexteLibre: l.moyenTexteLibre,
          instrumentCode: l.instrumentCode,
          periodiciteId: l.periodiciteId,
          limiteSpecTexte: l.limiteSpecTexte,
          estCritique: l.estCritique,
          instruction: l.instruction,
          observations: l.observations,
          machineCode: l.machineCode,
          estVerifPresence: l.estVerifPresence,
          defauthequeId: l.defauthequeId,
          refPlanProduit: l.refPlanProduit,
          machineCodeCtrlPoste: l.machineCodeCtrlPoste,
          risqueDefautId: l.risqueDefautId,
          libre1: payload.nouveauCodeArticleSage,
          extraColonnes: l.extraColonnes || []
        }))
      }))
    };

    const response = await apiClient.post(`/PlanFabrication`, newDoc);
    return { data: { id: response.data.id || response.data } };
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
