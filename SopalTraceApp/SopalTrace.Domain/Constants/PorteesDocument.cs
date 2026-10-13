namespace SopalTrace.Domain.Constants;

/// <summary>
/// Portée d'un document qualité (miroir de CK_DocumentEntete_Portee).
/// GENERIQUE : s'applique selon le type, la nature ou la famille d'article.
/// SPECIFIQUE : s'applique à un code article Sage versionné (CodeArticleSageVersionne obligatoire).
/// </summary>
public static class PorteesDocument
{
    public const string Generique  = "GENERIQUE";
    public const string Specifique = "SPECIFIQUE";
}
