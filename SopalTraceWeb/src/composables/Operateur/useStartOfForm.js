import { ref, computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import alertesService from '@/services/alertesService';
import operateurService from '@/services/operateurService';

export function useStartOfForm(dependencies) {
  const { toast, router, activeOfContext } = dependencies;
  const authStore = useAuthStore();

  const showModal = ref(false);
  const selectedOf = ref(null);
  const errorMessage = ref('');

  const form = ref({
    numeroOf: '',
    operationCode: '',
    machineCode: '',
    numEquipe: 1,
    matriculeOperateur: authStore.user?.matricule || 'OP_INCONNU',
    longueur: null,
    diametre: null
  });

  const isSignalingPlan = ref(false);
  const planSignale = ref(false);
  const isLongueurInitialized = ref(false);
  const isDiametreInitialized = ref(false);

  const selectedOpState = computed(() => {
    if (!selectedOf.value || !form.value.operationCode) return null;
    return selectedOf.value.gammeOperatoire?.find(o => o.operationCode === form.value.operationCode);
  });

  const ouvrirModalDemarrage = (of) => {
    selectedOf.value = of;
    errorMessage.value = '';
    planSignale.value = false;
    form.value = {
      numeroOf: of.numeroOf,
      operationCode: '',
      machineCode: '',
      numEquipe: 1,
      matriculeOperateur: authStore.user?.matricule || 'OP_INCONNU',
      longueur: null,
      diametre: null
    };
    showModal.value = true;
  };

  const fermerModal = () => {
    showModal.value = false;
    selectedOf.value = null;
  };

  const reprendreOfExistant = () => {
    const opState = selectedOpState.value;
    if (opState && opState.activeExecControleOfId) {
      const ofNumero = selectedOf.value.numeroOf;
      fermerModal();
      activeOfContext.value = {
        id: opState.activeExecControleOfId,
        numeroOf: ofNumero,
        operationCode: opState.operationCode,
        machineCode: opState.activeMachineCode,
        statut: opState.activeExecStatut,
        estEnReglage: opState.estEnReglage || false,
        a_Des_Controles_Reglage: opState.a_Des_Controles_Reglage || false
      };
      router.push({ query: { execution: opState.activeExecControleOfId } });
    }
  };

  const signalerPlanManquant = async () => {
    isSignalingPlan.value = true;
    try {
      const data = {
        operationCode: form.value.operationCode || (selectedOf.value.gammeOperatoire && selectedOf.value.gammeOperatoire.length > 0 ? selectedOf.value.gammeOperatoire[0].operationCode : 'Inconnu'),
        posteCode: form.value.machineCode || 'Non spécifié',
        articleCode: selectedOf.value.codeArticle,
        descriptionProbleme: errorMessage.value || 'Plan manquant ou introuvable.'
      };
      await alertesService.signalerPlanManquant(data);
      planSignale.value = true;
      toast.add({ severity: 'success', summary: 'Signalé', detail: 'Le responsable a été notifié par email.', life: 4000 });
    } catch (error) {
      const msg = error.response?.data?.message || 'Impossible d\'envoyer le signalement.';
      toast.add({ severity: 'error', summary: 'Erreur', detail: msg, life: 6000 });
    } finally {
      isSignalingPlan.value = false;
    }
  };

  const onOperationChange = async (op) => {
    form.value.operationCode = op.operationCode;
    form.value.machineCode = op.machinePrevueCode || '';
    form.value.longueur = null;
    form.value.diametre = null;
    isLongueurInitialized.value = false;
    isDiametreInitialized.value = false;
    errorMessage.value = '';

    if (selectedOf.value && selectedOf.value.gammeOperatoire) {
      const currentIndex = selectedOf.value.gammeOperatoire.findIndex(o => o.operationCode === op.operationCode);
      if (currentIndex > 0) {
        const prevOp = selectedOf.value.gammeOperatoire[currentIndex - 1];
        if (!prevOp.activeExecStatut) {
          errorMessage.value = `L'opération précédente (${prevOp.operationCode || prevOp.operationLibelle}) n'est pas encore commencée. Vous devez commencer les opérations dans l'ordre de la gamme.`;
          return;
        }
      }
    }
    
    if (selectedOf.value?.codeArticle) {
      try {
        const res = await operateurService.verifierPlan(selectedOf.value.codeArticle, op.operationCode);
        if (!res.data.existe) {
          errorMessage.value = "Aucun plan de contrôle n'est actif pour cet article.";
        } else {
          const isTronnage = op.operationCode === 'TRONC' || op.operationCode === 'TRN';
          if (isTronnage) {
            if (res.data.longueur != null) {
              form.value.longueur = res.data.longueur;
              isLongueurInitialized.value = true;
            }
            if (res.data.diametre != null) {
              form.value.diametre = res.data.diametre;
              isDiametreInitialized.value = true;
            }
          }
        }
      } catch (err) {
        console.error(err);
        errorMessage.value = "Erreur lors de la vérification du plan.";
      }
    }
  };

  const demarrerOf = async () => {
    errorMessage.value = '';
    try {
      const res = await operateurService.demarrerOf({ ...form.value });
      fermerModal();
      activeOfContext.value = {
        id: res.data.id,
        numeroOf: res.data.numeroOf,
        operationCode: res.data.operationCode,
        machineCode: res.data.machineCode,
        statut: res.data.statut,
        estEnReglage: res.data.estEnReglage,
        a_Des_Controles_Reglage: res.data.a_Des_Controles_Reglage
      };
      router.push({ query: { execution: res.data.id } });
    } catch (error) {
      errorMessage.value = error.response?.data?.message || error.message;
    }
  };

  return {
    showModal,
    selectedOf,
    errorMessage,
    form,
    isSignalingPlan,
    planSignale,
    isLongueurInitialized,
    isDiametreInitialized,
    selectedOpState,
    
    ouvrirModalDemarrage,
    fermerModal,
    reprendreOfExistant,
    signalerPlanManquant,
    onOperationChange,
    demarrerOf
  };
}
