import { computed } from 'vue';
import { useVerifMachineStore } from '@/stores/verifMachineStore';
import { MachineStrategyFactory } from '../strategies/MachineStrategyFactory';

export function useVerifMachineTable() {
    const store = useVerifMachineStore();
    
    const strategy = computed(() => MachineStrategyFactory.getStrategy(store.entete.machineCode, store));

    const isMachineSansConformite = computed(() => {
        const code = (store.entete.machineCode || '').toUpperCase();
        return code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') || 
               code.includes('MAS19') || code.includes('MAS20') || code.startsWith('SER');
    });

    const hasFamilleHeaders = computed(() => store.entete.afficheFamilles && store.familles.length > 0);
    const hasSubHeaders = computed(() => strategy.value.hasSubHeaders);

    const getCustomColumnsAfter = (targetTable, key) => {
        const cols = store.entete.configurationColonnes || [];
        const parsed = typeof cols === 'string' ? JSON.parse(cols) : cols;
        return (parsed || []).filter(col => {
            if (col.insertAfter !== key) return false;
            if (targetTable && targetTable !== 'all' && col.targetTable && col.targetTable !== 'all') {
                if (col.targetTable !== targetTable) return false;
            }
            return true;
        });
    };

    const ensureColonnes = (ligne) => {
        if (!ligne.valeursColonnesSpecifiques) {
            ligne.valeursColonnesSpecifiques = {};
        }
        return ligne.valeursColonnesSpecifiques;
    };

    const getLigneTotalRows = (ligne) => {
        return ligne.groups.reduce((total, group) => total + group.rows.length, 0);
    };

    const totalColumns = computed(() => {
        let count = 3;
        if (strategy.value.showMoyenDetection) count++;
        count += hasFamilleHeaders.value ? store.familles.length : 1;
        if (strategy.value.showFuiteEtalon) count++;
        
        count += strategy.value.hasSubHeaders ? 5 : 4;
        
        if (strategy.value.hideDp) count -= 1;
        if (strategy.value.hidePressionAndDp) count -= 2;
        
        count += getCustomColumnsAfter('all', 'any').length;
        return count;
    });

    const onUpdateRisqueName = (oldValue, newValue) => {
        store.lignesRisques.forEach(l => {
            if (l.libelleRisque === oldValue) {
                l.libelleRisque = newValue;
            }
        });
        store.lignesConformite.forEach(l => {
            if (l.libelleRisque === oldValue) {
                l.libelleRisque = newValue;
            }
        });
    };

    const getFuiteValue = (row) => {
        return store.getPieceValue(row, null, 'FEC') || store.getPieceValue(row, null, 'FENC');
    };

    const getFuiteRole = (row) => {
        if (store.getPieceValue(row, null, 'FEC')) return 'FEC';
        if (store.getPieceValue(row, null, 'FENC')) return 'FENC';
        return 'FEC';
    };

    const setFuiteValue = (row, pieceId) => {
        const role = getFuiteRole(row);
        const piece = [...store.fuitesEtalon].find(p => p.id === pieceId);
        const finalRole = piece?.typePiece || role;
        store.setPieceValue(row, null, 'FEC', null);
        store.setPieceValue(row, null, 'FENC', null);
        if (pieceId) store.setPieceValue(row, null, finalRole, pieceId);
    };

    return {
        store,
        strategy,
        isMachineSansConformite,
        hasFamilleHeaders,
        hasSubHeaders,
        getCustomColumnsAfter,
        ensureColonnes,
        getLigneTotalRows,
        totalColumns,
        onUpdateRisqueName,
        getFuiteValue,
        getFuiteRole,
        setFuiteValue
    };
}
