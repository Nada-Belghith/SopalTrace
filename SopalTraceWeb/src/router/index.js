import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '@/layouts/MainLayout.vue'
import { useAuthStore } from '@/stores/authStore'

// Page d'accueil de chaque rôle (après connexion, ou quand une page n'est pas accessible).
const ACCUEIL_PAR_ROLE = {
  ADMIN: 'admin-dashboard',
  MAGASINIER: 'magasinier-scan-of',
  SUPERVISEUR_QUALITE: 'superviseur-dashboard',
  OPERATEUR: 'operateur-of-semi-fini',
  RESPONSABLE_QUALITE: 'qualite-tracabilite-totale',
  SERVICE_ACHATS: 'documents-externes-depot',
  SERVICE_COMMERCIAL: 'documents-externes-depot',
  SERVICE_QUALITE: 'documents-externes-depot',
  SERVICE_DI: 'documents-externes-depot'
}
const accueil = (role) => ACCUEIL_PAR_ROLE[role] ?? 'dev-hub'
const ROLES_DEPOT_DOCUMENTS = ['SERVICE_ACHATS', 'SERVICE_COMMERCIAL', 'SERVICE_QUALITE', 'SERVICE_DI']

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
      path: '/auth/setup-password',
      name: 'setup-password',
      component: () => import('@/views/Auth/SetupPasswordView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/',
      redirect: () => ({ name: accueil(useAuthStore().userRole) })
    },
    {
      path: '/dev',
      component: MainLayout,
      meta: { requiresAuth: true, roles: ['ADMIN', 'RESPONSABLE_DI', 'RESPONSABLE_QUALITE', 'SUPERVISEUR_QUALITE'] }, // Empêche l'accès à l'OPERATEUR par défaut
      children: [
        // 1. ADMIN - DASHBOARD ET HABILITATIONS
        {
          path: 'admin-dashboard',
          name: 'admin-dashboard',
          component: () => import('@/views/Admin/AdminDashboard.vue'),
          meta: { roles: ['ADMIN'] }
        },
        {
          path: 'habilitations',
          name: 'admin-habilitations',
          component: () => import('@/views/Admin/GestionHabilitations.vue'),
          meta: { roles: ['ADMIN'] }
        },
        // 2. LE HUB (Le choix du type de plan - Image 1)
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
          meta: { roles: ['ADMIN', 'RESPONSABLE_DI'] }
        },
        {
          path: 'hub-structures',
          name: 'dev-hub-structures',
          component: () => import('@/views/QualityPlans/DevStructHub.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
        },

        // --- ASS : MODELISATION DES PLANS D'ASSEMBLAGE ---
        {
          path: 'ass/nouveau',
          name: 'dev-ass-create',
          component: () => import('@/views/QualityPlans/Assemblage/AssPlanEditor.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'ass/editer/:id',
          name: 'dev-ass-edit',
          component: () => import('@/views/QualityPlans/Assemblage/AssPlanEditor.vue'),
          meta: { roles: ['ADMIN', 'RESPONSABLE_QUALITE', 'SUPERVISEUR_QUALITE'] }
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
          meta: { roles: ['ADMIN', 'RESPONSABLE_DI'] }
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
        //   component: () => import('@/views/QualityPlans/Assemblage/AssPlanEditor.vue'),
        // },

        // 4. ÉDITEUR PRODUIT FINI
        {
          path: 'produit-fini/nouveau',
          name: 'dev-pf-create',
          component: () => import('@/views/QualityPlans/DocumentProduitFini/DocumentProduitFiniEditor.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'produit-fini/editer/:id',
          name: 'dev-pf-edit',
          component: () => import('@/views/QualityPlans/DocumentProduitFini/DocumentProduitFiniEditor.vue'),
        },

        // 5. ÉDITEUR VÉRIF MACHINE (BEE, MAS, SER...)
        {
          path: 'verif-machine/nouveau',
          name: 'dev-vm-create',
          component: () => import('@/views/QualityPlans/VerifMachine/VerificationMachineEditor.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'verif-machine/editer/:id',
          name: 'dev-vm-edit',
          component: () => import('@/views/QualityPlans/VerifMachine/VerificationMachineEditor.vue'),
        },

        // 6. RÉSULTAT DE CONTRÔLE (NC)
        {
          path: 'resultat-controle/nouveau',
          name: 'dev-rc-create',
          component: () => import('@/views/QualityPlans/DocumentControlePoste/DocumentControlePosteEditor.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'resultat-controle/editer/:id',
          name: 'dev-rc-edit',
          component: () => import('@/views/QualityPlans/DocumentControlePoste/DocumentControlePosteEditor.vue'),
        },

        // NOUVEAU: RÉSULTAT CONTRÔLE C.F.
        {
          path: 'resultat-controle-cf/nouveau',
          name: 'dev-rccf-create',
          component: () => import('@/views/QualityPlans/ResultatControleCF/RccfModeleEditor.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
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
          component: () => import('@/views/QualityPlans/Echantillonnage/DocumentEchantillonnageEditor.vue'),
          meta: { roles: ['ADMIN', 'SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'echantillonnage/editer/:id',
          name: 'dev-ech-edit',
          component: () => import('@/views/QualityPlans/Echantillonnage/DocumentEchantillonnageEditor.vue'),
        }
      ]
    },
    {
      path: '/operateur',
      name: 'operateur-root',
      component: MainLayout,
      meta: { requiresAuth: true, roles: ['ADMIN', 'OPERATEUR'] },
      children: [
        {
          path: 'of-semi-fini',
          name: 'operateur-of-semi-fini',
          component: () => import('@/views/Operateur/OperateurDashboardView.vue')
        },
        {
          path: 'of-fini',
          name: 'operateur-of-fini',
          component: () => import('@/views/Operateur/OperateurAssemblageView.vue')
        },
        {
          path: 'echantillonnage/:execControleOfId',
          name: 'exec-echantillonnage',
          component: () => import('@/views/Operateur/ExecEchantillonnageView.vue')
        },
        {
          path: 'resultat-controle/:execControleOfId',
          name: 'exec-resultat-controle',
          component: () => import('@/views/Operateur/ExecRcPosteView.vue')
        },
        {
          // Vérifications machine d'un poste à un moment : ?posteCode=...&periodicite=DEMARRAGE|APRES_PAUSE|FIN_POSTE
          path: 'verif-machines/:execControleOfId',
          name: 'exec-verif-machines',
          component: () => import('@/views/Operateur/VerifMachinesPosteView.vue')
        },
        {
          path: 'plan-assemblage/:execControleOfId',
          name: 'exec-plan-assemblage',
          component: () => import('@/views/Operateur/ExecPlanAssemblageView.vue')
        },
        {
          // Résultat contrôle en cours de fabrication (ENCF) d'un poste : ?of=...&poste=...
          path: 'encf/:execControleOfId',
          name: 'operateur-encf',
          component: () => import('@/components/Execution/ExecEncfForm.vue')
        },
        {
          path: 'tracabilite/:execControleOfId',
          name: 'exec-tracabilite',
          component: () => import('@/views/Operateur/OperateurTracabiliteView.vue')
        },
        {
          path: 'verif-machine/:id',
          name: 'operateur-vm-view',
          component: () => import('@/views/QualityPlans/VerifMachine/VerificationMachineEditor.vue')
        },
        {
          path: 'verif-machine-exec/:id',
          name: 'operateur-vm-exec',
          component: () => import('@/components/Operateur/OperateurVerifMachineExec.vue')
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
        },
        {
          path: 'scan-bl',
          name: 'magasinier-scan-bl',
          component: () => import('@/views/Magasinier/BlScannerView.vue')
        }
      ]
    },
    {
      path: '/superviseur',
      name: 'superviseur-root',
      component: MainLayout,
      // Tableau de bord des documents : superviseur qualité, responsable DI (rubrique « Analyses ») et administrateur.
      meta: { requiresAuth: true, roles: ['ADMIN', 'SUPERVISEUR_QUALITE', 'RESPONSABLE_DI'] },
      children: [
        {
          path: 'dashboard',
          name: 'superviseur-dashboard',
          component: () => import('@/views/Superviseur/SuperviseurDashboardView.vue')
        }
      ]
    },
    {
      path: '/qualite',
      name: 'qualite-root',
      component: MainLayout,
      meta: { requiresAuth: true, roles: ['ADMIN', 'RESPONSABLE_QUALITE', 'SUPERVISEUR_QUALITE'] },
      children: [
        {
          // Dossier documentaire d'un OF (documents internes et externes) : responsable qualité seulement.
          path: 'tracabilite',
          name: 'qualite-tracabilite-totale',
          component: () => import('@/views/Qualite/TracabiliteTotaleView.vue'),
          meta: { roles: ['RESPONSABLE_QUALITE'] }
        },
        {
          path: 'formulaires',
          name: 'qualite-formulaires',
          component: () => import('@/views/Qualite/FormulairesView.vue'),
          meta: { roles: ['SUPERVISEUR_QUALITE'] }
        },
        {
          path: 'modeles-documents',
          name: 'qualite-modeles-documents',
          component: () => import('@/views/Qualite/ModelesDocumentsView.vue'),
          // Le superviseur qualité définit les formulaires, l'administrateur leur rattache les modèles Excel.
          meta: { roles: ['ADMIN'] }
        }
      ]
    },
    {
      // Dépôt des documents externes par les services (achats, commercial, qualité, développement industriel).
      path: '/documents-externes',
      name: 'documents-externes-root',
      component: MainLayout,
      meta: { requiresAuth: true, roles: ROLES_DEPOT_DOCUMENTS },
      children: [
        {
          path: 'depot',
          name: 'documents-externes-depot',
          component: () => import('@/views/DocumentsExternes/DepotDocumentsExternesView.vue')
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
    // Page demandée gardée (lien d'un e-mail de relance, par exemple) : la connexion y ramène.
    return to.fullPath === '/' ? { name: 'login' } : { name: 'login', query: { retour: to.fullPath } };
  }

  if (to.name === 'login' && authStore.isAuthenticated) {
    return { name: accueil(authStore.userRole) };
  }

  const requiredRoles = to.meta.roles;
  if (requiredRoles && !authStore.hasAccess(requiredRoles)) {
    // Pas de redirection en boucle : on s'arrête si l'on va déjà vers la page d'accueil du rôle.
    const fallbackRoute = accueil(authStore.userRole);
    if (to.name !== fallbackRoute) {
      return { name: fallbackRoute };
    }
  }

  return true;
});

router.onError((error, to) => {
  if (
    error?.message?.includes('Failed to fetch dynamically imported module') ||
    error?.message?.includes('Importing a module script failed')
  ) {
    if (to?.fullPath) {
      window.location.assign(to.fullPath);
    } else {
      window.location.reload();
    }
  }
});

export default router
