import { modeleFabricationService as fabModeleService } from '@/services/modeleFabricationService';
import { documentService as assPlanService } from '@/services/documentService';
import { planFabricationService as fabPlanService } from '@/services/planFabricationService';

export function useFabModeleVersioning() {
  const creerNouvelleVersionModele = (payload) => fabModeleService.newModeleVersion(payload);
  const restaurerModele = (payload) => fabModeleService.restoreModele(payload);
  const upgradeModele = (id) => fabModeleService.upgradeModele(id);

  return { creerNouvelleVersionModele, restaurerModele, upgradeModele };
}

export function useAssPlanVersioning() {
  const creerNouvelleVersionPlan = (payload) => assPlanService.newPlanVersion(payload);
  const restaurerPlan = (payload) => assPlanService.restorePlan(payload);

  return { creerNouvelleVersionPlan, restaurerPlan };
}

export function useFabPlanVersioning() {
  const creerNouvelleVersionPlan = (payload) => fabPlanService.newPlanVersion(payload);
  
  const mettreAJourValeurs = (id, payload, legendeMoyens = null, remarques = null, finaliser = true, nom = null, modifiePar = 'Admin', codeArticleSage = null) => 
      fabPlanService.mettreAJourValeurs(id, payload, legendeMoyens, remarques, finaliser, nom, modifiePar, codeArticleSage);
      
  const restaurerPlan = (payload) => fabPlanService.restorePlan(payload);
  const upgradePlan = (id) => fabPlanService.upgradePlan(id);

  return { creerNouvelleVersionPlan, mettreAJourValeurs, restaurerPlan, upgradePlan };
}
