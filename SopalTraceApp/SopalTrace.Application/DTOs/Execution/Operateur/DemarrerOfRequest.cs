using System;
using System.Text.Json.Serialization;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class DemarrerOfRequest
{
    public string NumeroOf { get; set; } = null!;
    public string OperationCode { get; set; } = null!;
    public string? MachineCode { get; set; } // Opérations avec machine (Operation.AvecMachine)
    public string? PosteCode { get; set; } // Pour ASS
    /// <summary>Renseigné côté serveur à partir du jeton JWT, jamais lu depuis la requête.</summary>
    [JsonIgnore]
    public string? MatriculeOperateur { get; set; }
    
    // Cotes nominales, retenues seulement si Operation.AvecCotesTronconnage
    public double? Longueur { get; set; }
    public double? Diametre { get; set; }
}
