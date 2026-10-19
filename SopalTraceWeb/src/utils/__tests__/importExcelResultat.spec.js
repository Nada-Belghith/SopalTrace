import { describe, it, expect } from 'vitest'
import { verifierImportExcel, messageImportExcel } from '../importExcelResultat'
import { mapImportedSection } from '../sectionUtils'

describe('compte rendu de l\'import Excel des plans', () => {
  it('fichier non reconnu : l\'erreur du serveur est levée', () => {
    expect(() => verifierImportExcel({ sections: [], erreurs: ['Aucune ligne d\'en-tête reconnue.'] }))
      .toThrow('Aucune ligne d\'en-tête reconnue.')
  })

  it('import complet sans remarque : succès avec le nombre de caractéristiques', () => {
    const resultat = { sections: [{ lignes: [{}, {}] }, { lignes: [{}] }], lignesLues: 3, erreurs: [], warnings: [] }
    expect(verifierImportExcel(resultat)).toBe(resultat)
    expect(messageImportExcel(resultat)).toMatchObject({
      severity: 'success',
      detail: '3 caractéristique(s) importée(s) dans 2 section(s).'
    })
  })

  it('points à vérifier : avertissement qui les liste, tronqué au-delà du maximum', () => {
    const warnings = ['Ligne 14 : texte placé dans les observations.', 'Instrument BEE46 créé à l\'enregistrement.', 'a', 'b', 'c']
    const message = messageImportExcel({ sections: [{ lignes: [{}] }], lignesLues: 1, warnings }, 2)
    expect(message.severity).toBe('warn')
    expect(message.detail).toContain('Ligne 14')
    expect(message.detail).toContain('… et 3 autre(s)')
  })

  it('le moyen hors catalogue importé est conservé en texte libre', () => {
    const section = mapImportedSection({
      nom: 'Réglages',
      lignes: [{ libelleAffiche: 'Passage du gaz', moyenControleId: null, moyenTexteLibre: 'Machine de marquage', instrumentCode: 'SER05' }]
    })
    expect(section.lignes[0]).toMatchObject({ moyenControleId: null, moyenTexteLibre: 'Machine de marquage', instrumentCode: 'SER05' })
  })
})
