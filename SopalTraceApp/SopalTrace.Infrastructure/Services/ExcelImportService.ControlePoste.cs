using ClosedXML.Excel;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services;

public partial class ExcelImportService
{
    // =========================================================================
    // PLAN NC - RÉSULTAT CONTRÔLE DE POSTE
    // Format réel du fichier Excel :
    //   Ligne titre : "Test de Non-conformité" (fusionnée, ignorée)
    //   Ligne en-tête : | N° | Machine / Banc d'essai | Désignation du défaut |
    //   Données : | 1 | MAS26 | ABSENCE/MAUVAIS MONTAGE JOINT ANTI-FUITE |
    // =========================================================================
    public Task<ImportNcExcelResultDto> ParseControlePosteExcelAsync(Stream excelStream, string fileName, string configurationColonnesJson = null)
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

            // Sauter lignes sans numéro en Col A (lignes de résumé, totaux, etc.)
            if (string.IsNullOrWhiteSpace(colA) || !int.TryParse(colA, out _)) continue;

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
