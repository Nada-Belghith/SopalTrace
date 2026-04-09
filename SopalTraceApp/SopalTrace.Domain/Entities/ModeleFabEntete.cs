using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabEntete
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string TypeRobinetCode { get; set; } = null!;

    public string NatureComposantCode { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? Notes { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public DateTime? ArchiveLe { get; set; }

    public string? ArchivePar { get; set; }

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual ICollection<ModeleFabSection> ModeleFabSections { get; set; } = new List<ModeleFabSection>();

    public virtual NatureComposant NatureComposantCodeNavigation { get; set; } = null!;

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();

    public virtual TypeRobinet TypeRobinetCodeNavigation { get; set; } = null!;
}
