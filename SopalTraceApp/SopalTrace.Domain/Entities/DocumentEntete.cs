using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentEntete
{
    public Guid Id { get; set; }

    public string TypeDocumentCode { get; set; } = null!;

    public string Portee { get; set; } = null!;

    public string? CodeArticleSageVersionne { get; set; }

    public Guid? DocumentSourceId { get; set; }

    public string Nom { get; set; } = null!;

    public string? Designation { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? OperationCode { get; set; }

    public Guid? FormulaireId { get; set; }

    public string? LegendeMoyens { get; set; }

    public string? Remarques { get; set; }

    public string? CommentaireVersion { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? NatureArticleCode { get; set; }

    public string? FamilleProduitFiniCode { get; set; }

    public string? PosteCode { get; set; }

    public string? MachineCode { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Autili CreeParNavigation { get; set; } = null!;

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<DocumentSection> DocumentSections { get; set; } = new List<DocumentSection>();

    public virtual DocumentEntete? DocumentSource { get; set; }

    public virtual ICollection<DocumentVerifMachineFamille> DocumentVerifMachineFamilles { get; set; } = new List<DocumentVerifMachineFamille>();

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ICollection<ExecControleOf> ExecControleOfs { get; set; } = new List<ExecControleOf>();

    public virtual FamilleProduitFini? FamilleProduitFiniCodeNavigation { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual ICollection<DocumentEntete> InverseDocumentSource { get; set; } = new List<DocumentEntete>();

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual Autili? ModifieParNavigation { get; set; }

    public virtual NatureArticle? NatureArticleCodeNavigation { get; set; }

    public virtual Operation? OperationCodeNavigation { get; set; }

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual SamplingPlan? SamplingPlan { get; set; }

    public virtual TypeDocument TypeDocumentCodeNavigation { get; set; } = null!;
}
