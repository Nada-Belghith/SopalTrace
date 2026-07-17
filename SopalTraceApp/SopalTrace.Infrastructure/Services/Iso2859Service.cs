using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Application.Services;

namespace SopalTrace.Infrastructure.Services;

public class Iso2859Service : IIso2859Service
{
    private readonly SopalTraceDbContext _context;

    public Iso2859Service(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetLettreCodeAsync(int tailleLot, string niveauControle)
    {
        // Nettoyage éventuel du préfixe "NIVEAU " si le client envoie "NIVEAU II" au lieu de "II"
        var niveauPropre = niveauControle.ToUpper().Replace("NIVEAU ", "").Trim();

        var regle = await _context.RefIso2859LettresCodes
            .Where(x => x.NiveauControle == niveauPropre 
                     && x.QteMin <= tailleLot 
                     && x.QteMax >= tailleLot)
            .FirstOrDefaultAsync();

        return regle?.CodeLettre;
    }

    public async Task<int?> GetEffectifEchantillonAsync(string lettreCode, double valeurNqa)
    {
        var echantillonnage = await _context.RefIso2859Echantillonnages
            .Where(x => x.CodeLettre == lettreCode && x.ValeurNqa == valeurNqa)
            .FirstOrDefaultAsync();

        return echantillonnage?.Quantite;
    }
}
