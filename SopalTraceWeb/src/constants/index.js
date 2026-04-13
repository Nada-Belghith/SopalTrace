/**
 * Énumérations et constantes globales
 */

// ============================================
// ROLES
// ============================================

export const ROLES = {
  ADMIN: 'admin',
  USER: 'user',
  MODERATOR: 'moderator',
  VIEWER: 'viewer'
}

export const ROLE_LABELS = {
  [ROLES.ADMIN]: 'Administrateur',
  [ROLES.USER]: 'Utilisateur',
  [ROLES.MODERATOR]: 'Modérateur',
  [ROLES.VIEWER]: 'Observateur'
}

export const ROLE_PERMISSIONS = {
  [ROLES.ADMIN]: [
    'VIEW_DASHBOARD',
    'CREATE_TRACE',
    'EDIT_TRACE',
    'DELETE_TRACE',
    'MANAGE_USERS',
    'VIEW_ANALYTICS',
    'EXPORT_DATA',
    'VIEW_SETTINGS',
    'EDIT_SETTINGS',
    'VIEW_LOGS'
  ],
  [ROLES.MODERATOR]: [
    'VIEW_DASHBOARD',
    'CREATE_TRACE',
    'EDIT_TRACE',
    'DELETE_TRACE',
    'VIEW_ANALYTICS',
    'EXPORT_DATA'
  ],
  [ROLES.USER]: [
    'VIEW_DASHBOARD',
    'CREATE_TRACE',
    'EDIT_TRACE',
    'VIEW_ANALYTICS'
  ],
  [ROLES.VIEWER]: [
    'VIEW_DASHBOARD',
    'VIEW_ANALYTICS'
  ]
}

// ============================================
// TRACE STATUS
// ============================================

export const TRACE_STATUS = {
  PENDING: 'En attente',
  PROCESSING: 'En cours',
  COMPLETED: 'Complet',
  FAILED: 'Erreur',
  CANCELLED: 'Annulé'
}

export const TRACE_STATUS_SEVERITY = {
  [TRACE_STATUS.PENDING]: 'warning',
  [TRACE_STATUS.PROCESSING]: 'info',
  [TRACE_STATUS.COMPLETED]: 'success',
  [TRACE_STATUS.FAILED]: 'danger',
  [TRACE_STATUS.CANCELLED]: 'secondary'
}

// ============================================
// API CONSTANTS
// ============================================

export const API_ENDPOINTS = {
  TRACES: {
    GET_ALL: '/Traces',
    GET_BY_ID: (id) => `/Traces/${id}`,
    CREATE: '/Traces',
    UPDATE: (id) => `/Traces/${id}`,
    DELETE: (id) => `/Traces/${id}`
  },
  USERS: {
    GET_PROFILE: '/Users/profile',
    UPDATE_PROFILE: '/Users/profile',
    CHANGE_PASSWORD: '/Users/change-password',
    GET_ALL: '/Users',
    GET_BY_ID: (id) => `/Users/${id}`,
    UPDATE: (id) => `/Users/${id}`,
    DELETE: (id) => `/Users/${id}`
  },
  REPORTS: {
    GET_ALL: '/Reports',
    GET_BY_ID: (id) => `/Reports/${id}`,
    GENERATE: '/Reports/generate',
    DOWNLOAD: (id) => `/Reports/${id}/download`
  }
}

// ============================================
// TOKEN CONSTANTS
// ============================================

// Token config removed (frontend auth disabled)

// ============================================
// HTTP STATUS
// ============================================

export const HTTP_STATUS = {
  OK: 200,
  CREATED: 201,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  INTERNAL_SERVER_ERROR: 500,
  SERVICE_UNAVAILABLE: 503
}

// ============================================
// ERROR CODES
// ============================================

export const ERROR_CODES = {
  INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
  EMAIL_ALREADY_EXISTS: 'EMAIL_ALREADY_EXISTS',
  USER_NOT_FOUND: 'USER_NOT_FOUND',
  TOKEN_EXPIRED: 'TOKEN_EXPIRED',
  TOKEN_INVALID: 'TOKEN_INVALID',
  INSUFFICIENT_PERMISSIONS: 'INSUFFICIENT_PERMISSIONS',
  VALIDATION_ERROR: 'VALIDATION_ERROR',
  NETWORK_ERROR: 'NETWORK_ERROR',
  UNKNOWN_ERROR: 'UNKNOWN_ERROR'
}

// ============================================
// MESSAGES
// ============================================

export const MESSAGES = {
  LOGIN_SUCCESS: 'Connexion réussie',
  LOGIN_ERROR: 'Erreur de connexion',
  LOGOUT_SUCCESS: 'Déconnexion réussie',
  REGISTER_SUCCESS: 'Inscription réussie',
  REGISTER_ERROR: 'Erreur lors de l\'inscription',
  TOKEN_REFRESH_SUCCESS: 'Token rafraîchi',
  UNAUTHORIZED: 'Vous n\'êtes pas autorisé',
  FORBIDDEN: 'Accès refusé',
  NOT_FOUND: 'Ressource non trouvée',
  NETWORK_ERROR: 'Erreur réseau. Veuillez vérifier votre connexion.',
  SESSION_EXPIRED: 'Votre session a expiré. Veuillez vous reconnecter.',
  INSUFFICIENT_PERMISSIONS: 'Vous n\'avez pas les permissions requises'
}

// ============================================
// PAGINATION
// ============================================

export const PAGINATION = {
  DEFAULT_PAGE_SIZE: 10,
  MAX_PAGE_SIZE: 100,
  PAGE_SIZES: [10, 20, 50, 100]
}