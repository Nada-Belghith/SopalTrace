import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import TacheAssemblage from '../TacheAssemblage.vue'

const monter = (tache) => mount(TacheAssemblage, { props: { tache: { code: 'VERIF_MACHINE', libelle: 'Vérification machine au démarrage', bloquante: true, ...tache } } })

describe('carte de tâche de l\'assistant d\'assemblage', () => {
  it('tâche à faire : action « Commencer »', async () => {
    const carte = monter({ etat: 'A_FAIRE', detail: 'À vérifier : MAS19.' })
    expect(carte.text()).toContain('À faire')
    expect(carte.text()).toContain('À vérifier : MAS19.')
    await carte.get('button').trigger('click')
    expect(carte.emitted('ouvrir')[0][0].code).toBe('VERIF_MACHINE')
  })

  it('document manquant : se signale', () => {
    const carte = monter({ code: 'PLAN_PREMIERE_PIECE', etat: 'MANQUANT' })
    expect(carte.text()).toContain('Document manquant')
    expect(carte.get('button').text()).toBe('Signaler')
  })

  it('contrôles de production : compteur des occurrences échues', () => {
    const carte = monter({ code: 'CONTROLES_PRODUCTION', etat: 'A_FAIRE', bloquante: false, compteur: 3 })
    expect(carte.text()).toContain('À suivre')
    expect(carte.text()).toContain('3')
    expect(carte.get('button').text()).toBe('Saisir les contrôles')
  })

  it('tâche faite : consultable', () => {
    expect(monter({ etat: 'FAIT' }).get('button').text()).toBe('Consulter')
  })
})
