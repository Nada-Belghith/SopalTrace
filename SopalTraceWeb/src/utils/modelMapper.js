/**
 * Utilitaires de transformation pour modèles QualityPlans
 * Utilisé par tous les types de modèles (Fabrication, Échantillonnage, PF, VerifMachine...)
 */

/**
 * Transforme la réponse backend d'un modèle en structure d'édition frontend
 * @param {Object} backendData - Données du backend
 * @returns {Object} Données formatées pour l'édition
 */
export function mapBackendModeleToEditor(backendData) {
  return {
    id: backendData.id,
    code: backendData.code,
    libelle: backendData.libelle,
    statut: backendData.statut,
    version: backendData.version,
    operationCode: backendData.operationCode || '',
    natureComposantCode: backendData.natureComposantCode || '',
    typeRobinetCode: backendData.typeRobinetCode || '',
    notes: backendData.notes || '',
    sections: (backendData.sections || []).map(mapBackendSectionToEditor)
  };
}

/**
 * Transforme une section backend en structure d'édition
 */
export function mapBackendSectionToEditor(section) {
  return {
    id: section.id,
    isFromDb: true,
    typeSectionId: section.typeSectionId || '',
    libelleSection: section.libelleSection || 'SECTION SANS NOM',
    ordreAffiche: section.ordreAffiche || 1,
    frequenceLibelle: section.frequenceLibelle || '',
    periodiciteId: section.periodiciteId || null,
    notes: section.notes || '',
    lignes: (section.lignes || []).map(mapBackendLigneToEditor)
  };
}

/**
 * Transforme une ligne backend en structure d'édition
 */
export function mapBackendLigneToEditor(ligne) {
  return {
    id: ligne.id,
    isFromDb: true,
    ordreAffiche: ligne.ordreAffiche || 1,
    typeCaracteristiqueId: ligne.typeCaracteristiqueId || '',
    libelleAffiche: ligne.libelleAffiche || '',
    typeControleId: ligne.typeControleId || '',
    moyenControleId: ligne.moyenControleId || null,
    // ⚠️ groupeInstrumentId supprimé
    instrumentCode: ligne.instrumentCode || null,
    periodiciteId: ligne.periodiciteId || null,
    instruction: ligne.instruction || '',
    estCritique: ligne.estCritique || false,
    valeurNominale: ligne.valeurNominale ?? null,
    toleranceInferieure: ligne.toleranceInferieure ?? null,
    toleranceSuperieure: ligne.toleranceSuperieure ?? null,
    unite: ligne.unite || '',
    limiteSpecTexte: ligne.limiteSpecTexte || '',
    observations: ligne.observations || '',
    moyenTexteLibre: ligne.moyenTexteLibre || ''
  };
}

/**
 * Prépare les données d'édition pour envoi au backend (créer/mettre à jour modèle)
 */
export function prepareModelePayload(entete, sections) {
  return {
    code: entete.code || 'MOD-AUTO-GEN',
    libelle: entete.libelle || `Modèle ${entete.code}`,
    typeRobinetCode: entete.typeRobinetCode || null,
    natureComposantCode: entete.natureComposantCode || null,
    operationCode: entete.operationCode || null,
    notes: entete.notes || '',
    sections: sections.map((s, idx) => ({
      ordreAffiche: idx + 1,
      typeSectionId: s.typeSectionId || null,
      libelleSection: s.libelleSection || 'SECTION SANS NOM',
      periodiciteId: s.periodiciteId || null,
      frequenceLibelle: s.frequenceLibelle || '',
      notes: s.notes || '',
      lignes: s.lignes.map((l, lIdx) => ({
        ordreAffiche: lIdx + 1,
        typeCaracteristiqueId: l.typeCaracteristiqueId || null,
        libelleAffiche: l.libelleAffiche,
        typeControleId: l.typeControleId || null,
        moyenControleId: l.moyenControleId || null,
        // ⚠️ groupeInstrumentId supprimé
        instrumentCode: l.instrumentCode,
        periodiciteId: l.periodiciteId,
        instruction: l.instruction,
        estCritique: l.estCritique,
        valeurNominale: l.valeurNominale ?? null,
        toleranceInferieure: l.toleranceInferieure ?? null,
        toleranceSuperieure: l.toleranceSuperieure ?? null,
        unite: l.unite || '',
        limiteSpecTexte: l.limiteSpecTexte || '',
        observations: l.observations || '',
        moyenTexteLibre: l.moyenTexteLibre || ''
      }))
    }))
  };
}

/**
 * Crée un snapshot pour dirty checking (détection de modifications)
 */
export function createModeleSnapshot(entete, sections) {
  return JSON.stringify({
    entete: {
      operationCode: entete.operationCode,
      natureComposantCode: entete.natureComposantCode,
      typeRobinetCode: entete.typeRobinetCode,
      libelle: entete.libelle,
      notes: entete.notes
    },
    sections: sections.map(s => ({
      typeSectionId: s.typeSectionId,
      modeFreq: s.modeFreq,
      periodiciteId: s.periodiciteId,
      freqNum: s.freqNum,
      typeVariable: s.typeVariable,
      freqHours: s.freqHours,
      nom: s.nom,
      lignes: s.lignes.map(l => ({
        typeCaracteristiqueId: l.typeCaracteristiqueId,
        libelleAffiche: l.libelleAffiche,
        typeControleId: l.typeControleId,
        moyenControleId: l.moyenControleId,
        // ⚠️ groupeInstrumentId supprimé
        instrumentCode: l.instrumentCode,
        instruction: l.instruction,
        estCritique: l.estCritique,
        valeurNominale: l.valeurNominale,
        toleranceInferieure: l.toleranceInferieure,
        toleranceSuperieure: l.toleranceSuperieure,
        unite: l.unite,
        limiteSpecTexte: l.limiteSpecTexte,
        observations: l.observations,
        moyenTexteLibre: l.moyenTexteLibre
      }))
    }))
  });
}

/**
 * Détecte si la structure a été modifiée (compare snapshots)
 */
export function hasModeleChanged(snapshotBefore, snapshotAfter) {
  if (!snapshotBefore || !snapshotAfter) return true;
  return snapshotBefore !== snapshotAfter;
}

/**
 * Génère un code de modèle automatique (ou vide si déjà présent)
 */
export function generateModeleCode(operation, nature, type) {
  if (!operation || !nature || !type) return null;
  return `MOD-${operation}-${nature}-${type}-V1`.toUpperCase();
}

/**
 * Prépare les données et crée/retrouve les périodicités
 * Fonction extraite pour garder les composants clean
 * @param {Array} sections - Sections/groupes du modèle
 * @param {Array} existingPeriodicites - Périodicités existantes en base
 * @param {Function} createPeriodiciteCallback - Fonction pour créer une nouvelle périodicité via API
 * @returns {Promise<Array>} Sections formatées pour envoi backend
 */
// ⚠️ CORRECTION : Ajout du paramètre "sections" manquant !
export async function prepareModeleDataAndFrequencies(sections, existingPeriodicites, createPeriodiciteCallback) {
  // Traiter les périodicités variables
  for (const g of sections) {
    if (g.modeFreq === 'VARIABLE' && !g.periodiciteId) {
      let libelleFreq = '';
      let codeFreq = '';
      const sP = g.freqNum > 1 ? 's' : '';

      // Formater le libellé selon le type
      if (g.typeVariable === 'HEURE') {
        const sH = g.freqHours > 1 ? 's' : '';
        libelleFreq = g.freqHours === 1
          ? `${g.freqNum} pièce${sP} / heure`
          : `${g.freqNum} pièce${sP} / ${g.freqHours} heure${sH}`;
        codeFreq = `${g.freqNum}P_${g.freqHours}H`;
      } else if (g.typeVariable === 'SERIE') {
        libelleFreq = `une série de ${g.freqNum} pièces`;
        codeFreq = `SERIE_${g.freqNum}P`;
      }

      // Chercher si cette périodicité existe déjà
      const perioExistante = existingPeriodicites.find(
        p => p.libelle.toLowerCase() === libelleFreq.toLowerCase()
      );

      if (perioExistante) {
        g.periodiciteId = perioExistante.id;
      } else {
        // Créer une nouvelle périodicité
        const payloadFreq = {
          code: codeFreq,
          libelle: libelleFreq,
          frequenceNum: g.freqNum,
          frequenceUnite: g.typeVariable === 'HEURE' ? `${g.freqHours}_HEURE` : 'SERIE',
          ordreAffichage: 5
        };
        const res = await createPeriodiciteCallback(payloadFreq);
        g.periodiciteId = res.data.periodiciteId || res.data.id;
        // Note: Le composant doit mettre à jour store.periodicites lui-même
      }
    }
  }

  // Formater les sections pour envoi backend
  return sections.map((g, idx) => ({
    id: g.isFromDb ? g.id : null,
    ordreAffiche: idx + 1,
    typeSectionId: g.typeSectionId || null,
    libelleSection: g.nom,
    periodiciteId: g.periodiciteId,
    frequenceLibelle: g.periodiciteId
      ? (existingPeriodicites || []).find(p => p.id === g.periodiciteId)?.libelle
      : '',
    lignes: g.lignes.map((l, lIdx) => ({
      id: l.isFromDb ? l.id : null,
      ordreAffiche: lIdx + 1,
      typeCaracteristiqueId: l.typeCaracteristiqueId || null,
      libelleAffiche: l.libelleAffiche,
      typeControleId: l.typeControleId || null,
      moyenControleId: l.moyenControleId || null,
      // ⚠️ groupeInstrumentId supprimé
      instrumentCode: l.instrumentCode,
      periodiciteId: g.periodiciteId,
      instruction: l.instruction,
      estCritique: l.estCritique,
      // Préserver valeurs numériques et champs libres pour sauvegarde
      valeurNominale: l.valeurNominale ?? null,
      toleranceSuperieure: l.toleranceSuperieure ?? null,
      toleranceInferieure: l.toleranceInferieure ?? null,
      unite: l.unite || '',
      limiteSpecTexte: l.limiteSpecTexte || '',
      observations: l.observations || '',
      moyenTexteLibre: l.moyenTexteLibre || ''
    }))
  }));
}