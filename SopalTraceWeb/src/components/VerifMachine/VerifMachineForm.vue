<template>
  <div class="space-y-6 max-w-[1400px] mx-auto pb-20">
    <Toast />
    <ConfirmDialog />

    <!-- ============================================================ -->
    <!-- ÉTAPE 1 : SÉLECTION MACHINE                                   -->
    <!-- ============================================================ -->
    <section v-if="!props.isReadOnly" class="bg-white rounded-xl shadow-sm border border-slate-200 p-5">
      <h2 class="text-sm font-bold text-slate-700 mb-4 flex items-center gap-2 uppercase tracking-wide">
        <i class="ri-map-pin-line text-slate-500"></i> 1. Machine Concernée
      </h2>
      <div class="max-w-md">
        <label class="block text-xs font-bold text-slate-500 mb-1">Machine</label>
        <select v-model="selectedMachineCode" @change="onMachineChange" :disabled="props.isReadOnly || store.entete.id"
          class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-slate-500 focus:ring-1 focus:ring-slate-500 outline-none bg-slate-50 cursor-pointer disabled:opacity-75 disabled:bg-slate-100 font-semibold text-slate-700 transition-all">
          <option value="">-- Choisir une machine --</option>
          <option v-for="mac in store.machines" :key="mac.code" :value="mac.code">
            {{ mac.code }} - {{ mac.libelle }}
          </option>
        </select>
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
            <input v-model="store.entete.nom" type="text" :disabled="props.isReadOnly"
              class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:border-slate-900 font-semibold text-slate-800 outline-none disabled:bg-slate-50">
          </div>
          <!-- Gestion des Familles -->
          <div v-if="store.entete.afficheFamilles" class="bg-slate-50 p-3 rounded-lg border border-slate-200">
            <label class="block text-xs font-bold text-slate-700 mb-2">Familles (En-têtes colonnes)</label>
            <div v-if="!props.isReadOnly" class="flex gap-2 mb-3">
              <select v-model="nouvelleFamille" @change="onAjouterFamille" class="flex-1 border border-slate-300 rounded px-2 py-1.5 text-xs outline-none focus:border-slate-900 bg-white font-bold">
                <option value="">-- Choisir une famille pour l'ajouter --</option>
                <option v-for="fam in store.famillesCorps" :key="fam.id" :value="fam.id">{{ fam.libelle }}</option>
              </select>
            </div>
            <div class="flex flex-wrap gap-2">
              <span v-for="fam in store.familles" :key="fam.id"
                class="inline-flex items-center gap-1 bg-white border border-slate-300 text-slate-800 text-[11px] font-bold px-2 py-1 rounded shadow-sm">
                {{ fam.libelle }}
                <i v-if="!props.isReadOnly" @click="store.supprimerFamille(fam.id)" class="ri-close-circle-fill text-slate-400 hover:text-red-500 cursor-pointer"></i>
              </span>
            </div>
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
                  <th :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[18%]">
                    {{ (store.entete.afficheConformite && !isMachineSansConformite) ? 'Test de conformité' : 'Risque/ Défaut' }}
                  </th>
                  <th :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">Moyen/ Méthode de contrôle</th>
                  <th :colspan="(store.entete.afficheMoyenDetectionRisques && !isMachineSansConformite && store.entete.afficheConformite) ? 1 : (store.entete.afficheMoyenDetectionRisques ? (isMachineSansConformite ? 1 : 2) : 1)" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">Périodicité</th>
                  <th v-if="store.entete.afficheMoyenDetectionRisques && (isMachineSansConformite || (store.entete.afficheConformite && !isMachineSansConformite))" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[10%]">
                    {{ isSERMachine ? 'Moyen de contrôle' : 'Moyen de détection' }}
                  </th>
                  <template v-if="hasFamilleHeaders">
                    <th :colspan="store.familles.length" class="p-2 border-b border-r border-slate-700 bg-slate-800/80">
                      {{ isSERMachine ? 'N° moyen de contrôle' : 'Numéro de la pièce référence' }}
                    </th>
                  </template>
                  <th v-else class="p-3 border-r border-slate-700 w-[15%]">
                    {{ isSERMachine ? 'N° moyen de contrôle' : 'Numéro de la pièce référence' }}
                  </th>
                  <th v-if="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%]">Fuite Étalon</th>
                  <th v-if="!props.isReadOnly" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 w-16 text-slate-400">Actions</th>
                </tr>
                <tr v-if="hasFamilleHeaders">
                  <th v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-24">{{ fam.libelle }}</th>
                </tr>
            </thead>

            <!-- ======= SECTION CONFORMITÉ ======= -->
            <template v-if="store.entete.afficheConformite && !isMachineSansConformite">
              <tbody class="border-b-4 border-slate-400">
                <tr class="bg-slate-100/50 border-b border-slate-300">
                    <td :colspan="12" class="p-2 px-4 text-[10px] font-bold uppercase tracking-widest text-slate-600 bg-slate-200/50">
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
                        class="border-b border-slate-200 hover:bg-blue-50/20 transition-colors">
                      <!-- NIVEAU 1 : RISQUE (Rowspan global) -->
                      <td v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-300 align-top bg-white">
                        <textarea v-model="ligne.libelleRisque" rows="2" :disabled="props.isReadOnly"
                          class="w-full text-xs font-bold text-slate-900 border-transparent focus:border-slate-400 rounded p-1 outline-none resize-none disabled:bg-transparent"></textarea>
                      </td>
                      <td v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-300 align-top bg-white">
                        <textarea v-model="ligne.libelleMethode" rows="2" :disabled="props.isReadOnly"
                          class="w-full text-xs border-transparent focus:border-slate-400 rounded p-1 outline-none resize-none disabled:bg-transparent"></textarea>
                      </td>

                      <!-- NIVEAU 2 : PÉRIODICITÉ (Rowspan du groupe) -->
                      <td v-if="rIdx === 0" :rowspan="group.rows.length" class="p-3 border-r border-slate-300 bg-slate-100/30 align-top">
                        <div class="flex flex-col gap-2">
                            <div class="flex items-center gap-1">
                                <div v-if="props.isReadOnly" class="text-[11px] font-black uppercase text-slate-700 whitespace-normal leading-tight">
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

                      <!-- NIVEAU 3 : DÉTAILS -->
                      <td v-if="store.entete.afficheMoyenDetectionRisques" class="p-2 border-r border-slate-200">
                        <select v-model="row.refMoyenDetectionId" :disabled="props.isReadOnly" class="w-full text-xs text-center border-transparent rounded p-1 uppercase focus:border-slate-500 outline-none bg-transparent">
                          <option value="">--</option>
                          <option v-for="md in store.moyensDetection" :key="md.id" :value="md.id">{{ md.libelle }}</option>
                        </select>
                      </td>
                      <template v-if="hasFamilleHeaders">
                        <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-300 text-center">
                          <select :value="store.getPieceValue(row, fam.refFamilleCorpsId, 'PRC')"
                            @change="e => store.setPieceValue(row, fam.refFamilleCorpsId, 'PRC', e.target.value)"
                            :disabled="props.isReadOnly"
                            class="w-full text-xs text-center border border-slate-200 rounded p-1 uppercase text-slate-900 font-bold focus:border-slate-500 outline-none bg-white/50 disabled:border-transparent">
                            <option value="">--</option>
                            <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                          </select>
                        </td>
                      </template>
                      <template v-else>
                        <td class="p-2 border-r border-slate-300 text-center">
                          <select :value="store.getPieceValue(row, null, 'PRC')"
                            @change="e => store.setPieceValue(row, null, 'PRC', e.target.value)"
                            :disabled="props.isReadOnly"
                            class="w-full text-xs text-center border border-slate-200 rounded p-1 uppercase focus:border-slate-500 outline-none bg-white/50 disabled:border-transparent">
                            <option value="">--</option>
                            <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                          </select>
                        </td>
                      </template>
                      <td v-if="store.entete.afficheFuiteEtalon || isBEE22" class="p-2 border-r border-slate-300 bg-blue-50/30 text-center">
                        <select :value="getFuiteValue(row)"
                          @change="e => setFuiteValue(row, e.target.value)"
                          :disabled="props.isReadOnly"
                          class="w-full text-xs text-center border border-slate-200 rounded p-1 text-blue-900 font-bold focus:border-blue-500 uppercase outline-none bg-white/50 disabled:border-transparent">
                          <option value="">--</option>
                          <option v-for="pr in store.fuitesEtalon" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                        </select>
                      </td>

                      <!-- ACTIONS -->
                      <td v-if="!props.isReadOnly" class="p-2 text-center bg-white border-r border-slate-300">
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
                    <td :colspan="12" class="p-2 px-4 text-[10px] font-bold uppercase tracking-widest text-slate-600 bg-slate-200/50">
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
                    <td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[18%]">Risque/ Défaut</td>
                    <td :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[15%]">Moyen/ Méthode de contrôle</td>
                    <td :colspan="(store.entete.afficheMoyenDetectionRisques && !isMachineSansConformite) ? 2 : 1" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[12%]">Périodicité</td>
                    <td v-if="store.entete.afficheMoyenDetectionRisques && !isMachineSansConformite" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 border-r border-slate-700 w-[10%]">Moyen de détection</td>
                    <template v-if="hasFamilleHeaders">
                      <td :colspan="store.familles.length" class="p-2 border-b border-r border-slate-700 bg-slate-800/80">
                        {{ isSERMachine ? 'N° moyen de contrôle' : 'Numéro de la pièce de référence' }}
                      </td>
                    </template>
                    <td v-else class="p-3 border-r border-slate-700 w-[15%]">
                        {{ isSERMachine ? 'N° moyen de contrôle' : 'Numéro de la pièce de référence' }}
                    </td>
                    <td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-2 border-r border-slate-700 w-[10%]">Fuite Étalon</td>
                    <td v-if="!props.isReadOnly" :rowspan="hasFamilleHeaders ? 2 : 1" class="p-3 w-16 text-slate-400">Actions</td>
                  </tr>
                  <tr v-if="hasFamilleHeaders" class="bg-slate-900 text-white text-[11px] uppercase tracking-wider font-bold border-b border-slate-700 text-center">
                    <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-700 bg-slate-800/50 text-[10px] w-24">{{ fam.libelle }}</td>
                  </tr>
                </template>
                <template v-for="(ligne, lIdx) in store.lignesRisques" :key="ligne._uid">
                  <!-- SÉPARATEUR VISUEL ENTRE DEUX RISQUES/DÉFAUTS -->
                  <tr v-if="lIdx > 0">
                    <td colspan="20" class="p-0" style="height: 6px; background: #cbd5e1; border-top: 2px solid #64748b;"></td>
                  </tr>
                  <template v-for="(group, gIdx) in ligne.groups" :key="group._uid">
                    <tr v-for="(row, rIdx) in group.rows" :key="row._uid" 
                        class="border-b border-slate-200 hover:bg-rose-50/20 transition-colors">
                      <!-- NIVEAU 1 : RISQUE -->
                      <td v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-300 align-top bg-white border-l-4 border-l-slate-900">
                        <textarea v-model="ligne.libelleRisque" rows="3" :disabled="props.isReadOnly"
                          class="w-full text-xs font-bold text-slate-900 border-transparent focus:border-slate-500 outline-none rounded p-1 resize-none disabled:bg-transparent"></textarea>
                      </td>
                      <td v-if="gIdx === 0 && rIdx === 0" :rowspan="getLigneTotalRows(ligne)" class="p-2 border-r border-slate-300 align-top bg-white">
                        <textarea v-model="ligne.libelleMethode" rows="3" :disabled="props.isReadOnly"
                          class="w-full text-xs border-transparent focus:border-slate-500 outline-none rounded p-1 resize-none disabled:bg-transparent"></textarea>
                      </td>

                      <!-- NIVEAU 2 : PÉRIODICITÉ -->
                      <td v-if="rIdx === 0" :rowspan="group.rows.length" :colspan="(store.entete.afficheMoyenDetectionRisques && !isMachineSansConformite) ? 2 : 1" class="p-3 border-r border-slate-300 bg-slate-100/30 align-top">
                        <div class="flex flex-col gap-2">
                            <div class="flex items-center gap-1">
                                <div v-if="props.isReadOnly" class="text-[11px] font-black uppercase text-slate-700 whitespace-normal leading-tight">
                                    {{ store.periodicites.find(p => p.id === group.periodiciteId)?.libelle || '--' }}
                                </div>
                                <select v-else v-model="group.periodiciteId" class="w-full text-[10px] font-black uppercase border border-slate-400 rounded px-2 py-1.5 outline-none focus:border-slate-600 bg-white shadow-sm transition-all">
                                    <option value="">-- PERIODE --</option>
                                    <option v-for="p in store.periodicites" :key="p.id" :value="p.id">{{ p.libelle }}</option>
                                </select>
                                <button v-if="!props.isReadOnly" @click="store.ajouterGroupPeriodicite(ligne)" class="text-blue-600 hover:text-blue-800" title="Ajouter une autre période">
                                    <i class="ri-add-circle-fill text-xl"></i>
                                </button>
                            </div>
                        </div>
                      </td>

                      <!-- NIVEAU 3 : DÉTAILS -->
                      <td v-if="store.entete.afficheMoyenDetectionRisques && isMachineSansConformite" class="p-2 border-r border-slate-200">
                        <select v-model="row.refMoyenDetectionId" :disabled="props.isReadOnly" class="w-full text-xs text-center border-transparent rounded p-1 uppercase focus:border-slate-500 outline-none bg-transparent">
                          <option value="">--</option>
                          <option v-for="md in store.moyensDetection" :key="md.id" :value="md.id">{{ md.libelle }}</option>
                        </select>
                      </td>
                      <template v-if="hasFamilleHeaders">
                        <td v-for="fam in store.familles" :key="fam.id" class="p-2 border-r border-slate-300 text-center">
                          <select :value="store.getPieceValue(row, fam.refFamilleCorpsId, 'PRC')"
                            @change="e => store.setPieceValue(row, fam.refFamilleCorpsId, 'PRC', e.target.value)"
                            :disabled="props.isReadOnly"
                            class="w-full text-xs text-center border border-slate-200 rounded p-1 uppercase text-slate-900 font-bold focus:border-slate-500 outline-none bg-white/50 disabled:border-transparent">
                            <option value="">--</option>
                            <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                          </select>
                        </td>
                      </template>
                      <template v-else>
                        <td class="p-2 border-r border-slate-200 text-center">
                          <select :value="store.getPieceValue(row, null, 'PRC')"
                            @change="e => store.setPieceValue(row, null, 'PRC', e.target.value)"
                            :disabled="props.isReadOnly"
                            class="w-full text-xs text-center border rounded p-1 uppercase focus:border-slate-500 outline-none bg-transparent disabled:border-transparent">
                            <option value="">--</option>
                            <option v-for="pr in store.piecesReference" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                          </select>
                        </td>
                      </template>
                      <td v-if="store.entete.afficheFuiteEtalon || isBEEMachine" class="p-2 border-r border-slate-200 bg-amber-50/10 text-center">
                        <select :value="getFuiteValue(row)"
                          @change="e => setFuiteValue(row, e.target.value)"
                          :disabled="props.isReadOnly"
                          class="w-full text-xs text-center border rounded p-1 text-slate-900 focus:border-slate-500 uppercase outline-none bg-transparent disabled:border-transparent">
                          <option value="">--</option>
                          <option v-for="pr in store.fuitesEtalon" :key="pr.id" :value="pr.id">{{ pr.code }}</option>
                        </select>
                      </td>

                      <!-- ACTIONS -->
                      <td v-if="!props.isReadOnly" class="p-2 text-center bg-white">
                        <div class="flex items-center justify-center gap-1">
                          <button @click="store.ajouterRowDetail(group)" class="text-emerald-600 hover:text-emerald-800 p-0.5" title="Ajouter Moyen/Fuite sous cette période">
                            <i class="ri-add-line text-lg"></i>
                          </button>
                          <button v-if="group.rows.length > 1 || ligne.groups.length > 1" @click="store.supprimerRowDetail(group, row._uid)" class="text-slate-300 hover:text-red-500 p-0.5">
                            <i class="ri-indeterminate-circle-line text-lg"></i>
                          </button>
                          <button v-if="gIdx === 0 && rIdx === 0" @click="store.supprimerLigne(ligne._uid, 'RISQUE')" class="text-slate-300 hover:text-red-600 p-0.5" title="Supprimer tout le risque">
                            <i class="ri-delete-bin-fill text-lg"></i>
                          </button>
                        </div>
                      </td>
                    </tr>
                  </template>
                </template>
              </tbody>
          </table>
        </div>
      </section>

      <!-- ============================================================ -->
      <!-- BARRE D'ACTIONS                                              -->
      <!-- ============================================================ -->
      <div v-if="!props.isReadOnly" class="bg-slate-50 border-t border-slate-200 p-6 flex justify-end mt-6 rounded-b-xl">
        <EditorActions 
          :label="store.entete.id ? 'Enregistrer les Modifications' : 'Créer le Plan'"
          loading-label="Enregistrement..."
          :icon="store.entete.id ? 'pi pi-save' : 'pi pi-plus'"
          variant="primary"
          :is-loading="store.isLoading"
          @submit="onSauvegarder"
          @cancel="onCancel"
        />
      </div>

    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useVerifMachineStore } from '@/stores/verifMachineStore';
import EditorActions from '@/components/Shared/EditorActions.vue';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';

const props = defineProps({
  isReadOnly: { type: Boolean, default: false }
});

const store = useVerifMachineStore();
const confirm = useConfirm();
const toast = useToast();
const router = useRouter();

const onCancel = () => {
  router.push('/dev/hub');
};

const selectedMachineCode = ref('');

// Synchroniser le code machine local avec le store
watch(() => store.entete.machineCode, (newVal) => {
  if (newVal) selectedMachineCode.value = newVal;
}, { immediate: true });

const isMachineSansConformite = computed(() => {
  if (!store.entete.machineCode) return false;
  const code = store.entete.machineCode.toUpperCase().replace('-', '').replace(' ', '').trim();
  return code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47') || code.includes('MAS19') || code.startsWith('SER');
});

watch(isMachineSansConformite, (newVal) => {
  if (newVal) {
    store.entete.afficheConformite = false;
    // Vider aussi les lignes locales pour être sûr
    store.lignesConformite = [];
  }
}, { immediate: true });

const isSERMachine = computed(() => {
  if (!store.entete.machineCode) return false;
  return store.entete.machineCode.toUpperCase().startsWith('SER');
});

const isBEEMachine = computed(() => {
  if (!store.entete.machineCode) return false;
  const code = store.entete.machineCode.toUpperCase().replace('-', '').replace(' ', '');
  return code.includes('BEE22') || code.includes('BEE46') || code.includes('BEE47');
});

const nouvelleFamille = ref('');

// Computed : est-ce qu'on affiche les en-têtes de familles ?
const hasFamilleHeaders = computed(() => store.entete.afficheFamilles && store.familles.length > 0);

// --- Rowspan Helpers ---
const getLigneTotalRows = (ligne) => {
    return ligne.groups.reduce((total, group) => total + group.rows.length, 0);
};

// --- Fuite Étalon helpers ---
const getFuiteValue = (row) => store.getPieceValue(row, null, 'FEC');
const setFuiteValue = (row, value) => store.setPieceValue(row, null, 'FEC', value);

// --- Événements ---
const onMachineChange = () => {
  if (selectedMachineCode.value) store.initialiserPlan(selectedMachineCode.value);
  else store.resetPlan();
};

const onAjouterFamille = () => {
  if (nouvelleFamille.value) {
    store.ajouterFamille(nouvelleFamille.value);
    nouvelleFamille.value = '';
  }
};

const emit = defineEmits(['saved']);
const onSauvegarder = async () => {
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

  if (!store.entete.id) {
    // S'assurer qu'on a bien la dernière version de la base de données
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

  try {
    const result = await store.sauvegarderPlanVerif();
    toast.add({ severity: 'success', summary: 'Succès', detail: 'Plan enregistré avec succès.', life: 3000 });
    emit('saved', result);
  } catch (err) {
    console.error('Erreur sauvegarde:', err);
    toast.removeAllGroups();
    
    // Si le backend renvoie des erreurs de validation (400)
    const backendData = err?.response?.data;
    if (backendData?.details && Array.isArray(backendData.details) && backendData.details.length > 0) {
      // On affiche seulement les 2 premières erreurs pour ne pas inonder l'écran
      backendData.details.slice(0, 2).forEach(detail => {
        toast.add({ severity: 'error', summary: 'Validation Serveur', detail, life: 5000 });
      });
      if (backendData.details.length > 2) {
        toast.add({ severity: 'warn', summary: 'Plus d\'erreurs', detail: `Et ${backendData.details.length - 2} autres problèmes détectés...`, life: 5000 });
      }
    } else {
      const msg = backendData?.message || 'Une erreur est survenue lors de la sauvegarde.';
      toast.add({ severity: 'error', summary: 'Erreur Serveur', detail: msg, life: 5000 });
    }
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
