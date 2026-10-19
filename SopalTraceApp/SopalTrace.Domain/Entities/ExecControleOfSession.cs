using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleOfSession
{
    public Guid Id { get; set; }

    public Guid ExecControleOfId { get; set; }

    public string Equipe { get; set; } = null!;

    public DateTime DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public string Statut { get; set; } = null!;

    public double TempsPauseTotalMinutes { get; set; }

    public bool HadLunchBreak { get; set; }

    public string? ClosingRequest { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Team EquipeNavigation { get; set; } = null!;

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual ICollection<ExecControleTranche> ExecControleTranches { get; set; } = new List<ExecControleTranche>();

    public virtual ICollection<ExecPrelevementIntermediaire> ExecPrelevementIntermediaires { get; set; } = new List<ExecPrelevementIntermediaire>();

    public virtual ICollection<ExecProductionPeriod> ExecProductionPeriods { get; set; } = new List<ExecProductionPeriod>();

    public virtual ICollection<ExecRegistreTracabilite> ExecRegistreTracabilites { get; set; } = new List<ExecRegistreTracabilite>();

    public virtual ICollection<ExecSessionPost> ExecSessionPosts { get; set; } = new List<ExecSessionPost>();
}
