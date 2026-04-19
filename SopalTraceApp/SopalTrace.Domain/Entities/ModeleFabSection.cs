using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabSection
{
    public Guid Id { get; set; }

    public Guid ModeleEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string LibelleSection { get; set; } = null!;

    public string? FrequenceLibelle { get; set; }

    public Guid? TypeSectionId { get; set; }

    public virtual ModeleFabEntete ModeleEntete { get; set; } = null!;

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual ICollection<PlanFabSection> PlanFabSections { get; set; } = new List<PlanFabSection>();

    public virtual TypeSection? TypeSection { get; set; }
}
