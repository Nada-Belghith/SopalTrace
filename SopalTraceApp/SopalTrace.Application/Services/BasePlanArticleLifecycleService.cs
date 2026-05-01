using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Classe abstraite pour les services de gestion des Plans par Article (spécifiques).
/// Étend BasePlanLifecycleService avec la sauvegarde automatique et le support du statut BROUILLON.
/// 
/// Adapté aux plans instanciés pour un article Sage où l'utilisateur peut:
/// - Sauvegarder automatiquement en arrière-plan (AutoSave)
/// - Laisser des brouillons incomplets
/// - Activer quand le brouillon est complet
/// 
/// Ajoute le hook:
/// - BeforeAutoSaveAsync: Actions spécifiques avant une sauvegarde automatique
/// </summary>
/// <typeparam name="TEntete">Type d'entité du plan (doit implémenter IPlanParArticle)</typeparam>
/// <typeparam name="TCreateDto">DTO pour la création</typeparam>
/// <typeparam name="TUpdateDto">DTO pour la mise à jour du brouillon</typeparam>
public abstract class BasePlanArticleLifecycleService<TEntete, TCreateDto, TUpdateDto>
    : BasePlanLifecycleService<TEntete, TCreateDto, TUpdateDto>
    where TEntete : IPlanParArticle
{
    protected BasePlanArticleLifecycleService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    // ==================== HOOKS (Article-specific) ====================

    /// <summary>
    /// Hook: Actions spécifiques à effectuer avant une sauvegarde automatique.
    /// Permet aux enfants d'ajouter de la logique métier ou des validations légères.
    /// Par défaut: pas d'action.
    /// </summary>
    /// <param name="plan">Plan à sauvegarder automatiquement</param>
    /// <param name="user">Utilisateur</param>
    /// <returns></returns>
    protected virtual Task BeforeAutoSaveAsync(TEntete plan, string user)
    {
        return Task.CompletedTask;
    }

    // ==================== PUBLIC METHODS (AutoSave) ====================

    /// <summary>
    /// Effectue une sauvegarde automatique du plan.
    /// 
    /// Processus:
    /// 1. Récupère le plan
    /// 2. Vérifie qu'il n'est pas archivé
    /// 3. Appelle le hook BeforeAutoSaveAsync
    /// 4. Met à jour la date de dernière sauvegarde automatique
    /// 5. Met à jour les métadonnées (ModifiePar, ModifieLe)
    /// 6. Persiste
    /// 
    /// Note: La sauvegarde automatique laisse le statut du plan INCHANGÉ.
    /// C'est au contrôle de la UI de décider du statut (BROUILLON ou ACTIF).
    /// </summary>
    /// <param name="id">ID du plan</param>
    /// <param name="user">Utilisateur effectuant l'AutoSave</param>
    /// <exception cref="KeyNotFoundException">Si le plan n'existe pas</exception>
    /// <exception cref="InvalidOperationException">Si le plan est archivé</exception>
    public async Task SauvegardeAutoAsync(Guid id, string user)
    {
        var plan = await ObtenirEntiteAsync(id);
        if (plan == null)
            throw new KeyNotFoundException($"Plan avec l'ID '{id}' introuvable.");

        if (plan.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Impossible de sauvegarder automatiquement un plan archivé.");

        var userSecure = SecuriserNomAuteur(user);

        // Hook: Logique spécifique avant AutoSave
        await BeforeAutoSaveAsync(plan, userSecure);

        // Mettre à jour les métadonnées de sauvegarde automatique

        plan.ModifiePar = userSecure;
        plan.ModifieLe = DateTime.UtcNow;

        // Persister
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Restaure un plan archivé en le réactivant en tant que nouvelle version.
    /// 
    /// Logique:
    /// 1. Récupère le plan archivé
    /// 2. Crée une nouvelle version (copie) du plan
    /// 3. Assigne une nouvelle numéro de version
    /// 4. Met à jour le statut à ACTIF
    /// 5. Persiste la nouvelle version
    /// 
    /// Note: L'ancienne version reste archivée pour traçabilité.
    /// Cette méthode est spécifique aux plans par article qui peuvent être restaurés.
    /// </summary>
    /// <param name="id">ID du plan archivé</param>
    /// <param name="updateDto">DTO de mise à jour (ex: motif de restauration)</param>
    /// <param name="user">Utilisateur effectuant la restauration</param>
    /// <returns>ID du nouveau plan restauré</returns>
    /// <exception cref="KeyNotFoundException">Si le plan n'existe pas</exception>
    /// <exception cref="InvalidOperationException">Si le plan n'est pas archivé</exception>
    public async Task<Guid> RestaurerPlanArchiveAsync(Guid id, TUpdateDto updateDto, string user)
    {
        var planArchive = await ObtenirEntiteAsync(id);
        if (planArchive == null)
            throw new KeyNotFoundException($"Plan avec l'ID '{id}' introuvable.");

        if (planArchive.Statut != StatutsPlan.Archive)
            throw new InvalidOperationException($"Seul un plan archivé peut être restauré (statut actuel: {planArchive.Statut}).");

        var userSecure = SecuriserNomAuteur(user);

        // Calculer la nouvelle version
        var nouvelleVersion = await CalculerNouvelleVersionAsync(planArchive);

        // Créer une nouvelle entité à partir de l'ancienne (via hook)
        var nouveauPlan = await CreerNouvelleVersionEntiteAsync(planArchive, updateDto, nouvelleVersion, userSecure);

        // Définir le statut à ACTIF
        nouveauPlan.Statut = StatutsPlan.Actif;
        nouveauPlan.CreePar = userSecure;
        nouveauPlan.CreeLe = DateTime.UtcNow;

        // Persister le nouveau plan
        await PersisterEntiteAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }
}
