using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PieceReference
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string TypePiece { get; set; } = null!;

    public string? Designation { get; set; }

    public string? FamilleDesc { get; set; }

    public string? MachineCode { get; set; }

    public bool Actif { get; set; }

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual ICollection<PlanVerifMachineMatricePiece> PlanVerifMachineMatricePieces { get; set; } = new List<PlanVerifMachineMatricePiece>();
}
