using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Article
{
    public string CodeArticle { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string? Designation2 { get; set; }

    public string NatureArticleCode { get; set; } = null!;

    public string TypeArticle { get; set; } = null!;

    public string Statut { get; set; } = null!;

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }

    public bool Actif { get; set; }

    public virtual ICollection<BomdNomenclature> BomdNomenclatureArticleParentNavigations { get; set; } = new List<BomdNomenclature>();

    public virtual ICollection<BomdNomenclature> BomdNomenclatureCodeComposantNavigations { get; set; } = new List<BomdNomenclature>();

    public virtual ICollection<MagPreparationOfLot> MagPreparationOfLots { get; set; } = new List<MagPreparationOfLot>();

    public virtual ICollection<MagQuickControlRapport> MagQuickControlRapports { get; set; } = new List<MagQuickControlRapport>();

    public virtual ICollection<MfgheadOrdreFabrication> MfgheadOrdreFabrications { get; set; } = new List<MfgheadOrdreFabrication>();

    public virtual ICollection<MfgmatBesoinOf> MfgmatBesoinOfs { get; set; } = new List<MfgmatBesoinOf>();

    public virtual NatureArticle NatureArticleCodeNavigation { get; set; } = null!;

    public virtual ICollection<PlanFabEntete> PlanFabEntetes { get; set; } = new List<PlanFabEntete>();

    public virtual ProduitFini? ProduitFini { get; set; }
}
