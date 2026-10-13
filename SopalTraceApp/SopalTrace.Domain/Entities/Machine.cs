using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Machine
{
    public string CodeMachine { get; set; } = null!;

    public string Libelle { get; set; } = null!;

    public string OperationCode { get; set; } = null!;

    public bool Actif { get; set; }

    public string? VerifModele { get; set; }

    public bool VerifSansConformite { get; set; }

    public bool VerifFuiteEtalon { get; set; }

    public bool VerifMoyenControleNumerote { get; set; }

    public bool VerifSansDeltaP { get; set; }

    public bool VerifSansPression { get; set; }

    public bool VerifTablesEtalonSeparees { get; set; }

    public bool VerifFuiteEnPa { get; set; }

    public bool VerifColonnesParTableau { get; set; }

    public virtual ICollection<DocumentEntete> DocumentEntetes { get; set; } = new List<DocumentEntete>();

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; } = new List<ExecControleDocumentStatut>();

    public virtual ICollection<ExecControleOf> ExecControleOfMachineCodeNavigations { get; set; } = new List<ExecControleOf>();

    public virtual ICollection<ExecControleOf> ExecControleOfMachineCodePrevuNavigations { get; set; } = new List<ExecControleOf>();

    public virtual Operation OperationCodeNavigation { get; set; } = null!;

    public virtual ICollection<PosteTravail> CodePostes { get; set; } = new List<PosteTravail>();

    public virtual ICollection<RefFamilleCorp> RefFamilleCorps { get; set; } = new List<RefFamilleCorp>();
}
