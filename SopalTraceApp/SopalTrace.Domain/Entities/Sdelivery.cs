using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Sdelivery
{
    public string NumeroBl { get; set; } = null!;

    public string CodeClient { get; set; } = null!;

    public DateOnly DateExpedition { get; set; }

    public string StatutBl { get; set; } = null!;
}
