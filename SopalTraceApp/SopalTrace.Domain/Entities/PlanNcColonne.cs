using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanNcColonne
{
    public Guid Id { get; set; }

    public Guid PlanNcenteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string MachineCode { get; set; } = null!;

    public string LibelleDefaut { get; set; } = null!;

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual PlanNcEntete PlanNcentete { get; set; } = null!;
    
    public Guid? FormulaireId { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public virtual ICollection<PlanNcColonne> PlanNcColonnes { get; set; } = new List<PlanNcColonne>();
}
