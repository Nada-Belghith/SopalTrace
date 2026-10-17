import { describe, expect, it } from 'vitest'
import { colonnesDepuisDocument, signatureColonnes } from '../colonnesUtils'

describe('colonnesUtils', () => {
  const relues = colonnesDepuisDocument([
    { cleColonne: 'custom_1', labelAffiche: 'test', typeValeur: 'TEXT' },
    { cleColonne: 'custom_2', labelAffiche: 'Etat', typeValeur: 'LIST', options: ['OK', 'NOK'] }
  ])

  it('relit les colonnes du serveur au format de l’éditeur', () => {
    expect(relues.map(c => c.key)).toEqual(['custom_1', 'custom_2'])
    expect(relues[1]).toMatchObject({ label: 'Etat', options: ['OK', 'NOK'] })
  })

  it('même structure dans un autre ordre : même empreinte', () => {
    expect(signatureColonnes([...relues].reverse())).toBe(signatureColonnes(relues))
  })

  it('colonne ajoutée, retirée ou renommée : empreinte différente', () => {
    const base = signatureColonnes(relues)
    expect(signatureColonnes([...relues, { key: 'custom_3', label: 'Nouvelle', type: 'TEXT' }])).not.toBe(base)
    expect(signatureColonnes(relues.slice(1))).not.toBe(base)
    expect(signatureColonnes([{ ...relues[0], label: 'renommée' }, relues[1]])).not.toBe(base)
  })
})
