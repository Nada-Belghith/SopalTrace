using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class AppRole
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<PreassignedRole> PreassignedRoles { get; set; } = new List<PreassignedRole>();

    public virtual ICollection<UtilisateursApp> UtilisateursApps { get; set; } = new List<UtilisateursApp>();
}
