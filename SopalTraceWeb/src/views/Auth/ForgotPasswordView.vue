<template>
  <div class="min-h-screen flex items-center justify-center bg-[#0f172a] p-4 font-sans relative overflow-hidden">
    <!-- Décorations d'arrière-plan -->
    <div class="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-blue-600/10 rounded-full blur-[120px]"></div>
    <div class="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] bg-indigo-600/10 rounded-full blur-[120px]"></div>

    <div class="w-full max-w-md animate-in fade-in zoom-in duration-700">
      <!-- Logo / Header -->
      <div class="text-center mb-10">
        <div class="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-2xl shadow-2xl shadow-blue-500/20 mb-6 rotate-3">
          <i class="pi pi-key text-4xl text-white"></i>
        </div>
        <h1 class="text-4xl font-black text-white tracking-tight mb-2">Récupération</h1>
        <p class="text-slate-400 font-medium">Réinitialisez votre mot de passe</p>
      </div>

      <!-- Carte Récupération -->
      <div class="bg-slate-800/50 backdrop-blur-xl border border-slate-700 p-8 rounded-3xl shadow-2xl">
        
        <!-- ÉTAPE 1 : Saisie de l'Email -->
        <form v-if="step === 1" @submit.prevent="handleForgot" class="space-y-6">
          <div class="text-center mb-6">
            <p class="text-slate-300 text-sm">Entrez votre email pour recevoir un code de vérification.</p>
          </div>
          
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Email</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-envelope text-lg"></i>
              </span>
              <input 
                v-model="email"
                type="email" 
                required
                placeholder="email@sopal.com"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
            </div>
          </div>

          <button 
            type="submit" 
            :disabled="authStore.isLoading"
            class="w-full bg-blue-600 hover:bg-blue-500 disabled:bg-slate-700 text-white py-4 rounded-2xl font-black uppercase tracking-widest transition-all flex items-center justify-center gap-3"
          >
            <i v-if="authStore.isLoading" class="pi pi-spin pi-spinner text-xl"></i>
            <span v-else>Envoyer le code</span>
          </button>
        </form>

        <!-- ÉTAPE 2 : Saisie du Code et Nouveau Mot de Passe -->
        <form v-else @submit.prevent="handleReset" class="space-y-5">
          <div class="bg-blue-500/10 border border-blue-500/20 p-4 rounded-2xl mb-6">
            <p class="text-blue-400 text-xs font-bold text-center">
              Un code a été envoyé à <br/> <span class="text-white">{{ email }}</span>
            </p>
          </div>

          <!-- Code -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Code de vérification</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-hashtag text-lg"></i>
              </span>
              <input 
                v-model="resetForm.code"
                type="text" 
                required
                maxlength="6"
                placeholder="000000"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold text-center tracking-[0.5em]"
              />
            </div>
          </div>

          <!-- Nouveau Mot de passe -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Nouveau mot de passe</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-lock text-lg"></i>
              </span>
              <input 
                v-model="resetForm.password"
                type="password" 
                required
                placeholder="••••••••"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
            </div>
          </div>

          <!-- Confirmation -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Confirmer</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-check-circle text-lg"></i>
              </span>
              <input 
                v-model="resetForm.confirmPassword"
                type="password" 
                required
                placeholder="••••••••"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
            </div>
          </div>

          <button 
            type="submit" 
            :disabled="authStore.isLoading"
            class="w-full bg-green-600 hover:bg-green-500 disabled:bg-slate-700 text-white py-4 rounded-2xl font-black uppercase tracking-widest transition-all flex items-center justify-center gap-3"
          >
            <i v-if="authStore.isLoading" class="pi pi-spin pi-spinner text-xl"></i>
            <span v-else>Réinitialiser</span>
          </button>

          <button 
            type="button" 
            @click="step = 1"
            class="w-full text-slate-400 text-xs font-bold hover:text-white transition-colors uppercase tracking-widest mt-2"
          >
            Retour
          </button>
        </form>

        <div v-if="authStore.error" class="mt-4 bg-red-500/10 border border-red-500/20 p-4 rounded-2xl flex items-start gap-3">
          <i class="pi pi-exclamation-circle text-red-500 mt-0.5"></i>
          <p class="text-xs font-bold text-red-400">{{ authStore.error }}</p>
        </div>

        <div class="mt-8 pt-6 border-t border-slate-700/50 text-center">
          <router-link to="/login" class="text-blue-400 font-bold hover:text-blue-300 transition-colors text-sm">
            Retour à la connexion
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from 'primevue/usetoast';

const router = useRouter();
const authStore = useAuthStore();
const toast = useToast();

const step = ref(1);
const email = ref('');
const resetForm = reactive({
  code: '',
  password: '',
  confirmPassword: ''
});

const handleForgot = async () => {
  try {
    await authStore.forgotPassword(email.value);
    toast.add({
      severity: 'info',
      summary: 'Code envoyé',
      detail: 'Veuillez vérifier votre boîte mail',
      life: 5000
    });
    step.value = 2;
  } catch {
    // Erreur gérée par le store
  }
};

const handleReset = async () => {
  if (resetForm.password !== resetForm.confirmPassword) {
    toast.add({
      severity: 'error',
      summary: 'Erreur',
      detail: 'Les mots de passe ne correspondent pas',
      life: 3000
    });
    return;
  }

  try {
    await authStore.resetPassword(email.value, resetForm.code, resetForm.password);
    toast.add({
      severity: 'success',
      summary: 'Succès',
      detail: 'Votre mot de passe a été réinitialisé',
      life: 3000
    });
    router.push('/login');
  } catch {
    // Erreur gérée par le store
  }
};
</script>
"
