import { computed } from 'vue';

export function useDocumentHeaderMeta(route, store) {
  // Navigation query parameters
  const isArchiveEditing = computed(() => route.query.draft === 'true' || route.query.upgrade === 'true');
  const isUpgradeMode = computed(() => route.query.upgrade === 'true');
  const isForcedView = computed(() => route.query.view === 'true');
  
  // Status and edit mode tracking
  const isArchived = computed(() => store.entete?.statut === 'ARCHIVE');
  const isEditMode = computed(() => !!store.entete?.id);
  
  // Read Only rules
  const isReadOnly = computed(() => {
    return (isEditMode.value && isArchived.value && !isArchiveEditing.value && !isUpgradeMode.value) || isForcedView.value;
  });

  return {
    isArchiveEditing,
    isUpgradeMode,
    isForcedView,
    isArchived,
    isEditMode,
    isReadOnly
  };
}
