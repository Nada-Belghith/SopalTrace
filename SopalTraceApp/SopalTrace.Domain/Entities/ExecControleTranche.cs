using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleTranche
{
    public Guid Id { get; set; }

    public Guid ExecControleOfid { get; set; }

    public string TrancheHoraire { get; set; } = null!;

    public DateTime HeureDebut { get; set; }

    public DateTime HeureFin { get; set; }

    public string? ResultatFinal { get; set; }

    public string? DetailsNc { get; set; }

    public string? ActionsCorrection { get; set; }

    public string? MatriculeApprobateur { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual ICollection<ExecPrelevement> ExecPrelevements { get; set; } = new List<ExecPrelevement>();
}
