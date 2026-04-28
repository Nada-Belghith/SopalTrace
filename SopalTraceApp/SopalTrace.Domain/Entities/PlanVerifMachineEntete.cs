using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanVerifMachineEntete
{
    public Guid Id { get; set; }

    public string MachineCode { get; set; } = null!;

    public string Nom { get; set; } = null!;

    public int? Version { get; set; }

    public string? Statut { get; set; }

    public bool? AfficheConformite { get; set; }

    public bool? AfficheMoyenDetectionRisques { get; set; }

    public bool? AfficheFamilles { get; set; }

    public bool? AfficheFuiteEtalon { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime? CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? Remarques { get; set; }

    public string? LegendeMoyens { get; set; }

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanVerifMachineFamille> PlanVerifMachineFamilles { get; set; } = new List<PlanVerifMachineFamille>();

    public virtual ICollection<PlanVerifMachineLigne> PlanVerifMachineLignes { get; set; } = new List<PlanVerifMachineLigne>();
}
