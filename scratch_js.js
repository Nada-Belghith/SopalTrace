const familles = [
  { id: '1', libelle: 'FAMILLE CORPS (23)' },
  { id: '2', libelle: 'FAMILLE CORPS (30, 35)' },
  { id: '3', libelle: 'FAMILLE CORPS (49)' },
  { id: '4', libelle: 'FAMILLE CORPS (40, 43, 44)' }
];

const testCases = [
  "Famille Corps (30, 35)",
  "Famille Corps (23)",
  "Famille Corps (40, 43, 44)",
  "Famille Corps (49)"
];

testCases.forEach(mpFamilleCode => {
  let correctedFamCode = mpFamilleCode
    .replace('2580A01', '25B0A01')
    .replace('25AUA01', '25UA01');
  
  const normMpFam = correctedFamCode.replace(/\s+/g, '').toUpperCase();
  console.log(`normMpFam: ${normMpFam}`);
  const importedFam = familles.find(f => {
    const normFLib = f.libelle?.replace(/\s+/g, '').toUpperCase() || '';
    return normFLib === normMpFam || correctedFamCode.includes(`(${f.libelle})`);
  });
  
  if (importedFam) {
    console.log(`Match for ${mpFamilleCode}: ${importedFam.libelle}`);
  } else {
    console.log(`NO MATCH for ${mpFamilleCode}`);
  }
});
