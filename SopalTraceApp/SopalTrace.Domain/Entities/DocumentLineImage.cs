using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentLineImage
{
    public Guid LineId { get; set; }

    public string ContentType { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    public virtual DocumentLigne Line { get; set; } = null!;
}
