import { qualityPlansService } from './qualityPlansService';

export const fabPlanService = {
  // Récupérer les dictionnaires (wrapping for backward compatibility)
  getDictionnaires: (...args) => qualityPlansService.getDictionnaires(...args),

  // Modèles (on force isAssExplicit à false et type à 'FAB')
  creerModele: (payload) => qualityPlansService.createModele(payload, false),
  getModeleById: (id) => qualityPlansService.getModeleById(id, 'FAB'),
  nouvelleVersionModele: (payload) => qualityPlansService.newModeleVersion(payload, false),
  updateModeleValeurs: (id, payload) => qualityPlansService.updateModeleValeurs(id, payload, false),
  activerModele: (id) => qualityPlansService.activerModele(id, 'FAB')
};
