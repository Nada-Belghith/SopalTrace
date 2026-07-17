/**
 * Utilitaires de transformation pour modèles QualityPlans
 * Utilisé par tous les types de modèles (Fabrication, Échantillonnage, PF, VerifMachine...)
 */

// Les fonctions mapBackendModeleToEditor, mapBackendSectionToEditor, mapBackendLigneToEditor, et prepareModelePayload
// ont été remplacées et unifiées dans src/utils/sectionUtils.js

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
      notes: entete.notes,
      configurationColonnes: typeof entete.configurationColonnes === 'string' ? JSON.parse(entete.configurationColonnes || '[]') : (entete.configurationColonnes || [])
    },
    sections: sections.map(s => ({
      typeSectionId: s.typeSectionId,
      modeFreq: s.modeFreq,
      periodiciteId: s.periodiciteId,
      freqNum: s.freqNum,
      typeVariable: s.typeVariable,
      freqHours: s.freqHours,
      libelleSection: s.libelleSection,
      lignes: s.lignes.map(l => ({
        typeCaracteristiqueId: l.typeCaracteristiqueId,
        libelleAffiche: l.libelleAffiche,
        typeControleId: l.typeControleId,
        moyenControleId: l.moyenControleId,
        // ⚠️ groupeInstrumentId supprimé
        instrumentCode: l.instrumentCode,
        instruction: l.instruction,
        estCritique: l.estCritique,
        unite: l.unite,
        limiteSpecTexte: l.limiteSpecTexte,
        observations: l.observations,
        moyenTexteLibre: l.moyenTexteLibre,
        valeursColonnesSpecifiques: l.valeursColonnesSpecifiques || {}
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
  return `MOD-${operation}-${nature}-${type}`.toUpperCase();
}

// La fonction prepareModeleDataAndFrequencies a été unifiée et migrée vers 
// prepareSectionsForBackend dans src/utils/sectionUtils.js