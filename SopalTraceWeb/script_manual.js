import fs from 'fs';
let c = fs.readFileSync('src/components/VerifMachine/VerifMachineForm.vue', 'utf8');

if (!c.includes('getCustomColumnsAfter')) {
    c = c.replace(
        'const showColumnModal = ref(false)',
        'const showColumnModal = ref(false)\n\nconst getCustomColumnsAfter = (key) => customColumns.value.filter(col => col.insertAfter === key);\nconst getPreviewCustomColumnsAfter = (key) => previewColumns.value.filter(col => col.insertAfter === key && !vmBaseColumns.value.find(b => b.key === col.key));'
    );
}

// 1. Remove old custom column blocks
c = c.replace(/<!-- CUSTOM COLUMNS HEADER -->[\s\S]*?<\/th>/g, '');
c = c.replace(/<!-- CUSTOM COLUMNS -->[\s\S]*?<\/td>/g, '');
c = c.replace(/<!-- CUSTOM COLUMNS PREVIEW HEADER -->[\s\S]*?<\/th>/g, '');
c = c.replace(/<!-- CUSTOM COLUMNS PREVIEW BODY -->[\s\S]*?<\/td>/g, '');

const inject = (str, target, afterStr) => {
    let index = str.indexOf(target);
    if (index === -1) { console.error('Not found:', target.slice(0, 50)); return str; }
    let endTag = target.includes('</th') ? '</th>' : '</td>';
    let endIdx = str.indexOf(endTag, index);
    if (endIdx === -1) { console.error('End tag not found for:', target.slice(0, 50)); return str; }
    
    let splitPos = endIdx + endTag.length;
    return str.slice(0, splitPos) + '\n' + afterStr + str.slice(splitPos);
};

// --- HEADERS (Main) ---
let ccol = (key) => `<th v-for="cCol in getCustomColumnsAfter('${key}')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>`;

c = inject(c, `{{ (store.entete.afficheConformite && !isMachineSansConformite) ? 'Test de conformité' : 'Risque/ Défaut' }}`, ccol('risque'));
c = inject(c, `{{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}`, ccol('methode'));
c = inject(c, `<th :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">
                      Périodicité`, ccol('periodicite'));
c = inject(c, `{{ (isArchitectureA || isBEEMachine || isMAS19) ? 'Moyen de contrôle' : 'Moyen de détection' }}`, ccol('moyen_detection'));
c = inject(c, `<th v-else class="p-3 border-r border-slate-700 w-[15%]">
                      {{ isBEEMachine ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}`, ccol('piece_reference'));
c = inject(c, `{{ isBEEMachine ? 'Numéro du fuite étalon' : 'Fuite Étalon' }}`, ccol('fuite_etalon'));
c = inject(c, `Pression d'entrée affichée (en bar)`, ccol('pression_entree'));
c = inject(c, `?P affichée (en Pa)`, ccol('dp_affichee'));
c = inject(c, `Résultat\n                    </th>`, ccol('resultat'));
c = inject(c, `Observation en cas de non-conformité\n                    </th>`, ccol('observation'));

// --- BODY CONFORMITE ---
let bcol = (key, vIf, rowspan, model) => `<td ${vIf} v-for="cCol in getCustomColumnsAfter('${key}')" :key="cCol.key" :rowspan="${rowspan}" class="p-2 border-r border-slate-400 align-top bg-amber-50/20"><textarea v-if="!props.isReadOnly" v-model="${model}.valeursColonnesSpecifiques[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea><div v-else class="text-xs font-bold text-slate-800">{{ ${model}.valeursColonnesSpecifiques[cCol.key] || '--' }}</div></td>`;

c = inject(c, `{{ ligne.risqueDefaut }}`, bcol('risque', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));
c = inject(c, `{{ ligne.methodeControle }}`, bcol('methode', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));
c = inject(c, `{{ group.periodicite || '--' }}`, bcol('periodicite', 'v-if="rIdx === 0"', 'group.rows.length', 'ligne'));
c = inject(c, `{{ group.moyenDetection || '--' }}`, bcol('moyen_detection', 'v-if="rIdx === 0"', 'group.rows.length', 'ligne'));
c = inject(c, `<td v-else class="p-2 border-r border-slate-400 font-medium whitespace-pre-wrap min-w-32">\n                              {{ row.numeroMoyen || '--' }}\n                            </td>`, bcol('piece_reference', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));
c = inject(c, `{{ row.numeroFuiteEtalon || '--' }}\n                            </td>`, bcol('fuite_etalon', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));
c = inject(c, `{{ row.pressionEntree || '--' }}`, bcol('pression_entree', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));
c = inject(c, `{{ row.dpAffichee || '--' }}`, bcol('dp_affichee', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));
c = inject(c, `{{ row.resultat || '--' }}`, bcol('resultat', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));
c = inject(c, `<div v-else class="text-xs font-bold text-slate-800">{{ row.observation || '--' }}</div>\n                            </td>`, bcol('observation', 'v-if="gIdx === 0 && rIdx === 0"', 'getLigneTotalRows(ligne)', 'ligne'));

// --- BODY RISQUES ---
let bcol_r = (key, vIf, rowspan, model) => `<td ${vIf} v-for="cCol in getCustomColumnsAfter('${key}')" :key="cCol.key" :rowspan="${rowspan}" class="p-2 border-r border-slate-400 align-top bg-amber-50/20"><textarea v-if="!props.isReadOnly" v-model="${model}.valeursColonnesSpecifiques[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea><div v-else class="text-xs font-bold text-slate-800">{{ ${model}.valeursColonnesSpecifiques[cCol.key] || '--' }}</div></td>`;

c = inject(c, `{{ rInfo.ligne.risqueDefaut }}`, bcol_r('risque', 'v-if="rInfo.isFirstRisque"', 'rInfo.rowspanRisque', 'rInfo.ligne'));
c = inject(c, `{{ rInfo.ligne.methodeControle }}`, bcol_r('methode', 'v-if="rInfo.isFirstRisque"', 'rInfo.rowspanRisque', 'rInfo.ligne'));

// --- PREVIEW HEADER ---
let phcol = (key) => `<th v-for="cCol in getPreviewCustomColumnsAfter('${key}')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>`;
// wait preview header uses <td> for the bottom rows! But my previous block had <th>. 
// Ah wait! Preview header uses <td>!
c = inject(c, `<td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[18%]">Risque/ Défaut</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('risque')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">{{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('methode')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td :rowspan="hasFamilleHeaders ? 2 : 1" :colspan="(isMAS26 && store.entete.afficheMoyenDetectionRisques) ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">Périodicité</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('periodicite')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td v-if="store.entete.afficheMoyenDetectionRisques && !isMAS26" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">{{ (isArchitectureA || isBEEMachine || isMAS19) ? 'Moyen de contrôle' : 'Moyen de détection' }}</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('moyen_detection')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td v-else class="p-3 border-r border-slate-700 w-[15%]">\n                        {{ (isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce de référence' }}\n                    </td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('piece_reference')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%]">Fuite Étalon</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('fuite_etalon')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">Pression d'entrée affichée (en bar)</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('pression_entree')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">?P affichée (en Pa)</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('dp_affichee')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[5%] text-[10px]">Résultat</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('resultat')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);
c = inject(c, `<td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%] text-[10px]">Observation en cas de non-conformité</td>`, `<td v-for="cCol in getPreviewCustomColumnsAfter('observation')" :key="cCol.key" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>`);

// --- PREVIEW BODY CONFORMITE ---
const injectLast = (str, target, afterStr) => {
    let index = str.lastIndexOf(target);
    if (index === -1) { console.error('Not found:', target.slice(0, 50)); return str; }
    let endTag = target.includes('</th') ? '</th>' : '</td>';
    let endIdx = str.indexOf(endTag, index);
    let splitPos = endIdx + endTag.length;
    return str.slice(0, splitPos) + '\n' + afterStr + str.slice(splitPos);
};

let pbcol = (key) => `<td v-for="cCol in getPreviewCustomColumnsAfter('${key}')" :key="cCol.key" class="p-3 border-r border-slate-100 bg-amber-50"><span class="text-amber-600 font-medium px-2 py-1 rounded border border-amber-200">Saisi...</span></td>`;

c = injectLast(c, `<td class="p-3 border-r border-slate-100 italic text-slate-400">Saisi...</td>`, pbcol('observation')); 
c = injectLast(c, `<td class="p-3 border-r border-slate-100 font-bold">C / NC</td>`, pbcol('resultat')); 
c = injectLast(c, `<td class="p-3 border-r border-slate-100 italic text-slate-400">Saisi...</td>`, pbcol('dp_affichee')); 
c = injectLast(c, `<td class="p-3 border-r border-slate-100 italic text-slate-400">Saisi...</td>`, pbcol('pression_entree'));
c = injectLast(c, `<td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-3 border-r border-slate-100">FE456</td>`, pbcol('fuite_etalon'));
c = injectLast(c, `<td v-else class="p-3 border-r border-slate-100">123</td>`, pbcol('piece_reference'));
c = injectLast(c, `<td v-if="store.entete.afficheMoyenDetectionRisques" class="p-3 border-r border-slate-100">-</td>`, pbcol('moyen_detection'));
c = injectLast(c, `<td class="p-3 border-r border-slate-100">1 / équipe</td>`, pbcol('periodicite'));
c = injectLast(c, `<td class="p-3 border-r border-slate-100">Visuel</td>`, pbcol('methode'));
c = injectLast(c, `<td class="p-3 border-r border-slate-100">Test...</td>`, pbcol('risque'));

// --- PREVIEW BODY RISQUES ---
// Because we already injected at `lastIndexOf`, they are now processed! 
// WAIT! There is a SECOND matching block for Risques. `lastIndexOf` starts from bottom, so it processes Risques!
// To process Conformité, we have to call it AGAIN!
let pbcol_r = (key) => `<td v-for="cCol in getPreviewCustomColumnsAfter('${key}')" :key="cCol.key" class="p-3 border-r border-slate-100 bg-amber-50"><span class="text-amber-600 font-medium px-2 py-1 rounded border border-amber-200">Saisi...</span></td>`;

c = injectLast(c, `<td class="p-3 border-r border-slate-100 italic text-slate-400">Saisi...</td>`, pbcol_r('observation')); 
c = injectLast(c, `<td class="p-3 border-r border-slate-100 font-bold">C / NC</td>`, pbcol_r('resultat')); 
c = injectLast(c, `<td class="p-3 border-r border-slate-100 italic text-slate-400">Saisi...</td>`, pbcol_r('dp_affichee')); 
c = injectLast(c, `<td class="p-3 border-r border-slate-100 italic text-slate-400">Saisi...</td>`, pbcol_r('pression_entree'));
c = injectLast(c, `<td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-3 border-r border-slate-100">FE456</td>`, pbcol_r('fuite_etalon'));
c = injectLast(c, `<td v-else class="p-3 border-r border-slate-100">123</td>`, pbcol_r('piece_reference'));
c = injectLast(c, `<td v-if="store.entete.afficheMoyenDetectionRisques" class="p-3 border-r border-slate-100">-</td>`, pbcol_r('moyen_detection'));
c = injectLast(c, `<td class="p-3 border-r border-slate-100">1 / équipe</td>`, pbcol_r('periodicite'));
c = injectLast(c, `<td class="p-3 border-r border-slate-100">Visuel</td>`, pbcol_r('methode'));
c = injectLast(c, `<td class="p-3 border-r border-slate-100">Fissure</td>`, pbcol_r('risque')); // Fissure is the Risque specific one!


fs.writeFileSync('src/components/VerifMachine/VerifMachineForm.vue', c);
console.log('Script ran perfectly!');
