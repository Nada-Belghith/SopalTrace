using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRegistreTracabilite
{
    public Guid Id { get; set; }

    public Guid ExecControleOfId { get; set; }

    public DateTime DateHeure { get; set; }

    public int Version { get; set; }

    public string? Corps { get; set; }

    public string? Volant { get; set; }

    public Guid? SessionId { get; set; }

    public string? Equipe { get; set; }

    public string? PosteCode { get; set; }

    public string? MatriculeOperateur { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledBy { get; set; }

    public string? CancellationReason { get; set; }

    public virtual Autili? CancelledByNavigation { get; set; }

    public virtual Team? EquipeNavigation { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual ICollection<ExecRegistreTracabiliteComposant> ExecRegistreTracabiliteComposants { get; set; } = new List<ExecRegistreTracabiliteComposant>();

    public virtual Autili? MatriculeOperateurNavigation { get; set; }

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual ExecControleOfSession? Session { get; set; }
}
