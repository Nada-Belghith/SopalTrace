using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface IExecEncfRepository
    {
        Task<ExecControleOf?> GetExecEncfAsync(Guid id);
        Task<ExecControleOf?> GetEnCoursExecEncfByOfAsync(string numeroOf, string posteCode);
        Task<MfgheadOrdreFabrication?> GetOfDetailsAsync(string numeroOf);
        Task AddExecEncfAsync(ExecControleOf entity);
        Task SaveChangesAsync();
        
        Task RemoveTranches(System.Collections.Generic.IEnumerable<ExecControleTranche> items);
        Task AddTranche(ExecControleTranche item);
        Task RemovePieceTypes(System.Collections.Generic.IEnumerable<ExecPieceType> items);
        Task AddPieceType(ExecPieceType item);
    }
}
