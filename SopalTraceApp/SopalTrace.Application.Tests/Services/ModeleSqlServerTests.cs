using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>
    /// Le modèle EF SQL Server est validé au premier usage du contexte (démarrage de l'API) : une règle
    /// refusée (ex. clé portant sur une colonne calculée) n'apparaît pas avec la base en mémoire.
    /// </summary>
    public class ModeleSqlServerTests
    {
        private static SopalTraceDbContext Contexte() => new(new DbContextOptionsBuilder<SopalTraceDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModeleSeulement;Trusted_Connection=True")
            .Options);

        [Fact]
        public void Modele_valide_sans_connexion()
        {
            using var contexte = Contexte();
            Assert.NotNull(contexte.Model.FindEntityType(typeof(Article)));
        }

        [Fact]
        public void Origine_article_calculee_par_la_base()
        {
            using var contexte = Contexte();
            var origine = contexte.Model.FindEntityType(typeof(Article))!.FindProperty(nameof(Article.Origin))!;
            Assert.Contains(ArticleOrigins.Purchased, origine.GetComputedColumnSql());
        }
    }
}
