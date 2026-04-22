import { ref, computed, watch } from 'vue';

export function useEditorValidation(sectionsRef, legendeMoyensRef, toast) {
  const showLegendValidation = ref(false);

  // Vérifier s'il y a au moins un instrument personnalisé dans toutes les sections
  const hasCustomInstrumentsGlobal = computed(() => {
    return (sectionsRef.value || []).some(section =>
      (section.lignes || []).some(ligne => 
        /[\*~!@#$%^&]/.test(ligne.instrumentCode || '')
      )
    );
  });

  // Enlever l'alerte si la légende devient valide
  watch(legendeMoyensRef, (value) => {
    if (value?.trim()) {
      showLegendValidation.value = false;
    }
  });

  const validerLegendeMoyens = () => {
    if (hasCustomInstrumentsGlobal.value && !legendeMoyensRef.value?.trim()) {
      showLegendValidation.value = true;
      toast.add({ 
        severity: 'warn', 
        summary: '⚠️ Légende OBLIGATOIRE', 
        detail: 'Vous utilisez du texte personnalisé (* *** etc.) - veuillez remplir la légende des moyens.', 
        life: 5000 
      });
      return false;
    }
    showLegendValidation.value = false;
    return true;
  };

  const isNullOrEmpty = (v) => v === null || v === undefined || v === '';
  const isIdValide = (id) => !isNullOrEmpty(id) && id !== '00000000-0000-0000-0000-000000000000';

  const validerSaisieValeurs = () => {
    const hasSections = sectionsRef.value.length > 0;
    if (!hasSections) {
      toast.add({ severity: 'warn', summary: 'Saisie requise', detail: 'La structure doit contenir au moins une section.', life: 5000 });
      return false;
    }

    let hasIncompleteLines = false;
    let hasIncompleteSections = false;

    sectionsRef.value.forEach(section => {
      if (!section.typeSectionId) {
        hasIncompleteSections = true;
      }
      
      (section.lignes || []).forEach(ligne => {
        if (!isIdValide(ligne.typeControleId) || !isIdValide(ligne.typeCaracteristiqueId)) {
          hasIncompleteLines = true;
        }
      });
    });

    if (hasIncompleteSections) {
      toast.add({ severity: 'error', summary: 'Section incomplète', detail: 'Veuillez définir la nature de toutes vos sections.', life: 6000 });
      return false;
    }

    if (hasIncompleteLines) {
      toast.add({ severity: 'error', summary: 'Ligne incomplète', detail: 'Les lignes de contrôle ajoutées doivent obligatoirement avoir une "Caractéristique" et un "Type de contrôle".', life: 6000 });
      return false;
    }

    return true;
  };

  const validerSaisiePlan = () => {
    const hasLignes = sectionsRef.value.some(section => (section.lignes || []).length > 0);
    if (!hasLignes) {
      toast.add({ severity: 'warn', summary: 'Saisie requise', detail: 'Veuillez ajouter au moins une ligne de contrôle.', life: 5000 });
      return false;
    }

    let hasMissingTypeControle = false;
    let hasMissingActivationFields = false;

    sectionsRef.value.forEach(section => {
      (section.lignes || []).forEach(ligne => {
        if (!ligne.typeControleId) {
          hasMissingTypeControle = true;
        }

        const hasValeurNominale =
          ligne.valeurNominale !== null &&
          ligne.valeurNominale !== undefined &&
          ligne.valeurNominale !== '';

        if (hasValeurNominale) {
          const tolSupMissing = ligne.toleranceSuperieure === null || ligne.toleranceSuperieure === undefined;
          const tolInfMissing = ligne.toleranceInferieure === null || ligne.toleranceInferieure === undefined;

          if (tolSupMissing || tolInfMissing) {
            hasMissingActivationFields = true;
          }
        }
      });
    });

    if (hasMissingTypeControle) {
      toast.add({
        severity: 'error',
        summary: 'Ligne incomplète',
        detail: 'Veuillez définir le "Type de contrôle" pour toutes vos lignes, ou supprimez les lignes vides avant d\'activer le plan.',
        life: 6000
      });
      return false;
    }

    if (hasMissingActivationFields) {
      toast.add({
        severity: 'error',
        summary: 'Champs obligatoires manquants',
        detail: 'Si une valeur nominale est saisie, renseignez aussi Tolérance min, Tolérance max.',
        life: 7000
      });
      return false;
    }

    return true;
  };

  return {
    showLegendValidation,
    hasCustomInstrumentsGlobal,
    validerLegendeMoyens,
    validerSaisieValeurs,
    validerSaisiePlan
  };
}
