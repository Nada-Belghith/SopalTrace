using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace SopalTrace.Infrastructure.Rapports;

/// <summary>
/// Images « dans la cellule » d'Excel 365 (Insertion &gt; Images &gt; Placer dans la cellule) : la cellule porte une
/// valeur enrichie (vm) qui renvoie, par xl/metadata.xml et xl/richData, au fichier image. Seul Excel 365 sait les
/// afficher (ailleurs la cellule vaut #VALUE!) : ces images sont lues ici pour être replacées en images flottantes.
/// </summary>
internal static class ImagesDansCellules
{
    private static readonly XNamespace Tableur = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relations = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Paquet = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace DonneesEnrichies = "http://schemas.microsoft.com/office/spreadsheetml/2017/richdata";
    private static readonly XNamespace RelationsEnrichies = "http://schemas.microsoft.com/office/spreadsheetml/2022/richvaluerel";

    public static bool Presentes(byte[] contenu)
    {
        using var archive = Ouvrir(contenu);
        return archive.Entries.Any(e => e.FullName.StartsWith("xl/richData/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Image de chaque cellule (adresse « A1 ») de la première feuille qui en contient une.</summary>
    public static Dictionary<string, byte[]> Lire(byte[] contenu)
    {
        var images = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        using var archive = Ouvrir(contenu);

        var feuille = CheminPremiereFeuille(archive);
        var metadonnees = Xml(archive, "xl/metadata.xml");
        var valeurs = Xml(archive, "xl/richData/rdrichvalue.xml");
        var structures = Xml(archive, "xl/richData/rdrichvaluestructure.xml");
        var liens = Xml(archive, "xl/richData/richValueRel.xml");
        var cibles = Xml(archive, "xl/richData/_rels/richValueRel.xml.rels");
        if (feuille == null || metadonnees == null || valeurs == null || structures == null || liens == null || cibles == null)
            return images;

        // vm (1-based) → bloc de valueMetadata → numéro du bloc futureMetadata XLRICHVALUE → valeur enrichie (rvb i).
        var blocsValeur = metadonnees.Root!.Element(Tableur + "valueMetadata")?.Elements(Tableur + "bk").ToList() ?? new();
        var valeursEnrichies = metadonnees.Root!.Elements(Tableur + "futureMetadata")
            .FirstOrDefault(f => (string?)f.Attribute("name") == "XLRICHVALUE")?
            .Elements(Tableur + "bk")
            .Select(b => (int?)b.Descendants(DonneesEnrichies + "rvb").FirstOrDefault()?.Attribute("i"))
            .ToList() ?? new();
        var structuresParIndice = structures.Root!.Elements(DonneesEnrichies + "s").ToList();
        var valeursParIndice = valeurs.Root!.Elements(DonneesEnrichies + "rv").ToList();
        var idsLiens = liens.Root!.Elements(RelationsEnrichies + "rel").Select(r => (string?)r.Attribute(Relations + "id")).ToList();
        var ciblesParId = cibles.Root!.Elements(Paquet + "Relationship")
            .ToDictionary(r => (string)r.Attribute("Id")!, r => (string)r.Attribute("Target")!);

        foreach (var cellule in Xml(archive, feuille)!.Descendants(Tableur + "c"))
        {
            var adresse = (string?)cellule.Attribute("r");
            if (adresse == null || !int.TryParse((string?)cellule.Attribute("vm"), out var vm) || vm < 1 || vm > blocsValeur.Count) continue;
            var indiceFutur = (int?)blocsValeur[vm - 1].Element(Tableur + "rc")?.Attribute("v");
            if (indiceFutur is not { } f || f < 0 || f >= valeursEnrichies.Count || valeursEnrichies[f] is not { } i || i < 0 || i >= valeursParIndice.Count) continue;

            var valeur = valeursParIndice[i];
            var structure = (int?)valeur.Attribute("s") is { } s && s >= 0 && s < structuresParIndice.Count ? structuresParIndice[s] : null;
            var position = structure?.Elements(DonneesEnrichies + "k").ToList().FindIndex(k => (string?)k.Attribute("n") == "_rvRel:LocalImageIdentifier") ?? -1;
            var champs = valeur.Elements(DonneesEnrichies + "v").ToList();
            if (position < 0 || position >= champs.Count || !int.TryParse(champs[position].Value, out var lien) || lien < 0 || lien >= idsLiens.Count) continue;
            if (idsLiens[lien] is not { } id || !ciblesParId.TryGetValue(id, out var cible)) continue;

            var image = archive.GetEntry(Resoudre("xl/richData/", cible));
            if (image == null) continue;
            using var flux = image.Open();
            using var memoire = new MemoryStream();
            flux.CopyTo(memoire);
            images[adresse] = memoire.ToArray();
        }
        return images;
    }

    private static ZipArchive Ouvrir(byte[] contenu) => new(new MemoryStream(contenu, writable: false), ZipArchiveMode.Read);

    internal static XDocument? Xml(ZipArchive archive, string chemin)
    {
        var entree = archive.GetEntry(chemin);
        if (entree == null) return null;
        using var flux = entree.Open();
        return XDocument.Load(flux);
    }

    private static string? CheminPremiereFeuille(ZipArchive archive)
    {
        var classeur = Xml(archive, "xl/workbook.xml");
        var relations = Xml(archive, "xl/_rels/workbook.xml.rels");
        var id = (string?)classeur?.Root?.Element(Tableur + "sheets")?.Elements(Tableur + "sheet").FirstOrDefault()?.Attribute(Relations + "id");
        var cible = (string?)relations?.Root?.Elements(Paquet + "Relationship").FirstOrDefault(r => (string?)r.Attribute("Id") == id)?.Attribute("Target");
        return cible == null ? null : Resoudre("xl/", cible);
    }

    /// <summary>Chemin dans l'archive d'une cible de relation (relative au dossier de la partie, ou absolue).</summary>
    internal static string Resoudre(string dossier, string cible)
    {
        if (cible.StartsWith('/')) return cible.TrimStart('/');
        var parties = (dossier + cible).Split('/').ToList();
        for (var i = 0; i < parties.Count; i++)
        {
            if (parties[i] != "..") continue;
            parties.RemoveRange(i - 1, 2);
            i -= 2;
        }
        return string.Join('/', parties);
    }
}
