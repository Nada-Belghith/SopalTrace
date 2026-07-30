import { computed } from 'vue';

export function useModelHeaderMeta(store, ctx) {
  const codeAffiche = computed(() => {
    if (ctx.isEditMode.value && ctx.codeOriginal.value) {
      if (ctx.isReadOnly.value) return ctx.codeOriginal.value;
      return `${ctx.codeOriginal.value.replace(/(?:[-\s]+V\d+)+$/i, '')}-V${ctx.version.value + 1}`;
    }
    return store.entete.code || store.codeModeleAuto;
  });

  const headerTitle = computed(() => {
    const nature = store.entete.natureComposantCode;
    const famille = store.entete.familleProduitCode;
    const poste = store.entete.posteCode;

    if (ctx.isForcedView.value) return 'Consultation du Plan Générique';

    if (nature === 'PISTON') {
      return ctx.isArchiveEditing.value ? "Création : Plan en cours d'assemblage PISTON" : "Plan en cours d'assemblage PISTON";
    }

    if (nature === 'PF') {
      let title = ctx.isArchiveEditing.value ? "Création : Plan en cours d'assemblage PF" : "Plan en cours d'assemblage PF";
      if (famille) {
        title += ` - ${famille}`;
      }
      const famObject = store.famillesProduit?.find(f => f.code === famille);
      const isSoupape = famObject?.libelle?.toLowerCase().includes('soupape');
      if (isSoupape && poste) {
        title += ` - Poste ${poste}`;
      }
      return title;
    }

    if (nature === 'CORPS' || nature === 'VOLANT') {
      return ctx.isArchiveEditing.value ? `Création : Plan en cours de fabrication ${nature}` : `Plan en cours de fabrication ${nature}`;
    }

    if (ctx.isEditMode.value) {
      if (ctx.isArchived.value && !ctx.isArchiveEditing.value) return 'Mise à jour d\'Archive';
      if (ctx.isArchiveEditing.value) return 'Création : Nouvelle Version';
      return `Édition du Plan Générique`;
    }
    return 'Création d\'un Plan Générique';
  });

  const headerSubtitle = computed(() => {
    const nature = store.entete.natureComposantCode;

    if (ctx.isForcedView.value) return 'Mode lecture seule (Aperçu de la structure).';

    if (nature === 'PISTON') {
      return 'Configuration générique pour les PISTONS (sans choix de famille).';
    }

    if (nature === 'PF') {
      return 'Configurez le plan selon la famille de produit fini sélectionnée.';
    }

    if (ctx.isEditMode.value) {
      if (ctx.isArchived.value && !ctx.isArchiveEditing.value) {
        return 'Vous consultez une archive. Mettre à jour vous permettra de préparer une nouvelle version.';
      }
      if (ctx.isArchiveEditing.value) {
        return 'Modifiez les valeurs. L\'ancienne version sera archivée automatiquement lors de la sauvegarde.';
      }
      return 'Modifiez la structure. L\'ancienne version sera archivée automatiquement.';
    }
    return 'Configurez la structure des plans du contrôle.';
  });

  const actionButtonLabel = computed(() => {
    if (ctx.isLoading.value) return 'Enregistrement...';
    if (ctx.isArchived.value && !ctx.isArchiveEditing.value) return 'Éditer et Mettre à jour ce Plan';
    if (ctx.isArchived.value && ctx.isArchiveEditing.value) return 'Enregistrer la Nouvelle Version';
    if (!ctx.isEditMode.value) return 'Créer et Activer le Modèle';
    return 'Créer Nouvelle Version';
  });

  const actionButtonIcon = computed(() => {
    if (ctx.isArchived.value) return 'pi pi-sync';
    if (ctx.isEditMode.value) return 'pi pi-save';
    return 'pi pi-check';
  });

  const actionButtonVariant = computed(() => {
    if (ctx.isArchived.value) return 'warning';
    if (ctx.isEditMode.value) return 'primary';
    return 'primary';
  });

  return {
    codeAffiche,
    headerTitle,
    headerSubtitle,
    actionButtonLabel,
    actionButtonIcon,
    actionButtonVariant
  };
}
