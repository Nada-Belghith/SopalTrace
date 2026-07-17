using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecRcPosteLigneBilan
{
    public Guid Id { get; set; }

    public Guid RcPosteEnteteId { get; set; }

    public string MachineCode { get; set; } = null!;

    public string DesignationDefaut { get; set; } = null!;

    public double TotalPiecesTestees { get; set; }

    public double NbPiecesRebutees { get; set; }

    public double NbPiecesConformes { get; set; }

    public virtual ICollection<ExecRcPosteHeure> ExecRcPosteHeures { get; set; } = new List<ExecRcPosteHeure>();

    public virtual Machine MachineCodeNavigation { get; set; } = null!;

    public virtual ExecRcPosteEntete RcPosteEntete { get; set; } = null!;
}
