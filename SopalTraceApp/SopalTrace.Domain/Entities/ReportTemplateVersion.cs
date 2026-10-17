using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ReportTemplateVersion
{
    public Guid Id { get; set; }

    public string ReportTemplateCode { get; set; } = null!;

    /// <summary>Formulaire propriétaire (VersionGroupId de ses lignes) ; null : modèle par défaut des documents sans formulaire.</summary>
    public Guid? FormGroupId { get; set; }

    /// <summary>Nom du modèle, propre à son formulaire.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Numéro dans son formulaire (dans son type pour un modèle par défaut).</summary>
    public int VersionNumber { get; set; }

    public string FileName { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    public string? ContentSha256 { get; set; }

    public string Status { get; set; } = null!;

    public string? ValidationWarnings { get; set; }

    public string? Comment { get; set; }

    public string? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public string? PublishedBy { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();

    public virtual Autili? PublishedByNavigation { get; set; }

    public virtual ICollection<ReportTemplateAssignment> ReportTemplateAssignments { get; set; } = new List<ReportTemplateAssignment>();

    public virtual ReportTemplate ReportTemplateCodeNavigation { get; set; } = null!;

    public virtual Autili? UploadedByNavigation { get; set; }
}
