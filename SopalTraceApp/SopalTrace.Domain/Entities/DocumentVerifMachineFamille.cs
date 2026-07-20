using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentVerifMachineFamille
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid RefFamilleCorpsId { get; set; }

    public virtual ICollection<DocumentVerifMachineMatricePiece> DocumentVerifMachineMatricePieces { get; set; } = new List<DocumentVerifMachineMatricePiece>();

    public virtual DocumentVerifMachineEntete PlanEntete { get; set; } = null!;

    public virtual RefFamilleCorp RefFamilleCorps { get; set; } = null!;
}
