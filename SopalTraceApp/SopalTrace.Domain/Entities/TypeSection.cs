using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class TypeSection
{
    public Guid Id { get; set; }

    public string TypeNormalise { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentSection> DocumentSections { get; set; } = new List<DocumentSection>();
}
