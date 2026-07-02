using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecPrelevementIntermediaire
{
    public Guid Id { get; set; }

    public Guid ExecControleOfid { get; set; }

    public Guid SectionId { get; set; }

    public string TrancheHoraire { get; set; } = null!;

    public int NumeroOccurrence { get; set; }

    public DateTime HeureNotifPrevue { get; set; }

    public DateTime? HeureReponse { get; set; }

    public string? Resultat { get; set; }

    public bool EstEnRetard { get; set; }

    public bool EstRepondu { get; set; }

    public DateTime CreeLe { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;
}
