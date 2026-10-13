using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class Instrument
{
    public string CodeInstrument { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public bool Actif { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<ExecEchantillonnage> ExecEchantillonnages { get; set; } = new List<ExecEchantillonnage>();
}
