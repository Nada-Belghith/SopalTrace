namespace SopalTrace.Application.Settings;

/// <summary>Paramètres de la génération des documents (section « Rapports »).</summary>
public class RapportsSettings
{
    /// <summary>Adresse du service de conversion PDF Gotenberg (ex. http://gotenberg:3000). Vide : aucune conversion.</summary>
    public string GotenbergUrl { get; set; } = "";

    /// <summary>Délai maximal d'une conversion en PDF.</summary>
    public int DelaiConversionSecondes { get; set; } = 60;

    /// <summary>Taille maximale d'un fichier de modèle envoyé.</summary>
    public int TailleMaximaleModeleKo { get; set; } = 5120;

    /// <summary>
    /// Intervalle de la vérification des modèles livrés à importer (modèle ajouté par une migration pendant que l'API
    /// tourne). Les documents, eux, sont générés à la demande (consultation, impression).
    /// </summary>
    public int IntervalleVerificationMinutes { get; set; } = 5;

    /// <summary>
    /// Import automatique des fichiers {CODE}.xlsx de DossierModelesLivres (et leur rattachement aux formulaires de
    /// départ). Désactivé par défaut : aucun modèle n'arrive tout seul, l'administrateur envoie et rattache lui-même
    /// chaque fichier. Sert aux tests (dossier tests/fixtures/modeles).
    /// </summary>
    public bool ImporterModelesLivres { get; set; }

    /// <summary>Dossier des fichiers à importer quand ImporterModelesLivres est activé.</summary>
    public string DossierModelesLivres { get; set; } = "";
}
