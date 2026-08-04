import { computed } from 'vue';
import { useOperateurStore } from '@/stores/execution/operateurStore';
import { useRoute } from 'vue-router';

export function useOfList(props) {
  const operateurStore = useOperateurStore();
  const route = useRoute();

  const ofs = computed(() => operateurStore.ofs);
  const loading = computed(() => operateurStore.loadingOfs);

  const filteredOfs = computed(() => {
    return ofs.value.filter(of => {
      // Un OF est considéré comme "Fini" (Assemblage) si sa gamme contient l'opération 'ASS'
      const isFini = of.gammeOperatoire && of.gammeOperatoire.some(o => o.operationCode === 'ASS');

      // On utilise prioritairement la prop 'typeOf' si elle est passée par le composant/routeur
      let currentType = props?.typeOf;
      if (!currentType) {
        if (route.name === 'operateur-of-fini' || (route.path.includes('of-fini') && !route.path.includes('semi'))) {
          currentType = 'fini';
        } else if (route.name === 'operateur-of-semi-fini' || route.path.includes('of-semi-fini')) {
          currentType = 'semi-fini';
        } else {
          currentType = 'all';
        }
      }

      if (currentType === 'fini') {
        return isFini;
      } else if (currentType === 'semi-fini') {
        return !isFini;
      }
      return true;
    });
  });

  const chargerOfs = async () => {
    try {
      await operateurStore.chargerOfs();
    } catch (error) {
      console.error("Erreur chargement OFs:", error);
      if (error.message === 'Network Error') {
        alert("Impossible de contacter le serveur. Le backend (API) est-il démarré ?");
      }
    }
  };

  /**
   * Statut réel d'un OF :
   * - "EN COURS" si au moins une opération existe dans Exec_ControleOf (activeExecControleOfId != null) et n'est pas CLOTURÉ
   * - "NON COMMENCÉ" si aucune opération n'a encore été démarrée
   */
  const getStatutReel = (of) => {
    if (!of.gammeOperatoire || of.gammeOperatoire.length === 0) return 'NON COMMENCÉ';
    const aUneOperationEnCours = of.gammeOperatoire.some(
      op => op.activeExecControleOfId != null && op.activeExecStatut !== 'CLOTURE'
    );
    return aUneOperationEnCours ? 'EN COURS' : 'NON COMMENCÉ';
  };

  return {
    ofs,
    loading,
    filteredOfs,
    chargerOfs,
    getStatutReel
  };
}
