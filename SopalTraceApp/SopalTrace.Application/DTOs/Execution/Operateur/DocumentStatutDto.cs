using System;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class DocumentStatutDto
{
    public Guid Id { get; set; }
    public string TypeDocument { get; set; } = null!;
    public string? PosteCode { get; set; }
    public Guid? DocId { get; set; }
    public string? LibelleFormulaire { get; set; }
    public string? MachineCode { get; set; }
    public string? MachineLibelle { get; set; }
    public bool EstDemarrageTermine { get; set; }
    public bool EstPauseTermine { get; set; }
    public bool EstTermine { get; set; }
    public DateTime? DateTermine { get; set; }
    public string? Equipe { get; set; }
    public DateTime? DateExecution { get; set; }
}
