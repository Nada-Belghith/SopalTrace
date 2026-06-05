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

    public string? Role { get; set; }

    public string? ConfigurationStructureJson { get; set; }

    public virtual ICollection<ModeleFabricationEntete> ModeleFabricationEntetes { get; set; } = new List<ModeleFabricationEntete>();

    public virtual ICollection<PlanAssemblageEntete> PlanAssemblageEntetes { get; set; } = new List<PlanAssemblageEntete>();

    public virtual ICollection<PlanFabricationEntete> PlanFabricationEntetes { get; set; } = new List<PlanFabricationEntete>();

    public virtual ICollection<PlanNonConformiteEntete> PlanNonConformiteEntetes { get; set; } = new List<PlanNonConformiteEntete>();

    public virtual ICollection<PlanProduitFiniEntete> PlanProduitFiniEntetes { get; set; } = new List<PlanProduitFiniEntete>();

    public virtual ICollection<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; } = new List<PlanVerifMachineEntete>();
}
