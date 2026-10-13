using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Modèles et plans de fabrication partagent Document_Entete : chaque dépôt ne voit que son type.</summary>
    public class FabricationDocumentRepositoryTests : IDisposable
    {
        private readonly SopalTraceDbContext _context;

        public FabricationDocumentRepositoryTests()
        {
            var options = RowVersionEnMemoire.Activer(new DbContextOptionsBuilder<SopalTraceDbContext>())
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new SopalTraceDbContext(options);
        }

        public void Dispose() => _context.Dispose();

        private static DocumentEntete Document(string type, string nom, string? codeArticle = null) => new()
        {
            Id = Guid.NewGuid(),
            TypeDocumentCode = type,
            Portee = codeArticle == null ? PorteesDocument.Generique : PorteesDocument.Specifique,
            CodeArticleSageVersionne = codeArticle,
            Nom = nom,
            Version = 1,
            Statut = DocumentStatuts.Actif,
            OperationCode = "OP1",
            CreePar = "55555"
        };

        [Fact]
        public async Task Ajout_FixeTypeEtPortee()
        {
            var modeles = new ModeleFabricationEnteteRepository(_context);
            var plans = new PlanFabricationEnteteRepository(_context);
            var modele = new DocumentEntete { Nom = "MOD1", Statut = DocumentStatuts.Actif, CreePar = "55555" };
            var plan = new DocumentEntete { Nom = "ART.1", CodeArticleSageVersionne = "ART.1", Statut = DocumentStatuts.Actif, CreePar = "55555" };

            await modeles.AddAsync(modele);
            await plans.AddAsync(plan);

            Assert.Equal((TypesDocument.ModeleFabrication, PorteesDocument.Generique), (modele.TypeDocumentCode, modele.Portee));
            Assert.Equal((TypesDocument.PlanFabrication, PorteesDocument.Specifique), (plan.TypeDocumentCode, plan.Portee));
        }

        [Fact]
        public async Task Lectures_NeRetournentQueLeTypeDuDepot()
        {
            var modele = Document(TypesDocument.ModeleFabrication, "PAS71");
            var plan = Document(TypesDocument.PlanFabrication, "PAS71", "PAS71.1");
            var planAssemblage = Document(TypesDocument.PlanAssemblage, "PAS71");
            _context.DocumentEntetes.AddRange(modele, plan, planAssemblage);
            await _context.SaveChangesAsync();

            var modeles = new ModeleFabricationEnteteRepository(_context);
            var plans = new PlanFabricationEnteteRepository(_context);

            Assert.Equal(modele.Id, Assert.Single(await modeles.GetAllAsync()).Id);
            Assert.Equal(modele.Id, (await modeles.GetActifByReferenceAsync(TypesDocument.ModeleFabrication, "PAS71"))?.Id);
            Assert.Null(await modeles.GetByIdAsync(planAssemblage.Id));

            Assert.Equal(plan.Id, Assert.Single(await plans.GetByFiltersAsync("OP1")).Id);
            Assert.Null(await plans.GetByIdAsync(modele.Id));
            Assert.Equal(1, await plans.GetLatestVersionAsync("PAS71.1"));
            Assert.Equal(0, await plans.GetLatestVersionAsync("INCONNU"));
        }
    }
}
