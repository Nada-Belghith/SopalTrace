using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class DemarrerOfAssemblageRequest
{
    [Required, StringLength(30)]
    public string NumeroOf { get; set; } = null!;

    [Required, MinLength(1, ErrorMessage = "Sélectionnez au moins un poste."), MaxLength(20)]
    public List<string> PosteCodes { get; set; } = new();

    [Required, RegularExpression(SopalTrace.Domain.Constants.Equipes.FormatCode, ErrorMessage = "Équipe invalide.")]
    public string Equipe { get; set; } = null!;
}

public class AjouterPostesRequest
{
    [Required, MaxLength(20)]
    public List<string> PosteCodes { get; set; } = new();

    [Required, RegularExpression(SopalTrace.Domain.Constants.Equipes.FormatCode, ErrorMessage = "Équipe invalide.")]
    public string Equipe { get; set; } = null!;
}

public class InitVerifMachineRequest
{
    [Required, StringLength(30)]
    public string PosteCode { get; set; } = null!;

    [Required, StringLength(30)]
    public string MachineCode { get; set; } = null!;
}

public class PauseOfRequest
{
    [Required, StringLength(200)]
    public string Raison { get; set; } = null!;

    [RegularExpression("^(DEJEUNER|AUTRE)$", ErrorMessage = "Type de pause invalide.")]
    public string TypePause { get; set; } = "AUTRE";
}

public class IgnorerTrancheRequest
{
    [StringLength(500)]
    public string? Raison { get; set; }
}
