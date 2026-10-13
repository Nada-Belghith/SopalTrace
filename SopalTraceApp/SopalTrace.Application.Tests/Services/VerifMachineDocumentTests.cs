using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Plans de vérification machine stockés dans Document_* (type VERIF_MACHINE, lignes sans section).</summary>
    public class VerifMachineDocumentTests : IDisposable
    {
        private readonly SopalTraceDbContext _context;

        public VerifMachineDocumentTests()
        {
            var options = RowVersionEnMemoire.Activer(new DbContextOptionsBuilder<SopalTraceDbContext>())
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new SopalTraceDbContext(options);
        }

        public void Dispose() => _context.Dispose();

        private static CreateDocumentVerifMachineRequestDto Demande(Guid familleDto)
        {
            var echeance = new CreateDocumentVerifMachineEcheanceDto
            {
                OrdreAffiche = 1,
                PeriodiciteMachineId = Guid.NewGuid(),
                MatricePieces = { new CreateDocumentVerifMachineMatricePieceDto { FamilleId = familleDto, RoleVerif = "PRC", PieceRefId = Guid.NewGuid() } }
            };
            return new CreateDocumentVerifMachineRequestDto
            {
                Nom = "VM MAS19",
                MachineCode = "MAS19",
                Familles = { new CreateDocumentVerifMachineFamilleDto { Id = familleDto, RefFamilleCorpsId = Guid.NewGuid(), OrdreAffiche = 1 } },
                LignesConformite = { new CreateDocumentVerifMachineLigneDto { OrdreAffiche = 1, TypeLigne = TypesLigneVerifMachine.Conformite, LibelleRisque = "Pièce conforme", LibelleMethode = "Passage stations 2-4" } },
                LignesRisques = { new CreateDocumentVerifMachineLigneDto { OrdreAffiche = 2, TypeLigne = TypesLigneVerifMachine.Risque, LibelleRisque = "Mauvais tarage", LibelleMethode = "Fuite à 26 bar", Echeances = { echeance } } }
            };
        }

        [Fact]
        public void AllerRetour_ConserveTypesLibellesEtMatriceReliéeAuxFamilles()
        {
            var familleDto = Guid.NewGuid();
            var entite = DocumentVerifMachineMapper.ToEntity(Demande(familleDto), "55555");

            Assert.Equal(TypesDocument.VerifMachine, entite.TypeDocumentCode);
            Assert.All(entite.DocumentLignes, l => Assert.Null(l.SectionId));
            Assert.All(entite.DocumentLignes, l => Assert.Equal(entite.Id, l.EnteteId));

            var famille = Assert.Single(entite.DocumentVerifMachineFamilles);
            var matrice = entite.DocumentLignes.SelectMany(l => l.DocumentVerifMachineEcheances).SelectMany(e => e.DocumentVerifMachineMatricePieces).Single();
            Assert.Equal(famille.Id, matrice.FamilleId);

            var dto = DocumentVerifMachineMapper.ToDto(entite);
            Assert.True(dto.AfficheConformite);
            Assert.True(dto.AfficheFamilles);
            var risque = dto.Lignes.Single(l => l.TypeLigne == TypesLigneVerifMachine.Risque);
            Assert.Equal(("Mauvais tarage", "Fuite à 26 bar"), (risque.LibelleRisque, risque.LibelleMethode));
        }

        [Fact]
        public async Task Depot_NeVoitQueLesPlansDeVerification()
        {
            var plan = DocumentVerifMachineMapper.ToEntity(Demande(Guid.NewGuid()), "55555");
            var repository = new DocumentVerifMachineEnteteRepository(_context);
            await repository.AddAsync(plan);
            _context.DocumentEntetes.Add(new DocumentEntete
            {
                TypeDocumentCode = TypesDocument.PlanFabrication, Portee = PorteesDocument.Specifique,
                CodeArticleSageVersionne = "ART.1", Nom = "ART.1", MachineCode = "MAS19",
                Statut = DocumentStatuts.Actif, CreePar = "55555"
            });
            await _context.SaveChangesAsync();

            var parMachine = Assert.Single(await repository.GetByMachineCodeAsync("MAS19"));
            Assert.Equal(plan.Id, parMachine.Id);
            Assert.Equal(2, parMachine.DocumentLignes.Count);
            Assert.Single(await repository.GetAllWithRelationsAsync());
        }
    }
}
