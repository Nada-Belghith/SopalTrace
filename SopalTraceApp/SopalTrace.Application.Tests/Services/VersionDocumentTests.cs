using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Un éditeur resté ouvert ne doit pas écraser les modifications enregistrées entre-temps par un autre utilisateur.</summary>
    public class VersionDocumentTests : IDisposable
    {
        private readonly SopalTraceDbContext _context;
        private readonly SopalTrace.Infrastructure.UnitOfWork.UnitOfWork _unitOfWork;

        public VersionDocumentTests()
        {
            var options = RowVersionEnMemoire.Activer(new DbContextOptionsBuilder<SopalTraceDbContext>())
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new SopalTraceDbContext(options);
            _unitOfWork = new SopalTrace.Infrastructure.UnitOfWork.UnitOfWork(_context);
        }

        public void Dispose() => _context.Dispose();

        private async Task<DocumentEntete> DocumentEnregistreAsync()
        {
            var document = new DocumentEntete { TypeDocumentCode = TypesDocument.PlanAssemblage, Nom = "Plan", Statut = DocumentStatuts.Actif, CreePar = "55555" };
            _context.DocumentEntetes.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        [Fact]
        public async Task VersionAJour_AcceptéeEtEntêteRéécrit()
        {
            var document = await DocumentEnregistreAsync();

            _unitOfWork.VerifierVersionDocument(document, VersionsDocument.EnTexte(document.RowVersion), obligatoire: true);

            Assert.True(_context.Entry(document).Property(d => d.ModifieLe).IsModified);
        }

        [Fact]
        public async Task VersionDépassée_Conflit()
        {
            var document = await DocumentEnregistreAsync();
            var versionOuverture = VersionsDocument.EnTexte(document.RowVersion);

            document.Nom = "Modifié par un collègue";
            await _context.SaveChangesAsync();

            Assert.Throws<ConflitConcurrenceException>(() => _unitOfWork.VerifierVersionDocument(document, versionOuverture, obligatoire: true));
        }

        [Fact]
        public async Task VersionAbsente_RefuséeSeulementSiObligatoire()
        {
            var document = await DocumentEnregistreAsync();

            Assert.Throws<RegleMetierException>(() => _unitOfWork.VerifierVersionDocument(document, null, obligatoire: true));
            _unitOfWork.VerifierVersionDocument(document, null, obligatoire: false);
            Assert.Throws<RegleMetierException>(() => _unitOfWork.VerifierVersionDocument(document, "pas du base64 !", obligatoire: false));
        }
    }
}
