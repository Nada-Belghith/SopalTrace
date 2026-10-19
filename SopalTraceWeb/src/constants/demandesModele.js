// Demandes de modèle (table ReportTemplateRequest) et nature des types de formulaires (FormRole.Nature).

export const NATURES_FORMULAIRE = {
  DOCUMENT: 'Document',
  RESPONSE_SHEET: 'Fiche de réponse'
}

export const ORIGINES_DEMANDE = {
  NEW_FORM: 'Nouveau formulaire',
  NEW_STRUCTURE: 'Nouvelle structure (colonnes changées)',
  SUPERVISOR: 'Demande du superviseur qualité'
}

export const CHANGEMENTS_VERSION = {
  SAME_VERSION: 'Même version',
  NEXT_VERSION: 'Version suivante'
}

export const STATUTS_DEMANDE = {
  PENDING: 'En attente',
  DONE: 'Traitée',
  CANCELLED: 'Annulée'
}

/** Ce que demande la demande, en une ligne (« Demande du superviseur qualité · version suivante »). */
export const libelleDemande = (demande) => demande.origine === 'SUPERVISOR'
  ? `${ORIGINES_DEMANDE.SUPERVISOR} · ${CHANGEMENTS_VERSION[demande.changementVersion]?.toLowerCase() ?? ''}`
  : ORIGINES_DEMANDE[demande.origine] ?? demande.origine
