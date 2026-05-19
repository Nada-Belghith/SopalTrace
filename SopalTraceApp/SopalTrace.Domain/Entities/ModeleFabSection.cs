using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabSection
{
    public Guid Id { get; set; }

    public Guid ModeleEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string LibelleSection { get; set; } = null!;

    public Guid? TypeSectionId { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public Guid? RegleEchantillonnageId { get; set; }

    public virtual ModeleFabEntete ModeleEntete { get; set; } = null!;

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual Periodicite? Periodicite { get; set; }

    public virtual ICollection<PlanFabSection> PlanFabSections { get; set; } = new List<PlanFabSection>();

    public virtual RefRegleEchantillonnage? RegleEchantillonnage { get; set; }

    public virtual TypeSection? TypeSection { get; set; }
}
