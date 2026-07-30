using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleLigneReponse
{
    public Guid Id { get; set; }

    public Guid ExecControleOfid { get; set; }

    public Guid? DocumentLigneId { get; set; }

    public string Contexte { get; set; } = null!;

    public string? NumeroReglage { get; set; }

    public string ResultatType { get; set; } = null!;

    public decimal? ValeurMesuree { get; set; }

    public string? DetailsNc { get; set; }

    public string? ActionsCorrection { get; set; }

    public Guid OperateurId { get; set; }

    public DateTime DateSaisie { get; set; }

    public virtual DocumentLigne? DocumentLigne { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual UtilisateursApp Operateur { get; set; } = null!;
}
