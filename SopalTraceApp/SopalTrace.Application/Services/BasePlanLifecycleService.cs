using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Classe abstraite centralisée pour le cycle de vie complet (CRUD + Versionning + Brouillon) de tous les plans qualité.
/// Implémente le pattern Template Method avec Hooks pour que les enfants injectent leurs règles spécifiques.
/// 
/// Responsabilités:
/// - Consultation (GetAsync)
/// - Création de brouillon (CreateDraftAsync)
/// - Activation/Versionning (ActivatePlanAsync)
/// - Archivage (ArchivePlanAsync)
/// - Mise à jour brouillon (UpdateDraftAsync)
/// 
/// Hooks (virtual) que les enfants peuvent implémenter:
/// - ValidateCreationAsync: Validation métier spécifique à la création
/// - ValidateDraftUpdateAsync: Validation à la mise à jour du brouillon
/// - HandleVersioningBeforeActivationAsync: Logique spécifique avant activation/versionning
/// - OnPlanActivatedAsync: Callback après activation réussie
/// </summary>
/// <typeparam name="TEntete">Type d'entité du plan (doit implémenter IPlanEntete)</typeparam>
/// <typeparam name="TCreateDto">DTO pour la création</typeparam>
/// <typeparam name="TUpdateDto">DTO pour la mise à jour du brouillon</typeparam>
public abstract class BasePlanLifecycleService<TEntete, TCreateDto, TUpdateDto>
    where TEntete : IPlanEntete
{
    protected readonly IUnitOfWork _unitOfWork;

    protected BasePlanLifecycleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // ==================== HOOKS (Template Method Pattern) ====================

    /// <summary>
    /// Hook: Valide les règles métier spécifiques à la création.
    /// À implémenter par les enfants pour appliquer des validations métier.
    /// Par défaut: pas de validation supplémentaire.
    /// </summary>
    /// <param name="dto">DTO de création</param>
    /// <returns>Liste des erreurs de validation (vide si valide)</returns>
    protected virtual Task<List<string>> ValidateCreationAsync(TCreateDto dto)
    {
        return Task.FromResult(new List<string>());
    }

    /// <summary>
    /// Hook: Valide les règles métier spécifiques à la mise à jour du brouillon.
    /// À implémenter par les enfants.
    /// Par défaut: pas de validation supplémentaire.
    /// </summary>
    /// <param name="plan">Plan à mettre à jour</param>
    /// <param name="dto">DTO de mise à jour</param>
    /// <returns>Liste des erreurs de validation (vide si valide)</returns>
    protected virtual Task<List<string>> ValidateDraftUpdateAsync(TEntete plan, TUpdateDto dto)
    {
        return Task.FromResult(new List<string>());
    }

    /// <summary>
    /// Hook: Effectue la logique métier spécifique avant activation/versionning.
    /// Exemple: archiver les plans actifs existants, vérifier des contraintes métier, etc.
    /// À implémenter par les enfants.
    /// Par défaut: pas d'action.
    /// </summary>
    /// <param name="plan">Plan à activer</param>
    /// <param name="user">Utilisateur effectuant l'action</param>
    /// <returns></returns>
    protected virtual Task HandleVersioningBeforeActivationAsync(TEntete plan, string user)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Hook: Callback appelé après l'activation réussie du plan.
    /// Peut être utilisé pour des logs, des notifications, etc.
    /// Par défaut: pas d'action.
    /// </summary>
    /// <param name="activatedPlan">Plan activé</param>
    /// <param name="user">Utilisateur</param>
    /// <returns></returns>
    protected virtual Task OnPlanActivatedAsync(TEntete activatedPlan, string user)
    {
        return Task.CompletedTask;
    }

    // ==================== ABSTRACT METHODS (à implémenter par les enfants) ====================

    /// <summary>
    /// Récupère une entité du plan par son ID.
    /// À implémenter par les enfants selon leur repository.
    /// </summary>
    /// <param name="id">ID du plan</param>
    /// <returns>L'entité ou null</returns>
    protected abstract Task<TEntete?> ObtenirEntiteAsync(Guid id);

    /// <summary>
    /// Crée une nouvelle entité de plan à partir du DTO de création.
    /// À implémenter par les enfants selon la spécificité du type de plan.
    /// </summary>
    /// <param name="dto">DTO de création</param>
    /// <param name="user">Utilisateur créateur</param>
    /// <returns>Nouvelle entité créée</returns>
    protected abstract Task<TEntete> CreerEntiteAsync(TCreateDto dto, string user);

    /// <summary>
    /// Applique les mises à jour du DTO sur l'entité brouillon.
    /// À implémenter par les enfants selon la structure spécifique.
    /// </summary>
    /// <param name="plan">Entité à mettre à jour</param>
    /// <param name="dto">DTO de mise à jour</param>
    /// <param name="user">Utilisateur effectuant la mise à jour</param>
    /// <returns></returns>
    protected abstract Task ApplierMiseAJourDraftAsync(TEntete plan, TUpdateDto dto, string user);

    /// <summary>
    /// Persiste une nouvelle entité en base de données.
    /// À implémenter par les enfants.
    /// </summary>
    /// <param name="plan">Entité à persister</param>
    /// <returns></returns>
    protected abstract Task PersisterEntiteAsync(TEntete plan);

    /// <summary>
    /// Calcule la nouvelle version pour un plan.
    /// À implémenter par les enfants selon leur logique de versioning.
    /// </summary>
    /// <param name="plan">Plan pour lequel calculer la version</param>
    /// <returns>Numéro de la nouvelle version</returns>
    protected abstract Task<int> CalculerNouvelleVersionAsync(TEntete plan);

    /// <summary>
    /// Crée une nouvelle version de l'entité (copie) à partir de l'ancienne.
    /// À implémenter par les enfants selon la stratégie de copie.
    /// </summary>
    /// <param name="ancienPlan">Plan source</param>
    /// <param name="dto">DTO de mise à jour pour la nouvelle version</param>
    /// <param name="nouvelleVersion">Numéro de la nouvelle version</param>
    /// <param name="user">Utilisateur créateur</param>
    /// <returns>Nouvelle entité</returns>
    protected abstract Task<TEntete> CreerNouvelleVersionEntiteAsync(TEntete ancienPlan, TUpdateDto dto, int nouvelleVersion, string user);

    /// <summary>
    /// Récupère un brouillon existant pour éviter les doublons lors d'une tentative de création.
    /// À implémenter par les enfants pour définir ce qui constitue un "doublon" (ex: même article/opération).
    /// </summary>
    /// <param name="dto">DTO de création</param>
    /// <returns>Brouillon existant ou null</returns>
    protected abstract Task<TEntete?> ObtenirBrouillonExistantAsync(TCreateDto dto);

    // ==================== PUBLIC METHODS (Core Lifecycle) ====================

    /// <summary>
    /// Consulte un plan par son ID.
    /// </summary>
    /// <param name="id">ID du plan</param>
    /// <returns>Entité du plan ou null</returns>
    public async Task<TEntete?> ConsulterPlanAsync(Guid id)
    {
        return await ObtenirEntiteAsync(id);
    }

    /// <summary>
    /// Crée un nouveau brouillon de plan.
    /// Applique les validations métier via le hook ValidateCreationAsync.
    /// </summary>
    /// <param name="dto">DTO de création</param>
    /// <param name="user">Utilisateur créateur</param>
    /// <returns>ID du plan créé</returns>
    /// <exception cref="InvalidOperationException">Si la validation métier échoue</exception>
    public async Task<Guid> CreerBrouillonAsync(TCreateDto dto, string user)
    {
        // 1. Vérifier si un brouillon identique existe déjà (pour éviter 409 Conflict)
        var brouillonExistant = await ObtenirBrouillonExistantAsync(dto);
        if (brouillonExistant != null)
        {
            return brouillonExistant.Id;
        }

        // 2. Valider via le hook
        var erreurs = await ValidateCreationAsync(dto);
        if (erreurs.Count > 0)
        {
            throw new InvalidOperationException($"Validation échouée: {string.Join("; ", erreurs)}");
        }

        // 3. Créer l'entité via le hook
        var plan = await CreerEntiteAsync(dto, SecuriserNomAuteur(user));

        // 4. Initialiser le plan au statut BROUILLON
        plan.Statut = StatutsPlan.Brouillon;
        // La version sera 1 par défaut pour un nouveau plan (ou gérée par l'implémentation de CreerEntite si besoin)
        if (plan.Version <= 0) plan.Version = 1;

        // 5. Persister
        try 
        {
            await PersisterEntiteAsync(plan);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception) // Capture les conflits de concurrence au cas où deux requêtes arrivent en même temps
        {
            var draftSaufConcurrence = await ObtenirBrouillonExistantAsync(dto);
            if (draftSaufConcurrence != null) return draftSaufConcurrence.Id;
            throw;
        }

        return plan.Id;
    }

    /// <summary>
    /// Met à jour un plan en état BROUILLON.
    /// </summary>
    /// <param name="id">ID du plan</param>
    /// <param name="dto">DTO de mise à jour</param>
    /// <param name="user">Utilisateur effectuant la mise à jour</param>
    /// <exception cref="KeyNotFoundException">Si le plan n'existe pas</exception>
    /// <exception cref="InvalidOperationException">Si le plan n'est pas en BROUILLON ou si validation échoue</exception>
    public async Task UpdateDraftAsync(Guid id, TUpdateDto dto, string user)
    {
        var plan = await ObtenirEntiteAsync(id);
        if (plan == null)
            throw new KeyNotFoundException($"Plan avec l'ID '{id}' introuvable.");

        if (plan.Statut != StatutsPlan.Brouillon)
            throw new InvalidOperationException($"Impossible de modifier un plan qui n'est pas en BROUILLON (statut actuel: {plan.Statut}).");

        // Valider via le hook
        var erreurs = await ValidateDraftUpdateAsync(plan, dto);
        if (erreurs.Count > 0)
        {
            throw new InvalidOperationException($"Validation échouée: {string.Join("; ", erreurs)}");
        }

        // Appliquer les mises à jour
        await ApplierMiseAJourDraftAsync(plan, dto, SecuriserNomAuteur(user));

        // Persister
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Active un plan brouillon (le passe en ACTIF) avec gestion du versionning.
    /// 
    /// Processus:
    /// 1. Récupère le plan
    /// 2. Vérifie qu'il est en BROUILLON
    /// 3. Appelle le hook HandleVersioningBeforeActivationAsync (pour archiver anciens plans, etc.)
    /// 4. Calcule la nouvelle version
    /// 5. Met à jour le plan avec la nouvelle version et le statut ACTIF
    /// 6. Persiste
    /// 7. Appelle le hook OnPlanActivatedAsync
    /// </summary>
    /// <param name="id">ID du plan</param>
    /// <param name="user">Utilisateur effectuant l'activation</param>
    /// <param name="updateDto">DTO optionnel de mise à jour avant activation (ex: commentaire version)</param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException">Si le plan n'existe pas</exception>
    /// <exception cref="InvalidOperationException">Si le plan n'est pas en BROUILLON</exception>
    public async Task ActiverPlanAsync(Guid id, string user, TUpdateDto? updateDto = default)
    {
        var plan = await ObtenirEntiteAsync(id);
        if (plan == null)
            throw new KeyNotFoundException($"Plan avec l'ID '{id}' introuvable.");

        if (plan.Statut != StatutsPlan.Brouillon)
            throw new InvalidOperationException($"Seul un plan en BROUILLON peut être activé (statut actuel: {plan.Statut}).");

        var userSecure = SecuriserNomAuteur(user);

        // Hook: Logique de versionning spécifique avant activation
        await HandleVersioningBeforeActivationAsync(plan, userSecure);

        // Calculer la nouvelle version
        var nouvelleVersion = await CalculerNouvelleVersionAsync(plan);

        // Appliquer les mises à jour optionnelles (ex: commentaire version)
        if (updateDto != null)
        {
            await ApplierMiseAJourDraftAsync(plan, updateDto, userSecure);
        }

        // Mettre à jour la version et le statut
        plan.Version = nouvelleVersion;
        plan.Statut = StatutsPlan.Actif;
        plan.ModifiePar = userSecure;
        plan.ModifieLe = DateTime.UtcNow;

        // Persister
        await _unitOfWork.CommitAsync();

        // Hook: Callback après activation
        await OnPlanActivatedAsync(plan, userSecure);
    }

    /// <summary>
    /// Archive un plan existant.
    /// </summary>
    /// <param name="id">ID du plan</param>
    /// <param name="user">Utilisateur effectuant l'archivage</param>
    /// <exception cref="KeyNotFoundException">Si le plan n'existe pas</exception>
    /// <exception cref="InvalidOperationException">Si le plan est déjà archivé</exception>
    public async Task ArchiverPlanAsync(Guid id, string user)
    {
        var plan = await ObtenirEntiteAsync(id);
        if (plan == null)
            throw new KeyNotFoundException($"Plan avec l'ID '{id}' introuvable.");

        if (plan.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Le plan est déjà archivé.");

        plan.Statut = StatutsPlan.Archive;
        plan.ModifiePar = SecuriserNomAuteur(user);
        plan.ModifieLe = DateTime.UtcNow;

        await _unitOfWork.CommitAsync();
    }

    // ==================== PROTECTED HELPERS ====================

    /// <summary>
    /// Sécurise le nom de l'auteur (max 20 caractères, défaut "SYSTEM").
    /// </summary>
    protected string SecuriserNomAuteur(string auteur)
    {
        if (string.IsNullOrWhiteSpace(auteur))
            return "SYSTEM";
        return auteur.Length > 20 ? auteur[..20] : auteur;
    }
}
