using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Infrastructure.Data;

// Complément du contexte généré (Database First) : ce fichier n'est pas écrasé par le scaffold.
public partial class SopalTraceDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Deux administrateurs qui traitent la même demande en même temps : le second enregistrement
        // échoue (UPDATE … WHERE AccountStatus = valeur lue) au lieu d'écraser la décision du premier.
        modelBuilder.Entity<UtilisateursApp>().Property(u => u.AccountStatus).IsConcurrencyToken();

        // Article.Origin est une colonne calculée (depuis TypeArticle) qui sert de clé alternative à la
        // nomenclature (parent fabriqué) : EF interdit une clé recalculée à chaque mise à jour, on la
        // déclare générée à l'insertion seulement (l'application ne modifie pas les articles Sage).
        modelBuilder.Entity<Article>().Property(a => a.Origin).ValueGeneratedOnAdd();

        // Ordre de création : attribué par la base (séquence) et jamais envoyé par l'application, ni à l'insertion
        // ni aux mises à jour (une entité détachée remise à jour ne peut pas l'écraser).
        OrdreDeCreationParLaBase(modelBuilder.Entity<DocumentEntete>().Property(d => d.CreationOrder));
        OrdreDeCreationParLaBase(modelBuilder.Entity<ExecControleDocumentStatut>().Property(s => s.CreationOrder));
        OrdreDeCreationParLaBase(modelBuilder.Entity<ReportTemplateAssignment>().Property(a => a.CreationOrder));
    }

    private static void OrdreDeCreationParLaBase(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<long> propriete)
    {
        propriete.ValueGeneratedOnAdd();
        propriete.Metadata.SetBeforeSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        propriete.Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
    }
}
