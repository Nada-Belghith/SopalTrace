/**
 * Génère un identifiant unique au format UUID v4.
 * crypto.randomUUID n'existe que dans un contexte sécurisé (HTTPS ou localhost) :
 * sur http://<serveur> on construit l'UUID avec crypto.getRandomValues, disponible partout.
 * @returns {string} L'identifiant généré
 */
export function genererUid() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  const octets = new Uint8Array(16);
  if (typeof crypto !== 'undefined' && typeof crypto.getRandomValues === 'function') {
    crypto.getRandomValues(octets);
  } else {
    for (let i = 0; i < 16; i++) octets[i] = Math.floor(Math.random() * 256);
  }
  octets[6] = (octets[6] & 0x0f) | 0x40; // version 4
  octets[8] = (octets[8] & 0x3f) | 0x80; // variante RFC 4122
  const hex = Array.from(octets, (o) => o.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
