using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ReportTemplate
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? DocumentTypeCode { get; set; }

    public string? ExecutionDocumentTypeCode { get; set; }

    public string? FormRole { get; set; }

    public virtual TypeDocument? DocumentTypeCodeNavigation { get; set; }

    public virtual ExecutionDocumentType? ExecutionDocumentTypeCodeNavigation { get; set; }

    public virtual FormRole? FormRoleNavigation { get; set; }

    public virtual ICollection<ReportTemplateInitialForm> ReportTemplateInitialForms { get; set; } = new List<ReportTemplateInitialForm>();

    public virtual ICollection<ReportTemplateVersion> ReportTemplateVersions { get; set; } = new List<ReportTemplateVersion>();
}
