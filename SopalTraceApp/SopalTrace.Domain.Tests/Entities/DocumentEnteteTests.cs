using System;
using SopalTrace.Domain.Entities;
using Xunit;

namespace SopalTrace.Domain.Tests.Entities
{
    public class DocumentEnteteTests
    {
        [Fact]
        public void DocumentEntete_Initialisation_ListesNeDoiventPasEtreNull()
        {
            // Arrange & Act
            var document = new DocumentEntete();

            // Assert
            // Dans le domaine, on s'assure que les entités sont toujours dans un état valide dès leur création.
            // Ici, on vérifie que les collections sont bien instanciées pour éviter les NullReferenceException.
            Assert.NotNull(document.DocumentLignes);
            Assert.Empty(document.DocumentLignes);

            Assert.NotNull(document.DocumentSections);
            Assert.Empty(document.DocumentSections);
        }

        [Fact]
        public void DocumentEntete_PeutAssignerDesValeursInitiales()
        {
            // Arrange
            var docId = Guid.NewGuid();
            
            // Act
            var document = new DocumentEntete
            {
                Id = docId,
                Statut = "BROUILLON",
                Version = 1,
                Nom = "Nouveau Plan",
                TypeDocumentCode = "RESULTAT_CF"
            };

            // Assert
            Assert.Equal(docId, document.Id);
            Assert.Equal("BROUILLON", document.Statut);
            Assert.Equal(1, document.Version);
            Assert.Equal("Nouveau Plan", document.Nom);
            Assert.Equal("RESULTAT_CF", document.TypeDocumentCode);
        }
    }
}
