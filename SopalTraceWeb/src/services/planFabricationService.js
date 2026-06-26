import apiClient from './apiClient'

export const planFabricationService = {
  async getPlansByFilters(typeRobinet, natureComposantCode, operationCode, posteCode = null, articleCode = null) {
    const params = { natureComposantCode, operationCode, posteCode, codeArticleSageVersionne: articleCode, familleProduitCode: typeRobinet };
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
    const response = await apiClient.delete(`/hub/plans/FAB/${id}`);
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

  async upgradePlan(id) {
    const response = await apiClient.post(`/PlanFabrication/nouvelle-version`, { ancienId: id });
    return { data: { planId: response.data.id } }; // Formatting to match frontend expectations
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

    // Le backend stocke le nom avec un suffixe numérique ex: "C-25B0A01.1"
    // alors que l'articleCode du wizard est "C-25B0A01"
    // On vérifie si le nom du plan COMMENCE PAR le code article saisi
    const normalize = (s) => (s || '').trim().toUpperCase();
    const artNorm = normalize(articleCode);

    const matches = (plan) => {
      const nomNorm = normalize(plan.nom);
      const codeNorm = normalize(plan.codeArticleSageVersionne);
      return (
        nomNorm === artNorm ||
        nomNorm.startsWith(artNorm + '.') ||
        codeNorm === artNorm ||
        codeNorm.startsWith(artNorm + '.')
      );
    };

    const actif = plans.find(p => p.statut === 'ACTIF' && matches(p));
    const brouillon = plans.find(p => p.statut === 'BROUILLON' && matches(p));

    console.log('[verifierEtatPlan] article:', articleCode, '| plans:', plans.length, '| brouillon:', brouillon?.id || null, '| actif:', actif?.id || null);

    return {
      data: {
        hasBrouillon: !!brouillon,
        hasActif: !!actif,
        brouillonId: brouillon ? brouillon.id : null,
        actifId: actif ? actif.id : null,
        actifVersion: actif ? actif.version : null
      }
    };
  },


  async instantiatePlan(payload) {
    let newDoc = {
      nom: payload.codeArticleSage,
      designation: payload.designation,
      statut: payload.statut,
      versionInitiale: payload.versionInitiale || 1,
      operationCode: payload.operationCode,
      refFormulaireCodeReference: payload.refFormulaireCodeReference || 'PRC',
      natureArticleCode: payload.natureComposantCode,
      familleProduitFiniCode: payload.familleCode,
      posteCode: payload.posteCode,
      libre1: payload.codeArticleSage,
      legendeMoyens: payload.legendeMoyens,
      remarques: payload.remarques,
      colonneDefs: payload.colonneDefs || [],
      sections: []
    };

    if (payload.sections && payload.sections.length > 0) {
      // Cas Excel import ou Clone : les sections sont déjà préparées dans le payload
      newDoc.sections = payload.sections.map((s, idx) => ({
        ordreAffiche: s.ordreAffiche || idx + 1,
        libelleSection: s.libelleSection || s.nom || '',
        typeSectionId: s.typeSectionId || null,
        periodiciteId: s.periodiciteId || null,
        regleEchantillonnageId: s.regleEchantillonnageId || null,
        lignes: (s.lignes || []).map((l, lIdx) => ({
          ordreAffiche: l.ordreAffiche || lIdx + 1,
          libelleAffiche: l.libelleAffiche || '',
          typeCaracteristiqueId: l.typeCaracteristiqueId || null,
          typeControleId: l.typeControleId || null,
          moyenControleId: l.moyenControleId || null,
          moyenTexteLibre: l.moyenTexteLibre || null,
          instrumentCode: l.instrumentCode || null,
          periodiciteId: l.periodiciteId || null,
          limiteSpecTexte: l.limiteSpecTexte || null,
          estCritique: l.estCritique || false,
          instruction: l.instruction || null,
          observations: l.observations || null,
          imageBase64: l.imageBase64 || null,
          extraColonnes: l.extraColonnes || []
        }))
      }));
    } else if (payload.modeleSourceId) {
      // Cas Modèle : on récupère les sections depuis l'API modèle
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
          libre1: l.codeArticleSage,
          extraColonnes: l.extraColonnes || []
        }))
      }));
    }

    const response = await apiClient.post(`/PlanFabrication`, newDoc);
    return { data: { id: response.data.id || response.data } };
  },

  async clonePlan(payload) {
    const sourceRes = await apiClient.get(`/PlanFabrication/${payload.planExistantId}`);
    const source = sourceRes.data;

    let newDoc = {
      nom: `PC-${payload.nouveauCodeArticleSage}`,
      designation: payload.nouvelleDesignation,
      versionInitiale: 1,
      operationCode: source.operationCode,
      refFormulaireCodeReference: source.formulaireCodeReference || 'PRC',
      natureArticleCode: source.natureArticleCode,
      familleProduitFiniCode: source.familleProduitFiniCode,
      posteCode: source.posteCode,
      statut: 'BROUILLON',
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
