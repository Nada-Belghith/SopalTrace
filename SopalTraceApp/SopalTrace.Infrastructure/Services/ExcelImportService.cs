using SopalTrace.Domain.Constants;
using ClosedXML.Excel;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SopalTrace.Infrastructure.Services.ExcelImport;

namespace SopalTrace.Infrastructure.Services;

public partial class ExcelImportService : IExcelImportService
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IFrequencyParserService _frequencyParserService;

    protected readonly Dictionary<string, Periodicite> _createdPerioCache = new();
    /// <summary>Périodicités déjà résolues pendant l'import ; null = libellé non reconnu (ignoré).</summary>
    protected readonly Dictionary<string, PeriodiciteMachine?> _createdPerioMachineCache = new();
    protected readonly Dictionary<string, PieceReference> _createdPiecesCache = new();
    protected readonly Dictionary<string, RefFamilleCorp> _createdFamiliesCache = new();
    protected readonly Dictionary<string, RefMoyenDetection> _createdMoyensCache = new();
    protected readonly IExcelImportFactory _factory;

    // --- CONFIGURATION DES MOTS-CLÉS (Plus flexible) ---
    protected static readonly string[] CaractKeywords = { "caracteristique", "carac", "cote", "parametre", "defaut", "risque" };
    protected static readonly string[] LimiteKeywords = { "limite", "specification", "specif", "tolerance", "valeur" };
    protected static readonly string[] InstrumentKeywords = { "code", "id", "ref" };
    protected static readonly string[] MoyenKeywords = { "moyen", "instrument", "appareil", "outil", "equipement" };
    protected static readonly string[] TypeKeywords = { "type", "methode" }; 
    protected static readonly string[] ControleKeywords = { "controle", "verif", "detec" };
    protected static readonly string[] ObsKeywords = { "observation", "remarque", "note", "instruction" };
    protected static readonly string[] PerioKeywords = { "period", "frequence", "echeance", "taux" };
    protected static readonly string[] ConformiteKeywords = { "conformit" };
    
    // --- HELPERS NORMALISATION ---
    protected static string NormalizeForSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "").Replace("\n", "").Replace("\r", "");
    }

    protected static bool MatchesAny(string normalizedText, string[] keywords)
    {
        return keywords.Any(k => normalizedText.Contains(k));
    }

    protected class VerifMachineColumnMapping
    {
        public int RisqueCol { get; set; } = 1;
        public int MethodeCol { get; set; } = 2;
        public int PerioCol { get; set; } = 3;
        public int? MoyenDetCol { get; set; } = null;
        public int PieceRefStartCol { get; set; } = 4;
        public int PieceRefEndCol { get; set; } = 4;
        public int? FuiteCol { get; set; } = null;
        public Dictionary<int, string> Familles { get; set; } = new();
        public Dictionary<int, string> CustomCols { get; set; } = new(); // ColumnNumber -> CustomKey
        public Dictionary<string, string> CustomColsDefinition { get; set; } = new(); // NormLabel -> Key
        public int HeaderBottomRow { get; set; } = -1;
    }

    public ExcelImportService(IUnitOfWork unitOfWork, IFrequencyParserService frequencyParserService, IExcelImportFactory factory)
    {
        _unitOfWork = unitOfWork;
        _frequencyParserService = frequencyParserService;
        _factory = factory;
    }

    protected string SafeSubstring(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength);
    }

    protected string FixEncoding(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        // Détection des séquences UTF-8 interprétées comme du Windows-1252 (ex: Ã© -> é)
        if (text.Contains("Ã") || text.Contains("Â"))
        {
            try
            {
                var bytes = System.Text.Encoding.GetEncoding(1252).GetBytes(text);
                var fixedText = System.Text.Encoding.UTF8.GetString(bytes);
                if (!fixedText.Contains("\uFFFD")) return fixedText;
            }
            catch { }
        }
        return text;
    }

    protected string SafeGetCellValue(IXLCell cell)
    {
        try
        {
            var value = cell.GetString().Trim().Replace("\n", " ");
            if (string.IsNullOrWhiteSpace(value) && cell.IsMerged())
            {
                value = cell.MergedRange().FirstCell().GetString().Trim().Replace("\n", " ");
            }
            return string.IsNullOrWhiteSpace(value) ? "" : FixEncoding(value);
        }
        catch { return ""; }
    }

    public async Task<ImportExcelResultDto> ParsePlanExcelAsync(Stream excelStream, string fileName, string? configurationColonnesJson = null)
    {
        PlanLu plan;
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            plan = await LireCsvAsync(excelStream);
        else
            plan = LecteurPlanExcel.Lire(excelStream, LireColonnesConfigurees(configurationColonnesJson));

        return await ConstruireResultatAsync(plan);
    }

    /// <summary>Colonnes personnalisées du formulaire : libellé normalisé → clé.</summary>
    private static Dictionary<string, string> LireColonnesConfigurees(string? configurationColonnesJson)
    {
        var colonnes = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(configurationColonnesJson)) return colonnes;
        try
        {
            var definitions = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(configurationColonnesJson);
            foreach (var col in definitions ?? new())
            {
                var cle = col.GetValueOrDefault("key")?.ToString();
                var libelle = LecteurPlanExcel.CleRecherche(col.GetValueOrDefault("label")?.ToString());
                if (!string.IsNullOrEmpty(cle) && libelle.Length > 0) colonnes[libelle] = cle;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Configuration illisible : les colonnes inconnues reçoivent une clé dérivée de leur libellé.
        }
        return colonnes;
    }

    /// <summary>Transforme le plan lu en résultat d'aperçu, sans rien écrire dans les référentiels.</summary>
    private async Task<ImportExcelResultDto> ConstruireResultatAsync(PlanLu plan)
    {
        var result = new ImportExcelResultDto();
        result.Erreurs.AddRange(plan.Erreurs);
        result.Warnings.AddRange(plan.Avertissements);
        result.Remarques = string.Join("\n", plan.Remarques);

        foreach (var (cle, (libelle, ordre)) in plan.ColonnesPerso)
        {
            result.ColonneDefs.Add(new SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentColonneDefDto
            {
                CleColonne = cle,
                LabelAffiche = libelle,
                TypeValeur = TypesValeurColonne.Texte,
                OrdreAffiche = ordre
            });
        }

        var resolveur = await ResolveurLignesPlan.ChargerAsync(_unitOfWork.DictionnaireQualiteRepository);
        foreach (var sectionLue in plan.Sections)
        {
            if (sectionLue.Lignes.Count == 0)
            {
                result.Warnings.Add($"Ligne {sectionLue.LigneExcel} : section « {sectionLue.Titre} » sans caractéristique, non importée.");
                continue;
            }

            var section = string.IsNullOrWhiteSpace(sectionLue.Titre)
                ? new ImportExcelSectionDto()
                : await ParseSectionHeaderAsync(sectionLue.Titre);
            foreach (var ligne in sectionLue.Lignes)
                section.Lignes.Add(resolveur.Resoudre(ligne, result.Warnings));
            result.Sections.Add(section);
        }

        result.Warnings.AddRange(resolveur.Recapitulatif());
        result.LignesLues = result.Sections.Sum(s => s.Lignes.Count);
        result.Succes = result.Erreurs.Count == 0;
        return result;
    }

    public async Task<ImportExcelSectionDto> ParseSectionHeaderAsync(string text, bool isContinuation = false)
    {
        var section = new ImportExcelSectionDto();
        string cleanText = FixEncoding(text.Replace("\n", " ").Replace("\r", " ").Trim());

        section.Nom = cleanText;

        string parenthesesContent = "";
        string naturePart = cleanText;

        var matches = Regex.Matches(cleanText, @"\((?>[^()]+|\((?<c>)|\)(?<-c>))*(?(c)(?!))\)");

        // Find the first top-level parentheses group that appears at the end of the section title
        // We want to capture the ENTIRE outer group like (Selon FE0591 : ... (A/B) (p/h))
        // NOT just the last nested sub-group like (p/h)
        // Strategy: find the FIRST opening paren whose matching closing paren is at the end of the string
        int firstTopLevelParen = -1;
        for (int i = 0; i < cleanText.Length; i++)
        {
            if (cleanText[i] == '(')
            {
                // Check if this opens a balanced group that ends at or near the end
                int depth = 0;
                int j = i;
                for (; j < cleanText.Length; j++)
                {
                    if (cleanText[j] == '(') depth++;
                    else if (cleanText[j] == ')') { depth--; if (depth == 0) break; }
                }
                // If this top-level group ends at the last char, it's our candidate
                if (depth == 0 && j == cleanText.Length - 1)
                {
                    firstTopLevelParen = i;
                    break;
                }
            }
        }

        if (firstTopLevelParen >= 0)
        {
            var outerGroup = cleanText.Substring(firstTopLevelParen);
            parenthesesContent = outerGroup.Substring(1, outerGroup.Length - 2).Trim();
            naturePart = cleanText.Substring(0, firstTopLevelParen).Trim();
        }
        else if (matches.Count > 0)
        {
            var lastMatch = matches[^1];
            parenthesesContent = lastMatch.Value.Substring(1, lastMatch.Value.Length - 2).Trim();
            naturePart = cleanText.Replace(lastMatch.Value, "").Trim();
        }
        else
        {
            var freqMatch = Regex.Match(cleanText, @"\b((?:100\s*%|\d+\s*(?:pièces?|pieces?|p/h|p/|%|échantillons?|echantillons?)).*)$", RegexOptions.IgnoreCase);
            if (freqMatch.Success)
            {
                parenthesesContent = freqMatch.Value.Trim();
                naturePart = cleanText.Substring(0, freqMatch.Index).Trim();
            }
        }

        string natureSearch = Regex.Replace(naturePart, @"^SEC\s*\d+\s*[-:]*", "", RegexOptions.IgnoreCase).Trim();
        natureSearch = Regex.Replace(natureSearch, @"^Caractéristiques\s*à\s*contrôler\s*", "", RegexOptions.IgnoreCase).Trim();
        natureSearch = Regex.Replace(natureSearch, @"^Contrôle\s*", "", RegexOptions.IgnoreCase).Trim();
        natureSearch = Regex.Replace(natureSearch, @"^Section\s*", "", RegexOptions.IgnoreCase).Trim();
        natureSearch = natureSearch.Trim('-', ' ', ':', '.');

        var allTypeSections = await _unitOfWork.DictionnaireQualiteRepository.GetAllTypeSectionsAsync();
        var normalizedSearch = NormalizeForSearch(natureSearch);
        var normalizedFull = NormalizeForSearch(naturePart);

        var typeSec = allTypeSections.FirstOrDefault(t => NormalizeForSearch(t.Libelle) == normalizedSearch);
        if (typeSec == null)
            typeSec = allTypeSections.FirstOrDefault(t => NormalizeForSearch(t.Libelle) == normalizedFull);
        if (typeSec == null)
            typeSec = allTypeSections.FirstOrDefault(t => normalizedSearch.Contains(NormalizeForSearch(t.Libelle)));

        string finalNatureLib = natureSearch;
        if (typeSec != null) section.TypeSectionId = typeSec.Id;

        string prefix = (finalNatureLib.ToLower().Contains("réglage") || finalNatureLib.ToLower().Contains("réglages")) 
                        ? "" : "Caractéristiques à contrôler ";
        
        section.LibelleSection = $"{prefix}{finalNatureLib}".Trim();
        
        // Nettoyer le Nom (pour l'UI) en ne gardant que la partie nature, et si c'est identique au type de section, on le vide pour éviter la redondance
        section.Nom = naturePart;
        if (typeSec != null && NormalizeForSearch(section.Nom).Contains(NormalizeForSearch(typeSec.Libelle)))
        {
            section.Nom = ""; // L'UI utilisera le typeSec.Libelle
        }

        if (!string.IsNullOrEmpty(parenthesesContent))
            await _frequencyParserService.ParseFrequencyAsync(section, parenthesesContent);

        return section;
    }

    private static List<string> ParseCsvLine(string line, char separator)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"') inQuotes = !inQuotes;
            else if (c == separator && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else currentField += c;
        }
        result.Add(currentField);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].StartsWith("\"") && result[i].EndsWith("\"") && result[i].Length >= 2)
                result[i] = result[i].Substring(1, result[i].Length - 2).Replace("\"\"", "\"");
            result[i] = result[i].Trim();
        }
        return result;
    }

    /// <summary>
    /// CSV « une ligne = une caractéristique » : Caractéristique;Limite;Type;Moyen;Instrument;…;Observations.
    /// Une ligne sans limite ni type est un titre de section.
    /// </summary>
    private static async Task<PlanLu> LireCsvAsync(Stream stream)
    {
        var plan = new PlanLu();
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        SectionLue? section = null;
        var numero = 0;

        while (await reader.ReadLineAsync() is { } line)
        {
            numero++;
            if (numero > LecteurPlanExcel.NombreMaximalDeLignes)
            {
                plan.Erreurs.Add($"Le fichier dépasse {LecteurPlanExcel.NombreMaximalDeLignes} lignes.");
                break;
            }
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsvLine(line, line.Contains(';') ? ';' : ',')
                .Select(LecteurPlanExcel.CorrigerEncodage).ToList();
            string Col(int i) => i < parts.Count ? parts[i] : string.Empty;

            var colA = Col(0);
            if (string.IsNullOrEmpty(colA)) continue;
            if (LecteurPlanExcel.CleRecherche(colA).StartsWith("caracteristique")) continue;

            if (string.IsNullOrEmpty(Col(1)) && string.IsNullOrEmpty(Col(2)))
            {
                if (colA.StartsWith("(") && section != null) section.Titre = $"{section.Titre} {colA}";
                else if (!colA.Contains('(') && section != null) plan.Remarques.Add(colA);
                else
                {
                    section = plan.Sections.FirstOrDefault(s => s.Titre.Equals(colA, StringComparison.OrdinalIgnoreCase));
                    if (section == null)
                    {
                        section = new SectionLue { Titre = colA, LigneExcel = numero };
                        plan.Sections.Add(section);
                    }
                }
                continue;
            }

            if (section == null)
            {
                plan.Avertissements.Add($"Ligne {numero} : caractéristique avant tout titre de section, ignorée.");
                continue;
            }

            section.Lignes.Add(new LigneLue
            {
                LigneExcel = numero,
                DerniereLigneExcel = numero,
                Caracteristique = colA,
                Limite = Col(1),
                TypeControle = Col(2),
                Moyen = Col(3),
                Instrument = Col(4),
                Observations = string.Join("\n", new[] { Col(6), Col(7), Col(8) }.Where(o => o.Length > 0))
            });
        }

        if (plan.Erreurs.Count == 0 && plan.Sections.All(s => s.Lignes.Count == 0))
            plan.Erreurs.Add("Aucune caractéristique trouvée dans le fichier CSV.");
        return plan;
    }
}
