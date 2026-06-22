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

    public Guid? RegleEchantillonnageId { get; set; }

    public string? Notes { get; set; }

    public string? NormeReference { get; set; }

    public int? NqaId { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual DocumentEntete Entete { get; set; } = null!;

    public virtual Nqa? Nqa { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual RefRegleEchantillonnage? RegleEchantillonnage { get; set; }

    public virtual TypeSection? TypeSection { get; set; }
}
