import fs from 'fs';
let c = fs.readFileSync('src/components/VerifMachine/VerifMachineForm.vue', 'utf8');

c = c.replace(
  /<!-- CUSTOM COLUMNS PREVIEW BODY -->[\s\S]*?<\/td>/g,
  ''
);

const previewBodyCols = [
  { p: /<td[^>]*>Test\.\.\.<\/td>/g, k: 'risque' },
  { p: /<td[^>]*>Visuel<\/td>/g, k: 'methode' },
  { p: /<td[^>]*>1 \/ [^\s]*quipe<\/td>/g, k: 'periodicite' },
  { p: /<td[^>]*>-<\/td>/g, k: 'moyen_detection' },
  { p: /<td v-else[^>]*>123<\/td>/g, k: 'piece_reference' },
  { p: /<td[^>]*>FE456<\/td>/g, k: 'fuite_etalon' },
  { p: /<td[^>]*>Saisi\.\.\.<\/td>\s*<td[^>]*>Saisi\.\.\.<\/td>\s*<td[^>]*>C \/ NC<\/td>\s*<td[^>]*>Saisi\.\.\.<\/td>/g, k: 'observation' }, 
  { p: /<td[^>]*>Fissure<\/td>/g, k: 'risque' }
];

// For the preview, they don't have v-if or rowspan for rows since it's just 1 line per section
previewBodyCols.forEach(bc => {
  if (bc.k === 'observation') {
      // observation includes the 4 cells in the regex to distinguish. 
      // It's the last one.
      c = c.replace(bc.p, match => {
          return match + '\n<td v-for="cCol in getPreviewCustomColumnsAfter(\'observation\')" :key="cCol.key" class="p-3 border-r border-slate-100 bg-amber-50"><span class="text-amber-600 font-medium px-2 py-1 rounded border border-amber-200">Saisi...</span></td>';
      });
      return;
  }
  
  c = c.replace(bc.p, match => {
     return match + '\n<td v-for="cCol in getPreviewCustomColumnsAfter(\'' + bc.k + '\')" :key="cCol.key" class="p-3 border-r border-slate-100 bg-amber-50"><span class="text-amber-600 font-medium px-2 py-1 rounded border border-amber-200">Saisi...</span></td>';
  });
});

// Since pression_entree, dp_affichee, resultat, observation are hard to regex individually without matching multiple, 
// let's do it safely by matching the exact sequence of 4 cells at the end of the preview row.
// Wait, my regex for 'observation' above matched the whole group of 4 cells and appended after it.
// To insert after pression_entree, dp, resultat:
// We can just regex the specific sequence.
c = c.replace(
  /<td[^>]*>Saisi\.\.\.<\/td>\s*<td[^>]*>Saisi\.\.\.<\/td>\s*<td[^>]*>C \/ NC<\/td>\s*<td[^>]*>Saisi\.\.\.<\/td>/g,
  match => {
      // The match contains:
      // <td ...>Saisi...</td> (Pression)
      // <td ...>Saisi...</td> (DP)
      // <td ...>C / NC</td> (Resultat)
      // <td ...>Saisi...</td> (Observation)
      let parts = match.split('</td>');
      // parts[0] + '</td>' is Pression
      // parts[1] + '</td>' is DP
      // parts[2] + '</td>' is Resultat
      // parts[3] + '</td>' is Observation
      return parts[0] + '</td>\n<td v-for="cCol in getPreviewCustomColumnsAfter(\'pression_entree\')" :key="cCol.key" class="p-3 border-r border-slate-100 bg-amber-50"><span class="text-amber-600 font-medium px-2 py-1 rounded border border-amber-200">Saisi...</span></td>' +
             parts[1] + '</td>\n<td v-for="cCol in getPreviewCustomColumnsAfter(\'dp_affichee\')" :key="cCol.key" class="p-3 border-r border-slate-100 bg-amber-50"><span class="text-amber-600 font-medium px-2 py-1 rounded border border-amber-200">Saisi...</span></td>' +
             parts[2] + '</td>\n<td v-for="cCol in getPreviewCustomColumnsAfter(\'resultat\')" :key="cCol.key" class="p-3 border-r border-slate-100 bg-amber-50"><span class="text-amber-600 font-medium px-2 py-1 rounded border border-amber-200">Saisi...</span></td>' +
             parts[3] + '</td>'; // observation already handled above! wait, if I run this regex AFTER the observation one, the observation one already appended its v-for.
             // Actually, I just run this instead of the observation one.
  }
);


fs.writeFileSync('src/components/VerifMachine/VerifMachineForm.vue', c);
console.log('Done preview');
