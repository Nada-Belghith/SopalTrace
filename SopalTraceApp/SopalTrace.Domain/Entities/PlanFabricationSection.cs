using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanFabricationSection
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public Guid? ModeleSectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public string LibelleSection { get; set; } = null!;

    public Guid? TypeSectionId { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public Guid? RegleEchantillonnageId { get; set; }

    public virtual ModeleFabricationSection? ModeleSection { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual PlanFabricationEntete PlanEntete { get; set; } = null!;

    public virtual ICollection<PlanFabricationLigne> PlanFabricationLignes { get; set; } = new List<PlanFabricationLigne>();

    public virtual RefRegleEchantillonnage? RegleEchantillonnage { get; set; }

    public virtual TypeSection? TypeSection { get; set; }
}
