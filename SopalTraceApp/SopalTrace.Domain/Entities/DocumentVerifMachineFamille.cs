using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentVerifMachineFamille
{
    public Guid Id { get; set; }

    public Guid EnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid RefFamilleCorpsId { get; set; }

    public virtual ICollection<DocumentVerifMachineMatricePiece> DocumentVerifMachineMatricePieces { get; set; } = new List<DocumentVerifMachineMatricePiece>();

    public virtual DocumentEntete Entete { get; set; } = null!;

    public virtual RefFamilleCorp RefFamilleCorps { get; set; } = null!;
}
