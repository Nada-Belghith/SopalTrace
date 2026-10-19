using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Sorder
{
    public string NumeroCommande { get; set; } = null!;

    public string CodeClient { get; set; } = null!;

    public DateOnly DateCommande { get; set; }

    public string? ReferenceCommandeClient { get; set; }

    public virtual ICollection<ExternalDocument> ExternalDocuments { get; set; } = new List<ExternalDocument>();

    public virtual ICollection<Sdelivery> Sdeliveries { get; set; } = new List<Sdelivery>();
}
