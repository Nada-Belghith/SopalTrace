using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Nqa
{
    public int Id { get; set; }

    public double ValeurNqa { get; set; }

    public virtual ICollection<DocumentEchantillonnageEntete> DocumentEchantillonnageEntetes { get; set; } = new List<DocumentEchantillonnageEntete>();

    public virtual ICollection<DocumentSection> DocumentSections { get; set; } = new List<DocumentSection>();
}
