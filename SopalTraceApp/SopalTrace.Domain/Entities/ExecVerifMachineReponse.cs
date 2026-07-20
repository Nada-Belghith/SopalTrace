using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecVerifMachineReponse
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

    public virtual DocumentVerifMachineEcheance DocumentVerifMachineEcheance { get; set; } = null!;

    public virtual ExecControleDocumentStatut ExecControleDocumentStatut { get; set; } = null!;
}
