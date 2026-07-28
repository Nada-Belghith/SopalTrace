using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteReponse
{
    public Guid Id { get; set; }

    public Guid ExecControleDocumentStatutId { get; set; }

    public string TrancheHoraire { get; set; } = null!;

    public double TotalNcHeure { get; set; }

    public double TotalRealiseHeure { get; set; }

    public virtual ExecControleDocumentStatut ExecControleDocumentStatut { get; set; } = null!;
}
