import { useConfirm } from 'primevue/useconfirm';

/**
 * Hook personnalisé pour afficher la boîte de dialogue de confirmation
 * d'archivage d'un plan actif existant.
 */
export function useActivePlanConfirmation() {
    const confirm = useConfirm();

    /**
     * Affiche la modale de confirmation.
     * @param {Object} options 
     * @param {string} options.typeDocument - Le type de document (ex: "plan produit fini", "fiche de contrôle", "plan de vérification")
     * @param {string} options.identifiant - Ce qui identifie le plan (ex: "cette famille", "ce poste (P1)", "la machine M1")
     * @param {number|string} [options.version] - La version du plan existant (optionnel)
     * @returns {Promise<boolean>} true si l'utilisateur confirme, false sinon.
     */
    const confirmArchivagePlanActif = async ({ typeDocument, identifiant, version }) => {
        let message = `Un ${typeDocument} actif `;
        if (version !== undefined && version !== null) {
            message += `(Version ${version}) `;
        }
        message += `existe déjà pour ${identifiant}.\n\nVoulez-vous l'archiver et activer cette nouvelle version ?`;

        return new Promise((resolve) => {
            confirm.require({
                message: message,
                header: 'Plan Actif Existant',
                icon: 'ri-error-warning-line text-amber-500',
                acceptLabel: 'Oui, archiver',
                rejectLabel: 'Annuler',
                accept: () => resolve(true),
                reject: () => resolve(false),
                onHide: () => resolve(false)
            });
        });
    };

    return {
        confirmArchivagePlanActif
    };
}
