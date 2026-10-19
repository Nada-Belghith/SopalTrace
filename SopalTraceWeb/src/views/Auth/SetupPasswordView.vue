<template>
  <div class="min-h-screen flex items-center justify-center bg-[#0f172a] p-4 font-sans relative overflow-hidden">
    <!-- Décorations d'arrière-plan -->
    <div class="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-emerald-600/10 rounded-full blur-[120px]"></div>
    <div class="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] bg-blue-600/10 rounded-full blur-[120px]"></div>

    <div class="w-full max-w-md animate-in fade-in zoom-in duration-700">
      <!-- Logo / Header -->
      <div class="text-center mb-10">
        <div class="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-tr from-emerald-600 to-blue-500 rounded-2xl shadow-2xl shadow-emerald-500/20 mb-6">
          <i class="pi pi-lock text-4xl text-white"></i>
        </div>
        <h1 class="text-3xl font-black text-white tracking-tight mb-2">Définir mon mot de passe</h1>
        <p class="text-slate-400 font-medium">Activez votre compte SopalTrace</p>
      </div>

      <!-- Carte -->
      <div class="bg-slate-800/50 backdrop-blur-xl border border-slate-700 p-8 rounded-3xl shadow-2xl">

        <!-- Succès -->
        <div v-if="success" class="text-center space-y-6">
          <div class="inline-flex items-center justify-center w-20 h-20 bg-emerald-500/20 border border-emerald-500/30 rounded-full mx-auto">
            <i class="pi pi-check-circle text-5xl text-emerald-400"></i>
          </div>
          <div>
            <h2 class="text-xl font-black text-white mb-2">Compte activé !</h2>
            <p class="text-slate-400 text-sm leading-relaxed">
              Votre mot de passe a été défini avec succès.<br>
              Vous pouvez maintenant vous connecter.
            </p>
          </div>
          <router-link to="/login" class="inline-block w-full bg-emerald-600 hover:bg-emerald-500 text-white py-4 rounded-2xl font-black uppercase tracking-widest transition-colors text-center">
            <i class="pi pi-sign-in mr-2"></i>
            Se connecter
          </router-link>
        </div>

        <!-- Lien absent ou incomplet -->
        <div v-else-if="!jeton" class="text-center space-y-5">
          <div class="inline-flex items-center justify-center w-20 h-20 bg-orange-500/20 border border-orange-500/30 rounded-full mx-auto">
            <i class="pi pi-link text-4xl text-orange-400"></i>
          </div>
          <div>
            <h2 class="text-xl font-black text-white mb-2">Lien d'activation incomplet</h2>
            <p class="text-slate-400 text-sm leading-relaxed">
              Ouvrez cette page avec le bouton de l'e-mail d'activation reçu après validation de votre demande.
              Si le lien a expiré, refaites une demande d'inscription : un nouveau lien vous sera envoyé.
            </p>
          </div>
          <router-link to="/register" class="inline-block w-full bg-slate-700 hover:bg-slate-600 text-white py-4 rounded-2xl font-black uppercase tracking-widest transition-colors text-center">
            Refaire une demande
          </router-link>
        </div>

        <form v-else @submit.prevent="handleSetupPassword" class="space-y-5">
          <!-- Info -->
          <div class="bg-blue-500/10 border border-blue-500/20 p-4 rounded-2xl flex items-start gap-3">
            <i class="pi pi-info-circle text-blue-400 mt-0.5 text-lg"></i>
            <p class="text-xs text-blue-300 leading-relaxed">
              Choisissez votre mot de passe personnel. Ce lien ne fonctionne qu'une seule fois.
            </p>
          </div>

          <!-- Nouveau mot de passe -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Nouveau mot de passe</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-lock text-lg"></i>
              </span>
              <input 
                v-model="form.nouveauMotDePasse"
                :type="showNew ? 'text' : 'password'"
                required
                placeholder="Min 8 car., 1 majuscule, 1 chiffre"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-12 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
              <button 
                type="button"
                @click="showNew = !showNew"
                class="absolute inset-y-0 right-0 pr-4 flex items-center text-slate-500 hover:text-white transition-colors"
              >
                <i :class="showNew ? 'pi pi-eye-slash' : 'pi pi-eye'"></i>
              </button>
            </div>
          </div>

          <!-- Confirmation -->
          <div>
            <label class="block text-[11px] font-black text-slate-400 uppercase tracking-widest mb-2 ml-1">Confirmer le mot de passe</label>
            <div class="relative group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-slate-500 group-focus-within:text-blue-400 transition-colors">
                <i class="pi pi-check-circle text-lg"></i>
              </span>
              <input 
                v-model="form.confirmation"
                :type="showNew ? 'text' : 'password'"
                required
                placeholder="••••••••"
                class="w-full bg-slate-900/50 border border-slate-600 text-white pl-12 pr-4 py-4 rounded-2xl focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder:text-slate-600 font-bold"
              />
            </div>
            <!-- Indicateur de correspondance -->
            <p v-if="form.confirmation && form.nouveauMotDePasse !== form.confirmation" class="text-red-400 text-xs mt-1 ml-1">
              <i class="pi pi-times mr-1"></i> Les mots de passe ne correspondent pas
            </p>
            <p v-else-if="form.confirmation && form.nouveauMotDePasse === form.confirmation" class="text-emerald-400 text-xs mt-1 ml-1">
              <i class="pi pi-check mr-1"></i> Les mots de passe correspondent
            </p>
          </div>

          <!-- Règles -->
          <div class="bg-slate-900/50 border border-slate-700 p-3 rounded-xl text-xs text-slate-500 space-y-1">
            <div :class="form.nouveauMotDePasse.length >= 8 ? 'text-emerald-400' : 'text-slate-500'" class="flex items-center gap-2">
              <i :class="form.nouveauMotDePasse.length >= 8 ? 'pi pi-check' : 'pi pi-circle'"></i>
              Au moins 8 caractères
            </div>
            <div :class="/[A-Z]/.test(form.nouveauMotDePasse) ? 'text-emerald-400' : 'text-slate-500'" class="flex items-center gap-2">
              <i :class="/[A-Z]/.test(form.nouveauMotDePasse) ? 'pi pi-check' : 'pi pi-circle'"></i>
              Au moins 1 lettre majuscule
            </div>
            <div :class="/[0-9]/.test(form.nouveauMotDePasse) ? 'text-emerald-400' : 'text-slate-500'" class="flex items-center gap-2">
              <i :class="/[0-9]/.test(form.nouveauMotDePasse) ? 'pi pi-check' : 'pi pi-circle'"></i>
              Au moins 1 chiffre
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
            :disabled="authStore.isLoading || form.nouveauMotDePasse !== form.confirmation"
            class="w-full bg-emerald-600 hover:bg-emerald-500 disabled:bg-slate-700 disabled:cursor-not-allowed text-white py-4 rounded-2xl font-black uppercase tracking-widest shadow-xl shadow-emerald-600/20 transition-all flex items-center justify-center gap-3 active:scale-[0.98]"
          >
            <i v-if="authStore.isLoading" class="pi pi-spin pi-spinner text-xl"></i>
            <template v-else>
              <i class="pi pi-check-circle"></i>
              <span>Activer mon compte</span>
            </template>
          </button>
        </form>

        <div class="mt-6 pt-5 border-t border-slate-700/50 text-center">
          <router-link to="/login" class="text-slate-400 text-sm font-medium hover:text-white transition-colors">
            <i class="pi pi-arrow-left mr-1"></i> Retour à la connexion
          </router-link>
        </div>
      </div>

      <p class="text-center mt-8 text-slate-500 text-xs font-medium">
        &copy; 2026 SOPAL - Tous droits réservés.
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from 'primevue/usetoast';
import { lireJetonActivation } from '@/utils/jetonActivation';

const route = useRoute();
const authStore = useAuthStore();
const toast = useToast();

const success = ref(false);
const showNew = ref(false);

// Le jeton est après « # » (jamais envoyé au serveur web) ; il est retiré de la barre d'adresse dès la lecture.
const jeton = ref(lireJetonActivation(route.hash));
if (jeton.value && typeof window !== 'undefined') {
  window.history.replaceState(window.history.state, '', route.path);
}

const form = reactive({
  nouveauMotDePasse: '',
  confirmation: ''
});

const handleSetupPassword = async () => {
  if (form.nouveauMotDePasse !== form.confirmation) {
    toast.add({ severity: 'error', summary: 'Erreur', detail: 'Les mots de passe ne correspondent pas', life: 3000 });
    return;
  }
  if (form.nouveauMotDePasse.length < 8 || !/[A-Z]/.test(form.nouveauMotDePasse) || !/[0-9]/.test(form.nouveauMotDePasse)) {
    toast.add({ severity: 'error', summary: 'Mot de passe faible', detail: 'Minimum 8 caractères, 1 majuscule, 1 chiffre', life: 4000 });
    return;
  }
  try {
    await authStore.setupPassword(jeton.value, form.nouveauMotDePasse);
    success.value = true;
  } catch (err) {
    toast.add({
      severity: 'error',
      summary: 'Erreur',
      detail: err.status === 401
        ? 'Ce lien est expiré ou a déjà été utilisé. Refaites une demande d\'inscription pour en recevoir un nouveau.'
        : (err.message || 'Activation impossible.'),
      life: 6000
    });
  }
};
</script>
