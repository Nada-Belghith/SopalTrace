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
  } else if (typeSec && typeSec.libelle) {
    // Si le titre ne contient pas le libellé du type, on l'injecte intelligemment
    const libTypeNorm = typeSec.libelle.toLowerCase().trim();
    const titreNorm = titre.toLowerCase();
    
    if (libTypeNorm && !titreNorm.includes(libTypeNorm)) {
      // Pour éviter de doubler "Caractéristiques à contrôler", on nettoie le début du titre
      let cleanTitre = titre;
      if (titreNorm.startsWith(defaultTitle.toLowerCase())) {
         cleanTitre = titre.substring(defaultTitle.length).trim();
      }
      
      const prefix = buildSectionTitleFromType(typeSec, defaultTitle);
      titre = cleanTitre ? `${prefix} ${cleanTitre}` : prefix;
    }
  }

  // Auto-nettoyage des fréquences dupliquées (bug précédent) dans le libellé pour l'affichage
  if (titre) {
      titre = titre.replace(/(?:\s*\([^)]*\)\s*)+$/, '').trim();
      
      // Ré-attacher la bonne fréquence si elle est dans la section
      if (section?.frequenceLibelle) {
          const freqNorm = section.frequenceLibelle.toLowerCase();
          if (!titre.toLowerCase().includes(freqNorm)) {
              titre = `${titre} (${section.frequenceLibelle})`;
          }
      }
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

  // Remove trailing empty parentheses and trim
  clean = clean.replace(/(?:\s*\([^)]*\)\s*)+$/, '').trim();
  clean = clean.replace(/^\(+|\)+$/g, '').trim();

  return clean;
}
