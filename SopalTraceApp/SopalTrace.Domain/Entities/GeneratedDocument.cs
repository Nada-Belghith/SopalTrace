using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class GeneratedDocument
{
    public Guid Id { get; set; }

    public Guid? DocumentEnteteId { get; set; }

    public Guid? ExecControleDocumentStatutId { get; set; }

    public Guid ReportTemplateVersionId { get; set; }

    public string SourceDataSha256 { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public byte[] PdfContent { get; set; } = null!;

    public string? PdfSha256 { get; set; }

    public byte[] WorkbookContent { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public string? RequestedBy { get; set; }

    public virtual DocumentEntete? DocumentEntete { get; set; }

    public virtual ExecControleDocumentStatut? ExecControleDocumentStatut { get; set; }

    public virtual ReportTemplateVersion ReportTemplateVersion { get; set; } = null!;

    public virtual Autili? RequestedByNavigation { get; set; }
}
