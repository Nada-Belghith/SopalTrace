import { libellePeriodicite } from '@/utils/frequencyUtils';

const DEFAULT_SECTION_TITLE = 'Caractéristiques à contrôler';

export function eqId(a, b) {
  if (!a || !b) return false;
  return String(a).trim().toLowerCase() === String(b).trim().toLowerCase();
}

export function findTypeSection(typesSection, typeSectionId) {
  if (!typeSectionId || !typesSection?.length) return null;
  return typesSection.find(t => eqId(t.id || t.Id, typeSectionId)) || null;
}

export function normalizeTypeSectionId(typeSectionId, typesSection) {
  const match = findTypeSection(typesSection, typeSectionId);
  return match ? (match.id || match.Id) : (typeSectionId || '');
}

export function isGenericSectionTitle(title) {
  if (!title) return true;
  const norm = title.trim().toLowerCase();
  return norm === '' || norm === 'section sans nom' || norm === DEFAULT_SECTION_TITLE.toLowerCase();
}

export function buildSectionTitleFromType(typeSection, defaultTitle = DEFAULT_SECTION_TITLE) {
  if (!typeSection?.libelle) return defaultTitle;
  const lib = typeSection.libelle.trim();
  if (lib.toLowerCase().includes('caractéristiques')) return lib;
  if (defaultTitle.toLowerCase().includes(lib.toLowerCase())) return defaultTitle;
  return `${defaultTitle} ${lib}`;
}

/**
 * Règle unique du titre d'une section (identique au serveur, LibellesSection.Construire) :
 * - section typée : « Caractéristiques à contrôler » + libellé du type (repris tel quel, parenthèses comprises)
 *   + « (périodicité) » si une périodicité est choisie ;
 * - section sans type : le titre libre (+ la périodicité si elle n'y figure pas déjà).
 */
export function titreSection(section, typesSection = [], periodicites = []) {
  const type = findTypeSection(typesSection, section?.typeSectionId);
  const libelleType = (type?.libelle || type?.Libelle || '').trim();
  const periodicite = libellePeriodiciteDeSection(section, periodicites)
    || periodiciteEnregistree(section, libelleType);

  if (libelleType) {
    return `${DEFAULT_SECTION_TITLE} ${libelleType}${periodicite ? ` (${periodicite})` : ''}`;
  }

  const libre = (section?.nom || section?.libelleSection || '').trim();
  if (!libre) return DEFAULT_SECTION_TITLE;
  return periodicite && !libre.toLowerCase().includes(periodicite.toLowerCase()) ? `${libre} (${periodicite})` : libre;
}

/** Périodicité de la section : choisie dans le référentiel, ou saisie (nombre de pièces / unité). */
function libellePeriodiciteDeSection(section, periodicites = []) {
  if (!section) return '';
  // L'identifiant fait foi (« Sans périodicité » le remet à null), quel que soit modeFreq.
  if (section.periodiciteId) {
    const p = (periodicites || []).find(x => eqId(x.id || x.Id, section.periodiciteId));
    if (p) return libellePeriodicite(p);
  }
  if (section.modeFreq === 'VARIABLE' && section.freqNum) {
    const unite = section.typeVariable || 'HEURE';
    return libellePeriodicite({ nbrPieces: section.freqNum, unite, nbrHeures: unite === 'HEURE' ? (section.freqHours || 1) : null });
  }
  return '';
}

/**
 * Périodicité absente du référentiel chargé (liste pas encore reçue, périodicité désactivée) : on reprend celle
 * du libellé enregistré par le serveur, « Caractéristiques à contrôler <type> (<périodicité>) ».
 */
function periodiciteEnregistree(section, libelleType) {
  if (!section?.periodiciteId || !libelleType) return '';
  const enregistre = (section.libelleSection || '').trim();
  const debut = `${DEFAULT_SECTION_TITLE} ${libelleType} (`;
  if (!enregistre.startsWith(debut) || !enregistre.endsWith(')')) return '';
  return enregistre.slice(debut.length, -1).trim();
}

/** Titre affiché d'une section (même règle que l'enregistrement). */
export function resolveSectionDisplayTitle(section, typesSection = [], periodicites = []) {
  return titreSection(section, typesSection, periodicites);
}

export function extractNomFromLibelle(libelleSection, typeSectionId, typesSection = [], defaultTitle = DEFAULT_SECTION_TITLE) {
  if (!libelleSection) return '';
  let clean = libelleSection.replace(/caractéristiques à contrôler/gi, '').trim();
  const typeSec = findTypeSection(typesSection, typeSectionId);
  if (typeSec?.libelle) {
    clean = clean.replace(new RegExp(typeSec.libelle.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&'), 'gi'), '').trim();
  }
  clean = clean.replace(/\s*\([^)]*(?:pièce|piece|heure|série|serie|échantillon|echantillon|100%)[^)]*\)\s*/gi, ' ').trim();
  clean = clean.replace(/^\(+|\)+$/g, '').trim();
  if (!clean || clean.toLowerCase() === defaultTitle.toLowerCase()) return '';
  return clean;
}

export function nettoyerNomSection(libelleSection, typeSectionId, typesSection = [], freqLib = '', regleLib = '') {
  if (!libelleSection) return '';
  const normalizeApostrophes = (s) => s.replace(/’/g, "'");
  let clean = normalizeApostrophes(libelleSection).replace(/caractéristiques à contrôler/gi, '').trim();

  const escapeRegExp = (str) => str.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&');

  if (typeSectionId) {
    const typeSec = findTypeSection(typesSection, typeSectionId);
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

  // On nettoie les parenthèses de fréquence (ex: (1 pièce / heure))
  clean = clean.replace(/\s*\([^)]*(?:pièce|piece|heure|série|serie|échantillon|echantillon|100%)[^)]*\)\s*/gi, ' ').trim();
  clean = clean.replace(/^\(+|\)+$/g, '').trim();

  return clean;
}
