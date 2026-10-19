/**
 * Lecture du compte rendu renvoyé par POST /api/ExcelImport/plan :
 * { sections, remarques, erreurs, warnings, lignesLues, succes }.
 */

/** Fichier non exploitable (aucun en-tête, aucune caractéristique) : l'erreur du serveur est levée telle quelle. */
export function verifierImportExcel(resultat) {
  const erreurs = resultat?.erreurs || []
  if (erreurs.length > 0 && !(resultat.sections?.length)) {
    throw new Error(erreurs.join(' '))
  }
  return resultat
}

/** Message de fin d'import : nombre de caractéristiques lues et points à vérifier. */
export function messageImportExcel(resultat, maxPoints = 4) {
  const sections = resultat?.sections?.length || 0
  const lignes = resultat?.lignesLues ?? (resultat?.sections || []).reduce((n, s) => n + (s.lignes?.length || 0), 0)
  const points = [...(resultat?.erreurs || []), ...(resultat?.warnings || [])]

  let detail = `${lignes} caractéristique(s) importée(s) dans ${sections} section(s).`
  if (points.length > 0) {
    const affiches = points.slice(0, maxPoints).join(' • ')
    const reste = points.length > maxPoints ? ` • … et ${points.length - maxPoints} autre(s)` : ''
    detail += ` À vérifier : ${affiches}${reste}`
  }

  return {
    severity: points.length > 0 ? 'warn' : 'success',
    summary: points.length > 0 ? 'Import terminé, points à vérifier' : 'Import réussi',
    detail,
    life: points.length > 0 ? 12000 : 4000
  }
}
