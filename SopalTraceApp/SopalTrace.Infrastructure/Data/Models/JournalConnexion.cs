using System;
using System.Collections.Generic;

namespace SopalTrace.Infrastructure.Data.Models;

public partial class JournalConnexion
{
    public Guid Id { get; set; }

    public string Matricule { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? AdresseIp { get; set; }

    public string? Details { get; set; }

    public DateTime? DateAction { get; set; }
}
