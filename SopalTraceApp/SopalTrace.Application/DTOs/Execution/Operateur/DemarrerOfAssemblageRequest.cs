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
    /// <summary>Postes sur lesquels l'équipe travaille (démarrés pour sa session) ; vide pour une clôture seule.</summary>
    [Required, MaxLength(20)]
    public List<string> PosteCodes { get; set; } = new();

    /// <summary>L'équipe vient seulement clôturer l'OF : aucun poste démarré, seuls les contrôles de fin d'OF.</summary>
    public bool PourCloturer { get; set; }

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

    /// <summary>Assemblage : équipe qui part en pause (par défaut la seule équipe ouverte sur l'OF).</summary>
    [StringLength(10)]
    public string? Equipe { get; set; }
}

public class IgnorerTrancheRequest
{
    [StringLength(500)]
    public string? Raison { get; set; }
}
