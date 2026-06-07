/* eslint-env node */
const fs = require('fs');

let f = fs.readFileSync('src/components/Execution/ExecEncfForm.vue', 'utf8');

const startTag = "      <!-- ─── SECTION 1 : APPROBATION PIÈCES TYPES ─── -->";
const endTag = "    </template>";

const startIndex = f.indexOf(startTag);
const lastClosingTemplate = f.lastIndexOf("    </template>");

if(startIndex !== -1 && lastClosingTemplate !== -1) {
  const newTemplate = `      <template v-if="store.planSourceData && store.planSourceData.sections">
        <template v-for="(sec, index) in store.planSourceData.sections" :key="index">
          
          <!-- ─── APPROBATION PIÈCES TYPES ─── -->
          <section v-if="sec.sectionType === 'APPROBATION'" class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden mb-6">
            <div class="bg-slate-800 px-4 py-3 flex items-center justify-between">
              <div class="flex items-center gap-3">
                <span class="bg-emerald-500/20 text-emerald-300 text-[10px] font-black px-2 py-1 rounded border border-emerald-500/30 uppercase tracking-widest">
                  SEC {{ index + 1 }}
                </span>
                <h3 class="text-sm font-bold text-slate-100 uppercase tracking-wide">{{ sec.libelleAffiche || 'Approbation des pièces types' }}</h3>
              </div>
              <button @click="addPieceType"
                      class="text-emerald-400 hover:text-emerald-300 text-[11px] font-black uppercase tracking-widest flex items-center gap-1 transition-colors">
                <i class="pi pi-plus text-xs"></i> Ajouter ligne
              </button>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse">
                <thead class="bg-slate-50 border-b border-slate-200">
                  <tr class="text-[11px] font-black text-slate-500 uppercase tracking-wider">
                    <th class="p-3 w-[20%] border-r border-slate-200">Heure</th>
                    <th class="p-3 w-[20%] text-center border-r border-slate-200">Résultat</th>
                    <th class="p-3 w-[30%] border-r border-slate-200">Signature (Matricule)</th>
                    <th class="p-3">Remarque</th>
                    <th class="p-3 w-10 text-center"></th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr v-for="(pt, idx) in piecesTypesDisplay" :key="idx"
                      class="hover:bg-slate-50/60 transition-colors group">
                    <td class="p-2 border-r border-slate-100">
                      <input type="text" v-model="pt.heureValidation"
                             placeholder="ex: 10:00"
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1.5 text-sm outline-none focus:bg-white transition-all" />
                    </td>
                    <td class="p-2 border-r border-slate-100 text-center">
                      <div class="flex items-center justify-center gap-2">
                        <button @click="pt.resultat = pt.resultat === 'C' ? null : 'C'"
                                :class="pt.resultat === 'C'
                                  ? 'bg-emerald-100 text-emerald-700 border-emerald-300 font-black'
                                  : 'bg-white text-slate-400 border-slate-200 hover:border-emerald-300 hover:text-emerald-600'"
                                class="w-10 h-8 rounded border text-xs font-bold transition-all">C</button>
                        <button @click="pt.resultat = pt.resultat === 'NC' ? null : 'NC'"
                                :class="pt.resultat === 'NC'
                                  ? 'bg-red-100 text-red-700 border-red-300 font-black'
                                  : 'bg-white text-slate-400 border-slate-200 hover:border-red-300 hover:text-red-500'"
                                class="w-10 h-8 rounded border text-xs font-bold transition-all">NC</button>
                      </div>
                    </td>
                    <td class="p-2 border-r border-slate-100">
                      <input type="text" v-model="pt.matriculeOperateur"
                             placeholder="Matricule..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1.5 text-sm outline-none focus:bg-white transition-all" />
                    </td>
                    <td class="p-2">
                      <input type="text" v-model="pt.remarque"
                             placeholder="Observation..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1.5 text-sm outline-none focus:bg-white transition-all" />
                    </td>
                    <td class="p-2 text-center">
                      <button v-if="pt._isReal" @click="removePieceType(idx)"
                              class="opacity-0 group-hover:opacity-100 text-slate-300 hover:text-red-500 transition-all p-1 rounded hover:bg-red-50">
                        <i class="pi pi-trash text-xs"></i>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="px-4 py-2 bg-slate-50 border-t border-slate-100">
              <p class="text-[10px] text-slate-400 italic">
                NB: L'approbation de la pièce type se fait : au démarrage série, après intervention maintenance et après arrêt pour réglage.
              </p>
            </div>
          </section>

          <!-- ─── REGLAGE (Caractéristiques à contrôler aux réglages) ─── -->
          <section v-if="sec.sectionType === 'REGLAGE' || sec.sectionType === 'CUSTOM' || sec.sectionType === 'LOT_POSTE'" class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden mb-6">
            <div class="bg-slate-800 px-4 py-3 flex items-center gap-3">
              <span class="bg-emerald-500/20 text-emerald-300 text-[10px] font-black px-2 py-1 rounded border border-emerald-500/30 uppercase tracking-widest">
                SEC {{ index + 1 }}
              </span>
              <h3 class="text-sm font-bold text-slate-100 uppercase tracking-wide">{{ sec.libelleAffiche || 'Caractéristiques à contrôler' }}</h3>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse">
                <thead class="bg-slate-50 border-b border-slate-200 sticky top-0 z-10">
                  <tr class="text-[11px] font-black text-slate-500 uppercase tracking-wider">
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-200 align-middle">Caractéristiques contrôlées</th>
                    <th colspan="2" class="p-2 text-center border-r border-b border-slate-200 w-[10%]">Résultat</th>
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-200 align-middle">Non-conformité</th>
                    <th rowspan="2" class="p-3 w-[25%] border-r border-slate-200 align-middle">Actions de correction</th>
                    <th rowspan="2" class="p-3 w-[15%] align-middle text-center">Approbation Matricule</th>
                  </tr>
                  <tr class="text-[11px] font-black uppercase tracking-wider border-b border-slate-200">
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-emerald-50 text-emerald-700">C</th>
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-red-50 text-red-600">NC</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr v-for="(ligne, lIdx) in sec.lignes" :key="lIdx"
                      :class="[
                        'transition-colors group hover:bg-slate-50/50'
                      ]">
                    <!-- Caractéristiques contrôlées -->
                    <td class="p-3 border-r border-slate-100">
                      <span class="text-xs font-semibold text-slate-800">{{ ligne.caracteristique }}</span>
                    </td>

                    <!-- Case C -->
                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setReglageResultat(ligne.id, 'C')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             getReglageResultat(ligne.id) === 'C'
                               ? 'bg-emerald-500 border-emerald-500 shadow-sm'
                               : 'border-slate-200 hover:border-emerald-400 bg-white'
                           ]">
                        <i v-if="getReglageResultat(ligne.id) === 'C'" class="pi pi-check text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <!-- Case NC -->
                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setReglageResultat(ligne.id, 'NC')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             getReglageResultat(ligne.id) === 'NC'
                               ? 'bg-red-500 border-red-500 shadow-sm'
                               : 'border-slate-200 hover:border-red-400 bg-white'
                           ]">
                        <i v-if="getReglageResultat(ligne.id) === 'NC'" class="pi pi-times text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <!-- Non-conformité -->
                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="getReglageData(ligne.id).nonConformite"
                                rows="1"
                                placeholder="Détails NC..."
                                class="w-full bg-transparent border border-transparent focus:border-red-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"
                                :disabled="getReglageResultat(ligne.id) !== 'NC'"></textarea>
                    </td>

                    <!-- Actions de correction -->
                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="getReglageData(ligne.id).actionsCorrection"
                                rows="1"
                                placeholder="Actions..."
                                class="w-full bg-transparent border border-transparent focus:border-emerald-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"></textarea>
                    </td>

                    <!-- Approbation Matricule -->
                    <td class="p-1.5">
                      <input type="text" v-model="getReglageData(ligne.id).approbationMatricule"
                             placeholder="Matricule..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1 text-xs text-center font-bold text-slate-700 outline-none focus:bg-white transition-all" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- ─── CONTRÔLE EN COURS DE FABRICATION (Tranches) ─── -->
          <section v-if="sec.sectionType === 'TRANCHES'" class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden mb-6">
            <div class="bg-slate-800 px-4 py-3 flex items-center gap-3">
              <span class="bg-emerald-500/20 text-emerald-300 text-[10px] font-black px-2 py-1 rounded border border-emerald-500/30 uppercase tracking-widest">
                SEC {{ index + 1 }}
              </span>
              <h3 class="text-sm font-bold text-slate-100 uppercase tracking-wide">{{ sec.libelleAffiche || 'Contrôle en cours de fabrication' }}</h3>
            </div>

            <div class="overflow-x-auto">
              <table class="w-full text-left border-collapse min-w-[900px]">
                <thead class="bg-slate-50 border-b border-slate-200 sticky top-0 z-10">
                  <tr class="text-[11px] font-black text-slate-500 uppercase tracking-wider">
                    <th rowspan="2" class="p-3 w-[12%] border-r border-slate-200 align-middle text-center">Fréquence</th>
                    <th colspan="2" class="p-2 text-center border-r border-b border-slate-200 w-[10%]">Résultats</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-200 align-middle">Non-conformité</th>
                    <th rowspan="2" class="p-3 w-[26%] border-r border-slate-200 align-middle">Actions de correction</th>
                    <th rowspan="2" class="p-3 w-[14%] align-middle text-center">Approbation</th>
                  </tr>
                  <tr class="text-[11px] font-black uppercase tracking-wider border-b border-slate-200">
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-emerald-50 text-emerald-700">C</th>
                    <th class="p-2 text-center border-r border-slate-200 w-[5%] bg-red-50 text-red-600">NC</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr v-for="(tranche, idx) in store.execData.tranches" :key="idx"
                      :class="[
                        'transition-colors group',
                        tranche.resultatFinal === 'NC' ? 'bg-red-50/40' : 'hover:bg-slate-50/50'
                      ]">
                    <td class="p-2 border-r border-slate-100 text-center">
                      <span class="text-xs font-bold text-slate-600">{{ tranche.trancheHoraire }}</span>
                    </td>

                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setResultat(tranche, 'C')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             tranche.resultatFinal === 'C'
                               ? 'bg-emerald-500 border-emerald-500 shadow-sm'
                               : 'border-slate-200 hover:border-emerald-400 bg-white'
                           ]">
                        <i v-if="tranche.resultatFinal === 'C'" class="pi pi-check text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <td class="p-2 border-r border-slate-100 text-center cursor-pointer"
                        @click="setResultat(tranche, 'NC')">
                      <div :class="[
                             'w-7 h-7 mx-auto rounded border-2 flex items-center justify-center transition-all',
                             tranche.resultatFinal === 'NC'
                               ? 'bg-red-500 border-red-500 shadow-sm'
                               : 'border-slate-200 hover:border-red-400 bg-white'
                           ]">
                        <i v-if="tranche.resultatFinal === 'NC'" class="pi pi-times text-white text-xs font-black"></i>
                      </div>
                    </td>

                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="tranche.detailsNC"
                                rows="1"
                                placeholder="Détails NC..."
                                class="w-full bg-transparent border border-transparent focus:border-red-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"
                                :disabled="tranche.resultatFinal !== 'NC'"></textarea>
                    </td>

                    <td class="p-1.5 border-r border-slate-100">
                      <textarea v-model="tranche.actionsCorrection"
                                rows="1"
                                placeholder="Actions..."
                                class="w-full bg-transparent border border-transparent focus:border-emerald-300 rounded px-2 py-1 text-xs outline-none focus:bg-white transition-all resize-none overflow-hidden"></textarea>
                    </td>

                    <td class="p-1.5">
                      <input type="text" v-model="tranche.matriculeApprobateur"
                             placeholder="Matricule..."
                             class="w-full bg-transparent border border-transparent focus:border-slate-300 rounded px-2 py-1 text-xs text-center font-bold text-slate-700 outline-none focus:bg-white transition-all" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

        </template>
      </template>
      
      <div v-else class="text-center text-slate-500 py-12">
        <i class="pi pi-exclamation-triangle text-4xl mb-4 text-orange-400"></i>
        <p>Les données du plan source (modèle) n'ont pas pu être chargées.</p>
      </div>

    </template>
`;
  
  let updated = f.substring(0, startIndex) + newTemplate + f.substring(lastClosingTemplate);
  fs.writeFileSync('src/components/Execution/ExecEncfForm.vue', updated, 'utf8');
  console.log("Successfully updated ExecEncfForm.vue template");
} else {
  console.log("Could not find start/end markers");
}
