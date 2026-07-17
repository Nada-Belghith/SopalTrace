using System;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class DocumentStatutDto
{
    public Guid Id { get; set; }
    public string TypeDocument { get; set; } = null!;
    public string? PosteCode { get; set; }
    public Guid? DocId { get; set; }
    public string? LibelleFormulaire { get; set; }
    public bool EstTermine { get; set; }
    public DateTime? DateTermine { get; set; }
}
