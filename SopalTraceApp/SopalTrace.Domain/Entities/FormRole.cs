using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class FormRole
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public int DisplayOrder { get; set; }

    /// <summary>DOCUMENT (contenu défini par le superviseur qualité) ou RESPONSE_SHEET (fiche de réponse des opérateurs).</summary>
    public string Nature { get; set; } = null!;

    public bool ScopeByFamily { get; set; }

    public bool ScopeByWorkstation { get; set; }

    public bool ScopeByMachine { get; set; }

    public bool ScopeByNature { get; set; }

    public bool ScopeByValveType { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<RefFormulaire> RefFormulaires { get; set; } = new List<RefFormulaire>();

    public virtual ICollection<ReportTemplate> ReportTemplates { get; set; } = new List<ReportTemplate>();
}
