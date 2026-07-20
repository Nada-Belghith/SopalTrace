using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentVerifMachineLigne
{
    public Guid Id { get; set; }

    public Guid PlanEnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string? TypeLigne { get; set; }

    public string LibelleRisque { get; set; } = null!;

    public string? LibelleMethode { get; set; }

    public virtual ICollection<DocumentVerifMachineEcheance> DocumentVerifMachineEcheances { get; set; } = new List<DocumentVerifMachineEcheance>();

    public virtual ICollection<DocumentVerifMachineLigneExtraColonne> DocumentVerifMachineLigneExtraColonnes { get; set; } = new List<DocumentVerifMachineLigneExtraColonne>();

    public virtual DocumentVerifMachineEntete PlanEntete { get; set; } = null!;
}
