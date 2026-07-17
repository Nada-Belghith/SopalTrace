using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public interface IIso2859Service
{
    Task<string?> GetLettreCodeAsync(int tailleLot, string niveauControle);
    Task<int?> GetEffectifEchantillonAsync(string lettreCode, double valeurNqa);
}
