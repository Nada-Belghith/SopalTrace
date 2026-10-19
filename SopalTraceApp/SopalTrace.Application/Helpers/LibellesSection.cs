namespace SopalTrace.Application.Helpers;

/// <summary>
/// Règle unique du libellé d'une section de plan, appliquée à l'enregistrement (serveur) et à l'affichage (front,
/// utils/sectionTitleUtils.js → titreSection) :
/// <list type="bullet">
/// <item>section typée : « Caractéristiques à contrôler » + libellé du type de section (repris tel quel)
/// + « (périodicité) » si une périodicité est choisie ;</item>
/// <item>section sans type : le titre libre saisi.</item>
/// </list>
/// </summary>
public static class LibellesSection
{
    public const string Prefixe = "Caractéristiques à contrôler";
    private const int LongueurMaximale = 300;

    public static string Construire(string? libelleType, string? libellePeriodicite, string? titreLibre)
    {
        string titre;
        if (!string.IsNullOrWhiteSpace(libelleType))
        {
            titre = $"{Prefixe} {libelleType.Trim()}";
            if (!string.IsNullOrWhiteSpace(libellePeriodicite))
                titre += $" ({libellePeriodicite.Trim()})";
        }
        else
        {
            titre = string.IsNullOrWhiteSpace(titreLibre) ? Prefixe : titreLibre.Trim();
        }
        return TexteReference.Tronquer(titre, LongueurMaximale);
    }

    /// <summary>Libellé d'une périodicité (identique à PeriodiciteDto.Libelle et au front).</summary>
    public static string Periodicite(int nbrPieces, string unite, int? nbrHeures)
    {
        var pluriel = nbrPieces > 1 ? "s" : "";
        if (unite == "HEURE")
        {
            var heures = nbrHeures is null or 1 ? "heure" : $"{nbrHeures} heures";
            return nbrPieces == 100 ? $"100% des pièces / {heures}" : $"{nbrPieces} pièce{pluriel} / {heures}";
        }
        return unite == "ECHANTILLON" ? $"{nbrPieces} échantillon{pluriel}" : $"une série de {nbrPieces} pièce{pluriel}";
    }
}
