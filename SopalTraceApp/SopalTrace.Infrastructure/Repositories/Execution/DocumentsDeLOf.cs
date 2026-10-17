using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

/// <summary>
/// Un OF démarré garde les documents avec lesquels il a commencé, même archivés depuis (changement de code, nouvelle
/// version) : seuls les nouveaux OF prennent le document actif.
/// </summary>
internal static class DocumentsDeLOf
{
    /// <summary>Document de la première fiche de ce type de l'OF (pour ce poste ou cette machine si précisé), sinon null.</summary>
    public static Task<Guid?> DocumentDejaUtiliseAsync(SopalTraceDbContext context, Guid execControleOfId, string typeExecution,
        string? posteCode = null, string? machineCode = null) =>
        context.ExecControleDocumentStatuts
            .AsNoTracking()
            .Where(s => s.ExecControleOfId == execControleOfId && s.TypeDocument == typeExecution && s.DocId != null
                        && (posteCode == null || s.PosteCode == posteCode)
                        && (machineCode == null || s.MachineCode == machineCode))
            .OrderBy(s => s.CreationOrder)
            .Select(s => s.DocId)
            .FirstOrDefaultAsync();
}
