using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SopalTrace.Application.DTOs.QualityPlans.PlanRCCF;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
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

            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault()
                    ?? throw new Domain.Exceptions.RegleMetierException("Le fichier Excel ne contient aucune feuille.");
                int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                int currentOrdre = 1;
                string currentSectionKey = "REGLAGE";

                for (int row = 1; row <= rowCount; row++)
                {
                    var cellValue = Texte(worksheet, row, 1);
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
                        string typeNormalise = "";
                        Guid? typeSectionId = null;
                        Guid? periodiciteId = null;
                        string modeFreq = "SANS";
                        int? freqNum = null;
                        string? typeVar = null;
                        int? freqHours = null;
                        // Précision affichée avec le titre : la règle de contrôle est portée par le type de section.
                        string? precision = null;

                        if (currentSectionKey == "REGLAGE")
                        {
                            libelle = "Caractéristiques à contrôler aux réglages (une série de 04 pièces)";
                            typeNormalise = SectionTypes.Reglage;
                            modeFreq = "VARIABLE";
                            freqNum = 4;
                            typeVar = "SERIE";
                            freqHours = 1;
                        }
                        else if (currentSectionKey == "TRANCHES")
                        {
                            libelle = "Caractéristiques à contrôler par échantillonnage en cours de production";
                            typeNormalise = SectionTypes.Echantillonnage2859;
                            precision = "Selon FE0591 : Effectif de l’échantillon /poste (A/B) (p/h)";
                        }
                        else if (currentSectionKey == "LOT_POSTE")
                        {
                            libelle = "Caractéristiques à contrôler au niveau du POSTE";
                            typeNormalise = SectionTypes.LotPoste;
                        }

                        // Résoudre TypeSectionId depuis la DB par TypeNormalise
                        var allTs = await _unitOfWork.DictionnaireQualiteRepository.GetAllTypeSectionsAsync();
                        var ts = allTs.FirstOrDefault(t => t.TypeNormalise == typeNormalise);
                        if (ts != null) typeSectionId = ts.Id;

                        // Résoudre PeriodiciteId
                        if (modeFreq == "VARIABLE")
                        {
                            var allPerios = await _unitOfWork.DictionnaireQualiteRepository.GetAllPeriodicitesAsync();
                            var per = allPerios.FirstOrDefault(p => p.NbrPieces == 4 && p.Unite == "SERIE");
                            if (per == null) per = allPerios.FirstOrDefault(p => p.NbrPieces == 5 && p.Unite == "SERIE");
                            if (per != null) periodiciteId = per.Id;
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
                            FrequenceLibelle = precision,
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
                    var limite = Texte(worksheet, row, 2);
                    var typeCtrl = Texte(worksheet, row, 3);
                    var moyenCtrl = Texte(worksheet, row, 4);

                    var obs = Texte(worksheet, row, 6);
                    if (string.IsNullOrWhiteSpace(obs)) obs = Texte(worksheet, row, 7);
                    if (string.IsNullOrWhiteSpace(obs)) obs = Texte(worksheet, row, 8);

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
                            tc = new TypeCaracteristique { Id = Guid.NewGuid(), Code = CodeImporte(), Libelle = caracName, Actif = true };
                            await _unitOfWork.DictionnaireQualiteRepository.AddTypeCaracteristiqueAsync(tc);
                            await _unitOfWork.CommitAsync();
                        }
                        typeCaracId = tc.Id;
                        typeCaracLibelle = tc.Libelle;
                    }

                    if (!string.IsNullOrEmpty(typeCtrl))
                    {
                        var tctrl = await _unitOfWork.DictionnaireQualiteRepository.GetTypeControleByLibelleAsync(typeCtrl);
                        if (tctrl == null)
                        {
                            tctrl = new TypeControle { Id = Guid.NewGuid(), Code = CodeImporte(), Libelle = typeCtrl, Actif = true };
                            await _unitOfWork.DictionnaireQualiteRepository.AddTypeControleAsync(tctrl);
                            await _unitOfWork.CommitAsync();
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
                                mc = new MoyenControle { Id = Guid.NewGuid(), Code = CodeImporte(), Libelle = moyenCtrl, Actif = true };
                                await _unitOfWork.DictionnaireQualiteRepository.AddMoyenControleAsync(mc);
                                await _unitOfWork.CommitAsync();
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

        /// <summary>Texte affiché de la cellule (valeur formatée, comme dans Excel), sans espaces autour.</summary>
        private static string Texte(IXLWorksheet feuille, int ligne, int colonne) =>
            feuille.Cell(ligne, colonne).GetFormattedString()?.Trim() ?? "";

        /// <summary>Code unique d'un élément de dictionnaire créé par l'import (même règle que l'import des plans).</summary>
        private static string CodeImporte() => $"EXC-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
    }
}
