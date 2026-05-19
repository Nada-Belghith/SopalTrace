using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class NatureArticle
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string Origine { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();

    public virtual ICollection<ModeleFabEntete> ModeleFabEntetes { get; set; } = new List<ModeleFabEntete>();

    public virtual ICollection<NatureArticleOperation> NatureArticleOperations { get; set; } = new List<NatureArticleOperation>();

    public virtual ICollection<PlanAssEntete> PlanAssEntetes { get; set; } = new List<PlanAssEntete>();
}
