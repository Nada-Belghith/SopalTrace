import { resolveFrequencyFromPeriodiciteId, parseFrequenceLibelle } from '@/utils/frequencyUtils';

export function usePlanMapper(store) {
  
  const normalizeId = (id) => (typeof id === 'string' && id.length <= 36 ? id : null);

  const normalizePlanData = (data) => {
    if (!data) return data;
    if (data.nom) {
      data.codeArticleSageVersionne = data.nom;
      const match = data.nom.match(/^(.*?)(\.\w+)?$/);
      data.codeArticleSage = match ? match[1] : data.nom;
    }
    return data;
  };

  const cleanSectionName = (libelleSection, typeSectionId, freqLib = '', regleLib = '') => {
    if (!libelleSection) return '';
    const normalizeApostrophes = (s) => s.replace(/’/g, "'");
    let clean = normalizeApostrophes(libelleSection).replace(/caractéristiques à contrôler/gi, '').trim();
    
    const escapeRegExp = (str) => str.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&');
    
    if (typeSectionId) {
      const typeSec = (store.typesSection || []).find(t => t.id === typeSectionId);
      if (typeSec && typeSec.libelle) {
        clean = clean.replace(new RegExp(escapeRegExp(normalizeApostrophes(typeSec.libelle)), 'gi'), '').trim();
      }
    }
    
    if (freqLib) {
      const freqNorm = normalizeApostrophes(freqLib);
      const freqPattern = '\\(?\\s*' + escapeRegExp(freqNorm) + '\\s*\\)?';
      clean = clean.replace(new RegExp(freqPattern, 'gi'), '').trim();
    }
    
    if (regleLib) {
      const regleNorm = normalizeApostrophes(regleLib);
      const reglePattern = '\\(?\\s*' + escapeRegExp(regleNorm) + '\\s*\\)?';
      clean = clean.replace(new RegExp(reglePattern, 'gi'), '').trim();
    }

    clean = clean.replace(/\(\s*\)/g, '').trim();
    clean = clean.replace(/^[\s\-_:/( )]+|[\s\-_:/( )]+$/g, '').trim();
    return clean;
  };

  const mapModelDataToSections = (modeleModel) => {
    return (modeleModel.sections || []).map(sec => {
      let freqData = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
      if (sec.periodiciteId) {
        const resolved = resolveFrequencyFromPeriodiciteId(sec.periodiciteId, store.periodicites || []);
        if (resolved) {
          freqData = resolved;
        }
      }
      const texteParse = sec.frequenceLibelle || sec.libelleSection || '';
      if (freqData.modeFreq === 'SANS') {
        if (texteParse) {
          freqData = parseFrequenceLibelle(texteParse, store.periodicites || []);
        }
      }

      let modeFreq = freqData.modeFreq;
      let regleEchantillonnageId = sec.regleEchantillonnageId || null;
      let periodiciteId = sec.periodiciteId || freqData.periodiciteId;
      let freqNum = sec.freqNum || freqData.freqNum;
      let typeVariable = sec.typeVariable || freqData.typeVariable;
      let freqHours = sec.freqHours || freqData.freqHours;

      if (regleEchantillonnageId) {
        modeFreq = 'FIXE';
      } else if (periodiciteId) {
        modeFreq = 'VARIABLE';
      } else if (texteParse) {
        const regMatch = (store.reglesEchantillonnage || []).find(r => r.libelle === texteParse);
        if (regMatch) {
          modeFreq = 'FIXE';
          regleEchantillonnageId = regMatch.id;
        }
      }

      let typeSectionId = sec.typeSectionId || '';
      if (!typeSectionId && sec.libelleSection) {
        const secLib = sec.libelleSection.trim().toLowerCase();
        let bestMatch = null;
        let maxLength = -1;

        store.typesSection.forEach(t => {
          const tLib = (t.libelle || '').trim().toLowerCase();
          if (!tLib || secLib === 'section sans nom') return;

          if (secLib.includes(tLib)) {
            if (tLib.length > maxLength) {
              maxLength = tLib.length;
              bestMatch = t;
            }
          }
        });

        if (bestMatch) {
          typeSectionId = bestMatch.id;
        }
      }

      return {
        id: crypto.randomUUID(),
        isFromDb: false,
        modeleSectionId: sec.id,
        typeSectionId,
        modeFreq,
        periodiciteId,
        regleEchantillonnageId,
        freqNum,
        typeVariable,
        freqHours,
        isNewFreq: false,
        frequenceLibelle: sec.frequenceLibelle || '',
        nom: cleanSectionName(sec.libelleSection, typeSectionId, sec.frequenceLibelle || '', sec.regleEchantillonnageLibelle || ''),
        lignes: [...(sec.lignes || [])].filter(lig => lig != null).sort((a, b) => (a.ordreAffiche || 0) - (b.ordreAffiche || 0)).map(lig => ({
          id: crypto.randomUUID(),
          isFromDb: false,
          modeleLigneSourceId: lig.id,
          typeCaracteristiqueId: lig.typeCaracteristiqueId,
          typeControleId: lig.typeControleId,
          moyenControleId: lig.moyenControleId,
          instrumentCode: lig.instrumentCode,
          moyenTexteLibre: lig.moyenTexteLibre || '',
          valeurNominale: lig.valeurNominale ?? null,
          toleranceSuperieure: lig.toleranceSuperieure ?? null,
          toleranceInferieure: lig.toleranceInferieure ?? null,
          unite: lig.unite || '',
          limiteSpecTexte: lig.limiteSpecTexte || '',
          instruction: lig.instruction || '',
          observations: lig.observations || '',
          estCritique: lig.estCritique,
          libelleAffiche: lig.libelleAffiche,
          imageBase64: lig.imageBase64 || null,
          valeursColonnesSpecifiques: lig.extraColonnes 
            ? Object.fromEntries(lig.extraColonnes.map(ec => [ec.cleColonne, ec.valeurColonne])) 
            : (lig.colonnesSupplementaires ? JSON.parse(lig.colonnesSupplementaires) : (lig.valeursColonnesSpecifiques || {}))
        }))
      };
    });
  };

  const syncDbIds = (dbPlanData, plan, sections) => {
    if (!dbPlanData) return;

    plan.value = normalizePlanData(dbPlanData);

    if (!dbPlanData.sections) return;

    sections.value.forEach((sec, sIdx) => {
      const dbSec = dbPlanData.sections.find(ds => ds.ordreAffiche === (sIdx + 1));

      if (dbSec) {
        sec.id = dbSec.id;
        sec.isFromDb = true;
        sec.modeleSectionId = dbSec.modeleSectionId;

        (sec.lignes || []).forEach((lig, lIdx) => {
          const dbLig = (dbSec.lignes || []).find(dl => dl.ordreAffiche === (lIdx + 1));

          if (dbLig) {
            lig.id = dbLig.id;
            lig.isFromDb = true;
            lig.modeleLigneSourceId = dbLig.modeleLigneSourceId;
          } else {
            lig.isFromDb = false;
          }
        });
      } else {
        sec.isFromDb = false;
      }
    });
  };

  const sanitizeMeasurements = (ligne, isDraft = false) => {
    const hasValeur = ligne.valeurNominale != null && ligne.valeurNominale !== '';
    const hasTolSup = ligne.toleranceSuperieure != null && ligne.toleranceSuperieure !== '';
    const hasTolInf = ligne.toleranceInferieure != null && ligne.toleranceInferieure !== '';

    if (isDraft) {
      return {
        valeurNominale: hasValeur ? ligne.valeurNominale : null,
        toleranceSuperieure: hasTolSup ? ligne.toleranceSuperieure : null,
        toleranceInferieure: hasTolInf ? ligne.toleranceInferieure : null
      };
    }

    if (hasValeur && (!hasTolSup || !hasTolInf)) {
      return {
        valeurNominale: null,
        toleranceSuperieure: null,
        toleranceInferieure: null
      };
    }

    return {
      valeurNominale: hasValeur ? ligne.valeurNominale : null,
      toleranceSuperieure: hasTolSup ? ligne.toleranceSuperieure : null,
      toleranceInferieure: hasTolInf ? ligne.toleranceInferieure : null
    };
  };

  const buildServicePayload = (sections, isDraft) => {
    return sections.value.map((originalSection, idx) => {
      let finalFrequenceLibelle = '';
      if (originalSection.modeFreq === 'VARIABLE') {
        const is100 = originalSection.freqNum === 100 && originalSection.typeVariable === 'HEURE';
        if (is100) {
          const p100 = (store.periodicites || []).find(p => p.frequenceNum === 100 || p.code === '100PCT_1H');
          finalFrequenceLibelle = p100 ? p100.libelle : "100% des pièces/h";
        } else {
          finalFrequenceLibelle = originalSection.frequenceLibelle || '';
        }
      } else if (originalSection.periodiciteId) {
        const matchingPeriod = (store.periodicites || []).find(p => {
          const pId = p.id || p.Id;
          return pId && typeof pId === 'string' && typeof originalSection.periodiciteId === 'string' && pId.toLowerCase() === originalSection.periodiciteId.toLowerCase();
        });
        finalFrequenceLibelle = matchingPeriod ? (matchingPeriod.libelle || matchingPeriod.Libelle || '') : '';
      }

      let regleEchLibelle = '';
      const regleEchId = originalSection.regleEchantillonnageId;
      if (regleEchId) {
        regleEchLibelle = (store.reglesEchantillonnage || []).find(r => r.id === regleEchId)?.libelle || '';
      }

      const typeSectionId = originalSection.typeSectionId;

      return {
        id: originalSection.isFromDb ? normalizeId(originalSection.id) : null,
        modeleSectionId: originalSection.modeleSectionId,
        ordreAffiche: idx + 1,
        typeSectionId: (typeSectionId && typeSectionId !== "") ? typeSectionId : null,
        libelleSection: originalSection.nom || originalSection.libelleSection || 'SECTION SANS NOM',
        notes: originalSection.notes || '',
        frequenceLibelle: finalFrequenceLibelle,
        regleEchantillonnageLibelle: regleEchLibelle,
        periodiciteId: (originalSection.periodiciteId && originalSection.periodiciteId !== "") ? originalSection.periodiciteId : null,
        regleEchantillonnageId: (regleEchId && regleEchId !== "") ? regleEchId : null,
        lignes: (originalSection.lignes || []).map((l, lIdx) => {
          const caractMatch = (store.typesCaracteristique || store.caracteristiques || []).find(c => c.id === l.typeCaracteristiqueId);
          const nomCaract = caractMatch?.libelle || '';
          
          const mesurements = sanitizeMeasurements(l, isDraft);
          const hasNumeric = mesurements.valeurNominale != null || mesurements.toleranceInferieure != null || mesurements.toleranceSuperieure != null;
          
          return {
            id: l.isFromDb ? normalizeId(l.id) : null,
            modeleLigneSourceId: l.modeleLigneSourceId,
            ordreAffiche: lIdx + 1,
            typeCaracteristiqueId: (l.typeCaracteristiqueId && l.typeCaracteristiqueId !== "") ? l.typeCaracteristiqueId : null,
            typeControleId: (l.typeControleId && l.typeControleId !== "") ? l.typeControleId : null,
            moyenControleId: (l.moyenControleId && l.moyenControleId !== "") ? l.moyenControleId : null,
            moyenTexteLibre: l.moyenTexteLibre || '',
            instrumentCode: l.instrumentCode || '',
            valeurNominale: hasNumeric ? mesurements.valeurNominale : null,
            toleranceSuperieure: hasNumeric ? mesurements.toleranceSuperieure : null,
            toleranceInferieure: hasNumeric ? mesurements.toleranceInferieure : null,
            unite: l.unite || '',
            limiteSpecTexte: !hasNumeric && l.limiteSpecTexte ? String(l.limiteSpecTexte).trim() : '',
            instruction: l.instruction || '',
            observations: l.observations || '',
            estCritique: l.estCritique || false,
            libelleAffiche: (l.libelleAffiche || nomCaract).trim(),
            imageBase64: l.imageBase64 || null,
            extraColonnes: l.valeursColonnesSpecifiques && Object.keys(l.valeursColonnesSpecifiques).length > 0 
              ? Object.entries(l.valeursColonnesSpecifiques).map(([key, val], idx) => ({
                  cleColonne: key,
                  valeurColonne: val,
                  ordreAffiche: idx + 1
                }))
              : []
          };
        })
      };
    });
  };

  return {
    normalizePlanData,
    cleanSectionName,
    mapModelDataToSections,
    syncDbIds,
    sanitizeMeasurements,
    buildServicePayload
  };
}
