using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Periodicite
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public int? FrequenceNum { get; set; }

    public string? FrequenceUnite { get; set; }

    public int OrdreAffichage { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<ModeleFabricationLigne> ModeleFabricationLignes { get; set; } = new List<ModeleFabricationLigne>();

    public virtual ICollection<ModeleFabricationSection> ModeleFabricationSections { get; set; } = new List<ModeleFabricationSection>();

    public virtual ICollection<OutilControle> OutilControles { get; set; } = new List<OutilControle>();

    public virtual ICollection<PlanAssemblageLigne> PlanAssemblageLignes { get; set; } = new List<PlanAssemblageLigne>();

    public virtual ICollection<PlanAssemblageSection> PlanAssemblageSections { get; set; } = new List<PlanAssemblageSection>();

    public virtual ICollection<PlanFabricationLigne> PlanFabricationLignes { get; set; } = new List<PlanFabricationLigne>();

    public virtual ICollection<PlanFabricationSection> PlanFabricationSections { get; set; } = new List<PlanFabricationSection>();

    public virtual ICollection<PlanProduitFiniSection> PlanProduitFiniSections { get; set; } = new List<PlanProduitFiniSection>();

    public virtual ICollection<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; } = new List<PlanVerifMachineEcheance>();
}
