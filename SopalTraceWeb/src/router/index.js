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
          // RECTIFICATION : On ajoute /QualityPlans/ dans le chemin
          component: () => import('@/views/QualityPlans/DevPlanHub.vue'),
        },

        // 2. ÉDITEUR FABRICATION (TRN, ESP, USI)
        // Gère les Corps et Volants pour les 3 premières opérations
        {
          path: 'fab/nouveau', 
          name: 'dev-fab-modele-create',
          // Ce chemin semble correct d'après votre image
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
        },
        {
          path: 'fab/editer/:id',
          name: 'dev-fab-edit',
          component: () => import('@/views/QualityPlans/Fabrication/FabModeleEditor.vue'),
        },

        // 3. ÉDITEUR ASSEMBLAGE (Plan Maître & Résultats)
        // Gère RGAFM (Manu), RGAFA (Auto), Soupape
        // {
        //   path: 'assemblage/nouveau',
        //   name: 'dev-ass-create',
        //   component: () => import('@/views/QualityPlans/Assemblage/AssModeleEditor.vue'),
        // },

        // // 4. ÉDITEUR PRODUIT FINI
        // {
        //   path: 'produit-fini/nouveau',
        //   name: 'dev-pf-create',
        //   component: () => import('@/views/QualityPlans/ProduitFini/PfModeleEditor.vue'),
        // },

        // // 5. ÉDITEUR VÉRIF MACHINE (BEE, MAS, SER...)
        // {
        //   path: 'verif-machine/nouveau',
        //   name: 'dev-vm-create',
        //   component: () => import('@/views/QualityPlans/VerifMachine/VmModeleEditor.vue'),
        // },

        // // 6. ÉDITEUR ÉCHANTILLONNAGE (Niveaux ISO)
        // {
        //   path: 'echantillonnage/nouveau',
        //   name: 'dev-ech-create',
        //   component: () => import('@/views/QualityPlans/Echantillonnage/EchModeleEditor.vue'),
        // }
      ]
    }
  ]
})

export default router