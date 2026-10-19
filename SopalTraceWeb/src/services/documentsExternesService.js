import apiClient from './apiClient';

const DELAI_FICHIER = 120000;

/**
 * Documents externes déposés par les services (achats, commercial, qualité). Le nom normalisé est formé par la base
 * à partir des numéros saisis ; le responsable qualité retrouve ces documents dans la traçabilité de chaque OF.
 */
const documentsExternesService = {
  /**
   * Types que l'utilisateur dépose : { code, libelle, referenceClientDeLaCommande, champs: [{ code, libelle,
   * longueurMaximale, format, dansLeNom }] } (champs dans l'ordre du nom).
   */
  getTypes: () => apiClient.get('/documents-externes/types').then(r => r.data.data),

  /** Commande client Sage et sa référence client : { numeroCommande, referenceCommandeClient } (aperçu du nom d'un BL). */
  getCommandeClient: (numero) => apiClient.get(`/documents-externes/commandes-clients/${encodeURIComponent(numero)}`).then(r => r.data.data),

  /**
   * Dépose un document : { typeCode, <numéros des champs>, fichier, remplacer, motifRemplacement }. Déjà déposé sans
   * remplacement : erreur 409 dont le message nomme le document en vigueur.
   */
  deposer: ({ fichier, ...valeurs }) => {
    const formulaire = new FormData()
    for (const [cle, valeur] of Object.entries(valeurs)) {
      if (valeur !== undefined && valeur !== null && valeur !== '') formulaire.append(cle, valeur)
    }
    if (fichier) formulaire.append('fichier', fichier)
    return apiClient.post('/documents-externes', formulaire, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: DELAI_FICHIER
    }).then(r => r.data)
  },

  /** Documents déposés, les plus récents d'abord (ceux du service de l'utilisateur ; tous pour le responsable qualité). */
  lister: (type, recherche) => apiClient.get('/documents-externes', { params: { type: type || undefined, recherche: recherche || undefined } })
    .then(r => r.data.data),

  fichier: (id) => apiClient.get(`/documents-externes/${id}/fichier`, { responseType: 'blob', timeout: DELAI_FICHIER }).then(r => r.data),

  /**
   * Relance du service qui dépose un document manquant (responsable qualité) : e-mail avec le lien de dépôt prérempli.
   * Renvoie le message de confirmation (service et nombre de destinataires).
   */
  relancer: (typeCode, numeros, numeroOf) =>
    apiClient.post('/documents-externes/relances', { typeCode, numeros, numeroOf }).then(r => r.data.message)
}

/**
 * Nom normalisé (comme la base) : les numéros saisis qui forment le nom, dans l'ordre des champs du type, séparés par
 * « - ». Un code est mis en majuscules ; une référence client garde son écriture, espaces et « / » remplacés par « _ ».
 * Pour un BL, la référence client (reprise de la commande Sage) termine le nom : valeurs.referenceCommandeClient.
 */
export const nomNormalise = (type, valeurs) => {
  if (!type) return ''
  const reference = (v) => v.replace(/[ /]/g, '_')
  const parties = type.champs.filter(c => c.dansLeNom !== false).map(c => {
    const valeur = (valeurs[c.code] ?? '').trim()
    return c.format === 'REFERENCE' ? reference(valeur) : valeur.toUpperCase()
  })
  if (type.referenceClientDeLaCommande) parties.push(reference((valeurs.referenceCommandeClient ?? '').trim()))
  return parties.every(Boolean) ? `${parties.join('-')}.pdf` : ''
}

export default documentsExternesService
