using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecPieceType
{
    public Guid Id { get; set; }

    public Guid ExecControleOfid { get; set; }

    public DateTime HeureValidation { get; set; }

    public string Resultat { get; set; } = null!;

    public string? Remarque { get; set; }

    public string MatriculeOperateur { get; set; } = null!;

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;
}
