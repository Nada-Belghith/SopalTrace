using SopalTrace.Infrastructure.Rapports;

namespace SopalTrace.Application.Tests.Rapports;

/// <summary>Modèles Excel de Sopal utilisés par les tests : dossier tests/fixtures/modeles du dépôt.</summary>
public static class ModelesDeTest
{
    public static readonly ModelesLivres Livres = new(Dossier());

    private static string Dossier()
    {
        for (var dossier = new DirectoryInfo(AppContext.BaseDirectory); dossier != null; dossier = dossier.Parent)
        {
            var chemin = Path.Combine(dossier.FullName, "tests", "fixtures", "modeles");
            if (Directory.Exists(chemin)) return chemin;
        }
        throw new DirectoryNotFoundException("Dossier tests/fixtures/modeles introuvable.");
    }
}
