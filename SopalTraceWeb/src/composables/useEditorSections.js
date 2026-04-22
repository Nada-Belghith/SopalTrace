import { ref } from 'vue';

export function useEditorSections() {
  const sections = ref([]);

  const ajouterSection = () => {
    sections.value.push({
      id: crypto.randomUUID(),
      isFromDb: false,
      typeSectionId: '',
      libelleSection: '',
      notes: '',
      ordreAffiche: sections.value.length,
      lignes: []
    });
  };

  const supprimerSection = (id) => {
    sections.value = sections.value.filter(s => s.id !== id);
  };

  const mettreAJourSection = (index, updatedSection) => {
    sections.value[index] = updatedSection;
  };

  const ajouterLigneASection = (sectionIndex) => {
    const s = sections.value[sectionIndex];
    if (s) {
      s.lignes.push({
        id: crypto.randomUUID(),
        isFromDb: false,
        typeCaracteristiqueId: null,
        libelleAffiche: '',
        typeControleId: null,
        moyenControleId: null,
        instrumentCode: null,
        moyenTexteLibre: '',
        toleranceSuperieure: null,
        toleranceInferieure: null,
        limiteSpecTexte: '',
        defauthequeId: null,
        instruction: '',
        observations: ''
      });
    }
  };

  const supprimerLigneASection = (sectionIndex, ligneId) => {
    const s = sections.value[sectionIndex];
    if (s) {
      s.lignes = s.lignes.filter(l => l.id !== ligneId);
    }
  };

  const mettreAJourLigne = (sectionIndex, updatedLigne) => {
    const s = sections.value[sectionIndex];
    if (s) {
      const idx = s.lignes.findIndex(l => l.id === updatedLigne.id);
      if (idx !== -1) {
        s.lignes[idx] = updatedLigne;
      }
    }
  };

  // Alias array methods for ModeleEditor which uses "groupe" instead of "section"
  // For better unified API, both should use section. But we expose these just in case or just use section.

  return {
    sections,
    ajouterSection,
    supprimerSection,
    mettreAJourSection,
    ajouterLigneASection,
    supprimerLigneASection,
    mettreAJourLigne
  };
}
