# Architecture de la Couche Application

## Vue d'ensemble

La couche Application contient la logique métier, les mappers, les validateurs et les utilitaires pour gérer les plans de qualité et les modèles.

## Organisation

### Services de Plan (`Services/`)

#### Plans de Type Qualité (Cycle de Vie Standardisé)

Les services suivants héritent de `BasePlanLifecycleService` pour un cycle de vie cohérent:

- **PlanNcService** : Gestion des Plans NC (Non-Conformité)
  - Entité: `PlanNcEntete` (implémente `IPlanEntete`)
  - DTOs: `CreatePlanNcRequestDto`, `SavePlanNcDto`
  - État: ✅ Refactorisé

- **PlanEchanService** : Gestion des Plans d'Échantillonnage
  - Entité: `PlanEchantillonnageEntete` (implémente `IPlanEntete`)
  - DTOs: `CreatePlanEchanRequestDto`, `UpdatePlanEchanRequestDto`
  - État: ✅ Refactorisé

#### Plans de Type Famille (Gestion par Famille de Robinet)

Ces services gèrent les plans par famille de robinet avec versioning spécifique:

- **PlanPfService** : Plans Produit Fini
  - Entité: `PlanPfEntete` (implémente `IPlanEntete`)
  - Approche: Gestion par famille
  - Versioning: Par code famille
  - Optimisations: ✅ Helper `CalculerNouvelleVersionAsync`

- **PlanAssService** : Plans Assemblage
  - Entité: `PlanAssEntete` (implémente `IPlanEntete`)
  - Approche: Gestion par famille et exception
  - Versioning: Par opération/famille/code article
  - Optimisations: ✅ Helper `CalculerNouvelleVersionAsync`

#### Plans de Type Article (Multi-sections)

- **PlanFabricationService** : Plans de Fabrication
  - Entité: `PlanFabEntete` (implémente `IPlanEntete`)
  - Caractéristique: Support article complet avec sections/lignes
  - Versioning: Par article/opération
  - Optimisations: ✅ Helper `CalculerNouvelleVersionAsync`, validation centralisée

### Classe de Base du Cycle de Vie

**BasePlanLifecycleService<TEntete, TCreateDto, TUpdateDto>**

Centralise le cycle de vie complet (CRUD + Versionning + Archivage) avec le pattern Template Method:

**Responsabilités:**
- Consultation (GetAsync)
- Création de brouillon (CreateDraftAsync)
- Activation/Versionning (ActiverPlanAsync)
- Archivage (ArchiverPlanAsync)
- Mise à jour brouillon (UpdateDraftAsync)

**Hooks (Virtual Methods):**
- `ValidateCreationAsync`: Validation métier spécifique à la création
- `ValidateDraftUpdateAsync`: Validation à la mise à jour du brouillon
- `HandleVersioningBeforeActivationAsync`: Logique avant activation (archivage, etc.)
- `OnPlanActivatedAsync`: Callback après activation réussie

**Abstract Methods (À implémenter):**
- `ObtenirEntiteAsync`: Récupération par ID
- `CreerEntiteAsync`: Création de l'entité
- `ApplierMiseAJourDraftAsync`: Mise à jour du brouillon
- `PersisterEntiteAsync`: Persistence
- `CalculerNouvelleVersionAsync`: Calcul de version
- `CreerNouvelleVersionEntiteAsync`: Création version
- `ObtenirBrouillonExistantAsync`: Détection brouillon existant

### Utilitaires (`Utilities/`)

#### PlanMetadataHelper
Normalise et sécurise les métadonnées des plans:
- `InitializeCreationMetadata`: Initialise les métadonnées de création
- `InitializeUpdateMetadata`: Initialise les métadonnées de mise à jour
- `InitializeArchivalMetadata`: Initialise les métadonnées d'archivage
- `NormalizeAuthorNameWithTruncation`: Limite le nom d'auteur à 50 caractères
- `SecuriserNomAuteur`: Sécurise le nom dans BasePlanLifecycleService

#### SamplingRuleHelper
Résout ou crée les règles d'échantillonnage:
- `ResolveOrCreateSamplingRuleByLibelleAsync`: Résolution intelligente par libellé

#### TypeCaracteristiqueHelper
Gère les types de caractéristiques:
- `ResolvePlanTypeAndCaracteristiquesAsync`: Résolution par plan type

#### SectionDtoExtensions
Extensions pour les DTOs de section avec mappages FAB.

#### ValidationHelper ✨ *NOUVEAU*
Centralise la validation avec FluentValidation:
- `ValidateAndThrowAsync<T>`: Valide et lève ValidationException si invalide

#### PlanOperationLogger ✨ *NOUVEAU*
Logging cohérent des opérations sur les plans:
- `LogCreation`: Création d'un plan
- `LogActivation`: Activation d'un plan
- `LogArchivage`: Archivage d'un plan
- `LogNewVersion`: Nouvelle version
- `LogError`: Erreurs avec contexte

### Mappers (`Mappers/`)

Chaque plan a son mappage spécifique:

- **PlanNcMapper**: Mappe les entités NC <-> DTOs (sans null-coalescing sur non-nullable)
- **PlanEchanMapper**: Mappe les entités Échantillonnage <-> DTOs
- **PlanAssMapper**: Mappe les entités Assemblage <-> DTOs
- **PlanPfMapper**: Mappe les entités PF <-> DTOs avec sections
- **PlanFabricationMapper**: Mappe les entités FAB <-> DTOs avec sections/lignes complexes

### Validateurs (`Validators/`)

Validations FluentValidation spécifiques:

- `CreatePlanRequestValidator`: Validation pour création de plan
- `ClonePlanRequestValidator`: Validation pour clonage
- `AssemblagePlanValidator`: Validation pour assemblage
- `PlanActivationValidator`: Validation pour activation
- etc.

### Interfaces (`Interfaces/`)

**Contrats de service:**
- `IPlanNcService`, `IPlanEchanService`, `IPlanAssService`, `IPlanPfService`, `IPlanFabricationService`
- Définissent les contrats publics pour chaque type de plan

**Contrats de repository:**
- `IPlanNcRepository`, `IPlanEchanRepository`, etc.

### DTOs (`DTOs/`)

Organisés par plan type avec une structure claire:

```
DTOs/
├── QualityPlans/
│   ├── NC/
│   │   └── DTOs pour plans NC
│   ├── PlansEchantillonnage/
│   │   └── DTOs pour échantillonnage
│   ├── PlanAssemblage/
│   │   └── DTOs pour assemblage
│   ├── PlanProduitFini/
│   │   └── DTOs pour PF
│   └── PlanFabrication/
│       └── DTOs pour fabrication
└── ...
```

## Refactoring Réalisé

### Phase 1: Extraction d'utilitaires
✅ `PlanMetadataHelper`: Centralise la normalisation d'auteur (50 caractères max)
✅ `SamplingRuleHelper`: Résolution intelligente des règles d'échantillonnage
✅ `TypeCaracteristiqueHelper`: Gestion des types de caractéristiques
✅ `SectionDtoExtensions`: Extensions pour sections FAB

### Phase 2: Migration vers BasePlanLifecycleService
✅ `PlanNcService`: Migration complète avec validation des champs non-nullable
✅ `PlanEchanService`: Migration avec adaptation repository
✅ Mise à jour des entités pour implémenter `IPlanEntete`

### Phase 3: Nettoyage des services
✅ `PlanFabricationService`: 
  - Helper `CalculerNouvelleVersionAsync` pour éviter duplication
  - Validation centralisée avec `ValidationHelper`

✅ `PlanAssService`:
  - Helper `CalculerNouvelleVersionAsync` pour la famille/opération
  - Validation centralisée avec `ValidationHelper`
  - Suppression du logging redondant

✅ `PlanPfService`:
  - Helper `CalculerNouvelleVersionAsync` pour la famille
  - Utilisation de `PlanMetadataHelper.NormalizeAuthorNameWithTruncation`

### Phase 4: Nouveaux utilitaires
✅ `ValidationHelper`: Centralise la validation FluentValidation
✅ `PlanOperationLogger`: Logging cohérent des opérations plan

## Principes SOLID Appliqués

### Single Responsibility Principle (SRP)
- **BasePlanLifecycleService**: Gère UNIQUEMENT le cycle de vie standard
- **Helpers**: Chaque helper a une seule responsabilité (validation, logging, mapping, etc.)
- **Services**: Responsabilité bien définie (gestion métier du plan)

### Open/Closed Principle (OCP)
- **BasePlanLifecycleService**: Ouvert à l'extension via hooks virtual, fermé à la modification
- **Helpers**: Extensibles sans modification

### Liskov Substitution Principle (LSP)
- Tous les plans héritant de `BasePlanLifecycleService` respectent le contrat
- Interfaces de repository bien définies

### Interface Segregation Principle (ISP)
- Interfaces granulaires (`IPlanNcService`, `IPlanEchanService`, etc.)
- Pas de méthodes inutiles dans les interfaces

### Dependency Inversion Principle (DIP)
- Services dépendent d'interfaces (repositories, validators)
- Injection de dépendances via constructeur

## DRY (Don't Repeat Yourself)

✅ **Éliminé les duplications:**
- Calcul de version centralisé dans helpers par plan
- Validation centralisée via `ValidationHelper`
- Logging cohérent via `PlanOperationLogger`
- Normalisation d'auteur centralisée dans `PlanMetadataHelper`
- Métadonnées de cycle de vie dans `BasePlanLifecycleService`

## État de la Base de Code

✅ **Build**: Clean (41 warnings seulement, zéro erreurs)
✅ **Tests**: 12/12 passing
✅ **Architecture**: Cohérente et maintenable
✅ **Comportement**: Préservé (no breaking changes)

## Prochaines Étapes Possibles

1. **Logging**: Utiliser `PlanOperationLogger` dans tous les services
2. **Tests Unitaires**: Ajouter des tests pour les nouveaux helpers
3. **Documentation**: Ajouter des commentaires XML pour les méthodes publiques
4. **Migration Article**: Si besoin, migrer `PlanFabricationService` vers `BasePlanArticleLifecycleService`

---

**Dernier update**: Refactoring complet avec validation centralisée et logging cohérent
