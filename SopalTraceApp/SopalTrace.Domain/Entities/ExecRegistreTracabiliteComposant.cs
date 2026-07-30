using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRegistreTracabiliteComposant
{
    public Guid Id { get; set; }

    public Guid RegistreTracabiliteId { get; set; }

    public string DesignationComposant { get; set; } = null!;

    public string? LotSelectionne { get; set; }

    public virtual ExecRegistreTracabilite RegistreTracabilite { get; set; } = null!;
}
