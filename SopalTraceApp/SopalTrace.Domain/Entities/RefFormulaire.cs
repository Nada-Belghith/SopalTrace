using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFormulaire
{
    public Guid Id { get; set; }

    public string CodeReference { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string? OperationCode { get; set; }

    public string? PosteCode { get; set; }

    public int Version { get; set; }

    public bool Actif { get; set; }

    public DateTime CreeLe { get; set; }

    public virtual ICollection<ModeleFabEntete> ModeleFabEntetes { get; set; } = new List<ModeleFabEntete>();

    public virtual Operation? OperationCodeNavigation { get; set; }

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; } = new List<PlanEchantillonnageEntete>();

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();

    public virtual ICollection<PlanNcEntete> PlanNcEntetes { get; set; } = new List<PlanNcEntete>();

    public virtual ICollection<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; } = new List<PlanVerifMachineEntete>();

    public virtual PosteTravail? PosteCodeNavigation { get; set; }
}
