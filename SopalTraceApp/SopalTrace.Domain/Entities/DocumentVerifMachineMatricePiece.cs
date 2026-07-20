using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentVerifMachineMatricePiece
{
    public Guid Id { get; set; }

    public Guid EcheanceId { get; set; }

    public Guid? FamilleId { get; set; }

    public string? RoleVerif { get; set; }

    public Guid? PieceRefId { get; set; }

    public virtual DocumentVerifMachineEcheance Echeance { get; set; } = null!;

    public virtual DocumentVerifMachineFamille? Famille { get; set; }

    public virtual PieceReference? PieceRef { get; set; }
}
