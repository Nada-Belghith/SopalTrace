using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanAssemblageEntete
{
    public Guid Id { get; set; }

    public string OperationCode { get; set; } = null!;

    public string? FamilleProduitFiniCode { get; set; }

    public string? NatureArticleCode { get; set; }

    public string? PosteCode { get; set; }

    public string? Designation { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public Guid? FormulaireId { get; set; }

    public string? LegendeMoyens { get; set; }

    public string? Remarques { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public virtual FamilleProduitFini? FamilleProduitFiniCodeNavigation { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual NatureArticle? NatureArticleCodeNavigation { get; set; }

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanAssemblageLigne> PlanAssemblageLignes { get; set; } = new List<PlanAssemblageLigne>();

    public virtual ICollection<PlanAssemblageSection> PlanAssemblageSections { get; set; } = new List<PlanAssemblageSection>();

    public virtual PosteTravail? PosteCodeNavigation { get; set; }
}
