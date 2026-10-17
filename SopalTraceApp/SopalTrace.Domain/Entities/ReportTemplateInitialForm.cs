using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ReportTemplateInitialForm
{
    public string FormCodeReference { get; set; } = null!;

    public string ReportTemplateCode { get; set; } = null!;

    public virtual ReportTemplate ReportTemplateCodeNavigation { get; set; } = null!;
}
