using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Periodicite
{
    public Guid Id { get; set; }

    public int NbrPieces { get; set; }

    public string Unite { get; set; } = null!;

    public int? NbrHeures { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<DocumentSection> DocumentSections { get; set; } = new List<DocumentSection>();
}
