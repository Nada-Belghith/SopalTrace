import { useConfirm } from 'primevue/useconfirm';

/**
 * Composable centralisant la gestion des boîtes de dialogue de confirmation.
 * Permet d'unifier le style (icônes, libellés, classes) dans toute l'application.
 */
export function useAppConfirm() {
    const confirm = useConfirm();

    /**
     * Méthode générique pour demander une confirmation
     */
    const ask = (options) => {
        confirm.require({
            header: options.header || 'Confirmation',
            message: options.message || 'Êtes-vous sûr de vouloir continuer ?',
            icon: options.icon || 'pi pi-exclamation-triangle text-yellow-500',
            acceptLabel: options.acceptLabel || 'Confirmer',
            rejectLabel: options.rejectLabel || 'Annuler',
            acceptClass: options.acceptClass || 'p-button-primary',
            rejectClass: options.rejectClass || 'p-button-secondary p-button-text',
            accept: options.accept,
            reject: options.reject
        });
    };

    /**
     * Confirmation spécifique pour une suppression (Danger/Rouge)
     */
    const confirmDelete = (onAccept, message = 'Cette action est irréversible. Voulez-vous vraiment supprimer cet élément ?', header = 'Suppression') => {
        ask({
            header,
            message,
            icon: 'pi pi-trash text-red-500',
            acceptLabel: 'Oui, Supprimer',
            acceptClass: 'p-button-danger',
            accept: onAccept
        });
    };

    /**
     * Confirmation spécifique pour un archivage ou une action importante
     */
    const confirmAction = (onAccept, message, header = 'Confirmation', icon = 'pi pi-info-circle text-blue-500') => {
        ask({
            header,
            message,
            icon,
            acceptLabel: 'Continuer',
            accept: onAccept
        });
    };

    return {
        ask,
        confirmDelete,
        confirmAction,
        confirm // Accès direct au service PrimeVue si besoin
    };
}
