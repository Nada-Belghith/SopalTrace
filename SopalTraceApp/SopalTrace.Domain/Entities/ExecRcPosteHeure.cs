using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteHeure
{
    public Guid Id { get; set; }

    public Guid ExecControleDocumentStatutId { get; set; }

    public Guid DocLigneId { get; set; }

    public DateTime DateExecution { get; set; }

    public string? MatriculeOp { get; set; }

    public string? Equipe { get; set; }

    public string TrancheHoraire { get; set; } = null!;

    public double NbNcParHeure { get; set; }

    public virtual DocumentLigne DocLigne { get; set; } = null!;

    public virtual ExecControleDocumentStatut ExecControleDocumentStatut { get; set; } = null!;
}
