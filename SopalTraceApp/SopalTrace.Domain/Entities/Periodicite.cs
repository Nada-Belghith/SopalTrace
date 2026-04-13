using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Periodicite
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public int? FrequenceNum { get; set; }

    public string? FrequenceUnite { get; set; }

    public int OrdreAffichage { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<ModeleFabLigne> ModeleFabLignes { get; set; } = new List<ModeleFabLigne>();

    public virtual ICollection<OutilControle> OutilControles { get; set; } = new List<OutilControle>();

    public virtual ICollection<PlanAssSection> PlanAssSections { get; set; } = new List<PlanAssSection>();

    public virtual ICollection<PlanFabLigne> PlanFabLignes { get; set; } = new List<PlanFabLigne>();

    public virtual ICollection<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; } = new List<PlanVerifMachineEcheance>();
}
