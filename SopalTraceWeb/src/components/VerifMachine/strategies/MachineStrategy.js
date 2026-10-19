// Présentation du formulaire de vérification d'une machine.
// Tout est piloté par la configuration de la machine (table Machine, colonnes Verif*) renvoyée par le référentiel :
// aucun comportement n'est déduit du code de la machine.
export class MachineStrategy {
    constructor(store, config) {
        this.store = store;
        this.config = config || {};
    }

    // Modèle de formulaire : BEE, MAS, SER ou GENERIC
    get role() { return this.config.verifModele || 'GENERIC'; }
    get estBee() { return this.role === 'BEE'; }
    get estSer() { return this.role === 'SER'; }

    // Sections et tableaux
    get sansConformite() { return !!this.config.sansConformite; }
    get tablesEtalonSeparees() { return !!this.config.tablesEtalonSeparees; }
    get colonnesParTableau() { return !!this.config.colonnesParTableau; }

    // En-têtes
    get methodeControleLabel() { return this.estBee ? 'Méthode de contrôle' : 'Moyen/ Méthode de contrôle'; }
    get moyenDetectionLabel() {
        return (this.estBee || this.estSer || this.config.moyenControleNumerote || this.isArchitectureA)
            ? 'Moyen de contrôle' : 'Moyen de détection';
    }
    get pieceReferenceLabel() {
        if (this.estBee || this.config.moyenControleNumerote) return 'Numéro du moyen de contrôle';
        if (this.estSer || this.isArchitectureA) return 'N° moyen de contrôle';
        return 'Numéro de la pièce référence';
    }
    get fuiteEtalonLabel() { return this.estBee ? 'Numéro du fuite étalon' : 'Fuite Étalon'; }
    get dpAfficheeLabel() { return this.config.fuiteEnPa ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)'; }
    get observationLabel() { return this.estSer ? 'Action en cas de non-conformité' : 'Observation en cas de non-conformité'; }

    // Visibilité des colonnes
    get showConformiteSection() { return this.store.entete.afficheConformite && !this.sansConformite; }
    get showMoyenDetection() { return this.store.entete.afficheMoyenDetectionRisques; }
    get showFuiteEtalon() { return !!this.store.entete.afficheFuiteEtalon || !!this.config.fuiteEtalon; }
    get hidePressionAndDp() { return !!this.config.sansPression; }
    get hideDp() { return !!this.config.sansDeltaP || this.hidePressionAndDp; }

    // Architecture A : même périodicité utilisée par plusieurs lignes d'un même risque (toujours vraie pour SER, jamais pour BEE)
    get isArchitectureA() {
        if (this.estSer) return true;
        if (this.estBee) return false;
        const parRisque = new Map();
        for (const ligne of this.store.lignesRisques) {
            const cle = ligne.libelleRisque || '';
            if (!parRisque.has(cle)) parRisque.set(cle, []);
            parRisque.get(cle).push(ligne);
        }
        for (const lignes of parRisque.values()) {
            if (lignes.length <= 1) continue;
            const vues = new Set();
            for (const ligne of lignes) {
                for (const group of ligne.groups) {
                    if (!group.periodiciteId) continue;
                    if (vues.has(group.periodiciteId)) return true;
                    vues.add(group.periodiciteId);
                }
            }
        }
        return false;
    }

    get hasFamilleHeaders() { return this.store.entete.afficheFamilles && this.store.familles.length > 0; }
    get hasSubHeaders() {
        return this.estBee || this.hasFamilleHeaders || this.tablesEtalonSeparees || this.hidePressionAndDp;
    }

    // Pièce de référence par défaut
    getRoleForPiece(inConformite) { return inConformite ? 'PRC' : 'PRNC'; }
}
