using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SopalTrace.Application.Dtos.VerifMachine;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

/// <summary>
/// Vérifications machine d'un poste pour l'équipe active, à un moment donné (démarrage, après la pause, fin de
/// poste) : une ligne par machine du poste (machines par défaut et machines ajoutées par l'équipe).
/// </summary>
public class VerifMachinesPosteDto
{
    public Guid ExecControleOfId { get; set; }
    public string NumeroOf { get; set; } = string.Empty;
    public string PosteCode { get; set; } = string.Empty;
    /// <summary>DEMARRAGE, APRES_PAUSE ou FIN_POSTE.</summary>
    public string Periodicite { get; set; } = string.Empty;
    public string? Equipe { get; set; }
    /// <summary>La session est ouverte (ni en pause, ni terminée) : saisie et validation possibles.</summary>
    public bool Modifiable { get; set; }
    public List<VerifMachinePosteLigneDto> Machines { get; set; } = new();
    /// <summary>Machines dont la vérification est attendue à ce moment.</summary>
    public int Attendues { get; set; }
    public int Validees { get; set; }
}

public class VerifMachinePosteLigneDto
{
    public string MachineCode { get; set; } = string.Empty;
    public string? Libelle { get; set; }
    /// <summary>Valeur de EtatsVerifMachine.</summary>
    public string Etat { get; set; } = string.Empty;
    /// <summary>Machine ajoutée par l'équipe (absente de la liste par défaut du poste).</summary>
    public bool Ajoutee { get; set; }
    public Guid? StatutId { get; set; }
    /// <summary>Plan de vérification actif de la machine.</summary>
    public Guid? PlanId { get; set; }
    public int PointsRepondus { get; set; }
    public int PointsTotal { get; set; }
    public DateTime? ValideeLe { get; set; }
    public string? ValideePar { get; set; }
}

/// <summary>Vérification machine déjà ouverte (historique du poste : par machine, équipe et jour).</summary>
public class VerifMachineHistoriqueDto
{
    public Guid StatutId { get; set; }
    public Guid? PlanId { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string? Equipe { get; set; }
    public DateTime? Jour { get; set; }
    public bool Demarrage { get; set; }
    public bool ApresPause { get; set; }
    public bool FinPoste { get; set; }
    public bool SessionActive { get; set; }
    public string? MatriculeOperateur { get; set; }
}

public class AjouterMachinePosteRequest
{
    [Required, RegularExpression("^[A-Za-z0-9_.-]{1,30}$", ErrorMessage = "Code machine invalide.")]
    public string MachineCode { get; set; } = null!;
}

/// <summary>Validation d'une vérification machine pour un moment : réponses (facultatives si déjà enregistrées).</summary>
public class ValiderVerifMachineRequest
{
    /// <summary>DEMARRAGE, APRES_PAUSE ou FIN_POSTE.</summary>
    [Required, RegularExpression("^(DEMARRAGE|APRES_PAUSE|FIN_POSTE)$", ErrorMessage = "Périodicité invalide.")]
    public string Periodicite { get; set; } = null!;

    [MaxLength(1000)]
    public List<ExecVerifMachineReponseDto> Reponses { get; set; } = new();
}
