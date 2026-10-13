import { computed, ref } from 'vue'

export const TOUS_LES_STATUTS = ''

const normaliser = (texte) => String(texte ?? '')
  .normalize('NFD').replace(/[̀-ͯ]/g, '')
  .toLowerCase()

/**
 * Recherche et filtre par statut d'une liste d'OF, côté écran.
 * @param {import('vue').Ref<Array>} ofs liste source
 * @param {(of: object) => string} statutDe statut d'affichage d'un OF
 * @param {Array<{ valeur: string, libelle: string }>} statuts statuts proposés, dans l'ordre d'affichage
 * @param {{ masques?: string[] }} [options] statuts masqués par défaut (ex. OF clôturés : « À traiter » les exclut,
 *        leur propre filtre les montre)
 */
export function useFiltreOfs(ofs, statutDe, statuts, { masques = [] } = {}) {
  const recherche = ref('')
  const statut = ref(TOUS_LES_STATUTS)

  const correspondRecherche = (of) => {
    const termes = normaliser(recherche.value).split(/\s+/).filter(Boolean)
    if (!termes.length) return true
    const texte = normaliser(`${of.numeroOf} ${of.codeArticle} ${of.designationArticle}`)
    return termes.every(t => texte.includes(t))
  }

  const trouves = computed(() => (ofs.value ?? []).filter(correspondRecherche))
  const aTraiter = computed(() => trouves.value.filter(of => !masques.includes(statutDe(of))))

  const optionsStatut = computed(() => [
    { valeur: TOUS_LES_STATUTS, libelle: masques.length ? 'À traiter' : 'Tous', nombre: aTraiter.value.length },
    ...statuts
      .map(s => ({ ...s, nombre: trouves.value.filter(of => statutDe(of) === s.valeur).length }))
      .filter(s => s.nombre > 0 || s.valeur === statut.value)
  ])

  const ofsAffiches = computed(() => statut.value === TOUS_LES_STATUTS
    ? aTraiter.value
    : trouves.value.filter(of => statutDe(of) === statut.value))

  return { recherche, statut, optionsStatut, ofsAffiches }
}
