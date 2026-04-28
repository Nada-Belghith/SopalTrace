import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '@/layouts/MainLayout.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/dev/hub'
    },
    {
      path: '/dev',
      component: MainLayout,
      children: [
        // 1. LE HUB (Le choix du type de plan - Image 1)
        {
          path: 'hub',
          name: 'dev-hub',
          component: () => import('@/views/QualityPlans/DevModelHub.vue'),
        },
        {
          path: 'hub-plans',
          name: 'dev-hub-plans',
          component: () => import('@/views/QualityPlans/DevPlanHub.vue'),
        },

        // 2. Ã‰DITEUR FABRICATION (TRN, ESP, USI) - GABARITS
        // GÃ¨re les Corps et Volants pour les 3 premiÃ¨res opÃ©rations
        {
          path: 'fab/nouveau', 
          name: 'dev-fab-modele-create',
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
        },
        {
          path: 'fab/editer/:id',
          name: 'dev-fab-edit',
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
        },

        // === NOUVEAU : Ã‰DITEUR PLANS PAR ARTICLE (Production) ===
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

        // 3. Ã‰DITEUR ASSEMBLAGE (Plan MaÃ®tre & RÃ©sultats)
        // GÃ¨re RGAFM (Manu), RGAFA (Auto), Soupape
        // {
        //   path: 'assemblage/nouveau',
        //   name: 'dev-ass-create',
        //   component: () => import('@/views/QualityPlans/Assemblage/AssModeleEditor.vue'),
        // },

        // 4. Ã‰DITEUR PRODUIT FINI
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

        // 5. Ã‰DITEUR VÃ‰RIF MACHINE (BEE, MAS, SER...)
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

        // 6. RÃ‰SULTAT DE CONTRÃ”LE (NC)
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
    }
  ]
})

export default router
