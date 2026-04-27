using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineFamille
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid RefFamilleCorpsId { get; set; }

    public virtual PlanVerifMachineEntete PlanEntete { get; set; } = null!;

    public virtual ICollection<PlanVerifMachineMatricePiece> PlanVerifMachineMatricePieces { get; set; } = new List<PlanVerifMachineMatricePiece>();

    public virtual RefFamilleCorp RefFamilleCorps { get; set; } = null!;
}
