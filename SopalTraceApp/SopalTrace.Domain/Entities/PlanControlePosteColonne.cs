using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanControlePosteColonne
{
    public Guid Id { get; set; }

    public Guid ControlePosteenteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string MachineCode { get; set; } = null!;

    public string LibelleDefaut { get; set; } = null!;

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual PlanControlePosteEntete PlanControlePosteEntete { get; set; } = null!;
    
    public Guid? FormulaireId { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public virtual ICollection<PlanControlePosteColonne> PlanControlePosteColonnes { get; set; } = new List<PlanControlePosteColonne>();
}
