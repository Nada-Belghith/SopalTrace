using ClosedXML.Excel;
using SopalTrace.Application.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

/// <summary>Champ d'une ligne de plan reconnu dans une ligne d'en-tête.</summary>
public enum ChampPlan { Caracteristique, Limite, TypeControle, Moyen, Instrument, Observations }

/// <summary>Contenu brut d'un plan lu dans Excel, avant tout rapprochement avec les référentiels.</summary>
public sealed class PlanLu
{
    public List<SectionLue> Sections { get; } = new();
    public List<string> Remarques { get; } = new();
    public List<string> Erreurs { get; } = new();
    public List<string> Avertissements { get; } = new();
    /// <summary>Colonnes hors standard rencontrées dans les en-têtes : clé → libellé.</summary>
    public Dictionary<string, (string Libelle, int Ordre)> ColonnesPerso { get; } = new();
}

public sealed class SectionLue
{
    public string Titre { get; set; } = string.Empty;
    public int LigneExcel { get; init; }
    public List<LigneLue> Lignes { get; } = new();
}

public sealed class LigneLue
{
    public int LigneExcel { get; init; }
    /// <summary>Dernière ligne Excel couverte (fusion verticale, lignes de continuation).</summary>
    public int DerniereLigneExcel { get; set; }
    public string Caracteristique { get; set; } = string.Empty;
    public string Limite { get; set; } = string.Empty;
    public string TypeControle { get; set; } = string.Empty;
    public string Moyen { get; set; } = string.Empty;
    public string Instrument { get; set; } = string.Empty;
    public string Observations { get; set; } = string.Empty;
    public Dictionary<string, string> ColonnesPerso { get; } = new();
    public string? Image { get; set; }
}

/// <summary>
/// Lit un plan de contrôle Excel (plans de fabrication, documents produit fini et assemblage) sans
/// accès à la base. Gère les variantes rencontrées dans les fichiers réels :
/// <list type="bullet">
/// <item>titre de section fusionné sur toute la largeur, éventuellement sur deux lignes, suivi de sa ligne d'en-tête ;</item>
/// <item>en-tête répété à chaque section, colonnes décalées ou fusionnées (B:C), retours à la ligne, fautes de frappe ;</item>
/// <item>caractéristique fusionnée sur plusieurs lignes, valeurs fusionnées partagées par plusieurs caractéristiques ;</item>
/// <item>lignes de continuation sans caractéristique (« (FA0974) » sous la limite, suite d'observation) ;</item>
/// <item>croquis ancrés dans les cellules, note finale (« NB : … »).</item>
/// </list>
/// Chaque anomalie est signalée avec son numéro de ligne Excel au lieu d'être ignorée en silence.
/// </summary>
public static class LecteurPlanExcel
{
    public const int NombreMaximalDeLignes = 5000;
    private const int LargeurMinimaleTitre = 3;

    private static readonly Regex DebutNote = new(@"^\s*(n\.?\s?b\b|nota\b|remarque\s*:)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static PlanLu Lire(Stream flux, IReadOnlyDictionary<string, string>? colonnesConfigurees = null)
    {
        using var classeur = new XLWorkbook(flux);
        var feuille = classeur.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("Le fichier Excel ne contient aucune feuille.");
        return Lire(feuille, colonnesConfigurees);
    }

    public static PlanLu Lire(IXLWorksheet feuille, IReadOnlyDictionary<string, string>? colonnesConfigurees = null)
    {
        var plan = new PlanLu();
        var grille = new Grille(feuille);

        if (grille.DerniereLigne == 0)
        {
            plan.Erreurs.Add("La feuille est vide.");
            return plan;
        }
        if (grille.DerniereLigne > NombreMaximalDeLignes)
        {
            plan.Erreurs.Add($"La feuille compte {grille.DerniereLigne} lignes : au-delà de {NombreMaximalDeLignes}, ce n'est pas un plan de contrôle.");
            return plan;
        }

        var lecture = new Lecture(plan, grille, colonnesConfigurees ?? new Dictionary<string, string>());
        lecture.Parcourir();
        lecture.RattacherImages(feuille);

        if (!lecture.EnTeteTrouve)
            plan.Erreurs.Add("Aucune ligne d'en-tête reconnue. Attendu au minimum : « Caractéristique contrôlée » et deux colonnes parmi « Limite de spécification », « Type de contrôle », « Moyen de contrôle », « Code instrument », « Observations ».");
        else if (plan.Sections.All(s => s.Lignes.Count == 0))
            plan.Erreurs.Add("Aucune caractéristique trouvée sous les en-têtes.");

        return plan;
    }

    /// <summary>Accès aux cellules en tenant compte des fusions.</summary>
    private sealed class Grille
    {
        private readonly IXLWorksheet _feuille;
        private readonly Dictionary<(int, int), IXLRangeAddress> _fusions = new();

        public int DerniereLigne { get; }
        public int DerniereColonne { get; }

        public Grille(IXLWorksheet feuille)
        {
            _feuille = feuille;
            foreach (var plage in feuille.MergedRanges)
            {
                var a = plage.RangeAddress;
                for (var l = a.FirstAddress.RowNumber; l <= a.LastAddress.RowNumber; l++)
                    for (var c = a.FirstAddress.ColumnNumber; c <= a.LastAddress.ColumnNumber; c++)
                        _fusions[(l, c)] = a;
            }

            var derniere = feuille.LastCellUsed(XLCellsUsedOptions.Contents);
            DerniereLigne = derniere == null ? 0 : feuille.LastRowUsed(XLCellsUsedOptions.Contents)!.RowNumber();
            DerniereColonne = derniere == null ? 0 : feuille.LastColumnUsed(XLCellsUsedOptions.Contents)!.ColumnNumber();
        }

        private string Brut(int ligne, int colonne)
        {
            try { return CorrigerEncodage(_feuille.Cell(ligne, colonne).GetFormattedString()).Trim(); }
            catch { return string.Empty; }
        }

        /// <summary>Valeur visible : celle de la cellule, ou de la cellule d'origine si elle est fusionnée.</summary>
        public string Valeur(int ligne, int colonne)
        {
            if (_fusions.TryGetValue((ligne, colonne), out var a))
                return Brut(a.FirstAddress.RowNumber, a.FirstAddress.ColumnNumber);
            return Brut(ligne, colonne);
        }

        /// <summary>Valeur propre : vide si la cellule est recouverte par une fusion commencée ailleurs.</summary>
        public string ValeurPropre(int ligne, int colonne)
            => EstRecouverte(ligne, colonne) ? string.Empty : Brut(ligne, colonne);

        public bool EstRecouverte(int ligne, int colonne)
            => _fusions.TryGetValue((ligne, colonne), out var a)
               && (a.FirstAddress.RowNumber != ligne || a.FirstAddress.ColumnNumber != colonne);

        /// <summary>Recouverte par une fusion commencée sur une ligne précédente.</summary>
        public bool EstRecouverteParLeHaut(int ligne, int colonne)
            => _fusions.TryGetValue((ligne, colonne), out var a) && a.FirstAddress.RowNumber < ligne;

        public int LargeurFusion(int ligne, int colonne)
            => _fusions.TryGetValue((ligne, colonne), out var a)
                ? a.LastAddress.ColumnNumber - a.FirstAddress.ColumnNumber + 1
                : 1;

        public int DerniereLigneFusion(int ligne, int colonne)
            => _fusions.TryGetValue((ligne, colonne), out var a) ? a.LastAddress.RowNumber : ligne;

        /// <summary>Cellules ayant une valeur propre sur la ligne.</summary>
        public List<(int Colonne, string Texte)> CellulesPropres(int ligne)
        {
            var cellules = new List<(int, string)>();
            for (var c = 1; c <= DerniereColonne; c++)
            {
                var texte = ValeurPropre(ligne, c);
                if (texte.Length > 0) cellules.Add((c, texte));
            }
            return cellules;
        }
    }

    private sealed class Lecture
    {
        private readonly PlanLu _plan;
        private readonly Grille _grille;
        private readonly IReadOnlyDictionary<string, string> _colonnesConfigurees;

        private Dictionary<ChampPlan, int>? _colonnes;
        private List<int> _colonnesObservations = new();
        private Dictionary<int, string> _colonnesPerso = new();
        private SectionLue? _section;
        private bool _enTeteDeSectionAttendu;
        private LigneLue? _ligne;
        private int _lignesAvantPremiereSection;

        public bool EnTeteTrouve { get; private set; }

        public Lecture(PlanLu plan, Grille grille, IReadOnlyDictionary<string, string> colonnesConfigurees)
        {
            _plan = plan;
            _grille = grille;
            _colonnesConfigurees = colonnesConfigurees;
        }

        public void Parcourir()
        {
            for (var l = 1; l <= _grille.DerniereLigne; l++)
            {
                var cellules = _grille.CellulesPropres(l);
                if (cellules.Count == 0) continue;

                if (EssayerEnTete(l, cellules)) continue;

                if (cellules.Count == 1 && EstTitreOuNote(l, cellules[0])) continue;

                if (_colonnes == null || _section == null)
                {
                    _lignesAvantPremiereSection++;
                    continue;
                }

                LireLigneDeDonnees(l);
            }

            if (_lignesAvantPremiereSection > 0)
                _plan.Avertissements.Add($"{_lignesAvantPremiereSection} ligne(s) situées avant le premier en-tête de tableau ont été ignorées (cartouche, titre du document…).");
        }

        // ---------- En-têtes ----------

        private bool EssayerEnTete(int ligne, List<(int Colonne, string Texte)> cellules)
        {
            if (cellules.Count < 3) return false;

            var champs = new Dictionary<ChampPlan, int>();
            var observations = new List<int>();
            var autres = new List<(int Colonne, string Texte)>();

            foreach (var (colonne, texte) in cellules)
            {
                var champ = Categoriser(texte);
                if (champ == null) { autres.Add((colonne, texte)); continue; }
                if (champ == ChampPlan.Observations) { observations.Add(colonne); continue; }
                if (!champs.ContainsKey(champ.Value)) champs[champ.Value] = colonne;
                else autres.Add((colonne, texte));
            }
            if (observations.Count > 0) champs[ChampPlan.Observations] = observations[0];

            if (!champs.ContainsKey(ChampPlan.Caracteristique) || champs.Count < 3) return false;

            EnTeteTrouve = true;
            _colonnes = champs;
            _colonnesObservations = observations;
            _colonnesPerso = new Dictionary<int, string>();
            foreach (var (colonne, texte) in autres)
            {
                var libelle = TexteReference.SurUneLigne(texte);
                var cle = _colonnesConfigurees.TryGetValue(CleRecherche(libelle), out var configuree)
                    ? configuree
                    : CleColonneHelper.Normalize(libelle);
                if (string.IsNullOrEmpty(cle)) continue;
                _colonnesPerso[colonne] = cle;
                if (!_plan.ColonnesPerso.ContainsKey(cle))
                    _plan.ColonnesPerso[cle] = (TexteReference.Tronquer(libelle, 200), colonne);
            }

            // En-tête répété sans nouveau titre (saut de page) : la section en cours continue.
            if (_section == null)
            {
                _plan.Avertissements.Add($"Ligne {ligne} : tableau sans titre de section au-dessus ; une section sans titre a été créée, à nommer dans l'éditeur.");
                OuvrirSection(string.Empty, ligne);
            }

            _enTeteDeSectionAttendu = false;
            _ligne = null;
            return true;
        }

        private static ChampPlan? Categoriser(string texte)
        {
            var cle = CleRecherche(texte);
            if (cle.Length == 0) return null;
            if (cle.Contains("caracteristique") || cle.Contains("carac") || cle.Contains("parametre") || cle.Contains("critere"))
                return ChampPlan.Caracteristique;
            if (cle.Contains("limite") || cle.Contains("specif") || cle.Contains("tolerance"))
                return ChampPlan.Limite;
            if (cle.Contains("code") || (cle.Contains("instrument") && !cle.Contains("moyen")))
                return ChampPlan.Instrument;
            if (cle.Contains("moyen") || cle.Contains("appareil") || cle.Contains("outil") || cle.Contains("equipement"))
                return ChampPlan.Moyen;
            if (cle.Contains("type") || cle.Contains("methode") || cle.Contains("natureducontrole"))
                return ChampPlan.TypeControle;
            if (cle.Contains("observation") || cle.Contains("remarque") || cle.Contains("commentaire") || cle.Contains("instruction"))
                return ChampPlan.Observations;
            return null;
        }

        // ---------- Titres et notes ----------

        private bool EstTitreOuNote(int ligne, (int Colonne, string Texte) cellule)
        {
            var texte = TexteReference.SurUneLigne(cellule.Texte);
            var large = _grille.LargeurFusion(ligne, cellule.Colonne) >= LargeurMinimaleTitre;

            if (DebutNote.IsMatch(texte))
            {
                AjouterRemarque(texte);
                return true;
            }

            // Titre sur deux lignes : la seconde ligne complète le titre en attente de son en-tête.
            if (large && _enTeteDeSectionAttendu && _section != null)
            {
                _section.Titre = $"{_section.Titre} {texte}".Trim();
                return true;
            }

            var suivieDUnEnTete = ProchaineLigneEstEnTete(ligne);
            if (suivieDUnEnTete && (large || _colonnes == null || cellule.Colonne == (_colonnes.GetValueOrDefault(ChampPlan.Caracteristique, 1))))
            {
                OuvrirSection(texte, ligne);
                _enTeteDeSectionAttendu = true;
                return true;
            }

            if (large)
            {
                AjouterRemarque(texte);
                return true;
            }

            // Texte isolé en fin de feuille, hors de toute colonne de valeur : note de bas de page.
            if (_colonnes != null && EstDerniereLigneRemplie(ligne) && texte.Length > 60)
            {
                AjouterRemarque(texte);
                return true;
            }
            return false;
        }

        private bool ProchaineLigneEstEnTete(int ligne)
        {
            for (var l = ligne + 1; l <= Math.Min(_grille.DerniereLigne, ligne + 3); l++)
            {
                var cellules = _grille.CellulesPropres(l);
                if (cellules.Count == 0) continue;
                if (cellules.Count < 3) return false;
                var champs = cellules.Select(c => Categoriser(c.Texte)).Where(c => c != null).Distinct().ToList();
                return champs.Contains(ChampPlan.Caracteristique) && champs.Count >= 3;
            }
            return false;
        }

        private bool EstDerniereLigneRemplie(int ligne)
        {
            for (var l = ligne + 1; l <= _grille.DerniereLigne; l++)
                if (_grille.CellulesPropres(l).Count > 0) return false;
            return true;
        }

        private void AjouterRemarque(string texte)
        {
            if (texte.Length > 0) _plan.Remarques.Add(texte);
        }

        private void OuvrirSection(string titre, int ligne)
        {
            _section = new SectionLue { Titre = titre, LigneExcel = ligne };
            _plan.Sections.Add(_section);
            _ligne = null;
        }

        // ---------- Lignes de données ----------

        private void LireLigneDeDonnees(int ligne)
        {
            var colCarac = _colonnes![ChampPlan.Caracteristique];

            if (_grille.ValeurPropre(ligne, colCarac).Length > 0)
            {
                // Nouvelle caractéristique : les valeurs fusionnées venant du haut sont partagées.
                _ligne = new LigneLue
                {
                    LigneExcel = ligne,
                    DerniereLigneExcel = _grille.DerniereLigneFusion(ligne, colCarac),
                    Caracteristique = TexteReference.SurUneLigne(_grille.Valeur(ligne, colCarac)),
                    Limite = TexteReference.SurUneLigne(Lire(ligne, ChampPlan.Limite, propre: false)),
                    TypeControle = TexteReference.SurUneLigne(Lire(ligne, ChampPlan.TypeControle, propre: false)),
                    Moyen = TexteReference.SurUneLigne(Lire(ligne, ChampPlan.Moyen, propre: false)),
                    Instrument = TexteReference.SurUneLigne(Lire(ligne, ChampPlan.Instrument, propre: false)),
                    Observations = LireObservations(ligne, propre: false)
                };
                foreach (var (colonne, cle) in _colonnesPerso)
                {
                    var valeur = TexteReference.SurUneLigne(_grille.Valeur(ligne, colonne));
                    if (valeur.Length > 0) _ligne.ColonnesPerso[cle] = valeur;
                }
                _section!.Lignes.Add(_ligne);
                return;
            }

            // Ligne sans caractéristique propre : suite de la caractéristique précédente
            // (fusion verticale ou complément saisi sur la ligne du dessous).
            if (_ligne == null)
            {
                _plan.Avertissements.Add($"Ligne {ligne} : valeurs sans caractéristique ni ligne précédente à compléter, ignorées.");
                return;
            }

            _ligne.DerniereLigneExcel = Math.Max(_ligne.DerniereLigneExcel, ligne);
            _ligne.Limite = Joindre(_ligne.Limite, TexteReference.SurUneLigne(Lire(ligne, ChampPlan.Limite, propre: true)), " ");
            _ligne.TypeControle = Joindre(_ligne.TypeControle, TexteReference.SurUneLigne(Lire(ligne, ChampPlan.TypeControle, propre: true)), " ");
            _ligne.Moyen = Joindre(_ligne.Moyen, TexteReference.SurUneLigne(Lire(ligne, ChampPlan.Moyen, propre: true)), " ");
            _ligne.Instrument = Joindre(_ligne.Instrument, TexteReference.SurUneLigne(Lire(ligne, ChampPlan.Instrument, propre: true)), " ");
            _ligne.Observations = Joindre(_ligne.Observations, LireObservations(ligne, propre: true), "\n");
            foreach (var (colonne, cle) in _colonnesPerso)
            {
                var valeur = TexteReference.SurUneLigne(_grille.ValeurPropre(ligne, colonne));
                if (valeur.Length > 0)
                    _ligne.ColonnesPerso[cle] = Joindre(_ligne.ColonnesPerso.GetValueOrDefault(cle, string.Empty), valeur, " ");
            }
        }

        private string Lire(int ligne, ChampPlan champ, bool propre)
        {
            if (!_colonnes!.TryGetValue(champ, out var colonne)) return string.Empty;
            return propre ? _grille.ValeurPropre(ligne, colonne) : _grille.Valeur(ligne, colonne);
        }

        private string LireObservations(int ligne, bool propre)
        {
            var textes = _colonnesObservations
                .Select(c => propre ? _grille.ValeurPropre(ligne, c) : _grille.Valeur(ligne, c))
                .Select(NettoyerTexteMultiligne)
                .Where(t => t.Length > 0)
                .Distinct();
            return string.Join("\n", textes);
        }

        private static string Joindre(string actuel, string ajout, string separateur)
        {
            if (ajout.Length == 0) return actuel;
            if (actuel.Length == 0) return ajout;
            return actuel + separateur + ajout;
        }

        // ---------- Croquis ----------

        public void RattacherImages(IXLWorksheet feuille)
        {
            var ignorees = new List<int>();
            var refusees = new List<int>();
            var enTrop = new List<int>();

            foreach (var image in feuille.Pictures)
            {
                int ligneAncre;
                string dataUrl;
                try
                {
                    ligneAncre = image.TopLeftCell.Address.RowNumber;
                    using var ms = new MemoryStream();
                    image.ImageStream.Position = 0;
                    image.ImageStream.CopyTo(ms);
                    var format = image.Format.ToString().ToLowerInvariant();
                    dataUrl = $"data:image/{(format == "jpg" ? "jpeg" : format)};base64,{Convert.ToBase64String(ms.ToArray())}";
                }
                catch
                {
                    continue;
                }

                var cible = _plan.Sections.SelectMany(s => s.Lignes)
                    .FirstOrDefault(l => ligneAncre >= l.LigneExcel && ligneAncre <= l.DerniereLigneExcel);
                if (cible == null) { ignorees.Add(ligneAncre); continue; }
                if (!ImagesLigne.EstAcceptee(dataUrl)) { refusees.Add(ligneAncre); continue; }
                if (cible.Image != null) { enTrop.Add(ligneAncre); continue; }
                cible.Image = dataUrl;
            }

            if (ignorees.Count > 0)
                _plan.Avertissements.Add($"Croquis ancré(s) hors d'une caractéristique (ligne(s) {Lister(ignorees)}) : non importé(s), à ajouter dans l'éditeur.");
            if (refusees.Count > 0)
                _plan.Avertissements.Add($"Croquis refusé(s) (format autre que PNG/JPEG/GIF ou taille supérieure à 2 Mo) ligne(s) {Lister(refusees)}.");
            if (enTrop.Count > 0)
                _plan.Avertissements.Add($"Une seule image par caractéristique : croquis supplémentaire(s) ignoré(s) ligne(s) {Lister(enTrop)}.");
        }

        private static string Lister(IEnumerable<int> lignes) => string.Join(", ", lignes.Distinct().OrderBy(l => l));
    }

    // ---------- Texte ----------

    /// <summary>Clé de comparaison d'un libellé d'en-tête : sans accents, espaces, ponctuation.</summary>
    public static string CleRecherche(string? texte)
        => Regex.Replace(TexteReference.Cle(texte), @"[^a-z0-9%]", "");

    private static string NettoyerTexteMultiligne(string texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return string.Empty;
        var lignes = texte.Replace("\r", "").Split('\n')
            .Select(TexteReference.SurUneLigne)
            .Where(l => l.Length > 0);
        return string.Join("\n", lignes);
    }

    /// <summary>Répare un texte UTF-8 lu comme du Windows-1252 (« Ã© » → « é »).</summary>
    public static string CorrigerEncodage(string texte)
    {
        if (string.IsNullOrEmpty(texte) || (!texte.Contains('Ã') && !texte.Contains('Â'))) return texte;
        try
        {
            var corrige = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.Latin1.GetBytes(texte));
            return corrige.Contains('�') ? texte : corrige;
        }
        catch
        {
            return texte;
        }
    }
}
