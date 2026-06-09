const DEFAULT_SECTION_TITLE = 'Caractéristiques à contrôler';

export function eqId(a, b) {
  if (!a || !b) return false;
  return String(a).trim().toLowerCase() === String(b).trim().toLowerCase();
}

export function findTypeSection(typesSection, typeSectionId) {
  if (!typeSectionId || !typesSection?.length) return null;
  return typesSection.find(t => eqId(t.id, typeSectionId)) || null;
}

export function normalizeTypeSectionId(typeSectionId, typesSection) {
  const match = findTypeSection(typesSection, typeSectionId);
  return match?.id || typeSectionId || '';
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

export function resolveSectionDisplayTitle(section, typesSection = [], defaultTitle = DEFAULT_SECTION_TITLE) {
  const typeSec = findTypeSection(typesSection, section?.typeSectionId);
  let titre = (section?.libelleSection || section?.nom || '').trim();

  if (isGenericSectionTitle(titre) && typeSec) {
    titre = buildSectionTitleFromType(typeSec, defaultTitle);
  } else if ((!titre || titre.toLowerCase() === 'section sans nom') && typeSec) {
    titre = buildSectionTitleFromType(typeSec, defaultTitle);
  }

  return titre || defaultTitle;
}

export function extractNomFromLibelle(libelleSection, typeSectionId, typesSection = [], defaultTitle = DEFAULT_SECTION_TITLE) {
  if (!libelleSection) return '';
  let clean = libelleSection.replace(/caractéristiques à contrôler/gi, '').trim();
  const typeSec = findTypeSection(typesSection, typeSectionId);
  if (typeSec?.libelle) {
    clean = clean.replace(new RegExp(typeSec.libelle.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&'), 'gi'), '').trim();
  }
  clean = clean.replace(/^\(+|\)+$/g, '').trim();
  if (!clean || clean.toLowerCase() === defaultTitle.toLowerCase()) return '';
  return clean;
}
