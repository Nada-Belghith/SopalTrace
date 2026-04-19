using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class NatureComposantOperation
{
    public string NatureComposantCode { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public int OrdreGamme { get; set; }

    public bool EstObligatoire { get; set; }

    public virtual NatureComposant NatureComposantCodeNavigation { get; set; } = null!;

    public virtual Operation OperationCodeNavigation { get; set; } = null!;
}
