using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanProduitFiniSection
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public Guid? TypeSectionId { get; set; }

    public string LibelleSection { get; set; } = null!;

    public Guid? PeriodiciteId { get; set; }

    public Guid? RegleEchantillonnageId { get; set; }

    public string? Notes { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual PlanProduitFiniEntete PlanEntete { get; set; } = null!;

    public virtual ICollection<PlanProduitFiniLigne> PlanProduitFiniLignes { get; set; } = new List<PlanProduitFiniLigne>();

    public virtual RefRegleEchantillonnage? RegleEchantillonnage { get; set; }

    public virtual TypeSection? TypeSection { get; set; }
}
