import { ref, computed, watch } from 'vue';

export function useEditorValidation(sectionsRef, legendeMoyensRef, toast) {
  const showLegendValidation = ref(false);

  // Helper pour afficher les alertes de manière robuste, que toast soit un raw PrimeVue toast ou un useAppToast()
  const showError = (message, title = 'Erreur') => {
    if (toast) {
      if (typeof toast.error === 'function') {
        toast.error(message, title);
      } else if (typeof toast.add === 'function') {
        toast.add({ severity: 'error', summary: title, detail: message, life: 5000 });
      } else {
        console.error(`[Toast Error] ${title}: ${message}`);
      }
    } else {
      console.error(`[Toast Error] ${title}: ${message}`);
    }
  };

  const showWarn = (message, title = 'Attention') => {
    if (toast) {
      if (typeof toast.warn === 'function') {
        toast.warn(message, title);
      } else if (typeof toast.add === 'function') {
        toast.add({ severity: 'warn', summary: title, detail: message, life: 4000 });
      } else {
        console.warn(`[Toast Warn] ${title}: ${message}`);
      }
    } else {
      console.warn(`[Toast Warn] ${title}: ${message}`);
    }
  };

  // Vérifier s'il y a au moins un instrument personnalisé dans toutes les sections
  const hasCustomInstrumentsGlobal = computed(() => {
    return (sectionsRef.value || []).some(section =>
      (section.lignes || []).some(ligne => 
        /[*~!@#$%^&]/.test(ligne.instrumentCode || '')
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
      showWarn('La structure doit contenir au moins une section.', 'Saisie requise');
      return false;
    }

    let hasIncompleteLines = false;

    sectionsRef.value.forEach(section => {
      (section.lignes || []).forEach(ligne => {
        // En Fabrication, on peut saisir un libellé à la main (libelleAffiche) 
        // ou choisir une caractéristique (typeCaracteristiqueId).
        const hasCarac = isIdValide(ligne.typeCaracteristiqueId) || !isNullOrEmpty(ligne.libelleAffiche);
        const hasCtrl = isIdValide(ligne.typeControleId);

        if (!hasCarac || !hasCtrl) {
          hasIncompleteLines = true;
        }
      });
    });

    if (hasIncompleteLines) {
      showError('Les lignes de contrôle ajoutées doivent obligatoirement avoir une "Caractéristique" et un "Type de contrôle".', 'Ligne incomplète');
      return false;
    }

    return true;
  };

  const validerSaisiePlan = () => {
    const hasLignes = sectionsRef.value.some(section => (section.lignes || []).length > 0);
    if (!hasLignes) {
      showWarn('Veuillez ajouter au moins une ligne de contrôle.', 'Saisie requise');
      return false;
    }

    let hasMissingTypeControle = false;

    sectionsRef.value.forEach(section => {
      (section.lignes || []).forEach(ligne => {
        if (!ligne.typeControleId) {
          hasMissingTypeControle = true;
        }
      });
    });

    if (hasMissingTypeControle) {
      showError('Veuillez définir le "Type de contrôle" pour toutes vos lignes, ou supprimez les lignes vides avant d\'activer le plan.', 'Ligne incomplète');
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
