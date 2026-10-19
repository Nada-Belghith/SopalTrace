using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentSection
{
    public Guid Id { get; set; }

    public Guid EnteteId { get; set; }

    public int OrdreAffiche { get; set; }

    public string LibelleSection { get; set; } = null!;

    public Guid? TypeSectionId { get; set; }

    public Guid? PeriodiciteId { get; set; }

    public string? Notes { get; set; }

    public int? NqaId { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual DocumentEntete Entete { get; set; } = null!;

    public virtual ICollection<ExecPrelevementIntermediaire> ExecPrelevementIntermediaires { get; set; } = new List<ExecPrelevementIntermediaire>();

    public virtual ICollection<ExecProductionPeriod> ExecProductionPeriods { get; set; } = new List<ExecProductionPeriod>();

    public virtual Nqa? Nqa { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual TypeSection? TypeSection { get; set; }
}
