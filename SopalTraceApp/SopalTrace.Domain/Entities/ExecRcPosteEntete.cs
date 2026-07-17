using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteEntete
{
    public Guid Id { get; set; }

    public Guid ExecControleOfId { get; set; }

    public DateTime DateSaisie { get; set; }

    public string? Equipe1Matricule { get; set; }

    public string? Equipe2Matricule { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual ICollection<ExecRcPosteLigneBilan> ExecRcPosteLigneBilans { get; set; } = new List<ExecRcPosteLigneBilan>();
}
