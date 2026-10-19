import { describe, it, expect } from 'vitest'
import { titreSection } from '../sectionTitleUtils'

const types = [
  { id: 'reglage', libelle: 'Contrôle aux réglages' },
  { id: 'poste', libelle: 'Contrôle par poste (1ère et dernière pièce)' },
  { id: 'lot-of', libelle: 'Contrôle lot OF (1ère et dernière pièce)' },
  { id: 'iso', libelle: 'Contrôle par échantillonnage (ISO 2859-1, NQA 0,65)' }
]
const periodicites = [{ id: 'serie-12', nbrPieces: 12, unite: 'SERIE', nbrHeures: null }]

describe('titre de section : libellé du type + périodicité (même règle que le serveur)', () => {
  it('le libellé du type est repris tel quel, parenthèses comprises', () => {
    expect(titreSection({ typeSectionId: 'poste', libelleSection: 'Caractéristiques à contrôler' }, types, periodicites))
      .toBe('Caractéristiques à contrôler Contrôle par poste (1ère et dernière pièce)')
  })

  it('un libellé enregistré incomplet est remplacé par celui du type', () => {
    expect(titreSection({ typeSectionId: 'lot-of', libelleSection: 'Caractéristiques à contrôler' }, types, periodicites))
      .toBe('Caractéristiques à contrôler Contrôle lot OF (1ère et dernière pièce)')
  })

  it('la périodicité choisie est ajoutée entre parenthèses', () => {
    expect(titreSection({ typeSectionId: 'reglage', periodiciteId: 'SERIE-12' }, types, periodicites))
      .toBe('Caractéristiques à contrôler Contrôle aux réglages (une série de 12 pièces)')
  })

  it('une périodicité saisie dans l\'éditeur est ajoutée de la même façon', () => {
    expect(titreSection({ typeSectionId: 'iso', modeFreq: 'VARIABLE', freqNum: 4, typeVariable: 'HEURE', freqHours: 1 }, types))
      .toBe('Caractéristiques à contrôler Contrôle par échantillonnage (ISO 2859-1, NQA 0,65) (4 pièces / heure)')
  })

  it('section sans type : titre libre', () => {
    expect(titreSection({ nom: 'Emballage' }, types)).toBe('Emballage')
    expect(titreSection({}, types)).toBe('Caractéristiques à contrôler')
  })

  it('section rechargée marquée « SANS » mais avec une périodicité : son identifiant fait foi', () => {
    expect(titreSection({ typeSectionId: 'reglage', periodiciteId: 'serie-12', modeFreq: 'SANS' }, types, periodicites))
      .toBe('Caractéristiques à contrôler Contrôle aux réglages (une série de 12 pièces)')
  })

  it('périodicité absente du référentiel chargé : reprise du libellé enregistré', () => {
    const section = {
      typeSectionId: 'reglage',
      periodiciteId: 'd1511476',
      modeFreq: 'SANS',
      libelleSection: 'Caractéristiques à contrôler Contrôle aux réglages (une série de 13 pièces)'
    }
    expect(titreSection(section, types, [])).toBe('Caractéristiques à contrôler Contrôle aux réglages (une série de 13 pièces)')
  })
})
