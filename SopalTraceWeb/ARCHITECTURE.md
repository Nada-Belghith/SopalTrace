# Architecture QualityPlans Frontend

## Vue d'ensemble

Cette architecture est conçue pour être **clean, extensible et maintenable** tout en préservant la logique métier existante. Elle permet de réutiliser la même structure pour tous les types de plans (Fabrication, Échantillonnage, Produit Fini, Vérification Machine, etc.).

---

## Structure des dossiers

```
src/
├── services/
│   ├── apiClient.js              # Client HTTP configuré (intercepteurs)
│   ├── qualityPlansService.js    # Centralisé - tous les appels API
│   └── fabPlanService.js         # Wrapper de compatibilité (déprécié)
│
├── composables/
│   ├── useVersioning.js          # Logique versioning (créer version, restaurer)
│   ├── useDirtyChecking.js       # Détection des modifications
│   ├── useModelManagement.js     # Gestion des modèles (load, create, edit)
│   ├── usePlanManagement.js      # Gestion des plans (instantiate, clone, save)
│   ├── useApi.js                 # (Existant) Utilitaires API
│   └── useNotification.js        # (Existant) Notifications toast
│
├── utils/
│   ├── modelMapper.js            # Transformations modèle (backend ↔ frontend)
│   ├── planMapper.js             # Transformations plan (backend ↔ frontend)
│   └── logger.js                 # (Existant)
│
├── components/
│   ├── Shared/
│   │   ├── ConfirmationDialog.vue    # Modal générique de confirmation
│   │   ├── VersioningDialog.vue      # Modal versioning/restauration
│   │   ├── EditorHeader.vue          # En-tête d'éditeur
│   │   ├── StatutBadge.vue           # Badge statut/version
│   │   └── EditorActions.vue         # Boutons d'action (annuler, soumettre)
│   │
│   └── Fabrication/
│       ├── FabModeleHeader.vue       # Header spécifique fab
│       ├── FabSectionCard.vue        # Carte section fabrication
│       └── FabLigneControl.vue       # Contrôle ligne fabrication
│
└── views/
    └── QualityPlans/
        ├── DevPlanHub.vue
        └── Fabrication/
            ├── FabModeleEditor.vue   # Refactorisé avec composables
            └── FabPlanEditor.vue     # Refactorisé avec composables
```

---

## Flux de données et responsabilités

### 1. **Services (API)**
- **`qualityPlansService.js`** : 
  - Centralisé pour modèles et plans
  - Méthodes : `getDictionnaires()`, `createModele()`, `getModeleById()`, `newModeleVersion()`, `restoreModele()`
  - Méthodes : `instantiatePlan()`, `clonePlan()`, `getPlanById()`, `newPlanVersion()`, `updatePlanValues()`
  - **Aucune logique métier** - juste appels HTTP

### 2. **Composables (Business Logic)**

#### `useModelManagement()`
Gère le cycle de vie complet d'un modèle:
```javascript
const { 
  modeleData, statut, version, isEditMode,
  loadModele, createModele, createNewVersion, restoreModele 
} = useModelManagement();
```

#### `usePlanManagement()`
Gère le cycle de vie complet d'un plan:
```javascript
const { 
  planData, statut, version, isEditMode,
  loadPlan, instantiateFromModele, cloneExistingPlan, 
  savePlanValues, createNewVersion 
} = usePlanManagement();
```

#### `useVersioning()`
Centralise logique versioning/restauration:
```javascript
const { 
  creerNouvelleVersionModele, restaurerModele,
  creerNouvelleVersionPlan, mettreAJourValeurs, restaurerPlan
} = useVersioning(); // ou spécifiquement:
const { creerNouvelleVersionModele, restaurerModele } = useModeleVersioning();
const { creerNouvelleVersionPlan, mettreAJourValeurs } = usePlanVersioning();
```

#### `useDirtyChecking()`
Détecte modifications sans détruire logique existante:
```javascript
const { isDirty, takeSnapshot, initializeSnapshot, confirmChanges } 
  = useDirtyChecking();
```

### 3. **Utilitaires (Data Transformation)**

#### `modelMapper.js`
- `mapBackendModeleToEditor()` : Backend → Frontend UI
- `prepareModelePayload()` : Frontend → Backend
- `createModeleSnapshot()` : État pour dirty checking
- `hasModeleChanged()` : Comparaison snapshots

#### `planMapper.js`
- `mapBackendPlanToEditor()` : Transformation plan
- `preparePlanValuesPayload()` : Formatage avant saving
- `createEmptySection()` / `createEmptyLine()` : Factory patterns
- `isMesure()`, `cleanupLineValuesOnTypeChange()` : Helpers métier

---

## Patterns et bonnes pratiques

### Pattern 1: Composition de composables
```javascript
// Dans une vue (FabModeleEditor.vue)
import { useModelManagement } from '@/composables/useModelManagement';
import { useDirtyChecking } from '@/composables/useDirtyChecking';
import { useModeleVersioning } from '@/composables/useVersioning';

export default {
  setup() {
    const { modeleData, statut, loadModele, createNewVersion } = useModelManagement();
    const { isDirty, takeSnapshot, initializeSnapshot } = useDirtyChecking();
    const { creerNouvelleVersionModele } = useModeleVersioning();
    
    // Logic ici utilise les 3 composables
  }
}
```

### Pattern 2: Transformation unidirectionnelle
```javascript
// Backend → Frontend (toujours passer par mapper)
const response = await qualityPlansService.getModeleById(id);
const uiData = mapBackendModeleToEditor(response.data);

// Frontend → Backend (toujours via preparer*)
const payload = prepareModelePayload(entete, sections);
await qualityPlansService.createModele(payload);
```

### Pattern 3: Composants entièrement réutilisables
```vue
<!-- Pareil pour tous les types de plans -->
<VersioningDialog
  :visible="showVersioningDialog"
  mode="new-version"
  @confirm="onVersionConfirm"
  @cancel="onVersionCancel"
/>
```

---

## Étendre pour un nouveau type de plan

### Exemple: `EchantillonnageEditor.vue`

```javascript
import { usePlanManagement } from '@/composables/usePlanManagement';
import { useDirtyChecking } from '@/composables/useDirtyChecking';
import { qualityPlansService } from '@/services/qualityPlansService';
import { mapBackendPlanToEditor, preparePlanValuesPayload } from '@/utils/planMapper';

export default {
  setup() {
    const { planData, statut, loadPlan, savePlanValues } = usePlanManagement();
    const { isDirty, takeSnapshot, initializeSnapshot } = useDirtyChecking();
    
    const sections = ref([]);
    
    onMounted(async () => {
      if (route.params.id) {
        await loadPlan(route.params.id);
        sections.value = planData.value?.sections || [];
        initializeSnapshot(sections.value);
      }
    });
    
    const savePlan = async () => {
      if (!isDirty.value) return; // Dirty checking
      
      // Utilise exactement la même logique que fab
      const payload = preparePlanValuesPayload(sections.value);
      await savePlanValues(planId.value, sections.value);
    };
    
    return { planData, statut, savePlan, isDirty };
  }
}
```

**C'est tout.** Pas besoin de dupliquer, pas besoin de redéfinir la logique. Réutilisable à 100%.

---

## Migration des vues existantes (Workflow)

### 1. **FabModeleEditor.vue**
```javascript
// Avant (ancien code)
const apiClient = ... // direct API calls partout

// Après (nouveau code)
const { modeleData, loadModele, createNewVersion } = useModelManagement();
const { isDirty, initializeSnapshot } = useDirtyChecking();
// Logique métier identique, juste better organized
```

### 2. **FabPlanEditor.vue**
```javascript
// Même refactoring
const { planData, instantiateFromModele, savePlanValues } = usePlanManagement();
const { isDirty, takeSnapshot } = useDirtyChecking();
```

---

## Testing

Chaque couche peut être testée indépendamment:

```javascript
// Test mapper
import { mapBackendModeleToEditor } from '@/utils/modelMapper';
test('transforms backend model correctly', () => {
  const backend = { id: '123', code: 'MOD-TEST' };
  const result = mapBackendModeleToEditor(backend);
  expect(result.code).toBe('MOD-TEST');
});

// Test composable
import { useModelManagement } from '@/composables/useModelManagement';
test('loads model', async () => {
  const { loadModele, modeleData } = useModelManagement();
  await loadModele('123');
  expect(modeleData.value).toBeDefined();
});
```

---

## Résumé : Non-destructif et clean

✅ **Logique métier conservée** - Aucune destruction, juste mieux organisée
✅ **Réutilisable** - Même code pour tous les types de plans
✅ **Testable** - Chaque couche isolée
✅ **Maintenable** - Séparation claire des responsabilités
✅ **Extensible** - Ajouter un nouveau type de plan = copier la vue + utiliser les composables
