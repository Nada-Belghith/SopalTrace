using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Infrastructure.Services
{
    public class FrequencyParserService : IFrequencyParserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FrequencyParserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Précision entre parenthèses d'un titre de section. Une référence de norme ou de procédure
        /// (« selon ISO 2859-1… », « Selon FE0591… », « 1ère et dernière pièce ») relève du type de section :
        /// elle reste dans le libellé, sans périodicité. Sinon, la périodicité est reconnue si possible.
        /// </summary>
        public async Task ParseFrequencyAsync(ImportExcelSectionDto section, string parenthesesContent)
        {
            if (string.IsNullOrEmpty(parenthesesContent))
                return;

            string contentLower = parenthesesContent.ToLower();
            section.FrequenceLibelle = parenthesesContent;

            if (EstReferenceDeNorme(contentLower))
                return;

            bool isFrequency = contentLower.Contains("pièce") || contentLower.Contains("piece") ||
                               contentLower.Contains("p/h") || contentLower.Contains("p/") ||
                               contentLower.Contains("série") || contentLower.Contains("serie") ||
                               contentLower.Contains("lot") || contentLower.Contains("%") ||
                               contentLower.Contains("échantillon") || contentLower.Contains("echantillon");
            if (!isFrequency)
                return;

            if (contentLower.Contains("100%"))
            {
                section.ModeFreq = "SANS";
                section.PeriodiciteId = null;
                var tousPiecesTs = (await _unitOfWork.DictionnaireQualiteRepository.GetAllTypeSectionsAsync())
                                    .FirstOrDefault(t => t.TypeNormalise == SopalTrace.Domain.Constants.SectionTypes.TousPieces);
                if (tousPiecesTs != null)
                    section.TypeSectionId = tousPiecesTs.Id;
                return;
            }

            var perioId = await ResolveOrCreatePeriodiciteFromTextAsync(parenthesesContent);
            if (perioId.HasValue)
            {
                section.PeriodiciteId = perioId.Value;
                var perio = (await _unitOfWork.DictionnaireQualiteRepository.GetAllPeriodicitesAsync())
                    .FirstOrDefault(p => p.Id == perioId.Value);
                if (perio != null)
                {
                    section.ModeFreq = "VARIABLE";
                    section.FrequenceLibelle = perio.Unite == "HEURE"
                        ? $"{perio.NbrPieces} pièce(s) / {perio.NbrHeures} heure(s)"
                        : perio.Unite == "SERIE" ? $"une série de {perio.NbrPieces} pièce(s)" : $"{perio.NbrPieces} échantillon(s)";
                    section.FreqNum = perio.NbrPieces;
                    section.TypeVariable = perio.Unite;
                    section.FreqHours = perio.NbrHeures ?? 1;
                }
            }
        }

        /// <summary>Texte décrivant une règle de contrôle (norme, procédure, lot) et non une fréquence.</summary>
        private static bool EstReferenceDeNorme(string contentLower) =>
            contentLower.Contains("selon") || contentLower.Contains("iso") ||
            contentLower.Contains("nqa") || contentLower.Contains("effectif") ||
            contentLower.Contains("tableau") || contentLower.Contains("première") ||
            contentLower.Contains("dernière") || contentLower.Contains("contrôle renforcé") ||
            contentLower.Contains("plan d'échantillonnage");

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

            // 2. Référence de norme ou 100 % : pas de périodicité
            if (EstReferenceDeNorme(contentLower) || contentLower.Contains("100%"))
            {
                return null;
            }

            // 3. Try parsing frequency details
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

            int? parsedNbrHeures = typeVariable == "HEURE" ? freqHours : null;

            // 4. Look up by new properties to avoid duplication
            var allPerios = await _unitOfWork.DictionnaireQualiteRepository.GetAllPeriodicitesAsync();
            var existingPerio = allPerios.FirstOrDefault(p => p.NbrPieces == freqNum && p.Unite == typeVariable && p.NbrHeures == parsedNbrHeures);

            if (existingPerio != null)
            {
                return existingPerio.Id;
            }

            // Create new
            var newPerio = new SopalTrace.Domain.Entities.Periodicite
            {
                Id = System.Guid.NewGuid(),
                NbrPieces = freqNum,
                Unite = typeVariable,
                NbrHeures = parsedNbrHeures,
                Actif = true
            };

            await _unitOfWork.DictionnaireQualiteRepository.AddPeriodiciteAsync(newPerio);
            await _unitOfWork.CommitAsync();

            return newPerio.Id;
        }
    }
}
