using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefIso2859LettresCode
{
    public int Id { get; set; }

    public int QteMin { get; set; }

    public int QteMax { get; set; }

    public string NiveauControle { get; set; } = null!;

    public string CodeLettre { get; set; } = null!;
}
