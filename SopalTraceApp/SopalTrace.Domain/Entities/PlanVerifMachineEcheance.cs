using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineEcheance
{
    public Guid Id { get; set; }

    public Guid PlanLigneId { get; set; }

    public Guid PeriodiciteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string? LibelleMoyen { get; set; }

    public virtual Periodicite Periodicite { get; set; } = null!;

    public virtual PlanVerifMachineLigne PlanLigne { get; set; } = null!;

    public virtual ICollection<PlanVerifMachinePieceRef> PlanVerifMachinePieceRefs { get; set; } = new List<PlanVerifMachinePieceRef>();
}
