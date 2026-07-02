using System;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class DemarrerOfRequest
{
    public string NumeroOf { get; set; } = null!;
    public string OperationCode { get; set; } = null!;
    public string? MachineCode { get; set; } // Pour USI/TRN
    public string? PosteCode { get; set; } // Pour ASS
    public int NumEquipe { get; set; }
    public string MatriculeOperateur { get; set; } = null!;
    
    // Champs spécifiques pour Tronçonnage (TRONC)
    public double? Longueur { get; set; }
    public double? Diametre { get; set; }
}
