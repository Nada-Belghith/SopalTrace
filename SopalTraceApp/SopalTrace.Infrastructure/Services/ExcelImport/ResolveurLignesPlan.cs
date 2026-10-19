using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

/// <summary>
/// Rapproche les lignes lues dans Excel des référentiels qualité, en lecture seule : rien n'est créé
/// pendant l'aperçu. Ce qui manque est créé à l'enregistrement (<see cref="ReferencesLignesQualite"/>).
/// </summary>
public sealed class ResolveurLignesPlan
{
    private const int LongueurLibelle = 250;
    private const int LongueurLibelleCaracteristique = 80;
    private const int LongueurLimite = 100;
    private const int LongueurMoyenTexteLibre = 100;

    private static readonly Regex CodeInstrumentValide = new(@"^[A-Za-z0-9][A-Za-z0-9\-_/.]{0,39}$", RegexOptions.Compiled);
    private static readonly Regex CodeEntreParentheses = new(@"\(\s*([A-Za-z0-9][A-Za-z0-9\-_/.]{0,39})\s*\)\s*$", RegexOptions.Compiled);
    private static readonly Regex LettresEspaceChiffres = new(@"^([A-Za-z]{2,})\s+(\d+)$", RegexOptions.Compiled);

    private readonly Dictionary<string, Guid> _caracteristiques;
    private readonly List<TypeControle> _typesControle;
    private readonly List<MoyenControle> _moyens;
    private readonly HashSet<string> _instruments;

    private readonly SortedSet<string> _instrumentsInconnus = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedSet<string> _typesInconnus = new(StringComparer.OrdinalIgnoreCase);

    private ResolveurLignesPlan(IEnumerable<TypeCaracteristique> caracteristiques, List<TypeControle> typesControle,
        List<MoyenControle> moyens, IEnumerable<Instrument> instruments)
    {
        _caracteristiques = new Dictionary<string, Guid>();
        foreach (var c in caracteristiques)
            _caracteristiques.TryAdd(TexteReference.Cle(c.Libelle), c.Id);
        _typesControle = typesControle;
        _moyens = moyens;
        _instruments = new HashSet<string>(instruments.Select(i => i.CodeInstrument), StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<ResolveurLignesPlan> ChargerAsync(IDictionnaireQualiteRepository repo)
        => new(
            (await repo.GetActiveTypeCaracteristiquesAsync()) ?? new List<TypeCaracteristique>(),
            (await repo.GetActiveTypeControlesAsync()) ?? new List<TypeControle>(),
            (await repo.GetActiveMoyenControlesAsync()) ?? new List<MoyenControle>(),
            (await repo.GetActiveInstrumentsAsync()) ?? new List<Instrument>());

    public ImportExcelLigneDto Resoudre(LigneLue lue, List<string> avertissements)
    {
        var position = lue.LigneExcel > 0 ? $"Ligne {lue.LigneExcel}" : "Ligne";
        var ligne = new ImportExcelLigneDto
        {
            LibelleAffiche = Limiter(lue.Caracteristique, LongueurLibelle, $"{position} : libellé de caractéristique", avertissements),
            LimiteSpecTexte = Limiter(lue.Limite, LongueurLimite, $"{position} : limite de spécification", avertissements),
            Observations = lue.Observations,
            ImageBase64 = lue.Image
        };
        foreach (var (cle, valeur) in lue.ColonnesPerso) ligne.ColonnesSupplementaires[cle] = valeur;

        var cleCarac = TexteReference.Cle(TexteReference.Tronquer(ligne.LibelleAffiche, LongueurLibelleCaracteristique));
        if (_caracteristiques.TryGetValue(cleCarac, out var idCarac)) ligne.TypeCaracteristiqueId = idCarac;

        if (lue.TypeControle.Length > 0)
        {
            var cleType = TexteReference.Cle(lue.TypeControle);
            var type = _typesControle.FirstOrDefault(t => TexteReference.Cle(t.Libelle) == cleType)
                    ?? _typesControle.FirstOrDefault(t => TexteReference.Cle(t.Code) == cleType);
            if (type != null) ligne.TypeControleId = type.Id;
            else _typesInconnus.Add(lue.TypeControle);
        }

        if (lue.Moyen.Length > 0)
        {
            var moyen = ReferencesLignesQualite.TrouverMoyen(_moyens, lue.Moyen);
            if (moyen != null) ligne.MoyenControleId = moyen.Id;
            else ligne.MoyenTexteLibre = Limiter(lue.Moyen, LongueurMoyenTexteLibre, $"{position} : moyen de contrôle", avertissements);
        }

        if (lue.Instrument.Length > 0)
        {
            var code = NormaliserCodeInstrument(lue.Instrument);
            if (code != null)
            {
                ligne.InstrumentCode = code;
                if (!_instruments.Contains(code)) _instrumentsInconnus.Add(code);
            }
            else
            {
                // Texte saisi dans la colonne « Code instrument » qui n'est pas un code : conservé en observation.
                ligne.Observations = string.IsNullOrEmpty(ligne.Observations) ? lue.Instrument : $"{lue.Instrument}\n{ligne.Observations}";
                avertissements.Add($"{position} : « {lue.Instrument} » n'est pas un code d'instrument ; texte placé dans les observations.");
            }
        }

        return ligne;
    }

    /// <summary>Avertissements récapitulatifs, à ajouter une fois toutes les lignes résolues.</summary>
    public IEnumerable<string> Recapitulatif()
    {
        if (_typesInconnus.Count > 0)
            yield return $"Type(s) de contrôle absent(s) du référentiel, à choisir dans l'éditeur : {string.Join(", ", _typesInconnus)}.";
        if (_instrumentsInconnus.Count > 0)
            yield return $"Instrument(s) absent(s) du référentiel, créé(s) à l'enregistrement du plan : {string.Join(", ", _instrumentsInconnus)}.";
    }

    /// <summary>
    /// « BEE LAB : (BAN47) » → BAN47, « SER 05 » → SER05, « CLD03 » → CLD03 ; null si le texte n'est pas un code.
    /// </summary>
    public static string? NormaliserCodeInstrument(string? texte)
    {
        var t = TexteReference.SurUneLigne(texte);
        if (t.Length == 0) return null;

        var entreParentheses = CodeEntreParentheses.Match(t);
        if (entreParentheses.Success) return entreParentheses.Groups[1].Value.ToUpperInvariant();

        var espace = LettresEspaceChiffres.Match(t);
        if (espace.Success) t = espace.Groups[1].Value + espace.Groups[2].Value;

        return CodeInstrumentValide.IsMatch(t) ? t.ToUpperInvariant() : null;
    }

    private static string Limiter(string texte, int longueur, string champ, List<string> avertissements)
    {
        if (texte.Length <= longueur) return texte;
        avertissements.Add($"{champ} trop long ({texte.Length} caractères) : coupé à {longueur}.");
        return TexteReference.Tronquer(texte, longueur);
    }
}
