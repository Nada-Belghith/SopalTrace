using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Team
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ICollection<ExecControleLigneReponse> ExecControleLigneReponses { get; set; } = new List<ExecControleLigneReponse>();

    public virtual ICollection<ExecControleOfSession> ExecControleOfSessions { get; set; } = new List<ExecControleOfSession>();

    public virtual ICollection<ExecRcPosteBilan> ExecRcPosteBilans { get; set; } = new List<ExecRcPosteBilan>();

    public virtual ICollection<ExecRcPosteReponse> ExecRcPosteReponses { get; set; } = new List<ExecRcPosteReponse>();

    public virtual ICollection<ExecRegistreTracabilite> ExecRegistreTracabilites { get; set; } = new List<ExecRegistreTracabilite>();

    public virtual ICollection<RefFormulaireEquipe> RefFormulaireEquipes { get; set; } = new List<RefFormulaireEquipe>();
}
