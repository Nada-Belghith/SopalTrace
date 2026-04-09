using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PlanEchantillonnageRegle
{
    public Guid Id { get; set; }

    public Guid FicheEnteteId { get; set; }

    public int? TailleMinLot { get; set; }

    public int? TailleMaxLot { get; set; }

    public string LettreCode { get; set; } = null!;

    public int EffectifEchantillonA { get; set; }

    public int NbPostesB { get; set; }

    public int? EffectifParPosteAb { get; set; }

    public int CritereAcceptationAc { get; set; }

    public int CritereRejetRe { get; set; }

    public virtual PlanEchantillonnageEntete FicheEntete { get; set; } = null!;
}
