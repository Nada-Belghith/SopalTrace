using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MoyenControle
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();
}
