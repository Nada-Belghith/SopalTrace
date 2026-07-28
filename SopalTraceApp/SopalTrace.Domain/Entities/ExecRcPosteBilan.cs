using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteBilan
{
    public Guid Id { get; set; }

    public Guid ExecControleDocumentStatutId { get; set; }

    public double TotalDefauts { get; set; }

    public double TotalPiecesTestees { get; set; }

    public double TauxNc { get; set; }

    public double NbPiecesRebutees { get; set; }

    public double NbPieceConforme { get; set; }

    public virtual ExecControleDocumentStatut ExecControleDocumentStatut { get; set; } = null!;
}
