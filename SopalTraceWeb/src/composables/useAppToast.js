import { useToast } from 'primevue/usetoast';

/**
 * Composable centralisant la gestion des notifications (Toasts)
 * Permet d'éviter de répéter la configuration (severity, life, etc.)
 */
export function useAppToast() {
    const toast = useToast();

    /**
     * Notification de succès (Vert)
     */
    const success = (message, title = 'Succès') => {
        toast.add({
            severity: 'success',
            summary: title,
            detail: message,
            life: 3000
        });
    };

    /**
     * Notification d'erreur (Rouge)
     */
    const error = (message, title = 'Erreur') => {
        toast.add({
            severity: 'error',
            summary: title,
            detail: message || 'Une erreur imprévue est survenue.',
            life: 5000
        });
    };

    /**
     * Notification d'avertissement (Orange)
     */
    const warn = (message, title = 'Attention') => {
        toast.add({
            severity: 'warn',
            summary: title,
            detail: message,
            life: 4000
        });
    };

    /**
     * Notification d'information (Bleu)
     */
    const info = (message, title = 'Information') => {
        toast.add({
            severity: 'info',
            summary: title,
            detail: message,
            life: 3000
        });
    };

    return {
        success,
        error,
        warn,
        info,
        // Accès direct si besoin d'une config spécifique
        toast
    };
}
