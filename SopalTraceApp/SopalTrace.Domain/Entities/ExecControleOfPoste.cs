using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleOfPoste
{
    public Guid Id { get; set; }

    public Guid ExecControleOfId { get; set; }

    public string PosteCode { get; set; } = null!;

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual PosteTravail PosteCodeNavigation { get; set; } = null!;
}
