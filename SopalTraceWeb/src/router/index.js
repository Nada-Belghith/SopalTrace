import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '@/layouts/MainLayout.vue'
import { useAuthStore } from '@/stores/authStore'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/Auth/LoginView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/Auth/RegisterView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/forgot-password',
      name: 'forgot-password',
      component: () => import('@/views/Auth/ForgotPasswordView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/',
      redirect: '/dev/hub'
    },
    {
      path: '/dev',
      component: MainLayout,
      meta: { requiresAuth: true }, // Toutes les routes sous /dev demandent une connexion
      children: [
        // 1. LE HUB (Le choix du type de plan - Image 1)
        {
          path: 'hub',
          name: 'dev-hub',
          component: () => import('@/views/QualityPlans/DevModelHub.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE', 'QUALITE'] } // Exemple de restriction
        },
        {
          path: 'hub-plans',
          name: 'dev-hub-plans',
          component: () => import('@/views/QualityPlans/DevPlanHub.vue'),
        },

        // 2. ÉDITEUR FABRICATION (TRN, ESP, USI) - GABARITS
        // Gère les Corps et Volants pour les 3 premières opérations
        {
          path: 'fab/nouveau', 
          name: 'dev-fab-modele-create',
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE'] }
        },
        {
          path: 'fab/editer/:id',
          name: 'dev-fab-edit',
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE'] }
        },

        // === NOUVEAU : ÉDITEUR PLANS PAR ARTICLE (Production) ===
        {
          path: 'fab/plans/nouveau',
          name: 'dev-fab-plan-create',
          component: () => import('@/views/QualityPlans/Fabrication/FabPlanEditor.vue'),
        },
        {
          path: 'fab/plans/editer/:id',
          name: 'dev-fab-plan-edit',
          component: () => import('@/views/QualityPlans/Fabrication/FabPlanEditor.vue'),
        },

        // 3. ÉDITEUR ASSEMBLAGE (Plan Maître & Résultats)
        // Gère RGAFM (Manu), RGAFA (Auto), Soupape
        // {
        //   path: 'assemblage/nouveau',
        //   name: 'dev-ass-create',
        //   component: () => import('@/views/QualityPlans/Assemblage/AssModeleEditor.vue'),
        // },

        // 4. ÉDITEUR PRODUIT FINI
        {
          path: 'produit-fini/nouveau',
          name: 'dev-pf-create',
          component: () => import('@/views/QualityPlans/ProduitFini/PfPlanEditor.vue'),
        },
        {
          path: 'produit-fini/editer/:id',
          name: 'dev-pf-edit',
          component: () => import('@/views/QualityPlans/ProduitFini/PfPlanEditor.vue'),
        },

        // 5. ÉDITEUR VÉRIF MACHINE (BEE, MAS, SER...)
        {
          path: 'verif-machine/nouveau',
          name: 'dev-vm-create',
          component: () => import('@/views/QualityPlans/VerifMachine/VmModeleEditor.vue'),
        },
        {
          path: 'verif-machine/editer/:id',
          name: 'dev-vm-edit',
          component: () => import('@/views/QualityPlans/VerifMachine/VmModeleEditor.vue'),
        },

        // 6. RÉSULTAT DE CONTRÔLE (NC)
        {
          path: 'resultat-controle/nouveau',
          name: 'dev-rc-create',
          component: () => import('@/views/QualityPlans/ResultatControle/RcModeleEditor.vue'),
        },
        {
          path: 'resultat-controle/editer/:id',
          name: 'dev-rc-edit',
          component: () => import('@/views/QualityPlans/ResultatControle/RcModeleEditor.vue'),
        },

        // 7. ÉDITEUR ÉCHANTILLONNAGE (Niveaux ISO)
        {
          path: 'echantillonnage/nouveau',
          name: 'dev-ech-create',
          component: () => import('@/views/QualityPlans/Echantillonnage/EchModeleEditor.vue'),
        },
        {
          path: 'echantillonnage/editer/:id',
          name: 'dev-ech-edit',
          component: () => import('@/views/QualityPlans/Echantillonnage/EchModeleEditor.vue'),
        }
      ]
    },
    {
      path: '/magasinier',
      name: 'magasinier-root',
      component: MainLayout,
      meta: { requiresAuth: true, roles: ['ADMIN', 'MAGASINIER'] },
      children: [
        {
          path: 'scan-of',
          name: 'magasinier-scan-of',
          component: () => import('@/views/Magasinier/OfScannerView.vue')
        }
      ]
    }
  ]
})

// Navigation Guard — style Vue Router 4 (return au lieu de next)
router.beforeEach(async (to) => {
  const authStore = useAuthStore();

  // 1. Vérification de l'authentification
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth !== false);

  if (requiresAuth && !authStore.isAuthenticated) {
    authStore.logout(); // logout() redirige déjà vers /login
    return false;
  }

  // 2. Si l'user est déjà connecté et tente d'aller sur /login
  if (to.name === 'login' && authStore.isAuthenticated) {
    if (authStore.userRole === 'MAGASINIER') return { name: 'magasinier-scan-of' };
    return { name: 'dev-hub' };
  }

  // 3. Vérification des rôles (Routage Intelligent)
  const requiredRoles = to.meta.roles;
  if (requiredRoles && !authStore.hasAccess(requiredRoles)) {
    if (authStore.userRole === 'MAGASINIER') return { name: 'magasinier-scan-of' };
    return { name: 'dev-hub' }; // Accès refusé → redirection hub
  }

  // ✅ Laisser passer
  return true;
});

export default router
