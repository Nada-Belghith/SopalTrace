using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachinePieceRef
{
    public Guid Id { get; set; }

    public Guid PlanLigneId { get; set; }

    public Guid PieceRefId { get; set; }

    public string RoleVerif { get; set; } = null!;

    public string ResultatAttendu { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual PieceReference PieceRef { get; set; } = null!;

    public virtual PlanVerifMachineLigne PlanLigne { get; set; } = null!;
}
