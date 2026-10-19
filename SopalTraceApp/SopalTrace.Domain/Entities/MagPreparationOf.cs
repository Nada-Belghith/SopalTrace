using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MagPreparationOf
{
    public Guid Id { get; set; }

    public string NumeroOf { get; set; } = null!;

    public string MatriculeMagasinier { get; set; } = null!;

    public string Statut { get; set; } = null!;

    public DateTime? DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    public string? IncompleteReason { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<MagPreparationOfLot> MagPreparationOfLots { get; set; } = new List<MagPreparationOfLot>();

    public virtual Autili MatriculeMagasinierNavigation { get; set; } = null!;

    public virtual MfgheadOrdreFabrication NumeroOfNavigation { get; set; } = null!;
}
