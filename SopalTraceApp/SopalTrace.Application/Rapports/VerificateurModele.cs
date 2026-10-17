using System;
using System.Collections.Generic;
using System.Linq;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Interfaces.Rapports;

namespace SopalTrace.Application.Rapports;

/// <summary>
/// Vérifie les champs d'un modèle par rapport au contrat de son type de document : un champ inconnu est refusé
/// (avec l'emplacement et, si possible, le nom voisin), une colonne propre {{Ligne.Colonne.CLE}} doit exister dans au
/// moins un des formulaires vérifiés (la version de formulaire visée, ou ceux du type), et les formulaires qui ne l'ont
/// pas sont signalés (colonne vide dans leurs documents). Une colonne propre écrite comme un champ de ligne
/// ({{Ligne.Test}}) est signalée avec la bonne écriture ({{Ligne.Colonne.test}}).
/// </summary>
public static class VerificateurModele
{
    public static VerificationModeleDto Verifier(AnalyseModele analyse, IContratRapport definition, IReadOnlyList<ColonnesFormulaire> formulaires)
    {
        var resultat = new VerificationModeleDto();
        resultat.Erreurs.AddRange(analyse.Erreurs);
        resultat.Avertissements.AddRange(analyse.Avertissements);
        resultat.Champs = analyse.Champs.Select(c => new ChampUtiliseDto { Nom = c.Nom, Emplacement = c.Emplacement }).ToList();

        var connus = definition.Champs.Select(c => c.Nom).ToHashSet(StringComparer.Ordinal);
        foreach (var groupe in analyse.Champs.GroupBy(c => c.Nom, StringComparer.Ordinal))
        {
            var nom = groupe.Key;
            var emplacements = string.Join(", ", groupe.Select(c => c.Emplacement).Distinct().Take(3));
            var (portee, nomCourt) = NomsChamps.Decomposer(nom);
            if (portee == PorteeChamp.ColonneLigne)
            {
                var avecColonne = formulaires.Where(f => f.LibellesParCle.ContainsKey(nomCourt)).ToList();
                if (avecColonne.Count == 0)
                {
                    var absence = formulaires.Count == 1
                        ? $"le formulaire {formulaires[0].CodeFormulaire} version {formulaires[0].Version} n'a pas cette colonne"
                        : "aucun formulaire de ce type n'a cette colonne";
                    var cle = CleColonne(nomCourt, formulaires);
                    resultat.Erreurs.Add($"{emplacements} : colonne « {nomCourt} » inconnue, {absence} ({{{{{nom}}}}})."
                                         + (cle == null || cle == nomCourt ? "" : $" Vouliez-vous dire {{{{{NomsChamps.Colonne(cle)}}}}} ?"));
                    continue;
                }
                foreach (var formulaire in formulaires.Except(avecColonne))
                    resultat.Avertissements.Add($"Le formulaire {formulaire.CodeFormulaire} n'a pas la colonne « {nomCourt} » : elle restera vide dans ses documents.");
            }
            else if (!connus.Contains(nom))
            {
                // {{Ligne.Test}} alors que « Test » est une colonne propre d'un formulaire : il faut {{Ligne.Colonne.test}}.
                if (portee == PorteeChamp.Ligne && CleColonne(nomCourt, formulaires) is { } cle)
                {
                    resultat.Erreurs.Add($"{emplacements} : champ {{{{{nom}}}}} inconnu. « {nomCourt} » est une colonne propre du formulaire : " +
                                         $"écrivez {{{{{NomsChamps.Colonne(cle)}}}}}.");
                    continue;
                }
                var voisin = Voisin(nom, connus);
                resultat.Erreurs.Add($"{emplacements} : champ {{{{{nom}}}}} inconnu." + (voisin == null ? "" : $" Vouliez-vous dire {{{{{voisin}}}}} ?"));
            }
        }

        var presents = analyse.Champs.Select(c => c.Nom).ToHashSet(StringComparer.Ordinal);
        var absents = definition.ChampsObligatoires.Where(c => !presents.Contains(c)).ToList();
        if (absents.Count > 0)
            resultat.Erreurs.Add(
                $"Champs obligatoires absents : {string.Join(", ", absents.Select(c => "{{" + c + "}}"))}. Ce fichier n'est pas un modèle de ce document " +
                "(autre formulaire, ou Excel vide d'origine) : partez de l'Excel de la version active (bouton « Excel ») et modifiez-le.");
        return resultat;
    }

    /// <summary>Clé de la colonne propre désignée par ce nom (sa clé ou son libellé, casse et espaces ignorés), ou null.</summary>
    private static string? CleColonne(string nom, IEnumerable<ColonnesFormulaire> formulaires)
    {
        static string Simplifier(string texte) => new(texte.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        var cherche = Simplifier(nom);
        return formulaires.SelectMany(f => f.LibellesParCle)
            .Where(c => Simplifier(c.Key) == cherche || Simplifier(c.Value) == cherche)
            .Select(c => c.Key)
            .FirstOrDefault();
    }

    /// <summary>Champ connu le plus proche (casse ignorée, au plus 3 caractères de différence).</summary>
    private static string? Voisin(string nom, IEnumerable<string> connus) => connus
        .Select(c => (Nom: c, Distance: Distance(nom.ToLowerInvariant(), c.ToLowerInvariant())))
        .Where(c => c.Distance <= 3)
        .OrderBy(c => c.Distance)
        .Select(c => c.Nom)
        .FirstOrDefault();

    private static int Distance(string a, string b)
    {
        var precedente = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var courante = new int[b.Length + 1];
            courante[0] = i;
            for (var j = 1; j <= b.Length; j++)
                courante[j] = Math.Min(Math.Min(courante[j - 1] + 1, precedente[j] + 1), precedente[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            precedente = courante;
        }
        return precedente[b.Length];
    }
}

/// <summary>
/// Données de l'aperçu d'un modèle : le modèle vide, avec seulement l'identité du formulaire visé (code, version,
/// désignation) et quelques lignes vides pour voir le tableau. Jamais les données d'un document existant.
/// </summary>
public static class DonneesApercu
{
    public const int LignesVides = 3;

    public static DonneesRapport Construire(IContratRapport definition, ColonnesFormulaire? formulaire, string? designation)
    {
        var donnees = new DonneesRapport();
        foreach (var champ in definition.Champs.Where(c => c.Portee == PorteeChamp.Document))
            donnees.Champs[champ.Nom] = "";
        if (formulaire != null)
        {
            donnees.Champs["CodeFormulaire"] = formulaire.CodeFormulaire;
            donnees.Champs["VersionFormulaire"] = formulaire.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
            donnees.Champs["DesignationFormulaire"] = designation ?? "";
        }

        var section = new DonneesSectionRapport();
        foreach (var champ in definition.Champs.Where(c => c.Portee == PorteeChamp.Section))
            section.Champs[NomsChamps.Decomposer(champ.Nom).Nom] = "";
        for (var l = 0; l < LignesVides; l++)
        {
            var ligne = new DonneesLigneRapport();
            foreach (var champ in definition.Champs.Where(c => c.Portee == PorteeChamp.Ligne))
                ligne.Champs[NomsChamps.Decomposer(champ.Nom).Nom] = "";
            foreach (var cle in formulaire?.LibellesParCle.Keys ?? Enumerable.Empty<string>())
                ligne.Colonnes[cle] = "";
            section.Lignes.Add(ligne);
        }
        donnees.Sections.Add(section);
        return donnees;
    }
}
