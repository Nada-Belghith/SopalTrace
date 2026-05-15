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
                
                var perio = await _unitOfWork.DictionnaireQualiteRepository.GetPeriodiciteByLibelleAsync(parenthesesContent);
                if (perio != null)
                {
                    section.PeriodiciteId = perio.Id;
                    section.ModeFreq = "VARIABLE";
                    section.FrequenceLibelle = perio.Libelle;
                    section.FreqNum = perio.FrequenceNum ?? 1;

                    if (perio.FrequenceUnite == "1_HEURE" || perio.FrequenceUnite == "PCT_HEURE" || perio.FrequenceUnite == "4_HEURE")
                    {
                        section.TypeVariable = "HEURE";
                        section.FreqHours = (perio.FrequenceUnite == "4_HEURE") ? 4 : 1;
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
    }
}
