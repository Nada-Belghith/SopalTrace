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
    IPlanVerifMachineEnteteRepository PlanVerifMachineEnteteRepository { get; }
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

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
