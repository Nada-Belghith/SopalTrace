using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using SopalTrace.Application.Mappers;
using SopalTrace.Application.Validators.QualityPlans;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Plan d'échantillonnage = Document_Entete (PLAN_ECHANTILLONNAGE) + paramètres ISO 2859-1 (SamplingPlan).</summary>
    public class PlanEchantillonnageTests : IDisposable
    {
        private readonly SopalTraceDbContext _context;

        public PlanEchantillonnageTests()
        {
            var options = RowVersionEnMemoire.Activer(new DbContextOptionsBuilder<SopalTraceDbContext>())
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new SopalTraceDbContext(options);
        }

        public void Dispose() => _context.Dispose();

        private static CreateDocumentEchantillonnageRequestDto Demande(int acceptation = 1, int rejet = 2, string niveau = "II") => new()
        {
            NiveauControle = niveau, TypePlan = "SIMPLE", ModeControle = "NORMAL", NqaId = 3,
            CritereAcceptationAc = acceptation, CritereRejetRe = rejet,
            Remarques = "r", LegendeMoyens = "l", CommentaireVersion = "c", RefFormulaireCodeReference = "FE-ECHAN-01"
        };

        [Fact]
        public void AllerRetour_ConserveLesParametresEtLaValeurNqa()
        {
            var entete = DocumentEchantillonnageMapper.NouveauPlan(Demande(), 3);
            entete.SamplingPlan!.AcceptableQualityLevel = new Nqa { Id = 3, ValeurNqa = 1.5m };
            entete.Statut = DocumentStatuts.Actif;

            var dto = entete.ToResponseDto()!;

            Assert.Equal(TypesDocument.PlanEchantillonnage, entete.TypeDocumentCode);
            Assert.Equal(("II", "SIMPLE", "NORMAL", 3, 1.5m), (dto.NiveauControle, dto.TypePlan, dto.ModeControle, dto.NqaId, dto.ValeurNqa));
            Assert.Equal((1, 2), (dto.CritereAcceptationAc, dto.CritereRejetRe));
        }

        [Theory]
        [InlineData(1, 2, "II", true)]
        [InlineData(2, 2, "II", true)]    // Ac / Re libres (pas de règle métier)
        [InlineData(3, 1, "II", true)]
        [InlineData(1, 2, "NIVEAU II", false)] // valeur refusée par la base
        public void Validateur_AppliqueLesReglesDeLaBase(int acceptation, int rejet, string niveau, bool valide)
        {
            var resultat = new CreateDocumentEchantillonnageRequestValidator().Validate(Demande(acceptation, rejet, niveau));
            Assert.Equal(valide, resultat.IsValid);
        }

        [Fact]
        public async Task Depot_RetourneLePlanActifAvecSesParametres()
        {
            var plan = DocumentEchantillonnageMapper.NouveauPlan(Demande(), 3);
            plan.Nom = DocumentEchantillonnageMapper.Nom(0);
            plan.Statut = DocumentStatuts.Actif;
            plan.CreePar = "55555";
            _context.Nqas.Add(new Nqa { Id = 3, ValeurNqa = 1.5m });
            await new DocumentEchantillonnageEnteteRepository(_context).AddAsync(plan);
            _context.DocumentEntetes.Add(new DocumentEntete { TypeDocumentCode = TypesDocument.PlanAssemblage, Nom = "Autre", Statut = DocumentStatuts.Actif, CreePar = "55555" });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var actif = await new DocumentEchantillonnageEnteteRepository(_context).GetPlanActifAsync();

            Assert.Equal(plan.Id, actif?.Id);
            Assert.Equal(1.5m, actif!.SamplingPlan!.AcceptableQualityLevel.ValeurNqa);
        }
    }
}
