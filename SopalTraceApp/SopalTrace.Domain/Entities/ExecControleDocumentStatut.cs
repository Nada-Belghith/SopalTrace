using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleDocumentStatut
{
    public Guid Id { get; set; }

    public Guid ExecControleOfId { get; set; }

    public string TypeDocument { get; set; } = null!;

    public string? PosteCode { get; set; }

    public string? MachineCode { get; set; }

    public Guid? DocId { get; set; }

    public Guid? SessionId { get; set; }

    public bool EstDemarrageTermine { get; set; }

    public bool EstPauseTermine { get; set; }

    public bool EstTermine { get; set; }

    public DateTime? DateTermine { get; set; }

    public string? Equipe { get; set; }

    public DateTime? DateExecution { get; set; }

    public string? MatriculeOperateur { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual DocumentEntete? Doc { get; set; }

    public virtual Team? EquipeNavigation { get; set; }

    public virtual ICollection<ExecControleLigneReponse> ExecControleLigneReponses { get; set; } = new List<ExecControleLigneReponse>();

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual ICollection<ExecEchantillonnage> ExecEchantillonnages { get; set; } = new List<ExecEchantillonnage>();

    public virtual ICollection<ExecRcPosteBilan> ExecRcPosteBilans { get; set; } = new List<ExecRcPosteBilan>();

    public virtual ICollection<ExecRcPosteReponse> ExecRcPosteReponses { get; set; } = new List<ExecRcPosteReponse>();

    public virtual ICollection<ExecVerifMachineReponse> ExecVerifMachineReponses { get; set; } = new List<ExecVerifMachineReponse>();

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual Autili? MatriculeOperateurNavigation { get; set; }

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual ExecControleOfSession? Session { get; set; }

    public virtual ExecutionDocumentType TypeDocumentNavigation { get; set; } = null!;
}
