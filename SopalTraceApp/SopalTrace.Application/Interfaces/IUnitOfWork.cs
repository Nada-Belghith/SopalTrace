using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

    using SopalTrace.Application.Interfaces.Repositories;

    public interface IUnitOfWork : IAsyncDisposable
    {
        IDocumentEnteteRepository DocumentEnteteRepository { get; }
        IUserRepository UserRepository { get; }
    
    IDictionnaireQualiteRepository DictionnaireQualiteRepository { get; }
    IRefFormulaireRepository RefFormulaireRepository { get; }
    IDocumentVerifMachineEnteteRepository DocumentVerifMachineEnteteRepository { get; }
    IDocumentEchantillonnageEnteteRepository DocumentEchantillonnageEnteteRepository { get; }
    IModeleFabricationEnteteRepository ModeleFabricationEnteteRepository { get; }
    IPlanFabricationEnteteRepository PlanFabricationEnteteRepository { get; }
    IAlerteRepository AlerteRepository { get; }
    IExecControleOfRepository ExecControleOfRepository { get; }
    IExecEchantillonnageRepository ExecEchantillonnageRepository { get; }

    Task BeginTransactionAsync();
    Task<int> CommitAsync();
    /// <summary>Sauvegarde immédiate sans fermer la transaction — utile pour flush les suppressions avant réinsertion.</summary>
    Task<int> FlushDeletesAsync();
    Task RollbackAsync();
    /// <summary>
    /// Detaches all tracked entities from the EF Core change tracker.
    /// Use this before calling infrastructure services that perform an intermediate
    /// SaveChangesAsync() to avoid contaminating the outer unit-of-work state.
    /// </summary>
    void DetachAllEntities();

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);

    /// <summary>
    /// Inscrit une entité créée pendant une mise à jour (nouvelle section, ligne ou valeur de colonne) comme ajoutée.
    /// Sans cela, EF prend une entité rattachée par une collection et dont l'identifiant est déjà renseigné
    /// pour une entité existante, et tente une mise à jour qui échoue (faux conflit de concurrence).
    /// </summary>
    void Ajouter(object entite);

    /// <summary>
    /// Refuse l'enregistrement d'un document modifié par quelqu'un d'autre depuis son ouverture.
    /// <paramref name="versionClient"/> est la version (RowVersion en base64) reçue par l'éditeur au chargement :
    /// différente de la version actuelle → conflit (409) ; absente → refus si <paramref name="obligatoire"/>.
    /// L'entête est marqué modifié pour que l'écriture contrôle aussi la version en base et la fasse évoluer,
    /// même quand seules des lignes ont changé.
    /// </summary>
    void VerifierVersionDocument(DocumentEntete document, string? versionClient, bool obligatoire);
}
