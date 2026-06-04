import { GenericMachineStrategy } from './GenericMachineStrategy';

export class BeeMachineStrategy extends GenericMachineStrategy {
    get role() { return 'BEE'; }

    get methodeControleLabel() { return 'Méthode de controle'; }
    get moyenDetectionLabel() { return 'Moyen de contrôle'; }
    get pieceReferenceLabel() { return 'Numéro du moyen de contrôle'; }
    get fuiteEtalonLabel() { return 'Numéro du fuite étalon'; }
    get dpAfficheeLabel() { 
        return this.store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : super.dpAfficheeLabel; 
    }

    get showFuiteEtalon() { return true; } // Forcé pour BEE
    
    get isArchitectureA() { return false; } // Forcé pour BEE
    
    get hasSubHeaders() { return true; } // Forcé pour BEE
}
