using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanEchantillonnageEntete
{
    public Guid Id { get; set; }

    public string NiveauControle { get; set; } = null!;

    public string TypePlan { get; set; } = null!;

    public string ModeControle { get; set; } = null!;

    public int NqaId { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public string? Remarques { get; set; }

    public string? LegendeMoyens { get; set; }

    public virtual Nqa Nqa { get; set; } = null!;

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();

    public virtual ICollection<PlanEchantillonnageRegle> PlanEchantillonnageRegles { get; set; } = new List<PlanEchantillonnageRegle>();
}
