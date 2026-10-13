using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExecControleLigneReponse
{
    public Guid Id { get; set; }

    public Guid ExecControleOfid { get; set; }

    public Guid? ExecControleDocumentStatutId { get; set; }

    public Guid DocumentLigneId { get; set; }

    public string Contexte { get; set; } = null!;

    public string? NumeroReglage { get; set; }

    public string? TrancheHoraire { get; set; }

    public string ResultatType { get; set; } = null!;

    public decimal? ValeurMesuree { get; set; }

    public int? Quantite { get; set; }

    public string? DetailsNc { get; set; }

    public string? ActionsCorrection { get; set; }

    public string MatriculeOperateur { get; set; } = null!;

    public string? Equipe { get; set; }

    public DateTime DateSaisie { get; set; }

    public virtual DocumentLigne DocumentLigne { get; set; } = null!;

    public virtual Team? EquipeNavigation { get; set; }

    public virtual ExecControleDocumentStatut? ExecControleDocumentStatut { get; set; }

    public virtual ExecControleOf ExecControleOf { get; set; } = null!;

    public virtual Autili MatriculeOperateurNavigation { get; set; } = null!;
}
