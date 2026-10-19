using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;
using SopalTrace.Infrastructure.Repositories.Execution;
using Xunit;

namespace SopalTrace.Application.Tests.Services;

/// <summary>
/// Un OF démarré garde les documents avec lesquels il a commencé, même archivés depuis (changement de code du
/// formulaire : FEMANU.1 archivé, DEMANU.1 recréé) ; un nouvel OF prend le document actif.
/// </summary>
public class DocumentsDeLOfTests : IDisposable
{
    private readonly SopalTraceDbContext _context;
    private readonly Guid _ofDemarre = Guid.NewGuid();
    private readonly Guid _nouvelOf = Guid.NewGuid();

    public DocumentsDeLOfTests()
    {
        var options = RowVersionEnMemoire.Activer(new DbContextOptionsBuilder<SopalTraceDbContext>())
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new SopalTraceDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    private Guid Document(string type, string statut, long ordre, string? machine = null)
    {
        var document = new DocumentEntete
        {
            Id = Guid.NewGuid(), TypeDocumentCode = type, Nom = $"{type} V1", Version = 1, Statut = statut, CreePar = "44444",
            MachineCode = machine, CreationOrder = ordre
        };
        _context.DocumentEntetes.Add(document);
        return document.Id;
    }

    private void Fiche(Guid execControleOfId, string type, Guid documentId, long ordre, string? machine = null, string? poste = null) =>
        _context.ExecControleDocumentStatuts.Add(new ExecControleDocumentStatut
        {
            Id = Guid.NewGuid(), ExecControleOfId = execControleOfId, TypeDocument = type, DocId = documentId, MachineCode = machine,
            PosteCode = poste, CreationOrder = ordre
        });

    [Fact]
    public async Task VerificationMachine_LOfDemarreGardeSonPlan_LeNouvelOfPrendLePlanActif()
    {
        var femanu = Document(TypesDocument.VerifMachine, StatutsPlan.Archive, 1, "MAS26");
        var demanu = Document(TypesDocument.VerifMachine, StatutsPlan.Actif, 2, "MAS26");
        Fiche(_ofDemarre, TypesDocumentExecution.VerifMachine, femanu, 10, "MAS26");
        await _context.SaveChangesAsync();
        var repository = new OperateurRepository(_context);

        Assert.Equal(femanu, await repository.GetPlanVerifMachinePourOfAsync(_ofDemarre, "MAS26"));
        Assert.Equal(demanu, await repository.GetPlanVerifMachinePourOfAsync(_nouvelOf, "MAS26"));
    }

    [Fact]
    public async Task Echantillonnage_LOfDemarreGardeSonPlan_SurTousSesPostes()
    {
        var femanu = Document(TypesDocument.PlanEchantillonnage, StatutsPlan.Archive, 1);
        var demanu = Document(TypesDocument.PlanEchantillonnage, StatutsPlan.Actif, 2);
        Fiche(_ofDemarre, TypesDocumentExecution.Echantillonnage, femanu, 10, poste: "PAS71");
        await _context.SaveChangesAsync();
        var repository = new DocumentEchantillonnageEnteteRepository(_context);

        Assert.Equal(femanu, (await repository.GetPlanPourOfAsync(_ofDemarre))?.Id);
        Assert.Equal(demanu, (await repository.GetPlanPourOfAsync(_nouvelOf))?.Id);
    }
}
