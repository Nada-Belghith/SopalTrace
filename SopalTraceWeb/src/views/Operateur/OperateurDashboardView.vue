<template>
  <div class="min-h-screen bg-slate-50 flex flex-col p-8">
    <Toast position="top-right" />
    <header class="mb-8" v-if="!activeOfContext.id">
      <h1 class="text-3xl font-bold text-slate-800 flex items-center">
        <svg class="w-6 h-6 mr-3 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16"></path></svg>
        Tableau de Bord Opérateur
      </h1>
      <p class="text-slate-500 mt-2 text-sm ml-9">Sélectionnez un Ordre de Fabrication pour démarrer vos contrôles.</p>
    </header>

    <div v-if="loading" class="text-center py-10">
      <p class="text-slate-500">Chargement des Ordres de Fabrication...</p>
    </div>

    <!-- Si l'opérateur est dans l'écran de contrôle actif -->
    <div v-else-if="activeOfContext.id" class="flex flex-col bg-white rounded-xl shadow-sm border border-slate-200 p-6 h-[80vh]">
      
      <div class="flex justify-between items-center mb-4">
        <!-- Bouton Retour -->
        <button @click="quitterExecution" class="text-blue-600 hover:text-blue-800 hover:underline text-sm flex items-center transition-colors font-medium">
          <i class="pi pi-arrow-left mr-2"></i> Retour à la liste des OFs
        </button>
        
        <!-- Bouton Rafraîchir -->
        <button @click="chargerOfs" class="text-slate-500 hover:text-slate-700 hover:underline text-sm flex items-center transition-colors font-medium">
          <i class="pi pi-refresh mr-2"></i> Rafraîchir les contrôles
        </button>
      </div>

      <div class="flex justify-between items-start mb-6 pb-4 border-b">
        <div>
          <h2 class="text-2xl font-bold text-slate-800">OF: {{ activeOfContext.numeroOf }} <span class="text-lg text-slate-500 font-normal">({{ activeOfContext.operationCode }})</span></h2>
          <p class="text-gray-500">Machine Réelle: <span class="font-bold text-slate-700">{{ activeOfContext.machineCode }}</span></p>
          <p class="text-gray-500">Statut: 
            <span class="font-semibold" :class="{'text-yellow-600': activeOfContext.statut === 'REGLAGE', 'text-green-600': activeOfContext.statut === 'EN_COURS'}">
              {{ activeOfContext.statut }}
            </span>
          </p>
        </div>
        
        <div class="flex items-center">
          <button v-if="activeOfContext.statut === 'EN_PAUSE' || activeOfContext.statut === 'REGLAGE' || activeOfContext.estEnReglage" @click="reprendre" class="px-4 py-2 bg-green-600 text-white font-semibold rounded-lg hover:bg-green-700 shadow-sm transition mr-2">
            ▶️ Reprendre la production
          </button>
          
          <button v-if="activeOfContext.statut === 'EN_COURS' && activeOfContext.a_Des_Controles_Reglage && !activeOfContext.estEnReglage" @click="mettreEnReglage" class="px-4 py-2 bg-yellow-500 text-white font-semibold rounded-lg hover:bg-yellow-600 shadow-sm transition mr-2">
            ⚙️ Mettre Aux Réglages
          </button>

          <button v-if="activeOfContext.statut === 'REGLAGE' || activeOfContext.estEnReglage" @click="mettreEnReglage" class="px-4 py-2 bg-yellow-600 text-white font-semibold rounded-lg hover:bg-yellow-700 shadow-sm transition mr-2">
            🔄 Nouveau Réglage
          </button>
          
          <button v-if="activeOfContext.statut === 'EN_COURS' && !activeOfContext.estEnReglage" @click="mettreEnPause" class="px-4 py-2 bg-slate-600 text-white font-semibold rounded-lg hover:bg-slate-700 shadow-sm transition mr-2">
            ⏸️ Pause
          </button>

          <button @click="cloturer" class="px-4 py-2 bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700 shadow-sm transition">
            ⏹️ Clôturer l'opération
          </button>
        </div>
      </div>
      <div class="flex-1 overflow-y-auto">
        <AlerteControle 
          :execControleOfId="activeOfContext.id" 
          :aDesControlesReglage="activeOfContext.a_Des_Controles_Reglage"
          :legendeMoyens="activeOfContext.legendeMoyens"
          :remarques="activeOfContext.remarques"
          @occurrence-submitted="chargerOfs" 
        />
      </div>
    </div>

    <!-- Liste des OFs sous forme de Cards -->
    <div v-else class="flex flex-wrap gap-6 ml-9">
      <div v-for="(of, index) in filteredOfs" :key="index" 
           @click="ouvrirModalDemarrage(of)"
           class="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 w-[400px] cursor-pointer hover:shadow-md hover:border-blue-300 transition-all flex flex-col justify-between">
        
        <div class="flex justify-between items-start mb-4">
          <span class="text-xs font-bold text-blue-600 tracking-wider">OF: {{ of.numeroOf }}</span>
          <span class="px-2 py-1 bg-green-50 text-green-600 font-bold text-[10px] rounded uppercase tracking-wider"
                :class="of.statutOf === 'EN_COURS' ? 'bg-green-50 text-green-600' : 'bg-yellow-50 text-yellow-600'">
            {{ of.statutOf }}
          </span>
        </div>

        <div class="mb-4">
          <h3 class="text-lg font-bold text-slate-800 leading-tight mb-1">{{ of.designationArticle }}</h3>
          <p class="text-sm text-slate-400 font-medium">{{ of.codeArticle }}</p>
        </div>

        <!-- Timeline Gamme Opératoire -->
        <div v-if="of.gammeOperatoire && of.gammeOperatoire.length > 0" class="mb-4 mt-2">
          <p class="text-[9px] text-slate-400 uppercase font-bold tracking-wider mb-3">Progression des opérations</p>
          <div class="relative flex items-center justify-between px-2">
            <!-- Ligne de fond -->
            <div class="absolute top-2 left-4 right-4 h-1 bg-slate-100 z-0 rounded-full"></div>
            
            <div v-for="(op, idx) in of.gammeOperatoire" :key="idx" class="relative z-10 flex flex-col items-center group">
              <!-- Point -->
              <div class="w-4 h-4 rounded-full border-2 transition-all duration-300 relative flex items-center justify-center shadow-sm"
                   :class="[
                     op.activeExecStatut === 'CLOTURE' ? 'border-green-500 bg-green-500' :
                     op.activeExecStatut === 'EN_COURS' ? 'border-blue-500 bg-blue-500 ring-4 ring-blue-100' :
                     op.activeExecStatut === 'EN_PAUSE' ? 'border-yellow-500 bg-yellow-400 ring-4 ring-yellow-50' :
                     'border-slate-300 bg-white'
                   ]">
                   <!-- Checkmark pour CLOTURE -->
                   <svg v-if="op.activeExecStatut === 'CLOTURE'" class="w-2.5 h-2.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="4" d="M5 13l4 4L19 7"></path></svg>
                   <!-- Point clignotant pour EN_COURS -->
                   <div v-else-if="op.activeExecStatut === 'EN_COURS'" class="w-1.5 h-1.5 bg-white rounded-full animate-pulse"></div>
                   <!-- Pause icon pour EN_PAUSE -->
                   <svg v-else-if="op.activeExecStatut === 'EN_PAUSE'" class="w-2 h-2 text-white" fill="currentColor" viewBox="0 0 24 24"><path d="M6 19h4V5H6v14zm8-14v14h4V5h-4z"></path></svg>
              </div>
              
              <!-- Code Opération -->
              <span class="text-[9px] font-bold mt-2 text-center truncate max-w-[55px]"
                    :class="[
                      op.activeExecStatut === 'EN_COURS' ? 'text-blue-700 font-extrabold' : 
                      op.activeExecStatut === 'CLOTURE' ? 'text-green-600' :
                      op.activeExecStatut === 'EN_PAUSE' ? 'text-yellow-600' :
                      'text-slate-400'
                    ]">
                {{ op.operationCode }}
              </span>

              <!-- Tooltip au survol -->
              <div class="absolute bottom-full mb-2 left-1/2 -translate-x-1/2 px-3 py-1.5 bg-slate-800 text-white text-[11px] font-medium rounded-md opacity-0 group-hover:opacity-100 transition-all pointer-events-none whitespace-nowrap z-50 shadow-xl border border-slate-700">
                {{ op.libelle }}
                <div class="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-800"></div>
              </div>
            </div>
          </div>
        </div>

        <div class="flex justify-between items-end border-t border-slate-100 pt-4 mt-auto">
          <div>
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Qté lancée</p>
            <p class="text-sm font-bold text-slate-700">{{ of.quantiteLancee }} / {{ of.quantitePrevue }}</p>
          </div>
          <div class="text-right">
            <p class="text-[10px] text-slate-400 uppercase font-bold tracking-wider mb-1">Date début</p>
            <p class="text-sm font-medium text-slate-600">{{ formatDate(of.dateDebut) }}</p>
          </div>
        </div>
      </div>
      
      <div v-if="filteredOfs.length === 0 && !loading" class="text-slate-500 mt-4">
        Aucun OF préparé disponible pour le moment dans cette catégorie.
      </div>
    </div>

    <!-- Modal Démarrer le contrôle -->
    <div v-if="showModal" class="fixed inset-0 bg-slate-900 bg-opacity-50 flex items-center justify-center z-50 p-4 backdrop-blur-sm transition-opacity">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl overflow-hidden flex flex-col max-h-[90vh]">
        
        <div class="p-6 border-b border-slate-100 flex justify-between items-start">
          <div>
            <h2 class="text-2xl font-bold text-slate-800">Démarrer le contrôle</h2>
            <p class="text-sm text-slate-500 mt-1">OF: {{ selectedOf.numeroOf }} - {{ selectedOf.designationArticle }}</p>
          </div>
          <button @click="fermerModal" class="text-slate-400 hover:text-slate-600">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
          </button>
        </div>

        <div v-if="errorMessage" class="m-6 p-4 bg-red-50 border border-red-200 rounded-xl flex items-start animate-fade-in-down">
          <svg class="w-6 h-6 text-red-500 mr-3 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
          <div>
            <h3 class="text-red-800 font-bold text-sm">Opération impossible</h3>
            <p class="text-red-600 text-sm mt-1">{{ errorMessage }}</p>
            
            <div v-if="errorMessage && errorMessage.includes('Aucun plan')">
              <p class="text-red-600 text-sm mt-1 font-semibold mb-3">Veuillez demander au responsable d'activer un plan pour cet article.</p>
              
              <button v-if="!planSignale" @click="signalerPlanManquant" :disabled="isSignalingPlan" class="px-4 py-2 bg-red-600 text-white text-xs font-bold rounded-lg hover:bg-red-700 transition flex items-center shadow-sm">
                <svg v-if="!isSignalingPlan" class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path></svg>
                <svg v-else class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                Signaler au Responsable DI
              </button>
              <div v-else class="text-green-700 font-bold text-sm flex items-center mt-2 bg-green-50 p-2 rounded-lg">
                <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
                Signalement envoyé avec succès !
              </div>
            </div>
          </div>
        </div>

        <div v-else class="p-6 overflow-y-auto flex-1">
          <p class="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4">Gamme Opératoire</p>
          
          <div class="space-y-3">
            <div v-for="(op, idx) in selectedOf.gammeOperatoire" :key="idx"
                 class="border rounded-xl transition-all"
                 :class="form.operationCode === op.operationCode ? 'border-blue-200 bg-blue-50/30' : 'border-slate-200 hover:border-slate-300'">
              
              <label class="flex justify-between items-center p-4 cursor-pointer">
                <div class="flex items-center">
                  <input type="radio" :value="op.operationCode" v-model="form.operationCode" @change="onOperationChange(op)"
                         class="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer">
                  <span class="ml-3 font-semibold text-slate-700">{{ op.libelle }}</span>
                </div>
                <div>
                  <span v-if="op.activeExecStatut === 'EN_COURS'" class="px-2 py-1 bg-green-100 text-green-700 text-xs font-bold rounded">EN COURS</span>
                  <span v-else-if="op.activeExecStatut === 'EN_PAUSE'" class="px-2 py-1 bg-yellow-100 text-yellow-700 text-xs font-bold rounded">EN PAUSE</span>
                  <span v-else-if="op.activeExecStatut === 'CLOTURE'" class="px-2 py-1 bg-gray-100 text-gray-700 text-xs font-bold rounded">CLÔTURÉ</span>
                  <span v-else class="px-2 py-1 bg-slate-100 text-slate-500 text-xs font-bold rounded">NON COMMENCÉ</span>
                </div>
              </label>

              <!-- Paramètres étendus si l'opération est sélectionnée -->
              <div v-if="form.operationCode === op.operationCode" class="px-4 pb-4 pt-1 animate-fade-in-down">
                <div class="flex gap-4">
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-500 uppercase tracking-wider mb-1">Machine / Poste Prévu</label>
                    <input type="text" :value="op.machinePrevueCode || 'N/A'" disabled 
                           class="w-full border border-slate-200 rounded-lg p-2.5 bg-white text-slate-500 text-sm font-medium">
                  </div>
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Machine / Poste Réel</label>
                    <input type="text" v-model="form.machineCode" placeholder="Machine utilisée"
                           class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none transition-all">
                  </div>
                  <div class="w-32">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Équipe</label>
                    <select v-model="form.numEquipe" class="w-full border border-slate-300 rounded-lg p-2.5 bg-white text-slate-800 text-sm font-medium focus:ring-2 focus:ring-blue-100 focus:border-blue-400 outline-none">
                      <option value="1">Matin</option>
                      <option value="2">A-Midi</option>
                      <option value="3">Nuit</option>
                    </select>
                  </div>
                </div>

                <div v-if="form.operationCode === 'TRONC' || form.operationCode === 'TRN'" class="flex gap-4 mt-4 animate-fade-in-down border-t border-blue-100 pt-4">
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Longueur (mm)</label>
                    <input type="number" step="0.01" v-model="form.longueur" placeholder="Ex: 120.5" :disabled="isLongueurInitialized"
                           class="w-full border border-slate-200 rounded-lg p-2.5 text-sm font-medium focus:outline-none transition-all"
                           :class="isLongueurInitialized ? 'bg-slate-50 text-slate-500 cursor-not-allowed' : 'bg-white text-slate-800 focus:ring-2 focus:ring-blue-100 focus:border-blue-400'">
                  </div>
                  <div class="flex-1">
                    <label class="block text-[10px] font-bold text-slate-600 uppercase tracking-wider mb-1">Diamètre (mm)</label>
                    <input type="number" step="0.01" v-model="form.diametre" placeholder="Ex: 15.2" :disabled="isDiametreInitialized"
                           class="w-full border border-slate-200 rounded-lg p-2.5 text-sm font-medium focus:outline-none transition-all"
                           :class="isDiametreInitialized ? 'bg-slate-50 text-slate-500 cursor-not-allowed' : 'bg-white text-slate-800 focus:ring-2 focus:ring-blue-100 focus:border-blue-400'">
                  </div>
                </div>
              </div>
            </div>
            
            <div v-if="!selectedOf.gammeOperatoire || selectedOf.gammeOperatoire.length === 0" class="text-sm text-red-500 p-4 bg-red-50 rounded-lg">
              Aucune gamme opératoire définie pour cet article.
            </div>
          </div>
        </div>

        <div class="p-5 border-t border-slate-100 bg-slate-50 flex justify-end space-x-3">
          <button @click="fermerModal" class="px-5 py-2.5 rounded-lg text-slate-600 font-semibold hover:bg-slate-200 transition-colors">
            {{ planSignale ? 'OK' : 'Annuler' }}
          </button>
          <button v-if="!errorMessage && (!selectedOpState || !selectedOpState.activeExecControleOfId)" @click="demarrerOf" :disabled="!form.operationCode"
                  class="px-5 py-2.5 rounded-lg bg-blue-600 text-white font-semibold flex items-center hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Démarrer le contrôle
          </button>
          <button v-if="!errorMessage && selectedOpState && selectedOpState.activeExecStatut !== 'CLOTURE' && selectedOpState.activeExecControleOfId" @click="reprendreOfExistant" :disabled="!form.operationCode"
                  class="px-5 py-2.5 rounded-lg bg-green-600 text-white font-semibold flex items-center hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Reprendre le contrôle
          </button>
          <button v-if="selectedOpState && selectedOpState.activeExecStatut === 'CLOTURE'" disabled
                  class="px-5 py-2.5 rounded-lg bg-gray-200 text-gray-500 font-semibold flex items-center cursor-not-allowed">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
            Opération Clôturée
          </button>
        </div>

      </div>
    </div>

    <!-- Modal Interception Départ -->
    <Dialog v-model:visible="showLeaveModal" modal header="Attention : OF toujours actif" :style="{ width: '450px' }" :closable="false">
      <div class="p-2 flex flex-col items-center">
        <div class="bg-yellow-100 p-4 rounded-full mb-4">
          <svg class="w-10 h-10 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
        </div>
        <p class="text-center text-slate-700 font-medium mb-6">
          Vous êtes sur le point de quitter cette page alors que l'OF <span class="font-bold text-slate-900">{{ activeOfContext.numeroOf }}</span> est toujours <span class="text-green-600 font-bold">EN COURS</span>.
          <br><br>Que souhaitez-vous faire ?
        </p>
        <div class="flex flex-col gap-3 w-full">
          <button @click="confirmLeave('pause')" class="w-full px-4 py-3 bg-yellow-500 text-white font-bold rounded-lg hover:bg-yellow-600 shadow-sm transition">
            ⏸️ Mettre l'OF en Pause et quitter
          </button>
          <button @click="confirmLeave('cloture')" class="w-full px-4 py-3 bg-red-600 text-white font-bold rounded-lg hover:bg-red-700 shadow-sm transition">
            ⏹️ Clôturer définitivement l'opération et quitter
          </button>
          <button @click="confirmLeave('quitter')" class="w-full px-4 py-3 bg-blue-600 text-white font-bold rounded-lg hover:bg-blue-700 shadow-sm transition">
            ▶️ Laisser l'OF en cours et quitter quand même
          </button>
          <button @click="cancelLeave" class="w-full px-4 py-3 bg-slate-200 text-slate-700 font-bold rounded-lg hover:bg-slate-300 shadow-sm transition mt-2">
            Annuler (Rester sur la page)
          </button>
        </div>
      </div>
    </Dialog>

    <!-- Modal Clôture Explicite -->
    <Dialog v-model:visible="showClotureModal" modal header="Confirmation de clôture" :style="{ width: '450px' }" :closable="false">
      <div class="p-2 flex flex-col items-center">
        <div class="bg-red-100 p-4 rounded-full mb-4">
          <svg class="w-10 h-10 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
        </div>
        <p class="text-center text-slate-700 font-medium mb-6">
          Êtes-vous sûr de vouloir clôturer l'opération pour cet OF ?<br><br>
          <span class="text-red-600 font-bold">Attention :</span> Tous les contrôles intermédiaires non réalisés seront ignorés.
        </p>
        <div class="flex flex-col gap-3 w-full">
          <button @click="confirmCloture" class="w-full px-4 py-3 bg-red-600 text-white font-bold rounded-lg hover:bg-red-700 shadow-sm transition">
            ⏹️ Oui, clôturer l'opération
          </button>
          <button @click="showClotureModal = false" class="w-full px-4 py-3 bg-slate-200 text-slate-700 font-bold rounded-lg hover:bg-slate-300 shadow-sm transition mt-2">
            Annuler
          </button>
        </div>
      </div>
    </Dialog>
    <!-- Modal Motif Pause Obligatoire -->
    <div v-if="raisonModalConfig.show" class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100] p-4 backdrop-blur-sm">
      <div class="bg-white rounded-xl max-w-md w-full p-6 shadow-2xl animate-fade-in-up">
        <h3 class="text-xl font-bold text-gray-900 mb-2 flex items-center">
          <svg class="w-6 h-6 mr-2 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>
          {{ raisonModalConfig.titre }}
        </h3>
        <p class="text-sm text-gray-600 mb-4">{{ raisonModalConfig.description }}</p>
        <textarea v-model="raisonModalConfig.texte" 
                  rows="3" 
                  class="w-full border border-gray-300 rounded-lg shadow-sm focus:border-yellow-500 focus:ring-yellow-500 p-3 mb-4 text-sm" 
                  placeholder="Saisissez le motif (obligatoire)..."
                  @keyup.enter="confirmerRaison"
                  autofocus></textarea>
        <div class="flex justify-end gap-3">
          <button @click="annulerRaison" class="px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg font-medium transition-colors">Annuler</button>
          <button @click="confirmerRaison" 
                  class="px-4 py-2 bg-yellow-600 text-white hover:bg-yellow-700 rounded-lg font-medium shadow-sm transition-colors disabled:opacity-50 disabled:cursor-not-allowed" 
                  :disabled="!raisonModalConfig.texte.trim()">
            Confirmer la Pause
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch, onUnmounted } from 'vue';
import { useRoute, useRouter, onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import operateurService from '@/services/operateurService';
import alertesService from '@/services/alertesService';
import AlerteControle from '@/components/Operateur/AlerteControle.vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import Dialog from 'primevue/dialog';
import { useAuthStore } from '@/stores/authStore';

const toast = useToast();
const route = useRoute();
const router = useRouter();

import { useOperateurStore } from '@/stores/execution/operateurStore';

const operateurStore = useOperateurStore();

const activeOfContext = computed({
  get: () => operateurStore.activeOfContext,
  set: (val) => operateurStore.setActiveOfContext(val)
});

const ofs = computed(() => operateurStore.ofs);
const loading = computed(() => operateurStore.loadingOfs);

const filteredOfs = computed(() => {
  return ofs.value.filter(of => {
    const isFini = of.gammeOperatoire && of.gammeOperatoire.some(o => o.operationCode === 'ASS');
    if (route.path.includes('/of-fini')) {
      return isFini;
    } else if (route.path.includes('/of-semi-fini')) {
      return !isFini;
    }
    return true;
  });
});

const showModal = ref(false);
const selectedOf = ref(null);
const errorMessage = ref('');

const authStore = useAuthStore();

const form = ref({
  numeroOf: '',
  operationCode: '',
  machineCode: '',
  numEquipe: 1,
  matriculeOperateur: authStore.user?.matricule || 'OP_INCONNU',
  longueur: null,
  diametre: null
});

const isSignalingPlan = ref(false);
const planSignale = ref(false);
const isLongueurInitialized = ref(false);
const isDiametreInitialized = ref(false);

const formatDate = (dateStr) => {
  if (!dateStr) return 'N/A';
  const d = new Date(dateStr);
  return d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
};

const chargerOfs = async () => {
  try {
    await operateurStore.chargerOfs();
    
    // Si on arrive sur la page avec un execId dans l'URL, on l'ouvre
    if (route.query.execution) {
      const execId = route.query.execution;
      let foundOf = null;
      let foundOp = null;
      
      for (const o of ofs.value) {
        if (o.gammeOperatoire) {
          const op = o.gammeOperatoire.find(x => x.activeExecControleOfId === execId);
          if (op) {
            foundOf = o;
            foundOp = op;
            break;
          }
        }
      }

      if (foundOf && foundOp) {
        activeOfContext.value = {
          id: foundOp.activeExecControleOfId,
          numeroOf: foundOf.numeroOf,
          operationCode: foundOp.operationCode,
          machineCode: foundOp.activeMachineCode,
          statut: foundOp.activeExecStatut,
          estEnReglage: foundOp.estEnReglage || false,
          a_Des_Controles_Reglage: foundOp.a_Des_Controles_Reglage || false,
          legendeMoyens: foundOp.legendeMoyens,
          remarques: foundOp.remarques
        };
      }
    }
  } catch (error) {
    console.error("Erreur chargement OFs:", error);
    if(error.message === 'Network Error') {
        alert("Impossible de contacter le serveur. Le backend (API) est-il démarré ?");
    }
  }
};

const selectedOpState = computed(() => {
  if (!selectedOf.value || !form.value.operationCode) return null;
  return selectedOf.value.gammeOperatoire.find(o => o.operationCode === form.value.operationCode);
});

const reprendreOfExistant = () => {
  const opState = selectedOpState.value;
  if (opState && opState.activeExecControleOfId) {
    const ofNumero = selectedOf.value.numeroOf;
    fermerModal();
    activeOfContext.value = {
      id: opState.activeExecControleOfId,
      numeroOf: ofNumero,
      operationCode: opState.operationCode,
      machineCode: opState.activeMachineCode,
      statut: opState.activeExecStatut,
      estEnReglage: opState.estEnReglage || false,
      a_Des_Controles_Reglage: opState.a_Des_Controles_Reglage || false
    };
    router.push({ query: { execution: opState.activeExecControleOfId } });
  }
};

const ouvrirModalDemarrage = (of) => {
  selectedOf.value = of;
  errorMessage.value = '';
  planSignale.value = false;
  form.value = {
    numeroOf: of.numeroOf,
    operationCode: '',
    machineCode: '',
    numEquipe: 1,
    matriculeOperateur: authStore.user?.matricule || 'OP_INCONNU',
    longueur: null,
    diametre: null
  };
  
  showModal.value = true;
};

const fermerModal = () => {
  showModal.value = false;
  selectedOf.value = null;
};

const signalerPlanManquant = async () => {
  isSignalingPlan.value = true;
  try {
    const data = {
      operationCode: form.value.operationCode || (selectedOf.value.gammeOperatoire && selectedOf.value.gammeOperatoire.length > 0 ? selectedOf.value.gammeOperatoire[0].operationCode : 'Inconnu'),
      posteCode: form.value.machineCode || 'Non spécifié',
      articleCode: selectedOf.value.codeArticle,
      descriptionProbleme: errorMessage.value || 'Plan manquant ou introuvable.'
    };
    await alertesService.signalerPlanManquant(data);
    planSignale.value = true;
    toast.add({ severity: 'success', summary: 'Signalé', detail: 'Le responsable a été notifié par email.', life: 4000 });
  } catch (error) {
    const msg = error.response?.data?.message || 'Impossible d\'envoyer le signalement.';
    toast.add({ severity: 'error', summary: 'Erreur', detail: msg, life: 6000 });
  } finally {
    isSignalingPlan.value = false;
  }
};

const onOperationChange = async (op) => {
  form.value.operationCode = op.operationCode;
  form.value.machineCode = op.machinePrevueCode || '';
  form.value.longueur = null;
  form.value.diametre = null;
  isLongueurInitialized.value = false;
  isDiametreInitialized.value = false;
  errorMessage.value = '';

  if (selectedOf.value && selectedOf.value.gammeOperatoire) {
    const currentIndex = selectedOf.value.gammeOperatoire.findIndex(o => o.operationCode === op.operationCode);
    if (currentIndex > 0) {
      // Vérifier l'opération juste avant
      const prevOp = selectedOf.value.gammeOperatoire[currentIndex - 1];
      // Si l'opération précédente n'a pas de statut (donc NON COMMENCÉ)
      if (!prevOp.activeExecStatut) {
        errorMessage.value = `L'opération précédente (${prevOp.operationCode || prevOp.operationLibelle}) n'est pas encore commencée. Vous devez commencer les opérations dans l'ordre de la gamme.`;
        return;
      }
    }
  }
  
  if (selectedOf.value?.codeArticle) {
    try {
      const res = await operateurService.verifierPlan(selectedOf.value.codeArticle, op.operationCode);
      if (!res.data.existe) {
        errorMessage.value = "Aucun plan de contrôle n'est actif pour cet article.";
      } else {
        const isTronnage = op.operationCode === 'TRONC' || op.operationCode === 'TRN';
        if (isTronnage) {
          if (res.data.longueur != null) {
            form.value.longueur = res.data.longueur;
            isLongueurInitialized.value = true;
          }
          if (res.data.diametre != null) {
            form.value.diametre = res.data.diametre;
            isDiametreInitialized.value = true;
          }
        }
      }
    } catch (err) {
      console.error(err);
      errorMessage.value = "Erreur lors de la vérification du plan.";
    }
  }
};

const demarrerOf = async () => {
  errorMessage.value = '';
  try {
    const res = await operateurService.demarrerOf({ ...form.value });
    fermerModal();
    activeOfContext.value = {
      id: res.data.id,
      numeroOf: res.data.numeroOf,
      operationCode: res.data.operationCode,
      machineCode: res.data.machineCode,
      statut: res.data.statut,
      estEnReglage: res.data.estEnReglage,
      a_Des_Controles_Reglage: res.data.a_Des_Controles_Reglage
    };
    router.push({ query: { execution: res.data.id } });
  } catch (error) {
    errorMessage.value = error.response?.data?.message || error.message;
  }
};

const mettreEnReglage = async () => {
  try {
    await operateurService.mettreEnReglage(activeOfContext.value.id);
    toast.add({ severity: 'success', summary: 'Mode Réglage', detail: 'Production mise en pause. Procédez aux contrôles de réglage.', life: 4000 });
    activeOfContext.value.estEnReglage = true;
    chargerOfs();
  } catch (error) {
    console.error(error);
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de mettre en réglage.', life: 3000 });
  }
};

const raisonModalConfig = ref({
  show: false,
  texte: '',
  titre: '',
  description: '',
  action: null,
  payload: null
});

const demanderRaisonPause = (titre, description, actionCallback, payload = null) => {
  raisonModalConfig.value = {
    show: true,
    texte: '',
    titre,
    description,
    action: actionCallback,
    payload
  };
};

const annulerRaison = () => {
  raisonModalConfig.value.show = false;
  actionEnCours.value = false;
};

const confirmerRaison = () => {
  if (!raisonModalConfig.value.texte.trim()) return;
  const { action, texte, payload } = raisonModalConfig.value;
  raisonModalConfig.value.show = false;
  if (action) {
    action(texte, payload);
  }
};

const mettreEnPause = () => {
  demanderRaisonPause(
    'Mettre en Pause',
    'Voulez-vous vraiment mettre la production en pause ? Veuillez saisir le motif (obligatoire) :',
    async (raison) => {
      try {
        await operateurService.mettreEnPause(activeOfContext.value.id, raison);
        toast.add({ severity: 'info', summary: 'Production en Pause', detail: 'L\'OF est maintenant en pause.', life: 4000 });
        activeOfContext.value.statut = 'EN_PAUSE';
        chargerOfs();
      } catch (error) {
        console.error(error);
        toast.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de mettre en pause.', life: 3000 });
      }
    }
  );
};

const reprendre = async () => {
  try {
    await operateurService.reprendreDepuisPause(activeOfContext.value.id);
    toast.add({ severity: 'success', summary: 'Reprise', detail: 'La production a repris.', life: 4000 });
    activeOfContext.value.statut = 'EN_COURS';
    activeOfContext.value.estEnReglage = false;
    chargerOfs();
  } catch (error) {
    console.error(error);
    toast.add({ severity: 'error', summary: 'Erreur', detail: error.response?.data?.message || 'Impossible de reprendre la production.', life: 3000 });
  }
};

const showClotureModal = ref(false);

const cloturer = () => {
  showClotureModal.value = true;
};

const confirmCloture = async () => {
  showClotureModal.value = false;
  try {
    await operateurService.cloturerOf(activeOfContext.value.id);
    activeOfContext.value = {}; // <-- Empêche l'intercepteur de route de bloquer la navigation
    quitterExecution();
    chargerOfs();
  } catch (error) {
    console.error(error);
  }
};

const quitterExecution = () => {
  if (activeOfContext.value && activeOfContext.value.statut !== 'EN_COURS') {
    activeOfContext.value = {};
  }
  router.push({ path: route.path, query: {} });
};

// --- GESTION DE LA NAVIGATION ET PROTECTION (PAUSE/CLÔTURE) ---

const showLeaveModal = ref(false);
const nextRouteOrQuery = ref(null);
const actionEnCours = ref(false);

watch(() => raisonModalConfig.value.show, (newVal) => {
  if (newVal) operateurStore.isPollingPaused = true;
  else operateurStore.isPollingPaused = false;
});

watch(showClotureModal, (newVal) => {
  if (newVal) operateurStore.isPollingPaused = true;
  else operateurStore.isPollingPaused = false;
});

watch(showLeaveModal, (newVal) => {
  if (newVal) operateurStore.isPollingPaused = true;
  else operateurStore.isPollingPaused = false;
});

watch(showModal, (newVal) => {
  if (newVal) operateurStore.isPollingPaused = true;
  else operateurStore.isPollingPaused = false;
});

const handleNavigation = (to) => {
  if (!authStore.token) return true; // Contourner si déconnexion en cours
  if (activeOfContext.value && activeOfContext.value.id && activeOfContext.value.statut === 'EN_COURS') {
    if (to.path !== route.path || !to.query.execution) {
      nextRouteOrQuery.value = to;
      showLeaveModal.value = true;
      return false;
    }
  }
  return true;
};

onBeforeRouteLeave((to, from) => {
  return handleNavigation(to);
});

onBeforeRouteUpdate((to, from) => {
  return handleNavigation(to);
});

const executeLeaveAction = async (actionType, raison = null) => {
  try {
    if (actionType === 'pause') {
      await operateurService.mettreEnPause(activeOfContext.value.id, raison);
      toast.add({ severity: 'success', summary: 'Succès', detail: 'OF mis en pause.', life: 3000 });
    } else if (actionType === 'cloture') {
      await operateurService.cloturerOf(activeOfContext.value.id);
      toast.add({ severity: 'success', summary: 'Succès', detail: 'OF clôturé.', life: 3000 });
    }
    
    showLeaveModal.value = false;
    activeOfContext.value = {};
    
    if (nextRouteOrQuery.value) {
      router.push(nextRouteOrQuery.value);
    }
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Action impossible.', life: 3000 });
  } finally {
    actionEnCours.value = false;
  }
};

const confirmLeave = (action) => {
  if (actionEnCours.value) return;
  actionEnCours.value = true;
  
  if (action === 'pause') {
    showLeaveModal.value = false;
    demanderRaisonPause(
      'Mettre en Pause avant de quitter',
      'Voulez-vous vraiment mettre la production en pause avant de quitter ? Veuillez saisir le motif (obligatoire) :',
      async (raison) => {
        await executeLeaveAction('pause', raison);
      }
    );
  } else {
    executeLeaveAction(action);
  }
};

const cancelLeave = () => {
  showLeaveModal.value = false;
  nextRouteOrQuery.value = null;
};

const handleUnload = (e) => {
  if (activeOfContext.value && activeOfContext.value.id && activeOfContext.value.statut === 'EN_COURS') {
    // Si l'opérateur ferme l'application (ou rafraîchit), on essaie discrètement de mettre l'OF en pause
    // On ne bloque plus la navigation avec preventDefault pour ne pas empêcher le F5
    const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5246';
    const url = `${apiUrl}/api/Operateur/of/${activeOfContext.value.id}/pause`;
    
    fetch(url, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ raison: "Fermeture inattendue de l'application" }),
      keepalive: true
    }).catch(() => {});
  }
};

onMounted(() => {
  chargerOfs();
  
  const refreshInterval = setInterval(chargerOfs, 60000);
  
  window.addEventListener('beforeunload', handleUnload);
  window.addEventListener('unload', handleUnload);

  onUnmounted(() => {
    clearInterval(refreshInterval);
    window.removeEventListener('beforeunload', handleUnload);
    window.removeEventListener('unload', handleUnload);
  });
});

watch(() => route.query.execution, (newVal) => {
  if (!newVal) {
    activeOfContext.value = {};
  } else {
    chargerOfs();
  }
});
</script>

<style scoped>
.animate-fade-in-down {
  animation: fadeInDown 0.2s ease-out;
}
@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-5px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
