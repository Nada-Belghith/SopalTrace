using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabricationSection
{
    public Guid Id { get; set; }

    public Guid ModeleEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string LibelleSection { get; set; } = null!;

    public Guid? TypeSectionId { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public Guid? RegleEchantillonnageId { get; set; }

    public virtual ModeleFabricationEntete ModeleEntete { get; set; } = null!;

    public virtual ICollection<ModeleFabricationLigne> ModeleFabricationLignes { get; set; } = new List<ModeleFabricationLigne>();

    public virtual Periodicite? Periodicite { get; set; }

    public virtual ICollection<PlanFabricationSection> PlanFabricationSections { get; set; } = new List<PlanFabricationSection>();

    public virtual RefRegleEchantillonnage? RegleEchantillonnage { get; set; }

    public virtual TypeSection? TypeSection { get; set; }
}
