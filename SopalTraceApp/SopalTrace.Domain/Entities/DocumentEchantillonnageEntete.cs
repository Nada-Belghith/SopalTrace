using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentEchantillonnageEntete
{
    public Guid Id { get; set; }

    public string NiveauControle { get; set; } = null!;

    public string TypePlan { get; set; } = null!;

    public string ModeControle { get; set; } = null!;

    public int NqaId { get; set; }

    public Guid? FormulaireId { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? CommentaireVersion { get; set; }

    public string? Remarques { get; set; }

    public string? LegendeMoyens { get; set; }

    public int CritereAcceptationAc { get; set; }

    public int CritereRejetRe { get; set; }

    public virtual ICollection<DocumentEchantillonnageRegle> DocumentEchantillonnageRegles { get; set; } = new List<DocumentEchantillonnageRegle>();

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual Nqa Nqa { get; set; } = null!;
}
