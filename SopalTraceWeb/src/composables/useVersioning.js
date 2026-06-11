import { fabModeleService } from '@/services/fabModeleService';
import { assModeleService } from '@/services/assModeleService';
import { fabPlanService } from '@/services/fabPlanService';

export function useFabModeleVersioning() {
  const creerNouvelleVersionModele = (payload) => fabModeleService.newModeleVersion(payload);
  const restaurerModele = (payload) => fabModeleService.restoreModele(payload);
  const upgradeModele = (id) => fabModeleService.upgradeModele(id);

  return { creerNouvelleVersionModele, restaurerModele, upgradeModele };
}

export function useAssModeleVersioning() {
  const creerNouvelleVersionModele = (payload) => assModeleService.newModeleVersion(payload);
  const restaurerModele = (payload) => assModeleService.restoreModele(payload);
  // Note: upgradeModele n'existe pas dans assModeleService

  return { creerNouvelleVersionModele, restaurerModele };
}

export function useFabPlanVersioning() {
  const creerNouvelleVersionPlan = (payload) => fabPlanService.newPlanVersion(payload);
  
  const mettreAJourValeurs = (id, payload, legendeMoyens = null, remarques = null, finaliser = true) => 
      fabPlanService.mettreAJourValeurs(id, payload, legendeMoyens, remarques, finaliser);
      
  const restaurerPlan = (payload) => fabPlanService.restorePlan(payload);
  const upgradePlan = (id) => fabPlanService.upgradePlan(id);

  return { creerNouvelleVersionPlan, mettreAJourValeurs, restaurerPlan, upgradePlan };
}