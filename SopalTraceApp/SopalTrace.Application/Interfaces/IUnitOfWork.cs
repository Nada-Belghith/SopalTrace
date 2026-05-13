using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IPlanAssRepository PlanAssRepository { get; }
    IUserRepository UserRepository { get; }

    IPlanEchanRepository PlanEchanRepository { get; }
    IPlanNcRepository PlanNcRepository { get; }

    IPlanVerifMachineRepository PlanVerifMachineRepository { get; }
    IDictionnaireQualiteRepository DictionnaireQualiteRepository { get; }

    Task BeginTransactionAsync();
    Task<int> CommitAsync();
    Task RollbackAsync();

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
