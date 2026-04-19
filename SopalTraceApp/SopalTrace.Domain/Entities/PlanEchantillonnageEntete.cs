using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanEchantillonnageEntete
{
    public Guid Id { get; set; }

    public string CodeReference { get; set; } = null!;

    public string? CodeArticleSage { get; set; }

    public string? MachineCode { get; set; }

    public Guid? FormulaireId { get; set; }

    public string NiveauControle { get; set; } = null!;

    public string TypePlan { get; set; } = null!;

    public string ModeControle { get; set; } = null!;

    public int NqaId { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual Nqa Nqa { get; set; } = null!;

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanEchantillonnageRegle> PlanEchantillonnageRegles { get; set; } = new List<PlanEchantillonnageRegle>();
}
