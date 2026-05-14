export function parseFrequenceLibelle(libelle, periodicites = []) {
  if (!libelle) {
    return { modeFreq: 'SANS', freqNum: 1, typeVariable: 'HEURE', freqHours: 1, periodiciteId: null };
  }

  const lib = libelle.toLowerCase();

  // 1. Chercher dans les périodicités fixes
  const perMatch = periodicites.find(p => p.libelle.toLowerCase() === lib);
  if (perMatch) {
    return { 
      modeFreq: 'FIXE', 
      periodiciteId: perMatch.id,
      freqNum: 1, 
      typeVariable: 'HEURE', 
      freqHours: 1 
    };
  }

  // 2. Tenter de parser comme variable
  if (lib.includes('pièce') || lib.includes('piece') || lib.includes('serie') || lib.includes('échantillon') || lib.includes('echantillon')) {
    const result = { modeFreq: 'VARIABLE', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
    
    // X pièces / Y heures
    const mH = lib.match(/(\d+)\s*pièce.*?(\d+)\s*heure/i);
    if (mH) {
      result.freqNum = parseInt(mH[1]);
      result.freqHours = parseInt(mH[2]);
      result.typeVariable = 'HEURE';
      return result;
    } 
    
    // X pièces / heure
    const mH1 = lib.match(/(\d+)\s*pièce.*?heure/i);
    if (mH1) {
      result.freqNum = parseInt(mH1[1]);
      result.freqHours = 1;
      result.typeVariable = 'HEURE';
      return result;
    }

    // Série de X
    const mS = lib.match(/série de (\d+)/i) || lib.match(/serie de (\d+)/i);
    if (mS) {
      result.freqNum = parseInt(mS[1]);
      result.typeVariable = 'SERIE';
      return result;
    }

    // X échantillons
    const mE = lib.match(/(\d+)\s*échantillon/i) || lib.match(/(\d+)\s*echantillon/i);
    if (mE) {
      result.freqNum = parseInt(mE[1]);
      result.typeVariable = 'ECHANTILLON';
      return result;
    }
    
    return result;
  }
  
  // Par défaut, s'il n'y a pas de match
  return { modeFreq: 'VARIABLE', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
}
