using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class TypeDocument
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string? DescriptionChamps { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();
}
