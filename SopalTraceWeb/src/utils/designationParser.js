/**
 * Extract common plan fields from a plan designation.
 * This logic handles Fabrication, Assemblage, Produit Fini, and Verif Machine.
 * 
 * @param {string} designation - The designation of the plan (e.g. "Plan PF RBGFA_SOUPAPE PAS71")
 * @param {Array} validFamilies - Array of family objects { code, libelle, ... }
 * @param {Array} validMachines - Array of machine objects { code, libelle, ... }
 * @param {Array} validPostes - Array of poste objects { code, libelle, ... }
 * @returns {Object} Extracted fields: familleCode, natureComposantCode, operationCode, posteCode, machineCode
 */
export function parseDesignation(designation, validFamilies = [], validMachines = [], validPostes = []) {
  if (!designation) return {};

  const d = designation.toUpperCase();
  const result = {
    familleCode: '',
    natureComposantCode: '',
    operationCode: '',
    posteCode: '',
    machineCode: ''
  };

  // 1. Famille
  // Sort by longest code first to avoid partial matches
  const sortedFamilies = [...validFamilies].sort((a, b) => {
    const codeA = (a.code || '').length;
    const codeB = (b.code || '').length;
    return codeB - codeA; 
  });
  
  const foundFamily = sortedFamilies.find(f => {
    const code = (f.code || '').toUpperCase();
    return code && d.includes(code);
  });

  if (foundFamily) {
    result.familleCode = foundFamily.code;
  } else if (d.includes('MANU') || d.includes('MANUELLE')) {
    const manuFamily = sortedFamilies.find(f => f.code === 'RBGFM');
    if (manuFamily) result.familleCode = manuFamily.code;
  }

  // 2. Article (natureComposantCode)
  if (d.includes('PISTON')) result.natureComposantCode = 'PISTON';
  else if (d.includes('PF') || d.includes('RBGFA-BAC') || d.includes('RBGFA_SOUPAPE')) result.natureComposantCode = 'PF';
  else if (d.includes('VOLANT')) result.natureComposantCode = 'VOLANT';
  else if (d.includes('CORPS')) result.natureComposantCode = 'CORPS';

  // 3. Opération
  if (d.includes('ASS') || d.includes('ASSEMBLAGE') || d.includes('PF')) {
    result.operationCode = 'ASS';
  } else if (d.includes('USINAGE') || d.includes('FAB')) {
    result.operationCode = 'USI'; // example
  }

  // 4. Poste
  // Check if any valid poste code is in the designation
  const sortedPostes = [...validPostes].sort((a, b) => {
    const codeA = (a.code || '').length;
    const codeB = (b.code || '').length;
    return codeB - codeA; 
  });

  const foundPoste = sortedPostes.find(p => {
    const code = (p.code || '').toUpperCase();
    // Use regex to ensure we match whole words or specific formats to avoid matching random numbers like "02" inside "2023"
    // But since codes like "PAS71" or "02" can be tricky, if it's PASxx we can just match it.
    // If it's a short number like "02", it should be surrounded by spaces, dash, or at the end.
    if (!code) return false;
    const regex = new RegExp(`(?:^|[^a-zA-Z0-9])${code}(?:[^a-zA-Z0-9]|$)`);
    return regex.test(d);
  });

  if (foundPoste) {
    result.posteCode = foundPoste.code;
  } else {
    // Fallback to regex for PASxx if not in validPostes (just in case)
    const posteMatch = d.match(/PAS\d+/);
    if (posteMatch) {
      result.posteCode = posteMatch[0];
    }
  }

  // 5. Machine (Verif Machine)
  const sortedMachines = [...validMachines].sort((a, b) => {
    const codeA = (a.code || '').length;
    const codeB = (b.code || '').length;
    return codeB - codeA; 
  });

  const foundMachine = sortedMachines.find(m => {
    const code = (m.code || '').toUpperCase();
    return code && d.includes(code);
  });

  if (foundMachine) {
    result.machineCode = foundMachine.code;
  }

  return result;
}
