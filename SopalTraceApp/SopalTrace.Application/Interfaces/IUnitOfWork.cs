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
    IPlanEchantillonnageEnteteRepository PlanEchantillonnageEnteteRepository { get; }

    Task BeginTransactionAsync();
    Task<int> CommitAsync();
    Task RollbackAsync();

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
