using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories.Execution;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>
    /// Sélection du plan d'assemblage (TrouverPlanAssemblageIdAsync) : toujours par la famille de produit fini
    /// de l'article (ou l'article lui-même), puis par poste.
    /// </summary>
    public class ExecPlanAssemblageRepositoryTests : IDisposable
    {
        private const string ArticleManuel = "25AKA01-1";
        private const string ArticleSoupape = "25B0A01-1";

        private readonly SopalTraceDbContext _context;
        private readonly ExecPlanAssemblageRepository _repository;

        public ExecPlanAssemblageRepositoryTests()
        {
            var options = RowVersionEnMemoire.Activer(new DbContextOptionsBuilder<SopalTraceDbContext>())
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new SopalTraceDbContext(options);
            _repository = new ExecPlanAssemblageRepository(_context);

            _context.FamilleProduitFinis.AddRange(
                new FamilleProduitFini { Code = "RBGFM", Designation = "Manuelle", TypeRobinetCode = "MAN", Actif = true },
                new FamilleProduitFini { Code = "RBGFA_SOUPAPE", Designation = "Soupape", TypeRobinetCode = "SOUP", Actif = true, PlanParPoste = true });
            AjouterProduitFini(ArticleManuel, "RBGFM");
            AjouterProduitFini(ArticleSoupape, "RBGFA_SOUPAPE");
            _context.SaveChanges();
        }

        public void Dispose() => _context.Dispose();

        private void AjouterProduitFini(string code, string famille)
        {
            _context.Articles.Add(new Article
            {
                CodeArticle = code, Designation = code, NatureArticleCode = "PF", TypeArticle = "PF", Origin = ArticleOrigins.Manufactured, Actif = true
            });
            _context.ProduitFinis.Add(new ProduitFini { CodeArticle = code, FamilleProduitFiniCode = famille, ArticleType = "PF" });
        }

        private Guid AjouterPlan(string? famille, string? posteCode, string? article = null, string statut = DocumentStatuts.Actif, int ageJours = 0)
        {
            var plan = new DocumentEntete
            {
                Id = Guid.NewGuid(),
                TypeDocumentCode = TypesDocument.PlanAssemblage,
                Nom = $"Plan {famille ?? article} {posteCode}",
                FamilleProduitFiniCode = famille,
                CodeArticleSageVersionne = article,
                PosteCode = posteCode,
                Statut = statut,
                CreePar = "ADMIN",
                CreeLe = DateTime.Now.AddDays(-ageJours)
            };
            _context.DocumentEntetes.Add(plan);
            _context.SaveChanges();
            return plan.Id;
        }

        [Fact]
        public async Task PlanDeLaFamilleSurLePoste_Trouve()
        {
            var plan = AjouterPlan("RBGFM", "PAS71");

            Assert.Equal(plan, await _repository.TrouverPlanAssemblageIdAsync(ArticleManuel, "PAS71"));
        }

        [Fact]
        public async Task PlanDUneAutreFamilleSurLePoste_JamaisRepris()
        {
            // Article manuel sur PAS72 : seul le plan soupape existe pour ce poste, il ne doit pas s'afficher.
            AjouterPlan("RBGFA_SOUPAPE", "PAS72");

            Assert.Null(await _repository.TrouverPlanAssemblageIdAsync(ArticleManuel, "PAS72"));
        }

        [Fact]
        public async Task PlanCommunATousLesPostes_RetenuPourUneFamilleSansPlanParPoste()
        {
            var commun = AjouterPlan("RBGFM", posteCode: null);

            Assert.Equal(commun, await _repository.TrouverPlanAssemblageIdAsync(ArticleManuel, "PAS72"));
        }

        [Fact]
        public async Task PlanDuPoste_PrioritaireSurLePlanCommun()
        {
            AjouterPlan("RBGFM", posteCode: null);
            var duPoste = AjouterPlan("RBGFM", "PAS71", ageJours: 10);

            Assert.Equal(duPoste, await _repository.TrouverPlanAssemblageIdAsync(ArticleManuel, "PAS71"));
        }

        [Fact]
        public async Task PlansParPosteEtPlanDeFamille_ChaquePosteASonPlan()
        {
            // Soupape : un plan par poste, et un plan de famille valable sur les autres postes.
            var pas72 = AjouterPlan("RBGFA_SOUPAPE", "PAS72");
            var pas73 = AjouterPlan("RBGFA_SOUPAPE", "PAS73");
            var famille = AjouterPlan("RBGFA_SOUPAPE", posteCode: null);

            Assert.Equal(pas72, await _repository.TrouverPlanAssemblageIdAsync(ArticleSoupape, "PAS72"));
            Assert.Equal(pas73, await _repository.TrouverPlanAssemblageIdAsync(ArticleSoupape, "PAS73"));
            Assert.Equal(famille, await _repository.TrouverPlanAssemblageIdAsync(ArticleSoupape, "PAS71"));
        }

        [Fact]
        public async Task PlanDeLArticle_PrioritaireSurLaFamille()
        {
            AjouterPlan("RBGFM", "PAS71");
            var specifique = AjouterPlan(famille: null, "PAS71", article: ArticleManuel, ageJours: 10);

            Assert.Equal(specifique, await _repository.TrouverPlanAssemblageIdAsync(ArticleManuel, "PAS71"));
        }

        [Fact]
        public async Task PlanNonActif_Ignore()
        {
            AjouterPlan("RBGFM", "PAS71", statut: DocumentStatuts.Archive);

            Assert.Null(await _repository.TrouverPlanAssemblageIdAsync(ArticleManuel, "PAS71"));
        }

        [Fact]
        public async Task ArticleSansFamille_AucunPlan()
        {
            AjouterPlan("RBGFM", "PAS71");

            Assert.Null(await _repository.TrouverPlanAssemblageIdAsync("COMPOSANT-1", "PAS71"));
        }
    }
}
