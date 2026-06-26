using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Interfaces;

namespace SopalTrace.Infrastructure.Services
{
    public class FrequencyParserService : IFrequencyParserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FrequencyParserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ParseFrequencyAsync(ImportExcelSectionDto section, string parenthesesContent)
        {
            if (string.IsNullOrEmpty(parenthesesContent))
                return;

            string contentLower = parenthesesContent.ToLower();

            // 1. Check if it's a known rule
            var regleMatch = await _unitOfWork.DictionnaireQualiteRepository.GetRegleEchantillonnageByLibelleAsync(parenthesesContent);

            bool isRule = regleMatch != null || 
                          contentLower.Contains("selon") || contentLower.Contains("iso") ||
                          contentLower.Contains("nqa") || contentLower.Contains("effectif") ||
                          contentLower.Contains("tableau") || contentLower.Contains("première") || 
                          contentLower.Contains("dernière") || contentLower.Contains("contrôle renforcé") ||
                          contentLower.Contains("plan d'échantillonnage");

            bool isFrequency = !isRule && (contentLower.Contains("pièce") || contentLower.Contains("piece") ||
                                           contentLower.Contains("p/h") || contentLower.Contains("p/") ||
                                           contentLower.Contains("série") || contentLower.Contains("serie") ||
                                           contentLower.Contains("lot") || contentLower.Contains("%") ||
                                           contentLower.Contains("échantillon") || contentLower.Contains("echantillon"));

            if (isRule)
            {
                var regle = await _unitOfWork.DictionnaireQualiteRepository.GetRegleEchantillonnageByLibelleAsync(parenthesesContent);
                string finalFreqLib = parenthesesContent;
                if (regle != null)
                {
                    section.RegleEchantillonnageId = regle.Id;
                    finalFreqLib = regle.Libelle;
                }
                
                section.ModeFreq = "FIXE";
                section.FrequenceLibelle = finalFreqLib;
                section.RegleEchantillonnageLibelle = finalFreqLib;
                
                section.LibelleSection += $" ({finalFreqLib})";
            }
            else if (isFrequency)
            {
                section.FrequenceLibelle = parenthesesContent;
                section.RegleEchantillonnageLibelle = parenthesesContent;

                var perioId = await ResolveOrCreatePeriodiciteFromTextAsync(parenthesesContent);
                if (perioId.HasValue)
                {
                    section.PeriodiciteId = perioId.Value;
                    var perio = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(parenthesesContent);
                    if (perio == null)
                    {
                        var allPerios = await _unitOfWork.DictionnaireQualiteRepository.GetAllPeriodicitesAsync();
                        perio = allPerios.FirstOrDefault(p => p.Id == perioId.Value);
                    }

                    if (perio != null)
                    {
                        section.ModeFreq = "VARIABLE";
                        section.FrequenceLibelle = perio.Libelle;
                        section.FreqNum = perio.FrequenceNum ?? 1;

                        if (perio.FrequenceUnite == "1_HEURE" || perio.FrequenceUnite == "PCT_HEURE" || perio.FrequenceUnite == "4_HEURE" || (perio.FrequenceUnite != null && perio.FrequenceUnite.EndsWith("_HEURE")))
                        {
                            section.TypeVariable = "HEURE";
                            if (perio.FrequenceUnite != null && perio.FrequenceUnite.Contains("_HEURE") && int.TryParse(perio.FrequenceUnite.Split('_')[0], out int h))
                            {
                                section.FreqHours = h;
                            }
                            else
                            {
                                section.FreqHours = (perio.FrequenceUnite == "4_HEURE") ? 4 : 1;
                            }
                        }
                        else if (perio.FrequenceUnite == "SERIE")
                        {
                            section.TypeVariable = "SERIE";
                        }
                        else if (perio.FrequenceUnite == "ECHANTILLON")
                        {
                            section.TypeVariable = "ECHANTILLON";
                        }
                    }
                }
                else
                {
                    section.ModeFreq = "VARIABLE";
                    
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
                
                section.LibelleSection += $" ({parenthesesContent})";
            }
            else
            {
                section.LibelleSection += $" ({parenthesesContent})";
            }
        }

        public async Task<System.Guid?> ResolveOrCreatePeriodiciteFromTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // 1. Extract parentheses content
            string contentToParse = text.Trim();
            var parenMatch = Regex.Match(text, @"\(([^()]+)\)");
            if (parenMatch.Success)
            {
                contentToParse = parenMatch.Groups[1].Value.Trim();
            }

            string contentLower = contentToParse.ToLower();

            // 2. Check if it's a rule (ISO, NQA, etc.)
            var regleMatch = await _unitOfWork.DictionnaireQualiteRepository.GetRegleEchantillonnageByLibelleAsync(contentToParse);
            bool isRule = regleMatch != null || 
                          contentLower.Contains("selon") || contentLower.Contains("iso") ||
                          contentLower.Contains("nqa") || contentLower.Contains("effectif") ||
                          contentLower.Contains("tableau") || contentLower.Contains("première") || 
                          contentLower.Contains("dernière") || contentLower.Contains("contrôle renforcé") ||
                          contentLower.Contains("plan d'échantillonnage");

            if (isRule)
            {
                return null;
            }

            // 3. Check if it exists as-is in the database
            var perio = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(contentToParse);
            if (perio != null)
            {
                return perio.Id;
            }

            // 4. Try parsing frequency details
            bool isFrequency = contentLower.Contains("pièce") || contentLower.Contains("piece") ||
                               contentLower.Contains("p/h") || contentLower.Contains("p/") ||
                               contentLower.Contains("série") || contentLower.Contains("serie") ||
                               contentLower.Contains("lot") || contentLower.Contains("%") ||
                               contentLower.Contains("échantillon") || contentLower.Contains("echantillon");

            if (!isFrequency)
            {
                return null;
            }

            int freqNum = 1;
            int freqHours = 1;
            string typeVariable = "HEURE";

            if ((contentLower.Contains("pièce") || contentLower.Contains("piece") || contentLower.Contains("p/h") || contentLower.Contains("p/")) &&
                (contentLower.Contains("heure") || contentLower.Contains("h")))
            {
                typeVariable = "HEURE";
                var m = Regex.Match(contentLower, @"(\d+)(?:%)?.*?(?:pièce|piece).*?/\s*(?:(\d+)\s*)?(?:heure|h\b)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    freqNum = int.Parse(m.Groups[1].Value);
                    freqHours = string.IsNullOrEmpty(m.Groups[2].Value) ? 1 : int.Parse(m.Groups[2].Value);
                }
            }
            else if (contentLower.Contains("série") || contentLower.Contains("serie") || contentLower.Contains("lot"))
            {
                typeVariable = "SERIE";
                var mS = Regex.Match(contentLower, @"(?:série|serie|lot).*?(\d+)\s*(?:pièce|piece|p)?", RegexOptions.IgnoreCase);
                if (!mS.Success) mS = Regex.Match(contentLower, @"(\d+)\s*(?:pièce|piece|p)", RegexOptions.IgnoreCase);
                if (mS.Success) freqNum = int.Parse(mS.Groups[1].Value);
            }
            else if (contentLower.Contains("échantillon") || contentLower.Contains("echantillon"))
            {
                typeVariable = "ECHANTILLON";
                var mE = Regex.Match(contentLower, @"(?:échantillons|echantillons|échantillon|echantillon).*?(\d+)", RegexOptions.IgnoreCase);
                if (!mE.Success) mE = Regex.Match(contentLower, @"(\d+)\s*(?:échantillons|echantillons|échantillon|echantillon)", RegexOptions.IgnoreCase);
                if (mE.Success) freqNum = int.Parse(mE.Groups[1].Value);
            }
            else
            {
                return null;
            }

            // Build standardized Code and Libelle
            string code = "";
            string libelle = "";
            string frequenceUnite = "";

            if (typeVariable == "HEURE")
            {
                if (freqNum == 100)
                {
                    code = "100PCT_1H";
                    libelle = "100% des pièces/h";
                    frequenceUnite = "PCT_HEURE";
                }
                else
                {
                    code = $"{freqNum}P_{freqHours}H";
                    string pluralP = freqNum > 1 ? "s" : "";
                    if (freqHours == 1)
                    {
                        libelle = $"{freqNum} pièce{pluralP} / heure";
                        frequenceUnite = "1_HEURE";
                    }
                    else
                    {
                        string pluralH = freqHours > 1 ? "s" : "";
                        libelle = $"{freqNum} pièce{pluralP} / {freqHours} heure{pluralH}";
                        frequenceUnite = $"{freqHours}_HEURE";
                    }
                }
            }
            else if (typeVariable == "SERIE")
            {
                code = $"SERIE_{freqNum}P";
                libelle = $"une série de {freqNum} pièces";
                frequenceUnite = "SERIE";
            }
            else if (typeVariable == "ECHANTILLON")
            {
                code = $"ECH_{freqNum}";
                string pluralE = freqNum > 1 ? "s" : "";
                libelle = $"{freqNum} échantillon{pluralE}";
                frequenceUnite = "ECHANTILLON";
            }

            // Look up by Code/Libelle again to avoid duplication
            var existingPerio = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByCodeAsync(code);
            if (existingPerio == null)
            {
                existingPerio = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(libelle);
            }

            if (existingPerio != null)
            {
                return existingPerio.Id;
            }

            // Create new
            var newPerio = new SopalTrace.Domain.Entities.Periodicite
            {
                Id = System.Guid.NewGuid(),
                Code = code,
                Libelle = libelle,
                FrequenceNum = freqNum,
                FrequenceUnite = frequenceUnite,
                OrdreAffichage = 5,
                Actif = true
            };

            await _unitOfWork.DictionnaireQualiteRepository.AddPeriodiciteAsync(newPerio);
            await _unitOfWork.CommitAsync();

            return newPerio.Id;
        }
    }
}
