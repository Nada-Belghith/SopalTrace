<script setup>
import { ref } from 'vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
const isMobileMenuOpen = ref(false);

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
        
        <!-- SECTION 0 : MODÈLE DES PLANS -->
        <div v-if="authStore.isResponsable" class="px-6 mb-2">
          <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Modèle des Plans</p>
        </div>
        <ul v-if="authStore.isResponsable" class="space-y-1 px-3 mb-6">
          <li class="pl-3 ml-5 border-l border-slate-700">
            <router-link to="/dev/fab/nouveau?mode=fabrication" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white group" active-class="bg-slate-800 text-white">
              <i class="pi pi-plus-circle text-blue-400 group-hover:rotate-90 transition-transform"></i> En cours de fabrication
            </router-link>
          </li>
        </ul>

        <!-- SECTION 1 : PLANS GÉNÉRIQUES -->
        <div v-if="authStore.isResponsable" class="px-6 mb-2 border-t border-slate-800 pt-6">
          <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Plans Génériques</p>
        </div>
        <ul v-if="authStore.isResponsable" class="space-y-1 px-3 mb-6">
          <li>
            <router-link to="/dev/hub" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-blue-600/10 text-blue-400 border border-blue-500/20 font-bold">
              <i class="pi pi-objects-column"></i> Plans Génériques
            </router-link>
          </li>
          
          <li class="pl-3 ml-5 border-l border-slate-700 mt-2 mb-4">
            <ul class="space-y-1">
              <li>
                <router-link to="/dev/fab/nouveau?mode=assembly" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white group" active-class="bg-slate-800 text-white">
                  <i class="pi pi-plus-circle text-blue-400 group-hover:rotate-90 transition-transform"></i> En cours d'assemblage
                </router-link>
              </li>
              <li>
                <router-link to="/dev/produit-fini/nouveau" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white" active-class="bg-slate-800 text-white">
                  <i class="pi pi-plus-circle text-indigo-400"></i> Contrôle Produit Fini 
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
              <li>
                <router-link to="/dev/resultat-controle/nouveau" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white" active-class="bg-slate-800 text-white">
                  <i class="pi pi-plus-circle text-red-400"></i> Résultat Contrôle Poste
                </router-link>
              </li>
            </ul>
          </li>
        </ul>

        <!-- SECTION 2 : PRODUCTION -->
        <template v-if="authStore.userRole !== 'MAGASINIER'">
          <div class="px-6 mb-2 border-t border-slate-800 pt-6">
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Plans par articles</p>
          </div>
          <ul class="space-y-1 px-3 mb-6">
            <li>
              <router-link to="/dev/hub-plans" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-emerald-600/10 text-emerald-400 border border-emerald-500/20 font-bold">
                <i class="pi pi-table"></i> Plans par article
              </router-link>
            </li>
            <li v-if="authStore.isResponsable" class="pl-3 ml-5 border-l border-slate-700 mt-2 mb-4">
              <ul class="space-y-1">
                <li>
                  <router-link to="/dev/fab/plans/nouveau" class="flex items-center gap-3 px-3 py-2 rounded-lg transition-all text-xs font-medium text-slate-400 hover:bg-slate-800 hover:text-white group" active-class="bg-slate-800 text-white">
                    <i class="pi pi-plus-circle text-emerald-400 group-hover:rotate-90 transition-transform"></i> Créer plan par article
                  </router-link>
                </li>
              </ul>
            </li>
          </ul>
        </template>

        <!-- SECTION : MAGASINIER -->
        <div v-if="authStore.userRole === 'MAGASINIER'" class="px-6 mb-2 border-t border-slate-800 pt-6">
          <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Magasin</p>
        </div>
        <ul v-if="authStore.userRole === 'MAGASINIER'" class="space-y-1 px-3 mb-6">
          <li>
            <router-link to="/magasinier/scan-of" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white" active-class="bg-blue-600/10 text-blue-400 border border-blue-500/20 font-bold">
              <i class="pi pi-qrcode"></i> Scan Ordre de Fab (OF)
            </router-link>
          </li>
        </ul>

        <!-- SECTION 3 : ANALYSES -->
        <template v-if="authStore.userRole !== 'MAGASINIER'">
          <div class="px-6 mt-8 mb-2 border-t border-slate-800 pt-6">
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Analyses</p>
          </div>
          <ul class="space-y-1 px-3">
            <li>
              <a href="#" class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-sm font-medium hover:bg-slate-800 hover:text-white">
                <i class="pi pi-chart-line"></i> Dashboard & Traçabilité
              </a>
            </li>
          </ul>
        </template>
      </nav>

      <!-- Footer Sidebar (User) -->
      <div class="p-4 border-t border-slate-800 bg-[#0f172a] shrink-0">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3 px-1 overflow-hidden">
            <div class="w-9 h-9 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-500 flex items-center justify-center text-white font-black text-xs shadow-lg shadow-blue-500/20">
              {{ getInitials(authStore.userName) }}
            </div>
            <div class="overflow-hidden">
              <p class="text-xs font-bold text-white truncate">{{ authStore.userName }}</p>
              <p class="text-[9px] font-black text-slate-500 uppercase tracking-widest truncate">{{ authStore.userRole }}</p>
            </div>
          </div>
          
          <button @click="handleLogout" class="p-2 text-slate-500 hover:text-red-400 transition-colors" title="Déconnexion">
            <i class="pi pi-power-off"></i>
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
