using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class GroupeInstrumentDetail
{
    public Guid Id { get; set; }

    public Guid GroupeId { get; set; }

    public string CodeInstrument { get; set; } = null!;

    public virtual Instrument CodeInstrumentNavigation { get; set; } = null!;

    public virtual GroupeInstrument Groupe { get; set; } = null!;
}
