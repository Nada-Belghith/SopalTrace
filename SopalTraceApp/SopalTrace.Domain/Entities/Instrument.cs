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

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual ICollection<PlanAssLigne> PlanAssLignes { get; set; } = new List<PlanAssLigne>();

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();

    public virtual ICollection<PlanPfLigne> PlanPfLignes { get; set; } = new List<PlanPfLigne>();
}
