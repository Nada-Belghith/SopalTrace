using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanAssEntete
{
    public Guid Id { get; set; }

    public string OperationCode { get; set; } = null!;

    public string? TypeRobinetCode { get; set; }

    public bool EstModele { get; set; }

    public string? CodeArticleSage { get; set; }

    public string Nom { get; set; } = null!;

    public string? Designation { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public int NbPiecesReglage { get; set; }

    public Guid? FicheEchantillonnageId { get; set; }

    public DateOnly? DateApplication { get; set; }

    public Guid? FormulaireId { get; set; }

    public string? LegendeMoyens { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public virtual PlanEchantillonnageEntete? FicheEchantillonnage { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanAssLigne> PlanAssLignes { get; set; } = new List<PlanAssLigne>();

    public virtual ICollection<PlanAssSection> PlanAssSections { get; set; } = new List<PlanAssSection>();

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
