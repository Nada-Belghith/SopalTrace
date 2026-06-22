using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFormulaire
{
    public Guid Id { get; set; }

    public string CodeReference { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? CreePar { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<ModeleFabricationEntete> ModeleFabricationEntetes { get; set; } = new List<ModeleFabricationEntete>();

    public virtual ICollection<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; } = new List<PlanEchantillonnageEntete>();

    public virtual ICollection<PlanFabricationEntete> PlanFabricationEntetes { get; set; } = new List<PlanFabricationEntete>();

    public virtual ICollection<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; } = new List<PlanVerifMachineEntete>();
}
