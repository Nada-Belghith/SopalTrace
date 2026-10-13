using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ReferencePieceRole
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public virtual ICollection<DocumentVerifMachineMatricePiece> DocumentVerifMachineMatricePieces { get; set; } = new List<DocumentVerifMachineMatricePiece>();

    public virtual ICollection<PieceReference> PieceReferences { get; set; } = new List<PieceReference>();
}
