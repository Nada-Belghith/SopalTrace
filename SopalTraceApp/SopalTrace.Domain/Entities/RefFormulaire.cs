using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFormulaire
{
    public Guid Id { get; set; }

    public string CodeReference { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? CreePar { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? Role { get; set; }

    public string? PosteCode { get; set; }

    public string? FamilleProduitFiniCode { get; set; }

    public virtual Autili? CreeParNavigation { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual FamilleProduitFini? FamilleProduitFiniCodeNavigation { get; set; }

    public virtual Autili? ModifieParNavigation { get; set; }

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual ICollection<RefFormulaireColonneDef> RefFormulaireColonneDefs { get; set; } = new List<RefFormulaireColonneDef>();

    public virtual ICollection<RefFormulaireEquipe> RefFormulaireEquipes { get; set; } = new List<RefFormulaireEquipe>();
}
