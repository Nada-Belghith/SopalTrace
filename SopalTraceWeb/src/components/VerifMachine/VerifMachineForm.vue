<template>
  <div class="space-y-6 max-w-[1400px] mx-auto pb-20">
    <Toast />
    <ConfirmDialog />

    <!-- ============================================================ -->
    <!-- ÉTAPE 1 : SÉLECTION MACHINE                                   -->
    <!-- ============================================================ -->
    <section v-if="!props.isReadOnly" class="bg-white rounded-xl shadow-sm border border-slate-200 p-5">
      <div class="flex flex-col md:flex-row md:justify-between md:items-center gap-4 mb-4">
        <h2 class="text-sm font-bold text-slate-700 flex items-center gap-2 uppercase tracking-wide">
          <i class="ri-map-pin-line text-slate-500"></i> 1. Informations générales & Machine Concernée
        </h2>

        <!-- ACTIONS -->
        <div class="flex items-center gap-3">
          <!-- Import Excel -->
          <div v-if="!props.isReadOnly && !store.entete.id" class="flex-shrink-0">
            <input type="file" ref="fileInput" @change="handleExcelImport" class="hidden" accept=".xlsx, .xls">
            <button @click="$refs.fileInput.click()" 
              class="h-[38px] px-4 flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-bold transition-colors shadow-sm"
              :disabled="store.isLoading">
              <i v-if="!store.isLoading" class="ri-file-excel-2-line text-lg"></i>
              <i v-else class="ri-loader-4-line animate-spin text-lg"></i>
              Importer la structure Excel
            </button>
          </div>

          <!-- Configurer Colonnes -->
          <button v-if="!props.isReadOnly && store.planInitialise" @click="showColumnModal = true" 
            class="h-[38px] px-4 flex items-center gap-2 bg-slate-900 hover:bg-slate-800 text-white rounded-lg text-xs font-bold transition-colors shadow-sm">
            <i class="pi pi-sliders-h"></i> Configurer Colonnes
          </button>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 items-end mb-4">
        <!-- CHOIX RÉFÉRENCE FORMULAIRE -->
        <div v-if="!props.isReadOnly && !store.entete.id" class="col-span-full mb-4 bg-blue-50/50 border border-blue-200 p-4 rounded-xl flex flex-col md:flex-row items-start md:items-center gap-4">
          <label class="block text-[11px] font-black text-blue-800 uppercase tracking-widest shrink-0">
            <i class="pi pi-file-import mr-1 text-blue-600"></i> Réf. Formulaire  *
          </label>
          <select 
            v-model="refFormulaireSelected" 
            class="w-full md:w-1/3 rounded px-3 py-2 text-sm font-semibold outline-none focus:border-blue-500 transition-shadow bg-white border border-slate-300 text-slate-800 cursor-pointer shadow-sm">
            <option value="">-- Choisir un formulaire générique --</option>
            <option v-for="ref in store.formulairesReferences" :key="ref.id" :value="ref.id">
              {{ ref.codeReference }} - {{ ref.designation }}
            </option>
          </select>
          <p class="text-xs text-blue-600/80 font-medium italic">
            La sélection du formulaire remplira automatiquement la machine ciblée.
          </p>
        </div>
        <div class="flex-1">
          <label class="block text-xs font-bold text-slate-500 mb-1">Machine</label>
          <select v-model="selectedMachineCode" @change="onMachineChange" :disabled="props.isReadOnly || store.entete.id"
            class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-slate-500 focus:ring-1 focus:ring-slate-500 outline-none bg-slate-50 cursor-pointer disabled:opacity-75 disabled:bg-slate-100 font-semibold text-slate-700 transition-all">
            <option value="">-- Choisir une machine --</option>
            <option v-for="mac in store.machines" :key="mac.code" :value="mac.code">
              {{ mac.code }} - {{ mac.libelle }}
            </option>
          </select>
        </div>

      </div>
    </section>

    <template v-if="store.planInitialise">

      <!-- ============================================================ -->
      <!-- ÉTAPE 2 : CONFIGURATION DE LA MATRICE                        -->
      <!-- ============================================================ -->
      <section v-if="!props.isReadOnly" class="bg-white rounded-xl shadow-sm border border-slate-200 p-5 border-l-4 border-l-slate-900">
        <div class="flex justify-between items-start mb-4">
          <h2 class="text-sm font-bold text-slate-700 flex items-center gap-2 uppercase tracking-wide">
            <i class="ri-table-line text-slate-500"></i> 2. Configuration du Plan
          </h2>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-xs font-bold text-slate-500 mb-1">Titre du Rapport</label>
            <input v-model="store.entete.nom" type="text" :disabled="props.isReadOnly" @blur="onNomBlur"
              class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-slate-900 font-semibold text-slate-800 outline-none disabled:bg-slate-50">
          </div>


        </div>
      </section>

      <!-- ============================================================ -->
      <!-- ÉTAPE 3 : TABLEAU DE DONNÉES                                 -->
      <!-- ============================================================ -->
      <section class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div class="overflow-x-auto w-full">
          <table class="w-full min-w-[1400px] text-left border-collapse text-sm">

            <!-- HEADER COMMUN -->
            <thead class="bg-slate-900 text-white text-[11px] uppercase tracking-wider font-bold border-b border-slate-700 text-center">
                <tr>
                  <th :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[18%]">
                    {{ (store.entete.afficheConformite && !isMachineSansConformite) ? 'Test de conformité' : 'Risque/ Défaut' }}
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('risque')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">
                    {{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('methode')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">
                    Périodicité
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('periodicite')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th v-if="store.entete.afficheMoyenDetectionRisques" :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">
                    {{ (isArchitectureA || isBEEMachine || isMAS19) ? 'Moyen de contrôle' : 'Moyen de détection' }}
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('moyen_detection')" :key="cCol.key" v-if="store.entete.afficheMoyenDetectionRisques" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <template v-if="hasFamilleHeaders">
                    <th :colspan="store.familles.length" class="p-2 border-b border-r border-slate-700 bg-slate-800/80">
                      {{ isBEEMachine ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                    </th>
                  </template>
                  <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">
                    {{ isBEEMachine ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('piece_reference')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th v-if="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%]">
                    {{ isBEEMachine ? 'Numéro du fuite étalon' : 'Fuite Étalon' }}
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('fuite_etalon')" :key="cCol.key" v-if="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">
                    Pression d'entrée affichée (en bar)
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('pression_entree')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">
                    {{ store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)' }}
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('dp_affichee')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th :colspan="hasSubHeaders ? 2 : 1" :rowspan="1" class="p-2 border-r border-slate-700 w-[5%] text-[10px]">
                    Résultat
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('resultat')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
                  <th :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%] text-[10px]">
                    Observation en cas de non-conformité
                  </th>
                  <th v-for="cCol in getCustomColumnsAfter('observation')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</th>
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


            <!-- ======= SECTION CONFORMITÉ ======= -->
            <template v-if="store.entete.afficheConformite && !isMachineSansConformite">
              <tbody class="border-b-4 border-slate-400">
                <tr class="bg-slate-100/50 border-b border-slate-300">
                    <td :colspan="totalColumns" class="p-2 px-4 text-[10px] font-bold uppercase tracking-widest text-slate-600 bg-slate-200/50">
                        <div class="flex justify-between items-center">
                            <span><i class="ri-checkbox-circle-line text-blue-600"></i> Section Conformité</span>
                            <button v-if="!props.isReadOnly" @click="store.ajouterLigneConformite()" class="text-blue-700 hover:text-blue-900 flex items-center gap-1 font-black">
                                <i class="ri-add-line"></i> Nouveau Test
                            </button>
                        </div>
                    </td>
                </tr>
                <template v-for="(ligne, lIdx) in store.lignesConformite" :key="ligne._uid">
                  <!-- SÉPARATEUR VISUEL ENTRE DEUX TESTS -->
                  <tr v-if="lIdx > 0">
                    <td colspan="20" class="p-0" style="height: 6px; background: #cbd5e1; border-top: 2px solid #64748b;"></td>
                  </tr>
                  <template v-for="(group, gIdx) in ligne.groups" :key="group._uid">
                    <tr v-for="(row, rIdx) in group.rows" :key="row._uid" 
                        class="border-b border-slate-300 hover:bg-blue-50/20 transition-colors"
                        :class="{'border-t-2 border-slate-400': gIdx > 0 && rIdx === 0}">
                      <!-- NIVEAU 1 : RISQUE (Rowspan global) -->
                      <td v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-400 align-top bg-white">
                        <textarea 
                          :value="ligne.libelleRisque" 
                          @input="e => onUpdateRisqueName(ligne.libelleRisque, e.target.value)"
                          rows="2" :disabled="props.isReadOnly"
                          class="w-full text-xs font-bold text-slate-900 border border-slate-200 focus:border-slate-400 rounded p-1 outline-none resize-none disabled:bg-transparent disabled:border-transparent"></textarea>
                      </td>

                      <!-- CUSTOM COLUMNS après RISQUE -->
                      <td v-for="cCol in getCustomColumnsAfter('risque')" :key="cCol.key" v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>

                      <td v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-400 align-top bg-white">
                        <textarea v-model="ligne.libelleMethode" rows="2" :disabled="props.isReadOnly"
                          class="w-full text-xs border border-slate-300 focus:border-slate-500 rounded p-1 outline-none resize-none disabled:bg-transparent disabled:border-transparent"></textarea>
                      </td>

                      <!-- CUSTOM COLUMNS après METHODE -->
                      <td v-for="cCol in getCustomColumnsAfter('methode')" :key="cCol.key" v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>

                      <!-- NIVEAU 2 : PÉRIODICITÉ (Rowspan du groupe) -->
                      <td v-if="rIdx === 0" :rowspan="group.rows.length" class="p-3 border-r border-slate-400 bg-slate-100/30 align-top">
                        <div class="flex flex-col gap-2">
                            <div class="flex items-center gap-1">
                                <div v-if="props.isReadOnly" class="text-[11px] font-black uppercase text-slate-700 whitespace-normal leading-tight px-2 py-1">
                                    {{ store.periodicites.find(p => p.id === group.periodiciteId)?.libelle || '--' }}
                                </div>
                                <select v-else v-model="group.periodiciteId" class="w-full text-[10px] font-black uppercase border border-slate-400 rounded px-2 py-1.5 outline-none focus:border-slate-600 bg-white shadow-sm">
                                    <option value="">-- PERIODE --</option>
                                    <option v-for="p in store.periodicites" :key="p.id" :value="p.id">{{ p.libelle }}</option>
                                </select>
                                <button v-if="!props.isReadOnly" @click="store.ajouterGroupPeriodicite(ligne)" class="text-blue-600 hover:text-blue-800" title="Ajouter une autre période">
                                    <i class="ri-add-circle-fill text-xl"></i>
                                </button>
                            </div>
                        </div>
                      </td>

                      <!-- CUSTOM COLUMNS après PERIODICITE -->
                      <td v-for="cCol in getCustomColumnsAfter('periodicite')" :key="cCol.key" v-if="rIdx === 0" :rowspan="group.rows.length" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>

                      <!-- NIVEAU 3 : DÉTAILS -->
                      <td v-if="store.entete.afficheMoyenDetectionRisques" class="p-2 border-r border-slate-400">
                        <div v-if="props.isReadOnly" class="text-xs text-center font-semibold text-slate-700 uppercase">
                           {{ store.moyensDetection.find(md => md.id === row.refMoyenDetectionId)?.libelle || '--' }}
                        </div>
                        <select v-else v-model="row.refMoyenDetectionId" class="w-full text-xs text-center border-transparent rounded p-1 uppercase focus:border-slate-500 outline-none bg-transparent">
                          <option value="">--</option>
                          <option v-for="md in store.moyensDetection" :key="md.id" :value="md.id">{{ md.libelle }}</option>
                        </select>
                      </td>

                      <!-- CUSTOM COLUMNS après MOYEN_DETECTION -->
                      <td v-for="cCol in getCustomColumnsAfter('moyen_detection')" :key="cCol.key" v-if="store.entete.afficheMoyenDetectionRisques" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>

                      <template v-if="hasFamilleHeaders">
                        <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-400 text-center">
                          <div v-if="props.isReadOnly" class="text-xs font-bold text-slate-800 uppercase">
                              {{ store.piecesReference.find(pr => pr.id === store.getPieceValue(row, fam.refFamilleCorpsId, 'PRC'))?.code || '--' }}
                          </div>
                          <select v-else :value="store.getPieceValue(row, fam.refFamilleCorpsId, 'PRC')"
                            @change="e => onPieceSelectChange(e, row, fam.refFamilleCorpsId, 'PRC')"
                            class="w-full text-xs text-center border border-slate-200 rounded p-1 uppercase text-slate-900 font-bold focus:border-slate-500 outline-none bg-white/50">
                            <option value="">--</option>
                            <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                            <option value="__ADD__" class="text-emerald-700 font-black">+ Ajouter...</option>
                          </select>
                        </td>
                      </template>
                      <template v-else>
                        <td class="p-2 border-r border-slate-400 text-center">
                          <div v-if="props.isReadOnly" class="text-xs font-bold text-slate-800 uppercase">
                              {{ store.piecesReference.find(pr => pr.id === store.getPieceValue(row, null, 'PRC'))?.code || '--' }}
                          </div>
                          <select v-else :value="store.getPieceValue(row, null, 'PRC')"
                            @change="e => onPieceSelectChange(e, row, null, 'PRC')"
                            class="w-full text-xs text-center border border-slate-200 rounded p-1 uppercase focus:border-slate-500 outline-none bg-white/50">
                            <option value="">--</option>
                            <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                            <option value="__ADD__" class="text-emerald-700 font-black">+ Ajouter...</option>
                          </select>
                        </td>
                      </template>

                      <!-- CUSTOM COLUMNS après PIECE_REFERENCE -->
                      <td v-for="cCol in getCustomColumnsAfter('piece_reference')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>

                      <td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-400 bg-blue-50/30 text-center">
                        <div v-if="props.isReadOnly" class="text-xs font-bold text-blue-900 uppercase">
                            {{ store.fuitesEtalon.find(pr => pr.id === getFuiteValue(row))?.code || '--' }}
                        </div>
                        <select v-else :value="getFuiteValue(row)"
                          @change="e => onFuiteSelectChange(e, row)"
                          class="w-full text-xs text-center border border-slate-200 rounded p-1 text-blue-900 font-bold focus:border-blue-500 uppercase outline-none bg-white/50">
                          <option value="">--</option>
                          <option v-for="pr in store.fuitesEtalon" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                          <option value="__ADD__" class="text-emerald-700 font-black">+ Ajouter...</option>
                        </select>
                      </td>

                      <!-- CUSTOM COLUMNS après FUITE_ETALON -->
                      <td v-for="cCol in getCustomColumnsAfter('fuite_etalon')" :key="cCol.key" v-if="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>

                      <!-- COLONNES OPÉRATEUR -->
                      <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
                      <!-- CUSTOM après pression_entree -->
                      <td v-for="cCol in getCustomColumnsAfter('pression_entree')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>
                      <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
                      <!-- CUSTOM après dp_affichee -->
                      <td v-for="cCol in getCustomColumnsAfter('dp_affichee')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>
                      <template v-if="hasSubHeaders">
                        <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center"></td>
                        <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center"></td>
                      </template>
                      <template v-else>
                        <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px] font-bold">C / NC</td>
                      </template>
                      <!-- CUSTOM après resultat -->
                      <td v-for="cCol in getCustomColumnsAfter('resultat')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>
                      <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
                      <!-- CUSTOM après observation -->
                      <td v-for="cCol in getCustomColumnsAfter('observation')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                        <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                        <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(ligne)[cCol.key] || '--' }}</div>
                      </td>

                      <!-- ACTIONS -->
                      <td v-if="!props.isReadOnly" class="p-1 text-center bg-white border-r border-slate-400 w-16">
                        <div class="flex items-center justify-center gap-1">
                          <button @click="store.ajouterRowDetail(group)" class="text-emerald-600 hover:text-emerald-800 p-0.5" title="Ajouter Moyen/Fuite sous cette période">
                            <i class="ri-add-line text-xl font-bold"></i>
                          </button>
                          <button v-if="group.rows.length > 1 || ligne.groups.length > 1" @click="store.supprimerRowDetail(group, row._uid)" class="text-slate-400 hover:text-red-500 p-0.5">
                            <i class="ri-indeterminate-circle-line text-lg"></i>
                          </button>
                          <button v-if="gIdx === 0 && rIdx === 0" @click="store.supprimerLigne(ligne._uid, 'CONFORMITE')" class="text-slate-400 hover:text-red-600 p-0.5" title="Supprimer tout le test">
                            <i class="ri-delete-bin-fill text-lg"></i>
                          </button>
                        </div>
                      </td>
                    </tr>
                  </template>
                </template>
              </tbody>
            </template>

            <!-- ======= SECTION RISQUES ======= -->
            <tbody class="divide-y divide-slate-100">
                <tr class="bg-slate-100/50 border-b border-slate-400">
                    <td :colspan="totalColumns" class="p-2 px-4 text-[10px] font-bold uppercase tracking-widest text-slate-600 bg-slate-200/50">
                        <div class="flex justify-between items-center">
                            <span><i class="ri-error-warning-line text-rose-600"></i> Section Risques & Défauts</span>
                            <button v-if="!props.isReadOnly" @click="store.ajouterLigneRisque()" class="text-blue-700 hover:text-blue-900 flex items-center gap-1 font-black">
                                <i class="ri-add-line"></i> Nouveau Risque
                            </button>
                        </div>
                    </td>
                </tr>
                
                <!-- HEADER SPÉCIFIQUE POUR LES RISQUES (Seulement si la section conformité est affichée au-dessus) -->
                <template v-if="store.entete.afficheConformite && !isMachineSansConformite">
                  <tr class="bg-slate-900 text-white text-[11px] uppercase tracking-wider font-bold border-b border-slate-700 text-center">
                    <td :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[18%]">Risque/ Défaut</td>
                    <td v-for="cCol in getCustomColumnsAfter('risque')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">{{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}</td>
                    <td v-for="cCol in getCustomColumnsAfter('methode')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td :rowspan="hasSubHeaders ? 2 : 1" :colspan="(isMAS26 && store.entete.afficheMoyenDetectionRisques) ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">Périodicité</td>
                    <td v-for="cCol in getCustomColumnsAfter('periodicite')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td v-if="store.entete.afficheMoyenDetectionRisques && !isMAS26" :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">{{ (isArchitectureA || isBEEMachine || isMAS19) ? 'Moyen de contrôle' : 'Moyen de détection' }}</td>
                    <td v-for="cCol in getCustomColumnsAfter('moyen_detection')" :key="cCol.key" v-if="store.entete.afficheMoyenDetectionRisques && !isMAS26" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <template v-if="hasFamilleHeaders">
                      <td :colspan="store.familles.length" class="p-2 border-b border-r border-slate-700 bg-slate-800/80">
                        {{ (isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce de référence' }}
                      </td>
                    </template>
                    <td v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">
                        {{ (isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce de référence' }}
                    </td>
                    <td v-for="cCol in getCustomColumnsAfter('piece_reference')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%]">Fuite Étalon</td>
                    <td v-for="cCol in getCustomColumnsAfter('fuite_etalon')" :key="cCol.key" v-if="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">Pression d'entrée affichée (en bar)</td>
                    <td v-for="cCol in getCustomColumnsAfter('pression_entree')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[8%] text-[10px]">{{ store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)' }}</td>
                    <td v-for="cCol in getCustomColumnsAfter('dp_affichee')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td :colspan="hasSubHeaders ? 2 : 1" :rowspan="1" class="p-2 border-r border-slate-700 w-[5%] text-[10px]">Résultat</td>
                    <td v-for="cCol in getCustomColumnsAfter('resultat')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[12%] text-[10px]">Observation en cas de non-conformité</td>
                    <td v-for="cCol in getCustomColumnsAfter('observation')" :key="cCol.key" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%] text-[10px] text-amber-400 uppercase">{{ cCol.label }}</td>
                    <td v-if="!props.isReadOnly" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-16 text-slate-400">Actions</td>
                  </tr>                  <tr v-if="hasSubHeaders" class="bg-slate-900 text-white text-[11px] uppercase tracking-wider font-bold border-b border-slate-700 text-center">
                    <template v-if="hasFamilleHeaders">
                      <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-24">{{ fam.libelle }}</td>
                    </template>
                    <td class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-8 text-emerald-400">C</td>
                    <td class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-8 text-rose-400">NC</td>
                  </tr>

                </template>
                <template v-for="(rInfo, rIdx) in flattenedRowsRisques" :key="rInfo.row._uid">
                  <!-- SÉPARATEUR VISUEL ENTRE DEUX RISQUES/DÉFAUTS -->
                  <tr v-if="rInfo.isFirstRisque && rIdx > 0">
                    <td colspan="20" class="p-0" style="height: 6px; background: #cbd5e1; border-top: 2px solid #64748b;"></td>
                  </tr>
                  
                  <tr class="border-b border-slate-300 hover:bg-rose-50/20 transition-colors"
                      :class="{'border-t-2 border-slate-400': rInfo.isFirstMethode}">
                      
                    <!-- NIVEAU 1 : RISQUE -->
                    <td v-if="rInfo.isFirstRisque" :rowspan="rInfo.rowspanRisque" class="p-2 border-r border-slate-400 align-top bg-white border-l-4 border-l-slate-900">
                      <textarea 
                        :value="rInfo.ligne.libelleRisque" 
                        @input="e => onUpdateRisqueName(rInfo.ligne.libelleRisque, e.target.value)"
                        rows="3" :disabled="props.isReadOnly"
                        class="w-full text-xs font-bold text-slate-900 border border-slate-200 focus:border-slate-500 outline-none rounded p-1 resize-none disabled:bg-transparent disabled:border-transparent"></textarea>
                    </td>

                    <!-- CUSTOM COLUMNS après RISQUE -->
                    <td v-for="cCol in getCustomColumnsAfter('risque')" :key="cCol.key" v-if="rInfo.isFirstRisque" :rowspan="rInfo.rowspanRisque" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>
                    
                    <!-- NIVEAU 2 : METHODE -->
                    <td v-if="rInfo.isFirstMethode" :rowspan="rInfo.rowspanMethode" class="p-2 border-r border-slate-400 align-top bg-white">
                      <textarea v-model="rInfo.ligne.libelleMethode" rows="3" :disabled="props.isReadOnly"
                        class="w-full text-xs border border-slate-200 focus:border-slate-500 outline-none rounded p-1 resize-none disabled:bg-transparent disabled:border-transparent"></textarea>
                    </td>

                    <!-- CUSTOM COLUMNS après METHODE -->
                    <td v-for="cCol in getCustomColumnsAfter('methode')" :key="cCol.key" v-if="rInfo.isFirstMethode" :rowspan="rInfo.rowspanMethode" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>

                    <!-- NIVEAU 3 : PÉRIODICITÉ -->
                    <td v-if="rInfo.isFirstPerio" :rowspan="rInfo.rowspanPerio" :colspan="(isMAS26 && store.entete.afficheMoyenDetectionRisques) ? 2 : 1" class="p-3 border-r border-slate-400 bg-slate-100/30 align-top">
                      <div class="flex flex-col gap-2">
                          <div class="flex items-center gap-1">
                              <div v-if="props.isReadOnly" class="text-[11px] font-black uppercase text-slate-700 whitespace-normal leading-tight">
                                  {{ store.periodicites.find(p => p.id === rInfo.group.periodiciteId)?.libelle || '--' }}
                              </div>
                              <select v-else v-model="rInfo.group.periodiciteId" class="w-full text-[10px] font-black uppercase border border-slate-400 rounded px-2 py-1.5 outline-none focus:border-slate-600 bg-white shadow-sm transition-all">
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
                    <td v-for="cCol in getCustomColumnsAfter('periodicite')" :key="cCol.key" v-if="rInfo.isFirstPerio" :rowspan="rInfo.rowspanPerio" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>

                    <!-- NIVEAU 4 : DÉTAILS -->
                    <td v-if="store.entete.afficheMoyenDetectionRisques && !isMAS26" class="p-2 border-r border-slate-400">
                      <select v-model="rInfo.row.refMoyenDetectionId" :disabled="props.isReadOnly" class="w-full text-xs text-center border-transparent rounded p-1 uppercase focus:border-slate-500 outline-none bg-transparent">
                        <option value="">--</option>
                        <option v-for="md in store.moyensDetection" :key="md.id" :value="md.id">{{ md.libelle }}</option>
                      </select>
                    </td>

                    <!-- CUSTOM COLUMNS après MOYEN_DETECTION -->
                    <td v-for="cCol in getCustomColumnsAfter('moyen_detection')" :key="cCol.key" v-if="store.entete.afficheMoyenDetectionRisques && !isMAS26" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>

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
                    <td v-for="cCol in getCustomColumnsAfter('piece_reference')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>

                    <td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-400 bg-amber-50/10 text-center">
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
                    <td v-for="cCol in getCustomColumnsAfter('fuite_etalon')" :key="cCol.key" v-if="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>

                    <!-- COLONNES OPÉRATEUR -->
                    <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
                    <!-- CUSTOM après pression_entree -->
                    <td v-for="cCol in getCustomColumnsAfter('pression_entree')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>
                    <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
                    <!-- CUSTOM après dp_affichee -->
                    <td v-for="cCol in getCustomColumnsAfter('dp_affichee')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>
                    <template v-if="hasSubHeaders">
                      <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center"></td>
                      <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center"></td>
                    </template>
                    <template v-else>
                      <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px] font-bold">C / NC</td>
                    </template>
                    <!-- CUSTOM après resultat -->
                    <td v-for="cCol in getCustomColumnsAfter('resultat')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>
                    <td class="p-2 border-r border-slate-400 bg-slate-50/50 text-center text-slate-400 italic text-[10px]">Saisi par l'opérateur</td>
                    <!-- CUSTOM après observation -->
                    <td v-for="cCol in getCustomColumnsAfter('observation')" :key="cCol.key" class="p-2 border-r border-slate-400 align-top bg-amber-50/20">
                      <textarea v-if="!props.isReadOnly" v-model="ensureColonnes(rInfo.ligne)[cCol.key]" class="w-full text-xs font-bold text-slate-800 border border-slate-200 focus:border-amber-400 outline-none rounded p-1 resize-none bg-white/50" rows="2"></textarea>
                      <div v-else class="text-xs font-bold text-slate-800">{{ ensureColonnes(rInfo.ligne)[cCol.key] || '--' }}</div>
                    </td>

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
      </section>

      <!-- ============================================================ -->
      <!-- REMARQUES & LÉGENDE                                          -->
      <!-- ============================================================ -->
      <RemarquesLegendeBox
        v-model:remarques="store.entete.remarques"
        v-model:legendeMoyens="store.entete.legendeMoyens"
        :is-read-only="props.isReadOnly"
      />

      <!-- ============================================================ -->
      <!-- BARRE D'ACTIONS                                              -->
      <!-- ============================================================ -->
      <div v-if="!props.isReadOnly" class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-6 rounded-b-xl">
        <EditorActions 
          :label="store.entete.id ? 'Enregistrer les Modifications' : 'Enregistrer le Plan'"
          loading-label="Enregistrement..."
          :icon="store.entete.id ? 'pi pi-save' : 'pi pi-check'"
          variant="primary"
          :is-loading="store.isLoading"
          @submit="onSauvegarder"
          @cancel="onCancel"
        />
      </div>

    </template>
  </div>

  <!-- ============================================================ -->
  <!-- MODAL DE CONFIGURATION DES COLONNES -->
  <ColumnConfigurator 
      v-model:visible="showColumnModal"
      v-model="store.entete.configurationColonnes"
      :baseColumns="vmBaseColumns"
  >
      <!-- GESTION DES FAMILLES DE CORPS -->
      <template #extra-configuration>
        <div v-if="store.entete.afficheFamilles" class="mt-6 border-t border-slate-700/50 pt-6">
          <h3 class="text-xs font-bold text-slate-300 uppercase tracking-wider mb-4 flex items-center gap-2">
            <i class="ri-node-tree text-emerald-500"></i> Familles de Corps / Réf.
          </h3>
          <div class="flex flex-col gap-3">
            <div class="relative">
              <AutoComplete 
                  v-model="selectedFamilleObj"
                  :suggestions="filteredFamilles"
                  @complete="searchFamille"
                  @item-select="onFamilleSelected"
                  optionLabel="label"
                  :dropdown="true"
                  placeholder="Rechercher pour ajouter une famille..."
                  class="w-full"
                  appendTo="body"
                  :inputStyle="{ width: '100%', padding: '0.6rem', fontSize: '0.875rem' }"
              />
            </div>
            
            <div class="bg-[#1e293b]/50 border border-slate-700/50 rounded-xl p-3 flex flex-wrap gap-2 max-h-[120px] overflow-y-auto">
              <div v-if="store.familles.length === 0" class="text-xs text-slate-500 italic w-full text-center py-2">
                Aucune famille sélectionnée.
              </div>
              <div v-for="fam in store.familles" :key="fam.id" class="flex items-center gap-2 bg-emerald-900/40 border border-emerald-700/50 text-emerald-100 px-3 py-1.5 rounded-lg text-xs font-semibold shadow-sm">
                <span>{{ fam.libelle }}</span>
                <button @click="store.supprimerFamille(fam.id)" class="text-emerald-400 hover:text-emerald-200 transition-colors ml-1" title="Retirer">
                  <i class="ri-close-line text-base leading-none"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- APERÇU COMPLET VERIF MACHINE -->
      <template #preview="{ previewColumns }">
        <div class="flex flex-col gap-6">
          <!-- Aperçu Conformité -->
          <div v-if="store.entete.afficheConformite && !isMachineSansConformite" class="border border-slate-300 rounded overflow-hidden">
            <div class="bg-[#0f172a] text-slate-200 border-l-4 border-emerald-500 text-[10px] font-bold uppercase p-2">Section Conformité</div>
            <div class="overflow-x-auto">
              <table class="w-full text-left text-xs whitespace-nowrap">
                <thead class="bg-slate-800 text-white font-black text-[10px] uppercase tracking-wider text-center">
                  <tr>
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <th v-if="cCol.key === 'risque'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">Test de conformité</th>
                      <th v-else-if="cCol.key === 'methode'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">{{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}</th>
                      <th v-else-if="cCol.key === 'periodicite'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">Périodicité</th>
                      <th v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ (isArchitectureA || isBEEMachine || isMAS19) ? 'Moyen de contrôle' : 'Moyen de détection' }}
                      </th>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                          <th :colspan="store.familles.length" class="p-2 border-b border-r border-slate-600 bg-slate-700">
                            {{ isBEEMachine ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                          </th>
                        </template>
                        <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">
                          {{ isBEEMachine ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                        </th>
                      </template>
                      <th v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ isBEEMachine ? 'Numéro du fuite étalon' : 'Fuite Étalon' }}
                      </th>
                      <th v-else-if="cCol.key === 'pression_entree'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">Pression d'entrée affichée (en bar)</th>
                      <th v-else-if="cCol.key === 'dp_affichee'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">{{ store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)' }}</th>
                      <th v-else-if="cCol.key === 'resultat'" :colspan="hasSubHeaders ? 2 : 1" :rowspan="1" class="p-2 border-r border-slate-600 w-20">Résultat</th>
                      <th v-else-if="cCol.key === 'observation'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">Observation en cas de non-conformité</th>
                      <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24 text-amber-400 bg-slate-700/50">
                        {{ cCol.label }}
                      </th>
                    </template>
                  </tr>
                  <tr v-if="hasSubHeaders">
                    <template v-if="hasFamilleHeaders">
                      <th v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-20">{{ fam.libelle }}</th>
                    </template>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-emerald-400">C</th>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-rose-400">NC</th>
                  </tr>
                </thead>
                <tbody class="text-slate-600 bg-white border border-t-0 border-slate-200">
                  <tr class="hover:bg-slate-50 text-center">
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <td v-if="cCol.key === 'risque'" class="p-2 border-r border-slate-200 text-left font-bold">Aspect visuel</td>
                      <td v-else-if="cCol.key === 'methode'" class="p-2 border-r border-slate-200">Visuel</td>
                      <td v-else-if="cCol.key === 'periodicite'" class="p-2 border-r border-slate-200">1 / équipe</td>
                      <td v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" class="p-2 border-r border-slate-200">M.D.</td>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                           <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-200">PRC...</td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200">PRC...</td>
                      </template>
                      <td v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-200 text-blue-600 font-bold">FE...</td>
                      <td v-else-if="cCol.key === 'pression_entree'" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <td v-else-if="cCol.key === 'dp_affichee'" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <template v-else-if="cCol.key === 'resultat'">
                        <template v-if="hasSubHeaders">
                           <td class="p-2 border-r border-slate-200"></td>
                           <td class="p-2 border-r border-slate-200"></td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200 font-bold">C / NC</td>
                      </template>
                      <td v-else-if="cCol.key === 'observation'" class="p-2 border-r border-slate-200 text-slate-400 italic">Obs...</td>
                      <td v-else class="p-2 border-r border-slate-200 bg-amber-50">
                        <span class="text-amber-600 bg-amber-100 px-1 py-0.5 rounded border border-amber-200 text-[10px]">Auto</span>
                      </td>
                    </template>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Aperçu Risques -->
          <div class="border border-slate-300 rounded overflow-hidden">
            <div class="bg-[#0f172a] text-slate-200 border-l-4 border-rose-500 text-[10px] font-bold uppercase p-2">Section Risques & Défauts</div>
            <div class="overflow-x-auto">
              <table class="w-full text-left text-xs whitespace-nowrap">
                <!-- Même Header -->
                <thead class="bg-slate-800 text-white font-black text-[10px] uppercase tracking-wider text-center">
                  <tr>
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <th v-if="cCol.key === 'risque'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">Risque/ Défaut</th>
                      <th v-else-if="cCol.key === 'methode'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">{{ isBEEMachine ? 'Méthode de controle' : 'Moyen/ Méthode de contrôle' }}</th>
                      <th v-else-if="cCol.key === 'periodicite'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">Périodicité</th>
                      <th v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ (isArchitectureA || isBEEMachine || isMAS19) ? 'Moyen de contrôle' : 'Moyen de détection' }}
                      </th>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                          <th :colspan="store.familles.length" class="p-2 border-b border-r border-slate-600 bg-slate-700">
                            {{ isBEEMachine ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                          </th>
                        </template>
                        <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">
                          {{ isBEEMachine ? 'Numéro du moyen de contrôle' : ((isArchitectureA || isMAS19) ? 'N° moyen de contrôle' : 'Numéro de la pièce référence') }}
                        </th>
                      </template>
                      <th v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24">
                        {{ isBEEMachine ? 'Numéro du fuite étalon' : 'Fuite Étalon' }}
                      </th>
                      <th v-else-if="cCol.key === 'pression_entree'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">Pression d'entrée affichée (en bar)</th>
                      <th v-else-if="cCol.key === 'dp_affichee'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-20">{{ store.entete.machineCode?.includes('BEE47') ? 'Fuite affichée (en Pa)' : 'ΔP affichée (en Pa)' }}</th>
                      <th v-else-if="cCol.key === 'resultat'" :colspan="hasSubHeaders ? 2 : 1" :rowspan="1" class="p-2 border-r border-slate-600 w-20">Résultat</th>
                      <th v-else-if="cCol.key === 'observation'" :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-32">Observation en cas de non-conformité</th>
                      <th v-else :rowspan="hasSubHeaders ? 2 : 1" class="p-2 border-r border-slate-600 w-24 text-amber-400 bg-slate-700/50">
                        {{ cCol.label }}
                      </th>
                    </template>
                  </tr>
                  <tr v-if="hasSubHeaders">
                    <template v-if="hasFamilleHeaders">
                      <th v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-20">{{ fam.libelle }}</th>
                    </template>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-emerald-400">C</th>
                    <th class="p-2 border-r border-slate-600 bg-slate-700/80 text-[9px] w-10 text-rose-400">NC</th>
                  </tr>
                </thead>
                <tbody class="text-slate-600 bg-white border border-t-0 border-slate-200">
                  <tr class="hover:bg-slate-50 text-center">
                    <template v-for="cCol in previewColumns" :key="cCol.key">
                      <td v-if="cCol.key === 'risque'" class="p-2 border-r border-slate-200 text-left font-bold text-red-700">Fissure</td>
                      <td v-else-if="cCol.key === 'methode'" class="p-2 border-r border-slate-200">Visuel</td>
                      <td v-else-if="cCol.key === 'periodicite'" class="p-2 border-r border-slate-200">1 / équipe</td>
                      <td v-else-if="cCol.key === 'moyen_detection'" v-show="store.entete.afficheMoyenDetectionRisques" class="p-2 border-r border-slate-200">M.D.</td>
                      <template v-else-if="cCol.key === 'piece_reference'">
                        <template v-if="hasFamilleHeaders">
                           <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-200">PRC...</td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200">PRC...</td>
                      </template>
                      <td v-else-if="cCol.key === 'fuite_etalon'" v-show="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-200 text-blue-600 font-bold">FE...</td>
                      <td v-else-if="cCol.key === 'pression_entree'" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <td v-else-if="cCol.key === 'dp_affichee'" class="p-2 border-r border-slate-200 text-slate-400 italic">Saisi...</td>
                      <template v-else-if="cCol.key === 'resultat'">
                        <template v-if="hasSubHeaders">
                           <td class="p-2 border-r border-slate-200"></td>
                           <td class="p-2 border-r border-slate-200"></td>
                        </template>
                        <td v-else class="p-2 border-r border-slate-200 font-bold">C / NC</td>
                      </template>
                      <td v-else-if="cCol.key === 'observation'" class="p-2 border-r border-slate-200 text-slate-400 italic">Obs...</td>
                      <td v-else class="p-2 border-r border-slate-200 bg-amber-50">
                        <span class="text-amber-600 bg-amber-100 px-1 py-0.5 rounded border border-amber-200 text-[10px]">Auto</span>
                      </td>
                    </template>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </template>
  </ColumnConfigurator>

  <!-- MODAL INLINE : CRÉATION PIÈCE RÉFÉRENCE / ÉTALON FUITE     -->
  <!-- ============================================================ -->
  <Teleport to="body">
    <div v-if="showAddPieceModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div class="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-md mx-4 overflow-hidden">
        <!-- Header -->
        <div class="bg-slate-900 text-white px-6 py-4 flex items-center justify-between">
          <div class="flex items-center gap-3">
            <i :class="['text-xl', addPieceType.startsWith('F') ? 'ri-drop-line text-blue-400' : 'ri-cube-line text-emerald-400']"></i>
            <div>
              <p class="text-xs font-bold uppercase tracking-widest text-slate-400">
                {{ addPieceType.startsWith('F') ? 'Étalon Fuite' : 'Pièce Référence' }}
              </p>
              <p class="text-base font-black">Ajouter dans le référentiel</p>
            </div>
          </div>
          <button @click="closeAddPieceModal" class="text-slate-400 hover:text-white transition-colors">
            <i class="ri-close-line text-2xl"></i>
          </button>
        </div>

        <!-- Body -->
        <div class="p-6 space-y-4">
          <!-- Type Pièce -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Type</label>
            <select v-model="addPieceType" class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm font-semibold text-slate-800 outline-none focus:border-slate-600 bg-slate-50">
              <option v-for="opt in typePieceOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
            </select>
          </div>

          <!-- Code -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Code *</label>
            <input
              v-model="newPiece.code"
              type="text"
              placeholder="Ex: PR-001, ET-FUITE-A"
              class="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm font-bold text-slate-800 uppercase outline-none focus:border-slate-900 bg-white tracking-widest"
              @keydown.enter="confirmAddPiece"
            />
          </div>

          <!-- Désignation -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Désignation <span class="text-slate-400 font-normal normal-case">(optionnel)</span></label>
            <input
              v-model="newPiece.designation"
              type="text"
              placeholder="Description courte..."
              class="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm text-slate-700 outline-none focus:border-slate-900 bg-white"
            />
          </div>

          <!-- Machine (pré-remplie) -->
          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1.5">Machine associée <span class="text-slate-400 font-normal normal-case">(optionnel)</span></label>
            <input
              v-model="newPiece.machineCode"
              type="text"
              placeholder="Code machine"
              class="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm text-slate-700 outline-none focus:border-slate-900 bg-white"
            />
          </div>
        </div>

        <!-- Footer -->
        <div class="bg-slate-50 border-t border-slate-200 px-6 py-4 flex justify-end gap-3">
          <button
            @click="closeAddPieceModal"
            class="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-100 transition-colors"
          >
            Annuler
          </button>
          <button
            @click="confirmAddPiece"
            :disabled="isCreatingPiece || !newPiece.code.trim()"
            class="px-6 py-2 text-sm font-bold text-white bg-slate-900 rounded-lg hover:bg-slate-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            <i :class="isCreatingPiece ? 'ri-loader-4-line animate-spin' : 'ri-add-circle-fill'"></i>
            {{ isCreatingPiece ? 'Création...' : 'Créer & Sélectionner' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useVerifMachineStore } from '@/stores/verifMachineStore';
import EditorActions from '@/components/Shared/EditorActions.vue';
import RemarquesLegendeBox from '@/components/Shared/RemarquesLegendeBox.vue';
import ColumnConfigurator from '@/components/Shared/ColumnConfigurator.vue';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import AutoComplete from 'primevue/autocomplete';
import apiClient from '@/services/apiClient';
import { parseDesignation } from '@/utils/designationParser';


const props = defineProps({
  isReadOnly: { type: Boolean, default: false }
});

const store = useVerifMachineStore();
const confirm = useConfirm();
const toast = useToast();
const router = useRouter();

// --- GESTION FAMILLES POUR COLUMN CONFIGURATOR ---
const selectedFamilleObj = ref(null);
const filteredFamilles = ref([]);

const allPossibleFamilles = computed(() => {
  const fCorps = store.famillesCorps.map(f => ({ id: f.id, label: f.code + ' - ' + f.libelle, type: 'famille' }));
  const pRef = store.piecesReference.map(p => ({ id: p.id, label: p.code, type: 'piece' }));
  return [...fCorps, ...pRef];
});

const searchFamille = (event) => {
  const q = event.query.toLowerCase();
  filteredFamilles.value = allPossibleFamilles.value.filter(f => 
    f.label.toLowerCase().includes(q) && 
    !store.familles.find(sf => sf.refFamilleCorpsId === f.id)
  );
};

const onFamilleSelected = (event) => {
  if (event.value && event.value.id) {
    store.ajouterFamille(event.value.id);
  }
  selectedFamilleObj.value = null;
};

const showColumnModal = ref(false);

// Helper : colonnes personnalisées à insérer après une colonne de base donnée
const getCustomColumnsAfter = (key) => {
  const cols = store.entete.configurationColonnes || [];
  const parsed = typeof cols === 'string' ? JSON.parse(cols) : cols;
  return (parsed || []).filter(col => col.insertAfter === key);
};

// Helper : garantit que valeursColonnesSpecifiques existe sur la ligne (protection anti-crash)
const ensureColonnes = (ligne) => {
  if (!ligne.valeursColonnesSpecifiques) {
    ligne.valeursColonnesSpecifiques = {};
  }
  return ligne.valeursColonnesSpecifiques;
};


const refFormulaireSelected = ref('');


onMounted(() => {
  if (!props.isReadOnly && !store.entete.id) {
    store.fetchFormulairesReferences('VERIF_MACHINE');
  }
});

watch(refFormulaireSelected, async (newRefId) => {
  if (!newRefId) {
    store.entete.configurationColonnes = [];
    return;
  }
  const refObj = store.formulairesReferences.find(r => r.id === newRefId);
  if (!refObj) return;

  const designation = refObj.designation || '';
  const parsed = parseDesignation(designation, [], store.machines || []);

  if (parsed.machineCode) {
    selectedMachineCode.value = parsed.machineCode;
    // On force l'initialisation de la machine de manière asynchrone pour ne pas écraser les colonnes ensuite
    await store.initialiserPlan(parsed.machineCode);
    // On met aussi à jour le nom
    store.entete.nom = designation;
  }

  // Appliquer la configuration des colonnes du formulaire sélectionné
  if (refObj.configurationStructureJson) {
    try {
      store.entete.configurationColonnes = typeof refObj.configurationStructureJson === 'string' 
        ? JSON.parse(refObj.configurationStructureJson) 
        : refObj.configurationStructureJson;
    } catch (e) {
      console.error("Erreur parsing configuration colonnes:", e);
      store.entete.configurationColonnes = [];
    }
  } else {
    store.entete.configurationColonnes = [];
  }
});

const onCancel = () => {
  router.push('/dev/hub');
};

const selectedMachineCode = ref('');

// Synchroniser le code machine local avec le store
watch(() => store.entete.machineCode, (newVal) => {
  selectedMachineCode.value = newVal || '';
}, { immediate: true });

const isMachineSansConformite = computed(() => {
  if (!store.entete.machineCode) return false;
  const code = store.entete.machineCode.toUpperCase().replace('-', '').replace(' ', '').trim();
  return code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') || 
         code.includes('MAS19') || code.includes('MAS20') || code.startsWith('SER');
});

watch(isMachineSansConformite, (newVal) => {
  if (newVal) {
    store.entete.afficheConformite = false;
    store.lignesConformite = [];
  }
}, { immediate: true });

const isSERMachine = computed(() => {
  if (!store.entete.machineCode) return false;
  return store.entete.machineCode.toUpperCase().startsWith('SER');
});

// Auto-détection de l'architecture (Périodicité parente de Méthode)
const isArchitectureA = computed(() => {
  if (isSERMachine.value) return true; // Force pour SER
  if (isBEEMachine.value) return false; // Force pour BEE (Architecture B)
  
  // Vérifie si dans un même risque, la même périodicité est utilisée par plusieurs méthodes (lignes)
  const byRisque = groupBy(store.lignesRisques, l => l.libelleRisque || '');
  for (const [, lignes] of Object.entries(byRisque)) {
    if (lignes.length <= 1) continue;
    const perioMap = new Map();
    for (const ligne of lignes) {
      for (const group of ligne.groups) {
        if (group.periodiciteId) {
          if (perioMap.has(group.periodiciteId)) {
            // Une autre méthode dans ce même risque a déjà utilisé cette périodicité
            return true;
          }
          perioMap.set(group.periodiciteId, true);
        }
      }
    }
  }
  return false;
});

const isBEEMachine = computed(() => {
  if (!store.entete.machineCode) return false;
  const code = store.entete.machineCode.toUpperCase().replace('-', '').replace(' ', '');
  return code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47');
});

const isMAS26 = computed(() => {
  if (!store.entete.machineCode) return false;
  return store.entete.machineCode.toUpperCase().includes('MAS26');
});

const isMAS19 = computed(() => {
  if (!store.entete.machineCode) return false;
  return store.entete.machineCode.toUpperCase().includes('MAS19');
});

const customColumns = computed(() => {
  if (!store.entete.configurationColonnes) return [];
  if (typeof store.entete.configurationColonnes === 'string') {
    try {
      return JSON.parse(store.entete.configurationColonnes);
    } catch (e) {
      return [];
    }
  }
  return store.entete.configurationColonnes;
});

const totalColumns = computed(() => {
  let count = 3; // Risque + Methode + Périodicité
  if (store.entete.afficheMoyenDetectionRisques) count++;
  count += hasFamilleHeaders.value ? store.familles.length : 1;
  if (store.entete.afficheFuiteEtalon || isBEEMachine.value) count++;
  count += hasSubHeaders.value ? 5 : 4; // Colonnes Opérateur : Pression, DP, Resultats(2), Observation
  count += customColumns.value.length; // Colonnes personnalisées
  if (!props.isReadOnly) count++;
  return count;
});

const vmBaseColumns = computed(() => {
  const cols = [
    { key: 'risque', label: 'RISQUE/ DÉFAUT' },
    { key: 'methode', label: isBEEMachine.value ? 'MÉTHODE DE CONTRÔLE' : 'MOYEN/ MÉTHODE DE CONTRÔLE' },
    { key: 'periodicite', label: 'PÉRIODICITÉ' }
  ];
  if (store.entete.afficheMoyenDetectionRisques) {
    cols.push({ key: 'moyen_detection', label: (isArchitectureA.value || isBEEMachine.value || isMAS19.value) ? 'MOYEN DE CONTRÔLE' : 'MOYEN DE DÉTECTION' });
  }
  cols.push({ key: 'piece_reference', label: isBEEMachine.value ? 'NUMÉRO DU MOYEN DE CONTRÔLE' : ((isArchitectureA.value || isMAS19.value) ? 'N° MOYEN DE CONTRÔLE' : 'NUMÉRO DE LA PIÈCE RÉFÉRENCE') });
  if (store.entete.afficheFuiteEtalon || isBEEMachine.value) {
    cols.push({ key: 'fuite_etalon', label: isBEEMachine.value ? 'NUMÉRO DU FUITE ÉTALON' : 'FUITE ÉTALON' });
  }
  cols.push({ key: 'pression_entree', label: "PRESSION D'ENTRÉE AFFICHÉE (EN BAR)" });
  cols.push({ key: 'dp_affichee', label: store.entete.machineCode?.includes('BEE47') ? 'FUITE AFFICHÉE (EN PA)' : 'ΔP AFFICHÉE (EN PA)' });
  cols.push({ key: 'resultat', label: 'RÉSULTAT (C/NC)' });
  cols.push({ key: 'observation', label: 'OBSERVATION EN CAS DE NON-CONFORMITÉ' });
  return cols;
});



// Computed : est-ce qu'on affiche les en-têtes de familles ?
const hasFamilleHeaders = computed(() => store.entete.afficheFamilles && store.familles.length > 0);
const hasSubHeaders = computed(() => hasFamilleHeaders.value || isBEEMachine.value);



// --- Rowspan Helpers ---
const getLigneTotalRows = (ligne) => {
    return ligne.groups.reduce((total, group) => total + group.rows.length, 0);
};

// Helper for grouping array by key
const groupBy = (array, keyGetter) => {
  return array.reduce((acc, item) => {
    const key = keyGetter(item);
    if (!acc[key]) acc[key] = [];
    acc[key].push(item);
    return acc;
  }, {});
};

// --- Architecture Handling ---
const flattenedRowsRisques = computed(() => {
  const rows = [];
  
  if (isArchitectureA.value) {
    // Architecture A: Risque -> Methode -> Periodicite (Visual grouping of Risk name only)
    const byRisque = groupBy(store.lignesRisques, l => l.libelleRisque || '');
    for (const [, lignes] of Object.entries(byRisque)) {
       const allTuplesRisque = [];

       lignes.forEach(ligne => {
          let mTuples = [];
          ligne.groups.forEach(group => {
             group.rows.forEach(r => {
                mTuples.push({ ligne, group, row: r });
             });
          });

          mTuples.forEach((t, i) => {
             t.isFirstMethode = i === 0;
             t.rowspanMethode = i === 0 ? mTuples.length : 0;
             allTuplesRisque.push(t);
          });
       });

       allTuplesRisque.forEach((t, i) => {
          t.isFirstRisque = i === 0;
          t.rowspanRisque = i === 0 ? allTuplesRisque.length : 0;
          
          const groupRows = t.group.rows;
          const rIdxInGroup = groupRows.findIndex(r => r._uid === t.row._uid);
          t.isFirstPerio = rIdxInGroup === 0;
          t.rowspanPerio = rIdxInGroup === 0 ? groupRows.length : 0;
          
          rows.push(t);
       });
    }
  } else {
    // Architecture B: Risque -> Methode -> Periodicite
    const byRisque = groupBy(store.lignesRisques, l => l.libelleRisque || '');
    for (const [, lignes] of Object.entries(byRisque)) {
       const allTuplesRisque = [];
       lignes.forEach(ligne => {
          let mTuples = [];
          ligne.groups.forEach(group => {
             let gTuples = [];
             group.rows.forEach(r => {
                const tuple = { ligne, group, row: r };
                gTuples.push(tuple);
                mTuples.push(tuple);
             });
             gTuples.forEach((t, i) => {
                t.isFirstPerio = i === 0;
                t.rowspanPerio = i === 0 ? gTuples.length : 0;
             });
          });
          mTuples.forEach((t, i) => {
             t.isFirstMethode = i === 0;
             t.rowspanMethode = i === 0 ? mTuples.length : 0;
             allTuplesRisque.push(t);
          });
       });
       allTuplesRisque.forEach((t, i) => {
          t.isFirstRisque = i === 0;
          t.rowspanRisque = i === 0 ? allTuplesRisque.length : 0;
          rows.push(t);
       });
    }
  }
  return rows;
});

// --- Propagation des changements de noms pour les groupes fusionnés ---
const onUpdateRisqueName = (oldValue, newValue) => {
  // On met à jour toutes les lignes qui avaient l'ancien nom
  store.lignesRisques.forEach(l => {
    if (l.libelleRisque === oldValue) {
      l.libelleRisque = newValue;
    }
  });
  
  // Idem pour la conformité si besoin (même si c'est plus rare)
  store.lignesConformite.forEach(l => {
    if (l.libelleRisque === oldValue) {
      l.libelleRisque = newValue;
    }
  });
};



// --- Fuite Étalon helpers ---
// Cherche FEC en priorité, sinon FENC (ex: FUI 24 = FEC, FUI 25 = FENC)
const getFuiteValue = (row) => {
  return store.getPieceValue(row, null, 'FEC') || store.getPieceValue(row, null, 'FENC');
};
// Détermine le rôle actuel (FEC ou FENC) pour la mise à jour
const getFuiteRole = (row) => {
  if (store.getPieceValue(row, null, 'FEC')) return 'FEC';
  if (store.getPieceValue(row, null, 'FENC')) return 'FENC';
  return 'FEC'; // Défaut pour les nouvelles sélections
};
const setFuiteValue = (row, pieceId) => {
  const role = getFuiteRole(row);
  // Déterminer le bon rôle selon le type de pièce sélectionnée
  const piece = [...store.fuitesEtalon].find(p => p.id === pieceId);
  const finalRole = piece?.typePiece || role;
  store.setPieceValue(row, null, 'FEC', null);  // Efface l'ancien FEC
  store.setPieceValue(row, null, 'FENC', null); // Efface l'ancien FENC
  if (pieceId) store.setPieceValue(row, null, finalRole, pieceId);
};

// ============================================================
// CRÉATION INLINE DE PIÈCE RÉFÉRENCE / ÉTALON FUITE
// ============================================================
const showAddPieceModal = ref(false);
const addPieceType = ref('PRC'); // 'PRC'|'PRNC' pour pièce réf, 'FEC'|'FENC' pour étalon
const addPieceContext = ref(null); // { row, familleCorpsId, role } pour savoir où affecter
const isCreatingPiece = ref(false);

const newPiece = ref({
  code: '',
  designation: '',
  machineCode: '',
});

const typePieceOptions = computed(() => {
  if (addPieceType.value === 'FEC' || addPieceType.value === 'FENC') {
    return [{ label: 'Fuite Étalon Conforme (FEC)', value: 'FEC' }, { label: 'Fuite Étalon Non Conforme (FENC)', value: 'FENC' }];
  }
  return [{ label: 'Pièce Réf. Conforme (PRC)', value: 'PRC' }, { label: 'Pièce Réf. Non Conforme (PRNC)', value: 'PRNC' }];
});

/**
 * Intercepte la sélection "__ADD__" dans un select de pièce référence.
 * @param {Event} event - L'événement de changement du select
 * @param {object} row - La ligne de données concernée
 * @param {string|null} familleCorpsId - L'ID famille corps (pour les matrices avec familles)
 * @param {string} role - 'PRC' ou 'PRNC'
 */
const onPieceSelectChange = (event, row, familleCorpsId, role) => {
  if (event.target.value === '__ADD__') {
    // Remettre la valeur précédente temporairement
    event.target.value = store.getPieceValue(row, familleCorpsId, role) || '';
    openAddPieceModal('piece', row, familleCorpsId, role);
  } else {
    store.setPieceValue(row, familleCorpsId, role, event.target.value);
  }
};

/**
 * Intercepte la sélection "__ADD__" dans un select d'étalon fuite.
 */
const onFuiteSelectChange = (event, row) => {
  if (event.target.value === '__ADD__') {
    event.target.value = getFuiteValue(row) || '';
    openAddPieceModal('fuite', row, null, getFuiteRole(row));
  } else {
    setFuiteValue(row, event.target.value);
  }
};

const openAddPieceModal = (mode, row, familleCorpsId, role) => {
  addPieceType.value = role;
  addPieceContext.value = { row, familleCorpsId, role };
  newPiece.value = { code: '', designation: '', machineCode: store.entete.machineCode || '' };
  showAddPieceModal.value = true;
};

const closeAddPieceModal = () => {
  showAddPieceModal.value = false;
  addPieceContext.value = null;
  newPiece.value = { code: '', designation: '', machineCode: '' };
};

const fileInput = ref(null);
const handleExcelImport = async (event) => {
  const file = event.target.files[0];
  if (!file) return;

  try {
    const result = await store.importerDepuisExcel(file);
    if (result.success) {
      toast.add({ severity: 'success', summary: 'Import Réussi', detail: 'La structure du plan a été importée avec succès.', life: 3000 });
      // On resynchronise le select machine si nécessaire
      selectedMachineCode.value = store.entete.machineCode;
    }
  } catch (error) {
    console.error("Erreur import Excel:", error);
    toast.add({ severity: 'error', summary: 'Échec de l\'import', detail: error.response?.data?.message || 'Une erreur est survenue lors de la lecture du fichier.', life: 5000 });
  } finally {
    // Reset input
    if (fileInput.value) fileInput.value.value = '';
  }
};

const confirmAddPiece = async () => {
  if (!newPiece.value.code.trim()) {
    toast.add({ severity: 'warn', summary: 'Code requis', detail: 'Veuillez saisir un code pour la pièce.', life: 3000 });
    return;
  }

  isCreatingPiece.value = true;
  try {
    const payload = {
      code: newPiece.value.code.trim().toUpperCase(),
      typePiece: addPieceType.value,
      designation: newPiece.value.designation.trim() || null,
      machineCode: newPiece.value.machineCode || null,
      familleDesc: null
    };

    const res = await apiClient.post('/referentiels/piece-reference', payload);
    const created = res.data.data;

    // Ajouter dans le store local selon le type
    const isFuite = ['FEC', 'FENC'].includes(addPieceType.value);
    if (isFuite) {
      store.fuitesEtalon.push(created);
    } else {
      store.piecesReference.push(created);
    }

    // Auto-sélectionner la nouvelle pièce dans la cellule concernée
    const ctx = addPieceContext.value;
    if (ctx) {
      store.setPieceValue(ctx.row, ctx.familleCorpsId, addPieceType.value, created.id);
    }

    toast.add({ severity: 'success', summary: 'Créé', detail: `"${created.code}" ajouté avec succès.`, life: 3000 });
    closeAddPieceModal();
  } catch (err) {
    const msg = err?.message || 'Erreur lors de la création.';
    toast.add({ severity: 'error', summary: 'Erreur', detail: msg, life: 4000 });
  } finally {
    isCreatingPiece.value = false;
  }
};

// --- Événements ---
const onMachineChange = async () => {
  if (selectedMachineCode.value) {
    const expectedCode = `FE-VM-${selectedMachineCode.value}`;
    const matchingRef = store.formulairesReferences.find(r => r.codeReference === expectedCode);
    if (matchingRef && refFormulaireSelected.value !== matchingRef.id) {
      // Auto-sélectionne le formulaire actif (ce qui déclenchera le watch et initialisera le plan avec les colonnes)
      refFormulaireSelected.value = matchingRef.id;
    } else {
      await store.initialiserPlan(selectedMachineCode.value);
    }
  } else {
    store.resetPlan();
    refFormulaireSelected.value = '';
  }
};



const emit = defineEmits(['saved']);
const onSauvegarder = async () => {
  if (store.isLoading) return;
  
  toast.removeAllGroups();
  // --- VALIDATION ---
  if (!store.entete.machineCode) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Veuillez sélectionner une machine.', life: 3000 });
    return;
  }
  if (!store.entete.nom || !store.entete.nom.trim()) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Le nom du plan est obligatoire.', life: 3000 });
    return;
  }

  const validateLignes = (lignes, sectionName) => {
    for (let i = 0; i < lignes.length; i++) {
      const l = lignes[i];
      const prefix = `${sectionName} (Ligne ${i + 1})`;
      
      if (!l.libelleRisque || !l.libelleRisque.trim()) {
        toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : Le libellé ${sectionName === 'Conformité' ? 'Test' : 'Risque'} est obligatoire.`, life: 4000 });
        return false;
      }
      if (!l.libelleMethode || !l.libelleMethode.trim()) {
        toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : Le Moyen/Méthode est obligatoire.`, life: 4000 });
        return false;
      }

      for (const group of l.groups) {
        if (!group.periodiciteId) {
          toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : La périodicité est obligatoire.`, life: 4000 });
          return false;
        }
        for (const row of group.rows) {
          const skipMoyenValidation = sectionName === 'Risque' && !isMachineSansConformite.value;
          if (store.entete.afficheMoyenDetectionRisques && !skipMoyenValidation && !row.refMoyenDetectionId) {
            toast.add({ severity: 'error', summary: 'Validation', detail: `${prefix} : Le moyen de détection est obligatoire.`, life: 4000 });
            return false;
          }
        }
      }
    }
    return true;
  };

  if (store.entete.afficheConformite && !isMachineSansConformite.value) {
    if (!validateLignes(store.lignesConformite, 'Conformité')) return;
  }
  if (!validateLignes(store.lignesRisques, 'Risque')) return;

  store.isLoading = true;
  try {
    if (!store.entete.id) {
      await store.fetchTousLesPlans();
      const planActif = (store.plansExistants || []).find(p => p.statut === 'ACTIF' && p.machineCode === selectedMachineCode.value);
      
      if (planActif) {
        const isConfirmed = await new Promise((resolve) => {
          confirm.require({
            message: `Un plan actif existe déjà pour la machine ${selectedMachineCode.value} (Version ${planActif.version}).\n\nVoulez-vous archiver le plan actif existant et activer ce nouveau plan (Version ${planActif.version + 1}) ?`,
            header: 'Plan Actif Existant',
            icon: 'ri-error-warning-line text-amber-500',
            acceptLabel: 'Oui, archiver',
            rejectLabel: 'Annuler',
            acceptClass: 'p-button-danger',
            accept: () => resolve(true),
            reject: () => resolve(false),
            onHide: () => resolve(false)
          });
        });
        
        if (!isConfirmed) return;
      }
    }

    const result = await store.sauvegarderPlanVerif();
    if (result.error) {
       toast.add({ severity: 'warn', summary: 'Attention', detail: result.error, life: 3000 });
       return;
    }
    emit('saved', result);
  } catch (err) {
    console.error('Erreur sauvegarde:', err);
    toast.removeAllGroups();
    
    const backendData = err?.response?.data;
    if (backendData?.details && Array.isArray(backendData.details) && backendData.details.length > 0) {
      backendData.details.slice(0, 2).forEach(detail => {
        toast.add({ severity: 'error', summary: 'Validation Serveur', detail, life: 5000 });
      });
      if (backendData.details.length > 2) {
        toast.add({ severity: 'warn', summary: "Plus d'erreurs", detail: `Et ${backendData.details.length - 2} autres problèmes détectés...`, life: 5000 });
      }
    } else {
      const msg = backendData?.message || 'Une erreur est survenue lors de la sauvegarde.';
      toast.add({ severity: 'error', summary: 'Erreur Serveur', detail: msg, life: 5000 });
    }
  } finally {
    store.isLoading = false;
  }
};

onMounted(async () => {
  try {
    await store.fetchDictionnaires();
    await store.fetchTousLesPlans();
  } catch {
    // Fallback data
  }
});
</script>

<style scoped>
textarea { resize: none; overflow: hidden; }
textarea:disabled { color: #334155; }
select:disabled { color: #334155; opacity: 1; -webkit-appearance: none; -moz-appearance: none; appearance: none; }
</style>

