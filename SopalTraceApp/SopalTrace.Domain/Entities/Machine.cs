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

    public virtual ICollection<DocumentLigne> DocumentLigneMachineCodeCtrlPosteNavigations { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<DocumentLigne> DocumentLigneMachineCodeNavigations { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<ExecControleOf> ExecControleOfMachineCodeNavigations { get; set; } = new List<ExecControleOf>();

    public virtual ICollection<ExecControleOf> ExecControleOfMachineCodePrevuNavigations { get; set; } = new List<ExecControleOf>();

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanFabricationEntete> PlanFabricationEntetes { get; set; } = new List<PlanFabricationEntete>();

    public virtual ICollection<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; } = new List<PlanVerifMachineEntete>();

    public virtual ICollection<PosteTravail> CodePostes { get; set; } = new List<PosteTravail>();

    public virtual ICollection<RefFamilleCorp> RefFamilleCorps { get; set; } = new List<RefFamilleCorp>();
}
