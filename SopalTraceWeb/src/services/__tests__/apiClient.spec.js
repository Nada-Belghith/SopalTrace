import { describe, it, expect, beforeEach } from 'vitest'
import apiClient from '../apiClient'

/** Remplace l'accès réseau : la requête reçoit directement la réponse voulue. */
const repondre = (status, data) => {
  apiClient.defaults.adapter = (config) => {
    const reponse = { status, data, headers: {}, config, statusText: '' }
    if (status >= 200 && status < 300) return Promise.resolve(reponse)
    const erreur = new Error(`HTTP ${status}`)
    erreur.config = config
    erreur.response = reponse
    erreur.isAxiosError = true
    return Promise.reject(erreur)
  }
}

const echec = async () => {
  try {
    await apiClient.get('/test')
  } catch (e) {
    return e
  }
  throw new Error('la requête aurait dû échouer')
}

describe('apiClient : messages d\'erreur montrés à l\'utilisateur', () => {
  beforeEach(() => localStorage.clear())

  it('400 de validation : le détail par règle est affiché', async () => {
    repondre(400, { message: 'Validation échouée', details: ['Le nom est obligatoire.', 'Le code est trop long.'] })
    const e = await echec()
    expect(e.status).toBe(400)
    expect(e.message).toBe('Le nom est obligatoire. Le code est trop long.')
    expect(e.response.data.message).toBe(e.message)
  })

  it('409 : le message métier du serveur est conservé', async () => {
    repondre(409, { message: 'Le lot L-001 est déjà scanné pour cet OF.' })
    expect((await echec()).message).toBe('Le lot L-001 est déjà scanné pour cet OF.')
  })

  it('403 : message de droits insuffisants', async () => {
    repondre(403, {})
    expect((await echec()).message).toContain('droits')
  })

  it('429 : invitation à patienter', async () => {
    repondre(429, '')
    expect((await echec()).message).toContain('Trop de requêtes')
  })

  it("503 : le message du serveur explique l'indisponibilité (conversion PDF arrêtée)", async () => {
    repondre(503, { message: 'Le service de conversion PDF ne répond pas : la génération reprendra automatiquement.' })
    expect((await echec()).message).toContain('conversion PDF')
  })

  it('téléchargement de fichier en erreur : le message JSON contenu dans le Blob est relu', async () => {
    repondre(400, new Blob([JSON.stringify({ message: 'Seul un document actif peut être généré à nouveau.' })], { type: 'application/json' }))
    expect((await echec()).message).toBe('Seul un document actif peut être généré à nouveau.')
  })

  it('500 : message générique avec l\'identifiant de trace, sans détail technique', async () => {
    repondre(500, { traceId: 'abc123', message: 'NullReferenceException at ...' })
    const e = await echec()
    expect(e.message).toContain('abc123')
    expect(e.message).not.toContain('NullReference')
  })
})
