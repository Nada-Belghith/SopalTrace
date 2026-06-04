import { GenericMachineStrategy } from './GenericMachineStrategy';

export class MasMachineStrategy extends GenericMachineStrategy {
    get role() { return 'MAS'; }

    get pieceReferenceLabel() { 
        return this.isMAS19 ? 'Numéro du moyen de contrôle' : super.pieceReferenceLabel; 
    }

    get moyenDetectionLabel() { 
        return this.isMAS19 ? 'Moyen de contrôle' : super.moyenDetectionLabel; 
    }

    get isMAS19() { return this.store.entete.machineCode?.toUpperCase().includes('MAS19'); }
    get isMAS22() { return this.store.entete.machineCode?.toUpperCase().includes('MAS22'); }
    get isMAS26() { return this.store.entete.machineCode?.toUpperCase().includes('MAS26'); }

    get hidePressionAndDp() { return this.isMAS22 || this.isMAS26; }
    get hideDp() { return this.isMAS19 || this.hidePressionAndDp; }

    get showMoyenDetection() { 
        // MAS26 cache la colonne MoyenDetection dans la section Risques (dans le design actuel, à adapter si besoin)
        return super.showMoyenDetection; 
    }
    
    get hasSubHeaders() { return super.hasSubHeaders || this.isMAS22 || this.isMAS26; }
}
