using System;

namespace SopalTrace.Domain.Entities;

public class JournalConnexion
{
    public Guid Id { get; set; }

    public string Matricule { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime? DateAction { get; set; }
}