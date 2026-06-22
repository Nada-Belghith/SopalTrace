using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefRegleEchantillonnage
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentSection> DocumentSections { get; set; } = new List<DocumentSection>();

    public virtual ICollection<ModeleFabricationSection> ModeleFabricationSections { get; set; } = new List<ModeleFabricationSection>();

    public virtual ICollection<PlanFabricationSection> PlanFabricationSections { get; set; } = new List<PlanFabricationSection>();
}
