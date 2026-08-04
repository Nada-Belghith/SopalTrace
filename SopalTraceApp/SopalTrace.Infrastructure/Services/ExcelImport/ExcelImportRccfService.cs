using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using SopalTrace.Application.DTOs.QualityPlans.PlanRCCF;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Infrastructure.Services.ExcelImport
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
        private readonly IUnitOfWork _unitOfWork;

        public ExcelImportRccfService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

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
                        string searchLibelle = "";
                        Guid? typeSectionId = null;
                        Guid? periodiciteId = null;
                        Guid? regleId = null;
                        string modeFreq = "SANS";
                        int? freqNum = null;
                        string? typeVar = null;
                        int? freqHours = null;
                        string? regleLibelle = null;

                        if (currentSectionKey == "REGLAGE")
                        {
                            libelle = "Caractéristiques à contrôler aux réglages (une série de 04 pièces)";
                            searchLibelle = "aux réglages";
                            modeFreq = "VARIABLE";
                            freqNum = 4;
                            typeVar = "SERIE";
                            freqHours = 1;
                        }
                        else if (currentSectionKey == "TRANCHES")
                        {
                            libelle = "Caractéristiques à contrôler par échantillonnage en cours de production";
                            searchLibelle = "par échantillonnage";
                            modeFreq = "FIXE";
                            regleLibelle = "Selon FE0591 : Effectif de l’échantillon /poste (A/B) (p/h)";
                        }
                        else if (currentSectionKey == "LOT_POSTE")
                        {
                            libelle = "Caractéristiques à contrôler au niveau du POSTE";
                            searchLibelle = "au niveau du poste";
                            modeFreq = "FIXE";
                            regleLibelle = "Caractéristiques à contrôler au niveau du POSTE";
                        }

                        // Résoudre TypeSectionId depuis la DB
                        var ts = await _unitOfWork.DictionnaireQualiteRepository.GetTypeSectionByLibelleAsync(searchLibelle);
                        if (ts != null) typeSectionId = ts.Id;

                        // Résoudre PeriodiciteId ou RegleEchantillonnageId
                        if (modeFreq == "VARIABLE")
                        {
                            var per = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync("une série de 4 pièces");
                            if (per == null) per = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync("une série de 5 pièces");
                            if (per != null) periodiciteId = per.Id;
                        }
                        else if (modeFreq == "FIXE" && !string.IsNullOrEmpty(regleLibelle))
                        {
                            var reg = await _unitOfWork.DictionnaireQualiteRepository.GetRegleEchantillonnageByLibelleAsync(regleLibelle);
                            if (reg != null) regleId = reg.Id;
                        }

                        sectionsDict[currentSectionKey] = new CreatePlanRccfSectionRequest
                        {
                            SectionType = currentSectionKey,
                            LibelleAffiche = libelle,
                            LibelleSection = libelle,
                            Nom = libelle,
                            OrdreAffiche = sectionsDict.Count + 1,
                            TypeSectionId = typeSectionId,
                            PeriodiciteId = periodiciteId,
                            RegleEchantillonnageId = regleId,
                            RegleEchantillonnageLibelle = regleLibelle,
                            FrequenceLibelle = regleLibelle,
                            ModeFreq = modeFreq,
                            FreqNum = freqNum,
                            TypeVariable = typeVar,
                            FreqHours = freqHours,
                            Lignes = new List<CreatePlanRccfLigneRequest>()
                        };
                    }

                    // Pour TRANCHES, si la grille horaire est laissée pour l'opérateur, on saute
                    if (currentSectionKey == "TRANCHES")
                    {
                        continue;
                    }

                    // Lire les colonnes
                    var limite = worksheet.Cells[row, 2].Text?.Trim() ?? "";
                    var typeCtrl = worksheet.Cells[row, 3].Text?.Trim() ?? "";
                    var moyenCtrl = worksheet.Cells[row, 4].Text?.Trim() ?? "";

                    var obs = worksheet.Cells[row, 6].Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(obs)) obs = worksheet.Cells[row, 7].Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(obs)) obs = worksheet.Cells[row, 8].Text?.Trim() ?? "";

                    // Détecter si c'est une remarque ou une note
                    if (lowerValue.StartsWith("nb") || lowerValue.StartsWith("remarque") || lowerValue.StartsWith("note") || lowerValue.StartsWith("observation"))
                    {
                        result.Remarques += cellValue.Trim() + "\n";
                        continue;
                    }

                    // Résolution des IDs de dictionnaire pour la ligne
                    Guid? typeCaracId = null;
                    string? typeCaracLibelle = null;
                    Guid? typeCtrlId = null;
                    string? typeCtrlLibelle = null;
                    Guid? moyenCtrlId = null;
                    string? moyenCtrlLibelle = null;
                    string? instrumentCode = null;

                    string caracName = cellValue.Trim();
                    if (!string.IsNullOrEmpty(caracName))
                    {
                        var tc = await _unitOfWork.DictionnaireQualiteRepository.GetTypeCaracteristiqueByLibelleAsync(caracName);
                        if (tc == null)
                        {
                            tc = new TypeCaracteristique { Id = Guid.NewGuid(), Libelle = caracName, Actif = true };
                            await _unitOfWork.DictionnaireQualiteRepository.AddTypeCaracteristiqueAsync(tc);
                        }
                        typeCaracId = tc.Id;
                        typeCaracLibelle = tc.Libelle;
                    }

                    if (!string.IsNullOrEmpty(typeCtrl))
                    {
                        var tctrl = await _unitOfWork.DictionnaireQualiteRepository.GetTypeControleByLibelleAsync(typeCtrl);
                        if (tctrl == null)
                        {
                            tctrl = new TypeControle { Id = Guid.NewGuid(), Libelle = typeCtrl, Actif = true };
                            await _unitOfWork.DictionnaireQualiteRepository.AddTypeControleAsync(tctrl);
                        }
                        typeCtrlId = tctrl.Id;
                        typeCtrlLibelle = tctrl.Libelle;
                    }

                    if (!string.IsNullOrEmpty(moyenCtrl))
                    {
                        var inst = await _unitOfWork.DictionnaireQualiteRepository.GetInstrumentByCodeAsync(moyenCtrl);
                        if (inst != null)
                        {
                            instrumentCode = inst.CodeInstrument;
                        }
                        else
                        {
                            var mc = await _unitOfWork.DictionnaireQualiteRepository.GetMoyenControleByLibelleAsync(moyenCtrl);
                            if (mc == null)
                            {
                                mc = new MoyenControle { Id = Guid.NewGuid(), Libelle = moyenCtrl, Actif = true };
                                await _unitOfWork.DictionnaireQualiteRepository.AddMoyenControleAsync(mc);
                            }
                            moyenCtrlId = mc.Id;
                            moyenCtrlLibelle = mc.Libelle;
                        }
                    }

                    sectionsDict[currentSectionKey].Lignes.Add(new CreatePlanRccfLigneRequest
                    {
                        Caracteristique = caracName,
                        LibelleAffiche = caracName,
                        TypeCaracteristiqueId = typeCaracId,
                        TypeCaracteristiqueLibelle = typeCaracLibelle,
                        TypeControleId = typeCtrlId,
                        TypeControleLibelle = typeCtrlLibelle,
                        MoyenControleId = moyenCtrlId,
                        MoyenControleLibelle = moyenCtrlLibelle,
                        InstrumentCode = instrumentCode,
                        LimiteSpecTexte = limite,
                        Observations = obs,
                        OrdreAffiche = currentOrdre++
                    });
                }
            }

            await _unitOfWork.CommitAsync();

            result.Sections = sectionsDict.Values.ToList();
            return result;
        }
    }
}
