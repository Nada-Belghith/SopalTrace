using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleDocumentStatut
{
    public Guid Id { get; set; }

    public Guid ExecControleOfId { get; set; }

    public string TypeDocument { get; set; } = null!;

    public string? PosteCode { get; set; }

    public Guid? DocId { get; set; }

    public bool EstTermine { get; set; }

    public DateTime? DateTermine { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual PosteTravail? PosteCodeNavigation { get; set; }
}
