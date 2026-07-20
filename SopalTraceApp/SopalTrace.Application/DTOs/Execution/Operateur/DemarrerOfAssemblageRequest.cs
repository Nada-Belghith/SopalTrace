using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class DemarrerOfAssemblageRequest
{
    public string NumeroOf { get; set; } = null!;
    public string OperationCode { get; set; } = null!; // Habituellement 'ASS'
    public List<string> PosteCodes { get; set; } = new List<string>();
    public string MatriculeOperateur { get; set; } = null!;
}
