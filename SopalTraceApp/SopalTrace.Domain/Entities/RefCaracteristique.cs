using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefCaracteristique
{
    public Guid Id { get; set; }

    public string Libelle { get; set; } = null!;

    public string LibelleNormalise { get; set; } = null!;

    public Guid? TypeCaracteristiqueId { get; set; }

    public string? LimiteSpecTexteDefaut { get; set; }

    public string? InstructionDefaut { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<ModeleFabricationLigne> ModeleFabricationLignes { get; set; } = new List<ModeleFabricationLigne>();

    public virtual ICollection<PlanFabricationLigne> PlanFabricationLignes { get; set; } = new List<PlanFabricationLigne>();

    public virtual TypeCaracteristique? TypeCaracteristique { get; set; }
}
