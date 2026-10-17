using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class RefFormulaire
{
    public Guid Id { get; set; }

    public string CodeReference { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? CreePar { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? Role { get; set; }

    public string? PosteCode { get; set; }

    public string? FamilleProduitFiniCode { get; set; }

    public string? TypeRobinetCode { get; set; }

    public string? MachineCode { get; set; }

    public string? NatureArticleCode { get; set; }

    public Guid VersionGroupId { get; set; }

    public long CreationOrder { get; set; }

    public virtual Autili? CreeParNavigation { get; set; }

    public virtual DocumentEntete? DocumentEntete { get; set; }

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual FamilleProduitFini? FamilleProduitFiniCodeNavigation { get; set; }

    public virtual FormCodeChange? FormCodeChangeNewForm { get; set; }

    public virtual FormCodeChange? FormCodeChangePreviousForm { get; set; }

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual Autili? ModifieParNavigation { get; set; }

    public virtual NatureArticle? NatureArticleCodeNavigation { get; set; }

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual ICollection<RefFormulaireColonneDef> RefFormulaireColonneDefs { get; set; } = new List<RefFormulaireColonneDef>();

    public virtual ICollection<RefFormulaireEquipe> RefFormulaireEquipes { get; set; } = new List<RefFormulaireEquipe>();

    public virtual ICollection<ReportTemplateAssignment> ReportTemplateAssignments { get; set; } = new List<ReportTemplateAssignment>();

    public virtual FormRole? RoleNavigation { get; set; }

    public virtual TypeRobinet? TypeRobinetCodeNavigation { get; set; }
}
