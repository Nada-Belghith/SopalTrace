// @vitest-environment node
import { describe, it, expect } from 'vitest'
import { MachineStrategy } from '../MachineStrategy'

const store = (entete = {}, lignesRisques = []) => ({
  entete: { afficheConformite: true, afficheMoyenDetectionRisques: true, afficheFuiteEtalon: false, afficheFamilles: false, ...entete },
  familles: [],
  lignesRisques
})

describe('MachineStrategy (configuration lue en base, jamais déduite du code machine)', () => {
  it('sans configuration : formulaire générique', () => {
    const s = new MachineStrategy(store(), {})
    expect(s.role).toBe('GENERIC')
    expect(s.methodeControleLabel).toBe('Moyen/ Méthode de contrôle')
    expect(s.pieceReferenceLabel).toBe('Numéro de la pièce référence')
    expect(s.showConformiteSection).toBe(true)
    expect(s.hideDp).toBe(false)
  })

  it('modèle BEE : libellés propres, fuite étalon et sous-en-têtes', () => {
    const s = new MachineStrategy(store(), { verifModele: 'BEE', fuiteEtalon: true, sansConformite: true })
    expect(s.methodeControleLabel).toBe('Méthode de contrôle')
    expect(s.pieceReferenceLabel).toBe('Numéro du moyen de contrôle')
    expect(s.showFuiteEtalon).toBe(true)
    expect(s.hasSubHeaders).toBe(true)
    expect(s.showConformiteSection).toBe(false)
    expect(s.isArchitectureA).toBe(false)
  })

  it('fuite en Pa et sans pression : libellé et colonnes masquées', () => {
    const s = new MachineStrategy(store(), { fuiteEnPa: true, sansPression: true })
    expect(s.dpAfficheeLabel).toBe('Fuite affichée (en Pa)')
    expect(s.hidePressionAndDp).toBe(true)
    expect(s.hideDp).toBe(true)
    expect(s.hasSubHeaders).toBe(true)
  })

  it('modèle SER : architecture A et libellé « action »', () => {
    const s = new MachineStrategy(store(), { verifModele: 'SER' })
    expect(s.isArchitectureA).toBe(true)
    expect(s.observationLabel).toBe('Action en cas de non-conformité')
    expect(s.pieceReferenceLabel).toBe('N° moyen de contrôle')
  })

  it('architecture A détectée quand un risque réutilise la même périodicité', () => {
    const lignes = [
      { libelleRisque: 'Fuite', groups: [{ periodiciteId: 'P1' }] },
      { libelleRisque: 'Fuite', groups: [{ periodiciteId: 'P1' }] }
    ]
    expect(new MachineStrategy(store({}, lignes), {}).isArchitectureA).toBe(true)
    expect(new MachineStrategy(store({}, lignes.slice(0, 1)), {}).isArchitectureA).toBe(false)
  })
})
