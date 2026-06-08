import { BeeMachineStrategy } from './BeeMachineStrategy';
import { MasMachineStrategy } from './MasMachineStrategy';
import { SerMachineStrategy } from './SerMachineStrategy';
import { GenericMachineStrategy } from './GenericMachineStrategy';

export class MachineStrategyFactory {
    static getStrategy(machineCode, store) {
        if (!machineCode) return new GenericMachineStrategy(store);
        
        const code = machineCode.toUpperCase().replace('-', '').replace(' ', '').trim();
        
        if (code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47')) {
            return new BeeMachineStrategy(store);
        }
        
        if (code.includes('MAS')) {
            return new MasMachineStrategy(store);
        }
        
        if (code.startsWith('SER')) {
            return new SerMachineStrategy(store);
        }

        return new GenericMachineStrategy(store);
    }
}
