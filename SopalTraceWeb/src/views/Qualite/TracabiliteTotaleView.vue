<template>
  <div class="min-h-screen bg-slate-50 flex flex-col p-6 animate-fade-in">
    <EcranEnConstruction />
    <!-- En-tête / Barre de recherche -->
    <div class="mb-8 flex flex-col items-center">
      <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mb-4 shadow-sm relative">
        <div class="absolute inset-0 rounded-full border-4 border-blue-500 border-t-transparent animate-spin" v-if="isSearching"></div>
        <i class="pi pi-box text-3xl text-blue-600 relative z-10"></i>
      </div>
      <h1 class="text-3xl font-black text-slate-800 mb-2">Traçabilité Totale</h1>
      <p class="text-slate-500 mb-6 text-center max-w-lg">
        Recherchez un numéro d'Ordre de Fabrication (OF) pour consulter l'intégralité du dossier documentaire associé en cascade.
      </p>

      <div class="relative w-full max-w-2xl group">
        <div class="absolute inset-y-0 left-0 flex items-center pl-4 pointer-events-none">
          <i class="pi pi-search text-slate-400 group-focus-within:text-blue-500 transition-colors"></i>
        </div>
        <input 
          v-model="searchQuery" 
          @keyup.enter="handleSearch"
          type="text" 
          class="w-full pl-12 pr-32 py-4 rounded-2xl border-2 border-slate-200 bg-white text-lg font-bold text-slate-800 shadow-sm focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-300 placeholder:font-normal"
          placeholder="Saisir le N° OF (ex: OF2607ASS02)..."
        >
        <button 
          @click="handleSearch"
          class="absolute inset-y-2 right-2 bg-blue-600 hover:bg-blue-700 text-white font-bold px-6 rounded-xl transition-all hover:shadow-lg active:scale-95 flex items-center gap-2 border-0 cursor-pointer"
        >
          <span>Tracer</span>
          <i class="pi pi-arrow-right text-sm"></i>
        </button>
      </div>

      <!-- Informations des lots (affichées après recherche) -->
      <div v-if="hasResults" class="mt-6 flex flex-wrap gap-4 text-sm font-bold animate-fade-in justify-center w-full max-w-2xl">
        <div class="bg-violet-100 text-violet-700 px-5 py-2.5 rounded-xl border border-violet-200 shadow-sm flex items-center gap-2">
          <i class="pi pi-barcode"></i> N° Lot MP : <span class="text-violet-900">24120004</span>
        </div>
        <div class="bg-blue-100 text-blue-700 px-5 py-2.5 rounded-xl border border-blue-200 shadow-sm flex items-center gap-2">
          <i class="pi pi-box"></i> N° Lot PF : <span class="text-blue-900">25011500</span>
        </div>
      </div>
    </div>

    <!-- État de chargement (Simulateur de branches) -->
    <div v-if="isSearching" class="flex-1 flex flex-col items-center justify-center py-12">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-8 w-full max-w-4xl mb-8">
        <!-- Branche 1 -->
        <div class="flex flex-col items-center text-center animate-pulse-fast">
          <div class="w-12 h-12 rounded-full bg-violet-100 flex items-center justify-center mb-3">
            <i class="pi pi-truck text-xl text-violet-600"></i>
          </div>
          <span class="text-sm font-bold text-slate-600">Branche Fournisseurs...</span>
        </div>
        <!-- Branche 2 -->
        <div class="flex flex-col items-center text-center animate-pulse-fast" style="animation-delay: 150ms;">
          <div class="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center mb-3">
            <i class="pi pi-cog text-xl text-blue-600"></i>
          </div>
          <span class="text-sm font-bold text-slate-600">Branche Interne...</span>
        </div>
        <!-- Branche 3 -->
        <div class="flex flex-col items-center text-center animate-pulse-fast" style="animation-delay: 300ms;">
          <div class="w-12 h-12 rounded-full bg-emerald-100 flex items-center justify-center mb-3">
            <i class="pi pi-send text-xl text-emerald-600"></i>
          </div>
          <span class="text-sm font-bold text-slate-600">Branche Clients...</span>
        </div>
      </div>
      <div class="text-slate-400 font-medium">Analyse en cascade en cours...</div>
    </div>

    <!-- Dashboard de Traçabilité -->
    <div v-else-if="hasResults" class="flex-1 grid grid-cols-1 lg:grid-cols-3 gap-6 animate-fade-in-up pb-10">
      
      <!-- Colonne 1: Fournisseurs -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden group flex flex-col max-h-[800px] overflow-y-auto">
        <div class="absolute inset-0 bg-gradient-to-br from-violet-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-violet-100 text-violet-600 flex items-center justify-center shrink-0">
            <i class="pi pi-truck text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Fournisseurs & Certificat</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Amont</p>
          </div>
        </div>
        
        <div class="flex flex-col gap-5 relative z-10">
          <!-- Section MP -->
          <div>
            <h3 @click="toggleSection('mp')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><i class="pi pi-box"></i> Matière Première (MP)</span>
              <i :class="expandedSections.mp ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.mp" class="flex flex-col gap-3">
              <DocumentCard 
                title="Certificat Conformité.pdf" 
                subtitle="Laiton CuZn39Pb3" 
                category="Certif MP" 
                icon="pi pi-file-pdf" 
                status="disponible"
                imagePath="/certificat_mp.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Rapport Contrôle MP.pdf" 
                subtitle="Analyse spectro" 
                category="Rapport MP" 
                icon="pi pi-file-pdf" 
                status="disponible"
                imagePath="/rapport_mp.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande FRS.pdf" 
                subtitle="Introuvable pour cette commande" 
                category="BC Frs" 
                icon="pi pi-file-pdf" 
                status="manquant"
                @click="handleDocClick"
              />
            </div>
          </div>
          
          <div class="h-px bg-slate-200"></div>

          <!-- Section Accessoire: Ressort -->
          <div>
            <h3 @click="toggleSection('acc_ressort')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><i class="pi pi-paperclip"></i> Accessoire : Ressort</span>
              <i :class="expandedSections.acc_ressort ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.acc_ressort" class="flex flex-col gap-3">
              <DocumentCard 
                title="Certificat d'origine.pdf" 
                subtitle="Ressort INOX 304" 
                category="Certif Acc" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Rapport Contrôle Réception.pdf" 
                subtitle="Réf: 11022" 
                category="Rapport" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande FRS.pdf" 
                subtitle="Cmd: SOPCAC-2410100" 
                category="BC Frs" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>

          <div class="h-px bg-slate-200"></div>

          <!-- Section Accessoire: Joint -->
          <div>
            <h3 @click="toggleSection('acc_joint')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><i class="pi pi-paperclip"></i> Accessoire : Joint Torique</span>
              <i :class="expandedSections.acc_joint ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.acc_joint" class="flex flex-col gap-3">
              <DocumentCard 
                title="Certificat Matière Joint.pdf" 
                subtitle="Introuvable pour ce lot" 
                category="Certificat" 
                icon="pi pi-file-check" 
                status="manquant"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Rapport Contrôle Réception.pdf" 
                subtitle="Réf: 11055" 
                category="Rapport" 
                icon="pi pi-chart-bar" 
                status="disponible"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande FRS.pdf" 
                subtitle="Cmd: BC-ACC-502" 
                category="BC Frs" 
                icon="pi pi-file-pdf" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Colonne 2: Interne / Production -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden group flex flex-col max-h-[800px] overflow-y-auto custom-scrollbar">
        <div class="absolute inset-0 bg-gradient-to-br from-blue-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-blue-100 text-blue-600 flex items-center justify-center shrink-0">
            <i class="pi pi-cog text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Production Sopal</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Interne</p>
          </div>
        </div>

        <div class="flex flex-col gap-5 relative z-10">
          <!-- ESTAMPAGE -->
          <div>
            <h3 @click="toggleSection('estomp')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><span class="w-2 h-2 rounded-full bg-orange-400"></span> Estampage (ESTOMP)</span>
              <i :class="expandedSections.estomp ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.estomp" class="flex flex-col gap-3">
              <DocumentCard 
                title="Plan de Fabrication" 
                subtitle="Code: PLAN-FAB-2511-EST" 
                category="Plan (DI)" 
                icon="pi pi-sitemap" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>

          <div class="h-px bg-slate-200"></div>

          <!-- TRONCONNAGE -->
          <div>
            <h3 @click="toggleSection('trn')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><span class="w-2 h-2 rounded-full bg-rose-400"></span> Tronçonnage (TRN)</span>
              <i :class="expandedSections.trn ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.trn" class="flex flex-col gap-3">
              <DocumentCard 
                title="Plan de Fabrication" 
                subtitle="Code: PLAN-FAB-2511-TRN" 
                category="Plan (DI)" 
                icon="pi pi-sitemap" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>

          <div class="h-px bg-slate-200"></div>

          <!-- USINAGE -->
          <div>
            <h3 @click="toggleSection('usi')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><span class="w-2 h-2 rounded-full bg-cyan-400"></span> Usinage (USI)</span>
              <i :class="expandedSections.usi ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.usi" class="flex flex-col gap-3">
              <DocumentCard 
                title="Plan de Fabrication" 
                subtitle="Code: PLAN-FAB-2511-USI" 
                category="Plan (DI)" 
                icon="pi pi-sitemap" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>

          <div class="h-px bg-slate-200"></div>

          <!-- ASSEMBLAGE -->
          <div>
            <h3 @click="toggleSection('ass')" class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center justify-between cursor-pointer hover:text-slate-600 transition-colors">
              <span class="flex items-center gap-2"><span class="w-2 h-2 rounded-full bg-blue-500"></span> Assemblage (ASS)</span>
              <i :class="expandedSections.ass ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
            </h3>
            <div v-show="expandedSections.ass" class="flex flex-col gap-3">
              <DocumentCard 
                title="Fiche d'Échantillonnage" 
                subtitle="Contrôle Assemblage Final" 
                category="Réponses" 
                icon="pi pi-check-square" 
                status="disponible"
                imagePath="/src/assets/images/fiche_echantillonnage.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Registre de Traçabilité" 
                subtitle="Validé par : O. Majdi" 
                category="Registre" 
                icon="pi pi-list" 
                status="disponible"
                imagePath="/src/assets/images/registre_tracabilite.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Plan d'Assemblage v4" 
                subtitle="Code: PLAN-ASS-SOUPAPE" 
                category="Document Spécifique" 
                icon="pi pi-sitemap" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Colonne 3: Clients / Expédition -->
      <div class="bg-white/60 backdrop-blur-xl border border-slate-200 rounded-3xl p-6 shadow-sm relative overflow-hidden group flex flex-col max-h-[800px] overflow-y-auto">
        <div class="absolute inset-0 bg-gradient-to-br from-emerald-50/50 to-transparent pointer-events-none"></div>
        <div class="flex items-center gap-3 mb-6 relative z-10 shrink-0">
          <div class="w-10 h-10 rounded-lg bg-emerald-100 text-emerald-600 flex items-center justify-center shrink-0">
            <i class="pi pi-send text-lg"></i>
          </div>
          <div>
            <h2 class="text-lg font-black text-slate-800 m-0">Expédition & Clients</h2>
            <p class="text-xs text-slate-500 m-0">Chaîne Aval</p>
          </div>
        </div>

        <div class="flex flex-col gap-5 relative z-10">
          <div>
            <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3">Bons de Livraison (BL)</h3>
            <div class="flex flex-col gap-3">
              <DocumentCard 
                title="Bon de Livraison BL-2409-55.pdf" 
                subtitle="Client: Magasin Central Sfax" 
                category="BL" 
                icon="pi pi-file-export" 
                status="disponible"
                imagePath="/bl.png"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Livraison BL-2409-58.pdf" 
                subtitle="Client: Magasin Central Tunis" 
                category="BL" 
                icon="pi pi-file-export" 
                status="disponible"
                @click="handleDocClick"
              />
            </div>
          </div>

          <div class="h-px bg-slate-200"></div>
          
          <div>
            <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3">Bons de Commande Clients</h3>
            <div class="flex flex-col gap-3">
              <DocumentCard 
                title="Bon de Commande Client.pdf" 
                subtitle="Introuvable (BC-CLI-909)" 
                category="BC Client" 
                icon="pi pi-envelope" 
                status="manquant"
                @click="handleDocClick"
              />
              <DocumentCard 
                title="Bon de Commande Client.pdf" 
                subtitle="Introuvable (BC-CLI-915)" 
                category="BC Client" 
                icon="pi pi-envelope" 
                status="manquant"
                @click="handleDocClick"
              />
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- Modale de Visualisation Document -->
    <Dialog v-model:visible="showDocModal" modal :header="selectedDoc?.title" :style="{ width: selectedDoc?.imagePath || selectedDoc?.title === 'Plan de Fabrication' || selectedDoc?.title === 'Fiche d\'Échantillonnage' || selectedDoc?.title === 'Registre de Traçabilité' ? '80vw' : '50vw' }" class="p-fluid printable-modal">
      <div class="flex flex-col items-center justify-center p-4 text-center bg-slate-50 rounded-xl border border-slate-200 border-dashed min-h-[300px]" id="printable-area">
        
        <template v-if="selectedDoc?.title === 'Plan de Fabrication'">
          <div class="w-full text-left bg-white p-2 rounded shadow border border-slate-200">
            <h2 class="text-xl font-bold mb-4 text-slate-800 border-b pb-2 px-2">Plan de Contrôle Article : 2511A01</h2>
            <div class="overflow-x-auto">
              <table class="w-full text-[10px] border-collapse border border-slate-300">
                <thead class="bg-[#1f3864] text-white">
                  <tr>
                    <th class="p-2 border border-slate-400 text-left">Caractéristique contrôlée</th>
                    <th class="p-2 border border-slate-400">test</th>
                    <th class="p-2 border border-slate-400">Limite de spécification</th>
                    <th class="p-2 border border-slate-400">Type de contrôle</th>
                    <th class="p-2 border border-slate-400">Moyen de contrôle</th>
                    <th class="p-2 border border-slate-400">Code instrument de contrôle</th>
                    <th class="p-2 border border-slate-400">kkkkk</th>
                    <th class="p-2 border border-slate-400 text-left">Observations</th>
                  </tr>
                </thead>
                <tbody>
                  <!-- Section 1 -->
                  <tr class="bg-[#d9e1f2] text-[#1f3864] font-bold text-center">
                    <td colspan="8" class="p-2 border border-slate-300">Caractéristiques à contrôler au réglage et en cours de production 60 pièces / heure</td>
                  </tr>
                  <tr class="text-center text-slate-700 bg-white">
                    <td class="p-2 border border-slate-300 text-left font-semibold">M23.2 x 2</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300">Attribut</td>
                    <td class="p-2 border border-slate-300">Bague filetée</td>
                    <td class="p-2 border border-slate-300 font-mono">OT850060068-001</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-left italic">Respecter Ø L</td>
                  </tr>
                  <tr class="text-center text-slate-700 bg-slate-50">
                    <td class="p-2 border border-slate-300 text-left font-semibold">M21.7 x14F g</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300">Attribut</td>
                    <td class="p-2 border border-slate-300">B. filetée P</td>
                    <td class="p-2 border border-slate-300 font-mono">OT850060001-001</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-left italic">Respecter Ø L<br>(4 filets complet)</td>
                  </tr>
                  <tr class="text-center text-slate-700 bg-white">
                    <td class="p-2 border border-slate-300 text-left font-semibold">Ø13</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300">0 ; 0.1</td>
                    <td class="p-2 border border-slate-300">Attribut</td>
                    <td class="p-2 border border-slate-300">Tampon lisse</td>
                    <td class="p-2 border border-slate-300 font-mono">OT850010068-001</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-left"></td>
                  </tr>
                  <tr class="text-center text-slate-700 bg-slate-50">
                    <td class="p-2 border border-slate-300 text-left font-semibold">Ø17.5</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300">+0.1 ; 0</td>
                    <td class="p-2 border border-slate-300">Mesure</td>
                    <td class="p-2 border border-slate-300">P à C</td>
                    <td class="p-2 border border-slate-300 font-mono">PAC***</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-left text-red-600 font-bold">!!Pas de vibration</td>
                  </tr>
                  
                  <!-- Section 2 -->
                  <tr class="bg-[#d9e1f2] text-[#1f3864] font-bold text-center">
                    <td colspan="8" class="p-2 border border-slate-300">Caractéristiques à contrôler au réglage et en cours de production 30 pièces / heure</td>
                  </tr>
                  <tr class="text-center text-slate-700 bg-white">
                    <td class="p-2 border border-slate-300 text-left font-semibold">Ra (=0.8 )</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300">X = 0.8</td>
                    <td class="p-2 border border-slate-300">Mesure</td>
                    <td class="p-2 border border-slate-300">Rugosimetre</td>
                    <td class="p-2 border border-slate-300 font-mono">RGM 01</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-left"></td>
                  </tr>

                  <!-- Section 3 -->
                  <tr class="bg-[#d9e1f2] text-[#1f3864] font-bold text-center">
                    <td colspan="8" class="p-2 border border-slate-300">Caractéristiques à contrôler aux réglages 60 pièces / heure</td>
                  </tr>
                  <tr class="text-center text-slate-700 bg-slate-50">
                    <td class="p-2 border border-slate-300 text-left font-semibold">14.1</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300">±0.1</td>
                    <td class="p-2 border border-slate-300">Attribut</td>
                    <td class="p-2 border border-slate-300">Calibre position</td>
                    <td class="p-2 border border-slate-300 font-mono">OT850100029-001</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-left"></td>
                  </tr>
                  <tr class="text-center text-slate-700 bg-white">
                    <td class="p-2 border border-slate-300 text-left font-semibold">33</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300">±0.3</td>
                    <td class="p-2 border border-slate-300">Mesure</td>
                    <td class="p-2 border border-slate-300">P à C</td>
                    <td class="p-2 border border-slate-300 font-mono">PAC*</td>
                    <td class="p-2 border border-slate-300 text-slate-400">-</td>
                    <td class="p-2 border border-slate-300 text-left"></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>

        <template v-else-if="selectedDoc?.title === 'Fiche d\'Échantillonnage'">
          <div class="w-full text-left bg-transparent rounded-lg">
            
            <!-- INFORMATIONS GÉNÉRALES -->
            <div class="mb-4 bg-white rounded-lg shadow-sm border border-slate-200 overflow-hidden">
              <div class="bg-slate-800 text-white font-bold px-4 py-2 text-xs flex items-center gap-2">
                <i class="pi pi-file-o"></i> INFORMATIONS GÉNÉRALES
              </div>
              <div class="grid grid-cols-3 gap-4 text-xs p-4">
                <div class="flex flex-col gap-2">
                  <div><span class="font-bold text-slate-700">Code article :</span> 2511A01-1</div>
                  <div><span class="font-bold text-slate-700">Atelier :</span> ASS</div>
                  <div><span class="font-bold text-slate-700">Poste :</span> PAS71</div>
                </div>
                <div class="flex flex-col gap-2">
                  <div><span class="font-bold text-slate-700">Désignation :</span> Produit Assemblage 2511</div>
                  <div><span class="font-bold text-slate-700">Date de fabrication :</span> 2026-09-03</div>
                  <div><span class="font-bold text-slate-700">Code instrument de mesure :</span> BAN59</div>
                </div>
                <div class="flex flex-col gap-2">
                  <div><span class="font-bold text-slate-700">Numéro de l'OF :</span> OF2607ASS01</div>
                  <div><span class="font-bold text-slate-700">Date de l'échantillonnage :</span> 2026-09-03 13:50</div>
                </div>
              </div>
            </div>

            <!-- PARAMÈTRES DU PLAN -->
            <div class="mb-4 bg-white rounded-lg shadow-sm border border-slate-200 overflow-hidden">
              <div class="bg-slate-800 text-white font-bold px-4 py-2 text-xs flex items-center gap-2">
                <i class="pi pi-sliders-h"></i> PARAMÈTRES DU PLAN
              </div>
              <div class="grid grid-cols-4 gap-6 text-xs p-4 items-end">
                <div>
                  <div class="font-bold text-slate-500 uppercase text-[10px] mb-2 tracking-wider">Niveau de contrôle</div>
                  <div class="flex border border-slate-200 rounded overflow-hidden text-center bg-slate-50 text-slate-400 font-bold text-[10px]">
                    <div class="flex-1 py-1 px-2 border-r border-slate-200">NIVEAU I</div>
                    <div class="flex-1 py-1 px-2 bg-emerald-600 text-white border-r border-slate-200 shadow-inner">NIVEAU II</div>
                    <div class="flex-1 py-1 px-2">NIVEAU III</div>
                  </div>
                </div>
                <div>
                  <div class="font-bold text-slate-500 uppercase text-[10px] mb-2 tracking-wider">Plan d'échantillonnage</div>
                  <div class="flex border border-slate-200 rounded overflow-hidden text-center bg-slate-50 text-slate-400 font-bold text-[10px]">
                    <div class="flex-1 py-1 px-2 bg-emerald-600 text-white border-r border-slate-200 shadow-inner">SIMPLE</div>
                    <div class="flex-1 py-1 px-2">DOUBLE</div>
                  </div>
                </div>
                <div>
                  <div class="font-bold text-slate-500 uppercase text-[10px] mb-2 tracking-wider">Mode de contrôle</div>
                  <div class="flex border border-slate-200 rounded overflow-hidden text-center bg-slate-50 text-slate-400 font-bold text-[10px]">
                    <div class="flex-1 py-1 px-2 border-r border-slate-200">RÉDUIT</div>
                    <div class="flex-1 py-1 px-2 bg-emerald-600 text-white border-r border-slate-200 shadow-inner">NORMAL</div>
                    <div class="flex-1 py-1 px-2">RENFORCÉ</div>
                  </div>
                </div>
                <div>
                  <div class="font-bold text-slate-500 uppercase text-[10px] mb-2 tracking-wider">NQA</div>
                  <div class="border border-emerald-200 rounded py-1 px-4 text-center font-bold text-slate-800 bg-white">
                    0.65
                  </div>
                </div>
              </div>
            </div>

            <!-- TABLEAU DE DÉTERMINATION -->
            <div class="bg-white rounded-lg shadow-sm border border-slate-200 overflow-hidden">
              <div class="bg-slate-800 text-white font-bold px-4 py-2 text-xs flex items-center gap-2">
                <i class="pi pi-table"></i> TABLEAU DE DÉTERMINATION DE L'ÉCHANTILLON
              </div>
              <table class="w-full text-xs text-center border-collapse">
                <thead class="bg-slate-50 text-slate-600 font-bold uppercase text-[10px] border-b border-slate-200">
                  <tr>
                    <th class="p-3 border-r border-slate-200">N°</th>
                    <th class="p-3 border-r border-slate-200">EFFECTIF DU<br>LOT<br><span class="text-slate-400 font-normal">(TRANCHE)</span></th>
                    <th class="p-3 border-r border-slate-200">LETTRE<br>CODE</th>
                    <th class="p-3 border-r border-slate-200">ÉCHANTILLON GLOBAL<br><span class="text-slate-400 font-normal">(A)</span></th>
                    <th class="p-3 border-r border-slate-200">NB POSTES<br><span class="text-slate-400 font-normal">(B)</span></th>
                    <th class="p-3 border-r border-slate-200">ÉCHANTILLON /POSTE<br><span class="text-slate-400 font-normal">(A/B)</span></th>
                    <th class="p-3 border-r border-slate-200 text-emerald-600">CRITÈRE<br>D'ACCEPTATION</th>
                    <th class="p-3 text-red-600">CRITÈRE DE<br>REJET</th>
                  </tr>
                </thead>
                <tbody>
                  <tr class="font-bold text-slate-700">
                    <td class="p-4 border-r border-slate-200 text-slate-400">1</td>
                    <td class="p-4 border-r border-slate-200 text-lg">1000</td>
                    <td class="p-4 border-r border-slate-200">J</td>
                    <td class="p-4 border-r border-slate-200">80</td>
                    <td class="p-4 border-r border-slate-200">1</td>
                    <td class="p-4 border-r border-slate-200">80</td>
                    <td class="p-4 border-r border-slate-200 text-emerald-600 text-lg">1</td>
                    <td class="p-4 text-red-600 text-lg">0</td>
                  </tr>
                </tbody>
              </table>
            </div>

          </div>
        </template>

        <template v-else-if="selectedDoc?.title === 'Registre de Traçabilité'">
          <div class="w-full text-left bg-white rounded-lg shadow-sm border border-slate-200 overflow-hidden">
            <div class="bg-slate-800 text-white font-bold px-4 py-3 text-sm flex items-center justify-between">
              <div class="flex items-center gap-2"><i class="pi pi-list"></i> REGISTRE DE TRAÇABILITÉ</div>
              <div class="text-xs font-normal">OF2607ASS01</div>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full text-xs text-center border-collapse">
                <thead class="bg-slate-50 text-slate-600 font-bold text-[10px] uppercase border-b-2 border-purple-500">
                  <tr>
                    <th class="p-3 text-left border-r border-slate-100">DATE / HEURE</th>
                    <th class="p-3 border-r border-slate-100">VERSION</th>
                    <th class="p-3 border-r border-slate-100 bg-blue-50/30 text-slate-500">SACHET ACCESSOIRES</th>
                    <th class="p-3 border-r border-slate-100 bg-blue-50/30 text-slate-500">RESSORT AUTO</th>
                    <th class="p-3 border-r border-slate-100 bg-blue-50/30 text-slate-500">MANETTE GAZ</th>
                    <th class="p-3 border-r border-slate-100 bg-yellow-50 text-slate-500">CORPS (USINÉ)</th>
                    <th class="p-3 border-r border-slate-100 bg-yellow-50 text-slate-500">VOLANT (USINÉ)</th>
                    <th class="p-3">ACTION</th>
                  </tr>
                </thead>
                <tbody class="text-slate-600">
                  <tr class="border-b border-slate-100 hover:bg-slate-50">
                    <td class="p-3 text-left font-mono">03/09/2026 14:34</td>
                    <td class="p-3"><span class="bg-purple-100 text-purple-700 font-bold px-2 py-1 rounded">V1</span></td>
                    <td class="p-3 text-emerald-600 font-bold font-mono border-l border-slate-100">LOT-ACC-4592</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-RES-8821</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-MAN-1093</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30 border-l border-slate-100">LOT-CORPS-302A</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30">LOT-VOLANT-7B</td>
                    <td class="p-3 border-l border-slate-100"><i class="pi pi-check-circle text-emerald-500 text-lg"></i></td>
                  </tr>
                  <tr class="border-b border-slate-100 hover:bg-slate-50">
                    <td class="p-3 text-left font-mono">03/09/2026 14:45</td>
                    <td class="p-3"><span class="bg-purple-100 text-purple-700 font-bold px-2 py-1 rounded">V2</span></td>
                    <td class="p-3 text-emerald-600 font-bold font-mono border-l border-slate-100">LOT-ACC-4592</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-RES-8821</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-MAN-1093</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30 border-l border-slate-100">LOT-CORPS-303A</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30">LOT-VOLANT-7B</td>
                    <td class="p-3 border-l border-slate-100"><i class="pi pi-check-circle text-emerald-500 text-lg"></i></td>
                  </tr>
                  <tr class="border-b border-slate-100 hover:bg-slate-50">
                    <td class="p-3 text-left font-mono">03/09/2026 15:12</td>
                    <td class="p-3"><span class="bg-purple-100 text-purple-700 font-bold px-2 py-1 rounded">V3</span></td>
                    <td class="p-3 text-emerald-600 font-bold font-mono border-l border-slate-100">LOT-ACC-4595</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-RES-8821</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-MAN-1093</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30 border-l border-slate-100">LOT-CORPS-303A</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30">LOT-VOLANT-7C</td>
                    <td class="p-3 border-l border-slate-100"><i class="pi pi-check-circle text-emerald-500 text-lg"></i></td>
                  </tr>
                  <tr class="hover:bg-slate-50">
                    <td class="p-3 text-left font-mono">03/09/2026 15:30</td>
                    <td class="p-3"><span class="bg-purple-100 text-purple-700 font-bold px-2 py-1 rounded">V4</span></td>
                    <td class="p-3 text-emerald-600 font-bold font-mono border-l border-slate-100">LOT-ACC-4595</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-RES-8825</td>
                    <td class="p-3 text-emerald-600 font-bold font-mono">LOT-MAN-1093</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30 border-l border-slate-100">LOT-CORPS-304A</td>
                    <td class="p-3 text-amber-700 font-bold font-mono bg-yellow-50/30">LOT-VOLANT-7C</td>
                    <td class="p-3 border-l border-slate-100"><i class="pi pi-check-circle text-emerald-500 text-lg"></i></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>

        <template v-else-if="selectedDoc?.imagePath && selectedDoc?.status === 'disponible'">
          <img :src="selectedDoc.imagePath" class="max-w-full max-h-[70vh] object-contain rounded shadow-sm border border-slate-200" alt="Document aperçu" />
        </template>
        
        <template v-else>
          <i :class="[selectedDoc?.icon || 'pi pi-file-pdf', 'text-5xl mb-4', selectedDoc?.status === 'disponible' ? 'text-blue-400' : 'text-red-400']"></i>
          <h3 class="text-xl font-bold text-slate-700 mb-2">
            {{ selectedDoc?.status === 'disponible' ? 'Aperçu du Document' : 'Document Introuvable' }}
          </h3>
          <p class="text-slate-500 max-w-sm">
            {{ selectedDoc?.status === 'disponible' 
              ? 'Le système chargerait ici le fichier PDF ou le composant Vue correspondant.' 
              : 'La recherche en cascade n\'a pas trouvé ce document avec la nomenclature stricte attendue.' }}
          </p>
        </template>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3 mt-4 w-full">
          <button v-if="selectedDoc?.status === 'disponible'" @click="printDoc" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg transition-colors border-0 cursor-pointer flex items-center shadow-sm">
            <i class="pi pi-print mr-2"></i> Imprimer
          </button>
          <button @click="showDocModal = false" class="px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-700 font-bold rounded-lg transition-colors border-0 cursor-pointer flex items-center">
            <i class="pi pi-times mr-2"></i> Fermer
          </button>
        </div>
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import EcranEnConstruction from '@/components/Shared/EcranEnConstruction.vue';
import { ref, reactive } from 'vue';
import DocumentCard from '@/components/Qualite/TracabiliteTotale/DocumentCard.vue';
import Dialog from 'primevue/dialog';

const expandedSections = reactive({
  mp: true,
  acc_ressort: false,
  acc_joint: false,
  estomp: false,
  trn: false,
  usi: false,
  ass: true
});

const toggleSection = (key) => {
  expandedSections[key] = !expandedSections[key];
};

const searchQuery = ref('');
const isSearching = ref(false);
const hasResults = ref(false);

const showDocModal = ref(false);
const selectedDoc = ref(null);

const handleSearch = () => {
  if (!searchQuery.value.trim()) return;
  
  isSearching.value = true;
  hasResults.value = false;

  // Simule le délai des 3 recherches parallèles
  setTimeout(() => {
    isSearching.value = false;
    hasResults.value = true;
  }, 2500);
};

const handleDocClick = (doc) => {
  selectedDoc.value = doc;
  showDocModal.value = true;
};

const printDoc = () => {
  window.print();
};
</script>

<style scoped>
.animate-fade-in {
  animation: fadeIn 0.5s ease-out forwards;
}
.animate-fade-in-up {
  animation: fadeInUp 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}
.animate-pulse-fast {
  animation: pulse 1s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.7; transform: scale(0.95); }
}
</style>
