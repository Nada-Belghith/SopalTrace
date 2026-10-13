/**
 * Valeurs partagées avec l'API pour l'exécution des OF (miroir de Domain/Constants/ExecutionConstantes.cs).
 */

export const OPERATION_ASSEMBLAGE = 'ASS'

export const STATUTS_EXECUTION = Object.freeze({
  EN_COURS: 'EN_COURS',
  EN_PAUSE: 'EN_PAUSE',
  REGLAGE: 'REGLAGE',
  CLOTURE: 'CLOTURE'
})

/** Statuts d'un OF déjà démarré (qu'une équipe peut rejoindre). */
export const STATUTS_EXECUTION_OUVERTS = Object.freeze([STATUTS_EXECUTION.EN_COURS, STATUTS_EXECUTION.REGLAGE, STATUTS_EXECUTION.EN_PAUSE])

export const TYPES_DOCUMENT = Object.freeze({
  VERIF_MACHINE: 'VERIF_MACHINE',
  CLOTURE_VM_POSTE: 'CLOTURE_VM_POSTE',
  RESULTAT_CONTROLE_POSTE: 'RESULTAT_CONTROLE_POSTE',
  CLOTURE_RC_POSTE: 'CLOTURE_RC_POSTE',
  ECHANTILLONNAGE: 'ECHANTILLONNAGE',
  PLAN_ASS: 'PLAN_ASS',
  REGISTRE_TRACA: 'REGISTRE_TRACA',
  RESULTAT_CONTROLE_CF: 'RESULTAT_CONTROLE_CF'
})

export const PERIODICITES = Object.freeze({
  DEMARRAGE: 'DEMARRAGE',
  APRES_PAUSE: 'APRES_PAUSE',
  FIN_POSTE: 'FIN_POSTE'
})

export const TYPES_PAUSE = Object.freeze({
  DEJEUNER: 'DEJEUNER',
  AUTRE: 'AUTRE'
})

/**
 * Moment d'une saisie sur le plan d'assemblage : première / dernière pièce (sections de lot),
 * réglage et toutes les pièces (contrôles à la demande de l'opérateur).
 */
export const MOMENTS_LOT = Object.freeze({
  PREMIERE_PIECE: 'PREMIERE_PIECE',
  DERNIERE_PIECE: 'DERNIERE_PIECE',
  REGLAGE: 'REGLAGE',
  TOUTES_PIECES: '100PCT'
})

/** Étapes de la session d'assemblage, calculées par le serveur (PhasesAssemblage). */
export const PHASES_ASSEMBLAGE = Object.freeze({
  ECHANTILLONNAGE: 'ECHANTILLONNAGE',
  DEMARRAGE: 'DEMARRAGE',
  PRODUCTION: 'PRODUCTION',
  PAUSE: 'PAUSE',
  REPRISE: 'REPRISE',
  FIN_POSTE: 'FIN_POSTE',
  FIN_OF: 'FIN_OF',
  CLOTURE: 'CLOTURE',
  SANS_EQUIPE: 'SANS_EQUIPE'
})

/** Tâches de l'assistant (TachesAssemblage côté serveur). */
export const TACHES_ASSEMBLAGE = Object.freeze({
  ECHANTILLONNAGE: 'ECHANTILLONNAGE',
  VERIF_MACHINE: 'VERIF_MACHINE',
  PLAN_PREMIERE_PIECE: 'PLAN_PREMIERE_PIECE',
  CONTROLES_PRODUCTION: 'CONTROLES_PRODUCTION',
  CONTROLE_POSTE: 'CONTROLE_POSTE',
  PLAN_DERNIERE_PIECE: 'PLAN_DERNIERE_PIECE',
  TRACABILITE: 'TRACABILITE',
  REGLAGE: 'REGLAGE',
  TOUTES_PIECES: 'TOUTES_PIECES'
})

export const ETATS_TACHE = Object.freeze({
  A_FAIRE: 'A_FAIRE',
  FAIT: 'FAIT',
  MANQUANT: 'MANQUANT',
  /** Rien à faire à ce moment : le serveur ne renvoie pas ces tâches dans l'étape. */
  NON_REQUIS: 'NON_REQUIS'
})

/** État de la vérification d'une machine du poste pour un moment (EtatsVerifMachine côté serveur). */
export const ETATS_VERIF_MACHINE = Object.freeze({
  A_FAIRE: 'A_FAIRE',
  EN_COURS: 'EN_COURS',
  VALIDEE: 'VALIDEE',
  NON_PREVUE: 'NON_PREVUE',
  PLAN_MANQUANT: 'PLAN_MANQUANT'
})

/** Libellés des moments de vérification machine. */
export const LIBELLES_PERIODICITE = Object.freeze({
  DEMARRAGE: 'Démarrage',
  APRES_PAUSE: 'Après la pause',
  FIN_POSTE: 'Fin de poste'
})

/**
 * Équipes d'un document de contrôle au poste qui n'en définit pas (3 × 8 h). Le superviseur les modifie dans
 * le document ; chaque équipe est rattachée à son code (celui de la session d'équipe).
 */
export const EQUIPES_CONTROLE_POSTE_PAR_DEFAUT = Object.freeze([
  { code: 'E1', nom: 'Équipe 1', debut: 6, fin: 14 },
  { code: 'E2', nom: 'Équipe 2', debut: 14, fin: 22 },
  { code: 'E3', nom: 'Équipe 3', debut: 22, fin: 6 }
])

export const DEMANDES_CLOTURE = Object.freeze({
  FIN_POSTE: 'FIN_POSTE',
  FIN_OF: 'FIN_OF'
})

export const RESULTATS_CONTROLE = Object.freeze({
  CONFORME: 'C',
  NON_CONFORME: 'NC'
})
