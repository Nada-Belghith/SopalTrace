namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class OfDisponibleDto
{
    public string NumeroOf { get; set; } = null!;
    public string CodeArticle { get; set; } = null!;
    public double QuantitePrevue { get; set; }
    public string StatutOf { get; set; } = null!;
}
