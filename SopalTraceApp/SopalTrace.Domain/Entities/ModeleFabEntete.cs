using System;
using System.Collections.Generic;

using SopalTrace.Domain.Interfaces;

namespace SopalTrace.Domain.Entities;

public partial class ModeleFabEntete : IPlanEntete
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public string NatureComposantCode { get; set; } = null!;

    public string? OperationCode { get; set; }

    public Guid? FormulaireId { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? Notes { get; set; }

    public string? LegendeMoyens { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public DateTime? ArchiveLe { get; set; }

    public string? ArchivePar { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual ICollection<ModeleFabSection> ModeleFabSections { get; set; } = new List<ModeleFabSection>();

    public virtual NatureComposant NatureComposantCodeNavigation { get; set; } = null!;

    public virtual Operation? OperationCodeNavigation { get; set; }

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
