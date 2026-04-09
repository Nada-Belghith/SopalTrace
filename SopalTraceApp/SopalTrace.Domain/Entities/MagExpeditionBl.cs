using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class MagExpeditionBl
{
    public Guid Id { get; set; }

    public string NumeroBl { get; set; } = null!;

    public string MatriculeMagasinier { get; set; } = null!;

    public string Statut { get; set; } = null!;

    public DateTime? DateDebut { get; set; }

    public DateTime? DateFin { get; set; }

    public virtual ICollection<MagExpeditionBlScanOf> MagExpeditionBlScanOfs { get; set; } = new List<MagExpeditionBlScanOf>();

    public virtual UtilisateursApp MatriculeMagasinierNavigation { get; set; } = null!;
}
