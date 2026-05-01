using ClosedXML.Excel;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExcelImportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private string SafeSubstring(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength);
    }

    private string SafeGetCellValue(IXLCell cell)
    {
        try
        {
            var value = cell.Value.ToString().Trim().Replace("\n", " ");
            return string.IsNullOrWhiteSpace(value) ? "" : value;
        }
        catch
        {
            return "";
        }
    }

    public async Task<ImportExcelResultDto> ParsePlanExcelAsync(Stream excelStream)
    {
        var result = new ImportExcelResultDto();

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new Exception("Le fichier Excel est vide ou invalide.");

        var rows = worksheet.RowsUsed().ToList();
        if (!rows.Any()) return result;

        ImportExcelSectionDto currentSection = null;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            string colA = row.Cell(1).GetString().Trim();
            string colB = row.Cell(2).GetString().Trim();
            string colC = row.Cell(3).GetString().Trim();

            // S'il n'y a rien dans A, on passe
            if (string.IsNullOrEmpty(colA)) continue;

            // Vérifier si c'est une ligne de titre des colonnes "Caractéristique..." ou "Code", etc.
            // On ignore ces lignes de titres pour ne pas les importer ni en section ni en ligne
            if (colA.Equals("Caractéristique contrôlée", StringComparison.OrdinalIgnoreCase) ||
                colA.Equals("Caractéristiques", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Une SECTION Excel est définie par :
            // La colonne A contient du texte (le titre de la section)
            // MAIS les colonnes B, C et D sont vides (la ligne n'est pas divisée en colonnes, 
            // tout le contenu est fusionné ou juste écrit dans A)
            if (string.IsNullOrEmpty(colB) && string.IsNullOrEmpty(colC))
            {
                var parsedSection = await ParseSectionHeaderAsync(colA);

                var existingSection = result.Sections.FirstOrDefault(s => 
                    s.Nom.Equals(parsedSection.Nom, StringComparison.OrdinalIgnoreCase) && 
                    s.FrequenceLibelle == parsedSection.FrequenceLibelle);

                if (existingSection != null)
                {
                    currentSection = existingSection;
                }
                else
                {
                    currentSection = parsedSection;
                    result.Sections.Add(currentSection);
                }
                continue;
            }

            // TOUT LE RESTE est considéré comme une LIGNE de données si on a déjà une section en cours
            // (La ligne a du contenu en A, et la ligne a été "divisée" puisqu'elle a du contenu en B ou C etc.)
            if (currentSection != null)
            {
                var ligne = await ParseLigneAsync(row);
                currentSection.Lignes.Add(ligne);
            }
        }

        return result;
    }

    private async Task<ImportExcelSectionDto> ParseSectionHeaderAsync(string text)
    {
        var section = new ImportExcelSectionDto();
        string cleanText = text.Replace("\n", " ").Replace("\r", " ").Trim();

        var matchFreq = Regex.Match(cleanText, @"\((.*?)\)");
        if (matchFreq.Success)
        {
            string freqStr = matchFreq.Groups[1].Value.Trim().ToLower();
            section.FrequenceLibelle = freqStr;
            section.Nom = cleanText.Replace(matchFreq.Value, "").Trim();

            if (freqStr.Contains("pièce") && freqStr.Contains("heure"))
            {
                section.ModeFreq = "VARIABLE";
                section.TypeVariable = "HEURE";
                var m = Regex.Match(freqStr, @"(\d+)\s*pièce.*\/\s*(\d+)\s*heure");
                if (m.Success)
                {
                    section.FreqNum = int.Parse(m.Groups[1].Value);
                    section.FreqHours = int.Parse(m.Groups[2].Value);
                }
            }
            else if (freqStr.Contains("série"))
            {
                section.ModeFreq = "VARIABLE";
                section.TypeVariable = "SERIE";
                var m = Regex.Match(freqStr, @"série.*(\d+)\s*pièce");
                if (m.Success) section.FreqNum = int.Parse(m.Groups[1].Value);
            }
            else
            {
                var per = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(freqStr);
                if (per != null)
                {
                    section.ModeFreq = "FIXE";
                    section.PeriodiciteId = per.Id;
                }
            }
        }
        else
        {
            section.Nom = cleanText;
        }

        // Vérifier si TypeSection existe déjà - ne pas la créer si elle existe
        var typeSec = await _unitOfWork.DictionnaireQualiteRepository.GetTypeSectionByLibelleAsync(section.Nom);
        if (typeSec == null)
        {
            typeSec = new TypeSection { 
                Id = Guid.NewGuid(), 
                Libelle = SafeSubstring(section.Nom, 100), 
                Code = $"EXC-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Actif = true
            };
            await _unitOfWork.DictionnaireQualiteRepository.AddTypeSectionAsync(typeSec);
            await _unitOfWork.CommitAsync();
        }
        section.TypeSectionId = typeSec.Id;

        return section;
    }

    private async Task<ImportExcelLigneDto> ParseLigneAsync(IXLRow row)
    {
        var ligne = new ImportExcelLigneDto();

        ligne.LibelleAffiche = SafeGetCellValue(row.Cell(1));
        string limiteSpec = SafeGetCellValue(row.Cell(2));
        string typeControle = SafeGetCellValue(row.Cell(3));
        string moyenControle = SafeGetCellValue(row.Cell(4));
        ligne.InstrumentCode = SafeGetCellValue(row.Cell(5));

        // Chercher les observations dans les colonnes 6, 7, 8... (au cas où elles seraient décalées)
        ligne.Observations = SafeGetCellValue(row.Cell(6));
        if (string.IsNullOrEmpty(ligne.Observations))
        {
            ligne.Observations = SafeGetCellValue(row.Cell(7));
        }
        if (string.IsNullOrEmpty(ligne.Observations))
        {
            ligne.Observations = SafeGetCellValue(row.Cell(8));
        }

        if (!string.IsNullOrEmpty(ligne.LibelleAffiche))
        {
            var typeCarac = await _unitOfWork.DictionnaireQualiteRepository.GetTypeCaracteristiqueByLibelleAsync(ligne.LibelleAffiche);
            if (typeCarac == null)
            {
                typeCarac = new TypeCaracteristique { 
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
                typeC = new TypeControle { 
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
                moyenC = new MoyenControle { 
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

        ligne.LimiteSpecTexte = limiteSpec;

        var matchPM = Regex.Match(limiteSpec, @"([0-9.,-]+)\s*(?:±|\+/-)\s*([0-9.,-]+)");
        if (matchPM.Success)
        {
            if (decimal.TryParse(matchPM.Groups[1].Value.Replace(",", "."), out decimal valNom)) ligne.ValeurNominale = valNom;
            if (decimal.TryParse(matchPM.Groups[2].Value.Replace(",", "."), out decimal tol))
            {
                ligne.ToleranceSuperieure = ligne.ValeurNominale.HasValue ? ligne.ValeurNominale.Value + Math.Abs(tol) : tol;
                ligne.ToleranceInferieure = ligne.ValeurNominale.HasValue ? ligne.ValeurNominale.Value - Math.Abs(tol) : -tol;
            }
            ligne.LimiteSpecTexte = string.Empty;
        }
        else if (limiteSpec.Contains(";"))
        {
            var parts = limiteSpec.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (decimal.TryParse(parts[0].Trim().Replace(",", "."), out decimal val1) && 
                    decimal.TryParse(parts[1].Trim().Replace(",", "."), out decimal val2))
                {
                    ligne.ToleranceInferieure = Math.Min(val1, val2);
                    ligne.ToleranceSuperieure = Math.Max(val1, val2);
                    ligne.LimiteSpecTexte = string.Empty;
                }
            }
        }
        else
        {
            if (decimal.TryParse(limiteSpec.Trim().Replace(",", "."), out decimal singleVal))
            {
                ligne.ValeurNominale = singleVal;
                ligne.LimiteSpecTexte = string.Empty;
            }
        }

        return ligne;
    }
}
