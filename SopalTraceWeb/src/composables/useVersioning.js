import { qualityPlansService } from '@/services/qualityPlansService';

export function useModeleVersioning() {
  const creerNouvelleVersionModele = (payload) => qualityPlansService.newModeleVersion(payload);
  const restaurerModele = (payload) => qualityPlansService.restoreModele(payload);

  return { creerNouvelleVersionModele, restaurerModele };
}

export function usePlanVersioning() {
  const creerNouvelleVersionPlan = (payload) => qualityPlansService.newPlanVersion(payload);
  
  const mettreAJourValeurs = (id, payload, legendeMoyens = null, remarques = null, finaliser = true) => 
      qualityPlansService.mettreAJourValeurs(id, payload, legendeMoyens, remarques, finaliser);
      
  const restaurerPlan = (payload) => qualityPlansService.restorePlan(payload);

  return { creerNouvelleVersionPlan, mettreAJourValeurs, restaurerPlan };
}