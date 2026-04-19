using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class NatureComposant
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string? TypeLotAttendu { get; set; }

    public bool EstGenerique { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<Itmmaster> Itmmasters { get; set; } = new List<Itmmaster>();

    public virtual ICollection<ModeleFabEntete> ModeleFabEntetes { get; set; } = new List<ModeleFabEntete>();

    public virtual ICollection<NatureComposantOperation> NatureComposantOperations { get; set; } = new List<NatureComposantOperation>();
}
