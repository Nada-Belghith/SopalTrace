import { defineStore } from 'pinia';
import operateurService from '@/services/operateurService';

export const useOperateurStore = defineStore('operateur', {
  state: () => ({
    ofs: [],
    activeOfContext: {},
    knownAlerts: new Set(),
    pollingInterval: null,
    isPollingPaused: false, // Bloque le rafraîchissement si l'utilisateur est en train de répondre
    isFirstLoad: true, // Prevents showing toasts for already existing alerts on login
    alertesParOf: {},  // Stocke les alertes par execControleOfId
    loadingOfs: false
  }),
  actions: {
    async chargerOfs() {
      if (this.isPollingPaused) return;
      this.loadingOfs = true;
      try {
        const res = await operateurService.getAllOfOperations();
        this.ofs = res.data;
      } catch (error) {
        console.error("Erreur chargement OFs:", error);
      } finally {
        this.loadingOfs = false;
      }
    },

    startGlobalPolling(toast, router) {
      if (this.pollingInterval) return; // Déjà démarré

      // Lancement initial
      this.checkAllAlerts(toast, router);

      // Polling toutes les 1 minute (60000 ms)
      this.pollingInterval = setInterval(() => {
        this.checkAllAlerts(toast, router);
      }, 60000);
    },

    stopGlobalPolling() {
      if (this.pollingInterval) {
        clearInterval(this.pollingInterval);
        this.pollingInterval = null;
      }
      this.isFirstLoad = true;
      this.knownAlerts.clear();
      this.alertesParOf = {};
    },

    async checkAllAlerts(toast, router) {
      if (this.isPollingPaused) return;

      // S'assurer qu'on a les OFs à jour
      await this.chargerOfs();

      // Trouver tous les OFs "EN_COURS"
      const activeExecIds = [];
      for (const of of this.ofs) {
        if (of.gammeOperatoire) {
          for (const op of of.gammeOperatoire) {
            if (op.activeExecStatut === 'EN_COURS' && op.activeExecControleOfId && op.operationCode !== 'ASS') {
              activeExecIds.push({
                execId: op.activeExecControleOfId,
                numeroOf: of.numeroOf,
                operationCode: op.operationCode
              });
            }
          }
        }
      }

      // Parcourir chaque OF en cours pour vérifier ses alertes
      for (const ctx of activeExecIds) {
        try {
          const res = await operateurService.getAlertesActives(ctx.execId);
          const tranches = res.data;

          this.alertesParOf[ctx.execId] = tranches;

          // Extraire toutes les occurrences non répondues (en attente ou retard)
          const occurrences = tranches.flatMap(t => t.occurrences).filter(o => !o.estRepondu);

          for (const occ of occurrences) {
            if (!this.knownAlerts.has(occ.id)) {
              this.knownAlerts.add(occ.id);

              if (!this.isFirstLoad) {
                // Afficher un toast
                toast.add({
                  severity: occ.estEnRetard ? 'error' : 'warn',
                  summary: `Nouveau contrôle : OF ${ctx.numeroOf}`,
                  detail: `Occurrence #${occ.numeroOccurrence} - ${ctx.operationCode}`,
                  life: 15000,
                  group: 'operator-alert',
                  data: { execId: ctx.execId }
                });
              }
            }
          }
        } catch (error) {
          console.error(`Erreur vérification alertes OF ${ctx.execId}:`, error);
        }
      }

      this.isFirstLoad = false;
    },

    // Définir le contexte actif (quand l'utilisateur ouvre un OF spécifique)
    setActiveOfContext(context) {
      this.activeOfContext = context;
    },

    clearActiveOfContext() {
      this.activeOfContext = {};
    }
  }
});
