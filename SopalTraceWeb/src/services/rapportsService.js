import apiClient from './apiClient';

// Conversion PDF et fichiers volumineux : délai plus long que les appels courants.
const DELAI_FICHIER = 120000;

const lireFichier = (url, params) =>
  apiClient.get(url, { params, responseType: 'blob', timeout: DELAI_FICHIER }).then(r => r.data);

/**
 * Ouvre un PDF dans un nouvel onglet. L'onglet est ouvert avant le téléchargement (sinon le navigateur le bloque
 * comme fenêtre surgissante) ; s'il est bloqué malgré tout, le fichier est téléchargé.
 */
export async function ouvrirPdf(chargement, nomFichier = 'document.pdf') {
  const onglet = window.open('', '_blank');
  try {
    const blob = await chargement();
    const url = URL.createObjectURL(new Blob([blob], { type: 'application/pdf' }));
    if (onglet) onglet.location.href = url;
    else telechargerBlob(blob, nomFichier);
    setTimeout(() => URL.revokeObjectURL(url), 60000);
  } catch (e) {
    onglet?.close();
    throw e;
  }
}

export function telechargerBlob(blob, nomFichier) {
  const url = URL.createObjectURL(blob);
  const lien = document.createElement('a');
  lien.href = url;
  lien.download = nomFichier;
  document.body.appendChild(lien);
  lien.click();
  lien.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60000);
}

/**
 * Ouvre le PDF puis la boîte d'impression du navigateur (cadre invisible). Si le navigateur ne permet pas
 * l'impression depuis le cadre, le PDF s'ouvre dans un nouvel onglet.
 */
export async function imprimerPdf(chargement, nomFichier = 'document.pdf') {
  const blob = await chargement()
  const url = URL.createObjectURL(new Blob([blob], { type: 'application/pdf' }))
  const cadre = document.createElement('iframe')
  cadre.style.position = 'fixed'
  cadre.style.width = '0'
  cadre.style.height = '0'
  cadre.style.border = '0'
  cadre.src = url
  cadre.onload = () => {
    try {
      cadre.contentWindow.focus()
      cadre.contentWindow.print()
    } catch {
      window.open(url, '_blank') ?? telechargerBlob(blob, nomFichier)
    }
  }
  document.body.appendChild(cadre)
  setTimeout(() => { cadre.remove(); URL.revokeObjectURL(url) }, 600000)
}

/** Chemin d'un document : document qualité ou document d'exécution (réponses des opérateurs). */
const cheminSource = (source) => source.execControleDocumentStatutId
  ? `/rapports/documents/par-execution/${source.execControleDocumentStatutId}`
  : `/rapports/documents/par-document/${source.documentEnteteId}`

/**
 * Modèles de rapport (administrateur) et documents mis en forme à la demande (rôles qualité) : un document est
 * toujours mis en forme avec le modèle en vigueur à sa création ; le PDF est produit au premier clic puis conservé.
 */
const rapportsService = {
  getModeles: () => apiClient.get('/rapports/modeles').then(r => r.data.data),
  getFormulaires: (code) => apiClient.get(`/rapports/modeles/${encodeURIComponent(code)}/formulaires`).then(r => r.data.data),
  getChamps: (code, formulaireId) => apiClient.get(`/rapports/modeles/${encodeURIComponent(code)}/champs`,
    { params: formulaireId ? { formulaireId } : undefined }).then(r => r.data.data),

  /**
   * Envoie un nouveau fichier de modèle, vérifié contre la version de formulaire visée (sinon contre tous les
   * formulaires du type). Accepté : { version, avertissements, champs } ; refusé : erreur 400 dont
   * response.data.erreurs liste les points à corriger.
   */
  envoyerVersion: (code, fichier, commentaire, formulaireId) => {
    const formulaire = new FormData()
    formulaire.append('fichier', fichier)
    if (commentaire) formulaire.append('commentaire', commentaire)
    if (formulaireId) formulaire.append('formulaireId', formulaireId)
    return apiClient.post(`/rapports/modeles/${encodeURIComponent(code)}/versions`, formulaire, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: DELAI_FICHIER
    }).then(r => r.data)
  },
  publierParDefaut: (id, commentaire) => apiClient.post(`/rapports/modeles/versions/${id}/par-defaut`, { commentaire }).then(r => r.data),
  affecterAuFormulaire: (id, formulaireId, commentaire) =>
    apiClient.post(`/rapports/modeles/versions/${id}/formulaire`, { formulaireId, commentaire }).then(r => r.data),
  /** Nouveau logo : chaque formulaire choisi passe à sa version suivante avec ce modèle, ses documents actifs sont recréés. */
  changerLogo: (id, formulaireIds, commentaire) =>
    apiClient.post(`/rapports/modeles/versions/${id}/changement-logo`, { formulaireIds, commentaire }).then(r => r.data),
  reviser: (id, versionRemplaceeId, commentaire) =>
    apiClient.post(`/rapports/modeles/versions/${id}/revision`, { versionRemplaceeId, commentaire }).then(r => r.data),
  /**
   * Demandes de modèle : { id, formulaireId, codeFormulaire, versionFormulaire, origine (NEW_FORM | NEW_STRUCTURE |
   * SUPERVISOR), motif, changementVersion (SAME_VERSION | NEXT_VERSION), statut (PENDING | DONE | CANCELLED), … }.
   */
  demandesModele: (statut) => apiClient.get('/rapports/demandes-modele', { params: statut ? { statut } : undefined }).then(r => r.data.data),
  /** Superviseur qualité : nouveau modèle demandé pour une ligne de formulaire (même version ou version suivante). */
  demanderModele: (formulaireId, motif, changementVersion) =>
    apiClient.post(`/rapports/formulaires/${formulaireId}/demande-modele`, { motif, changementVersion }).then(r => r.data),
  annulerDemandeModele: (id) => apiClient.post(`/rapports/demandes-modele/${id}/annuler`).then(r => r.data),
  /** Administrateur : rattache la version qui satisfait la demande (le choix de version du superviseur s'applique). */
  traiterDemandeModele: (id, versionId, commentaire) =>
    apiClient.post(`/rapports/demandes-modele/${id}/traiter`, { versionId, commentaire }).then(r => r.data),
  /**
   * Le superviseur qualité change le code d'un formulaire (même version : l'ancien formulaire et ses documents sont
   * archivés tels quels, les documents recréés sous le nouveau code) ou crée le formulaire d'un champ d'application.
   */
  renommerCodeFormulaire: (formulaireId, code) => apiClient.put(`/referentiels/formulaires/${formulaireId}/code`, { code }).then(r => r.data),
  documentsActifsFormulaire: (formulaireId) => apiClient.get(`/referentiels/formulaires/${formulaireId}/documents-actifs`).then(r => r.data),
  creerFormulaire: (formulaire) => apiClient.post('/referentiels/formulaires', formulaire).then(r => r.data),
  /** Tous les formulaires en cours, par rôle, avec l'état de leur modèle Excel et les modèles du rôle. */
  getFormulairesParRole: () => apiClient.get('/rapports/formulaires').then(r => r.data.data),
  /**
   * Rôles des formulaires (table FormRole) : { code, libelle, ordre, nature (DOCUMENT | RESPONSE_SHEET), parFamille, parPoste, parMachine, parNature,
   * parTypeRobinet } ; nature et type de robinet ne désignent un formulaire que sans famille.
   */
  rolesFormulaire: () => apiClient.get('/referentiels/formulaires/roles').then(r => r.data.data),
  /** Familles (avec leur type de robinet), postes, machines, natures et types de robinet actifs : listes du champ d'application. */
  listesChampApplication: () => apiClient.get('/referentiels/formulaires/champs-application').then(r => r.data.data),
  apercuVersion: (id, params) => lireFichier(`/rapports/modeles/versions/${id}/apercu`, params),
  fichierVersion: (id) => lireFichier(`/rapports/modeles/versions/${id}/fichier`),

  getTracabilite: (numeroOf) => apiClient.get(`/rapports/tracabilite/${encodeURIComponent(numeroOf)}`).then(r => r.data.data),
  getEtat: (source) => apiClient.get(cheminSource(source)).then(r => r.data.data),
  /** PDF du document (produit au premier clic) ; modeleActuel : réimpression avec le modèle le plus récent. */
  pdfDocument: (source, modeleActuel = false) => lireFichier(`${cheminSource(source)}/pdf`, modeleActuel ? { modeleActuel } : undefined),
  excelDocument: (source, modeleActuel = false) => lireFichier(`${cheminSource(source)}/excel`, modeleActuel ? { modeleActuel } : undefined),
  /** Fiche vierge du document qualité (celle que rempliront les opérateurs), en PDF ou en Excel rempli. */
  ficheViergePdf: (documentEnteteId) => lireFichier(`/rapports/documents/par-document/${documentEnteteId}/fiche-vierge/pdf`),
  ficheViergeExcel: (documentEnteteId) => lireFichier(`/rapports/documents/par-document/${documentEnteteId}/fiche-vierge/excel`),
  /** Fichier Excel du modèle qui met en forme le document (administrateur, superviseur qualité). */
  modeleDocument: (source, modeleActuel = false) => lireFichier(`${cheminSource(source)}/modele`, modeleActuel ? { modeleActuel } : undefined),
  /** PDF ou Excel d'un document déjà produit (historique). */
  pdf: (id) => lireFichier(`/rapports/documents/${id}/pdf`),
  excel: (id) => lireFichier(`/rapports/documents/${id}/excel`)
}

export default rapportsService
