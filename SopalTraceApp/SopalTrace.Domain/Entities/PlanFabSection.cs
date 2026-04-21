using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanFabSection
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public Guid? ModeleSectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public string LibelleSection { get; set; } = null!;

    public string? FrequenceLibelle { get; set; }

    public virtual ModeleFabSection? ModeleSection { get; set; }

    public virtual PlanFabEntete PlanEntete { get; set; } = null!;

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();
}
