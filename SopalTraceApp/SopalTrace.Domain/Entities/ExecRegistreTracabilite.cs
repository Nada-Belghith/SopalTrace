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

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual ICollection<ExecRegistreTracabiliteComposant> ExecRegistreTracabiliteComposants { get; set; } = new List<ExecRegistreTracabiliteComposant>();
}
