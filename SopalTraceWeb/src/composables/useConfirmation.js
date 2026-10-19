import { reactive } from 'vue'

/**
 * Boîte de confirmation commune à toute l'application (affichée par ModaleConfirmation, montée une seule fois
 * dans App.vue). Une seule demande à la fois : une nouvelle demande annule la précédente.
 */
const etat = reactive({
  visible: false,
  options: {},
  valeur: '',
  erreur: ''
})
let resoudre = null

const terminer = (confirme) => {
  const r = resoudre
  resoudre = null
  etat.visible = false
  r?.({ confirme, valeur: confirme ? etat.valeur.trim() : null })
}

/**
 * Demande une confirmation.
 * @param {object} options
 * @param {string} options.titre
 * @param {string} [options.message]
 * @param {string[]} [options.details] éléments listés sous le message (codes, lots…)
 * @param {boolean} [options.danger] action destructive : bouton rouge
 * @param {string} [options.libelleConfirmer]
 * @param {string} [options.libelleAnnuler]
 * @param {string} [options.icone] icône PrimeIcons du bouton de confirmation
 * @param {{ libelle: string, obligatoire?: boolean, longueurMax?: number, placeholder?: string }} [options.saisie]
 *        texte à saisir (motif…)
 * @returns {Promise<{ confirme: boolean, valeur: string|null }>}
 */
export function confirmer(options) {
  if (resoudre) terminer(false)
  etat.options = options
  etat.valeur = ''
  etat.erreur = ''
  etat.visible = true
  return new Promise(resolve => { resoudre = resolve })
}

export function useConfirmation() {
  const valider = () => {
    const saisie = etat.options.saisie
    if (saisie?.obligatoire && !etat.valeur.trim()) {
      etat.erreur = `${saisie.libelle} est obligatoire.`
      return
    }
    terminer(true)
  }
  return { etat, valider, annuler: () => terminer(false), confirmer }
}
