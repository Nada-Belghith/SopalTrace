using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteReponse
{
    public Guid Id { get; set; }

    public Guid ExecControleDocumentStatutId { get; set; }

    public string TrancheHoraire { get; set; } = null!;

    public int TotalRealiseHeure { get; set; }

    public string? Equipe { get; set; }

    public string? MatriculeOperateur { get; set; }

    public DateTime? DateSaisie { get; set; }

    public virtual Team? EquipeNavigation { get; set; }

    public virtual ExecControleDocumentStatut ExecControleDocumentStatut { get; set; } = null!;

    public virtual Autili? MatriculeOperateurNavigation { get; set; }
}
