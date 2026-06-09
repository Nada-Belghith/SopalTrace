import { defineStore } from 'pinia';
import { authService } from '@/services/authService';
import router from '@/router';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: JSON.parse(localStorage.getItem('user')) || null,
    token: localStorage.getItem('token') || null,
    isLoading: false,
    error: null
  }),

  getters: {
    isAuthenticated: (state) => {
      if (!state.token) return false;
      
      try {
        const base64Url = state.token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const payload = JSON.parse(window.atob(base64));
        const isExpired = Date.now() >= payload.exp * 1000;
        return !isExpired;
      } catch (e) {
        return false;
      }
    },
    userRole: (state) => state.user?.role || 'GUEST',
    userName: (state) => state.user?.nom || 'Utilisateur',
    
    // Vérification de permissions spécifiques
    isAdmin: (state) => state.user?.role === 'ADMIN',
    isResponsable: (state) => {
      const role = state.user?.role || '';
      return role === 'ADMIN' || role.includes('RESPONSABLE');
    },
    isOperateur: (state) => state.user?.role === 'OPERATEUR'
  },

  actions: {
    async login(matricule, password) {
      this.isLoading = true;
      this.error = null;
      try {
        const data = await authService.login(matricule, password);
        this.setSession(data, matricule);
        return true;
      } catch (err) {
        this.error = err.response?.data?.message || 'Identifiants invalides';
        throw err;
      } finally {
        this.isLoading = false;
      }
    },

    async register(matricule, email, password) {
      this.isLoading = true;
      this.error = null;
      try {
        const data = await authService.register(matricule, email, password);
        this.setSession(data, matricule);
        return true;
      } catch (err) {
        this.error = err.response?.data?.message || "Erreur lors de l'inscription";
        throw err;
      } finally {
        this.isLoading = false;
      }
    },

    setSession(data, matricule) {
      this.token = data.token;
      this.user = {
        nom: data.nomComplet,
        role: data.roleApp,
        matricule: matricule
      };
      localStorage.setItem('token', this.token);
      localStorage.setItem('user', JSON.stringify(this.user));
    },

    logout() {
      authService.logout().catch(() => {}); // Logout silencieux au back
      
      this.user = null;
      this.token = null;
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      
      router.push('/login');
    },

    /**
     * Vérifie si l'utilisateur a accès à une route selon son rôle
     */
    hasAccess(requiredRoles) {
      if (!requiredRoles || requiredRoles.length === 0) return true;
      if (!this.user || !this.user.role) return false;
      
      const userRole = this.user.role;
      return requiredRoles.some(role => 
        userRole === role || 
        (role === 'RESPONSABLE' && userRole.includes('RESPONSABLE'))
      );
    },

    async forgotPassword(email) {
      this.isLoading = true;
      this.error = null;
      try {
        await authService.forgotPassword(email);
        return true;
      } catch (err) {
        this.error = err.response?.data?.message || "Erreur lors de l'envoi du code";
        throw err;
      } finally {
        this.isLoading = false;
      }
    },

    async resetPassword(email, code, password) {
      this.isLoading = true;
      this.error = null;
      try {
        await authService.resetPassword(email, code, password);
        return true;
      } catch (err) {
        this.error = err.response?.data?.message || "Erreur lors de la réinitialisation";
        throw err;
      } finally {
        this.isLoading = false;
      }
    }
  }
});
