<template>
  <div class="min-h-screen flex items-center justify-center bg-[#0f172a] p-4 font-sans relative overflow-hidden">
    <!-- Décorations d'arrière-plan -->
    <div class="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-blue-600/10 rounded-full blur-[120px]"></div>
    <div class="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] bg-indigo-600/10 rounded-full blur-[120px]"></div>

    <div class="w-full max-w-md animate-in fade-in zoom-in duration-700">
      <!-- Logo / Header -->
      <div class="text-center mb-10">
        <div class="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-2xl shadow-2xl shadow-blue-500/20 mb-6 rotate-3">
          <i class="pi pi-shield text-4xl text-white"></i>
        </div>
        <h1 class="text-4xl font-black text-white tracking-tight mb-2">Sopal<span class="text-blue-500">Trace</span></h1>
        <p class="text-slate-400 font-medium">Système de Traçabilité & Qualité</p>
      </div>

      <!-- Carte Login -->
      <div class="bg-slate-800/50 backdrop-blur-xl border border-slate-700 p-8 rounded-3xl shadow-2xl">
        <form @submit.prevent="handleLogin" class="space-y-6">
          <!-- Matricule -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Matricule Employé</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-user text-lg"></i>
              </span>
              <input 
                v-model="matricule"
                type="text" 
                required
                placeholder="Ex: 4589"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
            </div>
          </div>

          <!-- Mot de passe -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Mot de passe</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-lock text-lg"></i>
              </span>
              <input 
                v-model="password"
                :type="showPassword ? 'text' : 'password'" 
                required
                placeholder="••••••••"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-12 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
              <button 
                type="button"
                @click="showPassword = !showPassword"
                class="absolute inset-y-0 right-0 pr-4 flex items-center text-slate-500 hover:text-white transition-colors"
              >
                <i :class="showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'"></i>
              </button>
            </div>
            <div class="flex justify-end">
              <router-link to="/forgot-password" class="text-xs font-bold text-blue-400 hover:text-blue-300 transition-colors">
                Mot de passe oublié ?
              </router-link>
            </div>
          </div>

          <!-- Erreur -->
          <div v-if="authStore.error" class="bg-red-500/10 border border-red-500/20 p-4 rounded-2xl flex items-start gap-3 animate-shake">
            <i class="pi pi-exclamation-circle text-red-500 mt-0.5"></i>
            <p class="text-xs font-bold text-red-400">{{ authStore.error }}</p>
          </div>

          <!-- Submit -->
          <button 
            type="submit" 
            :disabled="authStore.isLoading"
            class="w-full bg-blue-600 hover:bg-blue-500 disabled:bg-slate-700 text-white py-4 rounded-2xl font-black uppercase tracking-widest shadow-xl shadow-blue-600/20 hover:shadow-blue-500/30 transition-all flex items-center justify-center gap-3 active:scale-[0.98]"
          >
            <i v-if="authStore.isLoading" class="pi pi-spin pi-spinner text-xl"></i>
            <span v-else>Se Connecter</span>
          </button>
        </form>
        
        <!-- Lien Inscription -->
        <div class="mt-8 pt-6 border-t border-slate-700/50 text-center">
          <p class="text-slate-400 text-sm font-medium">
            Pas encore de compte ? 
            <router-link to="/register" class="text-blue-400 font-bold hover:text-blue-300 transition-colors ml-1">
              S'inscrire
            </router-link>
          </p>
        </div>
      </div>

      <!-- Footer -->
      <p class="text-center mt-8 text-slate-500 text-xs font-medium">
        &copy; 2026 SOPAL - Tous droits réservés.
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from 'primevue/usetoast';

const router = useRouter();
const authStore = useAuthStore();
const toast = useToast();

const matricule = ref('');
const password = ref('');
const showPassword = ref(false);

const handleLogin = async () => {
  try {
    await authStore.login(matricule.value, password.value);
    
    toast.add({
      severity: 'success',
      summary: 'Connexion réussie',
      detail: `Bienvenue, ${authStore.userName}`,
      life: 3000
    });

    // Redirection intelligente
    router.push('/');
  } catch {
    // L'erreur est gérée dans le store
  }
};
</script>

<style scoped>
.animate-shake {
  animation: shake 0.5s cubic-bezier(.36,.07,.19,.97) both;
}

@keyframes shake {
  10%, 90% { transform: translate3d(-1px, 0, 0); }
  20%, 80% { transform: translate3d(2px, 0, 0); }
  30%, 50%, 70% { transform: translate3d(-4px, 0, 0); }
  40%, 60% { transform: translate3d(4px, 0, 0); }
}
</style>
