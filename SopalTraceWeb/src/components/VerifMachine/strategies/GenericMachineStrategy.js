import { IMachineStrategy } from './IMachineStrategy';

export class GenericMachineStrategy extends IMachineStrategy {
    get role() { return 'GENERIC'; }

    // Détection d'architecture (Architecture A : même périodicité utilisée par plusieurs lignes d'un même risque)
    get isArchitectureA() {
        const byRisque = this._groupBy(this.store.lignesRisques, l => l.libelleRisque || '');
        for (const [key, lignes] of Object.entries(byRisque)) {
            if (lignes.length <= 1) continue;
            const perioMap = new Map();
            for (const ligne of lignes) {
                for (const group of ligne.groups) {
                    if (group.periodiciteId) {
                        if (perioMap.has(group.periodiciteId)) return true;
                        perioMap.set(group.periodiciteId, true);
                    }
                }
            }
        }
        return false;
    }

    get moyenDetectionLabel() { 
        return this.isArchitectureA ? 'Moyen de contrôle' : super.moyenDetectionLabel; 
    }

    get pieceReferenceLabel() { 
        return this.isArchitectureA ? 'N° moyen de contrôle' : super.pieceReferenceLabel; 
    }

    _groupBy(array, keyGetter) {
        return array.reduce((acc, item) => {
            const key = keyGetter(item);
            if (!acc[key]) acc[key] = [];
            acc[key].push(item);
            return acc;
        }, {});
    }
}
