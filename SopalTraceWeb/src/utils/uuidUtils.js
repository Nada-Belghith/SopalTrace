/**
 * Génère un identifiant unique (UUID v4 ou fallback robuste)
 * @returns {string} L'identifiant généré
 */
export function genererUid() {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return "id-" + Math.random().toString(36).substring(2, 11) + "-" + Date.now().toString(36);
}
