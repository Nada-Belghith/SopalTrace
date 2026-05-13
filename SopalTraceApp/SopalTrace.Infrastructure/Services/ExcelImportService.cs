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

namespace SopalTrace.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly Dictionary<string, Periodicite> _createdPerioCache = new();
    private readonly Dictionary<string, PieceReference> _createdPiecesCache = new();
    private readonly Dictionary<string, RefFamilleCorp> _createdFamiliesCache = new();
    private readonly Dictionary<string, RefMoyenDetection> _createdMoyensCache = new();

    private class PlanColumnMapping
    {
        public bool IsInitialized { get; set; } = false;
        public int CaractCol { get; set; } = 1;
        public int LimiteCol { get; set; } = 2;
        public int TypeCol { get; set; } = 3;
        public int MoyenCol { get; set; } = 4;
        public int InstCol { get; set; } = 5;
        public List<int> ObsCols { get; set; } = new List<int>();
    }

    private class VerifMachineColumnMapping
    {
        public int RisqueCol { get; set; } = 1;
        public int MethodeCol { get; set; } = 2;
        public int PerioCol { get; set; } = 3;
        public int? MoyenDetCol { get; set; } = null;
        public int PieceRefStartCol { get; set; } = 4;
        public int PieceRefEndCol { get; set; } = 4;
        public int? FuiteCol { get; set; } = null;
        public Dictionary<int, string> Familles { get; set; } = new();
        public int HeaderBottomRow { get; set; } = -1;
    }

    public ExcelImportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private string SafeSubstring(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength);
    }

    private string FixEncoding(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.Contains("Ã") || text.Contains(""))
        {
            try
            {
                var bytes = System.Text.Encoding.GetEncoding(1252).GetBytes(text);
                var fixedText = System.Text.Encoding.UTF8.GetString(bytes);
                if (!fixedText.Contains("")) return fixedText;
            }
            catch { }
        }
        return text;
    }

    private string SafeGetCellValue(IXLCell cell)
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

    public async Task<ImportExcelResultDto> ParsePlanExcelAsync(Stream excelStream, string fileName)
    {
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return await ParseCsvAsync(excelStream);

        var result = new ImportExcelResultDto();
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new Exception("Le fichier Excel est vide ou invalide.");

        var rows = worksheet.RowsUsed().ToList();
        if (!rows.Any()) return result;

        var map = new PlanColumnMapping();
        ImportExcelSectionDto currentSection = null;

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

            // DÉTECTION D'EN-TÊTE DE COLONNE (SMART)
            bool isHeaderRow = row.CellsUsed().Count() >= 2 &&
                               row.CellsUsed().Any(c => {
                                   var v = SafeGetCellValue(c).ToLower();
                                   return v.Contains("caractéristique") || v.Contains("caracteristique") || v.Contains("cote") || v.Contains("paramètre") || v.Contains("point");
                               }) &&
                               row.CellsUsed().Any(c => {
                                   var v = SafeGetCellValue(c).ToLower();
                                   return v.Contains("limite") || v.Contains("spécification") || v.Contains("specification") || v.Contains("tolérance") || v.Contains("valeur") || v.Contains("type") || v.Contains("contrôle");
                               });

            if (isHeaderRow)
            {
                map = new PlanColumnMapping();
                foreach (var cell in row.CellsUsed())
                {
                    string val = SafeGetCellValue(cell).ToLower();
                    int cIdx = cell.Address.ColumnNumber;

                    if (val.Contains("caractéristique") || val.Contains("caracteristique") || val.Contains("cote") || val.Contains("paramètre") || val.Contains("point")) map.CaractCol = cIdx;
                    else if (val.Contains("limite") || val.Contains("spécification") || val.Contains("specification") || val.Contains("tolérance") || val.Contains("valeur")) map.LimiteCol = cIdx;
                    else if (val.Contains("code") || val.Contains("id") || val.Contains("réf")) map.InstCol = cIdx;
                    else if (val.Contains("moyen") || val.Contains("instrument") || val.Contains("appareil") || val.Contains("outil")) map.MoyenCol = cIdx;
                    else if (val.Contains("type") || (val.Contains("contrôle") && !val.Contains("moyen"))) map.TypeCol = cIdx;
                    else if (val.Contains("observation") || val.Contains("remarque") || val.Contains("note") || val.Contains("instruction")) map.ObsCols.Add(cIdx);
                }
                
                // Si l'instrument n'a pas été trouvé explicitement, on cherche une colonne "Code" à côté du moyen
                if (map.InstCol == 5 && !SafeGetCellValue(row.Cell(map.InstCol)).ToLower().Contains("code"))
                {
                     // Déjà par défaut à 5, on laisse si rien de mieux
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
                // On regarde s'il y a des données après
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
                    // C'est du texte en-dessous du tableau, donc on l'ajoute aux remarques
                    result.Remarques += colA_Raw + "\n";
                }
                continue;
            }

            int evalCaractCol = map.IsInitialized ? map.CaractCol : 1;
            string colCaract = SafeGetCellValue(row.Cell(evalCaractCol));

            if (string.IsNullOrEmpty(colCaract)) continue;

            if (currentSection != null && map.IsInitialized)
            {
                string colLimite = SafeGetCellValue(row.Cell(map.LimiteCol));
                string colType = SafeGetCellValue(row.Cell(map.TypeCol));

                if (!string.IsNullOrEmpty(colLimite) || !string.IsNullOrEmpty(colType))
                {
                    var ligne = await ParseLigneAsync(row, map);
                    currentSection.Lignes.Add(ligne);
                }
                else result.Remarques += colCaract + "\n";
            }
        }
        return result;
    }

    // =========================================================================
    // PARSING DE SECTION : AUCUNE CREATION DE DONNEES POUR NE PAS POLLUER
    // =========================================================================
    public async Task<ImportExcelSectionDto> ParseSectionHeaderAsync(string text, bool isContinuation = false)
    {
        var section = new ImportExcelSectionDto();
        string cleanText = FixEncoding(text.Replace("\n", " ").Replace("\r", " ").Trim());

        // L'aperçu (Nom) garde TOUJOURS la phrase complète exacte du fichier Excel
        section.Nom = cleanText;

        // 1. ISOLER LE CONTENU ENTRE PARENTHÈSES
        string parenthesesContent = "";
        string naturePart = cleanText;

        var matches = Regex.Matches(cleanText, @"\((?>[^()]+|\((?<c>)|\)(?<-c>))*(?(c)(?!))\)");
        if (matches.Count > 0)
        {
            var lastMatch = matches[^1];
            parenthesesContent = lastMatch.Value.Substring(1, lastMatch.Value.Length - 2).Trim();
            naturePart = cleanText.Replace(lastMatch.Value, "").Trim();
        }

        // Nettoyage supplémentaire pour la nature : on enlève les préfixes communs
        string natureSearch = Regex.Replace(naturePart, @"^SEC\s*\d+\s*[-:]*", "", RegexOptions.IgnoreCase).Trim();
        natureSearch = Regex.Replace(natureSearch, @"^Caractéristiques\s*à\s*contrôler\s*", "", RegexOptions.IgnoreCase).Trim();
        natureSearch = Regex.Replace(natureSearch, @"^Contrôle\s*", "", RegexOptions.IgnoreCase).Trim();
        natureSearch = Regex.Replace(natureSearch, @"^Section\s*", "", RegexOptions.IgnoreCase).Trim();
        
        // Enlève les tirets/espaces superflus au début/fin après nettoyage
        natureSearch = natureSearch.Trim('-', ' ', ':', '.');

        // 2. RECHERCHER LA NATURE DE SECTION DANS LE DICTIONNAIRE
        var typeSec = await _unitOfWork.DictionnaireQualiteRepository.GetTypeSectionByLibelleAsync(natureSearch);
        if (typeSec == null && naturePart != natureSearch)
        {
            typeSec = await _unitOfWork.DictionnaireQualiteRepository.GetTypeSectionByLibelleAsync(naturePart.Trim());
        }

        string finalNatureLib = natureSearch;
        if (typeSec != null)
        {
            section.TypeSectionId = typeSec.Id;
        }

        // On commence à construire le libellé complet
        // Le user veut "Caractéristiques à contrôler " comme préfixe si possible
        string prefix = (finalNatureLib.ToLower().Contains("réglage") || finalNatureLib.ToLower().Contains("réglages")) 
                        ? "" : "Caractéristiques à contrôler ";
        
        section.LibelleSection = $"{prefix}{finalNatureLib}".Trim();

        // 3. ANALYSER LE CONTENU ENTRE PARENTHÈSES (RÈGLE OU FRÉQUENCE)
        if (!string.IsNullOrEmpty(parenthesesContent))
        {
            string contentLower = parenthesesContent.ToLower();

            // On tente une recherche directe en base pour savoir si c'est une règle connue
            var regleMatch = await _unitOfWork.DictionnaireQualiteRepository.GetRegleEchantillonnageByLibelleAsync(parenthesesContent);

            bool isRule = regleMatch != null || 
                          contentLower.Contains("selon") || contentLower.Contains("iso") ||
                          contentLower.Contains("nqa") || contentLower.Contains("effectif") ||
                          contentLower.Contains("tableau") || contentLower.Contains("première") || 
                          contentLower.Contains("dernière");

            // Fallback : Si le texte est long et contient "pièce", c'est probablement une règle complexe
            if (!isRule && parenthesesContent.Length > 20 && (contentLower.Contains("pièce") || contentLower.Contains("piece")))
            {
                isRule = true;
            }

            bool isFrequency = !isRule && (contentLower.Contains("pièce") || contentLower.Contains("piece") ||
                                           contentLower.Contains("p/h") || contentLower.Contains("p/") ||
                                           contentLower.Contains("série") || contentLower.Contains("serie") ||
                                           contentLower.Contains("lot") || contentLower.Contains("%") ||
                                           contentLower.Contains("échantillon") || contentLower.Contains("echantillon"));

            if (isRule)
            {
                // RECHERCHER LA RÈGLE D'ÉCHANTILLONNAGE
                var regle = await _unitOfWork.DictionnaireQualiteRepository.GetRegleEchantillonnageByLibelleAsync(parenthesesContent);
                string finalFreqLib = parenthesesContent;
                if (regle != null)
                {
                    section.RegleEchantillonnageId = regle.Id;
                    section.ModeFreq = "FIXE";
                    finalFreqLib = regle.Libelle;
                }
                section.FrequenceLibelle = finalFreqLib;
                section.RegleEchantillonnageLibelle = finalFreqLib;
                
                // Concaténer à LibelleSection
                section.LibelleSection += $" ({finalFreqLib})";
            }
            else if (isFrequency)
            {
                // Parser la fréquence
                section.FrequenceLibelle = parenthesesContent;
                
                // PRIORITÉ 1 : On vérifie si c'est une périodicité EXACTE dans le dictionnaire (ex: "100% des pièces/h")
                var perio = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(parenthesesContent);
                if (perio != null)
                {
                    section.PeriodiciteId = perio.Id;
                    section.ModeFreq = "FIXE";
                    section.FrequenceLibelle = perio.Libelle;
                    section.FreqNum = perio.FrequenceNum ?? 1;
                    if (perio.FrequenceUnite == "1_HEURE" || perio.FrequenceUnite == "PCT_HEURE")
                    {
                        section.TypeVariable = "HEURE";
                        section.FreqHours = 1;
                    }
                }
                else
                {
                    // PRIORITÉ 2 : Parsing dynamique par Regex si pas trouvé dans le dictionnaire
                    section.ModeFreq = "VARIABLE";
                    contentLower = parenthesesContent.ToLower();
                    
                    if ((contentLower.Contains("pièce") || contentLower.Contains("piece") || contentLower.Contains("p/h") || contentLower.Contains("p/")) &&
                        (contentLower.Contains("heure") || contentLower.Contains("h")))
                    {
                        section.TypeVariable = "HEURE";
                        var m = Regex.Match(contentLower, @"(\d+)(?:%)?.*?(?:pièce|piece).*?/\s*(?:(\d+)\s*)?(?:heure|h\b)", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            section.FreqNum = int.Parse(m.Groups[1].Value);
                            section.FreqHours = string.IsNullOrEmpty(m.Groups[2].Value) ? 1 : int.Parse(m.Groups[2].Value);
                        }
                    }
                    else if (contentLower.Contains("série") || contentLower.Contains("serie") || contentLower.Contains("lot"))
                    {
                        section.TypeVariable = "SERIE";
                        var mS = Regex.Match(contentLower, @"(?:série|serie|lot).*?(\d+)\s*(?:pièce|piece|p)?", RegexOptions.IgnoreCase);
                        if (!mS.Success) mS = Regex.Match(contentLower, @"(\d+)\s*(?:pièce|piece|p)", RegexOptions.IgnoreCase);
                        if (mS.Success) section.FreqNum = int.Parse(mS.Groups[1].Value);
                    }
                    else if (contentLower.Contains("échantillon") || contentLower.Contains("echantillon"))
                    {
                        section.TypeVariable = "ECHANTILLON";
                        var mE = Regex.Match(contentLower, @"(?:échantillons|echantillons|échantillon|echantillon).*?(\d+)", RegexOptions.IgnoreCase);
                        if (!mE.Success) mE = Regex.Match(contentLower, @"(\d+)\s*(?:échantillons|echantillons|échantillon|echantillon)", RegexOptions.IgnoreCase);
                        if (mE.Success) section.FreqNum = int.Parse(mE.Groups[1].Value);
                    }
                }
                
                // Concaténer à LibelleSection
                section.LibelleSection += $" ({parenthesesContent})";
            }
            else
            {
                // Autre chose entre parenthèses
                section.LibelleSection += $" ({parenthesesContent})";
            }
        }

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

    private async Task<ImportExcelLigneDto> ParseLigneAsync(IXLRow row, PlanColumnMapping map)
    {
        var ligne = new ImportExcelLigneDto();
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

        return await CompleteParseLigneAsync(ligne, typeControle, moyenControle, limiteSpec);
    }

    private async Task<ImportExcelLigneDto> ParseLigneAsync(IXLRow row)
    {
        var ligne = new ImportExcelLigneDto();

        ligne.LibelleAffiche = SafeGetCellValue(row.Cell(1));
        string limiteSpec = SafeGetCellValue(row.Cell(2));
        string typeControle = SafeGetCellValue(row.Cell(3));
        string moyenControle = SafeGetCellValue(row.Cell(4));
        ligne.InstrumentCode = SafeSubstring(SafeGetCellValue(row.Cell(5)).Trim(), 40);

        ligne.Observations = SafeGetCellValue(row.Cell(6));
        if (string.IsNullOrEmpty(ligne.Observations)) ligne.Observations = SafeGetCellValue(row.Cell(7));
        if (string.IsNullOrEmpty(ligne.Observations)) ligne.Observations = SafeGetCellValue(row.Cell(8));

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
                    Statut = "ACTIF",
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

    // =========================================================================
    // VERIF MACHINE (inchangé)
    // =========================================================================
    private VerifMachineColumnMapping BuildVMMap(List<IXLRow> rows, int startIdx)
    {
        var map = new VerifMachineColumnMapping();
        var searchRows = rows.Skip(startIdx).Take(8).ToList();

        foreach (var r in searchRows)
        {
            foreach (var c in r.CellsUsed())
            {
                string val = SafeGetCellValue(c).ToLower();
                int cNum = c.Address.ColumnNumber;

                if ((val.Contains("conformit") && !val.Contains("observation") && !val.Contains("non")) || val.Contains("risque") || val.Contains("défaut") || val.Contains("defaut") || val.Contains("caract")) 
                {
                    if (map.RisqueCol == 0) map.RisqueCol = cNum;
                }
                else if (val.Contains("méthode") || val.Contains("methode")) map.MethodeCol = cNum;
                else if (val.Contains("périod") || val.Contains("period")) map.PerioCol = cNum;
                else if (!val.Contains("num") && val.Contains("moyen") && (val.Contains("détec") || val.Contains("detec") || val.Contains("dét") || val.Contains("det") || val.Contains("contr"))) map.MoyenDetCol = cNum;
                else if (val.Contains("fuite") && val.Contains("étalon")) map.FuiteCol = cNum;
                else if (val.Contains("num") || val.Contains("piec")) 
                {
                    if ((val.Contains("pi") && (val.Contains("num") || val.Contains("réf") || val.Contains("ref"))) ||
                        (val.Contains("num") && val.Contains("moyen") && val.Contains("contr")))
                    {
                        map.PieceRefStartCol = cNum;
                        if (c.IsMerged()) map.PieceRefEndCol = c.MergedRange().LastColumn().ColumnNumber();
                        else map.PieceRefEndCol = cNum;
                    }
                }
            }
        }

        foreach (var r in searchRows)
        {
            bool foundFamiliesInThisRow = false;
            if (map.PieceRefStartCol > 0 && map.PieceRefEndCol >= map.PieceRefStartCol)
            {
                for (int cNum = map.PieceRefStartCol; cNum <= map.PieceRefEndCol; cNum++)
                {
                    string fVal = SafeGetCellValue(r.Cell(cNum));
                    if (!string.IsNullOrWhiteSpace(fVal) && (fVal.Contains("Corps", StringComparison.OrdinalIgnoreCase) || fVal.StartsWith("(")))
                    {
                        map.Familles[cNum] = fVal;
                        map.HeaderBottomRow = r.RowNumber();
                        foundFamiliesInThisRow = true;
                    }
                }
            }
            if (foundFamiliesInThisRow) break;
        }
        return map;
    }

    public async Task<ImportVerifMachineExcelResultDto> ParseVerifMachineExcelAsync(Stream excelStream, string fileName)
    {
        ResetCaches();
        var result = new ImportVerifMachineExcelResultDto();
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new Exception("Le fichier Excel est vide ou invalide.");

        var fileNameMatch = Regex.Match(fileName, @"VM_([A-Z0-9-]+)", RegexOptions.IgnoreCase);
        if (fileNameMatch.Success) result.MachineCode = fileNameMatch.Groups[1].Value;

        var rows = worksheet.RowsUsed().ToList();
        var map = BuildVMMap(rows, 0);

        foreach (var fam in map.Familles.Values)
        {
            await GetOrCreateFamilleAsync(fam);
            result.Familles.Add(fam);
        }

        bool inConformite = false;
        // Détecter la section initiale à partir des lignes d'en-tête (qui seront sautées par la boucle principale)
        for (int j = 0; j <= map.HeaderBottomRow && j < rows.Count; j++)
        {
            string txt = string.Join(" ", rows[j].CellsUsed().Select(c => SafeGetCellValue(c)));
            if (txt.Contains("RISQUE", StringComparison.OrdinalIgnoreCase) || txt.Contains("DÉFAUT", StringComparison.OrdinalIgnoreCase) || txt.Contains("DEFAUT", StringComparison.OrdinalIgnoreCase)) inConformite = false;
            else if (txt.Contains("CONFORMIT", StringComparison.OrdinalIgnoreCase)) inConformite = true;
        }

        ImportVerifMachineLigneDto? currentLigne = null;
        string lastSeenRisque = "";
        string lastSeenMethode = "";
        string lastSeenPerio = "";

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            
            string rawCol1 = SafeGetCellValue(row.Cell(1)); 
            string rowFullText = string.Join(" ", row.CellsUsed()
                .Select(c => SafeGetCellValue(c))
                .Where(v => !string.IsNullOrWhiteSpace(v)));

            if (rowFullText.Contains("Rapport", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(result.NomPlan))
            {
                result.NomPlan = rawCol1;
                var machineMatch = Regex.Match(rawCol1, @"([A-Z0-9-]{3,10})$");
                if (machineMatch.Success) result.MachineCode = machineMatch.Value;
            }

            if (map.HeaderBottomRow > -1 && row.RowNumber() <= map.HeaderBottomRow) continue;

            // 1. DÉTECTION DE CHANGEMENT DE SECTION (Header)
            bool isSectionTitle = rowFullText.Contains("CONFORMIT", StringComparison.OrdinalIgnoreCase) || 
                                  rowFullText.Contains("RISQUE", StringComparison.OrdinalIgnoreCase) || 
                                  rowFullText.Contains("DÉFAUT", StringComparison.OrdinalIgnoreCase) ||
                                  rowFullText.Contains("DEFAUT", StringComparison.OrdinalIgnoreCase);

            bool isHeaderRow = isSectionTitle || row.CellsUsed().Any(c => {
                var val = SafeGetCellValue(c).ToLower().Trim();
                return val == "périodicité" || val == "periodicite" || 
                       val.Contains("moyen de") || 
                       val == "méthode de controle" || val == "methode de controle" ||
                       val.StartsWith("moyen/ méthode");
            });

            if (isHeaderRow)
            {
                if (rowFullText.Contains("RISQUE", StringComparison.OrdinalIgnoreCase) || rowFullText.Contains("DÉFAUT", StringComparison.OrdinalIgnoreCase) || rowFullText.Contains("DEFAUT", StringComparison.OrdinalIgnoreCase)) inConformite = false;
                else if (rowFullText.Contains("CONFORMIT", StringComparison.OrdinalIgnoreCase)) inConformite = true;
                
                currentLigne = null;
                
                // Vérifier si cette ligne contient les vraies colonnes
                bool hasColumns = row.CellsUsed().Any(c => 
                {
                    var val = SafeGetCellValue(c).ToLower();
                    return val.Contains("méthode") || val.Contains("methode") || val.Contains("périod") || val.Contains("period");
                });

                if (hasColumns)
                {
                    // Recalcul dynamique des colonnes UNIQUEMENT si c'est la ligne des colonnes
                    map.MoyenDetCol = null;
                    map.PieceRefStartCol = 0;
                    map.FuiteCol = null;

                    foreach (var c in row.CellsUsed())
                    {
                        string val = SafeGetCellValue(c).ToLower();
                        if (val.Contains("conformit") && !val.Contains("observation") && !val.Contains("non")) map.RisqueCol = c.Address.ColumnNumber;
                        else if (val.Contains("risque") || val.Contains("défaut") || val.Contains("defaut") || val.Contains("caract")) map.RisqueCol = c.Address.ColumnNumber;
                        else if (val.Contains("méthode") || val.Contains("methode")) map.MethodeCol = c.Address.ColumnNumber;
                        else if (val.Contains("périod") || val.Contains("period")) map.PerioCol = c.Address.ColumnNumber;
                        else if (!val.Contains("num") && (val.Contains("détec") || val.Contains("detec") || val.Contains("dét") || val.Contains("det") || (val.Contains("moyen") && val.Contains("contr")))) map.MoyenDetCol = c.Address.ColumnNumber;
                        else if (val.Contains("num") || val.Contains("piec")) 
                        {
                            if ((val.Contains("pi") && (val.Contains("num") || val.Contains("réf") || val.Contains("ref"))) ||
                                (val.Contains("num") && val.Contains("moyen") && val.Contains("contr")))
                            {
                                map.PieceRefStartCol = c.Address.ColumnNumber;
                                if (c.IsMerged()) map.PieceRefEndCol = c.MergedRange().LastColumn().ColumnNumber();
                                else map.PieceRefEndCol = c.Address.ColumnNumber;
                            }
                        }
                        else if (val.Contains("fuite") && val.Contains("étalon")) map.FuiteCol = c.Address.ColumnNumber;
                    }
                }
                continue;
            }

            // 2. LECTURE DES VALEURS BRUTES
            string rawColA = SafeGetCellValue(row.Cell(map.RisqueCol > 0 ? map.RisqueCol : 1));
            string rawColB = SafeGetCellValue(row.Cell(map.MethodeCol > 0 ? map.MethodeCol : 2));
            string rawColC = SafeGetCellValue(row.Cell(map.PerioCol > 0 ? map.PerioCol : 3));
            string colD = map.MoyenDetCol.HasValue ? SafeGetCellValue(row.Cell(map.MoyenDetCol.Value)) : "";

            if (IsLigneLegendePieces(rowFullText) ||
                rowFullText.Contains("Validé par", StringComparison.OrdinalIgnoreCase) ||
                rowFullText.Contains("Valide par", StringComparison.OrdinalIgnoreCase) ||
                rowFullText.Contains("Matricule", StringComparison.OrdinalIgnoreCase) ||
                rowFullText.Contains("Signature", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (rawColA.Equals("Test de conformité", StringComparison.OrdinalIgnoreCase) ||
                rawColA.Equals("Risque/ Défaut", StringComparison.OrdinalIgnoreCase) ||
                rawColA.Equals("Numéro de la pièce référence", StringComparison.OrdinalIgnoreCase) ||
                rawColA.Equals("Numéro de la pièce de référence", StringComparison.OrdinalIgnoreCase)) continue;

            // 3. HÉRITAGE UNIVERSEL DES VALEURS (Architecture A ou B)
            if (!string.IsNullOrWhiteSpace(rawColA)) lastSeenRisque = rawColA;
            if (!string.IsNullOrWhiteSpace(rawColB)) lastSeenMethode = rawColB;
            if (!string.IsNullOrWhiteSpace(rawColC)) lastSeenPerio = rawColC;

            string colA = string.IsNullOrWhiteSpace(rawColA) ? lastSeenRisque : rawColA;
            string colB = string.IsNullOrWhiteSpace(rawColB) ? lastSeenMethode : rawColB;
            string colC = string.IsNullOrWhiteSpace(rawColC) ? lastSeenPerio : rawColC;

            // Une ligne est valide si elle a un libellé OU une période OU une pièce de référence
            bool hasPieceInRow = map.PieceRefStartCol > 0 && !string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(map.PieceRefStartCol)));
            if (string.IsNullOrWhiteSpace(colA) && string.IsNullOrWhiteSpace(colB) && string.IsNullOrWhiteSpace(colC) && !hasPieceInRow) continue;

            if (!string.IsNullOrWhiteSpace(colA) || !string.IsNullOrWhiteSpace(colB) || currentLigne == null)
            {
                var listToSearch = inConformite ? result.LignesConformite : result.LignesRisques;
                var existingLigne = listToSearch.LastOrDefault(l => l.LibelleRisque == (colA ?? "") && l.LibelleMethode == (colB ?? ""));

                if (existingLigne != null)
                {
                    currentLigne = existingLigne;
                }
                else
                {
                    currentLigne = new ImportVerifMachineLigneDto
                    {
                        LibelleRisque = colA ?? "",
                        LibelleMethode = colB ?? ""
                    };

                    if (inConformite) result.LignesConformite.Add(currentLigne);
                    else result.LignesRisques.Add(currentLigne);
                }
            }

            if (currentLigne != null)
            {
                var periodiciteLibelle = colC;

                var existingEcheance = currentLigne.Echeances.FirstOrDefault(e => e.PeriodiciteLibelle == periodiciteLibelle);
                ImportVerifMachineEcheanceDto echeance;

                if (existingEcheance != null)
                {
                    echeance = existingEcheance;
                }
                else
                {
                    echeance = new ImportVerifMachineEcheanceDto
                    {
                        PeriodiciteLibelle = periodiciteLibelle,
                        Rows = new List<ImportVerifMachineRowDto>()
                    };

                    var normalizedPerio = periodiciteLibelle?.Trim();
                    if (!string.IsNullOrEmpty(normalizedPerio))
                    {
                        if (!_createdPerioCache.TryGetValue(normalizedPerio, out var periodicite))
                        {
                            periodicite = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(normalizedPerio);
                            if (periodicite == null)
                            {
                                periodicite = new Periodicite { Id = Guid.NewGuid(), Code = SafeSubstring(normalizedPerio.Replace(" ", "").ToUpper(), 30), Libelle = normalizedPerio, Actif = true };
                                await _unitOfWork.DictionnaireQualiteRepository.AddPeriodiciteAsync(periodicite);
                                await _unitOfWork.CommitAsync();
                            }
                            _createdPerioCache[normalizedPerio] = periodicite;
                        }
                        echeance.PeriodiciteId = periodicite.Id;
                    }

                    currentLigne.Echeances.Add(echeance);
                }

                bool hasMoyenDet = !string.IsNullOrWhiteSpace(colD) && map.MoyenDetCol.HasValue;

                if (hasMoyenDet)
                {
                    var moyenDet = await GetOrCreateMoyenDetectionAsync(colD);

                    var newRow = new ImportVerifMachineRowDto
                    {
                        MoyenDetectionLibelle = colD,
                        MoyenDetectionId = moyenDet.Id,
                        MatricePieces = new List<ImportVerifMachineMatriceDto>()
                    };

                    echeance.Rows.Add(newRow);
                }
                else if (echeance.Rows.Count == 0)
                {
                    echeance.Rows.Add(new ImportVerifMachineRowDto
                    {
                        MoyenDetectionLibelle = "",
                        MatricePieces = new List<ImportVerifMachineMatriceDto>()
                    });
                }

                var lastRow = echeance.Rows.LastOrDefault();
                if (lastRow != null)
                {
                    if (map.Familles.Count > 0)
                    {
                        foreach (var kvp in map.Familles)
                        {
                            int excelCol = kvp.Key;
                            string pieceCode = SafeGetCellValue(row.Cell(excelCol));
                            await ProcessPieceCellAsync(pieceCode, kvp.Value, lastRow, inConformite, colD, result.MachineCode!);
                        }
                    }
                    else
                    {
                        int targetCol = map.PieceRefStartCol > 0 ? map.PieceRefStartCol : 4;
                        string pieceCode = SafeGetCellValue(row.Cell(targetCol));
                        await ProcessPieceCellAsync(pieceCode, "", lastRow, inConformite, colD, result.MachineCode!);
                    }
                }

                if (map.FuiteCol.HasValue)
                {
                    string colFuite = SafeGetCellValue(row.Cell(map.FuiteCol.Value));
                    if (!string.IsNullOrWhiteSpace(colFuite) && lastRow != null)
                    {
                        string role = "FEC";
                        if (colD.Contains("FENC", StringComparison.OrdinalIgnoreCase)) role = "FENC";
                        else if (!inConformite && !colD.Contains("PRC")) role = "FENC";

                        var fuite = await GetOrCreatePieceRefAsync(colFuite.Trim(), role, result.MachineCode!);

                        lastRow.MatricePieces.Add(new ImportVerifMachineMatriceDto
                        {
                            PieceRefCode = fuite.Code,
                            PieceRefId = fuite.Id,
                            RoleVerif = role,
                            FamilleCode = ""
                        });
                    }
                }
            }
        }

        await _unitOfWork.CommitAsync();
        result.LegendeMoyens = BuildLegendeMoyensFromImport(result);

        return result;
    }

    private void ResetCaches()
    {
        _createdPerioCache.Clear();
        _createdPiecesCache.Clear();
        _createdFamiliesCache.Clear();
        _createdMoyensCache.Clear();
    }

    private async Task ProcessPieceCellAsync(string pieceCode, string familleCode, ImportVerifMachineRowDto rowDto, bool inConformite, string colD, string machineCode)
    {
        if (string.IsNullOrWhiteSpace(pieceCode) || pieceCode.Trim().ToUpper() == "X") return;

        string role = inConformite ? "PRC" : "PRNC";
        if (colD.Contains("PRNC", StringComparison.OrdinalIgnoreCase)) role = "PRNC";
        else if (colD.Contains("PRC", StringComparison.OrdinalIgnoreCase)) role = "PRC";

        var piece = await GetOrCreatePieceRefAsync(pieceCode.Trim(), role, machineCode);

        rowDto.MatricePieces.Add(new ImportVerifMachineMatriceDto
        {
            PieceRefCode = piece.Code,
            PieceRefId = piece.Id,
            RoleVerif = role,
            FamilleCode = familleCode
        });
    }

    private static bool IsLigneLegendePieces(string colA)
    {
        if (string.IsNullOrWhiteSpace(colA)) return false;
        int count = 0;
        if (Regex.IsMatch(colA, @"\bPRC\s*:", RegexOptions.IgnoreCase)) count++;
        if (Regex.IsMatch(colA, @"\bPRNC\s*:", RegexOptions.IgnoreCase)) count++;
        if (Regex.IsMatch(colA, @"\bFENC\s*:", RegexOptions.IgnoreCase)) count++;
        if (Regex.IsMatch(colA, @"\bFEC\s*:", RegexOptions.IgnoreCase)) count++;
        return count >= 2;
    }

    private static string BuildLegendeMoyensFromImport(ImportVerifMachineExcelResultDto result)
    {
        var usedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ligne in result.LignesConformite.Concat(result.LignesRisques))
        {
            foreach (var ech in ligne.Echeances)
            {
                foreach (var row in ech.Rows)
                {
                    foreach (var piece in row.MatricePieces)
                    {
                        if (!string.IsNullOrWhiteSpace(piece.RoleVerif))
                            usedRoles.Add(piece.RoleVerif.ToUpper());
                    }
                }
            }
        }

        var definitions = new Dictionary<string, string>
        {
            ["PRC"] = "PRC : Pièce de Référence Conforme",
            ["PRNC"] = "PRNC : Pièce de Référence Non Conforme",
            ["FEC"] = "FEC : Fuite Étalon Conforme",
            ["FENC"] = "FENC : Fuite Étalon Non Conforme"
        };

        var lines = new List<string>();
        foreach (var key in new[] { "PRC", "PRNC", "FEC", "FENC" })
        {
            if (usedRoles.Contains(key) && definitions.TryGetValue(key, out var def))
                lines.Add(def);
        }

        return lines.Count > 0 ? string.Join(" | ", lines) : string.Empty;
    }

    private async Task<PieceReference> GetOrCreatePieceRefAsync(string code, string typePiece, string machineCode)
    {
        var normalizedCode = SafeSubstring(code.ToUpper().Trim(), 30);

        if (_createdPiecesCache.TryGetValue(normalizedCode, out var cachedPiece))
            return cachedPiece;

        var piece = await _unitOfWork.DictionnaireQualiteRepository.GetPieceReferenceByCodeAsync(normalizedCode);

        if (piece == null)
        {
            piece = new PieceReference
            {
                Id = Guid.NewGuid(),
                Code = normalizedCode,
                TypePiece = SafeSubstring(typePiece, 10),
                Actif = true,
                Designation = SafeSubstring(code, 150),
                FamilleDesc = string.IsNullOrWhiteSpace(machineCode) ? "Import Excel" : SafeSubstring(machineCode, 50)
            };

            await _unitOfWork.DictionnaireQualiteRepository.AddPieceReferenceAsync(piece);
            await _unitOfWork.CommitAsync();
        }

        _createdPiecesCache[normalizedCode] = piece;
        return piece;
    }

    private async Task<RefFamilleCorp> GetOrCreateFamilleAsync(string header)
    {
        var match = Regex.Match(header, @"\(([^)]+)\)");
        string code = match.Success ? match.Groups[1].Value.Trim().ToUpper() : header.Trim().ToUpper();
        string normalizedCode = SafeSubstring(code, 30);

        if (_createdFamiliesCache.TryGetValue(normalizedCode, out var cachedFam))
            return cachedFam;

        var famille = await _unitOfWork.DictionnaireQualiteRepository.GetFamilleCorpsByCodeAsync(normalizedCode);
        if (famille == null)
        {
            famille = new RefFamilleCorp
            {
                Id = Guid.NewGuid(),
                Code = normalizedCode,
                Designation = SafeSubstring(header.Trim(), 100),
                Actif = true
            };
            await _unitOfWork.DictionnaireQualiteRepository.AddFamilleCorpsAsync(famille);
            await _unitOfWork.CommitAsync();
        }

        _createdFamiliesCache[normalizedCode] = famille;
        return famille;
    }

    private async Task<RefMoyenDetection> GetOrCreateMoyenDetectionAsync(string libelle)
    {
        var normalized = libelle.Trim();
        if (_createdMoyensCache.TryGetValue(normalized, out var cachedMd))
            return cachedMd;

        var md = await _unitOfWork.DictionnaireQualiteRepository.GetMoyenDetectionByLibelleAsync(normalized);
        if (md == null)
        {
            md = new RefMoyenDetection
            {
                Id = Guid.NewGuid(),
                // Génération de code robuste : 12 chars + hash de 6 chars pour éviter les doublons sur phrases longues
                Code = SafeSubstring(normalized.Replace(" ", "").ToUpper(), 12) + normalized.GetHashCode().ToString("X").PadLeft(6, '0').Substring(0, 6),
                Designation = SafeSubstring(normalized, 100),
                Actif = true
            };
            await _unitOfWork.DictionnaireQualiteRepository.AddMoyenDetectionAsync(md);
            await _unitOfWork.CommitAsync();
        }

        _createdMoyensCache[normalized] = md;
        return md;
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
    // =========================================================================
    // PLAN NC - RÉSULTAT CONTRÔLE DE POSTE
    // Format réel du fichier Excel :
    //   Ligne titre : "Test de Non-conformité" (fusionnée, ignorée)
    //   Ligne en-tête : | N° | Machine / Banc d'essai | Désignation du défaut |
    //   Données : | 1 | MAS26 | ABSENCE/MAUVAIS MONTAGE JOINT ANTI-FUITE |
    // =========================================================================
    public Task<ImportNcExcelResultDto> ParsePlanNcExcelAsync(Stream excelStream, string fileName)
    {
        var result = new ImportNcExcelResultDto();
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new Exception("Le fichier Excel est vide ou invalide.");

        // Détection du code poste depuis le nom du fichier (ex: "RC_71.xlsx" -> "71")
        var fileMatch = Regex.Match(fileName, @"RC[_-]?([A-Z0-9]+)", RegexOptions.IgnoreCase);
        if (fileMatch.Success) result.PosteCode = fileMatch.Groups[1].Value.ToUpper();

        var rows = worksheet.RowsUsed().ToList();
        int dataStartRow = 0;

        // Parcours des premières lignes pour détecter titre et en-tête
        for (int i = 0; i < Math.Min(rows.Count, 8); i++)
        {
            var row = rows[i];
            var fullText = string.Join(" ", row.CellsUsed().Select(c => SafeGetCellValue(c)));

            // Ligne de titre (ex: "Test de Non-conformité", "Fiche de Contrôle...")
            if (fullText.Contains("Non-conformit", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("non conformit", StringComparison.OrdinalIgnoreCase) ||
                (fullText.Contains("fiche", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(result.NomPlan)))
            {
                result.NomPlan = fullText.Trim();

                // Tenter d'extraire le code poste depuis le titre
                var posteMatch = Regex.Match(fullText, @"POSTE[:\s]*([A-Z0-9]+)", RegexOptions.IgnoreCase);
                if (posteMatch.Success && string.IsNullOrEmpty(result.PosteCode))
                    result.PosteCode = posteMatch.Groups[1].Value.ToUpper();

                dataStartRow = i + 1;
                continue;
            }

            // Ligne d'en-tête de colonnes (contient "Machine" ou "Désignation" ou "N°")
            string cell1 = SafeGetCellValue(row.Cell(1)).Trim().ToLower();
            string cell2 = SafeGetCellValue(row.Cell(2)).Trim().ToLower();
            string cell3 = SafeGetCellValue(row.Cell(3)).Trim().ToLower();

            bool isHeaderRow = cell1.Contains("n°") || cell1 == "n" ||
                               cell2.Contains("machine") || cell2.Contains("banc") ||
                               cell3.Contains("désignation") || cell3.Contains("designation") || cell3.Contains("défaut") || cell3.Contains("defaut");

            if (isHeaderRow)
            {
                dataStartRow = i + 1; // Les données commencent à la ligne suivante
                break;
            }
        }

        // Lecture des données
        // Format : Col A = N° (numérique, ignoré) | Col B = Machine | Col C = Défaut
        for (int i = dataStartRow; i < rows.Count; i++)
        {
            var row = rows[i];
            string colA = SafeGetCellValue(row.Cell(1)).Trim(); // N° - ignoré
            string colB = SafeGetCellValue(row.Cell(2)).Trim(); // Machine / Banc d'essai
            string colC = SafeGetCellValue(row.Cell(3)).Trim(); // Désignation du défaut

            // Sauter lignes vides
            if (string.IsNullOrWhiteSpace(colB) && string.IsNullOrWhiteSpace(colC)) continue;

            // Sauter si colA est un en-tête textuel qui a pu glisser
            if (colA.ToLower().Contains("n°") || colA.ToLower() == "n") continue;

            // Lignes de remarques (machine vide, mais texte dans défaut)
            if (string.IsNullOrWhiteSpace(colB))
            {
                result.Remarques += colC + "\n";
                continue;
            }

            result.Lignes.Add(new ImportNcLigneDto
            {
                MachineCode = colB.ToUpper(),
                LibelleDefaut = colC
            });
        }

        if (string.IsNullOrEmpty(result.NomPlan))
            result.NomPlan = !string.IsNullOrEmpty(result.PosteCode)
                ? $"Fiche de Contrôle - Poste {result.PosteCode}"
                : "Fiche de Contrôle de Poste";

        return Task.FromResult(result);
    }
}