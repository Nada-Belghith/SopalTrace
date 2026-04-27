using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineEcheance
{
    public Guid Id { get; set; }

    public Guid PlanLigneId { get; set; }

    public Guid PeriodiciteId { get; set; }

    public Guid? RefMoyenDetectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public virtual Periodicite Periodicite { get; set; } = null!;

    public virtual PlanVerifMachineLigne PlanLigne { get; set; } = null!;

    public virtual ICollection<PlanVerifMachineMatricePiece> PlanVerifMachineMatricePieces { get; set; } = new List<PlanVerifMachineMatricePiece>();

    public virtual RefMoyenDetection? RefMoyenDetection { get; set; }
}
