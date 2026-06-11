export function parseFrequenceLibelle(libelle, periodicites = []) {
  if (!libelle) {
    return { modeFreq: 'SANS', freqNum: 1, typeVariable: 'HEURE', freqHours: 1, periodiciteId: null };
  }

  const lib = libelle.toLowerCase().trim();

  let result = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };

  // Parser format code stocké en base par erreur: "5P_1H", "3P_2H", "100PCT_1H", "SERIE_5P", "ECH_3"
  const codeFormatH = lib.match(/^(\d+)p_(\d+)h$/i);
  if (codeFormatH) {
    result.modeFreq = 'VARIABLE';
    result.freqNum = parseInt(codeFormatH[1]);
    result.freqHours = parseInt(codeFormatH[2]);
    result.typeVariable = 'HEURE';
    return result;
  }
  const codeFormatSerie = lib.match(/^serie_(\d+)p$/i);
  if (codeFormatSerie) {
    result.modeFreq = 'VARIABLE';
    result.freqNum = parseInt(codeFormatSerie[1]);
    result.typeVariable = 'SERIE';
    return result;
  }
  const codeFormatEch = lib.match(/^ech_(\d+)$/i);
  if (codeFormatEch) {
    result.modeFreq = 'VARIABLE';
    result.freqNum = parseInt(codeFormatEch[1]);
    result.typeVariable = 'ECHANTILLON';
    return result;
  }


  let foundVariable = false;
  if (lib.includes('pièce') || lib.includes('piece') || lib.includes('serie') || lib.includes('échantillon') || lib.includes('echantillon')) {
    const mH = lib.match(/(\d+)\s*pièce.*?(\d+)\s*heure/i);
    if (mH) {
      result.freqNum = parseInt(mH[1]);
      result.freqHours = parseInt(mH[2]);
      result.typeVariable = 'HEURE';
      foundVariable = true;
    } else {
      const mH1 = lib.match(/(\d+)\s*pièce.*?heure/i);
      if (mH1) {
        result.freqNum = parseInt(mH1[1]);
        result.freqHours = 1;
        result.typeVariable = 'HEURE';
        foundVariable = true;
      } else {
        const mS = lib.match(/série de (\d+)/i) || lib.match(/serie de (\d+)/i);
        if (mS) {
          result.freqNum = parseInt(mS[1]);
          result.typeVariable = 'SERIE';
          foundVariable = true;
        } else {
          const mE = lib.match(/(\d+)\s*échantillon/i) || lib.match(/(\d+)\s*echantillon/i);
          if (mE) {
            result.freqNum = parseInt(mE[1]);
            result.typeVariable = 'ECHANTILLON';
            foundVariable = true;
          }
        }
      }
    }
  }

  if (foundVariable) {
    result.modeFreq = 'VARIABLE';
    return result;
  }

  const perMatch = periodicites.find(p => p.libelle.toLowerCase() === lib);
  if (perMatch) {
    result.modeFreq = 'FIXE';
    result.periodiciteId = perMatch.id;
    return result;
  }

  return result; // modeFreq sera 'SANS' si on arrive ici
}
