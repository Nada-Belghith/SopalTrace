using System.ComponentModel.DataAnnotations;

namespace SopalTrace.Application.Alertes;

public class PlanManquantContexte
{
    [Required, StringLength(20)]
    public string OperationCode { get; set; } = null!;

    [Required, StringLength(30)]
    public string PosteCode { get; set; } = null!;

    [StringLength(30)]
    public string? ArticleCode { get; set; }

    [StringLength(30)]
    public string? NumeroOf { get; set; }

    [StringLength(30)]
    public string? MachineCode { get; set; }

    [StringLength(150)]
    public string? DesignationArticle { get; set; }

    /// <summary>Déduite côté serveur si absente.</summary>
    [StringLength(250)]
    public string? FamilleArticle { get; set; }

    /// <summary>Renseigné côté serveur à partir de l'utilisateur connecté.</summary>
    public string? NomOperateur { get; set; }

    /// <summary>Renseigné côté serveur à partir de l'utilisateur connecté.</summary>
    public string? EmailOperateur { get; set; }

    [Required, StringLength(1000)]
    public string DescriptionProbleme { get; set; } = null!;
}
