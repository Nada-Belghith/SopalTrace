using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabricationEntete
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string NatureArticleCode { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public string? FamilleProduitFiniCode { get; set; }

    public Guid? FormulaireId { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? Notes { get; set; }

    public string? LegendeMoyens { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public virtual FamilleProduitFini? FamilleProduitFiniCodeNavigation { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual ICollection<ModeleFabricationSection> ModeleFabricationSections { get; set; } = new List<ModeleFabricationSection>();

    public virtual NatureArticle NatureArticleCodeNavigation { get; set; } = null!;

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanFabricationEntete> PlanFabricationEntetes { get; set; } = new List<PlanFabricationEntete>();
}
