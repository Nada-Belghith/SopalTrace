using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Data.Interceptors;
using SopalTrace.Infrastructure.Repositories;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Journal d'audit, auteur des modifications, hash des refresh tokens et traduction des erreurs de base.</summary>
    public class IntegriteEtAuditTests : IDisposable
    {
        private const string Matricule = "55555";
        private readonly SopalTraceDbContext _context;

        public IntegriteEtAuditTests()
        {
            var utilisateur = new Mock<ICurrentUserService>();
            utilisateur.Setup(u => u.Matricule).Returns(Matricule);

            var options = RowVersionEnMemoire.Activer(new DbContextOptionsBuilder<SopalTraceDbContext>())
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .AddInterceptors(new AuditableEntityInterceptor(utilisateur.Object))
                .Options;
            _context = new SopalTraceDbContext(options);
        }

        public void Dispose() => _context.Dispose();

        private DocumentEntete NouveauDocument() => new()
        {
            TypeDocumentCode = TypesDocumentExecution.PlanAssemblage,
            Nom = "Plan PAS71",
            Statut = DocumentStatuts.Actif,
            CreePar = "AUTRE"
        };

        [Fact]
        public async Task Creation_AttribueIdentifiantAuteurEtJournalise()
        {
            var document = NouveauDocument();
            _context.DocumentEntetes.Add(document);
            await _context.SaveChangesAsync();

            Assert.NotEqual(Guid.Empty, document.Id);
            Assert.Equal(Matricule, document.CreePar);

            var ligne = Assert.Single(_context.AuditLogs);
            Assert.Equal("INSERT", ligne.Action);
            Assert.Equal(document.Id.ToString(), ligne.EntityId);
            Assert.Equal(Matricule, ligne.UserMatricule);
            Assert.Equal("Plan PAS71", JsonDocument.Parse(ligne.Changes).RootElement.GetProperty("Nom").GetString());
        }

        [Fact]
        public async Task Modification_JournaliseAvantApresEtAuteur()
        {
            var document = NouveauDocument();
            _context.DocumentEntetes.Add(document);
            await _context.SaveChangesAsync();

            document.Nom = "Plan PAS72";
            await _context.SaveChangesAsync();

            var ligne = _context.AuditLogs.Single(a => a.Action == "UPDATE");
            var nom = JsonDocument.Parse(ligne.Changes).RootElement.GetProperty("Nom");
            Assert.Equal("Plan PAS71", nom.GetProperty("avant").GetString());
            Assert.Equal("Plan PAS72", nom.GetProperty("apres").GetString());
            Assert.Equal(Matricule, document.ModifiePar);
        }

        [Fact]
        public async Task SecretsJamaisJournalises()
        {
            _context.UtilisateursApps.Add(new UtilisateursApp
            {
                Matricule = Matricule,
                NomComplet = "Opérateur",
                Email = "op@sopal.tn",
                MotDePasseHash = "HASH-SECRET",
                RoleApp = RolesApp.Operateur
            });
            await _context.SaveChangesAsync();

            var ligne = Assert.Single(_context.AuditLogs);
            Assert.DoesNotContain("HASH-SECRET", ligne.Changes);
            Assert.DoesNotContain("MotDePasseHash", ligne.Changes);
        }

        [Fact]
        public async Task OccurrencesPlanifieesEtRecalculsTechniques_NonJournalises()
        {
            var occurrence = new ExecPrelevementIntermediaire { TrancheHoraire = "08:00", HeureNotifPrevue = DateTime.Now };
            _context.ExecPrelevementIntermediaires.Add(occurrence);
            await _context.SaveChangesAsync();
            Assert.Empty(_context.AuditLogs);

            occurrence.EstEnRetard = true;
            await _context.SaveChangesAsync();
            Assert.Empty(_context.AuditLogs);

            occurrence.EstRepondu = true;
            occurrence.Resultat = ResultatsControle.Conforme;
            await _context.SaveChangesAsync();
            Assert.Equal("UPDATE", Assert.Single(_context.AuditLogs).Action);
        }

        [Fact]
        public async Task RefreshToken_StockeUniquementLEmpreinte()
        {
            var utilisateur = new UtilisateursApp { Matricule = Matricule, NomComplet = "Op", Email = "op@sopal.tn", MotDePasseHash = "x", RoleApp = RolesApp.Operateur };
            _context.UtilisateursApps.Add(utilisateur);
            await _context.SaveChangesAsync();
            var repository = new UserRepository(_context);

            await repository.AddRefreshTokenAsync(utilisateur.Id, "jeton-clair", DateTime.Now.AddDays(1));

            var stocke = Assert.Single(_context.RefreshTokens);
            Assert.NotEqual("jeton-clair", stocke.TokenHash);
            Assert.Equal(64, stocke.TokenHash.Length);
            Assert.Equal(utilisateur.Id, (await repository.GetRefreshTokenAsync("jeton-clair"))?.Utilisateur.Id);
            Assert.Null(await repository.GetRefreshTokenAsync("autre-jeton"));
        }

        [Fact]
        public void ConflitDeConcurrence_TraduitEn409()
        {
            var traduite = DbUpdateExceptionExtensions.TraduireErreurBase(new DbUpdateConcurrencyException("conflit"));
            Assert.IsType<ConflitConcurrenceException>(traduite);
        }

        [Fact]
        public void ErreurInconnue_InchangeePourNePasMasquerUnBug()
        {
            var erreur = new InvalidOperationException("bug");
            Assert.Same(erreur, DbUpdateExceptionExtensions.TraduireErreurBase(erreur));
        }

        [Theory]
        [InlineData(TypesDocument.ControlePoste, RolesFormulaire.ResultatControlePoste)]
        [InlineData(TypesDocument.ResultatCF, RolesFormulaire.ResultatControleCf)]
        [InlineData(TypesDocument.PlanAssemblage, RolesFormulaire.EnCoursDeAssemblage)]
        [InlineData(TypesDocument.PlanProduitFini, RolesFormulaire.ProduitFini)]
        public void RoleFormulaireParDefaut_CorrespondAuTypeDeDocument(string typeDocument, string roleAttendu)
        {
            Assert.Equal(roleAttendu, RolesFormulaire.PourTypeDocument(typeDocument));
        }

        [Theory]
        [InlineData("E1", "E1")]
        [InlineData("e2", "E2")]
        [InlineData("3", "E3")]
        [InlineData("Equipe 1", "E1")]
        public void Equipe_EstNormaliseeEnCode(string valeur, string attendu)
        {
            Assert.Equal(attendu, Equipes.Normaliser(valeur));
        }

        [Fact]
        public void Equipe_SansNumero_Refusee()
        {
            Assert.Throws<RegleMetierException>(() => Equipes.Normaliser("Matin"));
            Assert.Null(Equipes.NormaliserOuNull(""));
        }
    }
}
