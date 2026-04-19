<template>
  <div class="flex h-screen bg-slate-50 font-sans overflow-hidden">
    
    <!-- SIDEBAR (Sombre) -->
    <aside class="w-[260px] bg-[#111827] text-slate-300 flex flex-col shrink-0 transition-all duration-300">
      <!-- Header Sidebar -->
      <div class="h-16 flex items-center gap-3 px-6 border-b border-slate-800 bg-[#0f172a]">
        <i class="pi pi-cog text-emerald-400 text-xl animate-spin-slow"></i>
        <span class="font-black text-white tracking-wide text-sm uppercase">Qualité Admin</span>
      </div>

      <!-- Navigation Principale -->
      <nav class="flex-1 overflow-y-auto py-6 custom-scrollbar">
        
        <!-- SECTION 1 : MODÈLES DE CONTRÔLE -->
        <div class="px-6 mb-2">
          <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Modèles de Contrôle</p>
        </div>
        <ul class="space-y-1 px-3 mb-6">
          <li>
            <router-link to="/dev/hub" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-blue-600/10 text-blue-400 border border-blue-500/20 font-bold">
              <i class="pi pi-objects-column"></i> Tous les Modèles
            </router-link>
          </li>
          
          <!-- Sous-menu de Création (Indenté élégamment) -->
          <li class="pl-3 ml-5 border-l border-slate-700 mt-2 mb-4">
            <ul class="space-y-1">
              <li>
                <router-link to="/dev/fab/nouveau" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white group" active-class="bg-slate-800 text-white">
                  <i class="pi pi-plus-circle text-blue-400 group-hover:rotate-90 transition-transform"></i> En cours Production
                </router-link>
              </li>
              <li>
                <router-link to="/dev/assemblage/nouveau" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white" active-class="bg-slate-800 text-white">
                  <i class="pi pi-plus-circle text-indigo-400"></i> contrôle Produit Fini 
                </router-link>
              </li>
              <li>
                <router-link to="/dev/verif-machine/nouveau" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white" active-class="bg-slate-800 text-white">
                  <i class="pi pi-plus-circle text-orange-400"></i> Vérification Machine
                </router-link>
              </li>
              <li>
                <router-link to="/dev/echantillonnage/nouveau" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white" active-class="bg-slate-800 text-white">
                  <i class="pi pi-plus-circle text-purple-400"></i> Échantillonnage
                </router-link>
              </li>
            </ul>
          </li>
        </ul>

        <!-- SECTION 2 : PRODUCTION -->
        <div class="px-6 mb-2 border-t border-slate-800 pt-6">
          <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Production</p>
        </div>
        <ul class="space-y-1 px-3 mb-6">
          <li>
            <router-link to="/dev/hub-plans" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-emerald-600/10 text-emerald-400 border border-emerald-500/20 font-bold">
              <i class="pi pi-table"></i> Tous les plans
            </router-link>
          </li>
          <li>
            <router-link to="/dev/fab/plans/nouveau" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-emerald-600/10 text-emerald-400 border border-emerald-500/20 font-bold">
              <i class="pi pi-sliders-v"></i> Créer plan pour article
            </router-link>
          </li>
        </ul>

        <!-- SECTION 3 : ANALYSES -->
        <div class="px-6 mt-8 mb-2 border-t border-slate-800 pt-6">
          <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Analyses</p>
        </div>
        <ul class="space-y-1 px-3">
          <li>
            <a href="#" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white">
              <i class="pi pi-chart-line"></i> Dashboard & Traçabilité
            </a>
          </li>
          <li>
            <a href="#" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white">
              <i class="pi pi-cloud-upload"></i> Gestion Documentaire
            </a>
          </li>
        </ul>
      </nav>

      <!-- Footer Sidebar (User) -->
      <div class="p-4 border-t border-slate-800 bg-[#0f172a]">
        <div class="flex items-center gap-3 px-2">
          <div class="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center text-white font-bold text-xs">
            AQ
          </div>
          <div class="overflow-hidden">
            <p class="text-xs font-bold text-white truncate">Admin Qualité</p>
            <p class="text-[10px] text-slate-400 truncate">Département Technique</p>
          </div>
        </div>
      </div>
    </aside>

    <!-- ZONE CENTRALE (Le contenu des pages s'affiche ici) -->
    <main class="flex-1 flex flex-col h-full overflow-hidden relative">
      <div class="flex-1 overflow-y-auto p-4 md:p-8">
        
        <!-- C'est ici que DevPlanHub ou les Éditeurs s'injectent -->
        <router-view /> 

      </div>
    </main>

  </div>
</template>

<script setup>
// Le layout gère la navigation latérale globale de l'application
</script>

<style scoped>
.animate-spin-slow {
  animation: spin 4s linear infinite;
}

/* Personnalisation subtile de la barre de défilement pour la sidebar */
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: #334155;
  border-radius: 20px;
}
</style>
