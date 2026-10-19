// @vitest-environment node
import { describe, it, expect } from 'vitest'
import { lignesInitiales, lignesRenseignees, saisieValide } from '../useSaisieControleLot'

const section = { id: 's1', lignesPlan: [{ id: 'l1', caracteristique: 'Étanchéité' }, { id: 'l2', caracteristique: 'Couple' }] }

describe('saisie d\'un contrôle de lot', () => {
  it('une ligne par caractéristique, sans résultat présélectionné', () => {
    expect(lignesInitiales(section)).toEqual([
      { ligneId: 'l1', caracteristique: 'Étanchéité', resultat: null, nc: '', actionCorrective: '' },
      { ligneId: 'l2', caracteristique: 'Couple', resultat: null, nc: '', actionCorrective: '' }
    ])
  })

  it('ressaisie : chaque ligne repart de sa dernière réponse', () => {
    const lignes = lignesInitiales(section, [{ ligneId: 'l1', resultat: 'NC', nc: 'Fuite', actionCorrective: 'Joint changé' }])
    expect(lignes[0]).toMatchObject({ resultat: 'NC', nc: 'Fuite', actionCorrective: 'Joint changé' })
    expect(lignes[1].resultat).toBeNull()
  })

  it('valide seulement quand chaque ligne a un résultat et chaque NC son détail', () => {
    const lignes = lignesInitiales(section)
    expect(saisieValide(lignes)).toBe(false)
    lignes[0].resultat = 'C'
    expect(lignesRenseignees(lignes)).toBe(1)
    lignes[1].resultat = 'NC'
    expect(saisieValide(lignes)).toBe(false)
    lignes[1].nc = '  Fuite  '
    expect(saisieValide(lignes)).toBe(true)
  })

  it('une section sans caractéristique ne se valide pas', () => {
    expect(saisieValide(lignesInitiales({ lignesPlan: [] }))).toBe(false)
  })
})
