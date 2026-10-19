using System;

namespace SopalTrace.Domain.Entities;

/// <summary>
/// Demande de modèle : ce que l'administrateur doit rattacher à un formulaire, et pourquoi (nouveau formulaire, nouvelle
/// structure, ou demande du superviseur qualité avec son choix de version).
/// </summary>
public partial class ReportTemplateRequest
{
    public long Id { get; set; }

    public Guid FormId { get; set; }

    /// <summary>NEW_FORM, NEW_STRUCTURE ou SUPERVISOR.</summary>
    public string Origin { get; set; } = null!;

    public string? Reason { get; set; }

    /// <summary>SAME_VERSION ou NEXT_VERSION (demande du superviseur seulement).</summary>
    public string? VersionChange { get; set; }

    /// <summary>PENDING, DONE ou CANCELLED.</summary>
    public string Status { get; set; } = null!;

    public string? RequestedBy { get; set; }

    public DateTime RequestedAt { get; set; }

    public string? HandledBy { get; set; }

    public DateTime? HandledAt { get; set; }

    public long? AssignmentId { get; set; }

    public virtual RefFormulaire Form { get; set; } = null!;

    public virtual ReportTemplateAssignment? Assignment { get; set; }
}
