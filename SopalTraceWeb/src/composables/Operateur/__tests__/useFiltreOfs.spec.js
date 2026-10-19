import { describe, it, expect } from 'vitest'
import { ref } from 'vue'
import { useFiltreOfs, TOUS_LES_STATUTS } from '../useFiltreOfs'

const ofs = ref([
  { numeroOf: 'OF2607ASS01', codeArticle: '2511A01-1', designationArticle: 'Produit Assemblage 2511', statut: 'EN_COURS' },
  { numeroOf: 'OF2607ASS02', codeArticle: '25B2A01-1-1', designationArticle: 'Robinet gaz auto (soupape 26 bar)', statut: null },
  { numeroOf: 'OF2607ASS03', codeArticle: '25B2A02', designationArticle: 'Robinet équerre', statut: 'EN_PAUSE' }
])
const statuts = [
  { valeur: 'EN_COURS', libelle: 'En cours' },
  { valeur: 'EN_PAUSE', libelle: 'En pause' },
  { valeur: 'REGLAGE', libelle: 'En réglage' },
  { valeur: 'NON_COMMENCE', libelle: 'Non commencés' }
]
const creer = () => useFiltreOfs(ofs, of => of.statut || 'NON_COMMENCE', statuts)

describe('useFiltreOfs', () => {
  it('affiche tous les OF sans recherche ni filtre', () => {
    const { ofsAffiches, statut } = creer()
    expect(statut.value).toBe(TOUS_LES_STATUTS)
    expect(ofsAffiches.value).toHaveLength(3)
  })

  it('cherche dans le numéro, le code et la désignation, sans tenir compte des accents ni de la casse', () => {
    const { recherche, ofsAffiches } = creer()
    recherche.value = 'EQUERRE'
    expect(ofsAffiches.value.map(o => o.numeroOf)).toEqual(['OF2607ASS03'])
    recherche.value = 'robinet soupape'
    expect(ofsAffiches.value.map(o => o.numeroOf)).toEqual(['OF2607ASS02'])
    recherche.value = 'ass01'
    expect(ofsAffiches.value.map(o => o.numeroOf)).toEqual(['OF2607ASS01'])
  })

  it('filtre par statut et compte les OF de chaque statut, en masquant les statuts vides', () => {
    const { statut, optionsStatut, ofsAffiches } = creer()
    expect(optionsStatut.value.map(o => [o.valeur, o.nombre])).toEqual([
      [TOUS_LES_STATUTS, 3], ['EN_COURS', 1], ['EN_PAUSE', 1], ['NON_COMMENCE', 1]
    ])
    statut.value = 'NON_COMMENCE'
    expect(ofsAffiches.value.map(o => o.numeroOf)).toEqual(['OF2607ASS02'])
  })

  it('masque par défaut les OF clôturés, que leur filtre montre', () => {
    const avecCloture = ref([...ofs.value, { numeroOf: 'OF2607ASS04', codeArticle: 'X', designationArticle: 'Robinet clôturé', statut: 'CLOTURE' }])
    const { statut, optionsStatut, ofsAffiches } = useFiltreOfs(avecCloture, of => of.statut || 'NON_COMMENCE',
      [...statuts, { valeur: 'CLOTURE', libelle: 'Clôturés' }], { masques: ['CLOTURE'] })

    expect(optionsStatut.value[0]).toMatchObject({ libelle: 'À traiter', nombre: 3 })
    expect(ofsAffiches.value.map(o => o.numeroOf)).not.toContain('OF2607ASS04')
    statut.value = 'CLOTURE'
    expect(ofsAffiches.value.map(o => o.numeroOf)).toEqual(['OF2607ASS04'])
  })

  it('combine recherche et statut, et garde le statut choisi visible même sans résultat', () => {
    const { recherche, statut, optionsStatut, ofsAffiches } = creer()
    statut.value = 'EN_PAUSE'
    recherche.value = 'soupape'
    expect(ofsAffiches.value).toHaveLength(0)
    expect(optionsStatut.value.find(o => o.valeur === 'EN_PAUSE')?.nombre).toBe(0)
  })
})
