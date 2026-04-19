import { qualityPlansService } from './qualityPlansService';

export const fabPlanService = {
  // Récupérer les dictionnaires (wrapping for backward compatibility)
  getDictionnaires: (...args) => qualityPlansService.getDictionnaires(...args),

  // Modèles (compat wrapper)
  creerModele: (...args) => qualityPlansService.createModele(...args),
  getModeleById: (...args) => qualityPlansService.getModeleById(...args),
  nouvelleVersionModele: (...args) => qualityPlansService.newModeleVersion(...args)
};
