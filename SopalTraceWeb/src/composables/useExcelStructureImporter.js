export function useExcelStructureImporter(store, toast) {
  const handleExcelImport = async (event) => {
    const file = event.target.files[0];
    if (!file) return;
    
    // Reset the input value so the same file can be selected again
    event.target.value = '';
    
    try {
      const result = await store.importerDepuisExcel(file);
      if (result.success) {
        toast.add({
          severity: 'success',
          summary: 'Importation terminée',
          detail: `${result.total} ligne(s) récupérée(s) depuis le fichier.`,
          life: 4000
        });
      }
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Échec de l\'import',
        detail: error.response?.data?.message || 'Impossible de lire le fichier Excel.',
        life: 5000
      });
    }
  };

  return { handleExcelImport };
}
