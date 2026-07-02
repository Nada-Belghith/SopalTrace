namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class OfOperationDisponibleDto
{
    public string NumeroOf { get; set; } = null!;
    public string CodeArticle { get; set; } = null!;
    public string OperationCode { get; set; } = null!;
    public string StatutOf { get; set; } = null!;
    public string? MachinePrevueCode { get; set; }
}
