// Interface "abstraite" pour la stratégie de machine
export class IMachineStrategy {
    constructor(store) {
        this.store = store;
    }

    get role() { return 'GENERIC'; }

    // Headers
    get methodeControleLabel() { return 'Moyen/ Méthode de contrôle'; }
    get moyenDetectionLabel() { return 'Moyen de détection'; }
    get pieceReferenceLabel() { return 'Numéro de la pièce référence'; }
    get fuiteEtalonLabel() { return 'Fuite Étalon'; }
    get dpAfficheeLabel() { return 'ΔP affichée (en Pa)'; }
    get observationLabel() { return 'Observation en cas de non-conformité'; }

    // Visibilité des colonnes
    get showConformiteSection() { return this.store.entete.afficheConformite; }
    get showMoyenDetection() { return this.store.entete.afficheMoyenDetectionRisques; }
    get showFuiteEtalon() { return this.store.entete.afficheFuiteEtalon; }
    get hidePressionAndDp() { return false; }
    get hideDp() { return false; }

    // Architecture des données
    get isArchitectureA() { return false; }
    get hasFamilleHeaders() { return this.store.entete.afficheFamilles && this.store.familles.length > 0; }
    get hasSubHeaders() { return this.hasFamilleHeaders; }

    // Obtenir la pièce de référence par défaut
    getRoleForPiece(inConformite) { return inConformite ? 'PRC' : 'PRNC'; }
}
