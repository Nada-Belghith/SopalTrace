using System;
using System.Collections.Generic;

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
    public string MatriculeOperateur { get; set; } = null!;
    public double? PressionEntree { get; set; }
    public double? FuiteAffichee { get; set; }
    public bool? Conforme { get; set; }
    public string? Observation { get; set; }
}

public class SaveExecVerifMachineRequest
{
    public Guid ExecControleDocumentStatutId { get; set; }
    public Guid? PeriodiciteMachineId { get; set; } // Ignoré côté backend mais envoyé par le frontend
    public string MatriculeOperateur { get; set; } = null!;
    
    public List<ExecVerifMachineReponseDto> Reponses { get; set; } = new();
}
