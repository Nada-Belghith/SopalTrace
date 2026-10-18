using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Porder
{
    public string NumeroCommande { get; set; } = null!;

    public string CodeFournisseur { get; set; } = null!;

    public DateOnly DateCommande { get; set; }

    public virtual ICollection<ExternalDocument> ExternalDocuments { get; set; } = new List<ExternalDocument>();
}
