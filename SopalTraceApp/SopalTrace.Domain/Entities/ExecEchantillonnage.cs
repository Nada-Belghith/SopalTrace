using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecEchantillonnage
{
    public Guid Id { get; set; }

    public Guid ExecControleDocumentStatutId { get; set; }

    public int TailleLot { get; set; }

    public int NbPostesB { get; set; }

    public string LettreCode { get; set; } = null!;

    public int EffectifEchantillonA { get; set; }

    public int? EffectifParPosteAb { get; set; }

    public int CritereAcceptationAc { get; set; }

    public int CritereRejetRe { get; set; }

    public virtual ExecControleDocumentStatut ExecControleDocumentStatut { get; set; } = null!;
}
