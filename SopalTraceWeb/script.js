import fs from 'fs';
let c = fs.readFileSync('src/components/VerifMachine/VerifMachineForm.vue', 'utf8');

c = c.replace(
  /<!-- CUSTOM COLUMNS HEADER -->[\s\S]*?<\/th>/g,
  ''
);

c = c.replace(
  /<!-- CUSTOM COLUMNS -->[\s\S]*?<\/td>/g,
  ''
);

const headerCols = [
  { p: /Test de conformit[^]* \? 'Test de conformit[^]*' : 'Risque\/ D[^]*faut'[^]*[\s\S]*?<\/th>/g, k: 'risque' },
  { p: /Moyen\/ M[^]*thode de contr[^]*le'[^]*[\s\S]*?<\/th>/g, k: 'methode' },
  { p: /P[^]*riodicit[^]*[\s\S]*?<\/th>/g, k: 'periodicite' },
  { p: /Moyen de contr[^]*le' : 'Moyen de d[^]*tection'[\s\S]*?<\/th>/g, k: 'moyen_detection' },
  { p: /<th v-else[^>]*>[\s\S]*?Num[^]*ro de la pi[^]*ce r[^]*f[^]*rence'\) \}\}[\s\S]*?<\/th>/g, k: 'piece_reference' },
  { p: /Fuite [^]*talon' \}\}[\s\S]*?<\/th>/g, k: 'fuite_etalon' },
  { p: /Pression d'entr[^]*e affich[^]*e[\s\S]*?<\/th>/g, k: 'pression_entree' },
  { p: /P affich[^]*e[\s\S]*?<\/th>/g, k: 'dp_affichee' },
  { p: /R[^]*sultat[\s\S]*?<\/th>/g, k: 'resultat' },
  { p: /Observation en cas de non-conformit[^]*[\s\S]*?<\/th>/g, k: 'observation' }
];

headerCols.forEach(hc => {
  c = c.replace(hc.p, match => {
     return match + '\n<th v-for="cCol in getCustomColumnsAfter(\'' + hc.k + '\')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>';
  });
});

const bodyCols = [
  { p: /<td[^>]*>\{\{\s*ligne.libelleRisque\s*\}\}[\s\S]*?<\/td>/g, k: 'risque' },
  { p: /<td[^>]*>[\s\S]*?ligne.libelleRisque[\s\S]*?<\/td>/g, k: 'risque' },
  { p: /<td[^>]*>[\s\S]*?ligne.libelleMethode[\s\S]*?<\/td>/g, k: 'methode' },
  { p: /<td[^>]*>[\s\S]*?group.periodicite[\s\S]*?<\/td>/g, k: 'periodicite' },
  { p: /<td[^>]*>[\s\S]*?group.periodiciteId[\s\S]*?<\/td>/g, k: 'periodicite' },
  { p: /<td[^>]*>[\s\S]*?row.refMoyenDetectionId[\s\S]*?<\/td>/g, k: 'moyen_detection' },
  { p: /<td[^>]*>[\s\S]*?PRC[\s\S]*?<\/td>/g, k: 'piece_reference' },
  { p: /<td[^>]*>[\s\S]*?getFuiteValue[\s\S]*?<\/td>/g, k: 'fuite_etalon' },
  { p: /<td[^>]*Saisi par l'op[^]*rateur<\/td>\s*<td[^>]*Saisi par l'op[^]*rateur<\/td>\s*<td[^>]*C \/ NC<\/td>\s*<td[^>]*Saisi par l'op[^]*rateur<\/td>/g, k: 'blocks' }
];

const injectBlock = (match) => {
    let tds = match.match(/<td[^>]*>.*?<\/td>/g);
    if(tds && tds.length === 4) {
        return tds[0] + '\n<td v-for="cCol in getCustomColumnsAfter(\'pression_entree\')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20"><textarea v-if="!props.isReadOnly" v-model="(typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="3"></textarea><div v-else class="text-xs font-bold text-slate-800">{{ (typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key] || \'--\' }}</div></td>\n' + 
               tds[1] + '\n<td v-for="cCol in getCustomColumnsAfter(\'dp_affichee\')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20"><textarea v-if="!props.isReadOnly" v-model="(typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="3"></textarea><div v-else class="text-xs font-bold text-slate-800">{{ (typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key] || \'--\' }}</div></td>\n' + 
               tds[2] + '\n<td v-for="cCol in getCustomColumnsAfter(\'resultat\')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20"><textarea v-if="!props.isReadOnly" v-model="(typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="3"></textarea><div v-else class="text-xs font-bold text-slate-800">{{ (typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key] || \'--\' }}</div></td>\n' + 
               tds[3] + '\n<td v-for="cCol in getCustomColumnsAfter(\'observation\')" :key="cCol.key" :rowspan="(typeof rInfo !== \'undefined\') ? rInfo.rowspanRisque : getLigneTotalRows(ligne)" class="p-2 border-r border-slate-400 align-top bg-amber-50/20" v-if="(typeof rInfo === \'undefined\') || rInfo.isFirstRisque"><textarea v-if="!props.isReadOnly" v-model="(typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="3"></textarea><div v-else class="text-xs font-bold text-slate-800">{{ (typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key] || \'--\' }}</div></td>';
    }
    return match;
}

c = c.replace(/<td[^>]*Saisi par l'op[^]*rateur<\/td>\s*<td[^>]*Saisi par l'op[^]*rateur<\/td>\s*<td[^>]*C \/ NC<\/td>\s*<td[^>]*Saisi par l'op[^]*rateur<\/td>/g, injectBlock);

// For the rest of body cols, we insert just after them
const simpleBodyCols = [
  { p: /<td[^>]*>[\s\S]*?ligne.libelleRisque[\s\S]*?<\/td>/g, k: 'risque', rs: "(typeof rInfo !== 'undefined') ? rInfo.rowspanRisque : 1", cond: "v-if=\"(typeof rInfo === 'undefined') || rInfo.isFirstRisque\"" },
  { p: /<td[^>]*>[\s\S]*?ligne.libelleMethode[\s\S]*?<\/td>/g, k: 'methode', rs: "(typeof rInfo !== 'undefined') ? rInfo.rowspanMethode : getLigneTotalRows(ligne)", cond: "v-if=\"(typeof rInfo === 'undefined') || rInfo.isFirstMethode\"" },
  { p: /<td[^>]*>[\s\S]*?group.periodiciteId[\s\S]*?<\/td>/g, k: 'periodicite', rs: "(typeof rInfo !== 'undefined') ? rInfo.rowspanPerio : group.rows.length", cond: "v-if=\"(typeof rInfo === 'undefined') || rInfo.isFirstPerio\"" },
  { p: /<td[^>]*>[\s\S]*?row.refMoyenDetectionId[\s\S]*?<\/td>/g, k: 'moyen_detection' },
  { p: /<td[^>]*>[\s\S]*?getPieceValue[\s\S]*?<\/td>/g, k: 'piece_reference' },
  { p: /<td[^>]*>[\s\S]*?getFuiteValue[\s\S]*?<\/td>/g, k: 'fuite_etalon' }
];

simpleBodyCols.forEach(hc => {
  c = c.replace(hc.p, match => {
      if(match.includes('getCustomColumnsAfter')) return match; 
      
      let extra = hc.cond ? ` ${hc.cond}` : '';
      let rs = hc.rs ? ` :rowspan="${hc.rs}"` : '';
      return match + '\n<td v-for="cCol in getCustomColumnsAfter(\'' + hc.k + '\')" :key="cCol.key"' + rs + extra + ' class="p-2 border-r border-slate-400 align-top bg-amber-50/20"><textarea v-if="!props.isReadOnly" v-model="(typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="3"></textarea><div v-else class="text-xs font-bold text-slate-800">{{ (typeof rInfo !== \'undefined\' ? rInfo.ligne : ligne).valeursColonnesSpecifiques[cCol.key] || \'--\' }}</div></td>';
  });
});

fs.writeFileSync('src/components/VerifMachine/VerifMachineForm.vue', c);
console.log('Done script');
