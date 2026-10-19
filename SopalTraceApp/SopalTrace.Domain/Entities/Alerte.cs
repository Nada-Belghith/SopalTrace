using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Alerte
{
    public Guid Id { get; set; }

    public string TypeAlerte { get; set; } = null!;

    public string CleEntite { get; set; } = null!;

    public string DonneesContexte { get; set; } = null!;

    public string Destinataires { get; set; } = null!;

    public DateTime DateAlerte { get; set; }

    public bool EstResolu { get; set; }

    public DateTime? DateResolution { get; set; }
}
