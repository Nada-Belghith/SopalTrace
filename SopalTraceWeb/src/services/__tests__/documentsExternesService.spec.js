import { describe, expect, it } from 'vitest'
import { nomNormalise } from '../documentsExternesService'

const rapport = {
  code: 'CONTROL_REPORT',
  champs: [{ code: 'numeroRapport' }, { code: 'numeroCommandeFournisseur' }]
}

describe('nomNormalise', () => {
  it('forme le nom comme la base : numéros dans l\'ordre des champs, en majuscules, séparés par « - »', () => {
    expect(nomNormalise(rapport, { numeroRapport: ' rc1 ', numeroCommandeFournisseur: 'cf2607001' })).toBe('RC1-CF2607001.pdf')
  })

  it('garde l\'écriture d\'une référence client, espaces et « / » remplacés par « _ »', () => {
    const bl = { code: 'DELIVERY_NOTE', champs: [{ code: 'numeroBl', format: 'CODE' }, { code: 'referenceCommandeClient', format: 'REFERENCE' }] }
    expect(nomNormalise(bl, { numeroBl: 'bl2607001', referenceCommandeClient: ' marché 11/2025 ' })).toBe('BL2607001-marché_11_2025.pdf')
  })

  it('BL : la commande Sage ne fait pas partie du nom, la référence client reprise de la commande le termine', () => {
    const bl = {
      code: 'DELIVERY_NOTE', referenceClientDeLaCommande: true,
      champs: [{ code: 'numeroBl', format: 'CODE' }, { code: 'numeroCommande', format: 'CODE', dansLeNom: false }]
    }
    expect(nomNormalise(bl, { numeroBl: 'bl1', numeroCommande: 'SOPCOV-1' })).toBe('')
    expect(nomNormalise(bl, { numeroBl: 'bl1', numeroCommande: 'SOPCOV-1', referenceCommandeClient: 'OA 044803' })).toBe('BL1-OA_044803.pdf')
  })

  it('reste vide tant qu\'un numéro manque ou sans type', () => {
    expect(nomNormalise(rapport, { numeroRapport: 'RC1' })).toBe('')
    expect(nomNormalise(rapport, { numeroRapport: 'RC1', numeroCommandeFournisseur: '  ' })).toBe('')
    expect(nomNormalise(undefined, {})).toBe('')
  })
})
