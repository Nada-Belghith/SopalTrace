/**
 * sectionUtils.js — Source unique de vérité pour la logique des sections
 *
 * Utilisé par : AssPlanEditor, FabPlanEditor, FabModeleEditor, documentProduitFiniStore, assPlanStore
 *
 * Fonctions exportées :
 *   1. mapLigneForBackend(ligne, lIdx)          → ligne locale → payload backend
 *   2. mapSectionForBackend(section, idx, periodicites) → section locale → payload backend
 *   3. prepareSectionsForBackend(...)            → gère périodicités VARIABLE + appelle mapSectionForBackend
 *   4. hydrateSectionFromBackend(...)            → section DB → état local éditor
 *   5. mapImportedSection(...)                   → section Excel → état local éditor
 */

import { parseFrequenceLibelle } from '@/utils/frequencyUtils';

// ─────────────────────────────────────────────────────────────────────────────
// 1. mapLigneForBackend
// ─────────────────────────────────────────────────────────────────────────────
export function mapLigneForBackend(ligne, lIdx) {
  const extraColonnes = Object.entries(ligne.valeursColonnesSpecifiques || {}).map(([k, v], idx) => ({
    cleColonne: k,
    valeurColonne: v ? String(v) : null,
    ordreAffiche: idx + 1
  }));

  return {
    id: (ligne.id && String(ligne.id).trim() !== '') ? ligne.id : null,
    ordreAffiche: lIdx + 1,
    typeCaracteristiqueId: ligne.typeCaracteristiqueId || null,
    libelleAffiche: ligne.libelleAffiche || null,
    typeControleId: ligne.typeControleId || null,
    moyenControleId: ligne.moyenControleId || null,
    instrumentCode: ligne.instrumentCode || null,
    moyenTexteLibre: ligne.moyenTexteLibre || null,
    limiteSpecTexte: ligne.limiteSpecTexte || null,
    defauthequeId: ligne.defauthequeId || null,
    instruction: ligne.instruction || null,
    observations: ligne.observations || null,
    estCritique: ligne.estCritique || false,
    unite: ligne.unite || '',
    imageBase64: ligne.imageBase64 || null,
    extraColonnes
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// 2. mapSectionForBackend
// ─────────────────────────────────────────────────────────────────────────────
export function mapSectionForBackend(section, idx, periodicites = []) {
  const resolvedPeriodiciteId = section.periodiciteId || (() => {
    if (!section.frequenceLibelle) return null;
    const found = periodicites.find(p => {
      const pLib = (p.libelle || p.Libelle || '').toLowerCase().trim();
      const sLib = (section.frequenceLibelle || '').toLowerCase().trim();
      return pLib === sLib;
    });
    return found ? (found.id || found.Id) : null;
  })();

  return {
    id: (section.id && String(section.id).trim() !== '') ? section.id : null,
    ordreAffiche: idx + 1,
    typeSectionId: (section.typeSectionId && section.typeSectionId !== '') ? section.typeSectionId : null,
    libelleSection: section.libelleSection || section.nom || `Section ${idx + 1}`,
    periodiciteId: resolvedPeriodiciteId,
    regleEchantillonnageId: section.regleEchantillonnageId || null,
    regleEchantillonnageLibelle: section.regleEchantillonnageLibelle || null,
    frequenceLibelle: section.frequenceLibelle || section.regleEchantillonnageLibelle || null,
    notes: section.notes || null,
    lignes: (section.lignes || []).map((l, lIdx) => mapLigneForBackend(l, lIdx))
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// 3. prepareSectionsForBackend
// ─────────────────────────────────────────────────────────────────────────────
export async function prepareSectionsForBackend(sections, periodicites, createPeriodiciteCallback) {
  for (const g of sections) {
    if (g.modeFreq === 'VARIABLE' && !g.periodiciteId) {
      let libelleFreq = '';
      let codeFreq = '';
      const sP = g.freqNum > 1 ? 's' : '';

      if (g.typeVariable === 'HEURE') {
        const sH = g.freqHours > 1 ? 's' : '';
        libelleFreq = g.freqHours === 1
          ? `${g.freqNum} pièce${sP} / heure`
          : `${g.freqNum} pièce${sP} / ${g.freqHours} heure${sH}`;
        codeFreq = `${g.freqNum}P_${g.freqHours}H`;
      } else if (g.typeVariable === 'SERIE') {
        libelleFreq = `une série de ${g.freqNum} pièces`;
        codeFreq = `SERIE_${g.freqNum}P`;
      } else if (g.typeVariable === 'ECHANTILLON') {
        libelleFreq = `${g.freqNum} échantillon${sP}`;
        codeFreq = `ECH_${g.freqNum}`;
      }

      const perioExistante = (periodicites || []).find(
        p => (p.libelle || p.Libelle || '').toLowerCase() === libelleFreq.toLowerCase()
      );

      if (perioExistante) {
        g.periodiciteId = perioExistante.id || perioExistante.Id;
      } else if (createPeriodiciteCallback) {
        const payloadFreq = {
          code: codeFreq,
          libelle: libelleFreq,
          frequenceNum: g.freqNum,
          frequenceUnite: g.typeVariable === 'HEURE'
            ? `${g.freqHours}_HEURE`
            : (g.typeVariable === 'ECHANTILLON' ? 'ECHANTILLON' : 'SERIE'),
          ordreAffichage: 5
        };
        const res = await createPeriodiciteCallback(payloadFreq);
        g.periodiciteId = res.data?.data || res.data?.periodiciteId || res.data?.id || res.data;
      }
    }
  }

  return sections.map((g, idx) => mapSectionForBackend(g, idx, periodicites));
}

// ─────────────────────────────────────────────────────────────────────────────
// 4. hydrateSectionFromBackend
// ─────────────────────────────────────────────────────────────────────────────
export function hydrateSectionFromBackend(section, periodicites = [], reglesEchantillonnage = []) {
  const hydrated = { ...section, isFromDb: true };

  if (section.regleEchantillonnageId) {
    const regle = reglesEchantillonnage.find(r => (r.id || r.Id) === section.regleEchantillonnageId);
    const libelle = regle ? (regle.libelle || regle.Libelle) : (section.regleEchantillonnageLibelle || '');
    hydrated.modeFreq = 'FIXE';
    hydrated.frequenceLibelle = libelle;
    hydrated.regleEchantillonnageLibelle = libelle;
  } else if (section.periodiciteId) {
    const perio = periodicites.find(p => (p.id || p.Id) === section.periodiciteId);
    if (perio) {
      const parsed = parseFrequenceLibelle(perio.libelle || perio.Libelle || '', periodicites);
      Object.assign(hydrated, {
        modeFreq: 'VARIABLE',
        freqNum: parsed.freqNum || perio.frequenceNum || 1,
        freqHours: parsed.freqHours || 1,
        typeVariable: parsed.typeVariable || 'HEURE',
        frequenceLibelle: perio.libelle || perio.Libelle || ''
      });
    } else {
      hydrated.modeFreq = section.modeFreq || 'SANS';
    }
  } else if (section.typeSectionId) {
    hydrated.modeFreq = section.modeFreq || 'SANS';
    if (!section.periodiciteId && !section.regleEchantillonnageId && section.libelleSection?.includes('(')) {
      const match = section.libelleSection.match(/\(([^)]+)\)\s*$/);
      if (match) {
        const parsingResult = parseFrequenceLibelle(match[1].trim(), periodicites);
        if (parsingResult?.modeFreq && parsingResult.modeFreq !== 'SANS') Object.assign(hydrated, parsingResult);
      }
    }
  } else if (section.libelleSection?.includes('(') && !section.typeSectionId) {
    const extractedFrequence = section.libelleSection.split('(').pop()?.replace(')', '');
    if (extractedFrequence) Object.assign(hydrated, parseFrequenceLibelle(extractedFrequence, periodicites));
    hydrated.modeFreq = 'VARIABLE';
  } else {
    hydrated.modeFreq = section.modeFreq || 'SANS';
  }

  // Normaliser valeursColonnesSpecifiques depuis extraColonnes ou colonnesSupplementaires
  if (hydrated.lignes) {
    hydrated.lignes = hydrated.lignes
      .sort((a, b) => (a.ordreAffiche ?? 9999) - (b.ordreAffiche ?? 9999))
      .map(l => {
        const valeursColonnesSpecifiques = {};
        if (l.extraColonnes?.length > 0) {
          l.extraColonnes.forEach(ec => { valeursColonnesSpecifiques[ec.cleColonne] = ec.valeurColonne; });
        } else if (l.colonnesSupplementaires) {
          Object.assign(valeursColonnesSpecifiques, typeof l.colonnesSupplementaires === 'string'
            ? JSON.parse(l.colonnesSupplementaires) : l.colonnesSupplementaires);
        }
        return { ...l, isFromDb: true, valeursColonnesSpecifiques };
      });
  }

  return hydrated;
}

// ─────────────────────────────────────────────────────────────────────────────
// 5. mapImportedSection
// ─────────────────────────────────────────────────────────────────────────────
export function mapImportedSection(sec, reglesEchantillonnage = []) {
  let modeFreq = sec.modeFreq || 'SANS';
  let regleEchantillonnageId = sec.regleEchantillonnageId || null;
  let freqNum = sec.freqNum || 1;
  let typeVariable = sec.typeVariable || 'HEURE';
  let freqHours = sec.freqHours || 1;

  if (regleEchantillonnageId) {
    modeFreq = 'FIXE';
  } else if (sec.frequenceLibelle) {
    const perMatch = reglesEchantillonnage.find(p =>
      (p.libelle || p.Libelle || '').toLowerCase().trim() === (sec.frequenceLibelle || '').toLowerCase().trim()
    );
    if (perMatch) {
      modeFreq = 'FIXE';
      regleEchantillonnageId = perMatch.id || perMatch.Id;
    } else {
      modeFreq = 'VARIABLE';
      const libelle = sec.frequenceLibelle.toLowerCase();
      if (libelle.includes('pièce') && libelle.includes('heure')) {
        typeVariable = 'HEURE';
        const match = libelle.match(/(\d+)\s*pièce.*\/\s*(\d+)\s*heure/);
        if (match) { freqNum = parseInt(match[1]); freqHours = parseInt(match[2]); }
        else {
          const pieceMatch = libelle.match(/(\d+)\s*pièce/);
          if (pieceMatch) { freqNum = parseInt(pieceMatch[1]); freqHours = 1; }
        }
      } else if (libelle.includes('échantillon')) {
        typeVariable = 'ECHANTILLON';
        const match = libelle.match(/(\d+)\s*échantillon/);
        if (match) freqNum = parseInt(match[1]);
      } else if (libelle.includes('série')) {
        typeVariable = 'SERIE';
        const serieMatch = libelle.match(/série de (\d+) pièces/);
        if (serieMatch) freqNum = parseInt(serieMatch[1]);
      }
    }
  }

  return {
    id: sec.id || crypto.randomUUID(),
    isFromDb: false,
    nom: sec.nom || '',
    libelleSection: sec.nom || sec.libelleSection || '',
    typeSectionId: sec.typeSectionId || '',
    notes: sec.notes || '',
    modeFreq,
    periodiciteId: sec.periodiciteId || null,
    regleEchantillonnageId,
    regleEchantillonnageLibelle: regleEchantillonnageId
      ? (sec.regleEchantillonnageLibelle || sec.frequenceLibelle || '')
      : (sec.frequenceLibelle || ''),
    frequenceLibelle: sec.frequenceLibelle || '',
    freqNum,
    typeVariable,
    freqHours,
    lignes: (sec.lignes || []).map(lig => ({
      id: lig.id || crypto.randomUUID(),
      isFromDb: false,
      typeCaracteristiqueId: lig.typeCaracteristiqueId || null,
      typeControleId: lig.typeControleId || null,
      moyenControleId: lig.moyenControleId || null,
      instrumentCode: lig.instrumentCode || null,
      valeurNominale: lig.valeurNominale || null,
      toleranceSuperieure: lig.toleranceSuperieure || null,
      toleranceInferieure: lig.toleranceInferieure || null,
      unite: lig.unite || '',
      limiteSpecTexte: lig.limiteSpecTexte || '',
      observations: lig.observations || '',
      instruction: lig.instruction || '',
      estCritique: lig.estCritique || false,
      libelleAffiche: lig.libelleAffiche || '',
      imageBase64: lig.imageBase64 || null,
      valeursColonnesSpecifiques: lig.colonnesSupplementaires
        ? (typeof lig.colonnesSupplementaires === 'string'
          ? JSON.parse(lig.colonnesSupplementaires) : lig.colonnesSupplementaires)
        : (lig.valeursColonnesSpecifiques || {})
    }))
  };
}
