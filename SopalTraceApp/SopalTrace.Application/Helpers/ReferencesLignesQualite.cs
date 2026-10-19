using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Helpers;

/// <summary>
/// Complète les référentiels d'un plan juste avant l'enregistrement (plans de fabrication, modèles,
/// documents produit fini et assemblage). L'import Excel n'écrit rien en base : c'est ici, à la
/// sauvegarde validée par l'utilisateur, que les éléments manquants sont rattachés ou créés, et que les
/// libellés de section sont alignés sur leur type (<see cref="LibellesSection"/>).
/// <list type="bullet">
/// <item>Type de caractéristique : rattaché par libellé, créé s'il n'existe pas.</item>
/// <item>Moyen de contrôle saisi en texte libre : rattaché au catalogue s'il y figure, sinon conservé
/// en texte libre (jamais les deux à la fois : contrainte CK_Doc_Ligne_Moyen_XOR).</item>
/// <item>Code instrument : créé s'il n'existe pas (clé étrangère obligatoire).</item>
/// </list>
/// </summary>
public static class ReferencesLignesQualite
{
    private const int LongueurLibelleCaracteristique = 80;
    private const int LongueurMoyenTexteLibre = 100;
    private const int LongueurCodeInstrument = 40;
    private const int LongueurDesignationInstrument = 100;

    public static async Task CompleterAsync(IEnumerable<DocumentSection>? sections, IDictionnaireQualiteRepository repo)
    {
        if (sections == null) return;
        var liste = sections.ToList();
        await NormaliserLibellesAsync(liste, repo);

        var lignes = liste.Where(s => s.DocumentLignes != null).SelectMany(s => s.DocumentLignes).ToList();
        if (lignes.Count == 0) return;

        var caracteristiques = new Dictionary<string, Guid>();
        var moyens = (await repo.GetActiveMoyenControlesAsync()) ?? new List<MoyenControle>();
        var instrumentsConnus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ligne in lignes)
        {
            await CompleterCaracteristiqueAsync(ligne, repo, caracteristiques);
            CompleterMoyen(ligne, moyens);
            await CompleterInstrumentAsync(ligne, repo, instrumentsConnus);
        }
    }

    /// <summary>
    /// Libellé enregistré = libellé du type + périodicité (<see cref="LibellesSection"/>) : la base, l'API et
    /// l'affichage donnent toujours le même titre, quel que soit le texte envoyé par l'éditeur ou l'import.
    /// </summary>
    private static async Task NormaliserLibellesAsync(List<DocumentSection> sections, IDictionnaireQualiteRepository repo)
    {
        if (!sections.Any(s => s.TypeSectionId.HasValue)) return;
        var types = (await repo.GetAllTypeSectionsAsync() ?? new List<TypeSection>()).ToDictionary(t => t.Id);
        var periodicites = (await repo.GetAllPeriodicitesAsync() ?? new List<Periodicite>()).ToDictionary(p => p.Id);

        foreach (var section in sections.Where(s => s.TypeSectionId.HasValue))
        {
            if (!types.TryGetValue(section.TypeSectionId!.Value, out var type)) continue;
            string? periodicite = section.PeriodiciteId.HasValue && periodicites.TryGetValue(section.PeriodiciteId.Value, out var p)
                ? LibellesSection.Periodicite(p.NbrPieces, p.Unite, p.NbrHeures)
                : null;
            section.LibelleSection = LibellesSection.Construire(type.Libelle, periodicite, section.LibelleSection);
        }
    }

    private static async Task CompleterCaracteristiqueAsync(DocumentLigne ligne, IDictionnaireQualiteRepository repo, Dictionary<string, Guid> dejaVus)
    {
        if (ligne.TypeCaracteristiqueId.HasValue || string.IsNullOrWhiteSpace(ligne.LibelleAffiche)) return;

        // Le catalogue limite le libellé à 80 caractères : la recherche se fait sur cette même forme,
        // sinon un libellé long serait recréé à chaque enregistrement.
        var libelle = TexteReference.Tronquer(TexteReference.SurUneLigne(ligne.LibelleAffiche), LongueurLibelleCaracteristique);
        var cle = TexteReference.Cle(libelle);

        if (!dejaVus.TryGetValue(cle, out var id))
        {
            var existant = await repo.GetTypeCaracteristiqueByLibelleAsync(libelle);
            if (existant == null)
            {
                existant = new TypeCaracteristique
                {
                    Id = Guid.NewGuid(),
                    Code = $"CAR-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    Libelle = libelle,
                    Actif = true
                };
                await repo.AddTypeCaracteristiqueAsync(existant);
            }
            id = existant.Id;
            dejaVus[cle] = id;
        }
        ligne.TypeCaracteristiqueId = id;
    }

    private static void CompleterMoyen(DocumentLigne ligne, List<MoyenControle> catalogue)
    {
        if (string.IsNullOrWhiteSpace(ligne.MoyenTexteLibre))
        {
            ligne.MoyenTexteLibre = null;
            return;
        }

        if (ligne.MoyenControleId.HasValue)
        {
            ligne.MoyenTexteLibre = null;
            return;
        }

        var connu = TrouverMoyen(catalogue, ligne.MoyenTexteLibre);
        if (connu != null)
        {
            ligne.MoyenControleId = connu.Id;
            ligne.MoyenTexteLibre = null;
        }
        else
        {
            ligne.MoyenTexteLibre = TexteReference.Tronquer(TexteReference.SurUneLigne(ligne.MoyenTexteLibre), LongueurMoyenTexteLibre);
        }
    }

    /// <summary>Recherche par libellé, par libellé sans précision entre parenthèses, puis par code.</summary>
    public static MoyenControle? TrouverMoyen(IEnumerable<MoyenControle> catalogue, string? texte)
    {
        var cle = TexteReference.Cle(texte);
        if (cle.Length == 0) return null;

        return catalogue.FirstOrDefault(m => TexteReference.Cle(m.Libelle) == cle)
            ?? catalogue.FirstOrDefault(m => TexteReference.Cle(TexteReference.SansPrecisionFinale(m.Libelle)) == cle)
            ?? catalogue.FirstOrDefault(m => TexteReference.Cle(m.Code) == cle);
    }

    private static async Task CompleterInstrumentAsync(DocumentLigne ligne, IDictionnaireQualiteRepository repo, HashSet<string> connus)
    {
        if (string.IsNullOrWhiteSpace(ligne.InstrumentCode))
        {
            ligne.InstrumentCode = null;
            return;
        }

        var code = TexteReference.Tronquer(ligne.InstrumentCode.Trim(), LongueurCodeInstrument);
        ligne.InstrumentCode = code;
        if (connus.Contains(code)) return;

        if (await repo.GetInstrumentByCodeAsync(code) == null)
        {
            await repo.AddInstrumentAsync(new Instrument
            {
                CodeInstrument = code,
                Designation = TexteReference.Tronquer(code, LongueurDesignationInstrument),
                Actif = true
            });
        }
        connus.Add(code);
    }
}
