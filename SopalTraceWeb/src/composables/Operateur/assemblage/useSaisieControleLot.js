import { ref, computed } from 'vue'
import execAssemblageService from '@/services/execAssemblageService'
import { useAppToast } from '@/composables/useAppToast'
import { logger } from '@/utils/logger'
import { RESULTATS_CONTROLE } from '@/constants/execution'

/**
 * Lignes de saisie d'une section, une par caractéristique : aucun résultat présélectionné (l'opérateur répond
 * à chaque ligne), sauf en ressaisie où l'on repart de la dernière réponse de chaque ligne.
 * @param {object} section section du plan ({ lignesPlan })
 * @param {Array<{ ligneId: string, resultat: string, nc?: string, actionCorrective?: string }>} [precedentes]
 */
export function lignesInitiales(section, precedentes = []) {
  const parLigne = new Map((precedentes || []).map(r => [r.ligneId, r]))
  return (section?.lignesPlan || []).map(l => {
    const precedente = parLigne.get(l.id)
    return {
      ligneId: l.id,
      caracteristique: l.caracteristique,
      resultat: precedente?.resultat ?? null,
      nc: precedente?.nc ?? '',
      actionCorrective: precedente?.actionCorrective ?? ''
    }
  })
}

/** Lignes déjà renseignées (C, ou NC avec son détail). */
export function lignesRenseignees(lignes) {
  return lignes.filter(l => l.resultat === RESULTATS_CONTROLE.CONFORME ||
    (l.resultat === RESULTATS_CONTROLE.NON_CONFORME && !!l.nc?.trim())).length
}

/** Chaque ligne a un résultat, et une non-conformité son détail (même règle que le serveur). */
export function saisieValide(lignes) {
  return lignes.length > 0 && lignesRenseignees(lignes) === lignes.length
}

/**
 * Saisie du contrôle d'une section de lot (première ou dernière pièce). Toutes les caractéristiques de la
 * section sont envoyées ; l'opérateur est identifié côté serveur par son jeton.
 * @param {import('vue').Ref<string>} execControleOfId
 * @param {import('vue').Ref<string>} posteCode
 * @param {import('vue').ComputedRef<string>} moment PREMIERE_PIECE ou DERNIERE_PIECE
 * @param {() => Promise<void>} onSaved
 */
export function useSaisieControleLot(execControleOfId, posteCode, moment, onSaved) {
  const toast = useAppToast()

  const section = ref(null)
  const lignes = ref([])
  const isSaving = ref(false)

  /** @param {object} sec section ; @param {Array} [precedentes] dernières réponses (ressaisie) */
  const ouvrir = (sec, precedentes = []) => {
    section.value = sec
    lignes.value = lignesInitiales(sec, precedentes)
  }

  const fermer = () => {
    section.value = null
  }

  const setResultat = (ligne, resultat) => {
    ligne.resultat = resultat
    if (resultat === RESULTATS_CONTROLE.CONFORME) {
      ligne.nc = ''
      ligne.actionCorrective = ''
    }
  }

  const setTous = (resultat) => lignes.value.forEach(l => setResultat(l, resultat))

  const estValide = computed(() => saisieValide(lignes.value))
  const renseignees = computed(() => lignesRenseignees(lignes.value))

  const enregistrer = async () => {
    if (!estValide.value || isSaving.value || !section.value) return
    isSaving.value = true
    try {
      await execAssemblageService.saveResultat(execControleOfId.value, {
        posteCode: posteCode.value,
        sectionId: section.value.id,
        moment: moment.value,
        lignesControle: lignes.value.map(l => ({
          ligneId: l.ligneId,
          resultat: l.resultat,
          nc: l.resultat === RESULTATS_CONTROLE.NON_CONFORME ? l.nc.trim() : null,
          actionCorrective: l.resultat === RESULTATS_CONTROLE.NON_CONFORME ? (l.actionCorrective?.trim() || null) : null
        }))
      })
      toast.success('Contrôle enregistré')
      fermer()
      if (onSaved) await onSaved()
    } catch (err) {
      logger.error('Erreur saisie contrôle de lot', err)
      toast.error('Erreur', err.message || 'Erreur lors de l\'enregistrement')
    } finally {
      isSaving.value = false
    }
  }

  return { section, lignes, isSaving, estValide, renseignees, ouvrir, fermer, setResultat, setTous, enregistrer }
}
