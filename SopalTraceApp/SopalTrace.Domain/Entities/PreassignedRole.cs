using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PreassignedRole
{
    public string Matricule { get; set; } = null!;

    public string RoleCode { get; set; } = null!;

    public string AssignedBy { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public virtual Autili MatriculeNavigation { get; set; } = null!;

    public virtual AppRole RoleCodeNavigation { get; set; } = null!;
}
