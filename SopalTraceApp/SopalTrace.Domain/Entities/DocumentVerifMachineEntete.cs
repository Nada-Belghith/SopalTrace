using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentVerifMachineEntete
{
    public Guid Id { get; set; }

    public string MachineCode { get; set; } = null!;

    public string Nom { get; set; } = null!;

    public int? Version { get; set; }

    public string? Statut { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime? CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public Guid? FormulaireId { get; set; }

    public string? Remarques { get; set; }

    public string? LegendeMoyens { get; set; }

    public virtual ICollection<DocumentVerifMachineFamille> DocumentVerifMachineFamilles { get; set; } = new List<DocumentVerifMachineFamille>();

    public virtual ICollection<DocumentVerifMachineLigne> DocumentVerifMachineLignes { get; set; } = new List<DocumentVerifMachineLigne>();

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Machine MachineCodeNavigation { get; set; } = null!;
}
