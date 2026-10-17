using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Rapports;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

/// <summary>
/// Modèle d'une nouvelle ligne de formulaire (nouvelle version, nouveau code) et demandes de modèle : la relation
/// « code + version → modèle » est toujours écrite dans l'historique des rattachements, jamais déduite. À appeler dans la
/// transaction qui crée la ligne, avant d'y créer des documents.
/// </summary>
internal static class ModelesDesFormulaires
{
    /// <summary>
    /// La nouvelle ligne (même structure que l'ancienne) reçoit le modèle actuel de l'ancienne : rattachement explicite
    /// « repris de ». Faux si l'ancienne n'avait pas de modèle.
    /// </summary>
    public static async Task<bool> RecopierModeleAsync(SopalTraceDbContext context, Guid ancienId, Guid nouveauId, string? matricule, string motif)
    {
        var groupe = await context.RefFormulaires.Where(f => f.Id == ancienId).Select(f => f.VersionGroupId).FirstAsync();
        var lignes = await context.RefFormulaires.AsNoTracking().AsSplitQuery()
            .Where(f => f.VersionGroupId == groupe && f.Id != nouveauId)
            .Include(f => f.RefFormulaireColonneDefs).ThenInclude(c => c.ColumnAllowedValues)
            .ToListAsync();
        var versions = lignes
            .Select(f => new VersionFormulaire(f.Id, f.CodeReference, f.Version, ColonneJsonMapper.SignatureColonnes(f.RefFormulaireColonneDefs), f.VersionGroupId, f.CreationOrder))
            .ToList();
        var ids = versions.Select(v => v.Id).ToList();
        var affectations = await context.ReportTemplateAssignments.AsNoTracking()
            .Where(a => a.FormId != null && ids.Contains(a.FormId.Value))
            .OrderBy(a => a.CreationOrder)
            .Select(a => new AffectationModele(a.ReportTemplateVersionId, a.ReportTemplateVersion.ReportTemplateCode,
                a.ReportTemplateVersion.VersionNumber, a.Kind, a.FormId, a.CreationOrder))
            .ToListAsync();
        var resolution = ResolutionModeleRapport.PourFormulaire(ResolutionModeleRapport.Maintenant, versions.First(v => v.Id == ancienId), versions, affectations);
        if (resolution.Modele is not { } modele) return false;

        context.ReportTemplateAssignments.Add(new ReportTemplateAssignment
        {
            ReportTemplateVersionId = modele.Affectation.VersionId,
            Kind = TypesAffectationModele.StructureFormulaire,
            FormId = nouveauId,
            Comment = motif.Length <= 500 ? motif : motif[..500],
            AssignedBy = matricule,
            AssignedAt = DateTime.Now
        });
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Demande automatique à l'administrateur (nouveau formulaire, nouvelle structure), si le type a un modèle Excel et
    /// qu'aucune demande n'attend déjà pour cette ligne.
    /// </summary>
    public static async Task DemanderAsync(SopalTraceDbContext context, Guid formulaireId, string origine, string? motif, string? matricule)
    {
        var role = await context.RefFormulaires.Where(f => f.Id == formulaireId).Select(f => f.Role).FirstAsync();
        if (role == null || !await context.ReportTemplates.AnyAsync(t => t.FormRole == role)) return;
        if (await context.ReportTemplateRequests.AnyAsync(r => r.FormId == formulaireId && r.Status == DemandesModele.EnAttente)) return;
        context.ReportTemplateRequests.Add(new ReportTemplateRequest
        {
            FormId = formulaireId,
            Origin = origine,
            Reason = motif,
            Status = DemandesModele.EnAttente,
            RequestedBy = matricule,
            RequestedAt = DateTime.Now
        });
        await context.SaveChangesAsync();
    }

    /// <summary>Une ligne remplacée (nouveau code, nouvelle version) transmet sa demande en attente à la nouvelle ligne.</summary>
    public static Task DeplacerDemandesAsync(SopalTraceDbContext context, Guid ancienId, Guid nouveauId) =>
        context.ReportTemplateRequests.Where(r => r.FormId == ancienId && r.Status == DemandesModele.EnAttente)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.FormId, nouveauId));

    /// <summary>Demande en attente devenue sans objet (la structure a encore changé) : annulée, gardée dans l'historique.</summary>
    public static Task AnnulerDemandesAsync(SopalTraceDbContext context, Guid formulaireId, string? matricule) =>
        context.ReportTemplateRequests.Where(r => r.FormId == formulaireId && r.Status == DemandesModele.EnAttente)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, DemandesModele.Annulee)
                .SetProperty(r => r.HandledBy, matricule)
                .SetProperty(r => r.HandledAt, DateTime.Now));
}
