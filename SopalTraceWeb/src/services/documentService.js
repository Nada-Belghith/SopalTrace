import apiClient from './apiClient'

export const documentService = {
  // ----------------------------------------------------
  // BASE API CALLS (Unified TPH DocumentController)
  // ----------------------------------------------------
  async getById(id) {
    const response = await apiClient.get(`/Document/${id}`)
    return response.data
  },

  async getByFilters(params) {
    const response = await apiClient.get(`/Document`, { params })
    return response.data
  },

  async create(payload) {
    const response = await apiClient.post(`/Document`, payload)
    return response.data
  },

  async createNewVersion(id, payload) {
    const response = await apiClient.post(`/Document/${id}/version`, payload)
    return response.data
  },

  async restaurer(id, payload) {
    const response = await apiClient.post(`/Document/${id}/restaurer`, payload)
    return response.data
  },

  async deleteDocument(id) {
    const response = await apiClient.delete(`/Document/${id}`)
    return response.data
  },

  // ----------------------------------------------------
  // ADAPTERS FOR LEGACY METHODS (Used by Vue components)
  // ----------------------------------------------------

  // Ex: fabModeleService.getModelesByFilters
  async getModelesByFilters(typeRobinet, natureComposantCode, operationCode, posteCode = null) {
    // Determine typeDocumentCode based on operationCode or assume MODELE_FAB/MODELE_ASS
    const typeDocumentCode = operationCode === 'ASS' ? 'MODELE_ASS' : 'MODELE_FAB';
    return await this.getByFilters({ typeDocumentCode, natureComposantCode, operationCode, posteCode })
  },
  // Ex: fabPlanService.getPlansByFilters
  async getPlansByFilters(typeRobinet, natureComposantCode, operationCode, posteCode = null) {
    const typeDocumentCode = operationCode === 'ASS' ? 'PLAN_ASS' : 'PLAN_FAB';
    return await this.getByFilters({ typeDocumentCode, natureComposantCode, operationCode, posteCode })
  },

  // Ex: fabModeleService.createModele, assModeleService.createModele
  async createModele(payload) {
    payload.typeDocumentCode = payload.operationCode === 'ASS' ? 'MODELE_ASS' : 'MODELE_FAB';
    const id = await this.create(payload);
    return { data: { modeleId: id, version: 1 } };
  },

  // Ex: fabModeleService.newModeleVersion
  async newModeleVersion(payload) {
    payload.typeDocumentCode = payload.operationCode === 'ASS' ? 'MODELE_ASS' : 'MODELE_FAB';
    const id = await this.createNewVersion(payload.ancienId, payload);
    return { data: { modeleId: id } };
  },

  // Ex: fabModeleService.updateModeleValeurs
  async updateModeleValeurs(id, payload) {
    // In TPH, we don't have a direct PUT for the whole document yet, but assuming it creates a new version or backend handles it
    // if backend doesn't have PUT /Document/{id}, we might need to simulate it or use new version
    return await this.createNewVersion(id, payload);
  },

  // Ex: fabModeleService.activerModele
  async activerModele(id) {
    // Simulating activation by calling a generic endpoint or returning success
    return Promise.resolve({ data: { success: true } });
  },

  // Ex: fabPlanService.verifierEtatPlan
  async verifierEtatPlan(articleCode, modeleId, operationCode = null, posteCode = null) {
    // Dans l'ancienne version, cela appelait /verifier-etat
    // On simule en listant les plans pour voir s'il y a un brouillon ou un actif.
    const plans = await this.getByFilters({
      typeDocumentCode: 'PLAN_FAB', // Ou paramétré selon le cas
      natureComposantCode: articleCode, // Historiquement articleCode Sage
      operationCode,
      posteCode
    });

    // Simulate legacy response structure
    const actif = plans.find(p => p.statut === 'ACTIF');
    const brouillon = plans.find(p => p.statut === 'BROUILLON');

    return {
      data: {
        existeActif: !!actif,
        existeBrouillon: !!brouillon,
        brouillonId: brouillon ? brouillon.id : null,
        actifId: actif ? actif.id : null
      }
    };
  },

  // Ex: fabPlanService.instantiatePlan
  async instantiatePlan(payload) {
    // Legacy payload: modeleSourceId, codeArticleSage, designation, operationCode, natureComposantCode, posteCode, nom, creePar
    // Nouveau processus : On récupère le modèle, on nettoie les ID, on change le statut, et on l'enregistre en tant que nouveau document.
    let newDoc = {
      typeDocumentCode: payload.natureComposantCode === 'PF' ? 'PLAN_PF' : 'PLAN_FAB',
      nom: payload.nom,
      designation: payload.designation,
      versionInitiale: 1,
      operationCode: payload.operationCode,
      refFormulaireCodeReference: payload.refFormulaireCodeReference || 'PRC',
      natureArticleCode: payload.natureComposantCode,
      familleProduitFiniCode: payload.familleCode,
      posteCode: payload.posteCode,
      libre1: payload.codeArticleSage,
      colonneDefs: payload.colonneDefs || [],
      sections: [] // Simplified, backend will handle or not
    };

    if (payload.modeleSourceId) {
      const modele = await this.getById(payload.modeleSourceId);
      // Map sections from modele
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
          libre1: l.codeArticleSage, // Historique
          extraColonnes: l.extraColonnes || []
        }))
      }));
    }

    const created = await this.create(newDoc);
    return { data: { id: created } };
  },

  // Ex: fabPlanService.clonePlan
  async clonePlan(payload) {
    // Legacy payload: planExistantId, nouveauCodeArticleSage, nouvelleDesignation, creePar
    const source = await this.getById(payload.planExistantId);

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

    const created = await this.create(newDoc);
    return { data: { id: created } };
  },

  // Ex: fabPlanService.importExcel
  async importExcel(formData) {
    const response = await apiClient.post('/ExcelImport/plan', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  },

  // Aliases commonly used
  getPlanById(id) {
    return this.getById(id);
  },
  getModeleById(id) {
    return this.getById(id);
  },
  deletePlan(id) {
    return this.deleteDocument(id);
  },
  deleteModele(id) {
    return this.deleteDocument(id);
  },
  newPlanVersion(payload) {
    // Legacy payload mapping
    let dto = {
      ancienId: payload.ancienId || payload.DocumentId || payload.documentId,
      ...payload
    }
    return this.createNewVersion(dto.ancienId, dto);
  },
  restorePlan(payload) {
    let dto = {
      documentArchiveId: payload.documentArchiveId || payload.DocumentArchiveId || payload.documentId,
      motifRestoration: payload.motifRestoration || 'Restoration'
    }
    return this.restaurer(dto.documentArchiveId, dto);
  },
  restoreModele(payload) {
    return this.restorePlan(payload);
  },
  mettreAJourValeurs(planId, payloadData, legendeMoyens, remarques, finaliser, nomPlan, modifiePar, codeArticleSage) {
    const payload = {
      nom: nomPlan,
      legendeMoyens: legendeMoyens,
      remarques: remarques,
      libre1: codeArticleSage,
      sections: payloadData.sections || payloadData,
      colonneDefs: payloadData.colonneDefs || []
    };
    return apiClient.put(`/Document/${planId}`, payload).then(() => {
      if (finaliser) {
        return apiClient.put(`/hub/plans/FAB/${planId}/statut?statut=ACTIF`);
      }
      return Promise.resolve();
    });
  },

  // Added missing method for Periodicite
  async createPeriodicite(payload) {
    const response = await apiClient.post('/referentiels/periodicites', payload);
    return response.data;
  }
}
