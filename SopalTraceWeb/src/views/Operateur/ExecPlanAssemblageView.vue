<template>
  <div class="exec-assemblage-container">
    <!-- En-tête -->
    <!-- En-tête COMME IMAGE 2 -->
    <div v-if="!embedded" class="mb-6 bg-white p-6 rounded-2xl shadow-sm border border-slate-200">
      <div class="flex justify-between items-center mb-4 pb-3 border-b border-slate-100">
        <button @click="goBack" class="text-blue-600 hover:text-blue-800 hover:underline text-sm flex items-center transition-colors font-semibold border-0 bg-transparent cursor-pointer p-0">
          <i class="pi pi-arrow-left mr-2"></i> Retour aux documents
        </button>
        <button @click="loadData" class="text-slate-500 hover:text-slate-700 hover:underline text-sm flex items-center transition-colors font-semibold border-0 bg-transparent cursor-pointer p-0" :disabled="isLoading">
          <i class="pi pi-refresh mr-2" :class="{'pi-spin': isLoading}"></i> Rafraîchir les contrôles
        </button>
      </div>

      <div class="flex justify-between items-start flex-wrap gap-4">
        <div>
          <h2 class="text-2xl font-black text-slate-800 m-0">OF: {{ execution?.numeroOf || '—' }} <span class="text-lg text-slate-500 font-normal">({{ execution?.codeArticle || '—' }})</span></h2>
          <p class="text-gray-500 m-0 mt-1.5 text-sm">Atelier: <span class="font-bold text-slate-700">{{ execution?.atelier || 'ASSEMBLAGE' }}</span> | Échantillonnage: <span class="font-bold text-slate-700">{{ execution?.effectifEchantillonParHeure || 4 }} p/h (Postes A/B)</span></p>
          <p class="text-gray-500 m-0 mt-1 text-sm">Statut: <span class="font-semibold text-green-600">{{ execution?.statut || 'EN_COURS' }}</span></p>
        </div>
        
        <div class="flex items-center gap-2 flex-wrap">
          <button v-if="execution?.statut === 'EN_PAUSE' || execution?.statut === 'REGLAGE'" @click="reprendreOf" class="px-4 py-2 bg-green-600 hover:bg-green-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-play"></i> Reprendre la production
          </button>
          
          <button @click="openReglagesModalDirect" class="px-4 py-2 bg-yellow-500 hover:bg-yellow-600 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-cog"></i> Mettre Aux Réglages
          </button>

          <button @click="showConfigDialog = true" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-clock"></i> Horaires & Postes
          </button>

          <button v-if="execution?.statut === 'EN_COURS' || !execution?.statut" @click="mettreEnPauseOf" class="px-4 py-2 bg-slate-600 hover:bg-slate-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-pause"></i> Pause
          </button>

          <button @click="cloturerOf" class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-stop"></i> Clôturer l'opération
          </button>

          <button v-if="execution?.statut !== 'CLOTURE' && execution?.statut !== 'TERMINE'" @click="cloturerDoc" :disabled="isCloturing" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold rounded-lg shadow-sm transition border-0 cursor-pointer flex items-center gap-1.5 text-sm">
            <i class="pi pi-check-circle"></i> Clôturer le Document
          </button>
          <span v-else class="px-3 py-1.5 bg-emerald-100 text-emerald-800 font-bold rounded-lg text-sm border border-emerald-300 flex items-center gap-1.5">
            <i class="pi pi-check-circle text-emerald-600"></i> Document Complété
          </span>
        </div>
      </div>
    </div>

    <!-- Chargement -->
    <div v-if="isLoading" class="loading-state">
      <i class="pi pi-spin pi-spinner" style="font-size: 2.5rem; color: var(--primary-color);"></i>
      <p>Chargement du document d'assemblage et des résultats en cours...</p>
    </div>

    <!-- Erreur -->
    <div v-else-if="errorMessage" class="error-card">
      <i class="pi pi-exclamation-triangle error-icon"></i>
      <div class="error-content">
        <h3>Impossible de charger le document d'assemblage</h3>
        <p>{{ errorMessage }}</p>
        <button class="retry-btn" @click="loadData"><i class="pi pi-refresh"></i> Réessayer</button>
      </div>
    </div>

    <!-- Liste des sections COMME IMAGE 2 -->
    <div v-else-if="execution && execution.sections" class="bg-white border rounded-lg p-6 shadow-sm relative">
      <!-- En-tête de la carte COMME IMAGE 2 -->
      <div class="flex justify-between items-center mb-6 border-b pb-4 flex-wrap gap-2">
        <h3 class="text-xl font-bold text-gray-800 m-0">Alertes de Contrôle Actives</h3>
        <div class="text-base font-mono bg-gray-100 px-3.5 py-1.5 rounded-lg text-gray-700 font-semibold border border-gray-300 shadow-2xs">
          Date et Heure actuelle : {{ dateHeureActuelle }}
        </div>
      </div>
      
      <!-- Section Contrôles en cours de production : géré en temps réel par AlerteControle -->
      <div v-if="execution?.sections?.some(s => s.typeSection === 'ECHANTILLONNAGE' || s.typeSection === 'REGLAGE_PROD')" class="mb-8">
        <h4 class="text-lg font-bold text-blue-600 mb-4 flex items-center">
          <i class="pi pi-check-circle mr-2"></i>
          Contrôles en cours de production
        </h4>
        <AlerteControle
          :execControleOfId="String(execControleOfId)"
          :aDesControlesReglage="reglageSections.length > 0"
          :showHeader="false"
          @occurrence-submitted="loadData"
        />
      </div>

      <!-- Sections LOT_POSTE / AUTRE (au cas où il resterait des sections statiques) -->
      <div v-for="sec in execution.sections.filter(s => s.typeSection !== 'REGLAGE' && s.typeSection !== 'REGLAGE_PROD' && s.typeSection !== 'ECHANTILLONNAGE')" :key="sec.id" class="mb-8 last:mb-0">
        <div v-if="execution?.statut !== 'EN_PAUSE'">
          <h4 class="text-lg font-bold text-blue-600 mb-4 flex items-center">
            <i class="pi pi-check-circle mr-2"></i>
            {{ formatTypeSection(sec.typeSection) }} — {{ sec.libelle }}
          </h4>

          <div v-if="getGroupedTranches(sec).length > 0">
            <div v-for="trancheGroup in getGroupedTranches(sec)" :key="trancheGroup.trancheLabel" class="mb-6 border rounded overflow-hidden shadow-sm">
              <div class="bg-slate-100 p-3 font-bold border-b text-slate-800 flex justify-between items-center">
                <div class="flex items-center space-x-3">
                  <span class="text-base">Tranche : {{ trancheGroup.trancheLabel }}</span>
                </div>
                <div class="flex gap-2">
                  <button @click="declarerTrancheReglage(trancheGroup)" class="text-xs bg-yellow-50 border border-yellow-200 text-yellow-700 px-3 py-1.5 rounded hover:bg-yellow-100 transition-colors flex items-center shadow-xs font-semibold cursor-pointer">
                    ⚙️ Déclarer comme Réglage
                  </button>
                  <button @click="ignorerTranche(trancheGroup)" class="text-xs bg-red-50 border border-red-200 text-red-600 px-3 py-1.5 rounded hover:bg-red-100 transition-colors flex items-center shadow-xs font-semibold cursor-pointer">
                    <svg class="w-3.5 h-3.5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                    Ignorer la tranche
                  </button>
                </div>
              </div>
              
              <div class="p-5 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5 bg-gray-50">
                <div 
                  v-for="res in trancheGroup.resultats" 
                  :key="res.id" 
                  class="p-5 rounded-xl border bg-white shadow-sm flex flex-col justify-between transition-all hover:shadow-md"
                  :class="getCardBorderClass(res)"
                >
                  <div>
                    <div class="font-bold flex items-center mb-3">
                      <span v-if="res.statutNotif === 'EN_RETARD' || isLocallyOverdue(res)" class="text-red-600 bg-red-100 px-2.5 py-1 rounded text-xs border border-red-200 font-bold uppercase tracking-wider">⚠️ EN RETARD</span>
                      <span v-else-if="res.statutNotif === 'A_VENIR' || isLocallyUpcoming(res)" class="text-blue-600 bg-blue-100 px-2.5 py-1 rounded text-xs border border-blue-200 font-bold uppercase tracking-wider">⏳ À VENIR</span>
                      <span v-else class="text-yellow-700 bg-yellow-100 px-2.5 py-1 rounded text-xs border border-yellow-300 font-bold uppercase tracking-wider">🔔 MAINTENANT</span>
                    </div>
                    <div class="text-gray-800 font-bold text-lg leading-snug">{{ res.frequence || sec.libelle }}</div>
                    <div class="text-sm font-medium text-gray-500 mt-2 flex items-center gap-1.5">
                      <i class="pi pi-clock text-gray-400"></i> Prévue à : <span class="font-semibold text-gray-700">{{ formatHeure(res.heurePrevue) }}</span>
                    </div>
                  </div>

                  <button 
                    @click="openSaisirDialog(res, sec)" 
                    :disabled="res.statutNotif === 'A_VENIR' || isLocallyUpcoming(res)"
                    :class="[
                      'mt-5 w-full px-4 py-2.5 text-white text-sm font-bold rounded-lg shadow-xs transition-all flex items-center justify-center gap-2 border-0',
                      (res.statutNotif === 'A_VENIR' || isLocallyUpcoming(res)) ? 'bg-gray-400 cursor-not-allowed opacity-75' : 'bg-blue-600 hover:bg-blue-700 active:bg-blue-800 cursor-pointer shadow-sm hover:shadow'
                    ]"
                  >
                    <i v-if="res.statutNotif !== 'A_VENIR' && !isLocallyUpcoming(res)" class="pi pi-check-square"></i>
                    {{ (res.statutNotif === 'A_VENIR' || isLocallyUpcoming(res)) ? 'Bientôt disponible' : 'Exécuter le contrôle' }}
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div v-else-if="sec.resultats && sec.resultats.length > 0 && sec.resultats.every(r => r.resultat)" class="col-span-full text-center py-10 bg-green-50 border border-green-200 rounded-2xl text-green-800 my-2 shadow-sm">
            <i class="pi pi-check-circle text-4xl text-green-600 mb-3 block"></i>
            <span class="font-bold text-lg block">Toutes les occurrences de contrôle pour cette section ont été effectuées !</span>
            <span class="text-sm text-green-700 mt-1 block">La section est à jour et complète.</span>
          </div>
          <div v-else-if="!sec.resultats || sec.resultats.length === 0" class="col-span-full text-center py-6 text-gray-500 text-sm">
            Aucune occurrence ou tranche horaire initialisée pour cette section.
          </div>
        </div>
      </div>
    </div>

    <!-- Modale Configuration des Horaires -->
    <div v-if="showConfigDialog" class="modal-overlay" @click.self="showConfigDialog = false">
      <div class="modal-card config-modal">
        <div class="modal-header">
          <h3><i class="pi pi-clock"></i> Configurer Horaires & Postes (FE0591)</h3>
          <button class="close-btn" @click="showConfigDialog = false"><i class="pi pi-times"></i></button>
        </div>
        <div class="modal-body">
          <p class="modal-intro">
            Renseignez les horaires du poste actuel ou des équipes pour générer automatiquement les fréquences de contrôle par heure ainsi que le contrôle de la première et dernière pièce (niveau lot).
          </p>

          <div v-for="(p, idx) in postesConfig" :key="idx" class="poste-config-row">
            <div class="form-group">
              <label>Nom du Poste / Équipe</label>
              <input type="text" v-model="p.posteCode" placeholder="ex: Poste 1 (Matin)" class="form-input" />
            </div>
            <div class="form-group">
              <label>Heure Début</label>
              <input type="time" v-model="p.heureDebut" class="form-input" />
            </div>
            <div class="form-group">
              <label>Heure Fin</label>
              <input type="time" v-model="p.heureFin" class="form-input" />
            </div>
            <button v-if="postesConfig.length > 1" class="btn-remove-poste" @click="removePosteRow(idx)">
              <i class="pi pi-trash"></i>
            </button>
          </div>

          <button class="btn-add-poste" @click="addPosteRow">
            <i class="pi pi-plus"></i> Ajouter une autre équipe / poste
          </button>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showConfigDialog = false">Annuler</button>
          <button class="btn-save" @click="saveHorairesConfig" :disabled="isConfiguring">
            <i class="pi pi-check" v-if="!isConfiguring"></i>
            <i class="pi pi-spin pi-spinner" v-else></i>
            Valider et Générer le Plan
          </button>
        </div>
      </div>
    </div>

    <!-- Modale Aux Réglages -->
    <div v-if="showReglagesModal" class="modal-overlay" @click.self="showReglagesModal = false">
      <div class="modal-card" style="width: 850px; max-width: 95vw; max-height: 90vh; overflow-y: auto;">
        <div class="modal-header">
          <h3><i class="pi pi-cog"></i> Contrôles de Réglage Obligatoires (Démarrage)</h3>
          <button class="close-btn" @click="showReglagesModal = false"><i class="pi pi-times"></i></button>
        </div>
        <div class="modal-body p-4">
          <p class="modal-intro mb-4 text-sm text-gray-600">
            Exécutez et validez ci-dessous les contrôles de réglage pour le Plan d'assemblage et pour le Résultat en cours d'assemblage (affichés l'un sous l'autre).
          </p>

          <div v-if="reglageSections.length === 0" class="text-center py-8 text-gray-500 font-semibold">
            Aucun contrôle de réglage requis pour ce document.
          </div>

          <div v-for="(sec, idx) in reglageSections" :key="sec.id" class="mb-6 bg-yellow-50 p-4 rounded-xl border border-yellow-200 shadow-sm">
            <div class="bg-yellow-100 p-2.5 font-bold border-b border-yellow-200 text-yellow-800 flex justify-between items-center rounded-t">
              <span class="flex items-center gap-2 font-bold text-base">
                <i class="pi pi-bookmark text-yellow-700"></i>
                {{ idx === 0 ? 'Plan d\'assemblage en cours' : 'Résultat de contrôle en cours' }} — {{ sec.libelle || 'Réglage Initial' }}
              </span>
              <span class="text-xs px-2.5 py-0.5 rounded font-bold" :class="isReglageTermine ? 'bg-green-100 text-green-800 border border-green-300' : 'bg-amber-100 text-amber-800 border border-amber-300'">
                {{ isReglageTermine ? '✔ Validé' : '⏳ En attente de validation' }}
              </span>
            </div>
            
            <div class="p-4 bg-white border border-yellow-300 rounded-b">
              <div class="font-bold flex items-center mb-2">
                <span class="text-red-600 bg-red-100 px-2 py-0.5 rounded text-xs border border-red-200 font-semibold">⚠️ REQUIS AVANT PRODUCTION</span>
              </div>
              <div class="text-gray-800 font-bold text-lg">{{ sec.libelle || 'Caractéristiques à contrôler aux réglages' }}</div>
              <div class="text-sm text-gray-500 mt-1">{{ sec.lignesPlan?.length || 0 }} caractéristique(s) à contrôler (Série de {{ execution.effectifEchantillonParHeure || 4 }} pièces)</div>
              
              <button 
                @click="openSaisirDialog(sec.resultats?.[0] || { id: 'reg-' + idx, frequence: 'Démarrage (Réglage Initial)' }, sec); showReglagesModal = false" 
                class="mt-4 w-full px-4 py-2.5 bg-yellow-500 hover:bg-yellow-600 text-white text-sm font-bold rounded-lg shadow transition-colors cursor-pointer flex items-center justify-center gap-2 border-0"
              >
                <i class="pi pi-check-square"></i> Exécuter le contrôle de réglage
              </button>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showReglagesModal = false">Fermer</button>
        </div>
      </div>
    </div>

    <!-- Modale Saisie Résultat (COMME IMAGE 2 - Estompage) -->
    <div v-if="showSaisieDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div class="bg-white rounded-xl shadow-2xl w-full max-w-6xl max-h-[90vh] flex flex-col overflow-hidden">
        
        <!-- Header Modal (COMME IMAGE 2) -->
        <div class="bg-slate-800 text-white p-4 flex justify-between items-center">
          <div>
            <h2 class="text-xl font-bold">Plan de Contrôle - {{ selectedRow?.frequence || selectedSection?.libelle }}</h2>
            <p class="text-slate-300 text-sm mt-1">Heure prévue : {{ formatHeure(selectedRow?.heurePrevue || new Date()) }} | Tranche : {{ formatTypeSection(selectedSection?.typeSection) }}</p>
          </div>
          <button @click="showSaisieDialog = false" class="text-slate-300 hover:text-white font-bold text-2xl px-2">&times;</button>
        </div>

        <!-- Informations Plan (Légende / Remarques) -->
        <div v-if="execution?.legendeMoyens || execution?.remarques" class="bg-blue-50 border-b border-blue-100 p-4 text-sm text-blue-900">
          <div v-if="execution?.legendeMoyens" class="mb-2"><strong class="font-bold">Légende des moyens :</strong> {{ execution.legendeMoyens }}</div>
          <div v-if="execution?.remarques"><strong class="font-bold">Remarques / Observations :</strong> {{ execution.remarques }}</div>
        </div>
        <div v-else class="bg-blue-50 border-b border-blue-100 p-3 text-sm text-blue-900">
          <strong class="font-bold">Légende des moyens :</strong> M : Manuel ; EXC-... : Calibre spécial
        </div>

        <!-- Body Modal (COMME IMAGE 2) -->
        <div class="p-4 overflow-y-auto flex-1 bg-gray-50">
          
          <!-- Tableau 1 : Consultation du Plan d'Assemblage (Aucune saisie) -->
          <div v-if="selectedSection?.lignesPlan && selectedSection.lignesPlan.length > 0" class="bg-white border rounded-lg overflow-hidden shadow-sm mb-6">
            <table class="w-full text-sm text-left">
              <thead class="text-xs text-white bg-slate-800 border-b">
                <tr>
                  <th class="px-3 py-3">Caractéristique contrôlée</th>
                  <th class="px-3 py-3 w-32">Limite de spécification</th>
                  <th class="px-3 py-3 w-32">Type de contrôle</th>
                  <th class="px-3 py-3 w-32">Moyen de contrôle</th>
                  <th class="px-3 py-3 w-32">Code instrument de contrôle</th>
                  <th class="px-3 py-3">Observations</th>
                </tr>
              </thead>
              <tbody>
                <tr class="bg-blue-100 border-b border-blue-200">
                  <td colspan="6" class="px-3 py-2 text-center font-bold text-blue-900 shadow-sm">
                    {{ selectedSection?.libelle }}
                  </td>
                </tr>
                <tr v-for="lp in selectedSection.lignesPlan" :key="lp.id" class="border-b last:border-b-0 hover:bg-gray-50 transition-colors">
                  <td class="px-3 py-3 font-semibold text-gray-900">
                    <span class="text-xs text-gray-400 font-mono mr-1.5">{{ lp.numero }}.</span>
                    {{ lp.caracteristique }}
                  </td>
                  <td class="px-3 py-3 text-gray-600 font-mono bg-gray-50 font-medium">{{ lp.limiteSpecTexte || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs uppercase">{{ lp.typeControle || 'ATTRIBUT' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ lp.moyenControle || 'Manuel' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs font-mono">{{ lp.instrument || '-' }}</td>
                  <td class="px-3 py-3 text-gray-600 text-xs">{{ lp.observations || '-' }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Tableau 2 : Saisie des Résultats (COMME IMAGE 2 - Bas) -->
          <div v-if="saisieForm.lignesControle && saisieForm.lignesControle.length > 0" class="bg-white border rounded-lg overflow-hidden shadow-sm">
            <table class="w-full text-sm text-left">
              <thead class="text-xs text-white bg-slate-800 border-b uppercase">
                <tr>
                  <th class="px-3 py-3">Caractéristiques contrôlées</th>
                  <th class="px-3 py-3 w-44 text-center">
                    <div>Résultat (C / NC)</div>
                    <div class="inline-flex rounded shadow-xs mt-1" role="group">
                      <button type="button" @click="setAllLignesResult('C')" class="px-2 py-0.5 text-[10px] uppercase font-bold border-0 rounded-l bg-green-500 text-white hover:bg-green-600 cursor-pointer transition-colors" title="Tout Conforme">Tout C</button>
                      <button type="button" @click="setAllLignesResult('NC')" class="px-2 py-0.5 text-[10px] uppercase font-bold border-0 rounded-r bg-red-500 text-white hover:bg-red-600 cursor-pointer transition-colors" title="Tout Non-Conforme">Tout NC</button>
                    </div>
                  </th>
                  <th class="px-3 py-3 w-56 text-center">Non-conformité</th>
                  <th class="px-3 py-3 w-56 text-center">Actions de correction</th>
                </tr>
              </thead>
              <tbody>
                <tr class="bg-blue-100 border-b border-blue-200">
                  <td colspan="4" class="px-3 py-2 text-center font-extrabold text-blue-950 shadow-sm uppercase text-sm tracking-wide">
                    {{ selectedSection?.libelle }}
                  </td>
                </tr>
                <tr v-for="lc in saisieForm.lignesControle" :key="lc.ligneId" class="border-b last:border-b-0 hover:bg-blue-50 transition-colors" :class="lc.resultat === 'NC' ? 'bg-red-50/60' : ''">
                  <td class="px-3 py-3 font-semibold text-gray-900">
                    <span class="text-xs text-gray-400 font-mono mr-1.5">{{ lc.numero }}.</span>
                    {{ lc.caracteristique }}
                  </td>
                  <td class="px-3 py-3 text-center">
                    <div class="inline-flex rounded-md shadow-sm" role="group">
                      <button type="button" 
                              @click="setLigneResultat(lc, 'C')"
                              :class="lc.resultat === 'C' ? 'bg-green-500 text-white border-green-600' : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'"
                              class="px-4 py-1.5 text-sm font-bold border rounded-l-lg transition-colors cursor-pointer">
                        C
                      </button>
                      <button type="button" 
                              @click="setLigneResultat(lc, 'NC')"
                              :class="lc.resultat === 'NC' ? 'bg-red-500 text-white border-red-600' : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'"
                              class="px-4 py-1.5 text-sm font-bold border-t border-b border-r rounded-r-lg transition-colors cursor-pointer">
                        NC
                      </button>
                    </div>
                  </td>
                  <td class="px-3 py-3 text-center">
                    <input v-if="lc.resultat === 'NC'" type="text" v-model="lc.nc" placeholder="Précisez la non-conformité..." class="w-full border border-red-300 rounded p-1.5 text-xs bg-white focus:ring-red-500 focus:border-red-500" />
                    <span v-else class="text-gray-400">-</span>
                  </td>
                  <td class="px-3 py-3 text-center">
                    <input v-if="lc.resultat === 'NC'" type="text" v-model="lc.actionCorrective" placeholder="Action corrective..." class="w-full border border-amber-300 rounded p-1.5 text-xs bg-white focus:ring-amber-500 focus:border-amber-500" />
                    <span v-else class="text-gray-400">-</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

        </div>

        <!-- Footer Modal (COMME IMAGE 2) -->
        <div class="border-t p-4 bg-white flex justify-end gap-3">
          <button @click="showSaisieDialog = false" class="px-4 py-2 border text-gray-600 font-bold rounded-lg hover:bg-gray-50 transition-colors">
            Annuler
          </button>
          
          <button @click="submitResultat" 
                  :disabled="isSavingResult || !isFormValid"
                  class="px-6 py-2 bg-blue-600 text-white font-bold rounded-lg hover:bg-blue-700 shadow disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2">
            <i class="pi pi-save" v-if="!isSavingResult"></i>
            <i class="pi pi-spin pi-spinner" v-else></i>
            Valider et Enregistrer
          </button>
        </div>

      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAppToast } from '@/composables/useAppToast'
import execAssemblageService from '@/services/execAssemblageService'
import operateurService from '@/services/operateurService'
import AlerteControle from '@/components/Operateur/AlerteControle.vue'
import { useAuthStore } from '@/stores/authStore'
import Swal from 'sweetalert2'

const authStore = useAuthStore()

const props = defineProps({
  embedded: {
    type: Boolean,
    default: false
  },
  execControleOfId: {
    type: [String, Number],
    default: null
  },
  selectedPoste: {
    type: String,
    default: null
  }
})

const route = useRoute()
const router = useRouter()
const toast = useAppToast()

const execControleOfId = ref(props.execControleOfId || route.params.execControleOfId)
const posteCode = ref(props.selectedPoste || route.query.posteCode || 'Poste 1')

watch(() => props.execControleOfId, (newId) => {
  if (newId && newId !== execControleOfId.value) {
    execControleOfId.value = newId
    loadData()
  }
})

watch(() => props.selectedPoste, (newPoste) => {
  if (newPoste && newPoste !== posteCode.value) {
    posteCode.value = newPoste
    loadData()
  }
})

const execution = ref(null)
const isLoading = ref(true)
const errorMessage = ref('')

// Config Horaires
const showConfigDialog = ref(false)
const showReglagesModal = ref(false)
const isConfiguring = ref(false)
const postesConfig = ref([
  { posteCode: 'Poste 1', heureDebut: '08:00', heureFin: '14:00' }
])

// Saisie Résultat
const showSaisieDialog = ref(false)
const isSavingResult = ref(false)
const selectedRow = ref(null)
const selectedSection = ref(null)
const activePieceIndex = ref(0)
const saisieForm = ref({
  trancheId: '',
  resultat: 'CONFORME',
  nonConformite: '',
  actionCorrective: '',
  approbation: '',
  remarques: '',
  piecesEchantillon: [],
  lignesControle: []
})

const isCloturing = ref(false)

const reglageSections = computed(() => {
  if (!execution.value?.sections) return []
  return execution.value.sections.filter(s => s.typeSection === 'REGLAGE' || s.typeSection === 'REGLAGE_PROD')
})

const isReglageTermine = computed(() => {
  if (reglageSections.value.length === 0) return true
  return reglageSections.value.every(sec => 
    sec.resultats && sec.resultats.length > 0 && sec.resultats.every(r => r.resultat === 'CONFORME' || r.resultat === 'NON_CONFORME')
  )
})

const dateHeureActuelle = ref('')
let timerInterval = null

onMounted(async () => {
  dateHeureActuelle.value = new Date().toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' })
  timerInterval = setInterval(() => {
    dateHeureActuelle.value = new Date().toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' })
  }, 1000)
  await loadData()
})

onUnmounted(() => {
  if (timerInterval) clearInterval(timerInterval)
})

const loadData = async () => {
  if (!execControleOfId.value) return
  isLoading.value = true
  errorMessage.value = ''
  try {
    const res = await execAssemblageService.getExecution(execControleOfId.value, posteCode.value)
    execution.value = res.data
    
    // Si des postes sont configurés, on les préremplit
    if (execution.value.postesConfigures && execution.value.postesConfigures.length > 0) {
      postesConfig.value = execution.value.postesConfigures.map(p => ({
        posteCode: p.posteCode,
        heureDebut: p.heureDebut,
        heureFin: p.heureFin
      }))
    }
  } catch (err) {
    console.error('Erreur chargement plan assemblage:', err)
    errorMessage.value = err.response?.data?.message || err.message || 'Erreur inconnue'
  } finally {
    isLoading.value = false
  }
}


const navigateAway = () => {
  router.push({
    name: 'operateur-of-fini',
    query: {
      execControleOfId: execControleOfId.value,
      posteCode: posteCode.value
    }
  })
}

const goBack = () => {
  navigateAway()
}

const addPosteRow = () => {
  postesConfig.value.push({
    posteCode: `Poste ${postesConfig.value.length + 1}`,
    heureDebut: '14:00',
    heureFin: '20:00'
  })
}

const removePosteRow = (idx) => {
  if (postesConfig.value.length > 1) {
    postesConfig.value.splice(idx, 1)
  }
}

const saveHorairesConfig = async () => {
  isConfiguring.value = true
  try {
    const res = await execAssemblageService.configurerHoraires(execControleOfId.value, {
      postes: postesConfig.value
    })
    execution.value = res.data
    showConfigDialog.value = false
    toast.success('Horaires configurés et contrôles générés avec succès !')
  } catch (err) {
    console.error('Erreur config horaires:', err)
    toast.error(err.response?.data?.message || 'Erreur lors de la configuration des horaires')
  } finally {
    isConfiguring.value = false
  }
}

const openReglagesModalDirect = () => {
  if (reglageSections.value && reglageSections.value.length > 0) {
    const sec = reglageSections.value[0]
    openSaisirDialog(sec.resultats?.[0], sec)
  } else {
    showReglagesModal.value = true
  }
}


const setAllLignesResult = (res) => {
  if (saisieForm.value.lignesControle) {
    saisieForm.value.lignesControle.forEach(lc => {
      lc.resultat = res
      if (res === 'C') {
        lc.nc = ''
        lc.actionCorrective = ''
      }
    })
  }
}

const setLigneResultat = (lc, res) => {
  lc.resultat = res
  if (res === 'C') {
    lc.nc = ''
    lc.actionCorrective = ''
  }
}

const hasAnyNonConformite = computed(() => {
  return saisieForm.value.lignesControle?.some(l => l.resultat === 'NC')
})

const openSaisirDialog = (res, sec) => {
  const targetRow = res || { id: 'reglage', frequence: sec.libelle || 'Démarrage (Réglage Initial)' }
  selectedRow.value = targetRow
  selectedSection.value = sec
  activePieceIndex.value = 0

  const sourceLignes = (sec.lignesResultat && sec.lignesResultat.length > 0) ? sec.lignesResultat : (sec.lignesPlan || [])

  const lignesCtrl = sourceLignes.map(l => ({
    ligneId: l.id,
    numero: l.numero,
    caracteristique: l.caracteristique,
    spec: l.limiteSpecTexte,
    instrument: l.instrument,
    resultat: 'C',
    nc: '',
    actionCorrective: ''
  }))

  saisieForm.value = {
    trancheId: res.id,
    resultat: res.resultat || 'C',
    nonConformite: res.nonConformite || '',
    actionCorrective: res.actionCorrective || '',
    approbation: authStore.user?.matricule || res.approbation || '',
    remarques: res.remarques || '',
    lignesControle: lignesCtrl
  }
  showSaisieDialog.value = true
}

const isFormValid = computed(() => {
  if (hasAnyNonConformite.value) {
    const ncLignesWithoutComment = saisieForm.value.lignesControle?.some(l => l.resultat === 'NC' && !l.nc?.trim())
    if (ncLignesWithoutComment) return false
  }
  return true
})

const submitResultat = async () => {
  if (!isFormValid.value) return
  isSavingResult.value = true
  try {
    const ncLignes = saisieForm.value.lignesControle?.filter(l => l.resultat === 'NC') || []
    if (ncLignes.length > 0) {
      saisieForm.value.resultat = 'NC'
      const ncText = ncLignes.map(l => `[Ligne ${l.numero} - ${l.caracteristique}]: ${l.nc || 'Non Conforme'}`).join(' ; ')
      const acText = ncLignes.filter(l => l.actionCorrective).map(l => `[Ligne ${l.numero}]: ${l.actionCorrective}`).join(' ; ')
      saisieForm.value.nonConformite = ncText
      saisieForm.value.actionCorrective = acText
    } else {
      saisieForm.value.resultat = 'C'
      saisieForm.value.nonConformite = ''
      saisieForm.value.actionCorrective = ''
    }

    await execAssemblageService.saveResultat(execControleOfId.value, saisieForm.value)
    toast.success('Résultat enregistré avec succès')
    showSaisieDialog.value = false
    await loadData()
  } catch (err) {
    console.error('Erreur saisie résultat:', err)
    toast.error(err.response?.data?.message || 'Erreur lors de l\'enregistrement')
  } finally {
    isSavingResult.value = false
  }
}

const cloturerDoc = async () => {
  if (!confirm('Êtes-vous sûr de vouloir clôturer et valider définitivement ce document d\'assemblage ?')) return
  isCloturing.value = true
  try {
    await execAssemblageService.cloturerExecution(execControleOfId.value)
    toast.success('Document d\'assemblage clôturé !')
    goBack()
  } catch (err) {
    console.error('Erreur clôture:', err)
    toast.error(err.response?.data?.message || 'Erreur lors de la clôture')
  } finally {
    isCloturing.value = false
  }
}

const mettreEnPauseOf = async () => {
  const result = await Swal.fire({
    title: 'Mettre la production en Pause ?',
    text: 'Voulez-vous vraiment mettre l\'OF en pause ?',
    input: 'text',
    inputPlaceholder: 'Motif (Optionnel, ex: Panne machine, pause déjeuner)...',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#eab308',
    cancelButtonColor: '#64748b',
    confirmButtonText: 'Oui, mettre en pause',
    cancelButtonText: 'Annuler'
  })

  if (result.isConfirmed) {
    try {
      const raison = result.value?.trim() || 'Pause'
      await operateurService.mettreEnPause(execControleOfId.value, raison)
      toast.success('Production en Pause', 'L\'OF est maintenant en pause.')
      if (execution.value) execution.value.statut = 'EN_PAUSE'
      await loadData()
    } catch (error) {
      console.error(error)
      toast.error('Erreur', 'Impossible de mettre en pause.')
    }
  }
}

const reprendreOf = async () => {
  try {
    await operateurService.reprendreDepuisPause(execControleOfId.value)
    toast.success('Reprise', 'La production a repris.')
    if (execution.value) execution.value.statut = 'EN_COURS'
    await loadData()
  } catch (error) {
    console.error(error)
    toast.error('Erreur', error.response?.data?.message || 'Impossible de reprendre la production.')
  }
}

const cloturerOf = async () => {
  const result = await Swal.fire({
    title: 'Clôturer l\'OF d\'assemblage ?',
    text: 'Êtes-vous sûr de vouloir clôturer définitivement cet ordre de fabrication ?',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#d33',
    cancelButtonColor: '#3085d6',
    confirmButtonText: 'Oui, clôturer',
    cancelButtonText: 'Annuler'
  })

  if (result.isConfirmed) {
    try {
      await operateurService.cloturerOf(execControleOfId.value)
      toast.success('Succès', 'L\'OF a été clôturé.')
      goBack()
    } catch (error) {
      console.error(error)
      toast.error('Erreur', 'Impossible de clôturer l\'OF.')
    }
  }
}

// Utilitaires de formatage
const formatTypeSection = (ts) => {
  if (ts === 'REGLAGE') return 'Réglages'
  if (ts === 'ECHANTILLONNAGE') return 'En Cours / Échantillonnage'
  if (ts === 'LOT_POSTE') return 'Niveau du Lot (OF)'
  return 'Standard'
}

const formatHeure = (dtStr) => {
  if (!dtStr) return '-'
  const date = new Date(dtStr)
  if (isNaN(date.getTime())) return '-'
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

const isLocallyUpcoming = (res) => {
  if (res.statutNotif === 'A_VENIR') return true
  if (!res.heurePrevue) return false
  const now = new Date()
  const prevue = new Date(res.heurePrevue)
  return prevue > new Date(now.getTime() + 60000)
}

const isLocallyOverdue = (res) => {
  if (res.statutNotif === 'EN_RETARD') return true
  if (!res.heurePrevue) return false
  const now = new Date()
  const prevue = new Date(res.heurePrevue)
  return prevue < new Date(now.getTime() - 15 * 60000) && res.statutNotif !== 'A_VENIR' && !isLocallyUpcoming(res)
}

const getCardBorderClass = (res) => {
  if (res.statutNotif === 'EN_RETARD' || isLocallyOverdue(res)) return 'border-red-300 hover:border-red-400'
  if (res.statutNotif === 'A_VENIR' || isLocallyUpcoming(res)) return 'border-blue-300 hover:border-blue-400'
  return 'border-yellow-300 hover:border-yellow-400'
}

const getGroupedTranches = (sec) => {
  if (!sec || !sec.resultats) return []
  
  const pending = sec.resultats.filter(r => !r.resultat)
  if (pending.length === 0) return []

  pending.sort((a, b) => new Date(a.heurePrevue) - new Date(b.heurePrevue))

  const now = new Date()
  const nowPlus1Min = new Date(now.getTime() + 60000)
  
  const dueOrOverdue = pending.filter(r => new Date(r.heurePrevue) <= nowPlus1Min)
  const future = pending.filter(r => new Date(r.heurePrevue) > nowPlus1Min)

  const visible = [...dueOrOverdue]
  
  if (future.length > 0) {
    const firstFutureTime = new Date(future[0].heurePrevue)
    const nextGroup = future.filter(r => Math.abs(new Date(r.heurePrevue) - firstFutureTime) < 3600000 && visible.length < 4)
    if (dueOrOverdue.length > 0) {
      visible.push(future[0])
    } else {
      visible.push(...nextGroup)
    }
  }

  const groupsMap = new Map()
  
  for (const res of visible) {
    const d = new Date(res.heurePrevue)
    const dateStr = d.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' })
    const hStart = d.getHours().toString().padStart(2, '0') + ':00'
    const hEnd = ((d.getHours() + 1) % 24).toString().padStart(2, '0') + ':00'
    const trancheKey = `${dateStr} | ${hStart} - ${hEnd}`
    
    if (!groupsMap.has(trancheKey)) {
      groupsMap.set(trancheKey, {
        trancheLabel: trancheKey,
        resultats: []
      })
    }
    groupsMap.get(trancheKey).resultats.push(res)
  }

  return Array.from(groupsMap.values())
}

const declarerTrancheReglage = async (trancheGroup) => {
  const result = await Swal.fire({
    title: 'Déclarer comme Réglage ?',
    text: `Voulez-vous vraiment déclarer tous les contrôles de la tranche ${trancheGroup.trancheLabel} comme Réglage ?`,
    icon: 'question',
    showCancelButton: true,
    confirmButtonColor: '#eab308',
    cancelButtonColor: '#64748b',
    confirmButtonText: 'Oui, déclarer',
    cancelButtonText: 'Annuler'
  })

  if (!result.isConfirmed) return

  try {
    for (const res of trancheGroup.resultats) {
      await execAssemblageService.saveResultat(execControleOfId.value, {
        trancheId: res.id,
        resultat: 'REGLAGE'
      })
    }
    toast.success('Succès', 'Tranche déclarée comme réglage.')
    await loadData()
  } catch (error) {
    console.error(error)
    toast.error('Erreur', error.response?.data?.message || 'Erreur lors de la déclaration.')
  }
}

const ignorerTranche = async (trancheGroup) => {
  const result = await Swal.fire({
    title: 'Ignorer la tranche ?',
    text: `Voulez-vous vraiment ignorer tous les contrôles de la tranche ${trancheGroup.trancheLabel} ?`,
    input: 'text',
    inputPlaceholder: 'Raison de l\'ignorance (Optionnel)...',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#ef4444',
    cancelButtonColor: '#64748b',
    confirmButtonText: 'Oui, ignorer',
    cancelButtonText: 'Annuler'
  })

  if (!result.isConfirmed) return

  try {
    const raison = result.value?.trim() || 'Ignoré'
    for (const res of trancheGroup.resultats) {
      await execAssemblageService.saveResultat(execControleOfId.value, {
        trancheId: res.id,
        resultat: 'IGNORE',
        remarques: raison
      })
    }
    toast.success('Succès', 'Tranche ignorée.')
    await loadData()
  } catch (error) {
    console.error(error)
    toast.error('Erreur', error.response?.data?.message || 'Erreur lors de l\'ignorance de la tranche.')
  }
}
</script>

<style scoped>
.exec-assemblage-container {
  padding: 1.5rem;
  max-width: 1400px;
  margin: 0 auto;
  font-family: 'Inter', system-ui, sans-serif;
  color: #1e293b;
}

/* En-tête */
.header-card {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  color: white;
  border-radius: 16px;
  padding: 1.5rem 2rem;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.2);
  margin-bottom: 2rem;
}
.header-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  gap: 1rem;
}
.back-btn {
  background: rgba(255, 255, 255, 0.1);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.2);
  padding: 0.6rem 1.2rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: all 0.2s;
}
.back-btn:hover {
  background: rgba(255, 255, 255, 0.2);
}
.header-actions {
  display: flex;
  gap: 1rem;
}
.config-btn {
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  color: white;
  border: none;
  padding: 0.6rem 1.2rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3);
  transition: transform 0.15s, box-shadow 0.15s;
}
.config-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(37, 99, 235, 0.4);
}
.cloture-btn {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
  border: none;
  padding: 0.6rem 1.2rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
  transition: transform 0.15s;
}
.cloture-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.of-details {
  display: flex;
  flex-wrap: wrap;
  gap: 1.2rem;
}
.detail-badge {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.12);
  padding: 0.6rem 1rem;
  border-radius: 10px;
  display: flex;
  flex-direction: column;
}
.detail-badge .label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #94a3b8;
  margin-bottom: 0.2rem;
}
.detail-badge .val {
  font-size: 1rem;
  font-weight: 700;
  color: #f8fafc;
}

/* Loading & Error */
.loading-state, .error-card {
  text-align: center;
  padding: 4rem 2rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);
}
.error-icon {
  font-size: 3rem;
  color: #ef4444;
  margin-bottom: 1rem;
}
.retry-btn {
  margin-top: 1rem;
  background: #3b82f6;
  color: white;
  border: none;
  padding: 0.6rem 1.5rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
}

/* Sections */
.sections-list {
  display: flex;
  flex-direction: column;
  gap: 2.5rem;
}
.section-card {
  background: #ffffff;
  border-radius: 16px;
  box-shadow: 0 10px 30px -10px rgba(0, 0, 0, 0.08);
  border: 1px solid #e2e8f0;
  overflow: hidden;
  transition: box-shadow 0.2s;
}
.section-card:hover {
  box-shadow: 0 15px 35px -10px rgba(0, 0, 0, 0.12);
}
.section-header {
  background: linear-gradient(90deg, #f8fafc 0%, #f1f5f9 100%);
  padding: 1.25rem 1.75rem;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.section-title {
  display: flex;
  align-items: center;
  gap: 1rem;
}
.sec-num {
  background: #3b82f6;
  color: white;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 1rem;
}
.section-title h3 {
  margin: 0;
  font-size: 1.35rem;
  font-weight: 700;
  color: #0f172a;
}
.sec-type-tag {
  padding: 0.3rem 0.8rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.sec-type-tag.reglage { background: #fee2e2; color: #991b1b; }
.sec-type-tag.echantillonnage { background: #dbeafe; color: #1e40af; }
.sec-type-tag.lot_poste { background: #fef3c7; color: #92400e; }

/* Sous-blocs */
.sub-block {
  padding: 1.5rem 1.75rem;
}
.plan-block {
  border-bottom: 2px dashed #cbd5e1;
  background: #fafcfd;
}
.result-block {
  background: #ffffff;
}
.sub-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.25rem;
}
.sub-title i {
  font-size: 1.25rem;
  color: #3b82f6;
}
.result-title i {
  color: #10b981;
}
.sub-title h4 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 700;
  color: #334155;
}
.notif-hint {
  margin-left: auto;
  font-size: 0.85rem;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  background: #f1f5f9;
  padding: 0.35rem 0.75rem;
  border-radius: 6px;
}

/* Tables */
.table-responsive {
  overflow-x: auto;
}
.data-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  font-size: 0.95rem;
}
.data-table th {
  background: #f8fafc;
  color: #475569;
  font-weight: 600;
  text-align: left;
  padding: 0.85rem 1rem;
  border-bottom: 2px solid #e2e8f0;
  text-transform: uppercase;
  font-size: 0.75rem;
  letter-spacing: 0.5px;
}
.data-table td {
  padding: 0.9rem 1rem;
  border-bottom: 1px solid #f1f5f9;
  vertical-align: middle;
}
.plan-table tbody tr:hover {
  background: #f8fafc;
}

/* Badges de Lignes Plan */
.spec-badge {
  background: #eff6ff;
  color: #1d4ed8;
  padding: 0.25rem 0.6rem;
  border-radius: 6px;
  font-weight: 600;
  font-size: 0.85rem;
}
.type-badge {
  background: #f1f5f9;
  color: #475569;
  padding: 0.25rem 0.6rem;
  border-radius: 6px;
  font-size: 0.85rem;
}
.obs-cell {
  font-style: italic;
  color: #64748b;
  max-width: 250px;
}

/* Badges de Résultat & Notification */
.notif-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.75rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 700;
}
.notif-badge.fait { background: #d1fae5; color: #065f46; }
.notif-badge.en_retard { 
  background: #fee2e2; 
  color: #991b1b; 
  animation: pulse-red 2s infinite; 
}
.notif-badge.a_faire { background: #fef3c7; color: #92400e; }
.notif-badge.a_venir { background: #e0f2fe; color: #0369a1; }

@keyframes pulse-red {
  0% { box-shadow: 0 0 0 0 rgba(239, 68, 68, 0.4); }
  70% { box-shadow: 0 0 0 8px rgba(239, 68, 68, 0); }
  100% { box-shadow: 0 0 0 0 rgba(239, 68, 68, 0); }
}

.res-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.8rem;
  border-radius: 6px;
  font-weight: 700;
  font-size: 0.85rem;
}
.res-badge.conforme { background: #d1fae5; color: #065f46; }
.res-badge.non-conforme { background: #fee2e2; color: #991b1b; }
.res-badge.pending { background: #f1f5f9; color: #94a3b8; }

.row-retard { background-color: #fff5f5 !important; }
.row-afaire { background-color: #fffbeb !important; }
.row-pause { background-color: #eff6ff !important; }
.row-reglage { background-color: #fef3c7 !important; }
.row-nc { background-color: #fef2f2 !important; font-weight: 600; }

.nc-text { color: #dc2626; font-weight: 600; }
.ac-text { color: #2563eb; }
.mat-badge { background: #f1f5f9; padding: 0.2rem 0.6rem; border-radius: 4px; font-weight: 600; font-size: 0.85rem; }

.action-btn {
  background: white;
  border: 1px solid #cbd5e1;
  color: #334155;
  padding: 0.45rem 0.85rem;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}
.action-btn:hover {
  background: #3b82f6;
  color: white;
  border-color: #3b82f6;
}

.empty-cell {
  text-align: center;
  color: #94a3b8;
  padding: 2.5rem !important;
  font-style: italic;
}

/* Modales */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
}
.modal-card {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 600px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  max-height: 90vh;
}
.modal-header {
  background: #f8fafc;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.modal-header h3 {
  margin: 0;
  font-size: 1.25rem;
  color: #0f172a;
  display: flex;
  align-items: center;
  gap: 0.6rem;
}
.close-btn {
  background: none; border: none; font-size: 1.25rem; color: #64748b; cursor: pointer;
}
.modal-body {
  padding: 1.5rem;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}
.modal-footer {
  padding: 1rem 1.5rem;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
}

/* Formulaires dans Modales */
.modal-intro {
  color: #475569;
  font-size: 0.95rem;
  line-height: 1.5;
  margin: 0;
  background: #eff6ff;
  padding: 0.85rem 1rem;
  border-radius: 8px;
  border-left: 4px solid #3b82f6;
}
.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.form-group label {
  font-weight: 600;
  font-size: 0.9rem;
  color: #334155;
}
.req { color: #ef4444; }
.form-input {
  padding: 0.65rem 0.85rem;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 0.95rem;
  transition: border-color 0.15s;
}
.form-input:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.poste-config-row {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr auto;
  gap: 0.75rem;
  align-items: end;
  background: #f8fafc;
  padding: 0.85rem;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
}
.btn-remove-poste {
  background: #fee2e2;
  color: #dc2626;
  border: none;
  width: 38px; height: 38px;
  border-radius: 8px;
  cursor: pointer;
}
.btn-add-poste {
  background: #f1f5f9;
  color: #3b82f6;
  border: 1px dashed #3b82f6;
  padding: 0.75rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;
}
.btn-add-poste:hover {
  background: #eff6ff;
}

/* Saisie boutons décision */
.decision-group {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}
.decision-btn {
  padding: 1rem;
  border-radius: 12px;
  border: 2px solid #e2e8f0;
  background: white;
  font-weight: 700;
  font-size: 1rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  transition: all 0.2s;
}
.conforme-btn:hover, .conforme-btn.active {
  border-color: #10b981;
  background: #ecfdf5;
  color: #065f46;
}
.non-conforme-btn:hover, .non-conforme-btn.active {
  border-color: #ef4444;
  background: #fef2f2;
  color: #991b1b;
}

.btn-cancel {
  background: white;
  border: 1px solid #cbd5e1;
  padding: 0.65rem 1.25rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
}
.btn-save {
  background: #3b82f6;
  color: white;
  border: none;
  padding: 0.65rem 1.5rem;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.btn-save:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.saisie-section-info {
  background: #f1f5f9;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  color: #475569;
}
</style>
