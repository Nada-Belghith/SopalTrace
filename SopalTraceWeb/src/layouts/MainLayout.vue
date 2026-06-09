<script setup>
import { ref } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { useRoute } from 'vue-router';

const authStore = useAuthStore();
const route = useRoute();
const isMobileMenuOpen = ref(false);

const isDocActive = (pathPrefix, mode = null) => {
  const isPathMatch = route.path.startsWith(pathPrefix);
  if (!isPathMatch) return false;
  
  // Eviter la collision (ex: '/dev/resultat-controle' qui matche '/dev/resultat-controle-cf')
  if (route.path.length > pathPrefix.length && route.path[pathPrefix.length] !== '/') {
      return false;
  }

  if (mode) {
    return route.query.mode === mode;
  }
  return true;
};

const handleLogout = () => {
  authStore.logout();
};

// Initiales pour l'avatar
const getInitials = (name) => {
  if (!name) return '??';
  return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
};

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value;
};
</script>

<template>
  <div class="flex h-screen bg-slate-50 font-sans overflow-hidden flex-col md:flex-row">
    
    <!-- MOBILE HEADER (visible on mobile only) -->
    <header class="md:hidden h-16 bg-[#111827] text-white flex items-center justify-between px-4 border-b border-slate-800 shrink-0 z-30">
      <div class="flex items-center gap-3">
        <button @click="toggleMobileMenu" class="p-2 -ml-2 text-slate-300 hover:text-white transition-colors" aria-label="Menu principal">
          <i class="pi pi-bars text-xl"></i>
        </button>
        <div class="flex items-center gap-2">
          <i class="pi pi-cog text-emerald-400 text-lg animate-spin-slow"></i>
          <span class="font-black tracking-wide text-xs uppercase text-white">Sopal Trace</span>
        </div>
      </div>
      
      <div class="flex items-center gap-3">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-tr from-blue-600 to-indigo-500 flex items-center justify-center text-white font-black text-xs shadow-md">
          {{ getInitials(authStore.userName) }}
        </div>
      </div>
    </header>

    <!-- MOBILE NAVIGATION OVERLAY (Drawer Backdrop) -->
    <div 
      v-if="isMobileMenuOpen" 
      @click="isMobileMenuOpen = false" 
      class="md:hidden fixed inset-0 bg-black/60 z-40 transition-opacity"
    ></div>

    <!-- SIDEBAR (Sombre - Responsive Drawer) -->
    <aside 
      class="fixed inset-y-0 left-0 w-[260px] bg-[#111827] text-slate-300 flex flex-col z-50 transform transition-transform duration-300 ease-in-out md:static md:translate-x-0 shrink-0"
      :class="isMobileMenuOpen ? 'translate-x-0 shadow-2xl' : '-translate-x-full md:translate-x-0'"
    >
      <!-- Header Sidebar -->
      <div class="h-16 flex items-center justify-between px-6 border-b border-slate-800 bg-[#0f172a]">
        <div class="flex items-center gap-3">
          <i class="pi pi-cog text-emerald-400 text-xl animate-spin-slow"></i>
          <span class="font-black text-white tracking-wide text-sm uppercase">Sopal Trace</span>
        </div>
        <!-- Close button on Mobile drawer -->
        <button @click="isMobileMenuOpen = false" class="md:hidden p-1 text-slate-400 hover:text-white" aria-label="Fermer le menu">
          <i class="pi pi-times text-lg"></i>
        </button>
      </div>

      <!-- Navigation Principale -->
      <nav class="flex-1 overflow-y-auto py-6 custom-scrollbar" @click="isMobileMenuOpen = false">
        
        <!-- SECTION : SUPERVISEUR QUALITE -->
        <template v-if="authStore.userRole === 'SUPERVISEUR_QUALITE' || authStore.userRole === 'ADMIN'">
          <!-- SECTION 1 : STRUCTURE DES PLANS -->
          <div class="px-6 mb-3">
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Structure des plans</p>
          </div>
          
          <ul class="px-4 mb-6 space-y-4">
            <!-- Plans Spécifiques Group -->
            <li>
              <router-link to="/dev/hub-plans" class="flex items-center gap-3 px-4 py-3 rounded-xl transition-all text-sm font-bold text-slate-400 hover:text-white hover:bg-slate-800/50" active-class="bg-[#241e17] text-amber-500 border-amber-500/20 shadow-[0_0_15px_rgba(245,158,11,0.08)] font-bold">
                <i class="pi pi-list text-amber-500 text-lg"></i>
                <span>Plans Spécifiques</span>
              </router-link>
              <ul class="ml-[2.1rem] pl-4 border-l border-slate-800/80 space-y-2 mt-2">
                <li>
                  <router-link to="/dev/fab/specifique" class="flex items-center gap-2.5 py-1.5 text-xs font-semibold text-slate-400 hover:text-white transition-colors" active-class="text-amber-400 font-bold">
                    <i class="pi pi-plus-circle text-amber-500 text-sm"></i> Plan de contrôle en cours de fabrication
                  </router-link>
                </li>
              </ul>
            </li>

            <!-- Documents Génériques Group -->
            <li>
              <router-link to="/dev/hub" class="flex items-center gap-3 px-4 py-3 rounded-xl border border-transparent transition-all text-sm font-semibold text-white hover:bg-slate-800/50" active-class="bg-[#1e293b] text-blue-400 border-blue-500/20 shadow-[0_0_15px_rgba(59,130,246,0.08)] font-bold">
                <i class="pi pi-th-large text-blue-500 text-lg"></i>
                <span>Documents Génériques</span>
              </router-link>
              <ul class="ml-[2.1rem] pl-4 border-l border-slate-800/80 space-y-1 mt-2">
                <li>
                  <router-link to="/dev/ass/nouveau" class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800/50 transition-colors" active-class="text-blue-400 font-bold bg-white/10 border border-white/5 shadow-sm">
                    <i class="pi pi-plus-circle text-blue-500 text-sm"></i> En cours d'assemblage
                  </router-link>
                </li>
                <li>
                  <router-link to="/dev/produit-fini/nouveau" class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800/50 transition-colors" active-class="text-blue-400 font-bold bg-white/10 border border-white/5 shadow-sm">
                    <i class="pi pi-plus-circle text-blue-500 text-sm"></i> Contrôle Produit Fini
                  </router-link>
                </li>
                <li>
                  <router-link to="/dev/verif-machine/nouveau" class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800/50 transition-colors" active-class="text-orange-400 font-bold bg-white/10 border border-white/5 shadow-sm">
                    <i class="pi pi-plus-circle text-orange-500 text-sm"></i> Vérification Machine
                  </router-link>
                </li>
                <li>
                  <router-link to="/dev/echantillonnage/nouveau" class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800/50 transition-colors" active-class="text-purple-400 font-bold bg-white/10 border border-white/5 shadow-sm">
                    <i class="pi pi-plus-circle text-purple-500 text-sm"></i> Échantillonnage
                  </router-link>
                </li>
                <li>
                  <router-link to="/dev/resultat-controle/nouveau" class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800/50 transition-colors" active-class="text-red-400 font-bold bg-white/10 border border-white/5 shadow-sm">
                    <i class="pi pi-plus-circle text-red-500 text-sm"></i> Résultat Contrôle Poste
                  </router-link>
                </li>
                <li>
                  <router-link to="/dev/resultat-controle-cf/nouveau" class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800/50 transition-colors" active-class="text-emerald-400 font-bold bg-white/10 border border-white/5 shadow-sm">
                    <i class="pi pi-plus-circle text-emerald-500 text-sm"></i> Résultat Contrôle C.F.
                  </router-link>
                </li>
              </ul>
            </li>
          </ul>
        </template>

        <!-- SECTION : RESPONSABLE DI -->
        <template v-if="authStore.isResponsable || authStore.userRole === 'ADMIN'">
          <!-- SECTION 1 : PLANS DE CONTRÔLES -->
          <div class="px-6 mb-3">
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Plans de contrôles</p>
          </div>
          
          <ul class="px-4 space-y-4 mb-6">
            <!-- Modèles de Fabrication Group -->
            <li>
              <router-link to="/dev/fab/modeles" class="flex items-center gap-3 px-4 py-3 rounded-xl border border-transparent transition-all text-sm font-semibold text-white hover:bg-slate-800/50" active-class="bg-[#241e17] text-amber-500 border-amber-500/20 shadow-[0_0_15px_rgba(245,158,11,0.08)] font-bold">
                <i class="pi pi-cog text-amber-500 text-lg"></i>
                <span>Modèles de Fabrication</span>
              </router-link>
              
              <!-- Sub-items nested under Modèles de Fabrication -->
              <ul class="ml-8 pl-4 border-l border-slate-800/80 space-y-2 mt-2">
                <li>
                  <router-link to="/dev/fab/nouveau?mode=fabrication" 
                    class="flex items-center gap-2.5 px-3 py-2 text-xs font-semibold rounded-lg transition-all"
                    :class="isDocActive('/dev/fab', 'fabrication') ? 'bg-slate-800 text-white border border-slate-700 shadow-md shadow-slate-900/50' : 'text-slate-400 hover:text-white hover:bg-slate-800/50'">
                    <i class="pi pi-plus-circle text-amber-500 text-sm"></i> Créer un Modèle
                  </router-link>
                </li>
              </ul>
            </li>

            <!-- Plans Spécifiques Group -->
            <li>
              <div class="flex items-center gap-3 px-4 py-3 rounded-xl border border-transparent transition-all text-sm font-semibold text-white">
                <i class="pi pi-th-large text-emerald-400 text-lg"></i>
                <span>Plans spécifiques</span>
              </div>
              
              <!-- Sub-items nested under Plans Spécifiques -->
              <ul class="ml-8 pl-4 border-l border-slate-800/80 space-y-2 mt-2">
                <li>
                  <router-link to="/dev/fab/plans/nouveau" 
                    class="flex items-center gap-2.5 px-3 py-2 text-xs font-semibold rounded-lg transition-all"
                    :class="isDocActive('/dev/fab/plans') ? 'bg-slate-800 text-white border border-slate-700 shadow-md shadow-slate-900/50' : 'text-slate-400 hover:text-white hover:bg-slate-800/50'">
                    <i class="pi pi-plus-circle text-emerald-400 text-sm"></i> Créer plan par article
                  </router-link>
                </li>
              </ul>
            </li>

            <!-- Documents Génériques Group (Lecture Seule) -->
            <li>
              <router-link to="/dev/hub" class="flex items-center gap-3 px-4 py-3 rounded-xl border border-transparent transition-all text-sm font-semibold text-white hover:bg-slate-800/50" active-class="bg-[#1e293b] text-blue-400 border-blue-500/20 shadow-[0_0_15px_rgba(59,130,246,0.08)] font-bold">
                <i class="pi pi-th-large text-blue-500 text-lg"></i>
                <span>Documents Génériques</span>
              </router-link>
            </li>
          </ul>
        </template>

        <!-- SECTION : PRODUCTION (OPERATEUR) -->
        <template v-if="authStore.userRole === 'OPERATEUR'">
          <div class="px-6 mb-2">
            <p class="text-xs font-black text-slate-500 uppercase tracking-widest">Production</p>
          </div>
          <ul class="space-y-1 px-3 mb-6">
            <li>
              <router-link to="/dev/hub-plans" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-emerald-600/10 text-emerald-400 border border-emerald-500/20 font-bold">
                <i class="pi pi-table"></i> Plans par article
              </router-link>
            </li>
          </ul>
        </template>

        <!-- SECTION 3 : ANALYSES -->
        <template v-if="authStore.userRole !== 'MAGASINIER'">
          <div class="px-6 mt-8 mb-3 border-t border-slate-800/80 pt-6">
            <p class="text-xs font-black text-slate-500 uppercase tracking-widest">Analyses</p>
          </div>
          <ul class="px-4 mb-6">
            <li>
              <router-link to="/superviseur/dashboard" class="flex items-center gap-3 px-4 py-3 rounded-xl border border-transparent transition-all text-sm font-semibold text-slate-400 hover:bg-slate-800/50 hover:text-white" active-class="bg-slate-800/40 text-white font-bold">
                <i class="pi pi-chart-line text-lg text-slate-400"></i> Dashboard & Traçabilité
              </router-link>
            </li>
          </ul>
        </template>

        <!-- SECTION : MAGASINIER -->
        <template v-if="authStore.userRole === 'MAGASINIER'">
          <div class="px-6 mb-2 border-t border-slate-800 pt-6">
            <p class="text-xs font-black text-slate-500 uppercase tracking-widest">Magasin</p>
          </div>
          <ul class="space-y-1 px-3 mb-6">
            <li>
              <router-link to="/magasinier/scan-of" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-blue-600/10 text-blue-400 border border-blue-500/20 font-bold">
                <i class="pi pi-qrcode"></i> Scan Ordre de Fab (OF)
              </router-link>
            </li>
          </ul>
        </template>
      </nav>

      <!-- Footer Sidebar (User) -->
      <div class="p-4 border-t border-slate-800/60 bg-[#0f172a] shrink-0">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3 px-1 overflow-hidden">
            <div class="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center text-white font-bold text-sm shadow-md">
              {{ getInitials(authStore.userName) }}
            </div>
            <div class="overflow-hidden">
              <p class="text-xs font-bold text-white truncate">{{ authStore.userName }}</p>
              <p class="text-[9px] font-black text-slate-500 uppercase tracking-widest truncate">{{ authStore.userRole }}</p>
            </div>
          </div>
          
          <button @click="handleLogout" class="p-2 text-slate-500 hover:text-red-400 transition-colors" title="Déconnexion">
            <i class="pi pi-power-off text-lg"></i>
          </button>
        </div>
      </div>
    </aside>

    <!-- ZONE CENTRALE (Le contenu des pages s'affiche ici) -->
    <main class="flex-1 flex flex-col h-full overflow-hidden relative">
      <div class="flex-1 overflow-y-auto p-4 md:p-6 lg:p-8">
        <router-view :key="$route.fullPath" /> 
      </div>
    </main>

  </div>
</template>

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
