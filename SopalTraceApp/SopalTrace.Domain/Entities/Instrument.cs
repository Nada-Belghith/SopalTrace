using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Instrument
{
    public string CodeInstrument { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string? Categorie { get; set; }

    public double? PrecisionLecture { get; set; }

    public string? Unite { get; set; }

    public DateOnly? DateEtalonnage { get; set; }

    public DateOnly? DateProchaineVerif { get; set; }

    public string Statut { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<GroupeInstrumentDetail> GroupeInstrumentDetails { get; set; } = new List<GroupeInstrumentDetail>();

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();
}
