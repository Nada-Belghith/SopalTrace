import { describe, it, expect } from 'vitest'
import { mapImportedSection, mapSectionForBackend, hydrateSectionFromBackend } from '../sectionUtils'

describe('sections : type de section et périodicité facultative', () => {
  it('une référence de norme importée ne devient pas une périodicité', () => {
    const s = mapImportedSection({ typeSectionId: 'ts-2859', modeFreq: 'SANS', frequenceLibelle: 'selon ISO 2859-1 : Tableau 2-A, avec un NQA de 0,65', lignes: [] })
    expect(s.modeFreq).toBe('SANS')
    expect(s.periodiciteId).toBeNull()
    expect(s).not.toHaveProperty('regleEchantillonnageId')
  })

  it('une fréquence importée devient une périodicité', () => {
    const s = mapImportedSection({ modeFreq: 'VARIABLE', periodiciteId: 'p-12', frequenceLibelle: 'une série de 12 pièces', lignes: [] })
    expect(s).toMatchObject({ modeFreq: 'VARIABLE', periodiciteId: 'p-12', typeVariable: 'SERIE', freqNum: 12 })
  })

  it('le payload envoyé ne contient plus de règle d\'échantillonnage', () => {
    const payload = mapSectionForBackend({ typeSectionId: 'ts', libelleSection: 'Contrôle aux réglages', lignes: [] }, 0, [])
    expect(payload).not.toHaveProperty('regleEchantillonnageId')
    expect(payload).toMatchObject({ typeSectionId: 'ts', periodiciteId: null })
  })

  it('une section relue sans périodicité reste sans périodicité', () => {
    const s = hydrateSectionFromBackend({ typeSectionId: 'ts', libelleSection: 'Contrôle lot OF (1ère et dernière pièce)', lignes: [] }, [])
    expect(s.modeFreq).toBe('SANS')
  })
})
