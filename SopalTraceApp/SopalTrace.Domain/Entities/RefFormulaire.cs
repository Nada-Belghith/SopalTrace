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

    public virtual ICollection<DocumentEchantillonnageEntete> DocumentEchantillonnageEntetes { get; set; } = new List<DocumentEchantillonnageEntete>();

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<DocumentVerifMachineEntete> DocumentVerifMachineEntetes { get; set; } = new List<DocumentVerifMachineEntete>();

    public virtual ICollection<ModeleFabricationEntete> ModeleFabricationEntetes { get; set; } = new List<ModeleFabricationEntete>();

    public virtual ICollection<PlanFabricationEntete> PlanFabricationEntetes { get; set; } = new List<PlanFabricationEntete>();
}
