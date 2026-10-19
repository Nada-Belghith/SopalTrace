import { describe, it, expect } from 'vitest'
import { idsDuMoment, periodiciteAffichee } from '../periodicitesMachine'

const periodicites = [
  { id: 'dem', libelle: 'Au démarrage', controlMoment: 'DEMARRAGE' },
  { id: 'dem-machine', libelle: 'Démarrage machine après réglage', controlMoment: 'DEMARRAGE' },
  { id: 'pau', libelle: 'Après la pause', controlMoment: 'APRES_PAUSE' },
  { id: 'fin', libelle: 'A la fin du poste', controlMoment: 'FIN_POSTE' },
  { id: 'hebdo', libelle: 'Fin de semaine', controlMoment: null }
]

describe('périodicités machine d\'un moment (code ControlMoment, jamais le libellé)', () => {
  it('toutes les périodicités du moment, quel que soit leur libellé', () => {
    expect(idsDuMoment(periodicites, 'DEMARRAGE')).toEqual(['dem', 'dem-machine'])
    // « Fin de semaine » contient « fin » mais n'est pas un moment de fin de poste.
    expect(idsDuMoment(periodicites, 'FIN_POSTE')).toEqual(['fin'])
    expect(idsDuMoment(periodicites, 'APRES_PAUSE')).toEqual(['pau'])
  })

  it('moment imposé : seuls ses groupes s\'affichent, aucun si la liste est vide', () => {
    expect(periodiciteAffichee('dem', { ids: ['dem', 'dem-machine'] })).toBe(true)
    expect(periodiciteAffichee('fin', { ids: ['dem'] })).toBe(false)
    expect(periodiciteAffichee('dem', { ids: [] })).toBe(false)
  })

  it('filtre manuel (fabrication) : une périodicité ou toutes', () => {
    expect(periodiciteAffichee('dem', { id: 'dem' })).toBe(true)
    expect(periodiciteAffichee('fin', { id: 'dem' })).toBe(false)
    expect(periodiciteAffichee('fin', {})).toBe(true)
  })
})
