// @vitest-environment node
import { describe, it, expect } from 'vitest'
import { TYPES_COLONNE, normaliserTypeColonne, lireOptionsColonne } from '../columnTypes'

describe('normaliserTypeColonne', () => {
  it('ramène les anciens libellés français aux codes de la base', () => {
    expect(normaliserTypeColonne('Texte')).toBe(TYPES_COLONNE.TEXTE)
    expect(normaliserTypeColonne('Nombre')).toBe(TYPES_COLONNE.NOMBRE)
    expect(normaliserTypeColonne('Liste')).toBe(TYPES_COLONNE.LISTE)
  })

  it('conserve un code déjà correct et traite le vide comme du texte', () => {
    expect(normaliserTypeColonne('DATE')).toBe(TYPES_COLONNE.DATE)
    expect(normaliserTypeColonne(undefined)).toBe(TYPES_COLONNE.TEXTE)
  })
})

describe('lireOptionsColonne', () => {
  it('découpe par ligne ou virgule, sans doublon ni vide', () => {
    expect(lireOptionsColonne('Conforme\nÀ reprendre, Rebut\n\nConforme')).toEqual(['Conforme', 'À reprendre', 'Rebut'])
  })
})
