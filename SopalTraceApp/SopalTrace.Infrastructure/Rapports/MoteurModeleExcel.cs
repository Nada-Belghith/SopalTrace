using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ClosedXML.Excel;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;

namespace SopalTrace.Infrastructure.Rapports;

/// <summary>
/// Moteur des modèles Excel (ClosedXML). Le bloc répété va de la première ligne contenant un champ Section.* à la
/// ligne contenant les champs Ligne.* : ses lignes de titre sont recopiées pour chaque section et sa ligne de détail
/// pour chaque ligne de la section (styles, bordures, cellules fusionnées et hauteurs compris). Les lignes situées
/// sous le bloc descendent d'autant ; la zone d'impression suit. Une ligne de détail sans hauteur fixée garde sa
/// hauteur automatique : le texte long est ajusté à la conversion PDF.
/// </summary>
public sealed class MoteurModeleExcel : IMoteurModeleExcel
{
    private static readonly Regex Champ = new(@"\{\{([^{}]*)\}\}", RegexOptions.Compiled);
    // Segments séparés par des points (Date, Ligne.Caracteristique, Equipe1.Heure3, Ligne.Equipe1.Heure3) ; clé libre
    // pour une colonne propre au formulaire (Ligne.Colonne.CLE).
    private static readonly Regex NomValide = new(@"^(?:[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z][A-Za-z0-9]*)*|Ligne\.Colonne\.[A-Za-z0-9_\-]+)$", RegexOptions.Compiled);
    /// <summary>
    /// Option d'un champ de ligne : {{Ligne.Risque|fusion}} fusionne verticalement les cellules consécutives du même
    /// groupe. Clé « Cle.Risque » fournie par les données : elle porte déjà la hiérarchie du tableau (ex. périodicité
    /// commune à deux méthodes d'un même risque) et suffit. Sinon, sur la valeur affichée, les colonnes fusionnées forment
    /// une hiérarchie de gauche à droite : une cellule ne fusionne que si celles de gauche fusionnent aussi.
    /// </summary>
    private const string OptionFusion = "fusion";
    private const string PrefixeCle = "Cle.";
    private const string FeuilleTravail = "__bloc_modele__";
    private const int LongueurMaximaleCellule = 32767;

    /// <summary>Bloc répété : lignes Debut à LigneDetail (incluse) ; les lignes avant LigneDetail sont le titre de section.</summary>
    private sealed record Bloc(int Debut, int LigneDetail);

    /// <summary>Cellule contenant des champs, hors bloc répété.</summary>
    private sealed record CelluleChamps(int Ligne, int Colonne);

    /// <summary>Colonne de la ligne de détail dont les cellules identiques consécutives sont fusionnées.</summary>
    private sealed record ColonneFusion(int Colonne, string Nom);

    private sealed class Structure
    {
        public IXLWorksheet Feuille { get; init; } = null!;
        public Bloc? Bloc { get; set; }
        public List<CelluleChamps> CellulesHorsBloc { get; } = new();
        public List<ColonneFusion> ColonnesFusion { get; } = new();
    }

    public AnalyseModele Analyser(byte[] contenu)
    {
        var analyse = new AnalyseModele();
        XLWorkbook classeur;
        try
        {
            classeur = Ouvrir(Normaliser(contenu));
        }
        catch (Exception)
        {
            analyse.Erreurs.Add("Le fichier n'est pas un classeur Excel .xlsx lisible.");
            return analyse;
        }
        using (classeur)
        {
            var plusieurs = classeur.Worksheets.Count > 1;
            foreach (var feuille in classeur.Worksheets)
            {
                var (erreurs, avertissements, champs) = (analyse.Erreurs.Count, analyse.Avertissements.Count, analyse.Champs.Count);
                LireStructure(feuille, analyse);
                if (!plusieurs) continue;
                // Plusieurs feuilles : chaque message et chaque emplacement nomment leur feuille.
                var prefixe = $"Feuille « {feuille.Name} » : ";
                for (var i = erreurs; i < analyse.Erreurs.Count; i++) analyse.Erreurs[i] = prefixe + analyse.Erreurs[i];
                for (var i = avertissements; i < analyse.Avertissements.Count; i++) analyse.Avertissements[i] = prefixe + analyse.Avertissements[i];
                for (var i = champs; i < analyse.Champs.Count; i++) analyse.Champs[i] = analyse.Champs[i] with { Emplacement = prefixe + analyse.Champs[i].Emplacement };
            }
        }
        return analyse;
    }

    public byte[] Remplir(byte[] contenu, DonneesRapport donnees)
    {
        contenu = Normaliser(contenu);
        using var classeur = Ouvrir(contenu);
        var tailles = TaillesImagesParFeuille(contenu);
        var modeles = classeur.Worksheets.ToList();

        // Pages suivantes de la première feuille : copies de la feuille encore vierge, placées juste après elle.
        var premiere = modeles[0];
        var aRemplir = new List<(IXLWorksheet Feuille, DonneesRapport Donnees, string Modele)> { (premiere, donnees, premiere.Name) };
        for (var i = 0; i < donnees.PagesSuivantes.Count; i++)
            aRemplir.Add((premiere.CopyTo(NomPage(premiere.Name, i + 2), premiere.Position + i + 1), donnees.PagesSuivantes[i], premiere.Name));
        for (var k = 1; k < modeles.Count; k++)
            aRemplir.Add((modeles[k], k - 1 < donnees.FeuillesSuivantes.Count ? donnees.FeuillesSuivantes[k - 1] : donnees, modeles[k].Name));

        foreach (var (feuille, valeurs, modele) in aRemplir)
            RemplirFeuille(classeur, feuille, valeurs, tailles.GetValueOrDefault(modele));

        using var sortie = new MemoryStream();
        classeur.SaveAs(sortie);
        return sortie.ToArray();
    }

    /// <summary>Nom d'une page recopiée (31 caractères au plus, limite d'Excel).</summary>
    private static string NomPage(string nom, int numero)
    {
        var suffixe = $" ({numero})";
        return (nom.Length + suffixe.Length > 31 ? nom[..(31 - suffixe.Length)] : nom) + suffixe;
    }

    private static void RemplirFeuille(XLWorkbook classeur, IXLWorksheet feuille, DonneesRapport donnees,
        IReadOnlyDictionary<string, (long Largeur, long Hauteur)>? taillesImages)
    {
        var analyse = new AnalyseModele();
        var structure = LireStructure(feuille, analyse);
        if (!analyse.Valide)
            throw new InvalidOperationException($"Modèle de rapport invalide (feuille {feuille.Name}) : " + string.Join(" ", analyse.Erreurs));

        var zoneImpression = feuille.PageSetup.PrintAreas.FirstOrDefault()?.RangeAddress;
        var (premiereLigne, premiereColonne) = zoneImpression == null ? (0, 0) : (zoneImpression.FirstAddress.RowNumber, zoneImpression.FirstAddress.ColumnNumber);
        var (derniereLigne, derniereColonne) = zoneImpression == null ? (0, 0) : (zoneImpression.LastAddress.RowNumber, zoneImpression.LastAddress.ColumnNumber);

        var decalage = 0;
        if (structure.Bloc is { } bloc)
        {
            // Modèle avec le tableau fixe du lot : ses sections n'y sont pas répétées une seconde fois.
            var tableauLot = analyse.Champs.Any(c => c.Nom.StartsWith("Lot1.", StringComparison.Ordinal));
            var sections = donnees.Sections.Where(s => !(tableauLot && s.RepriseDansTableauLot)).ToList();
            decalage = RepeterBloc(classeur, feuille, bloc, donnees, sections, DerniereColonne(feuille), structure.ColonnesFusion);
        }

        // Champs du document hors du bloc (les lignes sous le bloc ont été décalées).
        foreach (var cellule in structure.CellulesHorsBloc)
        {
            var ligne = structure.Bloc != null && cellule.Ligne > structure.Bloc.LigneDetail ? cellule.Ligne + decalage : cellule.Ligne;
            Remplacer(feuille.Cell(ligne, cellule.Colonne), nom => donnees.Valeur(nom, null, null));
        }
        RemplacerEnTetesPiedsDePage(feuille, nom => donnees.Valeur(nom, null, null));
        if (taillesImages != null) RetablirTaillesImages(feuille, taillesImages);

        if (zoneImpression != null)
        {
            feuille.PageSetup.PrintAreas.Clear();
            var fin = Math.Max(premiereLigne, derniereLigne + (structure.Bloc != null && derniereLigne >= structure.Bloc.LigneDetail ? decalage : 0));
            // Une colonne ajoutée au modèle à droite de sa zone d'impression est imprimée aussi (sinon le tableau est coupé).
            feuille.PageSetup.PrintAreas.Add(premiereLigne, premiereColonne, fin, Math.Max(derniereColonne, DerniereColonneRemplie(feuille)));
        }
        // Toute la largeur de la zone imprimée sur une page (une colonne ajoutée ne déborde jamais à droite) ; la hauteur
        // se répartit sur autant de pages que nécessaire.
        feuille.PageSetup.FitToPages(1, 0);
    }

    /// <summary>
    /// Texte plus long que sa cellule : retour à la ligne et ligne agrandie pour tout montrer (la conversion en PDF ne
    /// recalcule pas la hauteur d'une ligne). La largeur d'une colonne Excel se compte en caractères d'une police de 11 pt.
    /// </summary>
    private static void AjusterHauteur(IXLWorksheet feuille, int ligne, int derniereColonne)
    {
        var rangee = feuille.Row(ligne);
        var requise = 0.0;
        foreach (var cellule in feuille.Range(ligne, 1, ligne, derniereColonne).CellsUsed(XLCellsUsedOptions.Contents))
        {
            var texte = cellule.GetFormattedString();
            if (string.IsNullOrWhiteSpace(texte)) continue;
            var fusion = cellule.MergedRange();
            var largeur = fusion == null
                ? feuille.Column(cellule.Address.ColumnNumber).Width
                : Enumerable.Range(fusion.RangeAddress.FirstAddress.ColumnNumber, fusion.ColumnCount()).Sum(c => feuille.Column(c).Width);
            var taille = cellule.Style.Font.FontSize > 0 ? cellule.Style.Font.FontSize : 11;
            var caracteresParLigne = Math.Max(1, largeur * 11 / taille / (cellule.Style.Font.Bold ? 1.1 : 1.0) * 0.9);
            var lignes = texte.Replace("\r", "").Split('\n').Sum(p => Math.Max(1, (int)Math.Ceiling(p.Length / caracteresParLigne)));
            if (lignes > 1) cellule.Style.Alignment.WrapText = true;
            requise = Math.Max(requise, lignes * taille * 1.3 + 3);
        }
        if (requise > rangee.Height + 0.5) rangee.Height = requise;
    }

    // ── Lecture et vérification de la structure ──────────────────────────

    private static XLWorkbook Ouvrir(byte[] contenu) => new(new MemoryStream(contenu, writable: false));

    private static Structure LireStructure(IXLWorksheet feuille, AnalyseModele analyse)
    {
        var structure = new Structure { Feuille = feuille };

        var lignesSection = new SortedSet<int>();
        var lignesDetail = new SortedSet<int>();
        var cellulesChamps = new List<(int Ligne, int Colonne, bool Repetee)>();
        var cellulesFusion = new List<(int Ligne, int Colonne, string Nom)>();
        foreach (var cellule in feuille.CellsUsed(XLCellsUsedOptions.Contents))
        {
            if (cellule.DataType == XLDataType.Error)
            {
                analyse.Erreurs.Add($"Cellule {cellule.Address.ToStringRelative()} : erreur Excel « {cellule.Value} », elle apparaîtrait dans les PDF. Corrigez ou videz cette cellule.");
                continue;
            }
            if (cellule.HasFormula || cellule.DataType != XLDataType.Text) continue;
            var texte = cellule.GetString();
            if (!texte.Contains("{{") && !texte.Contains("}}")) continue;

            var emplacement = $"Cellule {cellule.Address.ToStringRelative()}";
            var champs = LireChamps(texte, emplacement, analyse);
            var noms = champs.Select(c => c.Nom).ToList();
            foreach (var (nom, fusion) in champs)
            {
                if (fusion)
                {
                    if (NomsChamps.Portee(nom) is PorteeChamp.Ligne or PorteeChamp.ColonneLigne)
                        cellulesFusion.Add((cellule.Address.RowNumber, cellule.Address.ColumnNumber, nom));
                    else
                        analyse.Erreurs.Add($"{emplacement} : l'option « |{OptionFusion} » n'est possible que sur un champ Ligne.* (cellules de la ligne répétée).");
                }
                analyse.Champs.Add(new ChampTrouve(nom, emplacement));
                var portee = NomsChamps.Portee(nom);
                if (portee == PorteeChamp.Section) lignesSection.Add(cellule.Address.RowNumber);
                else if (portee is PorteeChamp.Ligne or PorteeChamp.ColonneLigne) lignesDetail.Add(cellule.Address.RowNumber);
            }
            if (noms.Count > 0)
                cellulesChamps.Add((cellule.Address.RowNumber, cellule.Address.ColumnNumber, noms.Any(n => NomsChamps.Portee(n) != PorteeChamp.Document)));
        }

        foreach (var (texte, emplacement) in TextesEnTetesPiedsDePage(feuille))
        {
            foreach (var (nom, _) in LireChamps(texte, emplacement, analyse))
            {
                analyse.Champs.Add(new ChampTrouve(nom, emplacement));
                if (NomsChamps.Portee(nom) != PorteeChamp.Document)
                    analyse.Erreurs.Add($"{emplacement} : le champ {{{{{nom}}}}} n'est possible que dans le tableau répété (une section ou une ligne n'a pas de sens dans l'en-tête ou le pied de page).");
            }
        }

        structure.Bloc = LireBloc(feuille, lignesSection, lignesDetail, analyse);
        if (structure.Bloc != null)
            structure.ColonnesFusion.AddRange(cellulesFusion
                .Where(c => c.Ligne == structure.Bloc.LigneDetail)
                .OrderBy(c => c.Colonne)
                .Select(c => new ColonneFusion(c.Colonne, c.Nom)));
        foreach (var (ligne, colonne, _) in cellulesChamps.Where(c => structure.Bloc == null || c.Ligne < structure.Bloc.Debut || c.Ligne > structure.Bloc.LigneDetail))
            structure.CellulesHorsBloc.Add(new CelluleChamps(ligne, colonne));

        if (!feuille.PageSetup.PrintAreas.Any())
            analyse.Avertissements.Add("Aucune zone d'impression n'est définie : toute la feuille sera imprimée.");
        else if (feuille.PageSetup.PrintAreas.Count() > 1)
            analyse.Erreurs.Add("Le modèle doit avoir une seule zone d'impression.");
        else if (feuille.PageSetup.PrintAreas.First().RangeAddress.LastAddress.ColumnNumber < DerniereColonneRemplie(feuille))
            analyse.Avertissements.Add("La zone d'impression s'arrête avant la dernière colonne remplie : elle sera élargie à toutes les colonnes dans les PDF.");
        return structure;
    }

    /// <summary>Champs d'un texte (nom et option de fusion) ; accolades mal fermées, noms et options invalides sont des erreurs.</summary>
    private static List<(string Nom, bool Fusion)> LireChamps(string texte, string emplacement, AnalyseModele analyse)
    {
        var noms = new List<(string, bool)>();
        foreach (Match correspondance in Champ.Matches(texte))
        {
            var parties = correspondance.Groups[1].Value.Split('|');
            var nom = parties[0].Trim();
            var options = parties.Skip(1).Select(o => o.Trim()).ToList();
            if (!NomValide.IsMatch(nom))
                analyse.Erreurs.Add($"{emplacement} : nom de champ invalide « {correspondance.Value} » (lettres et chiffres, ex. {{{{Date}}}}, {{{{Ligne.Caracteristique}}}}).");
            else if (options.Any(o => o != OptionFusion))
                analyse.Erreurs.Add($"{emplacement} : option inconnue dans « {correspondance.Value} » (seule « |{OptionFusion} » existe).");
            else
                noms.Add((nom, options.Count > 0));
        }
        var reste = Champ.Replace(texte, "");
        if (reste.Contains("{{") || reste.Contains("}}"))
            analyse.Erreurs.Add($"{emplacement} : accolades mal fermées, un champ s'écrit {{{{Nom}}}}.");
        return noms;
    }

    private static Bloc? LireBloc(IXLWorksheet feuille, SortedSet<int> lignesSection, SortedSet<int> lignesDetail, AnalyseModele analyse)
    {
        if (lignesDetail.Count == 0)
        {
            if (lignesSection.Count > 0)
                analyse.Erreurs.Add($"Les champs Section.* (ligne {lignesSection.Min}) demandent, sous le titre de section, une ligne de détail avec les champs Ligne.*.");
            return null;
        }
        if (lignesDetail.Count > 1)
        {
            analyse.Erreurs.Add($"Les champs Ligne.* doivent être sur une seule ligne Excel, la ligne répétée (trouvés lignes {string.Join(", ", lignesDetail)}).");
            return null;
        }
        var ligneDetail = lignesDetail.Min;
        if (lignesSection.Any(l => l > ligneDetail))
        {
            analyse.Erreurs.Add($"Les champs Section.* doivent être au-dessus de la ligne de détail (ligne {ligneDetail}).");
            return null;
        }
        var bloc = new Bloc(lignesSection.Count > 0 ? lignesSection.Min : ligneDetail, ligneDetail);

        foreach (var fusion in feuille.MergedRanges)
        {
            var debut = fusion.RangeAddress.FirstAddress.RowNumber;
            var fin = fusion.RangeAddress.LastAddress.RowNumber;
            var chevauche = debut <= bloc.LigneDetail && fin >= bloc.Debut;
            var contenue = debut >= bloc.Debut && fin <= bloc.LigneDetail;
            if (chevauche && !contenue)
                analyse.Erreurs.Add($"Les cellules fusionnées {fusion.RangeAddress.ToStringRelative()} débordent du bloc répété (lignes {bloc.Debut} à {bloc.LigneDetail}).");
            else if (contenue && debut == bloc.LigneDetail && fin > debut)
                analyse.Erreurs.Add($"La ligne de détail {bloc.LigneDetail} ne peut pas contenir de cellules fusionnées sur plusieurs lignes ({fusion.RangeAddress.ToStringRelative()}).");
        }
        if (feuille.Pictures.Any(p => p.TopLeftCell.Address.RowNumber >= bloc.Debut))
            analyse.Avertissements.Add($"Une image est placée à partir du bloc répété (ligne {bloc.Debut}) : sa position n'est pas garantie quand le tableau s'allonge.");
        return bloc;
    }

    // ── Répétition du bloc ───────────────────────────────────────────────

    /// <summary>Écrit les sections et leurs lignes à la place du bloc ; retourne le nombre de lignes ajoutées (négatif si retirées).</summary>
    private static int RepeterBloc(XLWorkbook classeur, IXLWorksheet feuille, Bloc bloc, DonneesRapport donnees,
        IReadOnlyList<DonneesSectionRapport> sections, int derniereColonne, IReadOnlyList<ColonneFusion> colonnesFusion)
    {
        var tailleTitre = bloc.LigneDetail - bloc.Debut;
        var tailleModele = tailleTitre + 1;
        var hauteurs = Enumerable.Range(bloc.Debut, tailleModele).Select(r => HauteurFixee(feuille, r)).ToArray();

        // Le bloc d'origine est copié à part : ses lignes sont réécrites par la première section.
        var travail = classeur.Worksheets.Add(FeuilleTravail);
        feuille.Range(bloc.Debut, 1, bloc.LigneDetail, derniereColonne).CopyTo(travail.Cell(1, 1));

        var lignesNecessaires = sections.Sum(s => tailleTitre + Math.Max(1, s.Lignes.Count));
        var decalage = lignesNecessaires - tailleModele;
        if (decalage > 0)
            feuille.Row(bloc.LigneDetail).InsertRowsBelow(decalage);
        else if (decalage < 0)
            feuille.Rows(bloc.Debut + lignesNecessaires, bloc.LigneDetail).Delete();

        if (lignesNecessaires > 0)
        {
            var zone = feuille.Range(bloc.Debut, 1, bloc.Debut + lignesNecessaires - 1, derniereColonne);
            foreach (var fusion in feuille.MergedRanges.Where(m => Chevauche(m.RangeAddress, zone.RangeAddress)).ToList())
                fusion.Unmerge();
            zone.Clear();

            var ligne = bloc.Debut;
            foreach (var section in sections)
            {
                if (tailleTitre > 0)
                {
                    Copier(travail, 1, tailleTitre, feuille, ligne, derniereColonne, hauteurs);
                    RemplirLignes(feuille, ligne, tailleTitre, derniereColonne, nom => donnees.Valeur(nom, section, null));
                    ligne += tailleTitre;
                }
                // Une section sans ligne garde une ligne de détail vide : son tableau n'est jamais réduit à l'en-tête.
                IEnumerable<DonneesLigneRapport?> details = section.Lignes.Count > 0 ? section.Lignes : new DonneesLigneRapport?[] { null };
                var premiereLigneDetail = ligne;
                foreach (var detail in details)
                {
                    Copier(travail, tailleModele, 1, feuille, ligne, derniereColonne, hauteurs[tailleTitre..]);
                    RemplirLignes(feuille, ligne, 1, derniereColonne, nom => donnees.Valeur(nom, section, detail));
                    AjusterHauteur(feuille, ligne, derniereColonne);
                    ligne++;
                }
                if (colonnesFusion.Count > 0 && section.Lignes.Count > 1)
                    Fusionner(feuille, premiereLigneDetail, section.Lignes, colonnesFusion);
            }
        }
        travail.Delete();
        return decalage;
    }

    private static void Copier(IXLWorksheet source, int premiereLigneSource, int nombre, IXLWorksheet cible, int ligneCible, int derniereColonne, double?[] hauteurs)
    {
        source.Range(premiereLigneSource, 1, premiereLigneSource + nombre - 1, derniereColonne).CopyTo(cible.Cell(ligneCible, 1));
        for (var i = 0; i < nombre; i++)
        {
            if (hauteurs[i] is { } hauteur) cible.Row(ligneCible + i).Height = hauteur;
            else cible.Row(ligneCible + i).ClearHeight();
        }
    }

    private static void RemplirLignes(IXLWorksheet feuille, int premiereLigne, int nombre, int derniereColonne, Func<string, string> valeur)
    {
        foreach (var cellule in feuille.Range(premiereLigne, 1, premiereLigne + nombre - 1, derniereColonne).CellsUsed(XLCellsUsedOptions.Contents))
            Remplacer(cellule, valeur);
    }

    private static void Remplacer(IXLCell cellule, Func<string, string> valeur)
    {
        if (cellule.HasFormula || cellule.DataType != XLDataType.Text) return;
        var texte = cellule.GetString();
        if (!texte.Contains("{{")) return;
        var resultat = Champ.Replace(texte, m => valeur(NomSansOption(m.Groups[1].Value)));
        cellule.Value = resultat.Length > LongueurMaximaleCellule ? resultat[..LongueurMaximaleCellule] : resultat;
    }

    private static string NomSansOption(string interieur) => interieur.Split('|')[0].Trim();

    /// <summary>
    /// Fusionne verticalement, colonne par colonne de gauche à droite, les lignes consécutives d'une section qui ont
    /// la même clé dans cette colonne et dans toutes les colonnes fusionnées à sa gauche.
    /// </summary>
    private static void Fusionner(IXLWorksheet feuille, int premiereLigne, IReadOnlyList<DonneesLigneRapport> lignes, IReadOnlyList<ColonneFusion> colonnes)
    {
        var cles = lignes.Select((l, i) => colonnes.Select(c => Cle(feuille, premiereLigne + i, c, l)).ToArray()).ToArray();
        var clesFournies = colonnes.Select(c => lignes.All(l => l.Champs.ContainsKey(PrefixeCle + NomsChamps.Decomposer(c.Nom).Nom))).ToArray();
        for (var k = 0; k < colonnes.Count; k++)
        {
            var parents = clesFournies[k] ? new[] { k } : Enumerable.Range(0, k + 1).ToArray();
            var debut = 0;
            while (debut < lignes.Count)
            {
                var fin = debut;
                while (fin + 1 < lignes.Count && cles[debut][k] != "" && parents.All(p => cles[fin + 1][p] == cles[debut][p]))
                    fin++;
                if (fin > debut)
                {
                    var colonne = colonnes[k].Colonne;
                    // Cellule déjà fusionnée en largeur sur sa ligne (ex. B:C) : la fusion verticale couvre toute sa largeur.
                    var derniere = feuille.Cell(premiereLigne + debut, colonne).IsMerged()
                        ? feuille.Cell(premiereLigne + debut, colonne).MergedRange().RangeAddress.LastAddress.ColumnNumber
                        : colonne;
                    for (var r = premiereLigne + debut; r <= premiereLigne + fin; r++)
                    {
                        if (feuille.Cell(r, colonne).IsMerged()) feuille.Cell(r, colonne).MergedRange().Unmerge();
                        if (r > premiereLigne + debut) feuille.Range(r, colonne, r, derniere).Clear(XLClearOptions.Contents);
                    }
                    feuille.Range(premiereLigne + debut, colonne, premiereLigne + fin, derniere).Merge();
                }
                debut = fin + 1;
            }
        }
    }

    /// <summary>Clé de regroupement d'une cellule : « Cle.Nom » des données de la ligne, sinon le texte affiché.</summary>
    private static string Cle(IXLWorksheet feuille, int ligne, ColonneFusion colonne, DonneesLigneRapport donnees)
    {
        var nomCourt = NomsChamps.Decomposer(colonne.Nom).Nom;
        return donnees.Champs.TryGetValue(PrefixeCle + nomCourt, out var cle) ? cle : feuille.Cell(ligne, colonne.Colonne).GetString();
    }

    /// <summary>Hauteur fixée dans le modèle, ou null si la ligne garde la hauteur par défaut (ajustée au texte).</summary>
    private static double? HauteurFixee(IXLWorksheet feuille, int ligne)
    {
        var hauteur = feuille.Row(ligne).Height;
        return Math.Abs(hauteur - feuille.RowHeight) < 0.01 ? null : hauteur;
    }

    private static int DerniereColonne(IXLWorksheet feuille)
    {
        var colonne = feuille.LastColumnUsed(XLCellsUsedOptions.All)?.ColumnNumber() ?? 1;
        foreach (var fusion in feuille.MergedRanges)
            colonne = Math.Max(colonne, fusion.RangeAddress.LastAddress.ColumnNumber);
        return colonne;
    }

    /// <summary>Dernière colonne qui a un texte ou une fusion (la mise en forme seule, ex. une colonne entière colorée, ne compte pas).</summary>
    private static int DerniereColonneRemplie(IXLWorksheet feuille)
    {
        var colonne = feuille.LastColumnUsed(XLCellsUsedOptions.Contents)?.ColumnNumber() ?? 1;
        foreach (var fusion in feuille.MergedRanges)
            colonne = Math.Max(colonne, fusion.RangeAddress.LastAddress.ColumnNumber);
        return colonne;
    }

    private static bool Chevauche(IXLRangeAddress a, IXLRangeAddress b) =>
        a.FirstAddress.RowNumber <= b.LastAddress.RowNumber && a.LastAddress.RowNumber >= b.FirstAddress.RowNumber &&
        a.FirstAddress.ColumnNumber <= b.LastAddress.ColumnNumber && a.LastAddress.ColumnNumber >= b.FirstAddress.ColumnNumber;

    // ── Images ───────────────────────────────────────────────────────────

    private const int MargeImagePixels = 3;
    private const double LargeurChiffrePixels = 7;   // police standard (Calibri 11)

    /// <summary>
    /// Les images « dans la cellule » (Excel 365) deviennent des images flottantes, centrées et ajustées à leur cellule
    /// (cellules fusionnées comprises), proportions conservées : LibreOffice les affiche comme Excel.
    /// </summary>
    private static byte[] Normaliser(byte[] contenu)
    {
        if (!ImagesDansCellules.Presentes(contenu)) return contenu;
        var images = ImagesDansCellules.Lire(contenu);
        using var classeur = Ouvrir(contenu);
        var feuille = classeur.Worksheet(1);
        var existantes = feuille.Pictures.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (adresse, image) in images)
        {
            var cellule = feuille.Cell(adresse);
            var zone = cellule.IsMerged() ? cellule.MergedRange() : cellule.AsRange();
            var largeur = Enumerable.Range(zone.RangeAddress.FirstAddress.ColumnNumber, zone.ColumnCount()).Sum(c => LargeurPixels(feuille.Column(c).Width));
            var hauteur = Enumerable.Range(zone.RangeAddress.FirstAddress.RowNumber, zone.RowCount()).Sum(r => (int)Math.Round(feuille.Row(r).Height * 96 / 72));
            cellule.Clear(XLClearOptions.Contents);

            var dessin = feuille.AddPicture(new MemoryStream(image));
            var echelle = Math.Min((largeur - 2.0 * MargeImagePixels) / dessin.OriginalWidth, (hauteur - 2.0 * MargeImagePixels) / dessin.OriginalHeight);
            var l = Math.Max(1, (int)(dessin.OriginalWidth * echelle));
            var h = Math.Max(1, (int)(dessin.OriginalHeight * echelle));
            dessin.MoveTo(zone.FirstCell(), (largeur - l) / 2, (hauteur - h) / 2);
            dessin.Width = l;
            dessin.Height = h;
        }
        if (TaillesImagesParFeuille(contenu).TryGetValue(feuille.Name, out var taillesOrigine))
            RetablirTaillesImages(feuille, taillesOrigine, existantes);

        using var sortie = new MemoryStream();
        classeur.SaveAs(sortie);
        return sortie.ToArray();
    }

    /// <summary>Largeur en pixels d'une colonne exprimée en caractères (règle d'Excel).</summary>
    private static int LargeurPixels(double caracteres) =>
        (int)Math.Truncate((256 * caracteres + Math.Truncate(128 / LargeurChiffrePixels)) / 256 * LargeurChiffrePixels);

    private const int EmuParPixel = 9525;
    private static readonly XNamespace Dessin = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
    private static readonly XNamespace Graphique = "http://schemas.openxmlformats.org/drawingml/2006/main";

    /// <summary>
    /// ClosedXML réécrit une image ancrée à une cellule à sa taille native : la taille affichée dans le modèle (logo)
    /// est relue dans le fichier d'origine et rétablie.
    /// </summary>
    private static void RetablirTaillesImages(IXLWorksheet feuille, IReadOnlyDictionary<string, (long Largeur, long Hauteur)> tailles,
        ISet<string>? seulement = null)
    {
        foreach (var image in feuille.Pictures.Where(p => seulement == null || seulement.Contains(p.Name)))
        {
            if (!tailles.TryGetValue(image.Name, out var taille)) continue;
            image.Width = (int)Math.Round(taille.Largeur / (double)EmuParPixel);
            image.Height = (int)Math.Round(taille.Hauteur / (double)EmuParPixel);
        }
    }

    private static readonly XNamespace Tableur = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationsDocument = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace RelationsPaquet = "http://schemas.openxmlformats.org/package/2006/relationships";

    /// <summary>Taille affichée (EMU) de chaque image du modèle, par feuille puis par nom d'image.</summary>
    private static Dictionary<string, Dictionary<string, (long Largeur, long Hauteur)>> TaillesImagesParFeuille(byte[] modele)
    {
        var resultat = new Dictionary<string, Dictionary<string, (long, long)>>(StringComparer.Ordinal);
        using var archive = new ZipArchive(new MemoryStream(modele, writable: false), ZipArchiveMode.Read);
        var classeur = ImagesDansCellules.Xml(archive, "xl/workbook.xml");
        var relations = ImagesDansCellules.Xml(archive, "xl/_rels/workbook.xml.rels");
        if (classeur?.Root == null || relations?.Root == null) return resultat;
        foreach (var feuille in classeur.Root.Element(Tableur + "sheets")?.Elements(Tableur + "sheet") ?? Enumerable.Empty<XElement>())
        {
            var id = (string?)feuille.Attribute(RelationsDocument + "id");
            var cible = (string?)relations.Root.Elements(RelationsPaquet + "Relationship").FirstOrDefault(r => (string?)r.Attribute("Id") == id)?.Attribute("Target");
            if (cible == null) continue;
            var chemin = ImagesDansCellules.Resoudre("xl/", cible);
            var dossier = chemin[..(chemin.LastIndexOf('/') + 1)];
            var relationsFeuille = ImagesDansCellules.Xml(archive, $"{dossier}_rels/{chemin[(chemin.LastIndexOf('/') + 1)..]}.rels");
            var dessin = (string?)relationsFeuille?.Root?.Elements(RelationsPaquet + "Relationship")
                .FirstOrDefault(r => ((string?)r.Attribute("Type"))?.EndsWith("/drawing", StringComparison.Ordinal) == true)?.Attribute("Target");
            if (dessin == null) continue;
            var xml = ImagesDansCellules.Xml(archive, ImagesDansCellules.Resoudre(dossier, dessin));
            if (xml != null) resultat[(string)feuille.Attribute("name")!] = TaillesImages(xml);
        }
        return resultat;
    }

    /// <summary>Taille affichée (EMU) de chaque image d'un dessin de feuille, par nom.</summary>
    private static Dictionary<string, (long Largeur, long Hauteur)> TaillesImages(XDocument dessin)
    {
        var tailles = new Dictionary<string, (long, long)>(StringComparer.Ordinal);
        {
            foreach (var ancre in dessin.Root?.Elements() ?? Enumerable.Empty<XElement>())
            {
                var image = ancre.Element(Dessin + "pic");
                var nom = image?.Element(Dessin + "nvPicPr")?.Element(Dessin + "cNvPr")?.Attribute("name")?.Value;
                // Taille de l'ancre (une cellule), sinon celle de la forme de l'image.
                var etendue = ancre.Element(Dessin + "ext")
                              ?? image?.Element(Dessin + "spPr")?.Element(Graphique + "xfrm")?.Element(Graphique + "ext");
                if (nom == null || etendue == null) continue;
                if (long.TryParse(etendue.Attribute("cx")?.Value, out var largeur) && long.TryParse(etendue.Attribute("cy")?.Value, out var hauteur))
                    tailles[nom] = (largeur, hauteur);
            }
        }
        return tailles;
    }

    // ── En-têtes et pieds de page ────────────────────────────────────────

    private static readonly XLHFOccurrence[] Occurrences = { XLHFOccurrence.OddPages, XLHFOccurrence.EvenPages, XLHFOccurrence.FirstPage };

    private static IEnumerable<(IXLHFItem Element, XLHFOccurrence Occurrence, string Emplacement)> ElementsEnTetesPiedsDePage(IXLWorksheet feuille)
    {
        foreach (var (zone, nomZone) in new[] { (feuille.PageSetup.Header, "En-tête"), (feuille.PageSetup.Footer, "Pied de page") })
        foreach (var (element, position) in new[] { (zone.Left, "gauche"), (zone.Center, "centre"), (zone.Right, "droite") })
        foreach (var occurrence in Occurrences)
            yield return (element, occurrence, $"{nomZone} ({position})");
    }

    private static IEnumerable<(string Texte, string Emplacement)> TextesEnTetesPiedsDePage(IXLWorksheet feuille) =>
        ElementsEnTetesPiedsDePage(feuille)
            .Select(e => (Texte: e.Element.GetText(e.Occurrence), e.Emplacement))
            .Where(e => !string.IsNullOrEmpty(e.Texte))
            .DistinctBy(e => (e.Texte, e.Emplacement));

    private static void RemplacerEnTetesPiedsDePage(IXLWorksheet feuille, Func<string, string> valeur)
    {
        foreach (var (element, occurrence, _) in ElementsEnTetesPiedsDePage(feuille).ToList())
        {
            var texte = element.GetText(occurrence);
            if (string.IsNullOrEmpty(texte) || !texte.Contains("{{")) continue;
            // « & » introduit un code de mise en forme dans un en-tête Excel : il est doublé dans les valeurs.
            var resultat = Champ.Replace(texte, m => valeur(m.Groups[1].Value.Trim()).Replace("&", "&&"));
            element.Clear(occurrence);
            element.AddText(resultat, occurrence);
        }
    }
}
