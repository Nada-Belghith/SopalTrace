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

    public Guid? PlanSourceId { get; set; }

    public string TypeOf { get; set; } = null!;

    public string Statut { get; set; } = null!;

    public bool EstEnReglage { get; set; }

    public DateTime DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    public double TempsPauseTotalMinutes { get; set; }

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ICollection<ExecControleLigneReponse> ExecControleLigneReponses { get; set; } = new List<ExecControleLigneReponse>();

    public virtual ICollection<ExecControleOfPoste> ExecControleOfPostes { get; set; } = new List<ExecControleOfPoste>();

    public virtual ICollection<ExecControleTranche> ExecControleTranches { get; set; } = new List<ExecControleTranche>();

    public virtual ICollection<ExecPieceType> ExecPieceTypes { get; set; } = new List<ExecPieceType>();

    public virtual ICollection<ExecPrelevementIntermediaire> ExecPrelevementIntermediaires { get; set; } = new List<ExecPrelevementIntermediaire>();

    public virtual ICollection<ExecRegistreTracabilite> ExecRegistreTracabilites { get; set; } = new List<ExecRegistreTracabilite>();

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual Machine? MachineCodePrevuNavigation { get; set; }

    public virtual MfgheadOrdreFabrication NumeroOfNavigation { get; set; } = null!;

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual PosteTravail? PosteCodePrevuNavigation { get; set; }
}
