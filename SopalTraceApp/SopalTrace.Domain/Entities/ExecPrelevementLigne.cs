using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecPrelevementLigne
{
    public Guid Id { get; set; }

    public Guid PrelevementId { get; set; }

    public Guid LignePlanId { get; set; }

    public string? Resultat { get; set; }

    public double? ValeurMesuree { get; set; }

    public string? Remarque { get; set; }

    public virtual ExecPrelevement Prelevement { get; set; } = null!;
}
