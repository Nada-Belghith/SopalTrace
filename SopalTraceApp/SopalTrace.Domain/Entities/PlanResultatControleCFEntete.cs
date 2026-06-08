using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanResultatControleCfEntete
{
    public Guid Id { get; set; }

    public string PosteCode { get; set; } = null!;

    public Guid? FormulaireId { get; set; }

    public string Nom { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? ConfigurationJson { get; set; }

    public string? Remarques { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual ICollection<PlanResultatControleCfSection> PlanResultatControleCfSections { get; set; } = new List<PlanResultatControleCfSection>();

    public virtual PosteTravail PosteCodeNavigation { get; set; } = null!;
}
