/**
 * Statuts de compte renvoyés par l'API d'administration (miroir de AccountStatuses côté serveur).
 * Cycle de vie : PENDING_APPROVAL → INVITED (approuvée) ou REJECTED (refusée) ; INVITED → ACTIVE (mot de passe défini) ;
 * ACTIVE / INVITED ↔ DISABLED. LOCKED : compte actif verrouillé temporairement après des échecs de connexion.
 */
export const STATUTS_COMPTE = Object.freeze({
  PENDING_APPROVAL: { libelle: 'Demande à traiter', classes: 'bg-amber-50 text-amber-700 border-amber-200', point: 'bg-amber-500' },
  INVITED: { libelle: 'Invitation envoyée', classes: 'bg-sky-50 text-sky-700 border-sky-200', point: 'bg-sky-500' },
  ACTIVE: { libelle: 'Actif', classes: 'bg-emerald-50 text-emerald-600 border-emerald-100', point: 'bg-emerald-500' },
  LOCKED: { libelle: 'Verrouillé', classes: 'bg-red-50 text-red-600 border-red-200', point: 'bg-red-500' },
  REJECTED: { libelle: 'Demande refusée', classes: 'bg-rose-50 text-rose-700 border-rose-200', point: 'bg-rose-500' },
  DISABLED: { libelle: 'Désactivé', classes: 'bg-slate-100 text-slate-500 border-slate-200', point: 'bg-slate-400' }
});

/** Libellés des événements du journal des connexions. */
export const ACTIONS_JOURNAL = Object.freeze({
  CONNEXION: 'Connexion',
  CONNEXION_ECHEC: 'Échec de connexion',
  DECONNEXION: 'Déconnexion',
  INSCRIPTION: 'Demande d\'accès',
  INSCRIPTION_REFUSEE: 'Demande d\'accès non retenue',
  INSCRIPTION_DOUBLON: 'Demande déjà enregistrée',
  DEMANDE_APPROUVEE: 'Demande approuvée',
  DEMANDE_REFUSEE: 'Demande refusée',
  INVITATION_RENVOYEE: 'Lien d\'activation renvoyé',
  EMAIL_NON_ENVOYE: 'E-mail non envoyé',
  ACTIVATION_COMPTE: 'Activation du compte',
  MOT_DE_PASSE_OUBLIE: 'Mot de passe oublié',
  MOT_DE_PASSE_REINITIALISE: 'Mot de passe réinitialisé',
  SESSION_REVOQUEE: 'Session révoquée (jeton réutilisé)',
  ROLE_MODIFIE: 'Rôle modifié',
  ROLE_PREATTRIBUE: 'Rôle pré-attribué',
  PREATTRIBUTION_SUPPRIMEE: 'Pré-attribution supprimée',
  COMPTE_DESACTIVE: 'Compte désactivé',
  COMPTE_REACTIVE: 'Compte réactivé',
  COMPTE_DEVERROUILLE: 'Compte déverrouillé',
  SESSIONS_REVOQUEES: 'Sessions fermées par l\'administrateur'
});

export const formaterDate = (valeur) => valeur
  ? new Date(valeur).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
  : '—';
