using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecutionDocumentType
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public bool Active { get; set; }

    public string? DocumentTypeCode { get; set; }

    public virtual TypeDocument? DocumentTypeCodeNavigation { get; set; }

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ICollection<ReportTemplate> ReportTemplates { get; set; } = new List<ReportTemplate>();
}
