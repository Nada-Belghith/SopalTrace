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
          meta: { roles: ['ADMIN', 'RESPONSABLE_DI', 'RESPONSABLE_QUALITE', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'hub-plans',
          name: 'dev-hub-plans',
          component: () => import('@/views/QualityPlans/DevPlanHub.vue'),
        },

        // --- ASS : MODELISATION DES PLANS D'ASSEMBLAGE ---
        {
          path: 'ass/nouveau',
          name: 'dev-ass-create',
          component: () => import('@/views/QualityPlans/Assemblage/AssModeleEditor.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE_QUALITE', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'ass/editer/:id',
          name: 'dev-ass-edit',
          component: () => import('@/views/QualityPlans/Assemblage/AssModeleEditor.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE', 'SUPERVISEUR_QUALITE'] }
        },

        // --- EXECUTION DES PLANS ---
        {
          path: 'exec/encf/:id',
          name: 'exec-encf-form',
          component: () => import('@/components/Execution/ExecEncfForm.vue'),
        },

        // 2. ÉDITEUR FABRICATION (TRN, ESP, USI) - GABARITS
        // Gère les Corps et Volants pour les 3 premières opérations
        {
          path: 'fab/modeles',
          name: 'dev-fab-modeles-hub',
          component: () => import('@/views/QualityPlans/DevModelHub.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE_DI', 'RESPONSABLE_QUALITE', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'fab/nouveau',
          name: 'dev-fab-modele-create',
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE_DI'] }
        },
        {
          path: 'fab/specifique',
          name: 'dev-fab-specifique',
          component: () => import('@/views/QualityPlans/Fabrication/FormStructureEditor.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'fab/editer/:id',
          name: 'dev-fab-edit',
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE_DI', 'SUPERVISEUR_QUALITE'] }
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
          component: () => import('@/views/QualityPlans/ControlePoste/ControlePosteEditor.vue'),
        },
        {
          path: 'resultat-controle/editer/:id',
          name: 'dev-rc-edit',
          component: () => import('@/views/QualityPlans/ControlePoste/ControlePosteEditor.vue'),
        },

        // NOUVEAU: RÉSULTAT CONTRÔLE C.F.
        {
          path: 'resultat-controle-cf/nouveau',
          name: 'dev-rccf-create',
          component: () => import('@/views/QualityPlans/ResultatControleCF/RccfModeleEditor.vue'),
        },
        {
          path: 'resultat-controle-cf/editer/:id',
          name: 'dev-rccf-edit',
          component: () => import('@/views/QualityPlans/ResultatControleCF/RccfModeleEditor.vue'),
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
    },
    {
      path: '/superviseur',
      name: 'superviseur-root',
      component: MainLayout,
      meta: { requiresAuth: true, roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] },
      children: [
        {
          path: 'dashboard',
          name: 'superviseur-dashboard',
          component: () => import('@/views/Superviseur/SuperviseurDashboardView.vue')
        }
      ]
    }
  ]
})

// Navigation Guard — style Vue Router 4
router.beforeEach(async (to, from) => {
  const authStore = useAuthStore();

  const requiresAuth = to.matched.some(record => record.meta.requiresAuth !== false);

  if (requiresAuth && !authStore.isAuthenticated) {
    return { name: 'login' };
  }

  if (to.name === 'login' && authStore.isAuthenticated) {
    if (authStore.userRole === 'MAGASINIER') return { name: 'magasinier-scan-of' };
    if (authStore.userRole === 'SUPERVISEUR_QUALITE') return { name: 'superviseur-dashboard' };
    if (authStore.userRole === 'OPERATEUR') return { name: 'operateur-dashboard' }; // adjust if operator route exists
    return { name: 'dev-hub' };
  }

  const requiredRoles = to.meta.roles;
  if (requiredRoles && !authStore.hasAccess(requiredRoles)) {
    // PREVENT INFINITE REDIRECT: if we're already trying to go to the fallback, stop redirecting.
    const fallbackRoute = authStore.userRole === 'MAGASINIER' ? 'magasinier-scan-of' : (authStore.userRole === 'SUPERVISEUR_QUALITE' ? 'superviseur-dashboard' : 'dev-hub');
    if (to.name !== fallbackRoute) {
      return { name: fallbackRoute };
    }
    // If the fallback route ITSELF requires roles the user doesn't have, they are completely unauthorized.
    // Ideally redirect to an unauthorized page. Here we just let them go to the page but it might be blank.
  }

  return true;
});

export default router
