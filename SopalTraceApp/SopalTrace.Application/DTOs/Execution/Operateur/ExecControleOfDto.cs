using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class ExecControleOfDto
{
    public Guid Id { get; set; }
    public string NumeroOf { get; set; } = null!;
    public string OperationCode { get; set; } = null!;
    public string? MachineCode { get; set; }
    public string? PosteCode { get; set; }
    public string Statut { get; set; } = null!;
    public DateTime DateDebut { get; set; }
    public bool A_Des_Controles_Reglage { get; set; }
    public bool EstEnReglage { get; set; }
    
    public IEnumerable<TrancheAlertesDto> AlertesActives { get; set; } = new List<TrancheAlertesDto>();
}
