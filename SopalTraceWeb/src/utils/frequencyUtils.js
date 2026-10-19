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

  const perMatch = periodicites.find(p => libellePeriodicite(p).toLowerCase() === lib);
  if (perMatch) {
    result.modeFreq = 'VARIABLE';
    result.periodiciteId = perMatch.id;
    return result;
  }

  return result; // modeFreq sera 'SANS' si on arrive ici
}

export function resolveFrequencyFromPeriodiciteId(periodiciteId, periodicites = []) {
  if (!periodiciteId) return null;
  const perio = (periodicites || []).find(p => {
    const pId = p.id || p.Id;
    return pId && typeof pId === 'string' && typeof periodiciteId === 'string' && pId.toLowerCase() === periodiciteId.toLowerCase();
  });
  if (!perio) return null;

  let modeFreq = 'VARIABLE';
  let freqNum = perio.nbrPieces !== undefined ? perio.nbrPieces : (perio.NbrPieces !== undefined ? perio.NbrPieces : 1);
  let typeVariable = 'HEURE';
  let freqHours = perio.nbrHeures !== undefined ? perio.nbrHeures : (perio.NbrHeures !== undefined ? perio.NbrHeures : 1);

  const unite = (perio.unite || perio.Unite || '').toUpperCase();
  if (!unite) {
    modeFreq = 'SANS';
  } else if (unite === 'HEURE') {
    typeVariable = 'HEURE';
  } else if (unite === 'SERIE') {
    typeVariable = 'SERIE';
  } else if (unite === 'ECHANTILLON') {
    typeVariable = 'ECHANTILLON';
  }

  return {
    modeFreq,
    periodiciteId,
    freqNum,
    typeVariable,
    freqHours
  };
}

/**
 * Libellé d'une périodicité, identique à celui de l'API (PeriodiciteDto.Libelle) : une périodicité créée
 * pendant l'édition et ajoutée à la liste locale doit s'afficher et se rechercher comme une périodicité relue.
 */
export function libellePeriodicite(p) {
  if (!p) return '';
  const existant = p.libelle || p.Libelle;
  if (existant) return existant;
  const n = p.nbrPieces ?? p.NbrPieces;
  const unite = p.unite || p.Unite;
  const heures = p.nbrHeures ?? p.NbrHeures;
  if (!n || !unite) return '';
  const pluriel = n > 1 ? 's' : '';
  if (unite === 'HEURE') {
    const h = (heures === 1 || heures == null) ? 'heure' : `${heures} heures`;
    return n === 100 ? `100% des pièces / ${h}` : `${n} pièce${pluriel} / ${h}`;
  }
  if (unite === 'ECHANTILLON') return `${n} échantillon${pluriel}`;
  return `une série de ${n} pièce${pluriel}`;
}

/** Périodicité prête à être ajoutée à une liste du référentiel (libellé calculé). */
export const periodiciteAvecLibelle = (p) => ({ ...p, libelle: libellePeriodicite(p) });
