import { describe, it, expect } from 'vitest'
import { confirmer, useConfirmation } from '../useConfirmation'

const { etat, valider, annuler } = useConfirmation()

describe('useConfirmation', () => {
  it('affiche la demande puis résout « confirmé » à la validation', async () => {
    const promesse = confirmer({ titre: 'Terminer le poste ?', message: 'Les contrôles de fin deviennent à faire.' })
    expect(etat.visible).toBe(true)
    expect(etat.options.titre).toBe('Terminer le poste ?')
    valider()
    expect(await promesse).toEqual({ confirme: true, valeur: '' })
    expect(etat.visible).toBe(false)
  })

  it('résout « non confirmé » à l\'annulation', async () => {
    const promesse = confirmer({ titre: 'Quitter ?' })
    annuler()
    expect(await promesse).toEqual({ confirme: false, valeur: null })
  })

  it('refuse de valider sans la saisie obligatoire, puis renvoie le texte saisi', async () => {
    const promesse = confirmer({ titre: 'Préparation incomplète', saisie: { libelle: 'Le motif', obligatoire: true } })
    etat.valeur = '   '
    valider()
    expect(etat.visible).toBe(true)
    expect(etat.erreur).toBe('Le motif est obligatoire.')
    etat.valeur = '  Rupture de stock  '
    valider()
    expect(await promesse).toEqual({ confirme: true, valeur: 'Rupture de stock' })
  })

  it('une nouvelle demande annule la précédente et repart d\'une saisie vide', async () => {
    const premiere = confirmer({ titre: 'A', saisie: { libelle: 'Motif' } })
    etat.valeur = 'texte'
    const seconde = confirmer({ titre: 'B' })
    expect(await premiere).toEqual({ confirme: false, valeur: null })
    expect(etat.options.titre).toBe('B')
    expect(etat.valeur).toBe('')
    annuler()
    expect((await seconde).confirme).toBe(false)
  })
})
