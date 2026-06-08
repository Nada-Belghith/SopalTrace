// eslint-disable-next-line no-undef
const fs = require('fs');

let f = fs.readFileSync('src/components/ResultatControleCF/ResultatControleCfForm.vue', 'utf8');

const startTag = "<!-- Le conteneur en opacité réduite pour montrer que c'est un aperçu inactif -->";
const endTag = "    <!-- ═══════════════════════════════════════════";

const startIndex = f.indexOf(startTag);
// find the index of "3. SECTIONS IMPORTÉES" which is the next section
const sectionsImporteesIndex = f.indexOf("3. SECTIONS IMPORTÉES DEPUIS EXCEL", startIndex);
// find the end of that div which is just before the closing tag of the root div
const lastClosingDiv = f.lastIndexOf("  </div>\n</template>");

if(startIndex !== -1 && lastClosingDiv !== -1) {
  const newTemplate = `      <!-- Le conteneur en opacité réduite pour montrer que c'est un aperçu inactif -->
      <div class="pointer-events-none opacity-90">
        
        <template v-for="(sec, index) in store.sections" :key="index">
        
          <!-- APPROBATION -->
          <div v-if="sec.sectionType === 'APPROBATION'" class="border border-slate-200 rounded-lg overflow-hidden shadow-sm mb-6 bg-white">
            <table class="w-full text-left border-collapse">
              <thead class="bg-[#0f1923] text-white">
                <tr class="text-[11px] font-black uppercase tracking-widest">
                  <th class="p-3 w-[25%] border-r border-slate-700">Heure</th>
                  <th class="p-3 w-[25%] text-center border-r border-slate-700">Résultat</th>
                  <th class="p-3 w-[50%]">Signature (Matricule)</th>
                </tr>
              </thead>
              <tbody>
                <tr class="bg-[#f8f9fb] border-t border-slate-200">
                  <td colspan="3" class="p-3 px-4 pointer-events-auto">
                    <div class="flex flex-col gap-2">
                      <div class="flex items-center gap-3">
                        <span class="bg-blue-50 text-blue-700 text-xs font-black px-2.5 py-1.5 rounded border border-blue-200 uppercase tracking-widest shrink-0 shadow-sm">
                          SEC {{ index + 1 }}
                        </span>
                        <input type="text" v-model="sec.libelleAffiche" :disabled="isReadOnly"
                               class="bg-white border border-slate-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-100 rounded-md text-sm font-medium text-slate-700 outline-none w-64 px-3 py-1.5 shadow-sm transition-all"
                               placeholder="Nom de la section..." />
                      </div>
                    </div>
                  </td>
                </tr>
                <tr class="bg-white border-t border-slate-100">
                  <td colspan="3" class="p-8 text-center">
                    <div class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-slate-50 border border-dashed border-slate-300 text-slate-400 text-xs font-black uppercase tracking-widest italic">
                      <i class="pi pi-pencil"></i>
                      À remplir par l'opérateur lors de l'exécution
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- TRANCHES -->
          <div v-if="sec.sectionType === 'TRANCHES'" class="border border-slate-200 rounded-lg overflow-hidden shadow-sm mb-6 bg-white">
            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[900px]">
                <thead class="bg-[#0f1923] text-white">
                  <tr class="text-[11px] font-black uppercase tracking-widest">
                    <th rowspan="2" class="p-3 w-[12%] border-r border-slate-700 align-middle text-center">Fréquence</th>
                    <th colspan="2" class="p-2 text-center border-r border-b border-slate-700 w-[10%]">Résultats</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-700 align-middle">Non-conformité</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-700 align-middle">Actions de correction</th>
                    <th rowspan="2" class="p-3 w-[14%] align-middle text-center">Approbation</th>
                  </tr>
                  <tr class="text-[11px] font-black uppercase tracking-widest border-t border-slate-700">
                    <th class="p-2 text-center border-r border-slate-700 w-[5%]">C</th>
                    <th class="p-2 text-center border-r border-slate-700 w-[5%]">NC</th>
                  </tr>
                </thead>
                <tbody>
                  <tr class="bg-[#f8f9fb] border-b border-slate-200">
                    <td colspan="6" class="p-3 px-4 pointer-events-auto">
                      <div class="flex flex-col gap-2">
                        <div class="flex items-center gap-3">
                          <span class="bg-blue-50 text-blue-700 text-xs font-black px-2.5 py-1.5 rounded border border-blue-200 uppercase tracking-widest shrink-0 shadow-sm">
                            SEC {{ index + 1 }}
                          </span>
                          <input type="text" v-model="sec.libelleAffiche" :disabled="isReadOnly"
                                 class="bg-white border border-slate-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-100 rounded-md text-sm font-medium text-slate-700 outline-none w-64 px-3 py-1.5 shadow-sm transition-all"
                                 placeholder="Nom de la section..." />
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr class="bg-white border-t border-slate-100">
                    <td colspan="6" class="p-12 text-center">
                      <div class="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-slate-50 border border-dashed border-slate-300 text-slate-400 text-sm font-black uppercase tracking-widest italic">
                        <i class="pi pi-pencil text-lg"></i>
                        La grille horaire (24h) sera remplie par l'opérateur lors de l'exécution
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- REGLAGE / CUSTOM / LOT_POSTE -->
          <div v-if="sec.sectionType === 'REGLAGE' || sec.sectionType === 'CUSTOM' || sec.sectionType === 'LOT_POSTE'" class="border border-slate-200 rounded-lg overflow-hidden shadow-sm mb-6 bg-white">
            <div class="bg-[#f8f9fb] border-b border-slate-200 p-3 px-4 flex items-center justify-between pointer-events-auto">
              <div class="flex flex-col gap-2">
                <div class="flex items-center gap-3">
                  <span class="bg-blue-50 text-blue-700 text-xs font-black px-2.5 py-1.5 rounded border border-blue-200 uppercase tracking-widest shrink-0 shadow-sm">
                    SEC {{ index + 1 }}
                  </span>
                  <input type="text" v-model="sec.libelleAffiche" :disabled="isReadOnly"
                         class="bg-white border border-slate-300 focus:border-blue-500 focus:ring-1 focus:ring-blue-100 rounded-md text-sm font-medium text-slate-700 outline-none w-64 px-3 py-1.5 shadow-sm transition-all"
                         placeholder="Nom de la section..." />
                  <span v-if="sec.sectionType === 'REGLAGE'" class="text-[10px] text-teal-600 font-bold bg-teal-50 px-2 py-1 rounded border border-teal-200 uppercase">
                    <i class="pi pi-sliders-v mr-1"></i>
                    RÉGLAGE
                  </span>
                </div>
              </div>
              <button v-if="!isReadOnly && sec.sectionType !== 'REGLAGE'" @click="removeSection(sec)" class="text-slate-400 hover:text-red-500 transition-colors self-start mt-1">
                <i class="pi pi-trash text-base"></i>
              </button>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[800px]">
                <thead class="bg-slate-50 border-b border-slate-200">
                  <tr class="text-[10px] font-black uppercase tracking-widest text-slate-700">
                    <th class="p-3 border-r border-slate-200">Caractéristique Contrôlée</th>
                    <th class="p-3 border-r border-slate-200">Limite Spécif.</th>
                    <th class="p-3 border-r border-slate-200">Type de Contrôle</th>
                    <th class="p-3 border-r border-slate-200">Moyen de Contrôle</th>
                    <th class="p-3">Observations</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(ligne, lIdx) in sec.lignes" :key="lIdx" class="border-b border-slate-100 last:border-none hover:bg-slate-50/50">
                    <td class="p-3 border-r border-slate-100 text-xs font-semibold text-slate-800">{{ ligne.caracteristique || '-' }}</td>
                    <td class="p-3 border-r border-slate-100 text-xs text-slate-600">{{ ligne.limite || '-' }}</td>
                    <td class="p-3 border-r border-slate-100 text-xs text-slate-600">{{ ligne.typeControle || '-' }}</td>
                    <td class="p-3 border-r border-slate-100 text-xs text-slate-600">{{ ligne.moyenControle || '-' }}</td>
                    <td class="p-3 text-xs text-slate-500">{{ ligne.observations || '-' }}</td>
                  </tr>
                  <tr v-if="!sec.lignes || sec.lignes.length === 0">
                    <td colspan="5" class="p-4 text-center text-xs text-slate-400 italic">
                      <span v-if="sec.sectionType === 'REGLAGE'">Aucune caractéristique. Utilisez le bouton "Importer la structure Excel" en haut.</span>
                      <span v-else>Aucune caractéristique importée.</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>
      </div>
    </div>
`;
  
  let updated = f.substring(0, startIndex) + newTemplate + f.substring(lastClosingDiv);
  fs.writeFileSync('src/components/ResultatControleCF/ResultatControleCfForm.vue', updated, 'utf8');
  console.log("Successfully updated ResultatControleCfForm.vue template");
} else {
  console.log("Could not find start/end markers");
}
