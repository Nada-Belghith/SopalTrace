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

    protected class PlanColumnMapping
    {
        public bool IsInitialized { get; set; } = false;
        public int CaractCol { get; set; } = 1;
        public int LimiteCol { get; set; } = 2;
        public int TypeCol { get; set; } = 3;
        public int MoyenCol { get; set; } = 4;
        public int InstCol { get; set; } = 5;
        public List<int> ObsCols { get; set; } = new List<int>();
        public Dictionary<int, string> CustomCols { get; set; } = new(); // ColumnNumber -> CustomKey
        public Dictionary<string, string> CustomColsDefinition { get; set; } = new(); // NormLabel -> Key
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
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return await ParseCsvAsync(excelStream);

        var result = new ImportExcelResultDto();
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new Exception("Le fichier Excel est vide ou invalide.");

        var imagesByRow = new Dictionary<int, string>();
        foreach (var pic in worksheet.Pictures)
        {
            try 
            {
                var r = pic.TopLeftCell.Address.RowNumber;
                using var ms = new MemoryStream();
                pic.ImageStream.CopyTo(ms);
                var format = pic.Format.ToString().ToLower();
                if (format == "jpeg") format = "jpg"; // Optional standardizing
                imagesByRow[r] = $"data:image/{format};base64,{Convert.ToBase64String(ms.ToArray())}";
            } catch { }
        }

        var rows = worksheet.RowsUsed().ToList();
        if (!rows.Any()) return result;

        var map = new PlanColumnMapping();
        
        var customColsDefinition = new Dictionary<string, string>(); // NormLabel -> Key
        if (!string.IsNullOrWhiteSpace(configurationColonnesJson))
        {
            try
            {
                var customCols = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(configurationColonnesJson);
                if (customCols != null)
                {
                    foreach (var col in customCols)
                    {
                        if (col.TryGetValue("key", out var keyObj) && col.TryGetValue("label", out var labelObj))
                        {
                            var key = keyObj?.ToString();
                            var label = NormalizeForSearch(labelObj?.ToString() ?? "");
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(label))
                            {
                                customColsDefinition[label] = key;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        ImportExcelSectionDto? currentSection = null;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            bool isHorizontalMerge = row.Cell(1).IsMerged() && row.Cell(1).MergedRange().ColumnCount() > 1;
            string colA_Raw = SafeGetCellValue(row.Cell(1));

            if (isHorizontalMerge && !string.IsNullOrWhiteSpace(colA_Raw))
            {
                if (colA_Raw.Trim().StartsWith("(") && currentSection != null)
                {
                    var tempSection = await ParseSectionHeaderAsync(colA_Raw, true);
                    if (tempSection.ModeFreq != "SANS" || !string.IsNullOrEmpty(tempSection.FrequenceLibelle))
                    {
                        currentSection.FrequenceLibelle = tempSection.FrequenceLibelle;
                        currentSection.ModeFreq = tempSection.ModeFreq;
                        currentSection.TypeVariable = tempSection.TypeVariable;
                        currentSection.FreqNum = tempSection.FreqNum;
                        currentSection.FreqHours = tempSection.FreqHours;
                        currentSection.RegleEchantillonnageId = tempSection.RegleEchantillonnageId;
                    }
                    else
                    {
                        currentSection.Nom += " " + colA_Raw;
                        var typeSec = await _unitOfWork.DictionnaireQualiteRepository.GetTypeSectionByLibelleAsync(currentSection.Nom.Trim());
                        if (typeSec != null) currentSection.TypeSectionId = typeSec.Id;
                    }
                    continue;
                }

                bool hasDataRowsAfter = false;
                for (int j = i + 1; j < Math.Min(i + 20, rows.Count); j++)
                {
                    var nextRow = rows[j];
                    if (nextRow.CellsUsed().Any(c => c.GetString().Contains("Limite", StringComparison.OrdinalIgnoreCase) || c.GetString().Contains("Type", StringComparison.OrdinalIgnoreCase)))
                    {
                        hasDataRowsAfter = true;
                        break;
                    }
                    string nextA = SafeGetCellValue(nextRow.Cell(1));
                    string nextB = SafeGetCellValue(nextRow.Cell(2));
                    if (!nextRow.Cell(1).IsMerged() && !string.IsNullOrEmpty(nextA) && !string.IsNullOrEmpty(nextB))
                    {
                        hasDataRowsAfter = true;
                        break;
                    }
                }

                if (hasDataRowsAfter)
                {
                    currentSection = await ParseSectionHeaderAsync(colA_Raw, false);
                    result.Sections.Add(currentSection);
                    continue;
                }
                else
                {
                    result.Remarques += colA_Raw + "\n";
                    continue;
                }
            }

            // DÉTECTION D'EN-TÊTE DE COLONNE (PLUS STRICTE)
            int matchCount = 0;
            if (row.CellsUsed().Any(c => MatchesAny(NormalizeForSearch(SafeGetCellValue(c)), CaractKeywords))) matchCount++;
            if (row.CellsUsed().Any(c => MatchesAny(NormalizeForSearch(SafeGetCellValue(c)), LimiteKeywords))) matchCount++;
            if (row.CellsUsed().Any(c => MatchesAny(NormalizeForSearch(SafeGetCellValue(c)), TypeKeywords))) matchCount++;
            if (row.CellsUsed().Any(c => MatchesAny(NormalizeForSearch(SafeGetCellValue(c)), ControleKeywords))) matchCount++;
            if (row.CellsUsed().Any(c => MatchesAny(NormalizeForSearch(SafeGetCellValue(c)), MoyenKeywords))) matchCount++;

            bool isHeaderRow = matchCount >= 3;

            if (isHeaderRow)
            {
                map = new PlanColumnMapping();
                map.CustomColsDefinition = customColsDefinition;
                foreach (var cell in row.CellsUsed())
                {
                    string val = NormalizeForSearch(SafeGetCellValue(cell));
                    int cIdx = cell.Address.ColumnNumber;

                    if (MatchesAny(val, CaractKeywords)) map.CaractCol = cIdx;
                    else if (MatchesAny(val, LimiteKeywords)) map.LimiteCol = cIdx;
                    else if (MatchesAny(val, InstrumentKeywords)) map.InstCol = cIdx;
                    else if (MatchesAny(val, MoyenKeywords)) map.MoyenCol = cIdx;
                    else if (MatchesAny(val, TypeKeywords) || (MatchesAny(val, ControleKeywords) && !MatchesAny(val, MoyenKeywords))) map.TypeCol = cIdx;
                    else if (MatchesAny(val, ObsKeywords)) map.ObsCols.Add(cIdx);
                    else if (!string.IsNullOrWhiteSpace(val))
                    {
                        var rawHeader = SafeGetCellValue(cell);
                        if (!string.IsNullOrWhiteSpace(rawHeader))
                        {
                            var customKey = SopalTrace.Application.Helpers.CleColonneHelper.Normalize(rawHeader);
                            map.CustomCols[cIdx] = customKey;
                            map.CustomColsDefinition[val] = customKey;
                            
                            if (!result.ColonneDefs.Any(c => c.CleColonne == customKey))
                            {
                                result.ColonneDefs.Add(new SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentColonneDefDto
                                {
                                    CleColonne = customKey,
                                    LabelAffiche = rawHeader.Length > 200 ? rawHeader.Substring(0, 200) : rawHeader,
                                    TypeValeur = "TEXTE",
                                    OrdreAffiche = cIdx
                                });
                            }
                        }
                    }
                }
                
                if (map.InstCol == 5 && !SafeGetCellValue(row.Cell(map.InstCol)).ToLower().Contains("code"))
                {
                     // Déjà par défaut à 5
                }

                if (!map.ObsCols.Any()) map.ObsCols.AddRange(new[] { Math.Max(map.InstCol, map.MoyenCol) + 1, Math.Max(map.InstCol, map.MoyenCol) + 2 });
                map.IsInitialized = true;
                continue;
            }

            bool hasData = false;
            if (map.IsInitialized)
            {
                hasData = !string.IsNullOrEmpty(SafeGetCellValue(row.Cell(map.LimiteCol))) || 
                          !string.IsNullOrEmpty(SafeGetCellValue(row.Cell(map.TypeCol)));
            }

            // DÉTECTION DE SECTION (FALLBACK SI NON MERGÉ)
            if (!isHorizontalMerge && !string.IsNullOrWhiteSpace(colA_Raw) && row.CellsUsed().Count() == 1 && !hasData)
            {
                bool hasDataRowsAfter = false;
                for (int j = i + 1; j < Math.Min(i + 20, rows.Count); j++)
                {
                    var nextRow = rows[j];
                    if (nextRow.CellsUsed().Any(c => c.GetString().Contains("Limite", StringComparison.OrdinalIgnoreCase) || c.GetString().Contains("Type", StringComparison.OrdinalIgnoreCase)))
                    {
                        hasDataRowsAfter = true; break;
                    }
                    if (!nextRow.Cell(1).IsMerged() && !string.IsNullOrEmpty(SafeGetCellValue(nextRow.Cell(1))) && !string.IsNullOrEmpty(SafeGetCellValue(nextRow.Cell(2))))
                    {
                        hasDataRowsAfter = true; break;
                    }
                }

                if (hasDataRowsAfter)
                {
                    currentSection = await ParseSectionHeaderAsync(colA_Raw, false);
                    result.Sections.Add(currentSection);
                }
                else if (map.IsInitialized)
                {
                    result.Remarques += colA_Raw + "\n";
                }
                continue;
            }

            int evalCaractCol = map.IsInitialized ? map.CaractCol : 1;
            string colCaract = SafeGetCellValue(row.Cell(evalCaractCol));

            bool rowHasOtherData = false;
            if (map.IsInitialized)
            {
                rowHasOtherData = !string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(map.LimiteCol))) ||
                                  !string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(map.TypeCol))) ||
                                  !string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(map.MoyenCol))) ||
                                  !string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(map.InstCol)));
                
                if (!rowHasOtherData)
                {
                    foreach (var colIdx in map.ObsCols)
                    {
                        if (!string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(colIdx)))) { rowHasOtherData = true; break; }
                    }
                }
                
                if (!rowHasOtherData)
                {
                    foreach (var colIdx in map.CustomCols.Keys)
                    {
                        if (!string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(colIdx)))) { rowHasOtherData = true; break; }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(colCaract) && !rowHasOtherData) continue;

            if (currentSection != null && map.IsInitialized)
            {
                if (rowHasOtherData || imagesByRow.ContainsKey(row.RowNumber())) // Always add row if it has an image!
                {
                    var ligne = await ParseLigneAsync(row, map, imagesByRow);
                    currentSection.Lignes.Add(ligne);
                }
                else if (!string.IsNullOrWhiteSpace(colCaract))
                {
                    result.Remarques += colCaract + "\n";
                }
            }
        }
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
        if (matches.Count > 0)
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

    private async Task<ImportExcelLigneDto> ParseLigneFromStringsAsync(string colA, string colB, string colC, string colD, string colE, string colG, string colH, string colI)
    {
        var ligne = new ImportExcelLigneDto();
        ligne.LibelleAffiche = colA;
        string limiteSpec = colB;
        string typeControle = colC;
        string moyenControle = colD;
        ligne.InstrumentCode = SafeSubstring(colE?.Trim() ?? string.Empty, 40);

        ligne.Observations = colG;
        if (string.IsNullOrEmpty(ligne.Observations)) ligne.Observations = colH;
        if (string.IsNullOrEmpty(ligne.Observations)) ligne.Observations = colI;

        return await CompleteParseLigneAsync(ligne, typeControle, moyenControle, limiteSpec);
    }

    private async Task<ImportExcelLigneDto> ParseLigneAsync(IXLRow row, PlanColumnMapping map, Dictionary<int, string>? imagesByRow = null)
    {
        var ligne = new ImportExcelLigneDto();
        if (imagesByRow != null && imagesByRow.TryGetValue(row.RowNumber(), out var img))
        {
            ligne.ImageBase64 = img;
        }
        
        ligne.LibelleAffiche = SafeGetCellValue(row.Cell(map.CaractCol));
        string limiteSpec = SafeGetCellValue(row.Cell(map.LimiteCol));
        string typeControle = SafeGetCellValue(row.Cell(map.TypeCol));
        string moyenControle = SafeGetCellValue(row.Cell(map.MoyenCol));
        ligne.InstrumentCode = SafeSubstring(SafeGetCellValue(row.Cell(map.InstCol)).Trim(), 40);

        var obsList = new List<string>();
        foreach (var colIdx in map.ObsCols)
        {
            string obs = SafeGetCellValue(row.Cell(colIdx));
            if (!string.IsNullOrWhiteSpace(obs)) obsList.Add(obs);
        }
        ligne.Observations = string.Join(" - ", obsList);

        if (map.CustomCols.Count > 0)
        {
            foreach (var kvp in map.CustomCols)
            {
                string cVal = SafeGetCellValue(row.Cell(kvp.Key));
                if (!string.IsNullOrWhiteSpace(cVal))
                {
                    ligne.ColonnesSupplementaires[kvp.Value] = cVal;
                }
            }
        }

        return await CompleteParseLigneAsync(ligne, typeControle, moyenControle, limiteSpec);
    }

    private async Task<ImportExcelLigneDto> CompleteParseLigneAsync(ImportExcelLigneDto ligne, string typeControle, string moyenControle, string limiteSpec)
    {
        if (!string.IsNullOrEmpty(ligne.LibelleAffiche))
        {
            var typeCarac = await _unitOfWork.DictionnaireQualiteRepository.GetTypeCaracteristiqueByLibelleAsync(ligne.LibelleAffiche);
            if (typeCarac == null)
            {
                typeCarac = new TypeCaracteristique
                {
                    Id = Guid.NewGuid(),
                    Libelle = SafeSubstring(ligne.LibelleAffiche, 80),
                    Code = $"EXC-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                    Actif = true
                };
                await _unitOfWork.DictionnaireQualiteRepository.AddTypeCaracteristiqueAsync(typeCarac);
                await _unitOfWork.CommitAsync();
            }
            ligne.TypeCaracteristiqueId = typeCarac.Id;
        }

        if (!string.IsNullOrEmpty(typeControle))
        {
            var typeC = await _unitOfWork.DictionnaireQualiteRepository.GetTypeControleByLibelleAsync(typeControle);
            if (typeC == null)
            {
                typeC = new TypeControle
                {
                    Id = Guid.NewGuid(),
                    Libelle = SafeSubstring(typeControle, 80),
                    Code = $"EXC-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                    Actif = true
                };
                await _unitOfWork.DictionnaireQualiteRepository.AddTypeControleAsync(typeC);
                await _unitOfWork.CommitAsync();
            }
            ligne.TypeControleId = typeC.Id;
        }

        if (!string.IsNullOrEmpty(moyenControle))
        {
            var moyenC = await _unitOfWork.DictionnaireQualiteRepository.GetMoyenControleByLibelleAsync(moyenControle);
            if (moyenC == null)
            {
                moyenC = new MoyenControle
                {
                    Id = Guid.NewGuid(),
                    Libelle = SafeSubstring(moyenControle, 100),
                    Code = $"EXC-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                    Actif = true
                };
                await _unitOfWork.DictionnaireQualiteRepository.AddMoyenControleAsync(moyenC);
                await _unitOfWork.CommitAsync();
            }
            ligne.MoyenControleId = moyenC.Id;
        }

        if (!string.IsNullOrEmpty(ligne.InstrumentCode))
        {
            var instrumentCode = SafeSubstring(ligne.InstrumentCode.Trim(), 40);
            var instrument = await _unitOfWork.DictionnaireQualiteRepository.GetInstrumentByCodeAsync(instrumentCode);
            if (instrument == null)
            {
                instrument = new Instrument
                {
                    CodeInstrument = instrumentCode,
                    Designation = SafeSubstring(instrumentCode, 100),
                    Statut = StatutsPlan.Actif,
                    Actif = true
                };
                await _unitOfWork.DictionnaireQualiteRepository.AddInstrumentAsync(instrument);
                await _unitOfWork.CommitAsync();
            }
            ligne.InstrumentCode = instrument.CodeInstrument;
        }

        ligne.LimiteSpecTexte = limiteSpec;
        return ligne;
    }

    private List<string> ParseCsvLine(string line, char separator)
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

    private async Task<ImportExcelResultDto> ParseCsvAsync(Stream stream)
    {
        var result = new ImportExcelResultDto();
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        ImportExcelSectionDto? currentSection = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            char separator = line.Contains(";") ? ';' : ',';
            var parts = ParseCsvLine(line, separator);

            string colA = FixEncoding(parts.Count > 0 ? parts[0] : "");
            string colB = FixEncoding(parts.Count > 1 ? parts[1] : "");
            string colC = FixEncoding(parts.Count > 2 ? parts[2] : "");
            string colD = FixEncoding(parts.Count > 3 ? parts[3] : "");
            string colE = FixEncoding(parts.Count > 4 ? parts[4] : "");
            string colG = FixEncoding(parts.Count > 6 ? parts[6] : "");
            string colH = FixEncoding(parts.Count > 7 ? parts[7] : "");
            string colI = FixEncoding(parts.Count > 8 ? parts[8] : "");

            if (string.IsNullOrEmpty(colA)) continue;
            if (colA.Equals("Caractéristique contrôlée", StringComparison.OrdinalIgnoreCase) ||
                colA.Equals("Caractéristiques", StringComparison.OrdinalIgnoreCase)) continue;

            if (string.IsNullOrEmpty(colB) && string.IsNullOrEmpty(colC))
            {
                bool hasFrequencyFormat = colA.Contains("(") && colA.Contains(")");

                if (!hasFrequencyFormat && currentSection != null)
                {
                    result.Remarques += colA + "\n";
                    continue;
                }

                if (colA.StartsWith("(") && currentSection != null)
                {
                    var tempSection = await ParseSectionHeaderAsync(colA, true);
                    if (tempSection.ModeFreq != "SANS" || !string.IsNullOrEmpty(tempSection.FrequenceLibelle))
                    {
                        currentSection.FrequenceLibelle = tempSection.FrequenceLibelle;
                        currentSection.ModeFreq = tempSection.ModeFreq;
                        currentSection.TypeVariable = tempSection.TypeVariable;
                        currentSection.FreqNum = tempSection.FreqNum;
                        currentSection.FreqHours = tempSection.FreqHours;
                        currentSection.RegleEchantillonnageId = tempSection.RegleEchantillonnageId;
                    }
                    else
                    {
                        currentSection.Nom += " " + colA;
                    }
                    continue;
                }

                var parsedSection = await ParseSectionHeaderAsync(colA, false);
                var existingSection = result.Sections.FirstOrDefault(s =>
                    s.Nom.Equals(parsedSection.Nom, StringComparison.OrdinalIgnoreCase) &&
                    s.FrequenceLibelle == parsedSection.FrequenceLibelle);

                currentSection = existingSection ?? parsedSection;
                if (existingSection == null) result.Sections.Add(currentSection);
                continue;
            }

            if (currentSection != null)
            {
                var ligne = await ParseLigneFromStringsAsync(colA, colB, colC, colD, colE, colG, colH, colI);
                currentSection.Lignes.Add(ligne);
            }
        }

        return result;
    }
}
