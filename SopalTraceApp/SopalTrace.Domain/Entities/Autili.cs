using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Autili
{
    public string Usr0 { get; set; } = null!;

    public string Intusr0 { get; set; } = null!;

    public int Enaflg0 { get; set; }

    public string Codmet0 { get; set; } = null!;

    public string? Addeml0 { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<DocumentEntete> DocumentEnteteCreeParNavigations { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<DocumentEntete> DocumentEnteteModifieParNavigations { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ICollection<ExecControleLigneReponse> ExecControleLigneReponses { get; set; } = new List<ExecControleLigneReponse>();

    public virtual ICollection<ExecControleTranche> ExecControleTranches { get; set; } = new List<ExecControleTranche>();

    public virtual ICollection<ExecPieceType> ExecPieceTypes { get; set; } = new List<ExecPieceType>();

    public virtual ICollection<ExecRcPosteBilan> ExecRcPosteBilans { get; set; } = new List<ExecRcPosteBilan>();

    public virtual ICollection<ExecRcPosteReponse> ExecRcPosteReponses { get; set; } = new List<ExecRcPosteReponse>();

    public virtual ICollection<ExecRegistreTracabilite> ExecRegistreTracabiliteCancelledByNavigations { get; set; } = new List<ExecRegistreTracabilite>();

    public virtual ICollection<ExecRegistreTracabilite> ExecRegistreTracabiliteMatriculeOperateurNavigations { get; set; } = new List<ExecRegistreTracabilite>();

    public virtual ICollection<ExecVerifMachineReponse> ExecVerifMachineReponses { get; set; } = new List<ExecVerifMachineReponse>();

    public virtual ICollection<MagExpeditionBl> MagExpeditionBls { get; set; } = new List<MagExpeditionBl>();

    public virtual ICollection<MagPreparationOf> MagPreparationOfs { get; set; } = new List<MagPreparationOf>();

    public virtual PreassignedRole? PreassignedRole { get; set; }

    public virtual ICollection<RefFormulaire> RefFormulaireCreeParNavigations { get; set; } = new List<RefFormulaire>();

    public virtual ICollection<RefFormulaire> RefFormulaireModifieParNavigations { get; set; } = new List<RefFormulaire>();

    public virtual UtilisateursApp? UtilisateursApp { get; set; }
}
