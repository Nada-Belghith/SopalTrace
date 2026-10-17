using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SopalTrace.Application.Rapports;

/// <summary>
/// Données d'un document à mettre en forme : champs du document, sections répétées et, dans chaque section, lignes
/// répétées (avec les colonnes propres au formulaire). Les valeurs sont déjà mises en forme (texte affiché).
/// Les dictionnaires sont triés : la même donnée produit toujours la même empreinte.
/// </summary>
public sealed class DonneesRapport
{
    public SortedDictionary<string, string> Champs { get; } = new(StringComparer.Ordinal);
    public List<DonneesSectionRapport> Sections { get; } = new();

    /// <summary>
    /// Pages suivantes de la première feuille du modèle : la feuille est recopiée pour chacune (ex. registre de
    /// traçabilité au-delà de 10 colonnes), chaque page ayant ses propres valeurs.
    /// </summary>
    public List<DonneesRapport> PagesSuivantes { get; } = new();

    /// <summary>Valeurs des feuilles suivantes du modèle (2e feuille, 3e…), dans l'ordre ; à défaut, celles du document.</summary>
    public List<DonneesRapport> FeuillesSuivantes { get; } = new();

    /// <summary>Valeur d'un champ du modèle dans le contexte d'une section et d'une ligne (texte vide si absente).</summary>
    public string Valeur(string nomChamp, DonneesSectionRapport? section, DonneesLigneRapport? ligne)
    {
        var (portee, nom) = NomsChamps.Decomposer(nomChamp);
        var valeurs = portee switch
        {
            PorteeChamp.ColonneLigne => ligne?.Colonnes,
            PorteeChamp.Ligne => ligne?.Champs,
            PorteeChamp.Section => section?.Champs,
            _ => Champs
        };
        return valeurs != null && valeurs.TryGetValue(nom, out var valeur) ? valeur : "";
    }

    /// <summary>Empreinte SHA-256 (hexadécimal majuscule, comme la base) des données : un document n'est régénéré que si elles changent.</summary>
    public string Empreinte()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(this);
        return Convert.ToHexString(SHA256.HashData(json));
    }
}

public sealed class DonneesSectionRapport
{
    public SortedDictionary<string, string> Champs { get; } = new(StringComparer.Ordinal);
    public List<DonneesLigneRapport> Lignes { get; } = new();
}

public sealed class DonneesLigneRapport
{
    public SortedDictionary<string, string> Champs { get; } = new(StringComparer.Ordinal);
    /// <summary>Colonnes propres au formulaire, par clé de colonne (Ref_Formulaire_ColonneDef.CleColonne).</summary>
    public SortedDictionary<string, string> Colonnes { get; } = new(StringComparer.Ordinal);
}

/// <summary>Portée d'un champ du modèle : où sa valeur est prise.</summary>
public enum PorteeChamp
{
    Document,
    Section,
    Ligne,
    ColonneLigne
}

/// <summary>
/// Syntaxe des champs d'un modèle : {{Date}} (document), {{Section.Libelle}} (section répétée),
/// {{Ligne.Caracteristique}} (ligne répétée), {{Ligne.Colonne.CLE}} (colonne propre au formulaire).
/// </summary>
public static class NomsChamps
{
    public const string PrefixeSection = "Section.";
    public const string PrefixeLigne = "Ligne.";
    public const string PrefixeColonne = "Ligne.Colonne.";

    public static (PorteeChamp Portee, string Nom) Decomposer(string nomChamp)
    {
        if (nomChamp.StartsWith(PrefixeColonne, StringComparison.Ordinal))
            return (PorteeChamp.ColonneLigne, nomChamp[PrefixeColonne.Length..]);
        if (nomChamp.StartsWith(PrefixeLigne, StringComparison.Ordinal))
            return (PorteeChamp.Ligne, nomChamp[PrefixeLigne.Length..]);
        if (nomChamp.StartsWith(PrefixeSection, StringComparison.Ordinal))
            return (PorteeChamp.Section, nomChamp[PrefixeSection.Length..]);
        return (PorteeChamp.Document, nomChamp);
    }

    public static PorteeChamp Portee(string nomChamp) => Decomposer(nomChamp).Portee;

    /// <summary>Nom complet d'une colonne propre au formulaire, à écrire dans le modèle.</summary>
    public static string Colonne(string cleColonne) => PrefixeColonne + cleColonne;
}

/// <summary>Champ proposé aux modèles d'un type de document (affiché dans l'écran des modèles).</summary>
public sealed record ChampRapport(string Nom, string Description, string Exemple)
{
    public PorteeChamp Portee => NomsChamps.Portee(Nom);
}

/// <summary>Nom de fichier sûr (en-têtes HTTP) : lettres et chiffres ASCII, tiret, point et souligné uniquement.</summary>
internal static class TexteFichier
{
    public static string Nettoyer(string texte)
    {
        var resultat = new StringBuilder();
        foreach (var c in texte)
            resultat.Append(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_');
        return resultat.ToString().Trim('_');
    }
}
