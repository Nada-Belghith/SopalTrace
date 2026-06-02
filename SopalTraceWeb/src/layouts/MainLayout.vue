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
        
        <template v-if="authStore.isResponsable || authStore.userRole === 'SUPERVISEUR_QUALITE'">
          <!-- SECTION 1 : STRUCTURE DES PLANS -->
          <div class="px-6 mb-3">
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Structure des plans</p>
          </div>
          <ul class="px-4 mb-6">
            <li>
              <router-link to="/dev/fab/specifique" class="flex items-center gap-3 px-4 py-3 rounded-xl border border-transparent transition-all text-sm font-semibold text-slate-400 hover:bg-slate-800/50 hover:text-white" active-class="bg-[#241e17] text-amber-500 border-amber-500/20 shadow-[0_0_15px_rgba(245,158,11,0.08)] font-bold">
                <i class="pi pi-list text-amber-500"></i> Plans Spécifiques
              </router-link>
            </li>
          </ul>

          <!-- SECTION 2 : PLANS GÉNÉRIQUES -->
          <div class="px-6 mb-3 flex items-center gap-2.5">
            <i class="pi pi-th-large text-blue-500 text-lg"></i>
            <span class="text-sm font-bold text-slate-200">Plans Génériques</span>
          </div>
          <ul class="ml-[2.1rem] pl-4 border-l border-slate-800/80 space-y-2 mb-6">
            <li>
              <router-link to="/dev/fab/nouveau?mode=assembly" class="flex items-center gap-2.5 py-1.5 text-xs font-semibold text-slate-400 hover:text-white transition-colors" active-class="text-blue-400 font-bold">
                <i class="pi pi-plus-circle text-blue-500 text-sm"></i> En cours d'assemblage
              </router-link>
            </li>
            <li>
              <router-link to="/dev/produit-fini/nouveau" class="flex items-center gap-2.5 py-1.5 text-xs font-semibold text-slate-400 hover:text-white transition-colors" active-class="text-blue-400 font-bold">
                <i class="pi pi-plus-circle text-blue-500 text-sm"></i> Contrôle Produit Fini
              </router-link>
            </li>
            <li>
              <router-link to="/dev/verif-machine/nouveau" class="flex items-center gap-2.5 py-1.5 text-xs font-semibold text-slate-400 hover:text-white transition-colors" active-class="text-orange-400 font-bold">
                <i class="pi pi-plus-circle text-orange-500 text-sm"></i> Vérification Machine
              </router-link>
            </li>
            <li>
              <router-link to="/dev/echantillonnage/nouveau" class="flex items-center gap-2.5 py-1.5 text-xs font-semibold text-slate-400 hover:text-white transition-colors" active-class="text-purple-400 font-bold">
                <i class="pi pi-plus-circle text-purple-500 text-sm"></i> Échantillonnage
              </router-link>
            </li>
            <li>
              <router-link to="/dev/resultat-controle/nouveau" class="flex items-center gap-2.5 py-1.5 text-xs font-semibold text-slate-400 hover:text-white transition-colors" active-class="text-red-400 font-bold">
                <i class="pi pi-plus-circle text-red-500 text-sm"></i> Résultat Contrôle Poste
              </router-link>
            </li>
          </ul>
        </template>

        <!-- SECTION : PRODUCTION (OPERATEUR) -->
        <template v-if="authStore.userRole === 'OPERATEUR'">
          <div class="px-6 mb-2">
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Production</p>
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
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Analyses</p>
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
            <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Magasin</p>
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
