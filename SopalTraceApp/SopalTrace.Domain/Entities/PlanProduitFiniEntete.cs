using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanProduitFiniEntete
{
    public Guid Id { get; set; }

    public string FamilleProduitFiniCode { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public Guid? FormulaireId { get; set; }

    public string? LegendeMoyens { get; set; }

    public string? Remarques { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public virtual FamilleProduitFini FamilleProduitFiniCodeNavigation { get; set; } = null!;

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual ICollection<PlanProduitFiniLigne> PlanProduitFiniLignes { get; set; } = new List<PlanProduitFiniLigne>();

    public virtual ICollection<PlanProduitFiniSection> PlanProduitFiniSections { get; set; } = new List<PlanProduitFiniSection>();
}
