import { qualityPlansService } from './qualityPlansService';

export const assPlanService = {
  // Récupérer les dictionnaires (pour le moment on utilise ceux de fabrication qui sont génériques)
  getDictionnaires: (...args) => qualityPlansService.getDictionnaires(...args),

  // Modèles (on force isAssExplicit à true et type à 'ASS')
  creerModele: (payload) => qualityPlansService.createModele(payload, true),
  getModeleById: (id) => qualityPlansService.getModeleById(id, 'ASS'),
  nouvelleVersionModele: (payload) => qualityPlansService.newModeleVersion(payload, true),
  updateModeleValeurs: (id, payload) => qualityPlansService.updateModeleValeurs(id, payload, true),
  activerModele: (id) => qualityPlansService.activerModele(id, 'ASS')
};
