using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Machine
{
    public string CodeMachine { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public string OperationCode { get; set; } = null!;

    public string TypeAffectation { get; set; } = null!;

    public string? RoleMachine { get; set; }

    public bool Actif { get; set; }

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PieceReference> PieceReferences { get; set; } = new List<PieceReference>();

    public virtual ICollection<PlanAssLigne> PlanAssLignes { get; set; } = new List<PlanAssLigne>();

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();

    public virtual ICollection<PlanNcLigne> PlanNcLignes { get; set; } = new List<PlanNcLigne>();

    public virtual PlanVerifMachineEntete? PlanVerifMachineEntete { get; set; }

    public virtual ICollection<RefFormulaire> RefFormulaires { get; set; } = new List<RefFormulaire>();

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }

    public virtual ICollection<PosteTravail> CodePostes { get; set; } = new List<PosteTravail>();
}
