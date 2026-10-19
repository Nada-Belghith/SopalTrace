/**
 * Jeton du lien d'activation : « …/auth/setup-password#token=<jeton> ».
 * Il est placé après « # » pour ne jamais être envoyé au serveur web (journaux, en-tête Referer).
 * @param {string} hash partie « #… » de l'URL
 * @returns {string} le jeton, ou une chaîne vide si le lien est incomplet
 */
export function lireJetonActivation(hash) {
  const parametres = new URLSearchParams((hash || '').replace(/^#/, ''))
  return (parametres.get('token') || '').trim()
}
