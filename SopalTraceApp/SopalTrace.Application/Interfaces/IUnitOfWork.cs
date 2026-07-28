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
}
