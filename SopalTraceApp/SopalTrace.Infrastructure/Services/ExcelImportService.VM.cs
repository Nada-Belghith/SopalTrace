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
    protected virtual VerifMachineColumnMapping BuildVMMap(List<IXLRow> rows, int startIdx, string configurationColonnesJson = null)
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
                else if (MatchesAny(val, TypeKeywords)) 
                {
                    if (map.MethodeCol == 0) map.MethodeCol = cNum;
                }
                else if (MatchesAny(val, PerioKeywords)) 
                {
                    if (map.PerioCol == 0) map.PerioCol = cNum;
                }
                else if (!val.Contains("num") && (val == "moyendedetection" || val == "moyendecontrole" || (MatchesAny(val, MoyenKeywords) && MatchesAny(val, ControleKeywords)))) 
                {
                    if (map.MoyenDetCol == null) map.MoyenDetCol = cNum;
                }
                else if (val.Contains("fuite") && val.Contains("etalon")) 
                {
                    if (map.FuiteCol == null) map.FuiteCol = cNum;
                }
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
            foreach (var c in r.CellsUsed())
            {
                string fVal = SafeGetCellValue(c);
                string normFVal = NormalizeForSearch(fVal);
                if (!string.IsNullOrWhiteSpace(fVal) && (normFVal.Contains("famille") || normFVal.Contains("corps") || Regex.IsMatch(fVal, @"^\s*\(\s*\d+")))
                {
                    map.Familles[c.Address.ColumnNumber] = fVal;
                    map.HeaderBottomRow = r.RowNumber();
                    foundFamiliesInThisRow = true;
                }
            }
            if (foundFamiliesInThisRow) break;
        }
        return map;
    }

    public virtual async Task<ImportVerifMachineExcelResultDto> ParseVerifMachineExcelAsync(Stream excelStream, string fileName, string configurationColonnesJson = null)
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
            bool hasConformiteInitial = MatchesAny(txt, ConformiteKeywords) && !txt.Contains("nonconformit");
            if (MatchesAny(txt, CaractKeywords) && !hasConformiteInitial) inConformite = false;
            else if (hasConformiteInitial) inConformite = true;
        }


        var strategy = _factory.GetStrategy(result.MachineCode);
        
        int headerBottomIdx = rows.FindIndex(r => r.RowNumber() == map.HeaderBottomRow);
        if (headerBottomIdx == -1) headerBottomIdx = 0;
        
        int startDataRow = headerBottomIdx + 1;
        if (startDataRow >= rows.Count) startDataRow = rows.Count - 1;

        // On regroupe les lignes avec la stratégie
        var lignesLogiques = strategy.Regrouper(rows, startDataRow, rows.Count - 1, map.RisqueCol > 0 ? map.RisqueCol : 1);

        string lastSeenRisque = "";
        string lastSeenMethode = "";
        string lastSeenPerio = "";

        foreach (var ll in lignesLogiques)
        {
            var pRow = ll.Principale;
            string rawColA = SafeGetCellValue(pRow.Cell(map.RisqueCol > 0 ? map.RisqueCol : 1));
            string rawColB = SafeGetCellValue(pRow.Cell(map.MethodeCol > 0 ? map.MethodeCol : 2));
            string rawColC = SafeGetCellValue(pRow.Cell(map.PerioCol > 0 ? map.PerioCol : 3));

            string pFullText = string.Join(" ", pRow.CellsUsed().Select(c => SafeGetCellValue(c)).Where(v => !string.IsNullOrWhiteSpace(v)));
            string normalizedFullText = NormalizeForSearch(pFullText);

            if (normalizedFullText.Contains("rapport") && string.IsNullOrEmpty(result.NomPlan))
            {
                result.NomPlan = rawColA;
                var machineMatch = System.Text.RegularExpressions.Regex.Match(rawColA, @"([A-Z0-9-]{3,10})$");
                if (machineMatch.Success) result.MachineCode = machineMatch.Value;
                continue;
            }

            if (IsLigneLegendePieces(pFullText) ||
                normalizedFullText.Contains("validepar") ||
                normalizedFullText.Contains("matricule") ||
                normalizedFullText.Contains("signature"))
            {
                continue;
            }

            bool hasConformiteTitle = pRow.CellsUsed().Any(c => MatchesAny(NormalizeForSearch(SafeGetCellValue(c)), ConformiteKeywords) && !NormalizeForSearch(SafeGetCellValue(c)).Contains("nonconformit"));
            bool hasCaractTitle = pRow.CellsUsed().Any(c => MatchesAny(NormalizeForSearch(SafeGetCellValue(c)), CaractKeywords));

            if (hasConformiteTitle || hasCaractTitle)
            {
                if (hasCaractTitle && !hasConformiteTitle) inConformite = false;
                else if (hasConformiteTitle) inConformite = true;
                continue;
            }

            string normalizedColA = NormalizeForSearch(rawColA);
            if (normalizedColA.Contains("testdeconformit") ||
                normalizedColA.Contains("risque/defaut") ||
                normalizedColA.Contains("risquedefaut") ||
                normalizedColA.Contains("numerodelapiece")) continue;

            if (!string.IsNullOrWhiteSpace(rawColA)) lastSeenRisque = rawColA;
            if (!string.IsNullOrWhiteSpace(rawColB)) lastSeenMethode = rawColB;
            if (!string.IsNullOrWhiteSpace(rawColC)) lastSeenPerio = rawColC;

            string risquePrincipale = string.IsNullOrWhiteSpace(rawColA) ? lastSeenRisque : rawColA;

            // Constuire la liste complète des sous-lignes (incluant la ligne principale elle-même si elle a des données)
            var allSubRows = new List<Dictionary<int, string>>();
            
            var dictPrincipale = new Dictionary<int, string>();
            int lastCol = rows.Max(r => r.LastCellUsed()?.Address.ColumnNumber ?? 1);
            
            for (int colIdx = 1; colIdx <= lastCol; colIdx++)
            {
                var cell = pRow.Cell(colIdx);
                string valeur = string.Empty;

                if (cell.IsMerged())
                    valeur = cell.MergedRange().FirstCell().GetString().Trim();
                else
                    valeur = cell.GetString().Trim();

                dictPrincipale[colIdx] = valeur;
            }
            allSubRows.Add(dictPrincipale);
            allSubRows.AddRange(ll.SousLignes);

            foreach (var sRow in allSubRows)
            {
                string methode = sRow.TryGetValue(map.MethodeCol > 0 ? map.MethodeCol : 2, out string m) && !string.IsNullOrWhiteSpace(m) ? m : "";
                string perio = sRow.TryGetValue(map.PerioCol > 0 ? map.PerioCol : 3, out string p) && !string.IsNullOrWhiteSpace(p) ? p : "";
                string moyenDet = map.MoyenDetCol.HasValue && sRow.TryGetValue(map.MoyenDetCol.Value, out string md) ? md : "";

                bool hasPieceInRow = map.PieceRefStartCol > 0 && sRow.TryGetValue(map.PieceRefStartCol, out string pf) && !string.IsNullOrWhiteSpace(pf);
                
                bool hasCustomColumnInRow = false;
                foreach (var colIdx in map.CustomCols.Keys)
                {
                    if (sRow.TryGetValue(colIdx, out string cv) && !string.IsNullOrWhiteSpace(cv))
                    {
                        hasCustomColumnInRow = true; break;
                    }
                }

                if (sRow == dictPrincipale)
                {
                    if (string.IsNullOrWhiteSpace(risquePrincipale) && string.IsNullOrWhiteSpace(rawColB) && string.IsNullOrWhiteSpace(rawColC) && !hasPieceInRow && !hasCustomColumnInRow) continue;
                    
                    if (string.IsNullOrWhiteSpace(methode)) methode = string.IsNullOrWhiteSpace(rawColB) ? lastSeenMethode : rawColB;
                    if (string.IsNullOrWhiteSpace(perio)) perio = string.IsNullOrWhiteSpace(rawColC) ? lastSeenPerio : rawColC;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(methode) && string.IsNullOrWhiteSpace(perio) && !hasPieceInRow && !hasCustomColumnInRow) continue;
                    
                    if (string.IsNullOrWhiteSpace(methode)) methode = lastSeenMethode;
                    if (string.IsNullOrWhiteSpace(perio)) perio = lastSeenPerio;
                }

                if (!string.IsNullOrWhiteSpace(methode)) lastSeenMethode = methode;
                if (!string.IsNullOrWhiteSpace(perio)) lastSeenPerio = perio;

                var listToSearch = inConformite ? result.LignesConformite : result.LignesRisques;
                var currentLigne = listToSearch.LastOrDefault(l => l.LibelleRisque == (risquePrincipale ?? ""));

                bool needNewLigne = false;
                if (currentLigne == null) needNewLigne = true;
                else if (currentLigne.LibelleMethode != (methode ?? "")) needNewLigne = true;
                else
                {
                    foreach (var kvp in map.CustomCols)
                    {
                        if (sRow.TryGetValue(kvp.Key, out string cVal) && !string.IsNullOrWhiteSpace(cVal))
                        {
                            if (currentLigne.ColonnesSupplementaires.TryGetValue(kvp.Value, out string existingVal) && existingVal != cVal)
                            {
                                needNewLigne = true;
                                break;
                            }
                        }
                    }
                }

                if (needNewLigne)
                {
                    currentLigne = new ImportVerifMachineLigneDto
                    {
                        LibelleRisque = risquePrincipale ?? "",
                        LibelleMethode = methode ?? ""
                    };
                    if (inConformite) result.LignesConformite.Add(currentLigne);
                    else result.LignesRisques.Add(currentLigne);
                }

                if (map.CustomCols.Count > 0)
                {
                    foreach (var kvp in map.CustomCols)
                    {
                        if (sRow.TryGetValue(kvp.Key, out string cVal) && !string.IsNullOrWhiteSpace(cVal))
                        {
                            currentLigne.ColonnesSupplementaires[kvp.Value] = cVal;
                        }
                    }
                }

                var periodiciteLibelle = perio;
                ImportVerifMachineEcheanceDto echeance = null;
                if (!string.IsNullOrWhiteSpace(periodiciteLibelle))
                {
                    var existingEcheance = currentLigne.Echeances.FirstOrDefault(e => e.PeriodiciteLibelle == periodiciteLibelle);
                    
                    if (existingEcheance != null) echeance = existingEcheance;
                    else
                    {
                        echeance = new ImportVerifMachineEcheanceDto { PeriodiciteLibelle = periodiciteLibelle, Rows = new List<ImportVerifMachineRowDto>() };
                        
                        var normalizedPerio = periodiciteLibelle?.Trim();
                        if (!string.IsNullOrEmpty(normalizedPerio))
                        {
                            if (!_createdPerioCache.TryGetValue(normalizedPerio, out var periodicite))
                            {
                                periodicite = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(normalizedPerio);
                                if (periodicite == null)
                                {
                                    periodicite = new Periodicite { Id = Guid.NewGuid(), Code = SafeSubstring(normalizedPerio.Replace(" ", "").ToUpper(), 22) + normalizedPerio.GetHashCode().ToString("X").PadLeft(6, '0').Substring(0, 6), Libelle = SafeSubstring(normalizedPerio, 80), Actif = true };
                                    await _unitOfWork.DictionnaireQualiteRepository.AddPeriodiciteAsync(periodicite);
                                }
                                _createdPerioCache[normalizedPerio] = periodicite;
                            }
                            echeance.PeriodiciteId = periodicite.Id;
                        }
                        
                        currentLigne.Echeances.Add(echeance);
                    }

                    var lastRowDto = echeance.Rows.LastOrDefault();
                    if (lastRowDto == null || (!string.IsNullOrWhiteSpace(moyenDet) && lastRowDto.MoyenDetectionLibelle != moyenDet))
                    {
                        lastRowDto = new ImportVerifMachineRowDto { MoyenDetectionLibelle = moyenDet, MatricePieces = new List<ImportVerifMachineMatriceDto>() };
                        
                        if (!string.IsNullOrWhiteSpace(moyenDet)) {
                            var moyenDetObj = await GetOrCreateMoyenDetectionAsync(moyenDet);
                            lastRowDto.MoyenDetectionId = moyenDetObj.Id;
                        }
                        
                        echeance.Rows.Add(lastRowDto);
                    }

                    if (map.Familles.Count > 0)
                    {
                        foreach (var kvp in map.Familles)
                        {
                            int excelCol = kvp.Key;
                            if (sRow.TryGetValue(excelCol, out string pieceCode) && !string.IsNullOrWhiteSpace(pieceCode))
                            {
                                await ProcessPieceCellAsync(pieceCode, kvp.Value, lastRowDto, inConformite, moyenDet, result.MachineCode!);
                            }
                        }
                    }
                    else
                    {
                        int targetCol = map.PieceRefStartCol > 0 ? map.PieceRefStartCol : 4;
                        if (sRow.TryGetValue(targetCol, out string pieceCode) && !string.IsNullOrWhiteSpace(pieceCode))
                        {
                            await ProcessPieceCellAsync(pieceCode, "", lastRowDto, inConformite, moyenDet, result.MachineCode!);
                        }
                    }

                    if (map.FuiteCol.HasValue && sRow.TryGetValue(map.FuiteCol.Value, out string colFuite) && !string.IsNullOrWhiteSpace(colFuite))
                    {
                        string role = "FEC";
                        if (moyenDet.Contains("FENC", StringComparison.OrdinalIgnoreCase)) role = "FENC";
                        else if (!inConformite && !moyenDet.Contains("PRC")) role = "FENC";

                        var fuite = await GetOrCreatePieceRefAsync(colFuite.Trim(), role, result.MachineCode!);

                        lastRowDto.MatricePieces.Add(new ImportVerifMachineMatriceDto
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

    protected void ResetCaches()
    {
        _createdPerioCache.Clear();
        _createdPiecesCache.Clear();
        _createdFamiliesCache.Clear();
        _createdMoyensCache.Clear();
    }

    protected async Task ProcessPieceCellAsync(string pieceCode, string familleCode, ImportVerifMachineRowDto rowDto, bool inConformite, string colD, string machineCode)
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

    protected static bool IsLigneLegendePieces(string colA)
    {
        if (string.IsNullOrWhiteSpace(colA)) return false;
        int count = 0;
        if (Regex.IsMatch(colA, @"\bPRC\s*:", RegexOptions.IgnoreCase)) count++;
        if (Regex.IsMatch(colA, @"\bPRNC\s*:", RegexOptions.IgnoreCase)) count++;
        if (Regex.IsMatch(colA, @"\bFENC\s*:", RegexOptions.IgnoreCase)) count++;
        if (Regex.IsMatch(colA, @"\bFEC\s*:", RegexOptions.IgnoreCase)) count++;
        return count >= 2;
    }

    protected static string BuildLegendeMoyensFromImport(ImportVerifMachineExcelResultDto result)
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

    protected async Task<PieceReference> GetOrCreatePieceRefAsync(string code, string typePiece, string machineCode)
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

    protected async Task<RefFamilleCorp> GetOrCreateFamilleAsync(string header)
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

    protected async Task<RefMoyenDetection> GetOrCreateMoyenDetectionAsync(string libelle)
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
