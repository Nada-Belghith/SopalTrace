using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IPlanAssRepository PlanAssRepository { get; }
    IUserRepository UserRepository { get; }

    IPlanEchanRepository PlanEchanRepository { get; }
    IControlePosteRepository ControlePosteRepository { get; }

    IPlanVerifMachineRepository PlanVerifMachineRepository { get; }
    IPlanRccfRepository PlanRccfRepository { get; }
    IDictionnaireQualiteRepository DictionnaireQualiteRepository { get; }
    IRefFormulaireRepository RefFormulaireRepository { get; }

    Task BeginTransactionAsync();
    Task<int> CommitAsync();
    Task RollbackAsync();

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
