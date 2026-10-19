using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SopalTrace.Application.Dtos.VerifMachine;

public class ExecVerifMachineSessionDto
{
    public string? NumeroOf { get; set; }
    public string? CodeArticle { get; set; }
    public string? DesignationArticle { get; set; }
    public string? Equipe { get; set; }
    public string? MachineCode { get; set; }
    public DateTime? DateExecution { get; set; }

    public List<VerifMachineSessionSummaryDto> AvailableSessions { get; set; } = new();

    public List<ExecVerifMachineReponseDto> Reponses { get; set; } = new();
}

public class VerifMachineSessionSummaryDto
{
    public Guid StatutId { get; set; }
    public string? Equipe { get; set; }
    public DateTime? DateExecution { get; set; }
    public DateTime? DateTermine { get; set; }
    public bool EstTermine { get; set; }
    public string? MatriculeOperateur { get; set; }
}

public class ExecVerifMachineReponseDto
{
    public Guid Id { get; set; }
    public Guid ExecControleDocumentStatutId { get; set; }
    public Guid DocumentVerifMachineEcheanceId { get; set; }
    public DateTime DateExecution { get; set; }
    /// <summary>Renvoyé en lecture ; ignoré en écriture (le matricule enregistré est celui du jeton).</summary>
    public string? MatriculeOperateur { get; set; }
    public decimal? PressionEntree { get; set; }
    public decimal? FuiteAffichee { get; set; }
    public bool? Conforme { get; set; }

    [StringLength(500)]
    public string? Observation { get; set; }
}

public class SaveExecVerifMachineRequest
{
    public Guid ExecControleDocumentStatutId { get; set; }

    /// <summary>Renseigné côté serveur à partir du jeton JWT, jamais lu depuis la requête.</summary>
    [JsonIgnore]
    public string MatriculeOperateur { get; set; } = string.Empty;

    [MaxLength(1000)]
    public List<ExecVerifMachineReponseDto> Reponses { get; set; } = new();
}
