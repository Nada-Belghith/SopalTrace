using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using SopalTrace.Application.DTOs.QualityPlans.PlanRCCF;

namespace SopalTrace.Application.Services
{
    public class ImportRccfResultDto
    {
        public List<CreatePlanRccfSectionRequest> Sections { get; set; } = new();
        public string Remarques { get; set; } = string.Empty;
    }

    public interface IExcelImportRccfService
    {
        Task<ImportRccfResultDto> ImportAssemblageExcelAsync(Stream stream);
    }

    public class ExcelImportRccfService : IExcelImportRccfService
    {
        public async Task<ImportRccfResultDto> ImportAssemblageExcelAsync(Stream stream)
        {
            var result = new ImportRccfResultDto();
            var sectionsDict = new Dictionary<string, CreatePlanRccfSectionRequest>();

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                int currentOrdre = 1;
                
                string currentSectionKey = "REGLAGE";
                
                for (int row = 1; row <= rowCount; row++)
                {
                    var cellValue = worksheet.Cells[row, 1].Text;
                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                    var lowerValue = cellValue.ToLower().Trim();

                    // Détecter les en-têtes de section pour changer la clé courante
                    if (lowerValue.Contains("échantillonnage en cours") || lowerValue.Contains("selon fe0591"))
                    {
                        currentSectionKey = "TRANCHES";
                        currentOrdre = 1;
                        continue;
                    }
                    if (lowerValue.Contains("niveau du poste"))
                    {
                        currentSectionKey = "LOT_POSTE";
                        currentOrdre = 1;
                        continue;
                    }
                    if (lowerValue.Contains("aux réglages (une série"))
                    {
                        currentSectionKey = "REGLAGE";
                        currentOrdre = 1;
                        continue;
                    }

                    // Ignorer les en-têtes de colonnes
                    if (lowerValue == "caractéristiques contrôlées" || lowerValue == "fréquence" || lowerValue == "frequence")
                        continue;

                    // Initialiser la section dans le dictionnaire si non présente
                    if (!sectionsDict.ContainsKey(currentSectionKey))
                    {
                        string libelle = "";
                        if (currentSectionKey == "REGLAGE") libelle = "Caractéristiques à contrôler aux réglages (une série de 04 pièces)";
                        else if (currentSectionKey == "TRANCHES") libelle = "Caractéristiques à contrôler par échantillonnage en cours de production";
                        else if (currentSectionKey == "LOT_POSTE") libelle = "Caractéristiques à contrôler au niveau du POSTE";

                        sectionsDict[currentSectionKey] = new CreatePlanRccfSectionRequest
                        {
                            SectionType = currentSectionKey,
                            LibelleAffiche = libelle,
                            OrdreAffiche = sectionsDict.Count + 1,
                            Lignes = new List<CreatePlanRccfLigneRequest>()
                        };
                    }

                    // Pour TRANCHES, l'utilisateur a spécifié de laisser la grille vide pour l'opérateur (ne pas importer les lignes)
                    if (currentSectionKey == "TRANCHES")
                    {
                        continue;
                    }

                    // Lire les autres colonnes comme pour Plan Assemblage
                    var limite = worksheet.Cells[row, 2].Text?.Trim() ?? "";
                    var type = worksheet.Cells[row, 3].Text?.Trim() ?? "";
                    var moyen = worksheet.Cells[row, 4].Text?.Trim() ?? "";
                    
                    var obs = worksheet.Cells[row, 6].Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(obs)) obs = worksheet.Cells[row, 7].Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(obs)) obs = worksheet.Cells[row, 8].Text?.Trim() ?? "";

                    // Détecter si c'est une remarque ou une note
                    if (lowerValue.StartsWith("nb") || lowerValue.StartsWith("remarque") || lowerValue.StartsWith("note") || lowerValue.StartsWith("observation"))
                    {
                        result.Remarques += cellValue.Trim() + "\n";
                        continue;
                    }

                    // Ajouter la caractéristique à la section courante avec les détails
                    sectionsDict[currentSectionKey].Lignes.Add(new CreatePlanRccfLigneRequest
                    {
                        Caracteristique = cellValue.Trim(),
                        LimiteSpecTexte = limite,
                        Observations = obs,
                        OrdreAffiche = currentOrdre++
                    });
                }
            }

            result.Sections = sectionsDict.Values.ToList();
            return result;
        }
    }
}
