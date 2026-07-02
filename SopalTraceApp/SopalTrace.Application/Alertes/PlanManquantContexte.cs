namespace SopalTrace.Application.Alertes;

public class PlanManquantContexte
{
    public string OperationCode { get; set; } = null!;
    public string PosteCode { get; set; } = null!;
    public string ArticleCode { get; set; } = null!;
    public string? NomOperateur { get; set; }
    public string? EmailOperateur { get; set; }
    public string DescriptionProbleme { get; set; } = null!;
}
