using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Machine
{
    public string CodeMachine { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public string TypeAffectation { get; set; } = null!;

    public string? RoleMachine { get; set; }

    public bool Actif { get; set; }

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanAssLigne> PlanAssLignes { get; set; } = new List<PlanAssLigne>();

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();

    public virtual ICollection<PlanNcLigne> PlanNcLignes { get; set; } = new List<PlanNcLigne>();

    public virtual ICollection<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; } = new List<PlanVerifMachineEntete>();

    public virtual ICollection<PosteTravail> CodePostes { get; set; } = new List<PosteTravail>();

    public virtual ICollection<RefFamilleCorp> RefFamilleCorps { get; set; } = new List<RefFamilleCorp>();
}
