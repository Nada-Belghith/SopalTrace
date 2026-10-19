import { describe, it, expect } from 'vitest'
import { lireJetonActivation } from '../jetonActivation'

describe('lien d\'activation', () => {
  it('lit le jeton placé après « # »', () => {
    expect(lireJetonActivation('#token=abc-DEF_123')).toBe('abc-DEF_123')
  })

  it('décode un jeton encodé dans l\'URL', () => {
    expect(lireJetonActivation('#token=a%2Bb')).toBe('a+b')
  })

  it('lien incomplet : chaîne vide', () => {
    expect(lireJetonActivation('')).toBe('')
    expect(lireJetonActivation('#autre=1')).toBe('')
    expect(lireJetonActivation(undefined)).toBe('')
  })
})
