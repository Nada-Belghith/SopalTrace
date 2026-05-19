using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabEntete
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string NatureArticleCode { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public Guid? FormulaireId { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? Notes { get; set; }

    public string? LegendeMoyens { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual ICollection<ModeleFabSection> ModeleFabSections { get; set; } = new List<ModeleFabSection>();

    public virtual NatureArticle NatureArticleCodeNavigation { get; set; } = null!;

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();
}
