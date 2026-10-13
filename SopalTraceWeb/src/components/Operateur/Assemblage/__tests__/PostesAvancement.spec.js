import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PostesAvancement from '../PostesAvancement.vue'

const monter = (postes, posteSelectionne = 'PAS71') => mount(PostesAvancement, { props: { postes, posteSelectionne } })
const texteDe = (wrapper, poste) => wrapper.findAll('button').find(b => b.text().startsWith(poste)).text()

describe('PostesAvancement', () => {
  it('affiche l\'état de chaque poste vu par l\'équipe', () => {
    const wrapper = monter([
      { posteCode: 'PAS71', demarre: true, phase: 'FIN_POSTE', equipeTitulaire: 'E1', tachesRestantes: 3 },
      { posteCode: 'PAS72', demarre: false, phase: 'POSTE_OCCUPE', equipeTitulaire: 'E2', tachesRestantes: 0 },
      { posteCode: 'PAS78', demarre: true, phase: 'POSTE_TERMINE', equipeTitulaire: null, tachesRestantes: 0 },
      { posteCode: 'PAS80', demarre: false, phase: 'NON_DEMARRE', equipeTitulaire: null, tachesRestantes: 1 }
    ])

    expect(texteDe(wrapper, 'PAS71')).toContain('3 à faire')
    expect(texteDe(wrapper, 'PAS72')).toContain('tenu par E2')
    expect(texteDe(wrapper, 'PAS78')).toContain('terminé')
    expect(texteDe(wrapper, 'PAS80')).toContain('non démarré')
  })

  it('émet le poste choisi', async () => {
    const wrapper = monter([
      { posteCode: 'PAS71', demarre: true, phase: 'PRODUCTION', tachesRestantes: 0 },
      { posteCode: 'PAS72', demarre: true, phase: 'PRODUCTION', tachesRestantes: 0 }
    ])

    await wrapper.findAll('button')[1].trigger('click')

    expect(wrapper.emitted('selectionner')).toEqual([['PAS72']])
    expect(wrapper.findAll('button')[0].attributes('aria-selected')).toBe('true')
  })
})
