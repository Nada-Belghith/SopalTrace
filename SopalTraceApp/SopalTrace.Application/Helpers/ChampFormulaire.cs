using SopalTrace.Domain.Constants;

namespace SopalTrace.Application.Helpers;

/// <summary>
/// Champ d'application d'un formulaire : avec son rôle, il désigne le formulaire des documents d'un type. Seuls les
/// critères qui existent pour ce type sont renseignés (les autres sont null). Le code du formulaire n'est jamais déduit :
/// c'est un libellé choisi par le superviseur qualité. Avec une famille, le type de robinet est celui de la famille (repris
/// par le serveur) ; sans famille, il peut désigner à lui seul le formulaire si le rôle le prévoit.
/// </summary>
public sealed record ChampFormulaire(string Role, string? Famille, string? Poste, string? Machine, string? Nature, string? TypeRobinet = null)
{
    public override string ToString()
    {
        var criteres = new[]
        {
            Famille == null ? null : $"famille {Famille}",
            Poste == null ? null : $"poste {Poste}",
            Machine == null ? null : $"machine {Machine}",
            Nature == null ? null : $"nature {Nature}",
            TypeRobinet == null || Famille != null ? null : $"type de robinet {TypeRobinet}"
        };
        var texte = string.Join(", ", System.Linq.Enumerable.Where(criteres, c => c != null));
        return texte.Length == 0 ? Role : $"{Role} ({texte})";
    }
}

public static class ChampsFormulaire
{
    /// <summary>
    /// Champ d'application du formulaire d'un document, selon son type :
    /// plan d'assemblage : famille et poste (sans famille, comme le piston : sa nature) ; plan produit fini : famille ;
    /// contrôle au poste : poste et famille ; vérification machine : machine ; résultats en cours de fabrication et plan
    /// d'échantillonnage : aucun critère (un seul formulaire). Null pour un type sans formulaire de ce service.
    /// </summary>
    public static ChampFormulaire? PourDocument(string typeDocumentCode, string? famille, string? poste, string? machine, string? nature)
    {
        famille = Valeur(famille); poste = Valeur(poste); machine = Valeur(machine); nature = Valeur(nature);
        return typeDocumentCode switch
        {
            TypesDocument.PlanAssemblage => new(RolesFormulaire.EnCoursDeAssemblage, famille, poste, null, famille == null ? nature : null),
            TypesDocument.PlanProduitFini => new(RolesFormulaire.ProduitFini, famille, null, null, null),
            TypesDocument.ControlePoste => new(RolesFormulaire.ResultatControlePoste, famille, poste, null, null),
            TypesDocument.ResultatCF => new(RolesFormulaire.ResultatControleCf, null, null, null, null),
            TypesDocument.VerifMachine => new(RolesFormulaire.VerifMachine, null, null, machine, null),
            TypesDocument.PlanEchantillonnage => new(RolesFormulaire.Echantillonnage, null, null, null, null),
            _ => null
        };
    }

    private static string? Valeur(string? texte) => string.IsNullOrWhiteSpace(texte) ? null : texte.Trim();
}
