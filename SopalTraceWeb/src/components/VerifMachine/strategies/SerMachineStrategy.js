import { GenericMachineStrategy } from './GenericMachineStrategy';

export class SerMachineStrategy extends GenericMachineStrategy {
    get role() { return 'SER'; }

    get moyenDetectionLabel() { return 'Moyen de contrôle'; }
    get pieceReferenceLabel() { return 'N° moyen de contrôle'; }
    get observationLabel() { return 'Action en cas de non-conformité'; }

    get hidePressionAndDp() { return this.isSER05; }
    
    get isSER05() { return this.store.entete.machineCode?.toUpperCase().includes('SER05'); }

    get isArchitectureA() { return true; } // Forcé pour SER
    get hasSubHeaders() { return super.hasSubHeaders || this.isSER05; }
}
