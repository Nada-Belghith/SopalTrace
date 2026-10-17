using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class NatureArticle
{
    public string Code { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public bool PlanGenerique { get; set; }

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<NatureArticleOperation> NatureArticleOperations { get; set; } = new List<NatureArticleOperation>();

    public virtual ICollection<RefFormulaire> RefFormulaires { get; set; } = new List<RefFormulaire>();
}
