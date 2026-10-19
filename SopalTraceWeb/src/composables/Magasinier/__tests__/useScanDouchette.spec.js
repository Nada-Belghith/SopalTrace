import { describe, it, expect, vi, afterEach } from 'vitest'
import { defineComponent, h, nextTick } from 'vue'
import { mount, flushPromises } from '@vue/test-utils'
import { useScanDouchette } from '../useScanDouchette'

/** Composant minimal : un champ de scan branché sur le composable. */
const monter = (options) => {
  let api
  const Champ = defineComponent({
    setup() {
      api = useScanDouchette(options)
      return () => h('input', {
        ref: api.champ,
        value: api.valeur.value,
        onInput: (e) => { api.valeur.value = e.target.value },
        onKeydown: api.surTouche
      })
    }
  })
  const wrapper = mount(Champ, { attachTo: document.body })
  return { wrapper, api: () => api }
}

/** Simule une douchette : chaque caractère comme une touche, puis Entrée. */
const scanner = async (wrapper, texte) => {
  const input = wrapper.find('input')
  for (const c of texte) {
    await input.trigger('keydown', { key: c })
    input.element.value += c
    await input.trigger('input')
  }
  await input.trigger('keydown', { key: 'Enter' })
  await flushPromises()
}

afterEach(() => { document.body.innerHTML = '' })

describe('useScanDouchette', () => {
  it('Entrée transmet le contenu nettoyé puis vide le champ', async () => {
    const onScan = vi.fn().mockResolvedValue()
    const { wrapper, api } = monter({ onScan })

    await scanner(wrapper, '  OF2607ASS01\t')

    expect(onScan).toHaveBeenCalledWith('OF2607ASS01')
    expect(api().valeur.value).toBe('')
  })

  it('le même code relu tout de suite est ignoré (anti double scan)', async () => {
    const onScan = vi.fn().mockResolvedValue()
    const { wrapper } = monter({ onScan, delaiDoublonMs: 5000 })

    await scanner(wrapper, 'L-001')
    await scanner(wrapper, 'L-001')
    await scanner(wrapper, 'L-002')

    expect(onScan.mock.calls.map(c => c[0])).toEqual(['L-001', 'L-002'])
  })

  it('les scans arrivés pendant un traitement sont traités ensuite, dans l\'ordre', async () => {
    const ordre = []
    let liberer
    const onScan = vi.fn().mockImplementation((c) => {
      ordre.push(`début ${c}`)
      return c === 'A' ? new Promise(r => { liberer = () => { ordre.push('fin A'); r() } }) : Promise.resolve()
    })
    const { api } = monter({ onScan })

    api().soumettre('A')
    api().soumettre('B')
    await nextTick()
    expect(ordre).toEqual(['début A'])

    liberer()
    await flushPromises()
    expect(ordre).toEqual(['début A', 'fin A', 'début B'])
  })

  it('un refus du serveur ne bloque pas les scans suivants', async () => {
    const onScan = vi.fn().mockRejectedValueOnce(new Error('Article hors nomenclature')).mockResolvedValue()
    const { api } = monter({ onScan })

    api().soumettre('VIS-9;L1')
    api().soumettre('CORPS-1;L1')
    await flushPromises()

    expect(onScan).toHaveBeenCalledTimes(2)
    expect(api().enCours.value).toBe(false)
  })

  it('une frappe hors du champ est redirigée vers le champ actif', async () => {
    const onScan = vi.fn().mockResolvedValue()
    const { api } = monter({ onScan })
    document.body.focus()

    for (const c of 'BL01') window.dispatchEvent(new KeyboardEvent('keydown', { key: c }))
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }))
    await flushPromises()

    expect(onScan).toHaveBeenCalledWith('BL01')
    expect(api().valeur.value).toBe('')
  })

  it('un champ inactif ne reçoit pas les frappes redirigées', async () => {
    const onScan = vi.fn().mockResolvedValue()
    monter({ onScan, estActif: () => false })

    for (const c of 'X1') window.dispatchEvent(new KeyboardEvent('keydown', { key: c }))
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }))
    await flushPromises()

    expect(onScan).not.toHaveBeenCalled()
  })
})
