import { ref, computed, watch } from 'vue';
import { useOperateurStore } from '@/stores/execution/operateurStore';
import { useAuthStore } from '@/stores/authStore';
import operateurService from '@/services/operateurService';

export function useOfControls(dependencies) {
  const { toast, router, route, chargerOfs } = dependencies;
  const operateurStore = useOperateurStore();
  const authStore = useAuthStore();

  const activeOfContext = computed({
    get: () => operateurStore.activeOfContext,
    set: (val) => operateurStore.setActiveOfContext(val)
  });

  // Gestion des modales et états d'action
  const showClotureModal = ref(false);
  const actionEnCours = ref(false);
  const showLeaveModal = ref(false);
  const nextRouteOrQuery = ref(null);
  
  const raisonModalConfig = ref({
    show: false,
    texte: '',
    titre: '',
    description: '',
    action: null,
    payload: null
  });

  const demanderRaisonPause = (titre, description, actionCallback, payload = null) => {
    raisonModalConfig.value = {
      show: true,
      texte: '',
      titre,
      description,
      action: actionCallback,
      payload
    };
  };

  const annulerRaison = () => {
    raisonModalConfig.value.show = false;
    actionEnCours.value = false;
  };

  const confirmerRaison = () => {
    const { action, texte, payload } = raisonModalConfig.value;
    raisonModalConfig.value.show = false;
    if (action) {
      action(texte, payload);
    }
  };

  const mettreEnReglage = async () => {
    try {
      await operateurService.mettreEnReglage(activeOfContext.value.id);
      toast.add({ severity: 'success', summary: 'Mode Réglage', detail: 'Production mise en pause. Procédez aux contrôles de réglage.', life: 4000 });
      activeOfContext.value.estEnReglage = true;
      if (chargerOfs) chargerOfs();
    } catch (error) {
      console.error(error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de mettre en réglage.', life: 3000 });
    }
  };

  const mettreEnPause = () => {
    demanderRaisonPause(
      'Mettre en Pause',
      'Voulez-vous vraiment mettre la production en pause ? (Motif Optionnel) :',
      async (raison) => {
        try {
          await operateurService.mettreEnPause(activeOfContext.value.id, raison);
          toast.add({ severity: 'info', summary: 'Production en Pause', detail: 'L\'OF est maintenant en pause.', life: 4000 });
          activeOfContext.value.statut = 'EN_PAUSE';
          if (chargerOfs) chargerOfs();
        } catch (error) {
          console.error(error);
          toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de mettre en pause.', life: 3000 });
        }
      }
    );
  };

  const reprendre = async () => {
    try {
      await operateurService.reprendreDepuisPause(activeOfContext.value.id);
      toast.add({ severity: 'success', summary: 'Reprise', detail: 'La production a repris.', life: 4000 });
      activeOfContext.value.statut = 'EN_COURS';
      activeOfContext.value.estEnReglage = false;
      if (chargerOfs) chargerOfs();
    } catch (error) {
      console.error(error);
      toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Impossible de reprendre la production.', life: 3000 });
    }
  };

  const cloturer = () => {
    showClotureModal.value = true;
  };

  const quitterExecution = () => {
    if (activeOfContext.value && activeOfContext.value.statut !== 'EN_COURS') {
      activeOfContext.value = {};
    }
    router.push({ path: route.path, query: {} });
  };

  const confirmCloture = async () => {
    showClotureModal.value = false;
    try {
      await operateurService.cloturerOf(activeOfContext.value.id);
      activeOfContext.value = {}; // <-- Empêche l'intercepteur de route de bloquer la navigation
      quitterExecution();
      if (chargerOfs) chargerOfs();
    } catch (error) {
      console.error(error);
    }
  };

  // --- GESTION DE LA NAVIGATION ET PROTECTION (PAUSE/CLÔTURE) ---
  
  // Suspendre le polling global si une modale est ouverte
  watch(() => raisonModalConfig.value.show, (newVal) => operateurStore.isPollingPaused = newVal);
  watch(showClotureModal, (newVal) => operateurStore.isPollingPaused = newVal);
  watch(showLeaveModal, (newVal) => operateurStore.isPollingPaused = newVal);

  const handleNavigation = (to) => {
    if (!authStore.token) return true; // Contourner si déconnexion en cours
    if (activeOfContext.value && activeOfContext.value.id && activeOfContext.value.statut === 'EN_COURS') {
      if (to.path !== route.path || !to.query.execution) {
        nextRouteOrQuery.value = to;
        showLeaveModal.value = true;
        return false;
      }
    }
    return true;
  };

  const executeLeaveAction = async (actionType, raison = null) => {
    try {
      if (actionType === 'pause') {
        await operateurService.mettreEnPause(activeOfContext.value.id, raison);
        toast.add({ severity: 'success', summary: 'Succès', detail: 'OF mis en pause.', life: 3000 });
      } else if (actionType === 'cloture') {
        await operateurService.cloturerOf(activeOfContext.value.id);
        toast.add({ severity: 'success', summary: 'Succès', detail: 'OF clôturé.', life: 3000 });
      }
      
      showLeaveModal.value = false;
      activeOfContext.value = {};
      
      if (nextRouteOrQuery.value) {
        router.push(nextRouteOrQuery.value);
      }
    } catch (error) {
      toast.add({ severity: 'error', summary: 'Erreur', detail: 'Action impossible.', life: 3000 });
    } finally {
      actionEnCours.value = false;
    }
  };

  const confirmLeave = (action) => {
    if (actionEnCours.value) return;
    actionEnCours.value = true;
    
    if (action === 'pause') {
      showLeaveModal.value = false;
      demanderRaisonPause(
        'Mettre en Pause avant de quitter',
        'Voulez-vous vraiment mettre la production en pause avant de quitter ? Veuillez saisir le motif (obligatoire) :',
        async (raison) => {
          await executeLeaveAction('pause', raison);
        }
      );
    } else {
      executeLeaveAction(action);
    }
  };

  const cancelLeave = () => {
    showLeaveModal.value = false;
    nextRouteOrQuery.value = null;
  };

  const handleUnload = (e) => {
    if (activeOfContext.value && activeOfContext.value.id && activeOfContext.value.statut === 'EN_COURS') {
      const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5246';
      const url = `${apiUrl}/api/Operateur/of/${activeOfContext.value.id}/pause`;
      
      fetch(url, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${authStore.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ raison: "Fermeture inattendue de l'application" }),
        keepalive: true
      }).catch(() => {});
    }
  };

  return {
    activeOfContext,
    
    // Actions standards
    mettreEnReglage,
    mettreEnPause,
    reprendre,
    cloturer,
    quitterExecution,
    
    // Etats et handlers pour Cloture
    showClotureModal,
    confirmCloture,
    
    // Etats et handlers pour Raison (Pause)
    raisonModalConfig,
    demanderRaisonPause,
    annulerRaison,
    confirmerRaison,
    
    // Etats et handlers pour Quitter (Navigation Guard)
    showLeaveModal,
    actionEnCours,
    handleNavigation,
    confirmLeave,
    cancelLeave,
    handleUnload
  };
}
