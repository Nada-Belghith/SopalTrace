/**
 * Utilitaires de transformation pour plans QualityPlans
 * Utilisé par tous les types de plans (Fabrication, Échantillonnage, PF, VerifMachine...)
 */

/**
 * Transforme la réponse backend d'un plan en structure d'édition frontend
 */
export function mapBackendPlanToEditor(backendData) {
  return {
    id: backendData.id,
    codeArticleSage: backendData.codeArticleSage || '',
    designation: backendData.designation || '',
    statut: backendData.statut,
    version: backendData.version || 1,
    sections: (backendData.sections || []).map(s => ({
      id: s.id,
      libelleSection: s.libelleSection,
      ordreAffiche: s.ordreAffiche,
      typeSectionId: s.typeSectionId || null,
      periodiciteId: s.periodiciteId || null,
      lignes: (s.lignes || []).map(l => ({
        id: l.id,
        ordreAffiche: l.ordreAffiche,
        outilSourceId: l.outilSourceId || null,
        typeCaracteristiqueId: l.typeCaracteristiqueId,
        libelleAffiche: l.libelleAffiche || '',
        typeControleId: l.typeControleId,
        moyenControleId: l.moyenControleId || null,
        groupeInstrumentId: l.groupeInstrumentId || null,
        instrumentCode: l.instrumentCode || null,
        periodiciteId: l.periodiciteId || null,
        valeurNominale: l.valeurNominale || null,
        toleranceSuperieure: l.toleranceSuperieure || null,
        toleranceInferieure: l.toleranceInferieure || null,
        unite: l.unite || '',
        limiteSpecTexte: l.limiteSpecTexte || '',
        observations: l.observations || '',
        instruction: l.instruction || '',
        estCritique: l.estCritique || false
      }))
    }))
  };
}

/**
 * Prépare les données pour submission (PUT /plans/{id}/valeurs)
 */
export function preparePlanValuesPayload(sections) {
  return sections.map(s => ({
    id: s.id && s.id.length <= 36 ? s.id : null,
    ordreAffiche: s.ordreAffiche,
    libelleSection: s.libelleSection,
    lignes: s.lignes.map(l => ({
      id: l.id && l.id.length <= 36 ? l.id : null,
      ordreAffiche: l.ordreAffiche,
      outilSourceId: l.outilSourceId,
      typeCaracteristiqueId: l.typeCaracteristiqueId,
      typeControleId: l.typeControleId,
      libelleAffiche: l.libelleAffiche,
      moyenControleId: l.moyenControleId,
      groupeInstrumentId: l.groupeInstrumentId,
      instrumentCode: l.instrumentCode,
      periodiciteId: l.periodiciteId,
      valeurNominale: l.valeurNominale,
      toleranceSuperieure: l.toleranceSuperieure,
      toleranceInferieure: l.toleranceInferieure,
      unite: l.unite,
      limiteSpecTexte: l.limiteSpecTexte,
      observations: l.observations,
      instruction: l.instruction,
      estCritique: l.estCritique
    }))
  }));
}

/**
 * Détermine s'il s'agit d'une mesure (pour afficher/cacher les tolérances)
 */
export function isMesure(typeControleId, typesControle = []) {
  const typeControl = typesControle.find(t => t.id === typeControleId);
  return typeControl && typeControl.code === 'MESURE';
}

/**
 * Nettoie les valeurs incompatibles quand on change le type de contrôle
 */
export function cleanupLineValuesOnTypeChange(ligne, typeControleId, isMesureType) {
  if (!isMesureType) {
    // Pas une mesure : vider les tolérances numériques
    ligne.valeurNominale = null;
    ligne.toleranceSuperieure = null;
    ligne.toleranceInferieure = null;
    ligne.unite = null;
  } else {
    // C'est une mesure : vider le texte libre
    ligne.limiteSpecTexte = null;
  }
}

/**
 * Crée une nouvelle section vierge pour un plan
 */
export function createEmptySection(index = 1) {
  return {
    id: crypto.randomUUID(),
    libelleSection: 'NOUVELLE SECTION',
    ordreAffiche: index,
    typeSectionId: null,
    periodiciteId: null,
    lignes: []
  };
}

/**
 * Crée une nouvelle ligne vierge pour une section
 */
export function createEmptyLine(typeCharacteristiqueId, typeControleId, index = 1) {
  return {
    id: crypto.randomUUID(),
    ordreAffiche: index,
    outilSourceId: null,
    typeCaracteristiqueId: typeCharacteristiqueId,
    libelleAffiche: 'Nouvelle Caractéristique',
    typeControleId: typeControleId,
    valeurNominale: null,
    toleranceSuperieure: null,
    toleranceInferieure: null,
    unite: '',
    limiteSpecTexte: '',
    estCritique: false
  };
}

/**
 * Prépare le payload pour instanciation d'un plan depuis un modèle
 */
export function prepareInstantiatePayload(modeleSourceId, codeArticleSage, designation) {
  return {
    modeleSourceId,
    codeArticleSage,
    designation,
    nom: `PC-${codeArticleSage}-V1`,
    creePar: 'ADMIN' // Sera remplacé par JWT en prod
  };
}

/**
 * Prépare le payload pour clonage d'un plan existant
 */
export function prepareClonePlanPayload(planSourceId, codeArticleSage, designation) {
  return {
    planExistantId: planSourceId,
    nouveauCodeArticleSage: codeArticleSage,
    nouvelleDesignation: designation,
    creePar: 'ADMIN'
  };
}
