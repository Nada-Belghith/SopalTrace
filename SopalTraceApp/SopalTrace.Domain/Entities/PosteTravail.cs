using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class PosteTravail
{
    public string CodePoste { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ICollection<ExecControleOfPoste> ExecControleOfPostes { get; set; } = new List<ExecControleOfPoste>();

    public virtual ICollection<ExecPrelevementIntermediaire> ExecPrelevementIntermediaires { get; set; } = new List<ExecPrelevementIntermediaire>();

    public virtual ICollection<ExecProductionPeriod> ExecProductionPeriods { get; set; } = new List<ExecProductionPeriod>();

    public virtual ICollection<ExecSessionPost> ExecSessionPosts { get; set; } = new List<ExecSessionPost>();

    public virtual ICollection<RefFormulaire> RefFormulaires { get; set; } = new List<RefFormulaire>();

    public virtual ICollection<Machine> CodeMachines { get; set; } = new List<Machine>();
}
