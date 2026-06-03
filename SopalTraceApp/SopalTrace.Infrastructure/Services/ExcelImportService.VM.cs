using ClosedXML.Excel;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services;

public partial class ExcelImportService
{

    // =========================================================================
    // VERIF MACHINE (inchangé)
    // =========================================================================
    private VerifMachineColumnMapping BuildVMMap(List<IXLRow> rows, int startIdx, string configurationColonnesJson = null)
    {
        var map = new VerifMachineColumnMapping();
        var searchRows = rows.Skip(startIdx).Take(8).ToList();
        
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
                                map.CustomColsDefinition[label] = key;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        foreach (var r in searchRows)
        {
            foreach (var c in r.CellsUsed())
            {
                string val = NormalizeForSearch(SafeGetCellValue(c));
                int cNum = c.Address.ColumnNumber;

                if ((MatchesAny(val, ConformiteKeywords) && !MatchesAny(val, ObsKeywords) && !val.Contains("non")) || MatchesAny(val, CaractKeywords)) 
                {
                    if (map.RisqueCol == 0) map.RisqueCol = cNum;
                }
                else if (MatchesAny(val, TypeKeywords)) map.MethodeCol = cNum;
                else if (MatchesAny(val, PerioKeywords)) map.PerioCol = cNum;
                else if (!val.Contains("num") && MatchesAny(val, MoyenKeywords) && MatchesAny(val, ControleKeywords)) map.MoyenDetCol = cNum;
                else if (val.Contains("fuite") && val.Contains("etalon")) map.FuiteCol = cNum;
                else if (val.Contains("num") || val.Contains("piece") || val.Contains("ref")) 
                {
                    if ((val.Contains("piec") && (val.Contains("num") || val.Contains("ref"))) ||
                        (val.Contains("num") && MatchesAny(val, MoyenKeywords) && MatchesAny(val, ControleKeywords)))
                    {
                        map.PieceRefStartCol = cNum;
                        if (c.IsMerged()) map.PieceRefEndCol = c.MergedRange().LastColumn().ColumnNumber();
                        else map.PieceRefEndCol = cNum;
                    }
                }
                else if (map.CustomColsDefinition.TryGetValue(val, out var customKey))
                {
                    map.CustomCols[cNum] = customKey;
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

    public async Task<ImportVerifMachineExcelResultDto> ParseVerifMachineExcelAsync(Stream excelStream, string fileName, string configurationColonnesJson = null)
    {
        ResetCaches();
        var result = new ImportVerifMachineExcelResultDto();
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new Exception("Le fichier Excel est vide ou invalide.");

        var fileNameMatch = Regex.Match(fileName, @"VM_([A-Z0-9-]+)", RegexOptions.IgnoreCase);
        if (fileNameMatch.Success) result.MachineCode = fileNameMatch.Groups[1].Value;

        var rows = worksheet.RowsUsed().ToList();
        var map = BuildVMMap(rows, 0, configurationColonnesJson);

        foreach (var fam in map.Familles.Values)
        {
            await GetOrCreateFamilleAsync(fam);
            result.Familles.Add(fam);
        }

        bool inConformite = false;
        // Détecter la section initiale à partir des lignes d'en-tête (qui seront sautées par la boucle principale)
        for (int j = 0; j <= map.HeaderBottomRow && j < rows.Count; j++)
        {
            string txt = NormalizeForSearch(string.Join(" ", rows[j].CellsUsed().Select(c => SafeGetCellValue(c))));
            if (MatchesAny(txt, CaractKeywords) && !MatchesAny(txt, ConformiteKeywords)) inConformite = false;
            else if (MatchesAny(txt, ConformiteKeywords)) inConformite = true;
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

            string normalizedFullText = NormalizeForSearch(rowFullText);

            if (normalizedFullText.Contains("rapport") && string.IsNullOrEmpty(result.NomPlan))
            {
                result.NomPlan = rawCol1;
                var machineMatch = Regex.Match(rawCol1, @"([A-Z0-9-]{3,10})$");
                if (machineMatch.Success) result.MachineCode = machineMatch.Value;
            }

            if (map.HeaderBottomRow > -1 && row.RowNumber() <= map.HeaderBottomRow) continue;

            // 1. DÉTECTION DE CHANGEMENT DE SECTION (Header)
            bool isSectionTitle = MatchesAny(normalizedFullText, ConformiteKeywords) || 
                                  MatchesAny(normalizedFullText, CaractKeywords);

            bool isHeaderRow = isSectionTitle || row.CellsUsed().Any(c => {
                var val = NormalizeForSearch(SafeGetCellValue(c));
                return MatchesAny(val, PerioKeywords) || 
                       MatchesAny(val, MoyenKeywords) || 
                       MatchesAny(val, TypeKeywords);
            });

            if (isHeaderRow)
            {
                if (MatchesAny(normalizedFullText, CaractKeywords) && !MatchesAny(normalizedFullText, ConformiteKeywords)) inConformite = false;
                else if (MatchesAny(normalizedFullText, ConformiteKeywords)) inConformite = true;
                
                currentLigne = null;
                
                // Vérifier si cette ligne contient les vraies colonnes
                bool hasColumns = row.CellsUsed().Any(c => 
                {
                    var val = NormalizeForSearch(SafeGetCellValue(c));
                    return MatchesAny(val, TypeKeywords) || MatchesAny(val, PerioKeywords);
                });

                if (hasColumns)
                {
                    // Recalcul dynamique des colonnes UNIQUEMENT si c'est la ligne des colonnes
                    map.MoyenDetCol = null;
                    map.PieceRefStartCol = 0;
                    map.FuiteCol = null;
                    map.CustomCols.Clear();

                    foreach (var c in row.CellsUsed())
                    {
                        string val = NormalizeForSearch(SafeGetCellValue(c));
                        if (MatchesAny(val, ConformiteKeywords) && !MatchesAny(val, ObsKeywords) && !val.Contains("non")) map.RisqueCol = c.Address.ColumnNumber;
                        else if (MatchesAny(val, CaractKeywords)) map.RisqueCol = c.Address.ColumnNumber;
                        else if (MatchesAny(val, TypeKeywords)) map.MethodeCol = c.Address.ColumnNumber;
                        else if (MatchesAny(val, PerioKeywords)) map.PerioCol = c.Address.ColumnNumber;
                        else if (!val.Contains("num") && MatchesAny(val, ControleKeywords)) map.MoyenDetCol = c.Address.ColumnNumber;
                        else if (val.Contains("num") || val.Contains("piece") || val.Contains("ref")) 
                        {
                            if ((val.Contains("piec") && (val.Contains("num") || val.Contains("ref"))) ||
                                (val.Contains("num") && MatchesAny(val, MoyenKeywords) && MatchesAny(val, ControleKeywords)))
                            {
                                map.PieceRefStartCol = c.Address.ColumnNumber;
                                if (c.IsMerged()) map.PieceRefEndCol = c.MergedRange().LastColumn().ColumnNumber();
                                else map.PieceRefEndCol = c.Address.ColumnNumber;
                            }
                        }
                        else if (val.Contains("fuite") && val.Contains("etalon")) map.FuiteCol = c.Address.ColumnNumber;
                        else if (map.CustomColsDefinition.TryGetValue(val, out var customKey))
                        {
                            map.CustomCols[c.Address.ColumnNumber] = customKey;
                        }
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
                normalizedFullText.Contains("validepar") ||
                normalizedFullText.Contains("matricule") ||
                normalizedFullText.Contains("signature"))
            {
                continue;
            }

            string normalizedColA = NormalizeForSearch(rawColA);
            if (normalizedColA.Contains("testdeconformit") ||
                normalizedColA.Contains("risque/defaut") ||
                normalizedColA.Contains("risquedefaut") ||
                normalizedColA.Contains("numerodelapiece")) continue;

            // 3. HÉRITAGE UNIVERSEL DES VALEURS (Architecture A ou B)
            if (!string.IsNullOrWhiteSpace(rawColA)) lastSeenRisque = rawColA;
            if (!string.IsNullOrWhiteSpace(rawColB)) lastSeenMethode = rawColB;
            if (!string.IsNullOrWhiteSpace(rawColC)) lastSeenPerio = rawColC;

            string colA = string.IsNullOrWhiteSpace(rawColA) ? lastSeenRisque : rawColA;
            string colB = string.IsNullOrWhiteSpace(rawColB) ? lastSeenMethode : rawColB;
            string colC = string.IsNullOrWhiteSpace(rawColC) ? lastSeenPerio : rawColC;

            // Une ligne est valide si elle a un libellé OU une période OU une pièce de référence OU une colonne personnalisée
            bool hasPieceInRow = map.PieceRefStartCol > 0 && !string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(map.PieceRefStartCol)));
            
            bool hasCustomColumnInRow = false;
            if (map.CustomCols.Count > 0)
            {
                foreach (var colIdx in map.CustomCols.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(SafeGetCellValue(row.Cell(colIdx))))
                    {
                        hasCustomColumnInRow = true;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(colA) && string.IsNullOrWhiteSpace(colB) && string.IsNullOrWhiteSpace(colC) && !hasPieceInRow && !hasCustomColumnInRow) continue;

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

                // Add or update custom columns values
                if (map.CustomCols.Count > 0)
                {
                    foreach (var kvp in map.CustomCols)
                    {
                        string cVal = SafeGetCellValue(row.Cell(kvp.Key));
                        if (!string.IsNullOrWhiteSpace(cVal))
                        {
                            currentLigne.ColonnesSupplementaires[kvp.Value] = cVal;
                        }
                    }
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
}
