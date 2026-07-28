using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFormulaireEquipe
{
    public Guid Id { get; set; }

    public Guid FormulaireId { get; set; }

    public string NomEquipe { get; set; } = null!;

    public int HeureDebut { get; set; }

    public int HeureFin { get; set; }

    public int OrdreAffiche { get; set; }

    public bool Actif { get; set; }

    public virtual RefFormulaire Formulaire { get; set; } = null!;
}
