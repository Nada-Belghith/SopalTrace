using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanFabEntete
{
    public Guid Id { get; set; }

    public Guid ModeleSourceId { get; set; }

    public string CodeArticleSage { get; set; } = null!;

    public string? Designation { get; set; }

    public string Nom { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public DateOnly? DateApplication { get; set; }

    public string? MachineDefautCode { get; set; }

    public Guid? FormulaireId { get; set; }

    public string? LegendeMoyens { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Machine? MachineDefautCodeNavigation { get; set; }

    public virtual ModeleFabEntete ModeleSource { get; set; } = null!;

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();

    public virtual ICollection<PlanFabSection> PlanFabSections { get; set; } = new List<PlanFabSection>();
}
