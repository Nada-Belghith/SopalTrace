import { describe, it, expect } from 'vitest'
import { libellePeriodicite, periodiciteAvecLibelle, parseFrequenceLibelle } from '../frequencyUtils'

describe('libellés de périodicité (identiques à l\'API)', () => {
  it.each([
    [{ nbrPieces: 5, unite: 'SERIE' }, 'une série de 5 pièces'],
    [{ nbrPieces: 4, unite: 'HEURE', nbrHeures: 1 }, '4 pièces / heure'],
    [{ nbrPieces: 1, unite: 'HEURE', nbrHeures: 4 }, '1 pièce / 4 heures'],
    [{ nbrPieces: 100, unite: 'HEURE', nbrHeures: 1 }, '100% des pièces / heure'],
    [{ nbrPieces: 2, unite: 'ECHANTILLON' }, '2 échantillons']
  ])('%o → %s', (p, attendu) => expect(libellePeriodicite(p)).toBe(attendu))

  it('une périodicité créée pendant l\'édition reçoit son libellé', () => {
    expect(periodiciteAvecLibelle({ id: 'x', nbrPieces: 12, unite: 'SERIE', nbrHeures: null }).libelle).toBe('une série de 12 pièces')
  })

  it('une liste contenant une périodicité sans libellé ne fait plus planter la recherche', () => {
    const liste = [{ id: 'sans-libelle', nbrPieces: 3, unite: 'SERIE' }, { id: 'p', libelle: 'Selon plan' }]
    expect(() => parseFrequenceLibelle('Selon plan', liste)).not.toThrow()
    expect(parseFrequenceLibelle('une série de 3 pièces', liste).modeFreq).toBe('VARIABLE')
  })
})
