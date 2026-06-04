<template>
  <div class="w-full mt-8">
    <div class="bg-slate-800 text-slate-200 border-l-4 border-rose-500 text-[11px] font-bold uppercase p-3 flex justify-between items-center rounded-t-lg">
      <span class="flex items-center gap-2"><i class="ri-error-warning-line text-rose-400 text-lg"></i> Section Risques & Défauts</span>
      <button v-if="!props.isReadOnly" @click="store.ajouterLigneRisque()" class="text-white bg-rose-600 hover:bg-rose-500 px-3 py-1.5 rounded shadow-sm transition-colors flex items-center gap-1 font-bold">
        <i class="ri-add-line"></i> Nouveau Risque
      </button>
    </div>
    <table class="w-full min-w-[1400px] text-left border-collapse text-sm border-x border-b border-slate-300 rounded-b-lg shadow-sm">
      <thead class="bg-slate-900 text-white text-[11px] uppercase tracking-wider font-bold border-b border-slate-700 text-center">
        
        <!-- HEADER SPÉCIFIQUE POUR LES RISQUES -->
          <tr class="bg-slate-900 text-white text-[11px] uppercase tracking-wider font-bold border-b border-slate-700 text-center">
            <th :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[18%]">Risque/ Défaut</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'risque')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">{{ strategy.role === 'BEE' ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'methode')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th :rowspan="hasSubHeaders ? 2 : 1" :colspan="(strategy.isMAS26 && store.entete.afficheMoyenDetectionRisques) ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">Périodicité</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'periodicite')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th v-if="store.entete.afficheMoyenDetectionRisques && !strategy.isMAS26" :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">{{ (strategy.isArchitectureA || strategy.role === 'BEE' || strategy.isMAS19 || strategy.isSER05) ? 'Moyen de contrôle' : 'Moyen de détection' }}</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'moyen_detection')" :key="cCol.key" v-if="store.entete.afficheMoyenDetectionRisques && !strategy.isMAS26" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <template v-if="hasFamilleHeaders">
              <th :colspan="store.familles.length" class="p-2 border-b border-r border-slate-700 bg-slate-800/80">
                {{ (strategy.role === 'BEE' || strategy.isMAS19) ? 'Numéro du moyen de contrôle' : ((strategy.isArchitectureA || strategy.isSER05) ? 'N° moyen de contrôle' : 'Numéro de la pièce de référence') }}
              </th>
            </template>
            <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">
                {{ (strategy.role === 'BEE' || strategy.isMAS19) ? 'Numéro du moyen de contrôle' : ((strategy.isArchitectureA || strategy.isSER05) ? 'N° moyen de contrôle' : 'Numéro de la pièce de référence') }}
            </th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'piece_reference')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th v-if="store.entete.afficheFuiteEtalon || strategy.role === 'BEE'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%]">Fuite Étalon</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'fuite_etalon')" :key="cCol.key" v-if="store.entete.afficheFuiteEtalon || strategy.role === 'BEE'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th v-if="!strategy.hidePressionAndDp" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">Pression d'entrée affichée (en bar)</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'pression_entree')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th v-if="!strategy.isMAS19 && !strategy.hidePressionAndDp" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">{{ store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)' }}</th>
            <th v-if="!strategy.isMAS19 && !strategy.hidePressionAndDp" v-for="cCol in getCustomColumnsAfter('risques', 'dp_affichee')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th :colspan="hasSubHeaders ? 2 : 1" :rowspan="1" class="p-2 border-r border-slate-700 w-[5%] text-[10px]">Résultats</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'resultat')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%] text-[10px]">{{ strategy.isSER05 ? 'Action en cas de non-conformité' : 'Observation en cas de non-conformité' }}</th>
            <th v-for="cCol in getCustomColumnsAfter('risques', 'observation')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
            <th v-if="!props.isReadOnly" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-16 text-slate-400">Actions</th>
          </tr>
          <tr v-if="hasSubHeaders" class="bg-slate-900 text-white text-[11px] uppercase tracking-wider font-bold border-b border-slate-700 text-center">
            <template v-if="hasFamilleHeaders">
              <th v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-24">{{ fam.libelle }}</th>
            </template>
            <th class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-8 text-emerald-400">C</th>
            <th class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-8 text-rose-400">NC</th>
          </tr>
    </thead>
    <tbody class="divide-y divide-slate-100">
        <template v-for="(rInfo, rIdx) in flattenedRowsRisques" :key="rInfo.row._uid">
          <!-- SÉPARATEUR VISUEL ENTRE DEUX RISQUES/DÉFAUTS -->
          <tr v-if="rInfo.isFirstRisque && rIdx > 0">
            <td colspan="20" class="p-0" style="height: 6px; background: #cbd5e1; border-top: 2px solid #64748b;"></td>
          </tr>
          
          <tr class="border-b border-slate-300 hover:bg-rose-50/20 transition-colors"
              :class="{'border-t-2 border-slate-400': rInfo.isFirstMethode}">
              
            <!-- NIVEAU 1 : RISQUE -->
            <td v-if="rInfo.isFirstRisque" :rowspan="rInfo.rowspanRisque" class="p-2 border-r border-slate-400 align-top bg-white border-l-4 border-l-slate-900">
              <textarea :value="rInfo.ligne.libelleRisque" @input="updateRisqueValue($event.target.value, rInfo.rowspanRisque, rowIndex, flattenedRowsRisques)"
                rows="3" :disabled="props.isReadOnly"
                class="w-full text-xs font-bold text-slate-900 border border-slate-200 focus:border-slate-500 outline-none rounded p-1 resize-none disabled:bg-transparent disabled:border-transparent"></textarea>
            </td>

            <!-- CUSTOM COLUMNS après RISQUE -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'risque')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>
            
            <!-- NIVEAU 2 : METHODE -->
            <td v-if="rInfo.isFirstMethode" :rowspan="rInfo.rowspanMethode" class="p-2 border-r border-slate-400 align-top bg-white">
              <textarea :value="rInfo.ligne.libelleMethode" @input="updateMethodeValue($event.target.value, rInfo.rowspanMethode, rowIndex, flattenedRowsRisques)" rows="3" :disabled="props.isReadOnly"
                class="w-full text-xs border border-slate-200 focus:border-slate-500 outline-none rounded p-1 resize-none disabled:bg-transparent disabled:border-transparent"></textarea>
            </td>

            <!-- CUSTOM COLUMNS après METHODE -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'methode')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>

            <!-- NIVEAU 3 : PÉRIODICITÉ -->
            <td v-if="rInfo.isDynFirst_Perio" :rowspan="rInfo.dynRowspan_Perio" :colspan="(strategy.isMAS26 && store.entete.afficheMoyenDetectionRisques) ? 2 : 1" class="p-3 border-r border-slate-400 bg-slate-100/30 align-top">
              <div class="flex flex-col gap-2">
                  <div class="flex items-center gap-1">
                      <div v-if="props.isReadOnly" class="text-[11px] font-black uppercase text-slate-700 whitespace-normal leading-tight">
                          {{ store.periodicites.find(p => p.id === rInfo.group.periodiciteId)?.libelle || '--' }}
                      </div>
                      <select v-else :value="rInfo.group.periodiciteId" @change="e => updatePeriodiciteValue(e.target.value, rInfo.dynRowspan_Perio, rowIndex, flattenedRowsRisques)" class="w-full text-[10px] font-black uppercase border border-slate-400 rounded px-2 py-1.5 outline-none focus:border-slate-600 bg-white shadow-sm transition-all">
                          <option value="">-- PERIODE --</option>
                          <option v-for="p in store.periodicites" :key="p.id" :value="p.id">{{ p.libelle }}</option>
                      </select>
                      <button v-if="!props.isReadOnly" @click="store.ajouterGroupPeriodicite(rInfo.ligne)" class="text-blue-600 hover:text-blue-800" title="Ajouter une autre période">
                          <i class="ri-add-circle-fill text-xl"></i>
                      </button>
                  </div>
              </div>
            </td>

            <!-- CUSTOM COLUMNS après PERIODICITE -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'periodicite')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>

            <!-- NIVEAU 4 : DÉTAILS -->
            <td v-if="store.entete.afficheMoyenDetectionRisques && !strategy.isMAS26" class="p-2 border-r border-slate-400">
              <select v-model="rInfo.row.refMoyenDetectionId" :disabled="props.isReadOnly" class="w-full text-xs text-center border-transparent rounded p-1 uppercase focus:border-slate-500 outline-none bg-transparent">
                <option value="">--</option>
                <option v-for="md in store.moyensDetection" :key="md.id" :value="md.id">{{ md.libelle }}</option>
              </select>
            </td>

            <!-- CUSTOM COLUMNS après MOYEN_DETECTION -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'moyen_detection')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>

            <template v-if="hasFamilleHeaders">
              <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-400 text-center">
                <select :value="store.getPieceValue(rInfo.row, fam.refFamilleCorpsId, 'PRNC') || store.getPieceValue(rInfo.row, fam.refFamilleCorpsId, 'PRC')"
                  @change="e => onPieceSelectChange(e, rInfo.row, fam.refFamilleCorpsId, store.getPieceValue(rInfo.row, fam.refFamilleCorpsId, 'PRC') ? 'PRC' : 'PRNC')"
                  :disabled="props.isReadOnly"
                  class="w-full text-xs text-center border border-slate-200 rounded p-1 uppercase text-slate-900 font-bold focus:border-slate-500 outline-none bg-white/50 disabled:border-transparent">
                  <option value="">--</option>
                  <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                  <option v-if="!props.isReadOnly" value="__ADD__" class="text-emerald-700 font-black">+ Ajouter...</option>
                </select>
              </td>
            </template>
            <template v-else>
              <td class="p-2 border-r border-slate-400 text-center">
                <select :value="store.getPieceValue(rInfo.row, null, 'PRNC') || store.getPieceValue(rInfo.row, null, 'PRC')"
                  @change="e => onPieceSelectChange(e, rInfo.row, null, store.getPieceValue(rInfo.row, null, 'PRC') ? 'PRC' : 'PRNC')"
                  :disabled="props.isReadOnly"
                  class="w-full text-xs text-center border rounded p-1 uppercase focus:border-slate-500 outline-none bg-transparent disabled:border-transparent">
                  <option value="">--</option>
                  <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                  <option v-if="!props.isReadOnly" value="__ADD__" class="text-emerald-700 font-black">+ Ajouter...</option>
                </select>
              </td>
            </template>

            <!-- CUSTOM COLUMNS après PIECE_REFERENCE -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'piece_reference')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>

            <td v-if="store.entete.afficheFuiteEtalon || strategy.role === 'BEE'" class="p-2 border-r border-slate-400 bg-amber-50/10 text-center">
              <select :value="getFuiteValue(rInfo.row)"
                @change="e => onFuiteSelectChange(e, rInfo.row)"
                :disabled="props.isReadOnly"
                class="w-full text-xs text-center border rounded p-1 text-slate-900 focus:border-slate-500 uppercase outline-none bg-transparent disabled:border-transparent">
                <option value="">--</option>
                <option v-for="pr in store.fuitesEtalon" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                <option v-if="!props.isReadOnly" value="__ADD__" class="text-emerald-700 font-black">+ Ajouter...</option>
              </select>
            </td>

            <!-- CUSTOM COLUMNS après FUITE_ETALON -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'fuite_etalon')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>

            <!-- COLONNES OPÉRATEUR -->
            <td v-if="!strategy.hidePressionAndDp" class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
            <!-- CUSTOM après pression_entree -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'pression_entree')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>
            <td v-if="!strategy.isMAS19 && !strategy.hidePressionAndDp" class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
            <!-- CUSTOM après dp_affichee -->
            <template v-if="!strategy.isMAS19 && !strategy.hidePressionAndDp">
              <template v-for="cCol in getCustomColumnsAfter('risques', 'dp_affichee')" :key="cCol.key">
                <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                  <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                  <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                </td>
              </template>
            </template>
            <template v-if="hasSubHeaders">
              <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center"></td>
              <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center"></td>
            </template>
            <template v-else>
              <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px] font-bold">C / NC</td>
            </template>
            <!-- CUSTOM après resultat -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'resultat')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>
            <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
            <!-- CUSTOM après observation -->
            <template v-for="cCol in getCustomColumnsAfter('risques', 'observation')" :key="cCol.key">
  <td v-if="rInfo[`isDynFirst_${cCol.key}`]" :rowspan="rInfo[`dynRowspan_${cCol.key}`]" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
    <textarea v-if="!props.isReadOnly" :value="ensureColonnes(rInfo.ligne)[cCol.key]" @input="updateCustomColumnValue(cCol.key, $event.target.value, rInfo[`dynRowspan_${cCol.key}`], rowIndex, flattenedRowsRisques)" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
    <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
  </td>
</template>

            <!-- ACTIONS -->
            <td v-if="!props.isReadOnly" class="p-1 text-center bg-white border-r border-slate-400 w-16">
              <div class="flex items-center justify-center gap-1">
                <button @click="store.ajouterRowDetail(rInfo.group)" class="text-emerald-600 hover:text-emerald-800 p-0.5" title="Ajouter Moyen/Fuite sous cette période">
                  <i class="ri-add-line text-lg"></i>
                </button>
                <button v-if="rInfo.group.rows.length > 1 || rInfo.ligne.groups.length > 1" @click="store.supprimerRowDetail(rInfo.group, rInfo.row._uid)" class="text-slate-300 hover:text-red-500 p-0.5">
                  <i class="ri-indeterminate-circle-line text-lg"></i>
                </button>
                <button v-if="rInfo.isFirstRisque" @click="store.supprimerLigne(rInfo.ligne._uid, 'RISQUE')" class="text-slate-300 hover:text-red-600 p-0.5" title="Supprimer tout le risque">
                  <i class="ri-delete-bin-fill text-lg"></i>
                </button>
              </div>
            </td>
          </tr>
        </template>
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useVerifMachineTable } from '../composables/useVerifMachineTable';

const props = defineProps({
  isReadOnly: { type: Boolean, default: false }
});

const emit = defineEmits(['add-piece']);

const {
  store,
  strategy,
  hasFamilleHeaders,
  hasSubHeaders,
  getCustomColumnsAfter,
  ensureColonnes,
  getLigneTotalRows,
  totalColumns,
  onUpdateRisqueName,
  getFuiteValue,
  setFuiteValue
} = useVerifMachineTable();

// --- Computed pour l'aplatissement du tableau Risques ---
const flattenedRowsRisques = computed(() => {
    let allTuples = [];
    if (store.lignesRisques && store.lignesRisques.length > 0) {
      store.lignesRisques.forEach(ligne => {
        ligne.groups.forEach(group => {
          group.rows.forEach(row => {
            allTuples.push({ ligne, group, row });
          });
        });
      });

      if (strategy.value.isArchitectureA || strategy.value.isSER05) {
        const perioOrder = [];
        store.lignesRisques.forEach(l => {
            l.groups.forEach(g => {
                if (!perioOrder.includes(g.periodiciteId)) {
                    perioOrder.push(g.periodiciteId);
                }
            });
        });
        allTuples.sort((a, b) => {
            return perioOrder.indexOf(a.group.periodiciteId) - perioOrder.indexOf(b.group.periodiciteId);
        });
      }
    }
  
    if (allTuples.length > 0) {
      // 1. Dynamic rowspans for Risque
      let currentRisque = null;
      let startIdxRisque = 0;
      for (let i = 0; i < allTuples.length; i++) {
          const val = allTuples[i].ligne.libelleRisque;
          if (i === 0) {
              currentRisque = val;
              allTuples[i].isFirstRisque = true;
              allTuples[i].rowspanRisque = 1;
              startIdxRisque = i;
          } else {
              if (val === currentRisque && val !== undefined && val !== null && val !== "") {
                  allTuples[startIdxRisque].rowspanRisque++;
                  allTuples[i].isFirstRisque = false;
                  allTuples[i].rowspanRisque = 0;
              } else {
                  currentRisque = val;
                  allTuples[i].isFirstRisque = true;
                  allTuples[i].rowspanRisque = 1;
                  startIdxRisque = i;
              }
          }
      }

      // 2. Dynamic rowspans for Methode
      let currentMethode = null;
      let startIdxMethode = 0;
      for (let i = 0; i < allTuples.length; i++) {
          const val = allTuples[i].ligne.libelleMethode;
          if (i === 0) {
              currentMethode = val;
              allTuples[i].isFirstMethode = true;
              allTuples[i].rowspanMethode = 1;
              startIdxMethode = i;
          } else {
              // HIERARCHICAL CHECK
              if (val === currentMethode && val !== undefined && val !== null && val !== "" && !allTuples[i].isFirstRisque) {
                  allTuples[startIdxMethode].rowspanMethode++;
                  allTuples[i].isFirstMethode = false;
                  allTuples[i].rowspanMethode = 0;
              } else {
                  currentMethode = val;
                  allTuples[i].isFirstMethode = true;
                  allTuples[i].rowspanMethode = 1;
                  startIdxMethode = i;
              }
          }
      }

      // 3. Dynamic rowspans for Periodicite
      let currentPerioId = null;
      let startIdxPerio = 0;
      for (let i = 0; i < allTuples.length; i++) {
          
          const val = allTuples[i].group.periodiciteId;
          if (i === 0) {
              currentPerioId = val;
              allTuples[i].isDynFirst_Perio = true;
              allTuples[i].dynRowspan_Perio = 1;
              startIdxPerio = i;
          } else {
              // HIERARCHICAL CHECK
              if (val === currentPerioId && val !== undefined && val !== null && val !== "" && !allTuples[i].isFirstMethode) {
                  allTuples[startIdxPerio].dynRowspan_Perio++;
                  allTuples[i].isDynFirst_Perio = false;
                  allTuples[i].dynRowspan_Perio = 0;
              } else {
                  currentPerioId = val;
                  allTuples[i].isDynFirst_Perio = true;
                  allTuples[i].dynRowspan_Perio = 1;
                  startIdxPerio = i;
              }
          }
      }

      // 4. Dynamic rowspans for Custom Columns
      if (store.entete.configurationColonnes) {
        try {
          const cCols = JSON.parse(store.entete.configurationColonnes).filter(c => c.target === 'risques');
          cCols.forEach(cCol => {
            let currentVal = null;
            let startIdx = 0;
            for (let i = 0; i < allTuples.length; i++) {
              const val = ensureColonnes(allTuples[i].ligne)[cCol.key];
              
              let parentIsNew = true;
              if (cCol.after === 'risques') {
                 parentIsNew = allTuples[i].isFirstRisque;
              } else if (cCol.after === 'methode') {
                 parentIsNew = allTuples[i].isFirstMethode;
              } else if (cCol.after === 'periodicite') {
                 parentIsNew = allTuples[i].isDynFirst_Perio;
              }

              if (i === 0) {
                currentVal = val;
                allTuples[i]['isDynFirst_' + cCol.key] = true;
                allTuples[i]['dynRowspan_' + cCol.key] = 1;
                startIdx = i;
              } else {
                if (val === currentVal && val !== "" && val !== undefined && val !== null) {
                  allTuples[startIdx]['dynRowspan_' + cCol.key]++;
                  allTuples[i]['isDynFirst_' + cCol.key] = false;
                  allTuples[i]['dynRowspan_' + cCol.key] = 0;
                } else {
                  currentVal = val;
                  allTuples[i]['isDynFirst_' + cCol.key] = true;
                  allTuples[i]['dynRowspan_' + cCol.key] = 1;
                  startIdx = i;
                }
              }
            }
          });
        } catch (e) {
          console.error("Erreur parsing custom columns:", e);
        }
      }
    }
  
    return allTuples;
  });

const updateCustomColumnValue = (key, newValue, rowspan, startIdx, rowsArray) => {
    for (let i = 0; i < rowspan; i++) {
        const targetLigne = rowsArray[startIdx + i].ligne;
        ensureColonnes(targetLigne)[key] = newValue;
    }
};

const updatePeriodiciteValue = (newEcheance, rowspan, startIdx, rowsArray) => {
    for (let i = 0; i < rowspan; i++) {
        const targetGroup = rowsArray[startIdx + i].group;
        if (targetGroup) {
             targetGroup.echeance = newEcheance ? { ...newEcheance } : null;
        }
    }
};

const onPieceSelectChange = (event, row, familleCorpsId, role) => {
  if (event.target.value === '__ADD__') {
    event.target.value = store.getPieceValue(row, familleCorpsId, role) || '';
    emit('add-piece', { type: 'piece', row, familleCorpsId, role });
  } else {
    store.setPieceValue(row, familleCorpsId, role, event.target.value);
  }
};

const onFuiteSelectChange = (event, row) => {
  if (event.target.value === '__ADD__') {
    event.target.value = getFuiteValue(row) || '';
    const role = store.getPieceValue(row, null, 'FEC') ? 'FEC' : (store.getPieceValue(row, null, 'FENC') ? 'FENC' : 'FEC');
    emit('add-piece', { type: 'fuite', row, familleCorpsId: null, role });
  } else {
    setFuiteValue(row, event.target.value);
  }
};
</script>






