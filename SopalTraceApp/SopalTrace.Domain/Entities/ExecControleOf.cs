using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleOf
{
    public Guid Id { get; set; }

    public string NumeroOf { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public string? MachineCodePrevu { get; set; }

    public string? PosteCodePrevu { get; set; }

    public string? MachineCode { get; set; }

    public string? PosteCode { get; set; }

    public int NumEquipe { get; set; }

    public Guid PlanSourceId { get; set; }

    public string TypePlan { get; set; } = null!;

    public string Statut { get; set; } = null!;

    public DateTime DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    public virtual ICollection<ExecControleTranche> ExecControleTranches { get; set; } = new List<ExecControleTranche>();

    public virtual ICollection<ExecPieceType> ExecPieceTypes { get; set; } = new List<ExecPieceType>();

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual Machine? MachineCodePrevuNavigation { get; set; }

    public virtual MfgheadOrdreFabrication NumeroOfNavigation { get; set; } = null!;

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual PosteTravail? PosteCodePrevuNavigation { get; set; }
}
