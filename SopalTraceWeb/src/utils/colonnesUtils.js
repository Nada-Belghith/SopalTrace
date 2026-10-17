import { normaliserTypeColonne } from '@/constants/columnTypes'

/** Colonnes propres d'un document relu (colonneDefs du serveur) au format de l'éditeur. */
export const colonnesDepuisDocument = (colonneDefs, insertAfterParDefaut = 'code_instrument') => (colonneDefs || []).map(c => ({
  key: c.cleColonne || c.key,
  label: c.labelAffiche || c.label,
  type: normaliserTypeColonne(c.typeValeur || c.type),
  options: c.options || [],
  insertAfter: c.insertAfter || insertAfterParDefaut
}))

/** Colonnes changées depuis le chargement (empreinte gardée dans l'en-tête) : seule une nouvelle version est possible. */
export const colonnesModifiees = (colonnes, signatureChargee) => !!signatureChargee && signatureColonnes(colonnes) !== signatureChargee

/**
 * Empreinte de la configuration des colonnes (clé, libellé, type, valeurs proposées), indépendante de l'ordre :
 * deux empreintes différentes = structure changée, une correction mineure est alors impossible.
 */
export const signatureColonnes = (colonnes) => JSON.stringify((Array.isArray(colonnes) ? colonnes : [])
  .map(c => [c.key ?? c.cleColonne ?? '', c.label ?? c.labelAffiche ?? '', normaliserTypeColonne(c.type ?? c.typeValeur), [...(c.options || [])]])
  .sort((a, b) => a[0].localeCompare(b[0])))
