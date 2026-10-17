using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Constants;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

/// <summary>
/// Formulaire d'une fiche de réponse (résultats du contrôle en cours d'assemblage ou de fabrication, registre) pour un
/// poste, une famille et un type de robinet.
/// </summary>
internal static class FormulairesResultats
{
    /// <summary>
    /// Formulaire en cours le plus précis du rôle : chaque critère renseigné (poste, famille, type de robinet) doit
    /// correspondre, et le plus de critères renseignés l'emporte. Null si aucun ne correspond ou si deux sont aussi précis.
    /// </summary>
    public static Task<Guid?> TrouverAsync(SopalTraceDbContext context, string? posteCode, string? familleCode, string? typeRobinetCode) =>
        TrouverAsync(context, RolesFormulaire.ResultatControleAssemblage, posteCode, familleCode, typeRobinetCode);

    public static async Task<Guid?> TrouverAsync(SopalTraceDbContext context, string role, string? posteCode, string? familleCode, string? typeRobinetCode)
    {
        var formulaires = await context.RefFormulaires
            .AsNoTracking()
            .Where(f => f.Role == role && f.Statut != StatutsPlan.Archive
                        && (f.PosteCode == null || f.PosteCode == posteCode)
                        && (f.FamilleProduitFiniCode == null || f.FamilleProduitFiniCode == familleCode)
                        && (f.TypeRobinetCode == null || f.TypeRobinetCode == typeRobinetCode))
            .Select(f => new { f.Id, f.PosteCode, f.FamilleProduitFiniCode, f.TypeRobinetCode })
            .ToListAsync();

        // Précision : nombre de critères du champ d'application renseignés (tous correspondent). Une famille porte toujours
        // son type de robinet : il ne compte à part que sans famille.
        var classes = formulaires
            .Select(f => (f.Id, Precision: (f.PosteCode != null ? 1 : 0) + (f.FamilleProduitFiniCode != null ? 2 : 0)
                                           + (f.FamilleProduitFiniCode == null && f.TypeRobinetCode != null ? 1 : 0)))
            .OrderByDescending(f => f.Precision)
            .ToList();
        if (classes.Count == 0 || (classes.Count > 1 && classes[0].Precision == classes[1].Precision)) return null;
        return classes[0].Id;
    }
}
