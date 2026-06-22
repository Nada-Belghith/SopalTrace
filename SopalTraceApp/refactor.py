import re

with open('SopalTrace.Infrastructure/Services/ExcelImportService.VM.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Define the start and end of the region to replace
start_idx = content.find('        ImportVerifMachineLigneDto? currentLigne = null;')
end_idx = content.find('        await _unitOfWork.CommitAsync();\n        result.LegendeMoyens = BuildLegendeMoyensFromImport(result);')

if start_idx == -1 or end_idx == -1:
    print('Could not find bounds! start:', start_idx, 'end:', end_idx)
    exit(1)

new_logic = '''
        var strategy = _factory.GetStrategy(result.MachineCode);
        int headerBottomRow = map.HeaderBottomRow > -1 ? map.HeaderBottomRow : 0;
        int startDataRow = headerBottomRow + 1;
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
            foreach (var cell in pRow.CellsUsed())
            {
                dictPrincipale[cell.Address.ColumnNumber] = cell.GetString().Trim();
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
'''

content = content[:start_idx] + new_logic + content[end_idx:]

with open('SopalTrace.Infrastructure/Services/ExcelImportService.VM.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print('File updated successfully.')
