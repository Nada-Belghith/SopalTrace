using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteBilan
{
    public Guid Id { get; set; }

    public Guid ExecControleDocumentStatutId { get; set; }

    public string? Equipe { get; set; }

    public int NbPiecesRebutees { get; set; }

    public string? MatriculeOperateur { get; set; }

    public DateTime? DateSaisie { get; set; }

    public virtual Team? EquipeNavigation { get; set; }

    public virtual ExecControleDocumentStatut ExecControleDocumentStatut { get; set; } = null!;

    public virtual Autili? MatriculeOperateurNavigation { get; set; }
}
