using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Helpers;

/// <summary>Contenu d'une étiquette de composant : article, lot, rapport de contrôle rapide et quantité (facultatifs).</summary>
public record EtiquetteComposant(string CodeArticle, string NumeroLot, string? RapportControle, decimal? Quantite);

/// <summary>
/// Lecture des codes scannés au magasin. La douchette fonctionne comme un clavier : elle « tape » le contenu
/// puis Entrée. Le serveur refait toute la lecture (le front n'est qu'une aide à la saisie) :
/// <list type="bullet">
/// <item>nettoyage : caractères de contrôle, préfixe d'identification de symbologie (« ]Q1 », « ]C1 »…) ;</item>
/// <item>étiquette composant au format « cd:ARTICLE/lot:LOT[/rpt:RAPPORT][/qty:QUANTITE] » (séparateurs / | ;)
///   ou positionnel « ARTICLE;LOT[;RAPPORT[;QUANTITE]] » ;</item>
/// <item>OF ou BL : le numéro seul, ou « of:NUMERO » / « bl:NUMERO ».</item>
/// </list>
/// </summary>
public static class EtiquetteMagasin
{
    public const int LongueurMax = 300;
    private const int LongueurCode = 30;
    private const int LongueurLot = 100;

    public const string FormatsComposant = "« cd:ARTICLE/lot:LOT/rpt:RAPPORT/qty:QUANTITE » ou « ARTICLE;LOT;RAPPORT;QUANTITE » (rapport et quantité facultatifs)";

    private static readonly Regex PrefixeSymbologie = new(@"^\][A-Za-z][0-9A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex NumeroDocument = new(@"^[A-Za-z0-9][A-Za-z0-9_\-\.]*$", RegexOptions.Compiled);
    private static readonly char[] Separateurs = { '/', '|', ';' };

    /// <summary>
    /// Caractères accentués qu'une douchette réglée en QWERTY produit à la place des chiffres 2, 7, 9 et 0 sur un poste AZERTY
    /// (les autres, comme l'esperluette ou l'apostrophe, peuvent figurer dans un texte ordinaire).
    /// </summary>
    private const string CaracteresClavierInverse = "éèçà";

    /// <summary>Supprime les caractères de contrôle et le préfixe de symbologie, puis les espaces autour.</summary>
    public static string Normaliser(string? brut)
    {
        if (string.IsNullOrWhiteSpace(brut))
            throw new EtiquetteIllisibleException("Aucun code lu. Scannez l'étiquette à nouveau.");

        var sb = new StringBuilder(brut.Length);
        foreach (var c in brut)
            if (!char.IsControl(c)) sb.Append(c);
        var texte = PrefixeSymbologie.Replace(sb.ToString().Trim(), "").Trim();

        if (texte.Length == 0)
            throw new EtiquetteIllisibleException("Aucun code lu. Scannez l'étiquette à nouveau.");
        if (texte.Length > LongueurMax)
            throw new EtiquetteIllisibleException($"Code trop long ({texte.Length} caractères, {LongueurMax} au maximum).");
        if (!texte.Any(char.IsDigit) && texte.Any(c => CaracteresClavierInverse.Contains(c)))
            throw new EtiquetteIllisibleException("Le code lu ne contient aucun chiffre mais des caractères « é è ç à » : la douchette est probablement réglée en clavier QWERTY. Réglez-la en AZERTY.");
        return texte;
    }

    public static EtiquetteComposant LireComposant(string? brut)
    {
        var texte = Normaliser(brut);
        string? article, lot, rapport, quantite;

        if (texte.Contains(':'))
        {
            var champs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var morceau in texte.Split(Separateurs, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var i = morceau.IndexOf(':');
                if (i > 0) champs[morceau[..i].Trim()] = morceau[(i + 1)..].Trim();
            }
            article = champs.GetValueOrDefault("cd");
            lot = champs.GetValueOrDefault("lot");
            rapport = champs.GetValueOrDefault("rpt");
            quantite = champs.GetValueOrDefault("qty");
        }
        else
        {
            var parts = texte.Split(';', StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || parts.Length > 4) throw FormatInconnu(texte);
            article = parts[0];
            lot = parts[1];
            rapport = parts.Length > 2 ? parts[2] : null;
            quantite = parts.Length > 3 ? parts[3] : null;
        }

        if (string.IsNullOrWhiteSpace(article) || string.IsNullOrWhiteSpace(lot)) throw FormatInconnu(texte);
        if (article.Length > LongueurCode) throw new EtiquetteIllisibleException($"Code article trop long (« {article} »).");
        if (lot.Length > LongueurLot) throw new EtiquetteIllisibleException("Numéro de lot trop long.");
        if (rapport?.Length > LongueurLot) throw new EtiquetteIllisibleException("Numéro de rapport de contrôle trop long.");

        return new EtiquetteComposant(article, lot, string.IsNullOrWhiteSpace(rapport) ? null : rapport, LireQuantite(quantite));
    }

    /// <summary>Numéro d'OF scanné (étiquette de l'OF) : le numéro seul ou « of:NUMERO ».</summary>
    public static string LireNumeroOf(string? brut) => LireNumero(brut, "of", "OF");

    /// <summary>Numéro de BL scanné : le numéro seul ou « bl:NUMERO ».</summary>
    public static string LireNumeroBl(string? brut) => LireNumero(brut, "bl", "BL");

    private static string LireNumero(string? brut, string cle, string nom)
    {
        var texte = Normaliser(brut);
        if (texte.StartsWith(cle + ":", StringComparison.OrdinalIgnoreCase)) texte = texte[(cle.Length + 1)..].Trim();
        if (texte.Length > LongueurCode || !NumeroDocument.IsMatch(texte))
            throw new EtiquetteIllisibleException($"« {Tronquer(texte)} » n'est pas un numéro de {nom} valide.");
        return texte;
    }

    private static decimal? LireQuantite(string? valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur)) return null;
        if (!decimal.TryParse(valeur.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var q) || q <= 0)
            throw new EtiquetteIllisibleException($"Quantité « {valeur} » invalide sur l'étiquette.");
        return q;
    }

    private static EtiquetteIllisibleException FormatInconnu(string texte) =>
        new($"Format d'étiquette inconnu (« {Tronquer(texte)} »). Formats attendus : {FormatsComposant}.");

    private static string Tronquer(string texte) => texte.Length <= 60 ? texte : texte[..60] + "…";
}
