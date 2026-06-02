using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecPrelevement
{
    public Guid Id { get; set; }

    public Guid ExecControleTrancheId { get; set; }

    public DateTime HeurePrevue { get; set; }

    public DateTime? HeureSaisie { get; set; }

    public string? ResultatGlobal { get; set; }

    public string? MatriculeOperateur { get; set; }

    public virtual ExecControleTranche ExecControleTranche { get; set; } = null!;

    public virtual ICollection<ExecPrelevementLigne> ExecPrelevementLignes { get; set; } = new List<ExecPrelevementLigne>();
}
