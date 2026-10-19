/**
 * Types des colonnes ajoutées par formulaire (miroir de la table ColumnValueType et de
 * TypesValeurColonne côté API). La valeur saisie est toujours transmise en texte :
 * nombre « 12.5 », date « AAAA-MM-JJ », oui/non « true » / « false », liste = une des options.
 */
export const TYPES_COLONNE = Object.freeze({
  TEXTE: 'TEXT',
  NOMBRE: 'NUMBER',
  DATE: 'DATE',
  OUI_NON: 'BOOLEAN',
  LISTE: 'LIST'
})

export const OPTIONS_TYPES_COLONNE = Object.freeze([
  { value: TYPES_COLONNE.TEXTE, label: 'Texte' },
  { value: TYPES_COLONNE.NOMBRE, label: 'Nombre' },
  { value: TYPES_COLONNE.DATE, label: 'Date' },
  { value: TYPES_COLONNE.OUI_NON, label: 'Oui / Non' },
  { value: TYPES_COLONNE.LISTE, label: 'Liste de valeurs' }
])

const ANCIENS_LIBELLES = { TEXTE: 'TEXT', NOMBRE: 'NUMBER', LISTE: 'LIST', BOOLEEN: 'BOOLEAN', 'BOOLÉEN': 'BOOLEAN' }

/** Ramène un type (code ou ancien libellé français) à son code ; vide = texte. */
export function normaliserTypeColonne(type) {
  if (!type) return TYPES_COLONNE.TEXTE
  const code = String(type).trim().toUpperCase()
  return ANCIENS_LIBELLES[code] || code
}

/** Découpe la saisie des options d'une liste (une par ligne ou séparées par des virgules). */
export function lireOptionsColonne(texte) {
  return [...new Set(String(texte || '').split(/[\n,;]/).map(o => o.trim()).filter(Boolean))]
}
