import { ref } from 'vue'
import alertesService from '@/services/alertesService'
import { useAppToast } from '@/composables/useAppToast'
import { OPERATION_ASSEMBLAGE } from '@/constants/execution'

/**
 * Signalements au superviseur d'un document manquant (plan machine, contrôle au poste, plan d'assemblage...).
 * @param {object} contexte refs { selectedPoste, tousLesPostesActifs, ofActif }
 */
export function useSignalementsAssemblage({ selectedPoste, tousLesPostesActifs, ofActif }) {
  const toast = useAppToast()

  /** Document actuellement signalable : null quand aucun dialogue n'est ouvert. */
  const documentManquant = ref(null)
  const isSignaling = ref(false)

  /**
   * Ouvre le dialogue de signalement.
   * @param {object} doc { titre, message, detail?, description, machineCode? }
   */
  const ouvrirSignalement = (doc) => {
    documentManquant.value = doc
  }

  const fermerSignalement = () => {
    documentManquant.value = null
  }

  const envoyer = async (description, extra = {}) => {
    await alertesService.signalerPlanManquant({
      operationCode: OPERATION_ASSEMBLAGE,
      posteCode: selectedPoste.value || tousLesPostesActifs.value[0],
      numeroOf: ofActif.value?.numeroOf,
      articleCode: ofActif.value?.codeArticle,
      designationArticle: ofActif.value?.designationArticle,
      descriptionProbleme: description,
      ...extra
    })
  }

  /** Envoie le signalement du document ouvert dans le dialogue. */
  const signalerDocumentManquant = async () => {
    const doc = documentManquant.value
    if (!doc) return
    isSignaling.value = true
    try {
      await envoyer(doc.description, doc.machineCode ? { machineCode: doc.machineCode } : {})
      toast.success('Signalé !', 'Une notification a été envoyée au superviseur.')
      fermerSignalement()
    } catch (error) {
      toast.error('Erreur', error.message || 'Impossible d\'envoyer l\'alerte.')
    } finally {
      isSignaling.value = false
    }
  }

  /** Signalement direct d'une catégorie sans document paramétré. Retourne vrai si l'envoi a réussi. */
  const signalerCategorieVide = async (nomDocument) => {
    isSignaling.value = true
    try {
      await envoyer(`Document manquant : ${nomDocument || 'Plan ou document'}.`)
      toast.success('Signalement envoyé', 'Signalement envoyé au Superviseur avec succès !')
      return true
    } catch (error) {
      toast.error('Erreur', error.message || 'Erreur lors de l\'envoi du signalement.')
      return false
    } finally {
      isSignaling.value = false
    }
  }

  // ─── Documents manquants connus ───
  const signalerPlanMachineManquant = (machineCode) => ouvrirSignalement({
    titre: 'Vérification Machine',
    message: `Aucun plan de vérification machine n'est paramétré pour ${machineCode}.`,
    detail: 'Contactez votre superviseur pour qu\'il crée ou active le document associé à cette machine.',
    description: `Document de vérification machine introuvable ou inactif pour la machine ${machineCode}`,
    machineCode
  })

  const signalerControlePosteManquant = () => ouvrirSignalement({
    titre: 'Contrôle au Poste',
    message: `Aucun plan de contrôle au poste n'est paramétré pour ${selectedPoste.value}.`,
    detail: 'Contactez votre superviseur pour qu\'il crée ou active le document associé à ce poste.',
    description: `Document Résultat Contrôle au Poste introuvable ou inactif pour le poste ${selectedPoste.value}`
  })

  const signalerPlanAssManquant = (descriptionDocument, messageErreur) => ouvrirSignalement({
    titre: 'Document Manquant - Assemblage & Résultats',
    message: messageErreur || 'Document requis introuvable.',
    aSignaler: descriptionDocument,
    detail: 'En cliquant ci-dessous, vous notifierez le superviseur précisément du document qui manque pour cet article.',
    description: `Document manquant : ${descriptionDocument || 'Plan d\'Assemblage'} pour le poste ${selectedPoste.value}`
  })

  return {
    documentManquant,
    isSignaling,
    fermerSignalement,
    signalerDocumentManquant,
    signalerCategorieVide,
    signalerPlanMachineManquant,
    signalerControlePosteManquant,
    signalerPlanAssManquant
  }
}
