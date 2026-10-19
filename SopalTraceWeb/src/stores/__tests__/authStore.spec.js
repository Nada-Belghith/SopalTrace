import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

vi.mock('@/router', () => ({ default: { push: vi.fn() } }))
vi.mock('@/services/authService', () => ({ authService: {} }))

import { useAuthStore } from '../authStore'

const jeton = (expDansSecondes) => {
  const b64 = (o) => btoa(JSON.stringify(o)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `${b64({ alg: 'HS256' })}.${b64({ exp: Math.floor(Date.now() / 1000) + expDansSecondes })}.signature`
}

describe('authStore', () => {
  beforeEach(() => {
    localStorage.clear()
    setActivePinia(createPinia())
  })

  it('accès accordé uniquement aux rôles listés (correspondance exacte)', () => {
    const store = useAuthStore()
    store.user = { role: 'RESPONSABLE_QUALITE' }

    expect(store.hasAccess(['ADMIN', 'RESPONSABLE_QUALITE'])).toBe(true)
    expect(store.hasAccess(['RESPONSABLE_DI'])).toBe(false)
    // L'ancien rôle générique « RESPONSABLE » n'existe plus : il ne donne aucun droit.
    expect(store.hasAccess(['RESPONSABLE'])).toBe(false)
    expect(store.hasAccess([])).toBe(true)
  })

  it('sans utilisateur, aucun accès aux routes protégées', () => {
    expect(useAuthStore().hasAccess(['OPERATEUR'])).toBe(false)
  })

  it('isResponsable : responsable DI ou qualité seulement', () => {
    const store = useAuthStore()
    store.user = { role: 'RESPONSABLE_DI' }
    expect(store.isResponsable).toBe(true)
    store.user = { role: 'SUPERVISEUR_QUALITE' }
    expect(store.isResponsable).toBe(false)
  })

  it('isAuthenticated suit l\'expiration du jeton', () => {
    const store = useAuthStore()
    store.token = jeton(3600)
    expect(store.isAuthenticated).toBe(true)
    store.token = jeton(-10)
    expect(store.isAuthenticated).toBe(false)
    store.token = 'illisible'
    expect(store.isAuthenticated).toBe(false)
  })
})
