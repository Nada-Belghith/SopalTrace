using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanNcEntete
{
    public Guid Id { get; set; }

    public string TypeRobinetCode { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public string PosteCode { get; set; } = null!;

    public Guid? FormulaireId { get; set; }

    public string Nom { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanNcColonne> PlanNcColonnes { get; set; } = new List<PlanNcColonne>();

    public virtual PosteTravail PosteCodeNavigation { get; set; } = null!;

    public virtual TypeRobinet TypeRobinetCodeNavigation { get; set; } = null!;
}
