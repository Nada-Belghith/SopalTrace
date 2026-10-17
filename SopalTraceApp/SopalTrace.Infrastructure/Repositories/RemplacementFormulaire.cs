using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

/// <summary>
/// Remplacement d'une ligne de formulaire utilisée (changement de code, nouveau logo) : rien n'est modifié, tout est
/// archivé tel quel puis recréé. À appeler dans une transaction.
/// </summary>
internal static class RemplacementFormulaire
{
    /// <summary>
    /// 1. les documents génériques en cours de la ligne, puis la ligne, sont archivés tels quels (dans cet ordre : la base
    /// refuse un document générique actif sur un formulaire archivé) ;
    /// 2. la nouvelle ligne préparée par l'appelant (nouveau code ou nouvelle version, même lignée et même champ
    /// d'application) reçoit la structure de l'ancienne ;
    /// 3. la demande de modèle en attente suit la nouvelle ligne ; avec <paramref name="recopierModele"/> (changement de
    /// code), la nouvelle ligne reçoit le modèle actuel de l'ancienne (rattachement explicite « repris de »), avant que
    /// ses documents soient recréés ;
    /// 4. chaque document est recréé à l'identique sur la nouvelle ligne : même statut, version et nom de la nouvelle ligne,
    /// motif de la version. Les documents spécifiques ne sont pas touchés.
    /// </summary>
    public static async Task<(RefFormulaire Nouveau, int DocumentsRecrees)> RemplacerAsync(SopalTraceDbContext context, Guid formulaireId,
        Func<RefFormulaire, RefFormulaire> nouvelleLigne, string matricule, Func<RefFormulaire, string> motifDeVersion, bool recopierModele = false)
    {
        var ancien = await context.RefFormulaires.AsNoTracking().FirstAsync(f => f.Id == formulaireId);
        var motif = motifDeVersion(ancien);
        var documents = await DocumentsGeneriquesEnCours(context, formulaireId)
            .OrderBy(d => d.CreationOrder)
            .Select(d => new { d.Id, d.Statut })
            .ToListAsync();
        var ids = documents.Select(d => d.Id).ToList();

        await context.DocumentEntetes.Where(d => ids.Contains(d.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Statut, StatutsPlan.Archive));
        await context.RefFormulaires.Where(f => f.Id == formulaireId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Statut, StatutsPlan.Archive));

        var nouveau = nouvelleLigne(ancien);
        context.RefFormulaires.Add(nouveau);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.CopyFormStructure @SourceFormId = {formulaireId}, @TargetFormId = {nouveau.Id}");
        await ModelesDesFormulaires.DeplacerDemandesAsync(context, formulaireId, nouveau.Id);
        if (recopierModele)
            await ModelesDesFormulaires.RecopierModeleAsync(context, formulaireId, nouveau.Id, matricule,
                $"Repris de {ancien.CodeReference}.{ancien.Version} : {motif}");

        var recrees = new List<Guid>();
        foreach (var document in documents)
        {
            var nouveauDocument = new SqlParameter("@NewDocumentId", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            await context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.CopyDocumentToForm @SourceDocumentId = @Source, @FormId = @Form, @Status = @Status, @CreatedBy = @CreatedBy, " +
                "@VersionComment = @Motif, @NewDocumentId = @NewDocumentId OUTPUT",
                new SqlParameter("@Source", document.Id), new SqlParameter("@Form", nouveau.Id), new SqlParameter("@Status", document.Statut),
                new SqlParameter("@CreatedBy", matricule), new SqlParameter("@Motif", motif), nouveauDocument);
            recrees.Add((Guid)nouveauDocument.Value);
        }

        // Le nom d'un document porte sa version : celle de la nouvelle ligne (inchangée pour un changement de code).
        if (nouveau.Version != ancien.Version)
            foreach (var document in await context.DocumentEntetes.Where(d => recrees.Contains(d.Id)).ToListAsync())
            {
                document.Nom = NomsDocument.AvecVersion(document.Nom, nouveau.Version);
                if (!string.IsNullOrWhiteSpace(document.Designation))
                    document.Designation = NomsDocument.AvecVersion(document.Designation, nouveau.Version);
            }
        await context.SaveChangesAsync();
        return (nouveau, documents.Count);
    }

    /// <summary>Copie d'une ligne de formulaire : même lignée, même champ d'application, en cours, créée par le matricule.</summary>
    public static RefFormulaire Copie(RefFormulaire ancien, string code, int version, string matricule) => new()
    {
        Id = Guid.NewGuid(),
        CodeReference = code,
        Designation = ancien.Designation,
        Version = version,
        Statut = ancien.Statut,
        CreePar = matricule,
        Role = ancien.Role,
        PosteCode = ancien.PosteCode,
        FamilleProduitFiniCode = ancien.FamilleProduitFiniCode,
        TypeRobinetCode = ancien.TypeRobinetCode,
        MachineCode = ancien.MachineCode,
        NatureArticleCode = ancien.NatureArticleCode,
        VersionGroupId = ancien.VersionGroupId
    };

    /// <summary>Documents génériques non archivés d'une ligne de formulaire : ceux qu'un remplacement recrée.</summary>
    public static IQueryable<DocumentEntete> DocumentsGeneriquesEnCours(SopalTraceDbContext context, Guid formulaireId) =>
        context.DocumentEntetes.Where(d => d.FormulaireId == formulaireId && d.Portee == PorteesDocument.Generique
                                           && d.Statut != StatutsPlan.Archive);

    public static async Task<bool> EstUtiliseAsync(SopalTraceDbContext context, Guid formulaireId) =>
        await context.DocumentEntetes.AnyAsync(d => d.FormulaireId == formulaireId)
        || await context.ExecControleDocumentStatuts.AnyAsync(s => s.FormId == formulaireId);
}
