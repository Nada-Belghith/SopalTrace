using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachinePieceRef
{
    public Guid Id { get; set; }

    public Guid EcheanceId { get; set; }

    public Guid PieceRefId { get; set; }

    public string RoleVerif { get; set; } = null!;

    public string ResultatAttendu { get; set; } = null!;

    public string? FamilleDesc { get; set; }

    public string? Notes { get; set; }

    public virtual PlanVerifMachineEcheance Echeance { get; set; } = null!;

    public virtual PieceReference PieceRef { get; set; } = null!;
}
