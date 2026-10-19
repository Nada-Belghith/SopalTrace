<template>
  <div class="min-h-screen flex items-center justify-center bg-[#0f172a] p-4 font-sans relative overflow-hidden">
    <!-- Décorations d'arrière-plan -->
    <div class="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-blue-600/10 rounded-full blur-[120px]"></div>
    <div class="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] bg-indigo-600/10 rounded-full blur-[120px]"></div>

    <div class="w-full max-w-md animate-in fade-in zoom-in duration-700">
      <!-- Logo / Header -->
      <div class="text-center mb-10">
        <div class="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-2xl shadow-2xl shadow-blue-500/20 mb-6 rotate-3">
          <i class="pi pi-user-plus text-4xl text-white"></i>
        </div>
        <h1 class="text-4xl font-black text-white tracking-tight mb-2">Inscription</h1>
        <p class="text-slate-400 font-medium">Rejoignez la plateforme SopalTrace</p>
      </div>

      <!-- Carte Inscription -->
      <div class="bg-slate-800/50 backdrop-blur-xl border border-slate-700 p-8 rounded-3xl shadow-2xl">

        <!-- Étape 1 : Saisie du matricule et email -->
        <form v-if="!emailSent" @submit.prevent="handleRegister" class="space-y-5">
          <!-- Info sécurité -->
          <div class="bg-blue-500/10 border border-blue-500/20 p-4 rounded-2xl flex items-start gap-3">
            <i class="pi pi-shield text-blue-400 mt-0.5 text-lg"></i>
            <p class="text-xs text-blue-300 leading-relaxed">
              Saisissez votre matricule et l'email professionnel enregistré dans l'ERP pour ce matricule
              (toute autre adresse est ignorée).<br>
              Votre demande est validée par un administrateur avant l'envoi de votre lien d'activation.
            </p>
          </div>

          <!-- Matricule -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Matricule Employé</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-id-card text-lg"></i>
              </span>
              <input 
                v-model="form.matricule"
                type="text" 
                required
                placeholder="Ex: 45891"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
            </div>
          </div>

          <!-- Email -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Email Professionnel</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-envelope text-lg"></i>
              </span>
              <input 
                v-model="form.email"
                type="email" 
                required
                placeholder="email@sopal.com"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
            </div>
          </div>

          <!-- Erreur -->
          <div v-if="authStore.error" class="bg-red-500/10 border border-red-500/20 p-4 rounded-2xl flex items-start gap-3">
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
            <template v-else>
              <i class="pi pi-send"></i>
              <span>Envoyer ma demande</span>
            </template>
          </button>
        </form>

        <!-- Étape 2 : Confirmation email envoyé -->
        <div v-else class="text-center space-y-6">
          <div class="inline-flex items-center justify-center w-20 h-20 bg-emerald-500/20 border border-emerald-500/30 rounded-full mx-auto">
            <i class="pi pi-check-circle text-5xl text-emerald-400"></i>
          </div>
          <div>
            <h2 class="text-xl font-black text-white mb-2">Demande transmise</h2>
            <p class="text-slate-400 text-sm leading-relaxed">
              Si le matricule et l'e-mail correspondent à votre fiche employé, vous recevrez un e-mail à<br>
              <span class="text-blue-400 font-bold">{{ form.email }}</span>
            </p>
          </div>
          <div class="bg-slate-900/50 border border-slate-700 p-4 rounded-2xl text-left space-y-2">
            <p class="text-xs font-bold text-slate-400 uppercase tracking-wider">Prochaines étapes</p>
            <div class="flex items-start gap-2 text-xs text-slate-300">
              <i class="pi pi-1 text-blue-400 mt-0.5"></i>
              <span>Un administrateur valide votre demande et vous attribue un rôle (sauf si votre accès a été préparé à l'avance)</span>
            </div>
            <div class="flex items-start gap-2 text-xs text-slate-300">
              <i class="pi pi-2 text-blue-400 mt-0.5"></i>
              <span>Vous recevez un lien d'activation personnel, utilisable une seule fois</span>
            </div>
            <div class="flex items-start gap-2 text-xs text-slate-300">
              <i class="pi pi-3 text-blue-400 mt-0.5"></i>
              <span>Vous choisissez votre mot de passe, puis vous vous connectez</span>
            </div>
          </div>
          <p class="text-[11px] text-slate-500">Rien reçu ? Vérifiez vos courriers indésirables, puis contactez l'administrateur.</p>
          <router-link to="/login" class="inline-block w-full bg-slate-700 hover:bg-slate-600 text-white py-3 rounded-2xl font-bold transition-colors text-center">
            Retour à la connexion
          </router-link>
        </div>

        <div class="mt-8 pt-6 border-t border-slate-700/50 text-center">
          <p class="text-slate-400 text-sm font-medium">
            Déjà un compte ? 
            <router-link to="/login" class="text-blue-400 font-bold hover:text-blue-300 transition-colors ml-1">
              Se connecter
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
import { ref, reactive } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from 'primevue/usetoast';

const authStore = useAuthStore();
const toast = useToast();

const emailSent = ref(false);
const form = reactive({
  matricule: '',
  email: ''
});

const handleRegister = async () => {
  try {
    await authStore.register(form.matricule, form.email);
    emailSent.value = true;
  } catch (err) {
    const errorMsg = err.message || "Erreur lors de l'inscription";
    toast.add({
      severity: 'error',
      summary: "Erreur d'inscription",
      detail: errorMsg,
      life: 5000
    });
  }
};
</script>
