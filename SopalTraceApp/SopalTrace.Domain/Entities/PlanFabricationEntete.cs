using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanFabricationEntete
{
    public Guid Id { get; set; }

    public Guid? ModeleSourceId { get; set; }

    public string CodeArticleSageVersionne { get; set; } = null!;

    public string? Designation { get; set; }

    public string Nom { get; set; } = null!;

    public int Version { get; set; }

    public string? OperationCode { get; set; }

    public string Statut { get; set; } = null!;

    public string? MachineDefautCode { get; set; }

    public Guid? FormulaireId { get; set; }

    public string? LegendeMoyens { get; set; }

    public string? Remarques { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Machine? MachineDefautCodeNavigation { get; set; }

    public virtual ModeleFabricationEntete? ModeleSource { get; set; }

    public virtual Operation? OperationCodeNavigation { get; set; }

    public virtual ICollection<PlanFabricationLigne> PlanFabricationLignes { get; set; } = new List<PlanFabricationLigne>();

    public virtual ICollection<PlanFabricationSection> PlanFabricationSections { get; set; } = new List<PlanFabricationSection>();
}
