using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefIso2859Echantillonnage
{
    public int Id { get; set; }

    public string CodeLettre { get; set; } = null!;

    public double ValeurNqa { get; set; }

    public int Quantite { get; set; }
}
