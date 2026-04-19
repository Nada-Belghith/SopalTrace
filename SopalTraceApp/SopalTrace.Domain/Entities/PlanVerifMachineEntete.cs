using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineEntete
{
    public Guid Id { get; set; }

    public string MachineCode { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public string TypeRapport { get; set; } = null!;

    public Guid? FormulaireId { get; set; }

    public string Nom { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? LegendeMoyens { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanVerifMachineLigne> PlanVerifMachineLignes { get; set; } = new List<PlanVerifMachineLigne>();

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
